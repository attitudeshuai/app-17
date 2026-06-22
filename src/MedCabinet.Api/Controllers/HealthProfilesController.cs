using MedCabinet.Application.DTOs.HealthProfile;
using MedCabinet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedCabinet.Api.Controllers;

[ApiController]
[Route("api/healthprofiles")]
[Authorize]
[Produces("application/json")]
public class HealthProfilesController : BaseController
{
    private readonly IHealthProfileService _healthProfileService;
    private readonly ILogger<HealthProfilesController> _logger;

    public HealthProfilesController(IHealthProfileService healthProfileService, ILogger<HealthProfilesController> logger)
    {
        _healthProfileService = healthProfileService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetHealthProfiles([FromQuery] HealthProfileQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _healthProfileService.GetHealthProfilesAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHealthProfile(int id)
    {
        var userId = GetCurrentUserId();
        var response = await _healthProfileService.GetHealthProfileByIdAsync(id, userId);
        return ApiResult(response);
    }

    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyHealthProfile([FromQuery] int householdId)
    {
        var userId = GetCurrentUserId();
        var response = await _healthProfileService.GetMyHealthProfileAsync(householdId, userId);
        return ApiResult(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateHealthProfile([FromBody] CreateHealthProfileRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"创建健康档案请求: 用户ID={request.UserId}, 家庭ID={request.HouseholdId}");
        var response = await _healthProfileService.CreateHealthProfileAsync(request, userId);
        return ApiResult(response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateHealthProfile(int id, [FromBody] UpdateHealthProfileRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"更新健康档案请求: ID={id}");
        var response = await _healthProfileService.UpdateHealthProfileAsync(id, request, userId);
        return ApiResult(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteHealthProfile(int id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"删除健康档案请求: ID={id}");
        var response = await _healthProfileService.DeleteHealthProfileAsync(id, userId);
        return ApiResult(response);
    }

    [HttpGet("{id}/audit-logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditLogs(int id, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var response = await _healthProfileService.GetAuditLogsAsync(id, pageIndex, pageSize, userId);
        return ApiResult(response);
    }

    [HttpGet("check-contraindications")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckMedicineContraindications([FromQuery] int userId, [FromQuery] int medicineId)
    {
        var currentUserId = GetCurrentUserId();
        var response = await _healthProfileService.CheckMedicineContraindicationsAsync(userId, medicineId, currentUserId);
        return ApiResult(response);
    }

    [HttpGet("{id}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportHealthReport(int id)
    {
        var userId = GetCurrentUserId();
        var response = await _healthProfileService.ExportHealthReportAsync(id, userId);
        return ApiResult(response);
    }

    [HttpGet("{id}/export-csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportHealthReportCsv(int id)
    {
        var userId = GetCurrentUserId();
        var response = await _healthProfileService.ExportHealthReportCsvAsync(id, userId);
        if (response.Code != 200 || response.Data == null)
        {
            return ApiResult(response);
        }

        return File(response.Data.Content, "text/csv; charset=utf-8", response.Data.FileName);
    }
}
