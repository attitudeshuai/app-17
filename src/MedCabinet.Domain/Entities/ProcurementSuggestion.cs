using MedCabinet.Domain.Enums;

namespace MedCabinet.Domain.Entities;

public class ProcurementSuggestion
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public int MedicineId { get; set; }
    public int? UserId { get; set; }
    public int SuggestedQuantity { get; set; }
    public DateTime SuggestedPurchaseDate { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public ProcurementStatus Status { get; set; }
    public decimal UsageFrequency { get; set; }
    public int CurrentStock { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime? PurchasedAt { get; set; }
    public int? PurchasedQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Household? Household { get; set; }
    public Medicine? Medicine { get; set; }
    public User? User { get; set; }
}
