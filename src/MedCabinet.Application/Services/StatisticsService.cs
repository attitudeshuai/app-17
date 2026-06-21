using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.Statistics;
using MedCabinet.Application.Interfaces;
using MedCabinet.Domain.Enums;
using MedCabinet.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedCabinet.Application.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(IUnitOfWork unitOfWork, ILogger<StatisticsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApiResponse<OverviewStatsDto>> GetOverviewStatsAsync(int userId, int? householdId = null)
    {
        try
        {
            // 获取用户有权限的家庭
            var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
            var householdIds = userMembers.Select(hm => hm.HouseholdId).ToList();

            if (!householdIds.Any())
            {
                return ApiResponse<OverviewStatsDto>.Success(new OverviewStatsDto());
            }

            // 如果指定了家庭，检查权限
            if (householdId.HasValue && !householdIds.Contains(householdId.Value))
            {
                return ApiResponse<OverviewStatsDto>.Error("无权限访问此家庭统计数据", 403);
            }

            var targetHouseholdIds = householdId.HasValue
                ? new List<int> { householdId.Value }
                : householdIds;

            var stats = new OverviewStatsDto
            {
                TotalHouseholds = targetHouseholdIds.Count
            };

            // 统计成员数量
            stats.TotalMembers = await _unitOfWork.HouseholdMembers
                .CountAsync(hm => targetHouseholdIds.Contains(hm.HouseholdId));

            // 统计药品
            var allMedicines = await _unitOfWork.Medicines
                .FindAsync(m => targetHouseholdIds.Contains(m.HouseholdId));
            var medicineList = allMedicines.ToList();

            stats.TotalMedicines = medicineList.Count;
            stats.ValidMedicines = medicineList.Count(m => m.Status == MedicineStatus.Valid);
            stats.NearExpiryMedicines = medicineList.Count(m => m.Status == MedicineStatus.NearExpiry);
            stats.ExpiredMedicines = medicineList.Count(m => m.Status == MedicineStatus.Expired);
            stats.EmptyMedicines = medicineList.Count(m => m.Status == MedicineStatus.Empty);

            // 分类统计
            stats.CategoryStats = medicineList
                .GroupBy(m => m.Category)
                .Select(g => new MedicineCategoryStatDto
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            // 统计用药记录
            var medIds = medicineList.Select(m => m.Id).ToList();
            if (medIds.Any())
            {
                stats.TotalUsages = await _unitOfWork.MedUsages
                    .CountAsync(mu => medIds.Contains(mu.MedicineId));

                // 最近6个月用药趋势
                var monthlyUsages = new List<MonthlyUsageStatDto>();
                for (int i = 5; i >= 0; i--)
                {
                    var month = DateTime.Now.AddMonths(-i);
                    var monthStart = new DateTime(month.Year, month.Month, 1);
                    var monthEnd = monthStart.AddMonths(1);

                    var usagesInMonth = await _unitOfWork.MedUsages
                        .CountAsync(mu => medIds.Contains(mu.MedicineId) &&
                                         mu.UsedAt >= monthStart &&
                                         mu.UsedAt < monthEnd);

                    monthlyUsages.Add(new MonthlyUsageStatDto
                    {
                        Month = month.ToString("yyyy-MM"),
                        UsageCount = usagesInMonth
                    });
                }
                stats.MonthlyUsageStats = monthlyUsages;
            }
            else
            {
                stats.TotalUsages = 0;
                stats.MonthlyUsageStats = new List<MonthlyUsageStatDto>();
            }

            // 统计提醒
            stats.TotalAlerts = await _unitOfWork.MedAlerts
                .CountAsync(a => medIds.Contains(a.MedicineId));
            stats.UnreadAlerts = await _unitOfWork.MedAlerts
                .CountAsync(a => medIds.Contains(a.MedicineId) && !a.IsRead);

            return ApiResponse<OverviewStatsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取统计数据失败");
            return ApiResponse<OverviewStatsDto>.Error("获取统计数据失败: " + ex.Message, 500);
        }
    }

    public async Task<ApiResponse<List<MonthlyUsageStatDto>>> GetTrendStatsAsync(TrendQueryParamsDto queryParams, int userId)
    {
        try
        {
            // 获取用户有权限的家庭
            var userMembers = await _unitOfWork.HouseholdMembers.FindAsync(hm => hm.UserId == userId);
            var householdIds = userMembers.Select(hm => hm.HouseholdId).ToList();

            if (!householdIds.Any())
            {
                return ApiResponse<List<MonthlyUsageStatDto>>.Success(new List<MonthlyUsageStatDto>());
            }

            // 如果指定了家庭，检查权限
            if (queryParams.HouseholdId.HasValue && !householdIds.Contains(queryParams.HouseholdId.Value))
            {
                return ApiResponse<List<MonthlyUsageStatDto>>.Error("无权限访问此家庭统计数据", 403);
            }

            var targetHouseholdIds = queryParams.HouseholdId.HasValue
                ? new List<int> { queryParams.HouseholdId.Value }
                : householdIds;

            // 获取这些家庭的药品ID
            var allMedicines = await _unitOfWork.Medicines
                .FindAsync(m => targetHouseholdIds.Contains(m.HouseholdId));
            var medicineIds = allMedicines.Select(m => m.Id).ToList();

            if (!medicineIds.Any())
            {
                return ApiResponse<List<MonthlyUsageStatDto>>.Success(new List<MonthlyUsageStatDto>());
            }

            var startDate = queryParams.StartDate ?? DateTime.Now.AddMonths(-6);
            var endDate = queryParams.EndDate ?? DateTime.Now;

            var monthlyStats = new List<MonthlyUsageStatDto>();
            var current = new DateTime(startDate.Year, startDate.Month, 1);

            while (current <= endDate)
            {
                var monthStart = current;
                var monthEnd = current.AddMonths(1);

                var count = await _unitOfWork.MedUsages
                    .CountAsync(mu => medicineIds.Contains(mu.MedicineId) &&
                                     mu.UsedAt >= monthStart &&
                                     mu.UsedAt < monthEnd);

                monthlyStats.Add(new MonthlyUsageStatDto
                {
                    Month = current.ToString("yyyy-MM"),
                    UsageCount = count
                });

                current = current.AddMonths(1);
            }

            return ApiResponse<List<MonthlyUsageStatDto>>.Success(monthlyStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取趋势统计失败");
            return ApiResponse<List<MonthlyUsageStatDto>>.Error("获取趋势统计失败: " + ex.Message, 500);
        }
    }
}
