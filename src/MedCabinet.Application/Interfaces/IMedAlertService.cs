using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.MedAlert;

namespace MedCabinet.Application.Interfaces;

public interface IMedAlertService
{
    Task<ApiResponse<PagedResult<MedAlertDto>>> GetMedAlertsAsync(MedAlertQueryParamsDto queryParams, int userId);
    Task<ApiResponse<MedAlertDto>> GetMedAlertByIdAsync(int id, int userId);
    Task<ApiResponse<MedAlertDto>> CreateMedAlertAsync(CreateMedAlertRequestDto request, int userId);
    Task<ApiResponse<MedAlertDto>> UpdateMedAlertAsync(int id, UpdateMedAlertRequestDto request, int userId);
    Task<ApiResponse> DeleteMedAlertAsync(int id, int userId);
    Task<ApiResponse<PagedResult<MedAlertDto>>> GetMyMedAlertsAsync(MedAlertQueryParamsDto queryParams, int userId);
}
