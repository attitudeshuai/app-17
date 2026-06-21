using Mapster;
using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.MedAlert;
using MedCabinet.Application.Interfaces;
using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Enums;
using MedCabinet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedCabinet.Application.Services;

public class MedAlertService : IMedAlertService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MedAlertService> _logger;

    public MedAlertService(IUnitOfWork unitOfWork, ILogger<MedAlertService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<MedAlertDto>>> GetMedAlertsAsync(MedAlertQueryParamsDto queryParams, int userId)
    {
        try
        {
            // 获取用户有权限的家庭
            var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
            var householdIds = userMembers.Select(hm => hm.HouseholdId).ToList();

            if (!householdIds.Any())
            {
                return ApiResponse<PagedResult<MedAlertDto>>.Success(new PagedResult<MedAlertDto>
                {
                    Items = new List<MedAlertDto>(),
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
                return ApiResponse<PagedResult<MedAlertDto>>.Success(new PagedResult<MedAlertDto>
                {
                    Items = new List<MedAlertDto>(),
                    TotalCount = 0,
                    PageIndex = queryParams.PageIndex,
                    PageSize = queryParams.PageSize
                });
            }

            var userIdFilter = queryParams.UserId;
            var alertTypeFilter = queryParams.AlertType;
            var isReadFilter = queryParams.IsRead;
            var keyword = queryParams.SearchKeyword?.ToLower();

            var (items, totalCount) = await _unitOfWork.MedAlerts.GetPagedAsync(
                queryParams.PageIndex,
                queryParams.PageSize,
                a => medicineIds.Contains(a.MedicineId) &&
                     (!userIdFilter.HasValue || a.UserId == userIdFilter.Value) &&
                     (!alertTypeFilter.HasValue || a.AlertType == alertTypeFilter.Value) &&
                     (!isReadFilter.HasValue || a.IsRead == isReadFilter.Value) &&
                     (string.IsNullOrEmpty(keyword) || a.Message.ToLower().Contains(keyword)),
                string.IsNullOrEmpty(queryParams.SortBy) ? "CreatedAt" : queryParams.SortBy,
                queryParams.SortDescending);

            var alertDtos = new List<MedAlertDto>();
            foreach (var item in items)
            {
                var medicine = await _unitOfWork.Medicines.GetByIdAsync(item.MedicineId);
                var user = await _unitOfWork.Users.GetByIdAsync(item.UserId);
                var dto = item.Adapt<MedAlertDto>();
                dto.MedicineName = medicine?.Name ?? string.Empty;
                dto.Username = user?.Username ?? string.Empty;
                alertDtos.Add(dto);
            }

            var result = new PagedResult<MedAlertDto>
            {
                Items = alertDtos,
                TotalCount = totalCount,
                PageIndex = queryParams.PageIndex,
                PageSize = queryParams.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize),
                HasPreviousPage = queryParams.PageIndex > 1,
                HasNextPage = queryParams.PageIndex * queryParams.PageSize < totalCount
            };

            return ApiResponse<PagedResult<MedAlertDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取提醒列表失败");
            return ApiResponse<PagedResult<MedAlertDto>>.Error("获取提醒列表失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedAlertDto>> GetMedAlertByIdAsync(int id, int userId)
    {
        try
        {
            var alert = await _unitOfWork.MedAlerts.GetByIdAsync(id);
            if (alert == null)
            {
                return ApiResponse<MedAlertDto>.Error("提醒不存在", 404);
            }

            // 验证权限（只有提醒所属用户或家庭管理员可以查看）
            var medicine = await _unitOfWork.Medicines.GetByIdAsync(alert.MedicineId);
            if (medicine == null)
            {
                return ApiResponse<MedAlertDto>.Error("药品不存在", 404);
            }

            var hasAccess = alert.UserId == userId || await HasPermissionAsync(medicine.HouseholdId, userId, "Owner", "Admin");
            if (!hasAccess)
            {
                return ApiResponse<MedAlertDto>.Error("无权限访问此提醒", 403);
            }

            var dto = alert.Adapt<MedAlertDto>();
            dto.MedicineName = medicine.Name;

            var user = await _unitOfWork.Users.GetByIdAsync(alert.UserId);
            dto.Username = user?.Username ?? string.Empty;

            return ApiResponse<MedAlertDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取提醒详情失败");
            return ApiResponse<MedAlertDto>.Error("获取提醒详情失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedAlertDto>> CreateMedAlertAsync(CreateMedAlertRequestDto request, int userId)
    {
        try
        {
            var medicine = await _unitOfWork.Medicines.GetByIdAsync(request.MedicineId);
            if (medicine == null)
            {
                return ApiResponse<MedAlertDto>.Error("药品不存在", 404);
            }

            // 验证权限
            var hasPermission = await HasPermissionAsync(medicine.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<MedAlertDto>.Error("无权限创建提醒", 403);
            }

            var alert = new MedAlert
            {
                MedicineId = request.MedicineId,
                UserId = userId,
                AlertType = request.AlertType,
                Message = request.Message,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.MedAlerts.AddAsync(alert);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"提醒创建成功: ID={alert.Id}");

            var dto = alert.Adapt<MedAlertDto>();
            dto.MedicineName = medicine.Name;

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            dto.Username = user?.Username ?? string.Empty;

            return ApiResponse<MedAlertDto>.Success(dto, "创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建提醒失败");
            return ApiResponse<MedAlertDto>.Error("创建提醒失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedAlertDto>> UpdateMedAlertAsync(int id, UpdateMedAlertRequestDto request, int userId)
    {
        try
        {
            var alert = await _unitOfWork.MedAlerts.GetByIdAsync(id);
            if (alert == null)
            {
                return ApiResponse<MedAlertDto>.Error("提醒不存在", 404);
            }

            // 验证权限
            var medicine = await _unitOfWork.Medicines.GetByIdAsync(alert.MedicineId);
            if (medicine == null)
            {
                return ApiResponse<MedAlertDto>.Error("药品不存在", 404);
            }

            var hasPermission = alert.UserId == userId || await HasPermissionAsync(medicine.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<MedAlertDto>.Error("无权限修改此提醒", 403);
            }

            if (request.IsRead.HasValue)
                alert.IsRead = request.IsRead.Value;
            if (request.Message != null)
                alert.Message = request.Message;

            await _unitOfWork.MedAlerts.UpdateAsync(alert);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"提醒更新成功: ID={id}");

            var dto = alert.Adapt<MedAlertDto>();
            dto.MedicineName = medicine.Name;

            var user = await _unitOfWork.Users.GetByIdAsync(alert.UserId);
            dto.Username = user?.Username ?? string.Empty;

            return ApiResponse<MedAlertDto>.Success(dto, "更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新提醒失败");
            return ApiResponse<MedAlertDto>.Error("更新提醒失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse> DeleteMedAlertAsync(int id, int userId)
    {
        try
        {
            var alert = await _unitOfWork.MedAlerts.GetByIdAsync(id);
            if (alert == null)
            {
                return ApiResponse.Error("提醒不存在", 404);
            }

            // 验证权限
            var medicine = await _unitOfWork.Medicines.GetByIdAsync(alert.MedicineId);
            if (medicine == null)
            {
                return ApiResponse.Error("药品不存在", 404);
            }

            var hasPermission = alert.UserId == userId || await HasPermissionAsync(medicine.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse.Error("无权限删除此提醒", 403);
            }

            await _unitOfWork.MedAlerts.DeleteAsync(alert);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"提醒删除成功: ID={id}");

            return ApiResponse.Success("删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除提醒失败");
            return ApiResponse.Error("删除提醒失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<PagedResult<MedAlertDto>>> GetMyMedAlertsAsync(MedAlertQueryParamsDto queryParams, int userId)
    {
        queryParams.UserId = userId;
        return await GetMedAlertsAsync(queryParams, userId);
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
