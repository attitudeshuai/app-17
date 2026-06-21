using System.Text;
using Mapster;
using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.ProcurementSuggestion;
using MedCabinet.Application.Interfaces;
using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Enums;
using MedCabinet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedCabinet.Application.Services;

public class ProcurementSuggestionService : IProcurementSuggestionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcurementSuggestionService> _logger;

    public ProcurementSuggestionService(IUnitOfWork unitOfWork, ILogger<ProcurementSuggestionService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<ProcurementSuggestionDto>>> GetProcurementSuggestionsAsync(
        ProcurementSuggestionQueryParamsDto queryParams, int userId)
    {
        try
        {
            var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
            var householdIds = userMembers.Select(hm => hm.HouseholdId).ToList();

            if (!householdIds.Any())
            {
                return ApiResponse<PagedResult<ProcurementSuggestionDto>>.Success(BuildEmptyPagedResult(queryParams));
            }

            if (queryParams.HouseholdId.HasValue && !householdIds.Contains(queryParams.HouseholdId.Value))
            {
                return ApiResponse<PagedResult<ProcurementSuggestionDto>>.Error("无权限访问此家庭采购建议", 403);
            }

            var householdIdFilter = queryParams.HouseholdId;
            var statusFilter = queryParams.Status;
            var urgencyFilter = queryParams.UrgencyLevel;
            var medicineIdFilter = queryParams.MedicineId;
            var userIdFilter = queryParams.UserId;
            var keyword = queryParams.SearchKeyword?.ToLower();

            var (items, totalCount) = await _unitOfWork.ProcurementSuggestions.GetPagedAsync(
                queryParams.PageIndex,
                queryParams.PageSize,
                ps => householdIds.Contains(ps.HouseholdId) &&
                     (!householdIdFilter.HasValue || ps.HouseholdId == householdIdFilter.Value) &&
                     (!statusFilter.HasValue || ps.Status == statusFilter.Value) &&
                     (!urgencyFilter.HasValue || ps.UrgencyLevel == urgencyFilter.Value) &&
                     (!medicineIdFilter.HasValue || ps.MedicineId == medicineIdFilter.Value) &&
                     (!userIdFilter.HasValue || ps.UserId == userIdFilter.Value) &&
                     (string.IsNullOrEmpty(keyword) ||
                         ps.Medicine != null && ps.Medicine.Name.ToLower().Contains(keyword) ||
                         ps.Notes.ToLower().Contains(keyword)),
                queryParams.SortBy ?? "CreatedAt",
                queryParams.SortDescending);

            var dtos = new List<ProcurementSuggestionDto>();
            foreach (var item in items)
            {
                var dto = item.Adapt<ProcurementSuggestionDto>();
                dto.MedicineName = item.Medicine?.Name ?? string.Empty;
                dto.Username = item.User?.Username;
                dto.HouseholdName = item.Household?.Name ?? string.Empty;
                dtos.Add(dto);
            }

            var result = new PagedResult<ProcurementSuggestionDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageIndex = queryParams.PageIndex,
                PageSize = queryParams.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / queryParams.PageSize),
                HasPreviousPage = queryParams.PageIndex > 1,
                HasNextPage = queryParams.PageIndex * queryParams.PageSize < totalCount
            };

            return ApiResponse<PagedResult<ProcurementSuggestionDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取采购建议列表失败");
            return ApiResponse<PagedResult<ProcurementSuggestionDto>>.Error("获取采购建议列表失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<ProcurementSuggestionDto>> GetProcurementSuggestionByIdAsync(int id, int userId)
    {
        try
        {
            var suggestion = await _unitOfWork.ProcurementSuggestions.GetByIdAsync(id);
            if (suggestion == null)
            {
                return ApiResponse<ProcurementSuggestionDto>.Error("采购建议不存在", 404);
            }

            var hasAccess = await HasHouseholdAccessAsync(suggestion.HouseholdId, userId);
            if (!hasAccess)
            {
                return ApiResponse<ProcurementSuggestionDto>.Error("无权限访问此采购建议", 403);
            }

            var dto = suggestion.Adapt<ProcurementSuggestionDto>();
            dto.MedicineName = suggestion.Medicine?.Name ?? string.Empty;
            dto.Username = suggestion.User?.Username;
            dto.HouseholdName = suggestion.Household?.Name ?? string.Empty;
            return ApiResponse<ProcurementSuggestionDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取采购建议详情失败");
            return ApiResponse<ProcurementSuggestionDto>.Error("获取采购建议详情失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<List<ProcurementSuggestionDto>>> GenerateProcurementSuggestionsAsync(
        GenerateProcurementSuggestionsRequestDto request, int userId)
    {
        try
        {
            var hasPermission = await HasPermissionAsync(request.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<List<ProcurementSuggestionDto>>.Error("无权限生成采购建议", 403);
            }

            var medicines = await _unitOfWork.Medicines.FindAsync(m => m.HouseholdId == request.HouseholdId);
            var medicineList = medicines.ToList();

            if (!medicineList.Any())
            {
                return ApiResponse<List<ProcurementSuggestionDto>>.Success(new List<ProcurementSuggestionDto>());
            }

            var members = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.HouseholdId == request.HouseholdId);
            var memberList = members.ToList();

            if (!memberList.Any())
            {
                return ApiResponse<List<ProcurementSuggestionDto>>.Success(new List<ProcurementSuggestionDto>());
            }

            var generatedSuggestions = new List<ProcurementSuggestion>();

            foreach (var medicine in medicineList)
            {
                var daysUntilExpiry = (int)(medicine.ExpiryDate - DateTime.Now).TotalDays;

                var allUsages = await _unitOfWork.MedUsages.FindAsync(mu => mu.MedicineId == medicine.Id);
                var usageList = allUsages.ToList();
                var recentUsages = usageList.Where(mu => mu.UsedAt >= DateTime.Now.AddDays(-30)).ToList();

                var perMemberSuggestions = new List<ProcurementSuggestion>();
                var totalSuggestedQuantityForMedicine = 0;
                var highestUrgencyForMedicine = UrgencyLevel.Low;

                foreach (var member in memberList)
                {
                    var memberUsages = recentUsages.Where(mu => mu.UserId == member.UserId).ToList();
                    var memberTotalUsage = memberUsages.Sum(mu => mu.UsedQuantity);
                    var memberUsageFrequency = memberTotalUsage / 30m;

                    bool memberNeedsProcurement = false;
                    int memberSuggestedQuantity = 0;
                    var memberUrgency = UrgencyLevel.Low;
                    var memberNotes = string.Empty;

                    if (memberUsageFrequency > 0)
                    {
                        var memberStockShare = medicine.StockQuantity > 0
                            ? (int)Math.Ceiling((double)medicine.StockQuantity * memberTotalUsage / Math.Max(recentUsages.Sum(mu => mu.UsedQuantity), 1))
                            : 0;

                        var memberDaysRemaining = memberUsageFrequency > 0
                            ? (int)(memberStockShare / memberUsageFrequency)
                            : 999;

                        if (medicine.StockQuantity <= 0)
                        {
                            memberNeedsProcurement = true;
                            memberSuggestedQuantity = Math.Max((int)Math.Ceiling(memberUsageFrequency * 30), 1);
                            memberUrgency = UrgencyLevel.Critical;
                            memberNotes = "库存已空，需立即采购";
                        }
                        else if (memberDaysRemaining <= 3)
                        {
                            memberNeedsProcurement = true;
                            memberSuggestedQuantity = Math.Max((int)Math.Ceiling(memberUsageFrequency * 30), 1);
                            memberUrgency = UrgencyLevel.Critical;
                            memberNotes = $"按成员用量，预计{memberDaysRemaining}天耗尽";
                        }
                        else if (memberDaysRemaining <= 7)
                        {
                            memberNeedsProcurement = true;
                            memberSuggestedQuantity = Math.Max((int)Math.Ceiling(memberUsageFrequency * 30), 1);
                            memberUrgency = UrgencyLevel.High;
                            memberNotes = $"按成员用量，预计{memberDaysRemaining}天耗尽";
                        }
                        else if (memberDaysRemaining <= 14)
                        {
                            memberNeedsProcurement = true;
                            memberSuggestedQuantity = Math.Max((int)Math.Ceiling(memberUsageFrequency * 30), 1);
                            memberUrgency = UrgencyLevel.Medium;
                            memberNotes = $"按成员用量，预计{memberDaysRemaining}天耗尽";
                        }
                    }
                    else if (daysUntilExpiry <= 30 && daysUntilExpiry > 0)
                    {
                        memberNeedsProcurement = true;
                        memberSuggestedQuantity = 1;
                        memberUrgency = daysUntilExpiry <= 7 ? UrgencyLevel.High : UrgencyLevel.Medium;
                        memberNotes = $"药品将在{daysUntilExpiry}天后过期，需提前采购替换";
                    }

                    if (!memberNeedsProcurement)
                    {
                        continue;
                    }

                    var memberSuggestedPurchaseDate = CalculateSuggestedPurchaseDate(memberUrgency);

                    var existingMemberPending = await _unitOfWork.ProcurementSuggestions.ExistsAsync(
                        ps => ps.MedicineId == medicine.Id &&
                              ps.HouseholdId == request.HouseholdId &&
                              ps.UserId == member.UserId &&
                              ps.Status == ProcurementStatus.Pending);

                    if (existingMemberPending)
                    {
                        continue;
                    }

                    var memberSuggestion = new ProcurementSuggestion
                    {
                        HouseholdId = request.HouseholdId,
                        MedicineId = medicine.Id,
                        UserId = member.UserId,
                        SuggestedQuantity = memberSuggestedQuantity,
                        SuggestedPurchaseDate = memberSuggestedPurchaseDate,
                        UrgencyLevel = memberUrgency,
                        Status = ProcurementStatus.Pending,
                        UsageFrequency = memberUsageFrequency,
                        CurrentStock = medicine.StockQuantity,
                        DaysUntilExpiry = daysUntilExpiry,
                        Notes = memberNotes
                    };

                    await _unitOfWork.ProcurementSuggestions.AddAsync(memberSuggestion);
                    perMemberSuggestions.Add(memberSuggestion);
                    generatedSuggestions.Add(memberSuggestion);

                    totalSuggestedQuantityForMedicine += memberSuggestedQuantity;
                    if (memberUrgency > highestUrgencyForMedicine)
                    {
                        highestUrgencyForMedicine = memberUrgency;
                    }
                }

                if (medicine.StockQuantity <= 0 && !perMemberSuggestions.Any())
                {
                    var familyFrequency = recentUsages.Sum(mu => mu.UsedQuantity) / 30m;
                    var suggestedQty = Math.Max((int)Math.Ceiling(familyFrequency * 30), 1);
                    var urgency = UrgencyLevel.Critical;
                    var suggestedDate = CalculateSuggestedPurchaseDate(urgency);

                    var existingHouseholdPending = await _unitOfWork.ProcurementSuggestions.ExistsAsync(
                        ps => ps.MedicineId == medicine.Id &&
                              ps.HouseholdId == request.HouseholdId &&
                              ps.UserId == null &&
                              ps.Status == ProcurementStatus.Pending);

                    if (!existingHouseholdPending)
                    {
                        var householdSuggestion = new ProcurementSuggestion
                        {
                            HouseholdId = request.HouseholdId,
                            MedicineId = medicine.Id,
                            UserId = null,
                            SuggestedQuantity = suggestedQty,
                            SuggestedPurchaseDate = suggestedDate,
                            UrgencyLevel = urgency,
                            Status = ProcurementStatus.Pending,
                            UsageFrequency = familyFrequency,
                            CurrentStock = medicine.StockQuantity,
                            DaysUntilExpiry = daysUntilExpiry,
                            Notes = "库存已空，家庭汇总采购"
                        };
                        await _unitOfWork.ProcurementSuggestions.AddAsync(householdSuggestion);
                        generatedSuggestions.Add(householdSuggestion);
                    }
                }
                else if (perMemberSuggestions.Any())
                {
                    var familyFrequency = recentUsages.Sum(mu => mu.UsedQuantity) / 30m;
                    var aggregatedUrgency = highestUrgencyForMedicine;
                    var aggregatedDate = CalculateSuggestedPurchaseDate(aggregatedUrgency);
                    var memberNames = string.Join("、", perMemberSuggestions
                        .Where(s => s.UserId.HasValue)
                        .Select(s =>
                        {
                            var m = memberList.FirstOrDefault(mm => mm.UserId == s.UserId);
                            return m?.User?.Username ?? $"成员{s.UserId}";
                        })
                        .Distinct());

                    var existingHouseholdPending = await _unitOfWork.ProcurementSuggestions.ExistsAsync(
                        ps => ps.MedicineId == medicine.Id &&
                              ps.HouseholdId == request.HouseholdId &&
                              ps.UserId == null &&
                              ps.Status == ProcurementStatus.Pending);

                    if (!existingHouseholdPending)
                    {
                        var householdSuggestion = new ProcurementSuggestion
                        {
                            HouseholdId = request.HouseholdId,
                            MedicineId = medicine.Id,
                            UserId = null,
                            SuggestedQuantity = totalSuggestedQuantityForMedicine,
                            SuggestedPurchaseDate = aggregatedDate,
                            UrgencyLevel = aggregatedUrgency,
                            Status = ProcurementStatus.Pending,
                            UsageFrequency = familyFrequency,
                            CurrentStock = medicine.StockQuantity,
                            DaysUntilExpiry = daysUntilExpiry,
                            Notes = $"家庭汇总采购，涉及成员: {memberNames}"
                        };
                        await _unitOfWork.ProcurementSuggestions.AddAsync(householdSuggestion);
                        generatedSuggestions.Add(householdSuggestion);
                    }
                }
            }

            if (generatedSuggestions.Any())
            {
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation($"为家庭{request.HouseholdId}生成了{generatedSuggestions.Count}条采购建议");
            }

            var memberIds = memberList.Select(m => m.UserId).Distinct().ToList();
            var users = memberIds.Any()
                ? (await _unitOfWork.Users.FindAsync(u => memberIds.Contains(u.Id))).ToDictionary(u => u.Id, u => u.Username)
                : new Dictionary<int, string>();

            var resultDtos = generatedSuggestions.Select(ps =>
            {
                var dto = ps.Adapt<ProcurementSuggestionDto>();
                dto.MedicineName = ps.Medicine?.Name ?? medicineList.FirstOrDefault(m => m.Id == ps.MedicineId)?.Name ?? string.Empty;
                dto.Username = ps.UserId.HasValue ? users.GetValueOrDefault(ps.UserId.Value, ps.User?.Username ?? string.Empty) : null;
                dto.HouseholdName = ps.Household?.Name ?? string.Empty;
                return dto;
            }).ToList();

            return ApiResponse<List<ProcurementSuggestionDto>>.Success(resultDtos, $"成功生成{generatedSuggestions.Count}条采购建议(含成员明细和家庭汇总)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成采购建议失败");
            return ApiResponse<List<ProcurementSuggestionDto>>.Error("生成采购建议失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<ProcurementSuggestionDto>> CreateProcurementSuggestionAsync(
        CreateProcurementSuggestionRequestDto request, int userId)
    {
        try
        {
            var hasPermission = await HasPermissionAsync(request.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<ProcurementSuggestionDto>.Error("无权限创建采购建议", 403);
            }

            var medicine = await _unitOfWork.Medicines.GetByIdAsync(request.MedicineId);
            if (medicine == null)
            {
                return ApiResponse<ProcurementSuggestionDto>.Error("药品不存在", 404);
            }

            var daysUntilExpiry = (int)(medicine.ExpiryDate - DateTime.Now).TotalDays;

            var suggestion = new ProcurementSuggestion
            {
                HouseholdId = request.HouseholdId,
                MedicineId = request.MedicineId,
                UserId = request.UserId,
                SuggestedQuantity = request.SuggestedQuantity,
                SuggestedPurchaseDate = request.SuggestedPurchaseDate,
                UrgencyLevel = request.UrgencyLevel,
                Status = ProcurementStatus.Pending,
                UsageFrequency = 0,
                CurrentStock = medicine.StockQuantity,
                DaysUntilExpiry = daysUntilExpiry,
                Notes = request.Notes ?? string.Empty
            };

            await _unitOfWork.ProcurementSuggestions.AddAsync(suggestion);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"手动创建采购建议: 药品ID={request.MedicineId}");

            var dto = suggestion.Adapt<ProcurementSuggestionDto>();
            dto.MedicineName = medicine.Name;
            dto.HouseholdName = suggestion.Household?.Name ?? string.Empty;
            return ApiResponse<ProcurementSuggestionDto>.Success(dto, "创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建采购建议失败");
            return ApiResponse<ProcurementSuggestionDto>.Error("创建采购建议失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<ProcurementSuggestionDto>> UpdateProcurementSuggestionAsync(
        int id, UpdateProcurementSuggestionRequestDto request, int userId)
    {
        try
        {
            var suggestion = await _unitOfWork.ProcurementSuggestions.GetByIdAsync(id);
            if (suggestion == null)
            {
                return ApiResponse<ProcurementSuggestionDto>.Error("采购建议不存在", 404);
            }

            var hasPermission = await HasPermissionAsync(suggestion.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse<ProcurementSuggestionDto>.Error("无权限修改采购建议", 403);
            }

            if (request.SuggestedQuantity.HasValue)
                suggestion.SuggestedQuantity = request.SuggestedQuantity.Value;
            if (request.SuggestedPurchaseDate.HasValue)
                suggestion.SuggestedPurchaseDate = request.SuggestedPurchaseDate.Value;
            if (request.UrgencyLevel.HasValue)
                suggestion.UrgencyLevel = request.UrgencyLevel.Value;
            if (request.Notes != null)
                suggestion.Notes = request.Notes;

            await _unitOfWork.ProcurementSuggestions.UpdateAsync(suggestion);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"更新采购建议: ID={id}");

            var dto = suggestion.Adapt<ProcurementSuggestionDto>();
            dto.MedicineName = suggestion.Medicine?.Name ?? string.Empty;
            dto.Username = suggestion.User?.Username;
            dto.HouseholdName = suggestion.Household?.Name ?? string.Empty;
            return ApiResponse<ProcurementSuggestionDto>.Success(dto, "更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新采购建议失败");
            return ApiResponse<ProcurementSuggestionDto>.Error("更新采购建议失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<ProcurementSuggestionDto>> MarkProcurementSuggestionAsync(
        int id, MarkProcurementSuggestionRequestDto request, int userId)
    {
        try
        {
            var suggestion = await _unitOfWork.ProcurementSuggestions.GetByIdAsync(id);
            if (suggestion == null)
            {
                return ApiResponse<ProcurementSuggestionDto>.Error("采购建议不存在", 404);
            }

            var hasAccess = await HasHouseholdAccessAsync(suggestion.HouseholdId, userId);
            if (!hasAccess)
            {
                return ApiResponse<ProcurementSuggestionDto>.Error("无权限操作此采购建议", 403);
            }

            if (request.Status == ProcurementStatus.Purchased)
            {
                suggestion.Status = ProcurementStatus.Purchased;
                suggestion.PurchasedAt = DateTime.UtcNow;
                suggestion.PurchasedQuantity = request.PurchasedQuantity ?? suggestion.SuggestedQuantity;

                var medicine = await _unitOfWork.Medicines.GetByIdAsync(suggestion.MedicineId);
                if (medicine != null)
                {
                    medicine.StockQuantity += suggestion.PurchasedQuantity.Value;
                    medicine.Status = CalculateMedicineStatus(medicine.ExpiryDate, medicine.StockQuantity);
                    await _unitOfWork.Medicines.UpdateAsync(medicine);
                }
            }
            else if (request.Status == ProcurementStatus.Ignored)
            {
                suggestion.Status = ProcurementStatus.Ignored;
            }

            if (request.Notes != null)
                suggestion.Notes = request.Notes;

            await _unitOfWork.ProcurementSuggestions.UpdateAsync(suggestion);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"标记采购建议: ID={id}, Status={request.Status}");

            var dto = suggestion.Adapt<ProcurementSuggestionDto>();
            dto.MedicineName = suggestion.Medicine?.Name ?? string.Empty;
            dto.Username = suggestion.User?.Username;
            dto.HouseholdName = suggestion.Household?.Name ?? string.Empty;
            return ApiResponse<ProcurementSuggestionDto>.Success(dto, "操作成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记采购建议失败");
            return ApiResponse<ProcurementSuggestionDto>.Error("标记采购建议失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse> DeleteProcurementSuggestionAsync(int id, int userId)
    {
        try
        {
            var suggestion = await _unitOfWork.ProcurementSuggestions.GetByIdAsync(id);
            if (suggestion == null)
            {
                return ApiResponse.Error("采购建议不存在", 404);
            }

            var hasPermission = await HasPermissionAsync(suggestion.HouseholdId, userId, "Owner", "Admin");
            if (!hasPermission)
            {
                return ApiResponse.Error("无权限删除采购建议", 403);
            }

            await _unitOfWork.ProcurementSuggestions.DeleteAsync(suggestion);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"删除采购建议: ID={id}");
            return ApiResponse.Success("删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除采购建议失败");
            return ApiResponse.Error("删除采购建议失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<ProcurementStatsDto>> GetProcurementStatsAsync(int? householdId, int userId)
    {
        try
        {
            var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
            var householdIds = userMembers.Select(hm => hm.HouseholdId).ToList();

            if (!householdIds.Any())
            {
                return ApiResponse<ProcurementStatsDto>.Success(new ProcurementStatsDto());
            }

            if (householdId.HasValue && !householdIds.Contains(householdId.Value))
            {
                return ApiResponse<ProcurementStatsDto>.Error("无权限访问此家庭统计数据", 403);
            }

            var isAdmin = await IsAdminInHousehold(householdId, userId);
            if (!isAdmin)
            {
                return ApiResponse<ProcurementStatsDto>.Error("仅管理员可查看采购统计", 403);
            }

            var targetHouseholdIds = householdId.HasValue
                ? new List<int> { householdId.Value }
                : householdIds;

            var allSuggestions = await _unitOfWork.ProcurementSuggestions
                .FindAsync(ps => targetHouseholdIds.Contains(ps.HouseholdId));
            var suggestionList = allSuggestions.ToList();

            var stats = new ProcurementStatsDto
            {
                TotalSuggestions = suggestionList.Count,
                PendingCount = suggestionList.Count(s => s.Status == ProcurementStatus.Pending),
                PurchasedCount = suggestionList.Count(s => s.Status == ProcurementStatus.Purchased),
                IgnoredCount = suggestionList.Count(s => s.Status == ProcurementStatus.Ignored),
                LowUrgencyCount = suggestionList.Count(s => s.UrgencyLevel == UrgencyLevel.Low),
                MediumUrgencyCount = suggestionList.Count(s => s.UrgencyLevel == UrgencyLevel.Medium),
                HighUrgencyCount = suggestionList.Count(s => s.UrgencyLevel == UrgencyLevel.High),
                CriticalUrgencyCount = suggestionList.Count(s => s.UrgencyLevel == UrgencyLevel.Critical)
            };

            var medicineIds = suggestionList.Select(s => s.MedicineId).Distinct().ToList();
            var medicines = await _unitOfWork.Medicines.FindAsync(m => medicineIds.Contains(m.Id));
            var medicineDict = medicines.ToDictionary(m => m.Id, m => m.Name);

            stats.ByMedicine = suggestionList
                .GroupBy(s => s.MedicineId)
                .Select(g => new ProcurementByMedicineDto
                {
                    MedicineId = g.Key,
                    MedicineName = medicineDict.GetValueOrDefault(g.Key, "未知药品"),
                    SuggestionCount = g.Count(),
                    TotalSuggestedQuantity = g.Sum(s => s.SuggestedQuantity),
                    PurchasedCount = g.Count(s => s.Status == ProcurementStatus.Purchased),
                    PendingCount = g.Count(s => s.Status == ProcurementStatus.Pending)
                })
                .OrderByDescending(m => m.SuggestionCount)
                .ToList();

            var userIds = suggestionList.Where(s => s.UserId.HasValue).Select(s => s.UserId!.Value).Distinct().ToList();
            var users = userIds.Any()
                ? (await _unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id))).ToDictionary(u => u.Id, u => u.Username)
                : new Dictionary<int, string>();

            stats.ByMember = suggestionList
                .Where(s => s.UserId.HasValue)
                .GroupBy(s => s.UserId!.Value)
                .Select(g => new ProcurementByMemberDto
                {
                    UserId = g.Key,
                    Username = users.GetValueOrDefault(g.Key, "未知用户"),
                    SuggestionCount = g.Count(),
                    TotalSuggestedQuantity = g.Sum(s => s.SuggestedQuantity),
                    PurchasedCount = g.Count(s => s.Status == ProcurementStatus.Purchased),
                    PendingCount = g.Count(s => s.Status == ProcurementStatus.Pending)
                })
                .OrderByDescending(m => m.SuggestionCount)
                .ToList();

            var monthlyTrend = new List<ProcurementMonthlyTrendDto>();
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                monthlyTrend.Add(new ProcurementMonthlyTrendDto
                {
                    Month = month.ToString("yyyy-MM"),
                    GeneratedCount = suggestionList.Count(s => s.CreatedAt >= monthStart && s.CreatedAt < monthEnd),
                    PurchasedCount = suggestionList.Count(s => s.PurchasedAt.HasValue &&
                                                              s.PurchasedAt.Value >= monthStart &&
                                                              s.PurchasedAt.Value < monthEnd)
                });
            }
            stats.MonthlyTrend = monthlyTrend;

            return ApiResponse<ProcurementStatsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取采购统计失败");
            return ApiResponse<ProcurementStatsDto>.Error("获取采购统计失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<CsvExportResult>> ExportByMedicineAsync(
        ProcurementExportQueryParamsDto queryParams, int userId)
    {
        try
        {
            var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
            var householdIds = userMembers.Select(hm => hm.HouseholdId).ToList();

            if (!householdIds.Any())
            {
                return ApiResponse<CsvExportResult>.Success(EmptyCsv("procurement-by-medicine"));
            }

            if (queryParams.HouseholdId.HasValue && !householdIds.Contains(queryParams.HouseholdId.Value))
            {
                return ApiResponse<CsvExportResult>.Error("无权限导出此家庭数据", 403);
            }

            var isAdmin = await IsAdminInHousehold(queryParams.HouseholdId, userId);
            if (!isAdmin)
            {
                return ApiResponse<CsvExportResult>.Error("仅管理员可导出采购报表", 403);
            }

            var targetHouseholdIds = queryParams.HouseholdId.HasValue
                ? new List<int> { queryParams.HouseholdId.Value }
                : householdIds;

            var suggestions = await _unitOfWork.ProcurementSuggestions
                .FindAsync(ps => targetHouseholdIds.Contains(ps.HouseholdId));

            var filtered = suggestions.AsEnumerable();

            if (queryParams.StartDate.HasValue)
                filtered = filtered.Where(s => s.CreatedAt >= queryParams.StartDate.Value);
            if (queryParams.EndDate.HasValue)
                filtered = filtered.Where(s => s.CreatedAt < queryParams.EndDate.Value.AddDays(1));

            var suggestionList = filtered.ToList();

            var medicineIds = suggestionList.Select(s => s.MedicineId).Distinct().ToList();
            var medicines = await _unitOfWork.Medicines.FindAsync(m => medicineIds.Contains(m.Id));
            var medicineDict = medicines.ToDictionary(m => m.Id, m => m.Name);

            var grouped = suggestionList
                .GroupBy(s => s.MedicineId)
                .Select(g => new
                {
                    MedicineId = g.Key,
                    MedicineName = medicineDict.GetValueOrDefault(g.Key, "未知药品"),
                    SuggestionCount = g.Count(),
                    TotalSuggestedQuantity = g.Sum(s => s.SuggestedQuantity),
                    TotalPurchasedQuantity = g.Sum(s => s.PurchasedQuantity ?? 0),
                    PurchasedCount = g.Count(s => s.Status == ProcurementStatus.Purchased),
                    PendingCount = g.Count(s => s.Status == ProcurementStatus.Pending),
                    IgnoredCount = g.Count(s => s.Status == ProcurementStatus.Ignored),
                    CriticalCount = g.Count(s => s.UrgencyLevel == UrgencyLevel.Critical),
                    HighCount = g.Count(s => s.UrgencyLevel == UrgencyLevel.High),
                    MediumCount = g.Count(s => s.UrgencyLevel == UrgencyLevel.Medium),
                    LowCount = g.Count(s => s.UrgencyLevel == UrgencyLevel.Low)
                })
                .OrderByDescending(m => m.SuggestionCount)
                .ToList();

            var headers = new[]
            {
                "药品ID", "药品名称", "建议总条数", "建议采购总数量",
                "已采购总数量", "已采购条数", "待采购条数", "已忽略条数",
                "紧急条数", "高优先级条数", "中优先级条数", "低优先级条数"
            };

            var rows = grouped.Select(r => new[]
            {
                r.MedicineId.ToString(),
                EscapeCsv(r.MedicineName),
                r.SuggestionCount.ToString(),
                r.TotalSuggestedQuantity.ToString(),
                r.TotalPurchasedQuantity.ToString(),
                r.PurchasedCount.ToString(),
                r.PendingCount.ToString(),
                r.IgnoredCount.ToString(),
                r.CriticalCount.ToString(),
                r.HighCount.ToString(),
                r.MediumCount.ToString(),
                r.LowCount.ToString()
            }).ToList();

            var fileName = $"procurement-by-medicine-{DateTime.Now:yyyyMMddHHmmss}.csv";
            var csvBytes = BuildCsv(headers, rows);

            return ApiResponse<CsvExportResult>.Success(new CsvExportResult
            {
                FileName = fileName,
                Content = csvBytes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按药品维度导出采购建议报表失败");
            return ApiResponse<CsvExportResult>.Error("导出报表失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<CsvExportResult>> ExportByMemberAsync(
        ProcurementExportQueryParamsDto queryParams, int userId)
    {
        try
        {
            var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
            var householdIds = userMembers.Select(hm => hm.HouseholdId).ToList();

            if (!householdIds.Any())
            {
                return ApiResponse<CsvExportResult>.Success(EmptyCsv("procurement-by-member"));
            }

            if (queryParams.HouseholdId.HasValue && !householdIds.Contains(queryParams.HouseholdId.Value))
            {
                return ApiResponse<CsvExportResult>.Error("无权限导出此家庭数据", 403);
            }

            var isAdmin = await IsAdminInHousehold(queryParams.HouseholdId, userId);
            if (!isAdmin)
            {
                return ApiResponse<CsvExportResult>.Error("仅管理员可导出采购报表", 403);
            }

            var targetHouseholdIds = queryParams.HouseholdId.HasValue
                ? new List<int> { queryParams.HouseholdId.Value }
                : householdIds;

            var suggestions = await _unitOfWork.ProcurementSuggestions
                .FindAsync(ps => targetHouseholdIds.Contains(ps.HouseholdId));

            var filtered = suggestions.AsEnumerable();

            if (queryParams.StartDate.HasValue)
                filtered = filtered.Where(s => s.CreatedAt >= queryParams.StartDate.Value);
            if (queryParams.EndDate.HasValue)
                filtered = filtered.Where(s => s.CreatedAt < queryParams.EndDate.Value.AddDays(1));

            var suggestionList = filtered.ToList();

            var userIds = suggestionList.Where(s => s.UserId.HasValue).Select(s => s.UserId!.Value).Distinct().ToList();
            var users = userIds.Any()
                ? (await _unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id))).ToDictionary(u => u.Id, u => u.Username)
                : new Dictionary<int, string>();

            var householdSuggestionCount = suggestionList.Count(s => !s.UserId.HasValue);
            var householdSuggestedQty = suggestionList.Where(s => !s.UserId.HasValue).Sum(s => s.SuggestedQuantity);
            var householdPurchasedQty = suggestionList.Where(s => !s.UserId.HasValue).Sum(s => s.PurchasedQuantity ?? 0);

            var grouped = suggestionList
                .Where(s => s.UserId.HasValue)
                .GroupBy(s => s.UserId!.Value)
                .Select(g => new
                {
                    UserId = g.Key,
                    Username = users.GetValueOrDefault(g.Key, "未知用户"),
                    SuggestionCount = g.Count(),
                    TotalSuggestedQuantity = g.Sum(s => s.SuggestedQuantity),
                    TotalPurchasedQuantity = g.Sum(s => s.PurchasedQuantity ?? 0),
                    PurchasedCount = g.Count(s => s.Status == ProcurementStatus.Purchased),
                    PendingCount = g.Count(s => s.Status == ProcurementStatus.Pending),
                    IgnoredCount = g.Count(s => s.Status == ProcurementStatus.Ignored),
                    CriticalCount = g.Count(s => s.UrgencyLevel == UrgencyLevel.Critical),
                    HighCount = g.Count(s => s.UrgencyLevel == UrgencyLevel.High),
                    MediumCount = g.Count(s => s.UrgencyLevel == UrgencyLevel.Medium),
                    LowCount = g.Count(s => s.UrgencyLevel == UrgencyLevel.Low)
                })
                .OrderByDescending(m => m.SuggestionCount)
                .ToList();

            var rows = new List<string[]>();

            if (householdSuggestionCount > 0)
            {
                rows.Add(new[]
                {
                    "",
                    "家庭汇总",
                    householdSuggestionCount.ToString(),
                    householdSuggestedQty.ToString(),
                    householdPurchasedQty.ToString(),
                    suggestionList.Count(s => !s.UserId.HasValue && s.Status == ProcurementStatus.Purchased).ToString(),
                    suggestionList.Count(s => !s.UserId.HasValue && s.Status == ProcurementStatus.Pending).ToString(),
                    suggestionList.Count(s => !s.UserId.HasValue && s.Status == ProcurementStatus.Ignored).ToString(),
                    suggestionList.Count(s => !s.UserId.HasValue && s.UrgencyLevel == UrgencyLevel.Critical).ToString(),
                    suggestionList.Count(s => !s.UserId.HasValue && s.UrgencyLevel == UrgencyLevel.High).ToString(),
                    suggestionList.Count(s => !s.UserId.HasValue && s.UrgencyLevel == UrgencyLevel.Medium).ToString(),
                    suggestionList.Count(s => !s.UserId.HasValue && s.UrgencyLevel == UrgencyLevel.Low).ToString()
                });
            }

            rows.AddRange(grouped.Select(r => new[]
            {
                r.UserId.ToString(),
                EscapeCsv(r.Username),
                r.SuggestionCount.ToString(),
                r.TotalSuggestedQuantity.ToString(),
                r.TotalPurchasedQuantity.ToString(),
                r.PurchasedCount.ToString(),
                r.PendingCount.ToString(),
                r.IgnoredCount.ToString(),
                r.CriticalCount.ToString(),
                r.HighCount.ToString(),
                r.MediumCount.ToString(),
                r.LowCount.ToString()
            }));

            var headers = new[]
            {
                "成员ID", "成员名称", "建议总条数", "建议采购总数量",
                "已采购总数量", "已采购条数", "待采购条数", "已忽略条数",
                "紧急条数", "高优先级条数", "中优先级条数", "低优先级条数"
            };

            var fileName = $"procurement-by-member-{DateTime.Now:yyyyMMddHHmmss}.csv";
            var csvBytes = BuildCsv(headers, rows);

            return ApiResponse<CsvExportResult>.Success(new CsvExportResult
            {
                FileName = fileName,
                Content = csvBytes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按家庭成员维度导出采购建议报表失败");
            return ApiResponse<CsvExportResult>.Error("导出报表失败: " + ex.Message, 500);
        }
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

    private static CsvExportResult EmptyCsv(string prefix)
    {
        return new CsvExportResult
        {
            FileName = $"{prefix}-{DateTime.Now:yyyyMMddHHmmss}.csv",
            Content = new byte[] { 0xEF, 0xBB, 0xBF }
        };
    }

    private DateTime CalculateSuggestedPurchaseDate(UrgencyLevel urgencyLevel)
    {
        return urgencyLevel switch
        {
            UrgencyLevel.Critical => DateTime.Now,
            UrgencyLevel.High => DateTime.Now.AddDays(2),
            UrgencyLevel.Medium => DateTime.Now.AddDays(7),
            UrgencyLevel.Low => DateTime.Now.AddDays(14),
            _ => DateTime.Now.AddDays(7)
        };
    }

    private MedicineStatus CalculateMedicineStatus(DateTime expiryDate, int stockQuantity)
    {
        if (stockQuantity <= 0)
            return MedicineStatus.Empty;

        var daysUntilExpiry = (expiryDate - DateTime.Now).TotalDays;

        if (daysUntilExpiry <= 0)
            return MedicineStatus.Expired;
        if (daysUntilExpiry <= 30)
            return MedicineStatus.NearExpiry;
        return MedicineStatus.Valid;
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

    private async Task<bool> IsAdminInHousehold(int? householdId, int userId)
    {
        var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
        var memberList = userMembers.ToList();

        if (householdId.HasValue)
        {
            var member = memberList.FirstOrDefault(hm => hm.HouseholdId == householdId.Value);
            return member != null && (member.Role == "Owner" || member.Role == "Admin");
        }

        return memberList.Any(hm => hm.Role == "Owner" || hm.Role == "Admin");
    }

    private PagedResult<ProcurementSuggestionDto> BuildEmptyPagedResult(ProcurementSuggestionQueryParamsDto queryParams)
    {
        return new PagedResult<ProcurementSuggestionDto>
        {
            Items = new List<ProcurementSuggestionDto>(),
            TotalCount = 0,
            PageIndex = queryParams.PageIndex,
            PageSize = queryParams.PageSize,
            TotalPages = 0,
            HasPreviousPage = false,
            HasNextPage = false
        };
    }
}
