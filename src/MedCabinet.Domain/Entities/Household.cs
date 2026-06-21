namespace MedCabinet.Domain.Entities;

public class Household
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual User? Creator { get; set; }
    public virtual ICollection<HouseholdMember> HouseholdMembers { get; set; } = new List<HouseholdMember>();
    public virtual ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
    public virtual ICollection<ProcurementSuggestion> ProcurementSuggestions { get; set; } = new List<ProcurementSuggestion>();
}
