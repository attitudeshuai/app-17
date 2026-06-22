using MedCabinet.Application.DTOs.HealthProfile;

namespace MedCabinet.Application.DTOs.MedUsage;

public class MedUsageDto
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public int UserId { get; set; }
    public string UsedBy { get; set; } = string.Empty;
    public int UsedQuantity { get; set; }
    public DateTime UsedAt { get; set; }
    public string? SymptomNote { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public List<ContraindicationWarningDto>? ContraindicationWarnings { get; set; }
}

public class CreateMedUsageRequestDto
{
    public int MedicineId { get; set; }
    public string UsedBy { get; set; } = string.Empty;
    public int UsedQuantity { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? SymptomNote { get; set; }
}

public class UpdateMedUsageRequestDto
{
    public string? UsedBy { get; set; }
    public int? UsedQuantity { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? SymptomNote { get; set; }
}

public class MedUsageQueryParamsDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? MedicineId { get; set; }
    public int? UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchKeyword { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}
