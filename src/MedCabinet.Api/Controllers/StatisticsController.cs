using MedCabinet.Application.DTOs.Statistics;
using MedCabinet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedCabinet.Api.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
[Produces("application/json")]
public class StatisticsController : BaseController
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(IStatisticsService statisticsService, ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    [HttpGet("overview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOverview([FromQuery] int? householdId = null)
    {
        var userId = GetCurrentUserId();
        var response = await _statisticsService.GetOverviewStatsAsync(userId, householdId);
        return ApiResult(response);
    }

    [HttpGet("trend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTrend([FromQuery] TrendQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _statisticsService.GetTrendStatsAsync(queryParams, userId);
        return ApiResult(response);
    }
}
