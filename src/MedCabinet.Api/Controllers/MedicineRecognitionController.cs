using MedCabinet.Application.DTOs.MedicineRecognition;
using MedCabinet.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedCabinet.Api.Controllers;

[ApiController]
[Route("api/medicine-recognition")]
[Authorize]
[Produces("application/json")]
public class MedicineRecognitionController : BaseController
{
    private readonly IMedicineRecognitionService _recognitionService;
    private readonly ILogger<MedicineRecognitionController> _logger;

    public MedicineRecognitionController(
        IMedicineRecognitionService recognitionService,
        ILogger<MedicineRecognitionController> logger)
    {
        _recognitionService = recognitionService;
        _logger = logger;
    }

    [HttpPost("recognize")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> RecognizeFromImage(
        IFormFile imageFile,
        [FromQuery] int? householdId)
    {
        var userId = GetCurrentUserId();

        if (imageFile == null)
        {
            return BadRequest(new { Code = 400, Message = "请上传图片文件" });
        }

        _logger.LogInformation($"药品图片识别请求: 用户={userId}, 家庭={householdId}, 文件={imageFile.FileName}");

        using var stream = imageFile.OpenReadStream();
        var response = await _recognitionService.RecognizeFromImageAsync(
            stream,
            imageFile.FileName,
            imageFile.ContentType,
            imageFile.Length,
            householdId,
            userId);

        return ApiResult(response);
    }

    [HttpPost("match")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MatchMedicine(
        [FromQuery] string medicineName,
        [FromQuery] string? specification,
        [FromQuery] int? householdId)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"药品匹配请求: 用户={userId}, 药品名={medicineName}");

        var response = await _recognitionService.MatchMedicineAsync(medicineName, specification, householdId, userId);
        return ApiResult(response);
    }

    [HttpGet("{recordId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecognitionRecord(int recordId)
    {
        var userId = GetCurrentUserId();
        var response = await _recognitionService.GetRecognitionRecordAsync(recordId, userId);
        return ApiResult(response);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecognitionRecords(
        [FromQuery] RecognitionRecordQueryParamsDto queryParams)
    {
        var userId = GetCurrentUserId();
        var response = await _recognitionService.GetRecognitionRecordsAsync(queryParams, userId);
        return ApiResult(response);
    }

    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmAndSave([FromBody] ConfirmRecognitionRequestDto request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation($"确认保存识别结果: 用户={userId}, 记录ID={request.RecordId}, 药品名={request.Name}");

        var response = await _recognitionService.ConfirmAndSaveAsync(request, userId);
        return ApiResult(response);
    }
}
