using MedCabinet.Application.DTOs.ProcurementSuggestion;
using MedCabinet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedCabinet.Api.Controllers;

[ApiController]
[Route("api/procurement-suggestions")]
[Authorize]
[Produces("application/json")]
public class ProcurementSuggestionsController : BaseController
{
    private readonly IProcurementSuggestionService _procurementService;
    private readonly ILogger<ProcurementSuggestionsController> _logger;

    public ProcurementSuggestionsController(
        IProcurementSuggestionService procurementService,
        ILogger<ProcurementSuggestionsController> logger)
    {
        _procurementService = procurementService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProcurementSuggestions([FromQuery] ProcurementSuggestionQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _procurementService.GetProcurementSuggestionsAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProcurementSuggestion(int id)
    {
        var userId = GetCurrentUserId();
        var response = await _procurementService.GetProcurementSuggestionByIdAsync(id, userId);
        return ApiResult(response);
    }

    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GenerateProcurementSuggestions([FromBody] GenerateProcurementSuggestionsRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"生成采购建议请求: 家庭ID={request.HouseholdId}");
        var response = await _procurementService.GenerateProcurementSuggestionsAsync(request, userId);
        return ApiResult(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProcurementSuggestion([FromBody] CreateProcurementSuggestionRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"手动创建采购建议: 药品ID={request.MedicineId}");
        var response = await _procurementService.CreateProcurementSuggestionAsync(request, userId);
        return ApiResult(response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProcurementSuggestion(int id, [FromBody] UpdateProcurementSuggestionRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"更新采购建议请求: ID={id}");
        var response = await _procurementService.UpdateProcurementSuggestionAsync(id, request, userId);
        return ApiResult(response);
    }

    [HttpPatch("{id}/mark")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkProcurementSuggestion(int id, [FromBody] MarkProcurementSuggestionRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"标记采购建议请求: ID={id}, Status={request.Status}");
        var response = await _procurementService.MarkProcurementSuggestionAsync(id, request, userId);
        return ApiResult(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProcurementSuggestion(int id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"删除采购建议请求: ID={id}");
        var response = await _procurementService.DeleteProcurementSuggestionAsync(id, userId);
        return ApiResult(response);
    }

    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProcurementStats([FromQuery] int? householdId = null)
    {
        var userId = GetCurrentUserId();
        var response = await _procurementService.GetProcurementStatsAsync(householdId, userId);
        return ApiResult(response);
    }

    [HttpGet("export/medicine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportByMedicine([FromQuery] ProcurementExportQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _procurementService.ExportByMedicineAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpGet("export/member")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportByMember([FromQuery] ProcurementExportQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _procurementService.ExportByMemberAsync(queryParams, userId);
        return ApiResult(response);
    }
}
