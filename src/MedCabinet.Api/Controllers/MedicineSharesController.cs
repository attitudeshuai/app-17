using MedCabinet.Application.DTOs.MedicineShare;
using MedCabinet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedCabinet.Api.Controllers;

[ApiController]
[Route("api/medicine-shares")]
[Authorize]
[Produces("application/json")]
public class MedicineSharesController : BaseController
{
    private readonly IMedicineShareService _shareService;
    private readonly ILogger<MedicineSharesController> _logger;

    public MedicineSharesController(IMedicineShareService shareService, ILogger<MedicineSharesController> logger)
    {
        _shareService = shareService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetShares([FromQuery] ShareQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _shareService.GetSharesAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShare(int id)
    {
        var userId = GetCurrentUserId();
        var response = await _shareService.GetShareByIdAsync(id, userId);
        return ApiResult(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateShare([FromBody] CreateShareRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"创建共享关系请求: 借出家庭={request.LenderHouseholdId}, 借入家庭={request.BorrowerHouseholdId}");
        var response = await _shareService.CreateShareAsync(request, userId);
        return ApiResult(response);
    }

    [HttpPost("accept-by-code")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptShareByCode([FromBody] AcceptShareByCodeRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"通过邀请码接受共享: 邀请码={request.InviteCode}");
        var response = await _shareService.AcceptShareByCodeAsync(request, userId);
        return ApiResult(response);
    }

    [HttpPost("{id}/revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeShare(int id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"解除共享关系: ID={id}");
        var response = await _shareService.RevokeShareAsync(id, userId);
        return ApiResult(response);
    }

    [HttpPut("{id}/medicines")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSharedMedicines(int id, [FromBody] UpdateSharedMedicinesRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"更新共享药品: 共享ID={id}, 药品数量={request.MedicineIds.Count}");
        var response = await _shareService.UpdateSharedMedicinesAsync(id, request, userId);
        return ApiResult(response);
    }

    [HttpGet("{id}/shared-medicines")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSharedMedicines(int id)
    {
        var userId = GetCurrentUserId();
        var response = await _shareService.GetSharedMedicinesForBorrowerAsync(id, userId);
        return ApiResult(response);
    }
}
