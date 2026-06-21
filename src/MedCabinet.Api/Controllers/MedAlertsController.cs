using MedCabinet.Application.DTOs.MedAlert;
using MedCabinet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedCabinet.Api.Controllers;

[ApiController]
[Route("api/medalerts")]
[Authorize]
[Produces("application/json")]
public class MedAlertsController : BaseController
{
    private readonly IMedAlertService _medAlertService;
    private readonly ILogger<MedAlertsController> _logger;

    public MedAlertsController(IMedAlertService medAlertService, ILogger<MedAlertsController> logger)
    {
        _medAlertService = medAlertService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMedAlerts([FromQuery] MedAlertQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _medAlertService.GetMedAlertsAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMedAlert(int id)
    {
        var userId = GetCurrentUserId();
        var response = await _medAlertService.GetMedAlertByIdAsync(id, userId);
        return ApiResult(response);
    }

    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyMedAlerts([FromQuery] MedAlertQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _medAlertService.GetMyMedAlertsAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateMedAlert([FromBody] CreateMedAlertRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"创建提醒请求: 药品ID={request.MedicineId}");
        var response = await _medAlertService.CreateMedAlertAsync(request, userId);
        return ApiResult(response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMedAlert(int id, [FromBody] UpdateMedAlertRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"更新提醒请求: ID={id}");
        var response = await _medAlertService.UpdateMedAlertAsync(id, request, userId);
        return ApiResult(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMedAlert(int id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"删除提醒请求: ID={id}");
        var response = await _medAlertService.DeleteMedAlertAsync(id, userId);
        return ApiResult(response);
    }
}
