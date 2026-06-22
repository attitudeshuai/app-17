using Mapster;
using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.MedicineShare;
using MedCabinet.Application.Interfaces;
using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Enums;
using MedCabinet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedCabinet.Application.Services;

public class MedicineShareService : IMedicineShareService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MedicineShareService> _logger;

    public MedicineShareService(IUnitOfWork unitOfWork, ILogger<MedicineShareService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<MedicineShareDto>>> GetSharesAsync(ShareQueryParamsDto queryParams, int userId)
    {
        try
        {
            var userHouseholdIds = await GetUserHouseholdIdsAsync(userId);
            if (!userHouseholdIds.Any())
            {
                return ApiResponse<PagedResult<MedicineShareDto>>.Success(CreateEmptyPagedResult(queryParams));
            }

            var householdIdFilter = queryParams.HouseholdId;
            var statusFilter = queryParams.Status;

            var (items, totalCount) = await _unitOfWork.MedicineShares.GetPagedAsync(
                queryParams.PageIndex,
                queryParams.PageSize,
                ms => (userHouseholdIds.Contains(ms.LenderHouseholdId) || userHouseholdIds.Contains(ms.BorrowerHouseholdId)) &&
                     (!householdIdFilter.HasValue || ms.LenderHouseholdId == householdIdFilter.Value || ms.BorrowerHouseholdId == householdIdFilter.Value) &&
                     (!statusFilter.HasValue || ms.Status == statusFilter.Value),
                queryParams.SortBy ?? "CreatedAt",
                queryParams.SortDescending);

            var dtos = items.Adapt<List<MedicineShareDto>>();
            await LoadSharedMedicinesAsync(dtos, items);

            var result = CreatePagedResult(dtos, totalCount, queryParams);
            return ApiResponse<PagedResult<MedicineShareDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取共享关系列表失败");
            return ApiResponse<PagedResult<MedicineShareDto>>.Error("获取共享关系列表失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedicineShareDto>> GetShareByIdAsync(int id, int userId)
    {
        try
        {
            var share = await _unitOfWork.MedicineShares.GetByIdAsync(id);
            if (share == null)
            {
                return ApiResponse<MedicineShareDto>.Error("共享关系不存在", 404);
            }

            var userHouseholdIds = await GetUserHouseholdIdsAsync(userId);
            if (!userHouseholdIds.Contains(share.LenderHouseholdId) && !userHouseholdIds.Contains(share.BorrowerHouseholdId))
            {
                return ApiResponse<MedicineShareDto>.Error("无权限访问此共享关系", 403);
            }

            var dto = share.Adapt<MedicineShareDto>();
            var sharedMedicines = await _unitOfWork.SharedMedicines.FindAsync(sm => sm.MedicineShareId == id);
            dto.SharedMedicines = sharedMedicines.Adapt<List<SharedMedicineDto>>();

            return ApiResponse<MedicineShareDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取共享关系详情失败");
            return ApiResponse<MedicineShareDto>.Error("获取共享关系详情失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedicineShareDto>> CreateShareAsync(CreateShareRequestDto request, int userId)
    {
        try
        {
            var hasPermission = await HasHouseholdPermissionAsync(request.LenderHouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<MedicineShareDto>.Error("无权限创建共享关系", 403);
            }

            if (request.LenderHouseholdId == request.BorrowerHouseholdId)
            {
                return ApiResponse<MedicineShareDto>.Error("不能与自己的家庭共享", 400);
            }

            var lenderHousehold = await _unitOfWork.Households.GetByIdAsync(request.LenderHouseholdId);
            var borrowerHousehold = await _unitOfWork.Households.GetByIdAsync(request.BorrowerHouseholdId);
            if (lenderHousehold == null || borrowerHousehold == null)
            {
                return ApiResponse<MedicineShareDto>.Error("家庭不存在", 404);
            }

            var inviteCode = await GenerateUniqueInviteCodeAsync();

            await _unitOfWork.BeginTransactionAsync();

            var share = new MedicineShare
            {
                LenderHouseholdId = request.LenderHouseholdId,
                BorrowerHouseholdId = request.BorrowerHouseholdId,
                InviteCode = inviteCode,
                CreatedByUserId = userId,
                ExpiresAt = request.ExpiresAt,
                Status = ShareStatus.Pending,
                Notes = request.Notes,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.MedicineShares.AddAsync(share);
            await _unitOfWork.SaveChangesAsync();

            if (request.MedicineIds != null && request.MedicineIds.Any())
            {
                foreach (var medicineId in request.MedicineIds.Distinct())
                {
                    var medicine = await _unitOfWork.Medicines.GetByIdAsync(medicineId);
                    if (medicine != null && medicine.HouseholdId == request.LenderHouseholdId)
                    {
                        var sharedMedicine = new SharedMedicine
                        {
                            MedicineShareId = share.Id,
                            MedicineId = medicineId,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };
                        await _unitOfWork.SharedMedicines.AddAsync(sharedMedicine);
                    }
                }
                await _unitOfWork.SaveChangesAsync();
            }

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation($"创建共享关系成功: ID={share.Id}, 邀请码={inviteCode}");

            var dto = share.Adapt<MedicineShareDto>();
            var sharedMedicines = await _unitOfWork.SharedMedicines.FindAsync(sm => sm.MedicineShareId == share.Id);
            dto.SharedMedicines = sharedMedicines.Adapt<List<SharedMedicineDto>>();

            return ApiResponse<MedicineShareDto>.Success(dto, "创建成功");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "创建共享关系失败");
            return ApiResponse<MedicineShareDto>.Error("创建共享关系失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedicineShareDto>> AcceptShareByCodeAsync(AcceptShareByCodeRequestDto request, int userId)
    {
        try
        {
            var shares = await _unitOfWork.MedicineShares.FindAsync(ms => ms.InviteCode == request.InviteCode);
            var share = shares.FirstOrDefault();

            if (share == null)
            {
                return ApiResponse<MedicineShareDto>.Error("邀请码无效", 404);
            }

            if (share.BorrowerHouseholdId != request.BorrowerHouseholdId)
            {
                return ApiResponse<MedicineShareDto>.Error("邀请码不匹配此家庭", 400);
            }

            var hasPermission = await HasHouseholdPermissionAsync(request.BorrowerHouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<MedicineShareDto>.Error("无权限接受共享", 403);
            }

            if (share.Status != ShareStatus.Pending)
            {
                return ApiResponse<MedicineShareDto>.Error("此共享邀请已处理或已失效", 400);
            }

            if (share.ExpiresAt.HasValue && share.ExpiresAt.Value < DateTime.Now)
            {
                share.Status = ShareStatus.Expired;
                await _unitOfWork.MedicineShares.UpdateAsync(share);
                await _unitOfWork.SaveChangesAsync();
                return ApiResponse<MedicineShareDto>.Error("邀请码已过期", 400);
            }

            share.Status = ShareStatus.Active;
            await _unitOfWork.MedicineShares.UpdateAsync(share);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"接受共享成功: 共享ID={share.Id}, 用户ID={userId}");

            var dto = share.Adapt<MedicineShareDto>();
            var sharedMedicines = await _unitOfWork.SharedMedicines.FindAsync(sm => sm.MedicineShareId == share.Id);
            dto.SharedMedicines = sharedMedicines.Adapt<List<SharedMedicineDto>>();

            return ApiResponse<MedicineShareDto>.Success(dto, "接受共享成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "接受共享失败");
            return ApiResponse<MedicineShareDto>.Error("接受共享失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse> RevokeShareAsync(int id, int userId)
    {
        try
        {
            var share = await _unitOfWork.MedicineShares.GetByIdAsync(id);
            if (share == null)
            {
                return ApiResponse.Error("共享关系不存在", 404);
            }

            var hasLenderPermission = await HasHouseholdPermissionAsync(share.LenderHouseholdId, userId, "Owner", "Admin");
            var hasBorrowerPermission = await HasHouseholdPermissionAsync(share.BorrowerHouseholdId, userId, "Owner", "Admin");

            if (!hasLenderPermission && !hasBorrowerPermission)
            {
                return ApiResponse.Error("无权限解除共享关系", 403);
            }

            if (share.Status == ShareStatus.Revoked || share.Status == ShareStatus.Expired)
            {
                return ApiResponse.Error("共享关系已解除或已过期", 400);
            }

            share.Status = ShareStatus.Revoked;
            share.RevokedAt = DateTime.Now;
            share.RevokedByUserId = userId;

            await _unitOfWork.MedicineShares.UpdateAsync(share);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"解除共享关系成功: ID={id}, 操作人={userId}");

            return ApiResponse.Success("解除共享成功，历史借用记录已保留");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解除共享关系失败");
            return ApiResponse.Error("解除共享关系失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedicineShareDto>> UpdateSharedMedicinesAsync(int shareId, UpdateSharedMedicinesRequestDto request, int userId)
    {
        try
        {
            var share = await _unitOfWork.MedicineShares.GetByIdAsync(shareId);
            if (share == null)
            {
                return ApiResponse<MedicineShareDto>.Error("共享关系不存在", 404);
            }

            var hasPermission = await HasHouseholdPermissionAsync(share.LenderHouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<MedicineShareDto>.Error("无权限修改共享药品", 403);
            }

            if (share.Status != ShareStatus.Active && share.Status != ShareStatus.Pending)
            {
                return ApiResponse<MedicineShareDto>.Error("共享关系状态不允许修改", 400);
            }

            var existingSharedMedicines = await _unitOfWork.SharedMedicines.FindAsync(sm => sm.MedicineShareId == shareId);

            await _unitOfWork.BeginTransactionAsync();

            foreach (var existing in existingSharedMedicines)
            {
                if (request.MedicineIds == null || !request.MedicineIds.Contains(existing.MedicineId))
                {
                    existing.IsActive = false;
                    await _unitOfWork.SharedMedicines.UpdateAsync(existing);
                }
            }

            if (request.MedicineIds != null)
            {
                foreach (var medicineId in request.MedicineIds.Distinct())
                {
                    var medicine = await _unitOfWork.Medicines.GetByIdAsync(medicineId);
                    if (medicine == null || medicine.HouseholdId != share.LenderHouseholdId)
                    {
                        continue;
                    }

                    var existing = existingSharedMedicines.FirstOrDefault(sm => sm.MedicineId == medicineId);
                    if (existing != null)
                    {
                        if (!existing.IsActive)
                        {
                            existing.IsActive = true;
                            await _unitOfWork.SharedMedicines.UpdateAsync(existing);
                        }
                    }
                    else
                    {
                        var sharedMedicine = new SharedMedicine
                        {
                            MedicineShareId = shareId,
                            MedicineId = medicineId,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };
                        await _unitOfWork.SharedMedicines.AddAsync(sharedMedicine);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation($"更新共享药品成功: 共享ID={shareId}");

            var dto = share.Adapt<MedicineShareDto>();
            var updatedSharedMedicines = await _unitOfWork.SharedMedicines.FindAsync(sm => sm.MedicineShareId == shareId);
            dto.SharedMedicines = updatedSharedMedicines.Adapt<List<SharedMedicineDto>>();

            return ApiResponse<MedicineShareDto>.Success(dto, "更新成功");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "更新共享药品失败");
            return ApiResponse<MedicineShareDto>.Error("更新共享药品失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<PagedResult<SharedMedicineDto>>> GetSharedMedicinesForBorrowerAsync(int shareId, int userId)
    {
        try
        {
            var share = await _unitOfWork.MedicineShares.GetByIdAsync(shareId);
            if (share == null)
            {
                return ApiResponse<PagedResult<SharedMedicineDto>>.Error("共享关系不存在", 404);
            }

            if (share.Status != ShareStatus.Active)
            {
                return ApiResponse<PagedResult<SharedMedicineDto>>.Error("共享关系未激活", 400);
            }

            var userHouseholdIds = await GetUserHouseholdIdsAsync(userId);
            if (!userHouseholdIds.Contains(share.BorrowerHouseholdId) && !userHouseholdIds.Contains(share.LenderHouseholdId))
            {
                return ApiResponse<PagedResult<SharedMedicineDto>>.Error("无权限查看共享药品", 403);
            }

            var sharedMedicines = await _unitOfWork.SharedMedicines.FindAsync(sm => sm.MedicineShareId == shareId && sm.IsActive);
            var dtos = sharedMedicines.Adapt<List<SharedMedicineDto>>();

            var result = new PagedResult<SharedMedicineDto>
            {
                Items = dtos,
                TotalCount = dtos.Count,
                PageIndex = 1,
                PageSize = dtos.Count,
                TotalPages = 1,
                HasPreviousPage = false,
                HasNextPage = false
            };

            return ApiResponse<PagedResult<SharedMedicineDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取共享药品列表失败");
            return ApiResponse<PagedResult<SharedMedicineDto>>.Error("获取共享药品列表失败: " + ex.Message, 500);
        }
    }

    private async Task<string> GenerateUniqueInviteCodeAsync()
    {
        var random = new Random();
        string code;
        do
        {
            code = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        } while (await _unitOfWork.MedicineShares.ExistsAsync(ms => ms.InviteCode == code));

        return code;
    }

    private async Task<List<int>> GetUserHouseholdIdsAsync(int userId)
    {
        var members = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
        return members.Select(hm => hm.HouseholdId).ToList();
    }

    private async Task<bool> HasHouseholdPermissionAsync(int householdId, int userId, params string[] allowedRoles)
    {
        var members = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.HouseholdId == householdId && hm.UserId == userId);
        var member = members.FirstOrDefault();
        return member != null && allowedRoles.Contains(member.Role);
    }

    private async Task LoadSharedMedicinesAsync(List<MedicineShareDto> dtos, IEnumerable<MedicineShare> shares)
    {
        var shareIds = shares.Select(s => s.Id).ToList();
        var allSharedMedicines = await _unitOfWork.SharedMedicines.FindAsync(sm => shareIds.Contains(sm.MedicineShareId));

        foreach (var dto in dtos)
        {
            dto.SharedMedicines = allSharedMedicines
                .Where(sm => sm.MedicineShareId == dto.Id)
                .Adapt<List<SharedMedicineDto>>();
        }
    }

    private static PagedResult<MedicineShareDto> CreateEmptyPagedResult(ShareQueryParamsDto queryParams)
    {
        return new PagedResult<MedicineShareDto>
        {
            Items = new List<MedicineShareDto>(),
            TotalCount = 0,
            PageIndex = queryParams.PageIndex,
            PageSize = queryParams.PageSize,
            TotalPages = 0,
            HasPreviousPage = false,
            HasNextPage = false
        };
    }

    private static PagedResult<MedicineShareDto> CreatePagedResult(List<MedicineShareDto> items, int totalCount, ShareQueryParamsDto queryParams)
    {
        return new PagedResult<MedicineShareDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = queryParams.PageIndex,
            PageSize = queryParams.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize),
            HasPreviousPage = queryParams.PageIndex > 1,
            HasNextPage = queryParams.PageIndex * queryParams.PageSize < totalCount
        };
    }
}
