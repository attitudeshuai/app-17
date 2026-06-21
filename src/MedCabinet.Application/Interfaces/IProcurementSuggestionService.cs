using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.ProcurementSuggestion;

namespace MedCabinet.Application.Interfaces;

public interface IProcurementSuggestionService
{
    Task<ApiResponse<PagedResult<ProcurementSuggestionDto>>> GetProcurementSuggestionsAsync(ProcurementSuggestionQueryParamsDto queryParams, int userId);
    Task<ApiResponse<ProcurementSuggestionDto>> GetProcurementSuggestionByIdAsync(int id, int userId);
    Task<ApiResponse<List<ProcurementSuggestionDto>>> GenerateProcurementSuggestionsAsync(GenerateProcurementSuggestionsRequestDto request, int userId);
    Task<ApiResponse<ProcurementSuggestionDto>> CreateProcurementSuggestionAsync(CreateProcurementSuggestionRequestDto request, int userId);
    Task<ApiResponse<ProcurementSuggestionDto>> UpdateProcurementSuggestionAsync(int id, UpdateProcurementSuggestionRequestDto request, int userId);
    Task<ApiResponse<ProcurementSuggestionDto>> MarkProcurementSuggestionAsync(int id, MarkProcurementSuggestionRequestDto request, int userId);
    Task<ApiResponse> DeleteProcurementSuggestionAsync(int id, int userId);
    Task<ApiResponse<ProcurementStatsDto>> GetProcurementStatsAsync(int? householdId, int userId);
    Task<ApiResponse<CsvExportResult>> ExportByMedicineAsync(ProcurementExportQueryParamsDto queryParams, int userId);
    Task<ApiResponse<CsvExportResult>> ExportByMemberAsync(ProcurementExportQueryParamsDto queryParams, int userId);
}
