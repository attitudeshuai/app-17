using MedCabinet.Domain.Enums;

namespace MedCabinet.Domain.Entities;

public class Medicine
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string Indication { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int StockQuantity { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public string? Contraindications { get; set; }
    public string? PhotoUrl { get; set; }
    public MedicineStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Household? Household { get; set; }
    public virtual ICollection<MedUsage> MedUsages { get; set; } = new List<MedUsage>();
    public virtual ICollection<MedAlert> MedAlerts { get; set; } = new List<MedAlert>();
    public virtual ICollection<ProcurementSuggestion> ProcurementSuggestions { get; set; } = new List<ProcurementSuggestion>();
}
