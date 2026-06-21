using Mapster;
using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.HouseholdMember;
using MedCabinet.Application.Interfaces;
using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace MedCabinet.Application.Services;

public class HouseholdMemberService : IHouseholdMemberService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HouseholdMemberService> _logger;

    public HouseholdMemberService(IUnitOfWork unitOfWork, ILogger<HouseholdMemberService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<HouseholdMemberDto>>> GetHouseholdMembersAsync(HouseholdMemberQueryParamsDto queryParams, int userId)
    {
        try
        {
            // 验证用户对家庭的访问权限
            if (queryParams.HouseholdId.HasValue)
            {
                var hasAccess = await HasHouseholdAccessAsync(queryParams.HouseholdId.Value, userId);
                if (!hasAccess)
                {
                    return ApiResponse<PagedResult<HouseholdMemberDto>>.Error("无权限访问此家庭成员信息", 403);
                }
            }

            // 构建查询
            Expression<Func<HouseholdMember, bool>> filter = hm => true;

            if (queryParams.HouseholdId.HasValue)
            {
                var householdId = queryParams.HouseholdId.Value;
                filter = hm => hm.HouseholdId == householdId;
            }
            else
            {
                // 如果没有指定家庭，只返回用户所在家庭的成员
                var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
                var householdIds = userMembers.Select(hm => hm.HouseholdId).ToList();
                if (householdIds.Any())
                {
                    filter = hm => householdIds.Contains(hm.HouseholdId);
                }
                else
                {
                    filter = hm => false;
                }
            }

            if (!string.IsNullOrEmpty(queryParams.SearchKeyword))
            {
                var keyword = queryParams.SearchKeyword.ToLower();
                // 这里简化处理，实际可以关联用户表搜索
            }

            var (items, totalCount) = await _unitOfWork.HouseholdMembers.GetPagedAsync(
                queryParams.PageIndex,
                queryParams.PageSize,
                filter,
                queryParams.SortBy,
                queryParams.SortDescending);

            var memberDtos = new List<HouseholdMemberDto>();
            foreach (var item in items)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(item.UserId);
                var dto = item.Adapt<HouseholdMemberDto>();
                if (user != null)
                {
                    dto.Username = user.Username;
                    dto.Email = user.Email;
                    dto.Avatar = user.Avatar;
                }
                memberDtos.Add(dto);
            }

            var result = new PagedResult<HouseholdMemberDto>
            {
                Items = memberDtos,
                TotalCount = totalCount,
                PageIndex = queryParams.PageIndex,
                PageSize = queryParams.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize),
                HasPreviousPage = queryParams.PageIndex > 1,
                HasNextPage = queryParams.PageIndex * queryParams.PageSize < totalCount
            };

            return ApiResponse<PagedResult<HouseholdMemberDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取家庭成员列表失败");
            return ApiResponse<PagedResult<HouseholdMemberDto>>.Error("获取家庭成员列表失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<HouseholdMemberDto>> GetHouseholdMemberByIdAsync(int id, int userId)
    {
        try
        {
            var member = await _unitOfWork.HouseholdMembers.GetByIdAsync(id);
            if (member == null)
            {
                return ApiResponse<HouseholdMemberDto>.Error("家庭成员不存在", 404);
            }

            // 验证权限
            var hasAccess = await HasHouseholdAccessAsync(member.HouseholdId, userId);
            if (!hasAccess)
            {
                return ApiResponse<HouseholdMemberDto>.Error("无权限访问此信息", 403);
            }

            var user = await _unitOfWork.Users.GetByIdAsync(member.UserId);
            var dto = member.Adapt<HouseholdMemberDto>();
            if (user != null)
            {
                dto.Username = user.Username;
                dto.Email = user.Email;
                dto.Avatar = user.Avatar;
            }

            return ApiResponse<HouseholdMemberDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取家庭成员详情失败");
            return ApiResponse<HouseholdMemberDto>.Error("获取家庭成员详情失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<HouseholdMemberDto>> AddMemberAsync(CreateHouseholdMemberRequestDto request, int userId)
    {
        try
        {
            // 验证权限
            var hasPermission = await HasPermissionAsync(request.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<HouseholdMemberDto>.Error("无权限添加成员", 403);
            }

            // 检查用户是否已在家庭中
            var exists = await _unitOfWork.HouseholdMembers
                .ExistsAsync(hm => hm.HouseholdId == request.HouseholdId && hm.UserId == request.UserId);
            if (exists)
            {
                return ApiResponse<HouseholdMemberDto>.Error("该用户已是家庭成员");
            }

            var member = new HouseholdMember
            {
                HouseholdId = request.HouseholdId,
                UserId = request.UserId,
                Role = request.Role,
                JoinedAt = DateTime.Now
            };

            await _unitOfWork.HouseholdMembers.AddAsync(member);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"添加成员成功: 家庭ID={request.HouseholdId}, 用户ID={request.UserId}");

            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
            var dto = member.Adapt<HouseholdMemberDto>();
            if (user != null)
            {
                dto.Username = user.Username;
                dto.Email = user.Email;
                dto.Avatar = user.Avatar;
            }

            return ApiResponse<HouseholdMemberDto>.Success(dto, "添加成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加家庭成员失败");
            return ApiResponse<HouseholdMemberDto>.Error("添加成员失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<HouseholdMemberDto>> UpdateMemberAsync(int id, UpdateHouseholdMemberRequestDto request, int userId)
    {
        try
        {
            var member = await _unitOfWork.HouseholdMembers.GetByIdAsync(id);
            if (member == null)
            {
                return ApiResponse<HouseholdMemberDto>.Error("家庭成员不存在", 404);
            }

            // 验证权限
            var hasPermission = await HasPermissionAsync(member.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<HouseholdMemberDto>.Error("无权限修改成员信息", 403);
            }

            if (!string.IsNullOrEmpty(request.Role))
            {
                member.Role = request.Role;
            }

            await _unitOfWork.HouseholdMembers.UpdateAsync(member);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"更新成员成功: ID={id}");

            var user = await _unitOfWork.Users.GetByIdAsync(member.UserId);
            var dto = member.Adapt<HouseholdMemberDto>();
            if (user != null)
            {
                dto.Username = user.Username;
                dto.Email = user.Email;
                dto.Avatar = user.Avatar;
            }

            return ApiResponse<HouseholdMemberDto>.Success(dto, "更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新家庭成员失败");
            return ApiResponse<HouseholdMemberDto>.Error("更新成员失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse> RemoveMemberAsync(int id, int userId)
    {
        try
        {
            var member = await _unitOfWork.HouseholdMembers.GetByIdAsync(id);
            if (member == null)
            {
                return ApiResponse.Error("家庭成员不存在", 404);
            }

            // 验证权限（Owner 可以删除任何人，Admin 可以删除 Member，不能删除自己或其他 Admin/Owner）
            var hasPermission = await HasPermissionAsync(member.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse.Error("无权限删除成员", 403);
            }

            // 不能删除所有者
            if (member.Role == "Owner")
            {
                return ApiResponse.Error("不能删除家庭所有者");
            }

            var currentUserMembers = await _unitOfWork.HouseholdMembers
                .FindAsync(hm => hm.HouseholdId == member.HouseholdId && hm.UserId == userId);
            var currentMember = currentUserMembers.FirstOrDefault();

            if (currentMember?.Role == "Admin" && member.Role == "Admin")
            {
                return ApiResponse.Error("管理员不能删除其他管理员", 403);
            }

            await _unitOfWork.HouseholdMembers.DeleteAsync(member);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"删除成员成功: ID={id}");

            return ApiResponse.Success("删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除家庭成员失败");
            return ApiResponse.Error("删除成员失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<HouseholdMemberDto>> JoinHouseholdByInviteCodeAsync(JoinHouseholdRequestDto request, int userId)
    {
        try
        {
            var households = await _unitOfWork.Households
                .FindAsync(h => h.InviteCode == request.InviteCode);
            var household = households.FirstOrDefault();

            if (household == null)
            {
                return ApiResponse<HouseholdMemberDto>.Error("邀请码无效");
            }

            // 检查用户是否已在家庭中
            var exists = await _unitOfWork.HouseholdMembers
                .ExistsAsync(hm => hm.HouseholdId == household.Id && hm.UserId == userId);
            if (exists)
            {
                return ApiResponse<HouseholdMemberDto>.Error("您已在此家庭中");
            }

            var member = new HouseholdMember
            {
                HouseholdId = household.Id,
                UserId = userId,
                Role = "Member",
                JoinedAt = DateTime.Now
            };

            await _unitOfWork.HouseholdMembers.AddAsync(member);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"用户加入家庭成功: 用户ID={userId}, 家庭ID={household.Id}");

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            var dto = member.Adapt<HouseholdMemberDto>();
            if (user != null)
            {
                dto.Username = user.Username;
                dto.Email = user.Email;
                dto.Avatar = user.Avatar;
            }

            return ApiResponse<HouseholdMemberDto>.Success(dto, "加入家庭成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加入家庭失败");
            return ApiResponse<HouseholdMemberDto>.Error("加入家庭失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<PagedResult<HouseholdMemberDto>>> GetMyMembersAsync(HouseholdMemberQueryParamsDto queryParams, int userId)
    {
        queryParams.HouseholdId = null; // 确保使用用户的家庭
        return await GetHouseholdMembersAsync(queryParams, userId);
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
