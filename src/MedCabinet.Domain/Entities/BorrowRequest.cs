using MedCabinet.Domain.Enums;

namespace MedCabinet.Domain.Entities;

public class BorrowRequest
{
    public int Id { get; set; }
    public int MedicineShareId { get; set; }
    public int MedicineId { get; set; }
    public int RequesterUserId { get; set; }
    public int RequestedQuantity { get; set; }
    public string? Purpose { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public BorrowRequestStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual MedicineShare? MedicineShare { get; set; }
    public virtual Medicine? Medicine { get; set; }
    public virtual User? RequesterUser { get; set; }
    public virtual User? ApprovedByUser { get; set; }
    public virtual BorrowRecord? BorrowRecord { get; set; }
}
