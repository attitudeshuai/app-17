using Mapster;
using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.Household;
using MedCabinet.Application.Interfaces;
using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedCabinet.Application.Services;

public class HouseholdService : IHouseholdService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HouseholdService> _logger;

    public HouseholdService(IUnitOfWork unitOfWork, ILogger<HouseholdService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<HouseholdDto>>> GetHouseholdsAsync(HouseholdQueryParamsDto queryParams, int userId)
    {
        try
        {
            // 用户只能查看自己加入的家庭
            var memberHouseholds = await _unitOfWork.HouseholdMembers
                .FindAsync(hm => hm.UserId == userId);
            var householdIds = memberHouseholds.Select(hm => hm.HouseholdId).ToList();

            var keyword = queryParams.SearchKeyword?.ToLower();

            var (items, totalCount) = await _unitOfWork.Households.GetPagedAsync(
                queryParams.PageIndex,
                queryParams.PageSize,
                h => householdIds.Contains(h.Id) &&
                     (string.IsNullOrEmpty(keyword) ||
                         h.Name.ToLower().Contains(keyword) ||
                         h.InviteCode.ToLower().Contains(keyword)),
                queryParams.SortBy,
                queryParams.SortDescending);

            var householdDtos = items.Adapt<List<HouseholdDto>>();

            // 填充成员数量和药品数量
            foreach (var dto in householdDtos)
            {
                dto.MemberCount = await _unitOfWork.HouseholdMembers
                    .CountAsync(hm => hm.HouseholdId == dto.Id);
                dto.MedicineCount = await _unitOfWork.Medicines
                    .CountAsync(m => m.HouseholdId == dto.Id);
            }

            var result = new PagedResult<HouseholdDto>
            {
                Items = householdDtos,
                TotalCount = totalCount,
                PageIndex = queryParams.PageIndex,
                PageSize = queryParams.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize),
                HasPreviousPage = queryParams.PageIndex > 1,
                HasNextPage = queryParams.PageIndex * queryParams.PageSize < totalCount
            };

            return ApiResponse<PagedResult<HouseholdDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取家庭列表失败");
            return ApiResponse<PagedResult<HouseholdDto>>.Error("获取家庭列表失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<HouseholdDto>> GetHouseholdByIdAsync(int id, int userId)
    {
        try
        {
            if (!await HasPermissionAsync(id, userId, "Owner", "Admin", "Member"))
            {
                return ApiResponse<HouseholdDto>.Error("无权限访问此家庭", 403);
            }

            var household = await _unitOfWork.Households.GetByIdAsync(id);
            if (household == null)
            {
                return ApiResponse<HouseholdDto>.Error("家庭不存在", 404);
            }

            var dto = household.Adapt<HouseholdDto>();
            dto.MemberCount = await _unitOfWork.HouseholdMembers.CountAsync(hm => hm.HouseholdId == id);
            dto.MedicineCount = await _unitOfWork.Medicines.CountAsync(m => m.HouseholdId == id);

            return ApiResponse<HouseholdDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取家庭详情失败");
            return ApiResponse<HouseholdDto>.Error("获取家庭详情失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<HouseholdDto>> CreateHouseholdAsync(CreateHouseholdRequestDto request, int userId)
    {
        try
        {
            // 生成唯一邀请码
            var inviteCode = GenerateInviteCode();
            while (await _unitOfWork.Households.ExistsAsync(h => h.InviteCode == inviteCode))
            {
                inviteCode = GenerateInviteCode();
            }

            var household = new Household
            {
                Name = request.Name,
                InviteCode = inviteCode,
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.Households.AddAsync(household);
                await _unitOfWork.SaveChangesAsync();

                // 创建者自动成为家庭成员（所有者）
                var member = new HouseholdMember
                {
                    HouseholdId = household.Id,
                    UserId = userId,
                    Role = "Owner",
                    JoinedAt = DateTime.Now
                };

                await _unitOfWork.HouseholdMembers.AddAsync(member);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            _logger.LogInformation($"家庭创建成功: {household.Name} (ID: {household.Id})");

            var dto = household.Adapt<HouseholdDto>();
            dto.MemberCount = 1;
            dto.MedicineCount = 0;

            return ApiResponse<HouseholdDto>.Success(dto, "创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建家庭失败");
            return ApiResponse<HouseholdDto>.Error("创建家庭失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<HouseholdDto>> UpdateHouseholdAsync(int id, UpdateHouseholdRequestDto request, int userId)
    {
        try
        {
            if (!await HasPermissionAsync(id, userId, "Owner", "Admin"))
            {
                return ApiResponse<HouseholdDto>.Error("无权限修改此家庭", 403);
            }

            var household = await _unitOfWork.Households.GetByIdAsync(id);
            if (household == null)
            {
                return ApiResponse<HouseholdDto>.Error("家庭不存在", 404);
            }

            if (!string.IsNullOrEmpty(request.Name))
            {
                household.Name = request.Name;
            }

            await _unitOfWork.Households.UpdateAsync(household);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"家庭更新成功: {household.Name} (ID: {household.Id})");

            var dto = household.Adapt<HouseholdDto>();
            dto.MemberCount = await _unitOfWork.HouseholdMembers.CountAsync(hm => hm.HouseholdId == id);
            dto.MedicineCount = await _unitOfWork.Medicines.CountAsync(m => m.HouseholdId == id);

            return ApiResponse<HouseholdDto>.Success(dto, "更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新家庭失败");
            return ApiResponse<HouseholdDto>.Error("更新家庭失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse> DeleteHouseholdAsync(int id, int userId)
    {
        try
        {
            if (!await HasPermissionAsync(id, userId, "Owner"))
            {
                return ApiResponse.Error("无权限删除此家庭", 403);
            }

            var household = await _unitOfWork.Households.GetByIdAsync(id);
            if (household == null)
            {
                return ApiResponse.Error("家庭不存在", 404);
            }

            await _unitOfWork.Households.DeleteAsync(household);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"家庭删除成功: {household.Name} (ID: {household.Id})");

            return ApiResponse.Success("删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除家庭失败");
            return ApiResponse.Error("删除家庭失败: " + ex.Message, 500);
        }
    }

    public async Task<bool> HasPermissionAsync(int householdId, int userId, params string[] allowedRoles)
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

    private string GenerateInviteCode()
    {
        return "FAM" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
    }
}
