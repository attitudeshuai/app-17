namespace MedCabinet.Domain.Entities;

public class MedUsage
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public int UserId { get; set; }
    public string UsedBy { get; set; } = string.Empty;
    public int UsedQuantity { get; set; }
    public DateTime UsedAt { get; set; }
    public string? SymptomNote { get; set; }

    public virtual Medicine? Medicine { get; set; }
    public virtual User? User { get; set; }
}
