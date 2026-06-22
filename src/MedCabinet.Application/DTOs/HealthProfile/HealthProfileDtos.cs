using MedCabinet.Domain.Enums;

namespace MedCabinet.Application.DTOs.HealthProfile;

public class HealthProfileDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int HouseholdId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public BloodType BloodType { get; set; } = BloodType.Unknown;
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? Medications { get; set; }
    public string? MedicalHistory { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string HouseholdName { get; set; } = string.Empty;
}

public class CreateHealthProfileRequestDto
{
    public int UserId { get; set; }
    public int HouseholdId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public BloodType? BloodType { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? Medications { get; set; }
    public string? MedicalHistory { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Notes { get; set; }
}

public class UpdateHealthProfileRequestDto
{
    public string? FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public BloodType? BloodType { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? Medications { get; set; }
    public string? MedicalHistory { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Notes { get; set; }
}

public class HealthProfileQueryParamsDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? HouseholdId { get; set; }
    public int? UserId { get; set; }
    public string? SearchKeyword { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}

public class HealthProfileAuditLogDto
{
    public int Id { get; set; }
    public int HealthProfileId { get; set; }
    public int ModifiedByUserId { get; set; }
    public string ModifiedByUsername { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ModifiedAt { get; set; }
}

public class ContraindicationWarningDto
{
    public string Level { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
}

public class MedicineContraindicationCheckResultDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public bool HasWarnings { get; set; }
    public List<ContraindicationWarningDto> Warnings { get; set; } = new();
}

public class HealthReportExportDto
{
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public string BloodType { get; set; } = string.Empty;
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string Allergies { get; set; } = string.Empty;
    public string ChronicDiseases { get; set; } = string.Empty;
    public string CurrentMedications { get; set; } = string.Empty;
    public string MedicalHistory { get; set; } = string.Empty;
    public string EmergencyContact { get; set; } = string.Empty;
    public string EmergencyPhone { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime ReportGeneratedAt { get; set; }
}
