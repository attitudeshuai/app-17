using System.Text;
using Mapster;
using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.HealthProfile;
using MedCabinet.Application.DTOs.ProcurementSuggestion;
using MedCabinet.Application.Interfaces;
using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Enums;
using MedCabinet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedCabinet.Application.Services;

public class HealthProfileService : IHealthProfileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HealthProfileService> _logger;

    public HealthProfileService(IUnitOfWork unitOfWork, ILogger<HealthProfileService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<HealthProfileDto>>> GetHealthProfilesAsync(
        HealthProfileQueryParamsDto queryParams, int currentUserId)
    {
        try
        {
            var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == currentUserId);
            var userHouseholdIds = userMembers.Select(hm => hm.HouseholdId).ToList();

            if (!userHouseholdIds.Any())
            {
                return ApiResponse<PagedResult<HealthProfileDto>>.Success(BuildEmptyPagedResult(queryParams));
            }

            if (queryParams.HouseholdId.HasValue && !userHouseholdIds.Contains(queryParams.HouseholdId.Value))
            {
                return ApiResponse<PagedResult<HealthProfileDto>>.Error("无权限访问此家庭健康档案", 403);
            }

            var targetHouseholdIds = queryParams.HouseholdId.HasValue
                ? new List<int> { queryParams.HouseholdId.Value }
                : userHouseholdIds;

            var isAdminInAnyHousehold = userMembers.Any(m =>
                targetHouseholdIds.Contains(m.HouseholdId) && (m.Role == "Owner" || m.Role == "Admin"));

            var householdIdFilter = queryParams.HouseholdId;
            var userIdFilter = queryParams.UserId;
            var keyword = queryParams.SearchKeyword?.ToLower();

            var (items, totalCount) = await _unitOfWork.HealthProfiles.GetPagedAsync(
                queryParams.PageIndex,
                queryParams.PageSize,
                hp => targetHouseholdIds.Contains(hp.HouseholdId) &&
                     (!householdIdFilter.HasValue || hp.HouseholdId == householdIdFilter.Value) &&
                     (!userIdFilter.HasValue || hp.UserId == userIdFilter.Value) &&
                     (string.IsNullOrEmpty(keyword) ||
                         hp.FullName.ToLower().Contains(keyword) ||
                         (hp.Allergies != null && hp.Allergies.ToLower().Contains(keyword)) ||
                         (hp.ChronicDiseases != null && hp.ChronicDiseases.ToLower().Contains(keyword))),
                queryParams.SortBy ?? "CreatedAt",
                queryParams.SortDescending);

            var dtos = new List<HealthProfileDto>();
            foreach (var item in items)
            {
                var canAccess = await CanAccessHealthProfile(item, currentUserId, isAdminInAnyHousehold);
                if (!canAccess)
                    continue;

                var dto = item.Adapt<HealthProfileDto>();
                dto.Username = item.User?.Username ?? string.Empty;
                dto.HouseholdName = item.Household?.Name ?? string.Empty;
                dtos.Add(dto);
            }

            var result = new PagedResult<HealthProfileDto>
            {
                Items = dtos,
                TotalCount = dtos.Count,
                PageIndex = queryParams.PageIndex,
                PageSize = queryParams.PageSize,
                TotalPages = (int)Math.Ceiling((double)dtos.Count / queryParams.PageSize),
                HasPreviousPage = queryParams.PageIndex > 1,
                HasNextPage = queryParams.PageIndex * queryParams.PageSize < dtos.Count
            };

            return ApiResponse<PagedResult<HealthProfileDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取健康档案列表失败");
            return ApiResponse<PagedResult<HealthProfileDto>>.Error("获取健康档案列表失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<HealthProfileDto>> GetHealthProfileByIdAsync(int id, int currentUserId)
    {
        try
        {
            var profile = await _unitOfWork.HealthProfiles.GetByIdAsync(id);
            if (profile == null)
            {
                return ApiResponse<HealthProfileDto>.Error("健康档案不存在", 404);
            }

            var canAccess = await CanAccessHealthProfile(profile, currentUserId);
            if (!canAccess)
            {
                return ApiResponse<HealthProfileDto>.Error("无权查看此健康档案，仅本人和家庭管理员可查看", 403);
            }

            var dto = profile.Adapt<HealthProfileDto>();
            dto.Username = profile.User?.Username ?? string.Empty;
            dto.HouseholdName = profile.Household?.Name ?? string.Empty;
            return ApiResponse<HealthProfileDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取健康档案详情失败");
            return ApiResponse<HealthProfileDto>.Error("获取健康档案详情失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<HealthProfileDto>> GetMyHealthProfileAsync(int householdId, int currentUserId)
    {
        try
        {
            var hasAccess = await HasHouseholdAccessAsync(householdId, currentUserId);
            if (!hasAccess)
            {
                return ApiResponse<HealthProfileDto>.Error("无权访问此家庭", 403);
            }

            var profiles = await _unitOfWork.HealthProfiles
                .FindAsync(hp => hp.HouseholdId == householdId && hp.UserId == currentUserId);
            var profile = profiles.FirstOrDefault();

            if (profile == null)
            {
                return ApiResponse<HealthProfileDto>.Error("您尚未在此家庭建立健康档案", 404);
            }

            var dto = profile.Adapt<HealthProfileDto>();
            dto.Username = profile.User?.Username ?? string.Empty;
            dto.HouseholdName = profile.Household?.Name ?? string.Empty;
            return ApiResponse<HealthProfileDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取我的健康档案失败");
            return ApiResponse<HealthProfileDto>.Error("获取我的健康档案失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<HealthProfileDto>> CreateHealthProfileAsync(
        CreateHealthProfileRequestDto request, int currentUserId)
    {
        try
        {
            var isOwnProfile = request.UserId == currentUserId;
            var hasPermission = isOwnProfile || await HasPermissionAsync(request.HouseholdId, currentUserId, "Owner", "Admin");

            if (!hasPermission)
            {
                return ApiResponse<HealthProfileDto>.Error("无权为他人创建健康档案，仅本人或家庭管理员可操作", 403);
            }

            var hasHouseholdAccess = await HasHouseholdAccessAsync(request.HouseholdId, request.UserId);
            if (!hasHouseholdAccess)
            {
                return ApiResponse<HealthProfileDto>.Error("该用户不是此家庭成员", 400);
            }

            var exists = await _unitOfWork.HealthProfiles
                .ExistsAsync(hp => hp.HouseholdId == request.HouseholdId && hp.UserId == request.UserId);
            if (exists)
            {
                return ApiResponse<HealthProfileDto>.Error("该成员在此家庭已有健康档案");
            }

            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

            var profile = new HealthProfile
            {
                UserId = request.UserId,
                HouseholdId = request.HouseholdId,
                FullName = string.IsNullOrEmpty(request.FullName) ? (user?.Username ?? string.Empty) : request.FullName,
                DateOfBirth = request.DateOfBirth,
                Age = request.Age,
                Gender = request.Gender,
                BloodType = request.BloodType ?? BloodType.Unknown,
                HeightCm = request.HeightCm,
                WeightKg = request.WeightKg,
                Allergies = request.Allergies,
                ChronicDiseases = request.ChronicDiseases,
                Medications = request.Medications,
                MedicalHistory = request.MedicalHistory,
                EmergencyContact = request.EmergencyContact,
                EmergencyPhone = request.EmergencyPhone,
                Notes = request.Notes
            };

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.HealthProfiles.AddAsync(profile);
                await _unitOfWork.SaveChangesAsync();

                var modifier = await _unitOfWork.Users.GetByIdAsync(currentUserId);
                await LogAudit(profile.Id, currentUserId, modifier?.Username ?? string.Empty, "Create", "Profile", null, "新建档案");
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            _logger.LogInformation($"健康档案创建成功: 用户ID={request.UserId}, 家庭ID={request.HouseholdId}");

            var dto = profile.Adapt<HealthProfileDto>();
            dto.Username = user?.Username ?? string.Empty;
            var household = await _unitOfWork.Households.GetByIdAsync(request.HouseholdId);
            dto.HouseholdName = household?.Name ?? string.Empty;

            return ApiResponse<HealthProfileDto>.Success(dto, "健康档案创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建健康档案失败");
            return ApiResponse<HealthProfileDto>.Error("创建健康档案失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<HealthProfileDto>> UpdateHealthProfileAsync(
        int id, UpdateHealthProfileRequestDto request, int currentUserId)
    {
        try
        {
            var profile = await _unitOfWork.HealthProfiles.GetByIdAsync(id);
            if (profile == null)
            {
                return ApiResponse<HealthProfileDto>.Error("健康档案不存在", 404);
            }

            var isOwnProfile = profile.UserId == currentUserId;
            var hasPermission = isOwnProfile || await HasPermissionAsync(profile.HouseholdId, currentUserId, "Owner", "Admin");

            if (!hasPermission)
            {
                return ApiResponse<HealthProfileDto>.Error("无权修改此健康档案，仅本人或家庭管理员可操作", 403);
            }

            var modifier = await _unitOfWork.Users.GetByIdAsync(currentUserId);
            var modifierName = modifier?.Username ?? string.Empty;
            var auditLogs = new List<HealthProfileAuditLog>();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (request.FullName != null && request.FullName != profile.FullName)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.FullName), profile.FullName, request.FullName));
                    profile.FullName = request.FullName;
                }
                if (request.DateOfBirth.HasValue && request.DateOfBirth != profile.DateOfBirth)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.DateOfBirth),
                        profile.DateOfBirth?.ToString("yyyy-MM-dd"), request.DateOfBirth.Value.ToString("yyyy-MM-dd")));
                    profile.DateOfBirth = request.DateOfBirth.Value;
                }
                if (request.Age.HasValue && request.Age != profile.Age)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.Age),
                        profile.Age?.ToString(), request.Age.Value.ToString()));
                    profile.Age = request.Age.Value;
                }
                if (request.Gender != null && request.Gender != profile.Gender)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.Gender), profile.Gender, request.Gender));
                    profile.Gender = request.Gender;
                }
                if (request.BloodType.HasValue && request.BloodType != profile.BloodType)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.BloodType),
                        profile.BloodType.ToString(), request.BloodType.Value.ToString()));
                    profile.BloodType = request.BloodType.Value;
                }
                if (request.HeightCm.HasValue && request.HeightCm != profile.HeightCm)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.HeightCm),
                        profile.HeightCm?.ToString(), request.HeightCm.Value.ToString()));
                    profile.HeightCm = request.HeightCm.Value;
                }
                if (request.WeightKg.HasValue && request.WeightKg != profile.WeightKg)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.WeightKg),
                        profile.WeightKg?.ToString(), request.WeightKg.Value.ToString()));
                    profile.WeightKg = request.WeightKg.Value;
                }
                if (request.Allergies != null && request.Allergies != profile.Allergies)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.Allergies), profile.Allergies, request.Allergies));
                    profile.Allergies = request.Allergies;
                }
                if (request.ChronicDiseases != null && request.ChronicDiseases != profile.ChronicDiseases)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.ChronicDiseases), profile.ChronicDiseases, request.ChronicDiseases));
                    profile.ChronicDiseases = request.ChronicDiseases;
                }
                if (request.Medications != null && request.Medications != profile.Medications)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.Medications), profile.Medications, request.Medications));
                    profile.Medications = request.Medications;
                }
                if (request.MedicalHistory != null && request.MedicalHistory != profile.MedicalHistory)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.MedicalHistory), profile.MedicalHistory, request.MedicalHistory));
                    profile.MedicalHistory = request.MedicalHistory;
                }
                if (request.EmergencyContact != null && request.EmergencyContact != profile.EmergencyContact)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.EmergencyContact), profile.EmergencyContact, request.EmergencyContact));
                    profile.EmergencyContact = request.EmergencyContact;
                }
                if (request.EmergencyPhone != null && request.EmergencyPhone != profile.EmergencyPhone)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.EmergencyPhone), profile.EmergencyPhone, request.EmergencyPhone));
                    profile.EmergencyPhone = request.EmergencyPhone;
                }
                if (request.Notes != null && request.Notes != profile.Notes)
                {
                    auditLogs.Add(CreateAuditLog(id, currentUserId, modifierName, "Update", nameof(profile.Notes), profile.Notes, request.Notes));
                    profile.Notes = request.Notes;
                }

                foreach (var log in auditLogs)
                {
                    await _unitOfWork.HealthProfileAuditLogs.AddAsync(log);
                }

                await _unitOfWork.HealthProfiles.UpdateAsync(profile);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            _logger.LogInformation($"健康档案更新成功: ID={id}, 修改了{auditLogs.Count}个字段");

            var dto = profile.Adapt<HealthProfileDto>();
            dto.Username = profile.User?.Username ?? string.Empty;
            dto.HouseholdName = profile.Household?.Name ?? string.Empty;
            return ApiResponse<HealthProfileDto>.Success(dto, $"健康档案更新成功，共修改{auditLogs.Count}项");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新健康档案失败");
            return ApiResponse<HealthProfileDto>.Error("更新健康档案失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse> DeleteHealthProfileAsync(int id, int currentUserId)
    {
        try
        {
            var profile = await _unitOfWork.HealthProfiles.GetByIdAsync(id);
            if (profile == null)
            {
                return ApiResponse.Error("健康档案不存在", 404);
            }

            var isOwnProfile = profile.UserId == currentUserId;
            var hasPermission = isOwnProfile || await HasPermissionAsync(profile.HouseholdId, currentUserId, "Owner", "Admin");

            if (!hasPermission)
            {
                return ApiResponse.Error("无权删除此健康档案，仅本人或家庭管理员可操作", 403);
            }

            await _unitOfWork.HealthProfiles.DeleteAsync(profile);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"健康档案删除成功: ID={id}");
            return ApiResponse.Success("健康档案删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除健康档案失败");
            return ApiResponse.Error("删除健康档案失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<PagedResult<HealthProfileAuditLogDto>>> GetAuditLogsAsync(
        int healthProfileId, int pageIndex, int pageSize, int currentUserId)
    {
        try
        {
            var profile = await _unitOfWork.HealthProfiles.GetByIdAsync(healthProfileId);
            if (profile == null)
            {
                return ApiResponse<PagedResult<HealthProfileAuditLogDto>>.Error("健康档案不存在", 404);
            }

            var canAccess = await CanAccessHealthProfile(profile, currentUserId);
            if (!canAccess)
            {
                return ApiResponse<PagedResult<HealthProfileAuditLogDto>>.Error("无权查看此档案的修改日志", 403);
            }

            var (items, totalCount) = await _unitOfWork.HealthProfileAuditLogs.GetPagedAsync(
                pageIndex,
                pageSize,
                log => log.HealthProfileId == healthProfileId,
                "ModifiedAt",
                true);

            var dtos = items.Select(log => log.Adapt<HealthProfileAuditLogDto>()).ToList();

            var result = new PagedResult<HealthProfileAuditLogDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasPreviousPage = pageIndex > 1,
                HasNextPage = pageIndex * pageSize < totalCount
            };

            return ApiResponse<PagedResult<HealthProfileAuditLogDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取修改日志失败");
            return ApiResponse<PagedResult<HealthProfileAuditLogDto>>.Error("获取修改日志失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<MedicineContraindicationCheckResultDto>> CheckMedicineContraindicationsAsync(
        int userId, int medicineId, int currentUserId)
    {
        try
        {
            var medicine = await _unitOfWork.Medicines.GetByIdAsync(medicineId);
            if (medicine == null)
            {
                return ApiResponse<MedicineContraindicationCheckResultDto>.Error("药品不存在", 404);
            }

            var profiles = await _unitOfWork.HealthProfiles
                .FindAsync(hp => hp.UserId == userId && hp.HouseholdId == medicine.HouseholdId);
            var profile = profiles.FirstOrDefault();

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            var result = new MedicineContraindicationCheckResultDto
            {
                UserId = userId,
                Username = user?.Username ?? string.Empty,
                MedicineId = medicineId,
                MedicineName = medicine.Name,
                HasWarnings = false,
                Warnings = new List<ContraindicationWarningDto>()
            };

            if (profile == null)
            {
                result.Warnings.Add(new ContraindicationWarningDto
                {
                    Level = "Info",
                    Type = "NoProfile",
                    Message = "该成员尚未建立健康档案，无法自动检查用药禁忌",
                    Detail = "建议先为该成员建立健康档案以启用用药安全检查功能"
                });
                return ApiResponse<MedicineContraindicationCheckResultDto>.Success(result);
            }

            var canAccess = await CanAccessHealthProfile(profile, currentUserId);
            if (!canAccess)
            {
                return ApiResponse<MedicineContraindicationCheckResultDto>.Error("无权查看此成员健康信息", 403);
            }

            if (!string.IsNullOrEmpty(profile.Allergies))
            {
                var allergyList = profile.Allergies.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrEmpty(a))
                    .ToList();

                var medicineText = $"{medicine.Name} {medicine.Category} {medicine.Indication} {medicine.Dosage} {medicine.Contraindications}".ToLower();

                foreach (var allergy in allergyList)
                {
                    if (medicineText.Contains(allergy.ToLower()))
                    {
                        result.HasWarnings = true;
                        result.Warnings.Add(new ContraindicationWarningDto
                        {
                            Level = "Danger",
                            Type = "Allergy",
                            Message = $"过敏警告：该成员对「{allergy}」过敏，而此药品可能含有相关成分",
                            Detail = $"药品信息：{medicine.Name} - {medicine.Category}"
                        });
                    }
                }
            }

            if (!string.IsNullOrEmpty(medicine.Contraindications))
            {
                var contraText = medicine.Contraindications.ToLower();
                var profileText = $"{profile.ChronicDiseases} {profile.MedicalHistory} {profile.Medications}".ToLower();

                if (!string.IsNullOrEmpty(profile.ChronicDiseases))
                {
                    var chronicList = profile.ChronicDiseases.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .ToList();

                    foreach (var chronic in chronicList)
                    {
                        if (contraText.Contains(chronic.ToLower()))
                        {
                            result.HasWarnings = true;
                            result.Warnings.Add(new ContraindicationWarningDto
                            {
                                Level = "Warning",
                                Type = "ChronicDisease",
                                Message = $"慢性病禁忌：该成员患有「{chronic}」，药品说明书中标注了相关禁忌症",
                                Detail = $"药品禁忌症：{medicine.Contraindications}"
                            });
                        }
                    }
                }

                if (!string.IsNullOrEmpty(profile.Medications))
                {
                    var currentMedsList = profile.Medications.Split(new[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(m => m.Trim())
                        .Where(m => !string.IsNullOrEmpty(m))
                        .ToList();

                    foreach (var med in currentMedsList)
                    {
                        if (contraText.Contains(med.ToLower()))
                        {
                            result.HasWarnings = true;
                            result.Warnings.Add(new ContraindicationWarningDto
                            {
                                Level = "Warning",
                                Type = "DrugInteraction",
                                Message = $"药物相互作用：该成员正在使用「{med}」，可能与此药存在相互作用",
                                Detail = $"药品禁忌症：{medicine.Contraindications}"
                            });
                        }
                    }
                }
            }

            if (profile.Age.HasValue)
            {
                if (profile.Age < 12 && !string.IsNullOrEmpty(medicine.Contraindications) &&
                    medicine.Contraindications.ToLower().Contains("儿童"))
                {
                    result.HasWarnings = true;
                    result.Warnings.Add(new ContraindicationWarningDto
                    {
                        Level = "Warning",
                        Type = "Age",
                        Message = "年龄警告：该成员为儿童，药品可能不适合儿童使用",
                        Detail = $"年龄：{profile.Age.Value}岁，请确认儿童用量或遵医嘱"
                    });
                }

                if (profile.Age >= 65 && !string.IsNullOrEmpty(medicine.Contraindications) &&
                    medicine.Contraindications.ToLower().Contains("老年"))
                {
                    result.HasWarnings = true;
                    result.Warnings.Add(new ContraindicationWarningDto
                    {
                        Level = "Warning",
                        Type = "Age",
                        Message = "年龄警告：该成员为老年人，需注意用药剂量调整",
                        Detail = $"年龄：{profile.Age.Value}岁，请遵医嘱调整用量"
                    });
                }
            }

            if (!result.HasWarnings)
            {
                result.Warnings.Add(new ContraindicationWarningDto
                {
                    Level = "Safe",
                    Type = "Checked",
                    Message = "未发现明显用药禁忌，建议仍需遵医嘱使用",
                    Detail = "本系统仅基于关键字匹配进行初步筛查，不能替代专业医疗建议"
                });
            }

            return ApiResponse<MedicineContraindicationCheckResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查用药禁忌失败");
            return ApiResponse<MedicineContraindicationCheckResultDto>.Error("检查用药禁忌失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<HealthReportExportDto>> ExportHealthReportAsync(int id, int currentUserId)
    {
        try
        {
            var profile = await _unitOfWork.HealthProfiles.GetByIdAsync(id);
            if (profile == null)
            {
                return ApiResponse<HealthReportExportDto>.Error("健康档案不存在", 404);
            }

            var canAccess = await CanAccessHealthProfile(profile, currentUserId);
            if (!canAccess)
            {
                return ApiResponse<HealthReportExportDto>.Error("无权导出此健康档案", 403);
            }

            var report = new HealthReportExportDto
            {
                FullName = profile.FullName,
                Gender = profile.Gender ?? "未填写",
                DateOfBirth = profile.DateOfBirth,
                Age = profile.Age,
                BloodType = GetBloodTypeDisplayName(profile.BloodType),
                HeightCm = profile.HeightCm,
                WeightKg = profile.WeightKg,
                Allergies = profile.Allergies ?? "无",
                ChronicDiseases = profile.ChronicDiseases ?? "无",
                CurrentMedications = profile.Medications ?? "无",
                MedicalHistory = profile.MedicalHistory ?? "无特殊病史",
                EmergencyContact = profile.EmergencyContact ?? "未填写",
                EmergencyPhone = profile.EmergencyPhone ?? "未填写",
                Notes = profile.Notes ?? string.Empty,
                ReportGeneratedAt = DateTime.Now
            };

            _logger.LogInformation($"健康报告导出成功: 档案ID={id}");
            return ApiResponse<HealthReportExportDto>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出健康报告失败");
            return ApiResponse<HealthReportExportDto>.Error("导出健康报告失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<CsvExportResult>> ExportHealthReportCsvAsync(int id, int currentUserId)
    {
        try
        {
            var profileResult = await ExportHealthReportAsync(id, currentUserId);
            if (profileResult.Code != 200 || profileResult.Data == null)
            {
                return ApiResponse<CsvExportResult>.Error(profileResult.Message, profileResult.Code);
            }

            var report = profileResult.Data;

            var headers = new[]
            {
                "项目", "内容"
            };

            var rows = new List<string[]>
            {
                new[] { "姓名", EscapeCsv(report.FullName) },
                new[] { "性别", EscapeCsv(report.Gender) },
                new[] { "出生日期", report.DateOfBirth?.ToString("yyyy-MM-dd") ?? "未填写" },
                new[] { "年龄", report.Age?.ToString() ?? "未填写" },
                new[] { "血型", EscapeCsv(report.BloodType) },
                new[] { "身高(cm)", report.HeightCm?.ToString() ?? "未填写" },
                new[] { "体重(kg)", report.WeightKg?.ToString() ?? "未填写" },
                new[] { "BMI", CalculateBmi(report.HeightCm, report.WeightKg) },
                new[] { "过敏史", EscapeCsv(report.Allergies) },
                new[] { "慢性病史", EscapeCsv(report.ChronicDiseases) },
                new[] { "当前用药", EscapeCsv(report.CurrentMedications) },
                new[] { "既往病史", EscapeCsv(report.MedicalHistory) },
                new[] { "紧急联系人", EscapeCsv(report.EmergencyContact) },
                new[] { "紧急联系电话", EscapeCsv(report.EmergencyPhone) },
                new[] { "备注", EscapeCsv(report.Notes) },
                new[] { "报告生成时间", report.ReportGeneratedAt.ToString("yyyy-MM-dd HH:mm:ss") }
            };

            var fileName = $"health-report-{report.FullName}-{DateTime.Now:yyyyMMddHHmmss}.csv";
            var csvBytes = BuildCsv(headers, rows);

            return ApiResponse<CsvExportResult>.Success(new CsvExportResult
            {
                FileName = fileName,
                Content = csvBytes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出健康报告CSV失败");
            return ApiResponse<CsvExportResult>.Error("导出健康报告CSV失败: " + ex.Message, 500);
        }
    }

    private async Task<bool> CanAccessHealthProfile(HealthProfile profile, int currentUserId, bool? preCheckedAdmin = null)
    {
        if (profile.UserId == currentUserId)
            return true;

        if (preCheckedAdmin.HasValue && preCheckedAdmin.Value)
            return true;

        var members = await _unitOfWork.HouseholdMembers
            .FindAsync(hm => hm.HouseholdId == profile.HouseholdId && hm.UserId == currentUserId);
        var member = members.FirstOrDefault();

        return member != null && (member.Role == "Owner" || member.Role == "Admin");
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
            return false;

        return allowedRoles.Contains(member.Role);
    }

    private async Task LogAudit(int healthProfileId, int modifiedByUserId, string modifiedByUsername,
        string changeType, string fieldName, string? oldValue, string? newValue)
    {
        var log = new HealthProfileAuditLog
        {
            HealthProfileId = healthProfileId,
            ModifiedByUserId = modifiedByUserId,
            ModifiedByUsername = modifiedByUsername,
            ChangeType = changeType,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ModifiedAt = DateTime.UtcNow
        };
        await _unitOfWork.HealthProfileAuditLogs.AddAsync(log);
    }

    private static HealthProfileAuditLog CreateAuditLog(int healthProfileId, int modifiedByUserId,
        string modifiedByUsername, string changeType, string fieldName, string? oldValue, string? newValue)
    {
        return new HealthProfileAuditLog
        {
            HealthProfileId = healthProfileId,
            ModifiedByUserId = modifiedByUserId,
            ModifiedByUsername = modifiedByUsername,
            ChangeType = changeType,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ModifiedAt = DateTime.UtcNow
        };
    }

    private static string GetBloodTypeDisplayName(BloodType bloodType)
    {
        return bloodType switch
        {
            BloodType.APositive => "A型 Rh+",
            BloodType.ANegative => "A型 Rh-",
            BloodType.BPositive => "B型 Rh+",
            BloodType.BNegative => "B型 Rh-",
            BloodType.ABPositive => "AB型 Rh+",
            BloodType.ABNegative => "AB型 Rh-",
            BloodType.OPositive => "O型 Rh+",
            BloodType.ONegative => "O型 Rh-",
            _ => "未填写"
        };
    }

    private static string CalculateBmi(decimal? heightCm, decimal? weightKg)
    {
        if (!heightCm.HasValue || !weightKg.HasValue || heightCm.Value <= 0)
            return "无法计算";

        var heightM = heightCm.Value / 100m;
        var bmi = weightKg.Value / (heightM * heightM);
        var category = string.Empty;

        if (bmi < 18.5m) category = "偏瘦";
        else if (bmi < 24m) category = "正常";
        else if (bmi < 28m) category = "超重";
        else category = "肥胖";

        return $"{bmi:F1} ({category})";
    }

    private static byte[] BuildCsv(string[] headers, List<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine(string.Join(",", headers));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static PagedResult<HealthProfileDto> BuildEmptyPagedResult(HealthProfileQueryParamsDto queryParams)
    {
        return new PagedResult<HealthProfileDto>
        {
            Items = new List<HealthProfileDto>(),
            TotalCount = 0,
            PageIndex = queryParams.PageIndex,
            PageSize = queryParams.PageSize,
            TotalPages = 0,
            HasPreviousPage = false,
            HasNextPage = false
        };
    }
}
