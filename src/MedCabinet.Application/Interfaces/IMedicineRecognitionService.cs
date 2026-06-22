using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.Medicine;
using MedCabinet.Application.DTOs.MedicineRecognition;

namespace MedCabinet.Application.Interfaces;

public interface IMedicineRecognitionService
{
    Task<ApiResponse<MedicineRecognitionResultDto>> RecognizeFromImageAsync(
        Stream imageStream, string fileName, string contentType, long fileSize, int? householdId, int userId);

    Task<ApiResponse<MatchedMedicineDto>> MatchMedicineAsync(string medicineName, string? specification, int? householdId, int userId);

    Task<ApiResponse<MedicineRecognitionRecordDto>> GetRecognitionRecordAsync(int recordId, int userId);

    Task<ApiResponse<MedicineDto>> ConfirmAndSaveAsync(ConfirmRecognitionRequestDto request, int userId);

    Task<ApiResponse<PagedResult<MedicineRecognitionRecordDto>>> GetRecognitionRecordsAsync(RecognitionRecordQueryParamsDto queryParams, int userId);
}
