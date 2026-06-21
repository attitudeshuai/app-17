using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.Statistics;

namespace MedCabinet.Application.Interfaces;

public interface IStatisticsService
{
    Task<ApiResponse<OverviewStatsDto>> GetOverviewStatsAsync(int userId, int? householdId = null);
    Task<ApiResponse<List<MonthlyUsageStatDto>>> GetTrendStatsAsync(TrendQueryParamsDto queryParams, int userId);
}
