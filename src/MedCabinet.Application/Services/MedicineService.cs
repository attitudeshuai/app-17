using Mapster;
using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.Medicine;
using MedCabinet.Application.Interfaces;
using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Enums;
using MedCabinet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedCabinet.Application.Services;

public class MedicineService : IMedicineService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MedicineService> _logger;
    private readonly IHealthProfileService _healthProfileService;

    public MedicineService(IUnitOfWork unitOfWork, ILogger<MedicineService> logger, IHealthProfileService healthProfileService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _healthProfileService = healthProfileService;
    }

    public async Task<ApiResponse<PagedResult<MedicineDto>>> GetMedicinesAsync(MedicineQueryParamsDto queryParams, int userId)
    {
        try
        {
            // 获取用户有权限访问的家庭
            var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
            var householdIds = userMembers.Select(hm => hm.HouseholdId).ToList();

            if (!householdIds.Any())
            {
                var emptyResult = new PagedResult<MedicineDto>
                {
                    Items = new List<MedicineDto>(),
                    TotalCount = 0,
                    PageIndex = queryParams.PageIndex,
                    PageSize = queryParams.PageSize,
                    TotalPages = 0,
                    HasPreviousPage = false,
                    HasNextPage = false
                };
                return ApiResponse<PagedResult<MedicineDto>>.Success(emptyResult);
            }

            // 验证家庭ID权限
            if (queryParams.HouseholdId.HasValue && !householdIds.Contains(queryParams.HouseholdId.Value))
            {
                return ApiResponse<PagedResult<MedicineDto>>.Error("无权限访问此家庭药品", 403);
            }

            var householdIdFilter = queryParams.HouseholdId;
            var statusFilter = queryParams.Status;
            var categoryFilter = queryParams.Category;
            var keyword = queryParams.SearchKeyword?.ToLower();

            var (items, totalCount) = await _unitOfWork.Medicines.GetPagedAsync(
                queryParams.PageIndex,
                queryParams.PageSize,
                m => householdIds.Contains(m.HouseholdId) &&
                     (!householdIdFilter.HasValue || m.HouseholdId == householdIdFilter.Value) &&
                     (!statusFilter.HasValue || m.Status == statusFilter.Value) &&
                     (string.IsNullOrEmpty(categoryFilter) || m.Category == categoryFilter) &&
                     (string.IsNullOrEmpty(keyword) ||
                         m.Name.ToLower().Contains(keyword) ||
                         m.Indication.ToLower().Contains(keyword) ||
                         m.Category.ToLower().Contains(keyword) ||
                         m.StorageLocation.ToLower().Contains(keyword)),
                queryParams.SortBy ?? "CreatedAt",
                queryParams.SortDescending);

            var medicineDtos = items.Adapt<List<MedicineDto>>();

            var result = new PagedResult<MedicineDto>
            {
                Items = medicineDtos,
                TotalCount = totalCount,
                PageIndex = queryParams.PageIndex,
                PageSize = queryParams.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize),
                HasPreviousPage = queryParams.PageIndex > 1,
                HasNextPage = queryParams.PageIndex * queryParams.PageSize < totalCount
            };

            return ApiResponse<PagedResult<MedicineDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取药品列表失败");
            return ApiResponse<PagedResult<MedicineDto>>.Error("获取药品列表失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedicineDto>> GetMedicineByIdAsync(int id, int userId)
    {
        try
        {
            var medicine = await _unitOfWork.Medicines.GetByIdAsync(id);
            if (medicine == null)
            {
                return ApiResponse<MedicineDto>.Error("药品不存在", 404);
            }

            // 验证权限
            var hasAccess = await HasHouseholdAccessAsync(medicine.HouseholdId, userId);
            if (!hasAccess)
            {
                return ApiResponse<MedicineDto>.Error("无权限访问此药品", 403);
            }

            var dto = medicine.Adapt<MedicineDto>();

            var checkResult = await _healthProfileService.CheckMedicineContraindicationsAsync(userId, id, userId);
            if (checkResult.Code == 200 && checkResult.Data != null)
            {
                dto.PersonalContraindicationWarnings = checkResult.Data.Warnings;
            }

            return ApiResponse<MedicineDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取药品详情失败");
            return ApiResponse<MedicineDto>.Error("获取药品详情失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedicineDto>> CreateMedicineAsync(CreateMedicineRequestDto request, int userId)
    {
        try
        {
            // 验证权限
            var hasPermission = await HasPermissionAsync(request.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<MedicineDto>.Error("无权限添加药品", 403);
            }

            // 计算药品状态
            var status = CalculateMedicineStatus(request.ExpiryDate, request.StockQuantity);

            var medicine = new Medicine
            {
                HouseholdId = request.HouseholdId,
                Name = request.Name,
                Category = request.Category,
                Indication = request.Indication,
                Dosage = request.Dosage,
                ExpiryDate = request.ExpiryDate,
                StockQuantity = request.StockQuantity,
                StorageLocation = request.StorageLocation,
                Contraindications = request.Contraindications,
                PhotoUrl = request.PhotoUrl,
                Status = status,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.Medicines.AddAsync(medicine);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"药品创建成功: {medicine.Name} (ID: {medicine.Id})");

            // 检查是否需要生成提醒
            await CheckAndCreateAlertsAsync(medicine);

            var dto = medicine.Adapt<MedicineDto>();
            return ApiResponse<MedicineDto>.Success(dto, "创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建药品失败");
            return ApiResponse<MedicineDto>.Error("创建药品失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedicineDto>> UpdateMedicineAsync(int id, UpdateMedicineRequestDto request, int userId)
    {
        try
        {
            var medicine = await _unitOfWork.Medicines.GetByIdAsync(id);
            if (medicine == null)
            {
                return ApiResponse<MedicineDto>.Error("药品不存在", 404);
            }

            // 验证权限
            var hasPermission = await HasPermissionAsync(medicine.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<MedicineDto>.Error("无权限修改药品", 403);
            }

            if (!string.IsNullOrEmpty(request.Name))
                medicine.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Category))
                medicine.Category = request.Category;
            if (request.Indication != null)
                medicine.Indication = request.Indication;
            if (request.Dosage != null)
                medicine.Dosage = request.Dosage;
            if (request.ExpiryDate.HasValue)
                medicine.ExpiryDate = request.ExpiryDate.Value;
            if (request.StockQuantity.HasValue)
                medicine.StockQuantity = request.StockQuantity.Value;
            if (request.StorageLocation != null)
                medicine.StorageLocation = request.StorageLocation;
            if (request.Contraindications != null)
                medicine.Contraindications = request.Contraindications;
            if (request.PhotoUrl != null)
                medicine.PhotoUrl = request.PhotoUrl;

            // 重新计算状态
            medicine.Status = CalculateMedicineStatus(medicine.ExpiryDate, medicine.StockQuantity);

            await _unitOfWork.Medicines.UpdateAsync(medicine);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"药品更新成功: {medicine.Name} (ID: {medicine.Id})");

            // 更新提醒
            await CheckAndCreateAlertsAsync(medicine);

            var dto = medicine.Adapt<MedicineDto>();
            return ApiResponse<MedicineDto>.Success(dto, "更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新药品失败");
            return ApiResponse<MedicineDto>.Error("更新药品失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse> DeleteMedicineAsync(int id, int userId)
    {
        try
        {
            var medicine = await _unitOfWork.Medicines.GetByIdAsync(id);
            if (medicine == null)
            {
                return ApiResponse.Error("药品不存在", 404);
            }

            // 验证权限
            var hasPermission = await HasPermissionAsync(medicine.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse.Error("无权限删除药品", 403);
            }

            await _unitOfWork.Medicines.DeleteAsync(medicine);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"药品删除成功: {medicine.Name} (ID: {medicine.Id})");

            return ApiResponse.Success("删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除药品失败");
            return ApiResponse.Error("删除药品失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedicineDto>> UpdateMedicineStatusAsync(int id, MedicineStatus status, int userId)
    {
        try
        {
            var medicine = await _unitOfWork.Medicines.GetByIdAsync(id);
            if (medicine == null)
            {
                return ApiResponse<MedicineDto>.Error("药品不存在", 404);
            }

            // 验证权限
            var hasPermission = await HasPermissionAsync(medicine.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<MedicineDto>.Error("无权限修改药品状态", 403);
            }

            medicine.Status = status;

            await _unitOfWork.Medicines.UpdateAsync(medicine);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"药品状态更新成功: ID={id}, Status={status}");

            var dto = medicine.Adapt<MedicineDto>();
            return ApiResponse<MedicineDto>.Success(dto, "状态更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新药品状态失败");
            return ApiResponse<MedicineDto>.Error("更新药品状态失败: " + ex.Message, 500);
        }
    }

    private MedicineStatus CalculateMedicineStatus(DateTime expiryDate, int stockQuantity)
    {
        if (stockQuantity <= 0)
        {
            return MedicineStatus.Empty;
        }

        var daysUntilExpiry = (expiryDate - DateTime.Now).TotalDays;

        if (daysUntilExpiry <= 0)
        {
            return MedicineStatus.Expired;
        }
        else if (daysUntilExpiry <= 30)
        {
            return MedicineStatus.NearExpiry;
        }
        else
        {
            return MedicineStatus.Valid;
        }
    }

    private async Task CheckAndCreateAlertsAsync(Medicine medicine)
    {
        try
        {
            // 获取家庭所有成员
            var members = await _unitOfWork.HouseholdMembers
                .FindAsync(hm => hm.HouseholdId == medicine.HouseholdId);

            var daysUntilExpiry = (medicine.ExpiryDate - DateTime.Now).TotalDays;

            foreach (var member in members)
            {
                // 过期提醒
                if (medicine.Status == MedicineStatus.Expired)
                {
                    var existingAlert = await _unitOfWork.MedAlerts
                        .ExistsAsync(a => a.MedicineId == medicine.Id &&
                                         a.UserId == member.UserId &&
                                         a.AlertType == AlertType.Expired &&
                                         !a.IsRead);

                    if (!existingAlert)
                    {
                        var alert = new MedAlert
                        {
                            MedicineId = medicine.Id,
                            UserId = member.UserId,
                            AlertType = AlertType.Expired,
                            Message = $"{medicine.Name}已过期，请及时处理。",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        };
                        await _unitOfWork.MedAlerts.AddAsync(alert);
                    }
                }

                // 临期提醒（30天内）
                if (medicine.Status == MedicineStatus.NearExpiry)
                {
                    var existingAlert = await _unitOfWork.MedAlerts
                        .ExistsAsync(a => a.MedicineId == medicine.Id &&
                                         a.UserId == member.UserId &&
                                         a.AlertType == AlertType.NearExpiry &&
                                         !a.IsRead);

                    if (!existingAlert)
                    {
                        var alert = new MedAlert
                        {
                            MedicineId = medicine.Id,
                            UserId = member.UserId,
                            AlertType = AlertType.NearExpiry,
                            Message = $"{medicine.Name}将在{Math.Ceiling(daysUntilExpiry)}天后过期，请及时处理。",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        };
                        await _unitOfWork.MedAlerts.AddAsync(alert);
                    }
                }

                // 库存不足提醒
                if (medicine.StockQuantity <= 5 && medicine.StockQuantity > 0)
                {
                    var existingAlert = await _unitOfWork.MedAlerts
                        .ExistsAsync(a => a.MedicineId == medicine.Id &&
                                         a.UserId == member.UserId &&
                                         a.AlertType == AlertType.LowStock &&
                                         !a.IsRead);

                    if (!existingAlert)
                    {
                        var alert = new MedAlert
                        {
                            MedicineId = medicine.Id,
                            UserId = member.UserId,
                            AlertType = AlertType.LowStock,
                            Message = $"{medicine.Name}库存不足，仅剩{medicine.StockQuantity}份。",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        };
                        await _unitOfWork.MedAlerts.AddAsync(alert);
                    }
                }

                // 库存为空提醒
                if (medicine.StockQuantity <= 0)
                {
                    var existingAlert = await _unitOfWork.MedAlerts
                        .ExistsAsync(a => a.MedicineId == medicine.Id &&
                                         a.UserId == member.UserId &&
                                         a.AlertType == AlertType.EmptyStock &&
                                         !a.IsRead);

                    if (!existingAlert)
                    {
                        var alert = new MedAlert
                        {
                            MedicineId = medicine.Id,
                            UserId = member.UserId,
                            AlertType = AlertType.EmptyStock,
                            Message = $"{medicine.Name}库存已空，请及时补充。",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        };
                        await _unitOfWork.MedAlerts.AddAsync(alert);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成药品提醒失败");
        }
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
