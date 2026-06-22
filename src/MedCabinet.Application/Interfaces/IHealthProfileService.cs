using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.HealthProfile;
using MedCabinet.Application.DTOs.ProcurementSuggestion;

namespace MedCabinet.Application.Interfaces;

public interface IHealthProfileService
{
    Task<ApiResponse<PagedResult<HealthProfileDto>>> GetHealthProfilesAsync(
        HealthProfileQueryParamsDto queryParams, int currentUserId);

    Task<ApiResponse<HealthProfileDto>> GetHealthProfileByIdAsync(int id, int currentUserId);

    Task<ApiResponse<HealthProfileDto>> GetMyHealthProfileAsync(int householdId, int currentUserId);

    Task<ApiResponse<HealthProfileDto>> CreateHealthProfileAsync(
        CreateHealthProfileRequestDto request, int currentUserId);

    Task<ApiResponse<HealthProfileDto>> UpdateHealthProfileAsync(
        int id, UpdateHealthProfileRequestDto request, int currentUserId);

    Task<ApiResponse> DeleteHealthProfileAsync(int id, int currentUserId);

    Task<ApiResponse<PagedResult<HealthProfileAuditLogDto>>> GetAuditLogsAsync(
        int healthProfileId, int pageIndex, int pageSize, int currentUserId);

    Task<ApiResponse<MedicineContraindicationCheckResultDto>> CheckMedicineContraindicationsAsync(
        int userId, int medicineId, int currentUserId);

    Task<ApiResponse<HealthReportExportDto>> ExportHealthReportAsync(int id, int currentUserId);

    Task<ApiResponse<CsvExportResult>> ExportHealthReportCsvAsync(int id, int currentUserId);
}
