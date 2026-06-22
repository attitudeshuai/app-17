using MedCabinet.Domain.Enums;

namespace MedCabinet.Application.DTOs.MedicineShare;

public class BorrowRequestDto
{
    public int Id { get; set; }
    public int MedicineShareId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int RequesterUserId { get; set; }
    public string RequesterUsername { get; set; } = string.Empty;
    public int LenderHouseholdId { get; set; }
    public string LenderHouseholdName { get; set; } = string.Empty;
    public int BorrowerHouseholdId { get; set; }
    public string BorrowerHouseholdName { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public string? Purpose { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public BorrowRequestStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUsername { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateBorrowRequestDto
{
    public int MedicineShareId { get; set; }
    public int MedicineId { get; set; }
    public int RequestedQuantity { get; set; }
    public string? Purpose { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
}

public class ApproveBorrowRequestDto
{
    public int? ApprovedQuantity { get; set; }
}

public class RejectBorrowRequestDto
{
    public string RejectionReason { get; set; } = string.Empty;
}

public class BorrowRecordDto
{
    public int Id { get; set; }
    public int MedicineShareId { get; set; }
    public int BorrowRequestId { get; set; }
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int LenderHouseholdId { get; set; }
    public string LenderHouseholdName { get; set; } = string.Empty;
    public int BorrowerHouseholdId { get; set; }
    public string BorrowerHouseholdName { get; set; } = string.Empty;
    public int BorrowerUserId { get; set; }
    public string BorrowerUsername { get; set; } = string.Empty;
    public int BorrowedQuantity { get; set; }
    public DateTime BorrowedAt { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public DateTime? LastReturnedAt { get; set; }
    public int ReturnedQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public BorrowRecordStatus Status { get; set; }
    public string? Notes { get; set; }
    public bool ReminderSent { get; set; }
}

public class ReturnBorrowedMedicineDto
{
    public int ReturnedQuantity { get; set; }
    public string? Notes { get; set; }
}

public class BorrowQueryParamsDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? HouseholdId { get; set; }
    public BorrowRequestStatus? RequestStatus { get; set; }
    public BorrowRecordStatus? RecordStatus { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}
