using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.MedUsage;

namespace MedCabinet.Application.Interfaces;

public interface IMedUsageService
{
    Task<ApiResponse<PagedResult<MedUsageDto>>> GetMedUsagesAsync(MedUsageQueryParamsDto queryParams, int userId);
    Task<ApiResponse<MedUsageDto>> GetMedUsageByIdAsync(int id, int userId);
    Task<ApiResponse<MedUsageDto>> CreateMedUsageAsync(CreateMedUsageRequestDto request, int userId);
    Task<ApiResponse<MedUsageDto>> UpdateMedUsageAsync(int id, UpdateMedUsageRequestDto request, int userId);
    Task<ApiResponse> DeleteMedUsageAsync(int id, int userId);
    Task<ApiResponse<PagedResult<MedUsageDto>>> GetMyMedUsagesAsync(MedUsageQueryParamsDto queryParams, int userId);
}
