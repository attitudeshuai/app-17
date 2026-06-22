namespace MedCabinet.Domain.Entities;

public class SharedMedicine
{
    public int Id { get; set; }
    public int MedicineShareId { get; set; }
    public int MedicineId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual MedicineShare? MedicineShare { get; set; }
    public virtual Medicine? Medicine { get; set; }
}
