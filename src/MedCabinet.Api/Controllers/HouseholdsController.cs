using MedCabinet.Application.DTOs.Household;
using MedCabinet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedCabinet.Api.Controllers;

[ApiController]
[Route("api/households")]
[Authorize]
[Produces("application/json")]
public class HouseholdsController : BaseController
{
    private readonly IHouseholdService _householdService;
    private readonly ILogger<HouseholdsController> _logger;

    public HouseholdsController(IHouseholdService householdService, ILogger<HouseholdsController> logger)
    {
        _householdService = householdService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHouseholds([FromQuery] HouseholdQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _householdService.GetHouseholdsAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHousehold(int id)
    {
        var userId = GetCurrentUserId();
        var response = await _householdService.GetHouseholdByIdAsync(id, userId);
        return ApiResult(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateHousehold([FromBody] CreateHouseholdRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"创建家庭请求: {request.Name}, 用户ID: {userId}");
        var response = await _householdService.CreateHouseholdAsync(request, userId);
        return ApiResult(response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateHousehold(int id, [FromBody] UpdateHouseholdRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"更新家庭请求: ID={id}, 用户ID: {userId}");
        var response = await _householdService.UpdateHouseholdAsync(id, request, userId);
        return ApiResult(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteHousehold(int id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"删除家庭请求: ID={id}, 用户ID: {userId}");
        var response = await _householdService.DeleteHouseholdAsync(id, userId);
        return ApiResult(response);
    }
}
