using MedCabinet.Domain.Enums;

namespace MedCabinet.Application.DTOs.MedicineShare;

public class MedicineShareDto
{
    public int Id { get; set; }
    public int LenderHouseholdId { get; set; }
    public string LenderHouseholdName { get; set; } = string.Empty;
    public int BorrowerHouseholdId { get; set; }
    public string BorrowerHouseholdName { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public ShareStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public int? RevokedByUserId { get; set; }
    public List<SharedMedicineDto> SharedMedicines { get; set; } = new();
}

public class SharedMedicineDto
{
    public int Id { get; set; }
    public int MedicineShareId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string MedicineCategory { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateShareRequestDto
{
    public int LenderHouseholdId { get; set; }
    public int BorrowerHouseholdId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
    public List<int> MedicineIds { get; set; } = new();
}

public class AcceptShareByCodeRequestDto
{
    public string InviteCode { get; set; } = string.Empty;
    public int BorrowerHouseholdId { get; set; }
}

public class UpdateSharedMedicinesRequestDto
{
    public List<int> MedicineIds { get; set; } = new();
}

public class ShareQueryParamsDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? HouseholdId { get; set; }
    public ShareStatus? Status { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}
