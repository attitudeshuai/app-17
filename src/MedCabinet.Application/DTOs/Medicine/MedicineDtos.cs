using MedCabinet.Application.DTOs.HealthProfile;
using MedCabinet.Domain.Enums;

namespace MedCabinet.Application.DTOs.Medicine;

public class MedicineDto
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Indication { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int StockQuantity { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public string? Contraindications { get; set; }
    public string? PhotoUrl { get; set; }
    public MedicineStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DaysUntilExpiry { get; set; }
    public List<ContraindicationWarningDto>? PersonalContraindicationWarnings { get; set; }
}

public class CreateMedicineRequestDto
{
    public int HouseholdId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Indication { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int StockQuantity { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public string? Contraindications { get; set; }
    public string? PhotoUrl { get; set; }
}

public class UpdateMedicineRequestDto
{
    public string? Name { get; set; }
    public string? Category { get; set; }
    public string? Indication { get; set; }
    public string? Dosage { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? StockQuantity { get; set; }
    public string? StorageLocation { get; set; }
    public string? Contraindications { get; set; }
    public string? PhotoUrl { get; set; }
}

public class UpdateMedicineStatusRequestDto
{
    public MedicineStatus Status { get; set; }
}

public class MedicineQueryParamsDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? HouseholdId { get; set; }
    public MedicineStatus? Status { get; set; }
    public string? Category { get; set; }
    public string? SearchKeyword { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}
