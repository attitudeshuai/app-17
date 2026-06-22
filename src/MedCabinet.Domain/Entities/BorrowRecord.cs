using MedCabinet.Domain.Enums;

namespace MedCabinet.Domain.Entities;

public class BorrowRecord
{
    public int Id { get; set; }
    public int MedicineShareId { get; set; }
    public int BorrowRequestId { get; set; }
    public int MedicineId { get; set; }
    public int LenderHouseholdId { get; set; }
    public int BorrowerHouseholdId { get; set; }
    public int BorrowerUserId { get; set; }
    public int BorrowedQuantity { get; set; }
    public DateTime BorrowedAt { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public int? ReturnedQuantity { get; set; }
    public BorrowRecordStatus Status { get; set; }
    public string? Notes { get; set; }
    public bool ReminderSent { get; set; }

    public virtual MedicineShare? MedicineShare { get; set; }
    public virtual BorrowRequest? BorrowRequest { get; set; }
    public virtual Medicine? Medicine { get; set; }
    public virtual Household? LenderHousehold { get; set; }
    public virtual Household? BorrowerHousehold { get; set; }
    public virtual User? BorrowerUser { get; set; }
}
