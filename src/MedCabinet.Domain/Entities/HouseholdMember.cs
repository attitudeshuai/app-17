namespace MedCabinet.Domain.Entities;

public class HouseholdMember
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }

    public virtual Household? Household { get; set; }
    public virtual User? User { get; set; }
}
