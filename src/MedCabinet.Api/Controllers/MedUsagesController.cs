using MedCabinet.Application.DTOs.MedUsage;
using MedCabinet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedCabinet.Api.Controllers;

[ApiController]
[Route("api/medusages")]
[Authorize]
[Produces("application/json")]
public class MedUsagesController : BaseController
{
    private readonly IMedUsageService _medUsageService;
    private readonly ILogger<MedUsagesController> _logger;

    public MedUsagesController(IMedUsageService medUsageService, ILogger<MedUsagesController> logger)
    {
        _medUsageService = medUsageService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMedUsages([FromQuery] MedUsageQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _medUsageService.GetMedUsagesAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMedUsage(int id)
    {
        var userId = GetCurrentUserId();
        var response = await _medUsageService.GetMedUsageByIdAsync(id, userId);
        return ApiResult(response);
    }

    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyMedUsages([FromQuery] MedUsageQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _medUsageService.GetMyMedUsagesAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateMedUsage([FromBody] CreateMedUsageRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"创建用药记录请求: 药品ID={request.MedicineId}");
        var response = await _medUsageService.CreateMedUsageAsync(request, userId);
        return ApiResult(response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMedUsage(int id, [FromBody] UpdateMedUsageRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"更新用药记录请求: ID={id}");
        var response = await _medUsageService.UpdateMedUsageAsync(id, request, userId);
        return ApiResult(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMedUsage(int id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"删除用药记录请求: ID={id}");
        var response = await _medUsageService.DeleteMedUsageAsync(id, userId);
        return ApiResult(response);
    }
}
