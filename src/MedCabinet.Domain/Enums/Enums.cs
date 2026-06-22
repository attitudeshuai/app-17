namespace MedCabinet.Domain.Enums;

public enum MedicineStatus
{
    Valid,
    NearExpiry,
    Expired,
    Empty
}

public enum AlertType
{
    NearExpiry,
    Expired,
    LowStock,
    EmptyStock,
    UsageReminder
}

public enum HouseholdRole
{
    Owner,
    Admin,
    Member
}

public enum ProcurementStatus
{
    Pending,
    Purchased,
    Ignored
}

public enum UrgencyLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum BloodType
{
    Unknown,
    APositive,
    ANegative,
    BPositive,
    BNegative,
    ABPositive,
    ABNegative,
    OPositive,
    ONegative
}

public enum ShareStatus
{
    Pending,
    Active,
    Expired,
    Revoked
}

public enum BorrowRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    Returned,
    Overdue
}

public enum BorrowRecordStatus
{
    Active,
    Returned,
    Overdue,
    Lost
}
