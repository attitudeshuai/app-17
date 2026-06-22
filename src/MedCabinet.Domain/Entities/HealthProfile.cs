using MedCabinet.Domain.Enums;

namespace MedCabinet.Domain.Entities;

public class HealthProfile
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

    public virtual User? User { get; set; }
    public virtual Household? Household { get; set; }
    public virtual ICollection<HealthProfileAuditLog> AuditLogs { get; set; } = new List<HealthProfileAuditLog>();
}
