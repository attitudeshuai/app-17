using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.Household;

namespace MedCabinet.Application.Interfaces;

public interface IHouseholdService
{
    Task<ApiResponse<PagedResult<HouseholdDto>>> GetHouseholdsAsync(HouseholdQueryParamsDto queryParams, int userId);
    Task<ApiResponse<HouseholdDto>> GetHouseholdByIdAsync(int id, int userId);
    Task<ApiResponse<HouseholdDto>> CreateHouseholdAsync(CreateHouseholdRequestDto request, int userId);
    Task<ApiResponse<HouseholdDto>> UpdateHouseholdAsync(int id, UpdateHouseholdRequestDto request, int userId);
    Task<ApiResponse> DeleteHouseholdAsync(int id, int userId);
    Task<bool> HasPermissionAsync(int householdId, int userId, params string[] allowedRoles);
}
