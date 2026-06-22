namespace MedCabinet.Domain.Entities;

public class HealthProfileAuditLog
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

    public virtual HealthProfile? HealthProfile { get; set; }
}
