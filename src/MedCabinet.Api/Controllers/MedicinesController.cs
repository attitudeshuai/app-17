using MedCabinet.Application.DTOs.Medicine;
using MedCabinet.Application.Interfaces;
using MedCabinet.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedCabinet.Api.Controllers;

[ApiController]
[Route("api/medicines")]
[Authorize]
[Produces("application/json")]
public class MedicinesController : BaseController
{
    private readonly IMedicineService _medicineService;
    private readonly ILogger<MedicinesController> _logger;

    public MedicinesController(IMedicineService medicineService, ILogger<MedicinesController> logger)
    {
        _medicineService = medicineService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMedicines([FromQuery] MedicineQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _medicineService.GetMedicinesAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMedicine(int id)
    {
        var userId = GetCurrentUserId();
        var response = await _medicineService.GetMedicineByIdAsync(id, userId);
        return ApiResult(response);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateMedicine([FromBody] CreateMedicineRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"创建药品请求: {request.Name}");
        var response = await _medicineService.CreateMedicineAsync(request, userId);
        return ApiResult(response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMedicine(int id, [FromBody] UpdateMedicineRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"更新药品请求: ID={id}");
        var response = await _medicineService.UpdateMedicineAsync(id, request, userId);
        return ApiResult(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMedicine(int id)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"删除药品请求: ID={id}");
        var response = await _medicineService.DeleteMedicineAsync(id, userId);
        return ApiResult(response);
    }

    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMedicineStatus(int id, [FromBody] UpdateMedicineStatusRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"更新药品状态请求: ID={id}, Status={request.Status}");
        var response = await _medicineService.UpdateMedicineStatusAsync(id, request.Status, userId);
        return ApiResult(response);
    }
}
