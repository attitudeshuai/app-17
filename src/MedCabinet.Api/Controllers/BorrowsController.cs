using MedCabinet.Application.DTOs.MedicineShare;
using MedCabinet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedCabinet.Api.Controllers;

[ApiController]
[Route("api/borrows")]
[Authorize]
[Produces("application/json")]
public class BorrowsController : BaseController
{
    private readonly IBorrowService _borrowService;
    private readonly ILogger<BorrowsController> _logger;

    public BorrowsController(IBorrowService borrowService, ILogger<BorrowsController> logger)
    {
        _borrowService = borrowService;
        _logger = logger;
    }

    [HttpGet("requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBorrowRequests([FromQuery] BorrowQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _borrowService.GetBorrowRequestsAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpGet("requests/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBorrowRequest(int id)
    {
        var userId = GetCurrentUserId();
        var response = await _borrowService.GetBorrowRequestByIdAsync(id, userId);
        return ApiResult(response);
    }

    [HttpPost("requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateBorrowRequest([FromBody] CreateBorrowRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"创建借用申请: 共享ID={request.MedicineShareId}, 药品ID={request.MedicineId}, 数量={request.RequestedQuantity}");
        var response = await _borrowService.CreateBorrowRequestAsync(request, userId);
        return ApiResult(response);
    }

    [HttpPost("requests/{id}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveBorrowRequest(int id, [FromBody] ApproveBorrowRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"批准借用申请: ID={id}");
        var response = await _borrowService.ApproveBorrowRequestAsync(id, request, userId);
        return ApiResult(response);
    }

    [HttpPost("requests/{id}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectBorrowRequest(int id, [FromBody] RejectBorrowRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"拒绝借用申请: ID={id}");
        var response = await _borrowService.RejectBorrowRequestAsync(id, request, userId);
        return ApiResult(response);
    }

    [HttpPost("requests/{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBorrowRequest(int id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"取消借用申请: ID={id}");
        var response = await _borrowService.CancelBorrowRequestAsync(id, userId);
        return ApiResult(response);
    }

    [HttpGet("records")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBorrowRecords([FromQuery] BorrowQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _borrowService.GetBorrowRecordsAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpGet("records/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBorrowRecord(int id)
    {
        var userId = GetCurrentUserId();
        var response = await _borrowService.GetBorrowRecordByIdAsync(id, userId);
        return ApiResult(response);
    }

    [HttpPost("records/{id}/return")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReturnBorrowedMedicine(int id, [FromBody] ReturnBorrowedMedicineDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"归还药品: 记录ID={id}, 数量={request.ReturnedQuantity}");
        var response = await _borrowService.ReturnBorrowedMedicineAsync(id, request, userId);
        return ApiResult(response);
    }

    [HttpPost("check-overdue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckOverdue()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"检查逾期借用: 操作人={userId}");
        var response = await _borrowService.CheckAndSendOverdueRemindersAsync();
        return ApiResult(response);
    }

    [HttpGet("admin/records")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllBorrowRecordsForAdmin([FromQuery] BorrowQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _borrowService.GetAllBorrowRecordsForAdminAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpGet("admin/shares")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllSharesForAdmin([FromQuery] ShareQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _borrowService.GetAllSharesForAdminAsync(queryParams, userId);
        return ApiResult(response);
    }
}
