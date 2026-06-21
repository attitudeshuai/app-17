using MedCabinet.Application.DTOs.Common;
using MedCabinet.Application.DTOs.Medicine;
using MedCabinet.Domain.Enums;

namespace MedCabinet.Application.Interfaces;

public interface IMedicineService
{
    Task<ApiResponse<PagedResult<MedicineDto>>> GetMedicinesAsync(MedicineQueryParamsDto queryParams, int userId);
    Task<ApiResponse<MedicineDto>> GetMedicineByIdAsync(int id, int userId);
    Task<ApiResponse<MedicineDto>> CreateMedicineAsync(CreateMedicineRequestDto request, int userId);
    Task<ApiResponse<MedicineDto>> UpdateMedicineAsync(int id, UpdateMedicineRequestDto request, int userId);
    Task<ApiResponse> DeleteMedicineAsync(int id, int userId);
    Task<ApiResponse<MedicineDto>> UpdateMedicineStatusAsync(int id, MedicineStatus status, int userId);
}
