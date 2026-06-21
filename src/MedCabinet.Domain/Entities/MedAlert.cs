using MedCabinet.Domain.Enums;

namespace MedCabinet.Domain.Entities;

public class MedAlert
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public int UserId { get; set; }
    public AlertType AlertType { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Medicine? Medicine { get; set; }
    public virtual User? User { get; set; }
}
