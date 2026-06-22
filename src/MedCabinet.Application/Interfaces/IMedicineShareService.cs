using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.MedicineShare;

namespace MedCabinet.Application.Interfaces;

public interface IMedicineShareService
{
    Task<ApiResponse<PagedResult<MedicineShareDto>>> GetSharesAsync(ShareQueryParamsDto queryParams, int userId);
    Task<ApiResponse<MedicineShareDto>> GetShareByIdAsync(int id, int userId);
    Task<ApiResponse<MedicineShareDto>> CreateShareAsync(CreateShareRequestDto request, int userId);
    Task<ApiResponse<MedicineShareDto>> AcceptShareByCodeAsync(AcceptShareByCodeRequestDto request, int userId);
    Task<ApiResponse> RevokeShareAsync(int id, int userId);
    Task<ApiResponse<MedicineShareDto>> UpdateSharedMedicinesAsync(int shareId, UpdateSharedMedicinesRequestDto request, int userId);
    Task<ApiResponse<PagedResult<SharedMedicineDto>>> GetSharedMedicinesForBorrowerAsync(int shareId, int userId);
}
