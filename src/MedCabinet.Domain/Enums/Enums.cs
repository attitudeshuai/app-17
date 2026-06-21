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
