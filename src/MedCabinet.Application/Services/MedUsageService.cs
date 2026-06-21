using Mapster;
using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.MedUsage;
using MedCabinet.Application.Interfaces;
using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedCabinet.Application.Services;

public class MedUsageService : IMedUsageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MedUsageService> _logger;

    public MedUsageService(IUnitOfWork unitOfWork, ILogger<MedUsageService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<MedUsageDto>>> GetMedUsagesAsync(MedUsageQueryParamsDto queryParams, int userId)
    {
        try
        {
            // 获取用户有权限的家庭
            var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
            var householdIds = userMembers.Select(hm => hm.HouseholdId).ToList();

            if (!householdIds.Any())
            {
                return ApiResponse<PagedResult<MedUsageDto>>.Success(new PagedResult<MedUsageDto>
                {
                    Items = new List<MedUsageDto>(),
                    TotalCount = 0,
                    PageIndex = queryParams.PageIndex,
                    PageSize = queryParams.PageSize
                });
            }

            // 获取这些家庭的所有药品ID
            var allMedicines = await _unitOfWork.Medicines
                .FindAsync(m => householdIds.Contains(m.HouseholdId));
            var medicineIds = allMedicines.Select(m => m.Id).ToList();

            if (!medicineIds.Any())
            {
                return ApiResponse<PagedResult<MedUsageDto>>.Success(new PagedResult<MedUsageDto>
                {
                    Items = new List<MedUsageDto>(),
                    TotalCount = 0,
                    PageIndex = queryParams.PageIndex,
                    PageSize = queryParams.PageSize
                });
            }

            // 如果指定了药品ID，检查权限
            if (queryParams.MedicineId.HasValue && !medicineIds.Contains(queryParams.MedicineId.Value))
            {
                return ApiResponse<PagedResult<MedUsageDto>>.Error("无权限访问此用药记录", 403);
            }

            var medicineIdFilter = queryParams.MedicineId;
            var userIdFilter = queryParams.UserId;
            var startDateFilter = queryParams.StartDate;
            var endDateFilter = queryParams.EndDate;
            var keyword = queryParams.SearchKeyword?.ToLower();

            var (items, totalCount) = await _unitOfWork.MedUsages.GetPagedAsync(
                queryParams.PageIndex,
                queryParams.PageSize,
                mu => medicineIds.Contains(mu.MedicineId) &&
                      (!medicineIdFilter.HasValue || mu.MedicineId == medicineIdFilter.Value) &&
                      (!userIdFilter.HasValue || mu.UserId == userIdFilter.Value) &&
                      (!startDateFilter.HasValue || mu.UsedAt >= startDateFilter.Value) &&
                      (!endDateFilter.HasValue || mu.UsedAt <= endDateFilter.Value) &&
                      (string.IsNullOrEmpty(keyword) ||
                         mu.UsedBy.ToLower().Contains(keyword) ||
                         (mu.SymptomNote != null && mu.SymptomNote.ToLower().Contains(keyword))),
                string.IsNullOrEmpty(queryParams.SortBy) ? "UsedAt" : queryParams.SortBy,
                queryParams.SortDescending);

            var usageDtos = new List<MedUsageDto>();
            foreach (var item in items)
            {
                var medicine = await _unitOfWork.Medicines.GetByIdAsync(item.MedicineId);
                var user = await _unitOfWork.Users.GetByIdAsync(item.UserId);
                var dto = item.Adapt<MedUsageDto>();
                dto.MedicineName = medicine?.Name ?? string.Empty;
                dto.Username = user?.Username ?? string.Empty;
                usageDtos.Add(dto);
            }

            var result = new PagedResult<MedUsageDto>
            {
                Items = usageDtos,
                TotalCount = totalCount,
                PageIndex = queryParams.PageIndex,
                PageSize = queryParams.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize),
                HasPreviousPage = queryParams.PageIndex > 1,
                HasNextPage = queryParams.PageIndex * queryParams.PageSize < totalCount
            };

            return ApiResponse<PagedResult<MedUsageDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用药记录列表失败");
            return ApiResponse<PagedResult<MedUsageDto>>.Error("获取用药记录失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedUsageDto>> GetMedUsageByIdAsync(int id, int userId)
    {
        try
        {
            var usage = await _unitOfWork.MedUsages.GetByIdAsync(id);
            if (usage == null)
            {
                return ApiResponse<MedUsageDto>.Error("用药记录不存在", 404);
            }

            // 验证权限
            var medicine = await _unitOfWork.Medicines.GetByIdAsync(usage.MedicineId);
            if (medicine == null)
            {
                return ApiResponse<MedUsageDto>.Error("药品不存在", 404);
            }

            var hasAccess = await HasHouseholdAccessAsync(medicine.HouseholdId, userId);
            if (!hasAccess)
            {
                return ApiResponse<MedUsageDto>.Error("无权限访问此记录", 403);
            }

            var dto = usage.Adapt<MedUsageDto>();
            dto.MedicineName = medicine.Name;

            var user = await _unitOfWork.Users.GetByIdAsync(usage.UserId);
            dto.Username = user?.Username ?? string.Empty;

            return ApiResponse<MedUsageDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用药记录详情失败");
            return ApiResponse<MedUsageDto>.Error("获取用药记录详情失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedUsageDto>> CreateMedUsageAsync(CreateMedUsageRequestDto request, int userId)
    {
        try
        {
            var medicine = await _unitOfWork.Medicines.GetByIdAsync(request.MedicineId);
            if (medicine == null)
            {
                return ApiResponse<MedUsageDto>.Error("药品不存在", 404);
            }

            // 验证权限
            var hasAccess = await HasHouseholdAccessAsync(medicine.HouseholdId, userId);
            if (!hasAccess)
            {
                return ApiResponse<MedUsageDto>.Error("无权限添加用药记录", 403);
            }

            // 检查库存
            if (medicine.StockQuantity < request.UsedQuantity)
            {
                return ApiResponse<MedUsageDto>.Error("库存不足");
            }

            var usage = new MedUsage
            {
                MedicineId = request.MedicineId,
                UserId = userId,
                UsedBy = request.UsedBy,
                UsedQuantity = request.UsedQuantity,
                UsedAt = request.UsedAt ?? DateTime.Now,
                SymptomNote = request.SymptomNote
            };

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 添加用药记录
                await _unitOfWork.MedUsages.AddAsync(usage);

                // 扣减库存
                medicine.StockQuantity -= request.UsedQuantity;

                // 更新药品状态
                if (medicine.StockQuantity <= 0)
                {
                    medicine.Status = Domain.Enums.MedicineStatus.Empty;
                }

                await _unitOfWork.Medicines.UpdateAsync(medicine);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            _logger.LogInformation($"用药记录创建成功: ID={usage.Id}");

            var dto = usage.Adapt<MedUsageDto>();
            dto.MedicineName = medicine.Name;

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            dto.Username = user?.Username ?? string.Empty;

            return ApiResponse<MedUsageDto>.Success(dto, "创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用药记录失败");
            return ApiResponse<MedUsageDto>.Error("创建用药记录失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedUsageDto>> UpdateMedUsageAsync(int id, UpdateMedUsageRequestDto request, int userId)
    {
        try
        {
            var usage = await _unitOfWork.MedUsages.GetByIdAsync(id);
            if (usage == null)
            {
                return ApiResponse<MedUsageDto>.Error("用药记录不存在", 404);
            }

            var medicine = await _unitOfWork.Medicines.GetByIdAsync(usage.MedicineId);
            if (medicine == null)
            {
                return ApiResponse<MedUsageDto>.Error("药品不存在", 404);
            }

            // 验证权限（仅创建者或家庭管理员可修改）
            var hasPermission = await HasPermissionAsync(medicine.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission && usage.UserId != userId)
            {
                return ApiResponse<MedUsageDto>.Error("无权限修改此记录", 403);
            }

            if (!string.IsNullOrEmpty(request.UsedBy))
                usage.UsedBy = request.UsedBy;
            if (request.UsedAt.HasValue)
                usage.UsedAt = request.UsedAt.Value;
            if (request.SymptomNote != null)
                usage.SymptomNote = request.SymptomNote;

            // 处理数量变更
            if (request.UsedQuantity.HasValue && request.UsedQuantity.Value != usage.UsedQuantity)
            {
                var quantityDiff = request.UsedQuantity.Value - usage.UsedQuantity;

                // 检查库存
                if (quantityDiff > 0 && medicine.StockQuantity < quantityDiff)
                {
                    return ApiResponse<MedUsageDto>.Error("库存不足");
                }

                medicine.StockQuantity -= quantityDiff;
                usage.UsedQuantity = request.UsedQuantity.Value;

                if (medicine.StockQuantity <= 0)
                {
                    medicine.Status = Domain.Enums.MedicineStatus.Empty;
                }

                await _unitOfWork.Medicines.UpdateAsync(medicine);
            }

            await _unitOfWork.MedUsages.UpdateAsync(usage);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"用药记录更新成功: ID={id}");

            var dto = usage.Adapt<MedUsageDto>();
            dto.MedicineName = medicine.Name;

            var user = await _unitOfWork.Users.GetByIdAsync(usage.UserId);
            dto.Username = user?.Username ?? string.Empty;

            return ApiResponse<MedUsageDto>.Success(dto, "更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用药记录失败");
            return ApiResponse<MedUsageDto>.Error("更新用药记录失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse> DeleteMedUsageAsync(int id, int userId)
    {
        try
        {
            var usage = await _unitOfWork.MedUsages.GetByIdAsync(id);
            if (usage == null)
            {
                return ApiResponse.Error("用药记录不存在", 404);
            }

            var medicine = await _unitOfWork.Medicines.GetByIdAsync(usage.MedicineId);
            if (medicine == null)
            {
                return ApiResponse.Error("药品不存在", 404);
            }

            // 验证权限
            var hasPermission = await HasPermissionAsync(medicine.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission && usage.UserId != userId)
            {
                return ApiResponse.Error("无权限删除此记录", 403);
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 归还库存
                medicine.StockQuantity += usage.UsedQuantity;

                // 重新计算状态
                if (medicine.StockQuantity > 0 && medicine.Status == Domain.Enums.MedicineStatus.Empty)
                {
                    var daysUntilExpiry = (medicine.ExpiryDate - DateTime.Now).TotalDays;
                    if (daysUntilExpiry <= 0)
                        medicine.Status = Domain.Enums.MedicineStatus.Expired;
                    else if (daysUntilExpiry <= 30)
                        medicine.Status = Domain.Enums.MedicineStatus.NearExpiry;
                    else
                        medicine.Status = Domain.Enums.MedicineStatus.Valid;
                }

                await _unitOfWork.Medicines.UpdateAsync(medicine);
                await _unitOfWork.MedUsages.DeleteAsync(usage);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            _logger.LogInformation($"用药记录删除成功: ID={id}");

            return ApiResponse.Success("删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除用药记录失败");
            return ApiResponse.Error("删除用药记录失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<PagedResult<MedUsageDto>>> GetMyMedUsagesAsync(MedUsageQueryParamsDto queryParams, int userId)
    {
        queryParams.UserId = userId;
        return await GetMedUsagesAsync(queryParams, userId);
    }

    private async Task<bool> HasHouseholdAccessAsync(int householdId, int userId)
    {
        var members = await _unitOfWork.HouseholdMembers
            .FindAsync(hm => hm.HouseholdId == householdId && hm.UserId == userId);
        return members.Any();
    }

    private async Task<bool> HasPermissionAsync(int householdId, int userId, params string[] allowedRoles)
    {
        var members = await _unitOfWork.HouseholdMembers
            .FindAsync(hm => hm.HouseholdId == householdId && hm.UserId == userId);
        var member = members.FirstOrDefault();

        if (member == null)
        {
            return false;
        }

        return allowedRoles.Contains(member.Role);
    }
}
