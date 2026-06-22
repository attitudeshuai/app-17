using MedCabinet.Domain.Enums;

namespace MedCabinet.Domain.Entities;

public class MedicineShare
{
    public int Id { get; set; }
    public int LenderHouseholdId { get; set; }
    public int BorrowerHouseholdId { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public ShareStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public int? RevokedByUserId { get; set; }

    public virtual Household? LenderHousehold { get; set; }
    public virtual Household? BorrowerHousehold { get; set; }
    public virtual User? CreatedByUser { get; set; }
    public virtual User? RevokedByUser { get; set; }
    public virtual ICollection<SharedMedicine> SharedMedicines { get; set; } = new List<SharedMedicine>();
    public virtual ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();
    public virtual ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
}
