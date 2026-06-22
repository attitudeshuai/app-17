namespace MedCabinet.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public bool IsSystemAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<HouseholdMember> HouseholdMembers { get; set; } = new List<HouseholdMember>();
    public virtual ICollection<MedUsage> MedUsages { get; set; } = new List<MedUsage>();
    public virtual ICollection<MedAlert> MedAlerts { get; set; } = new List<MedAlert>();
    public virtual ICollection<ProcurementSuggestion> ProcurementSuggestions { get; set; } = new List<ProcurementSuggestion>();
}
