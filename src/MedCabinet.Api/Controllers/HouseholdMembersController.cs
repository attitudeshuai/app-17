using MedCabinet.Application.DTOs.HouseholdMember;
using MedCabinet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedCabinet.Api.Controllers;

[ApiController]
[Route("api/householdmembers")]
[Authorize]
[Produces("application/json")]
public class HouseholdMembersController : BaseController
{
    private readonly IHouseholdMemberService _householdMemberService;
    private readonly ILogger<HouseholdMembersController> _logger;

    public HouseholdMembersController(IHouseholdMemberService householdMemberService, ILogger<HouseholdMembersController> logger)
    {
        _householdMemberService = householdMemberService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHouseholdMembers([FromQuery] HouseholdMemberQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _householdMemberService.GetHouseholdMembersAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHouseholdMember(int id)
    {
        var userId = GetCurrentUserId();
        var response = await _householdMemberService.GetHouseholdMemberByIdAsync(id, userId);
        return ApiResult(response);
    }

    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyMembers([FromQuery] HouseholdMemberQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _householdMemberService.GetMyMembersAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddMember([FromBody] CreateHouseholdMemberRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"添加家庭成员请求: 家庭ID={request.HouseholdId}, 用户ID={request.UserId}");
        var response = await _householdMemberService.AddMemberAsync(request, userId);
        return ApiResult(response);
    }

    [HttpPost("join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> JoinHousehold([FromBody] JoinHouseholdRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"用户加入家庭请求: 邀请码={request.InviteCode}");
        var response = await _householdMemberService.JoinHouseholdByInviteCodeAsync(request, userId);
        return ApiResult(response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMember(int id, [FromBody] UpdateHouseholdMemberRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"更新家庭成员请求: ID={id}");
        var response = await _householdMemberService.UpdateMemberAsync(id, request, userId);
        return ApiResult(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(int id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"删除家庭成员请求: ID={id}");
        var response = await _householdMemberService.RemoveMemberAsync(id, userId);
        return ApiResult(response);
    }
}
