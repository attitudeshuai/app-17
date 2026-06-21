namespace MedCabinet.Application.DTOs.HouseholdMember;

public class HouseholdMemberDto
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class CreateHouseholdMemberRequestDto
{
    public int HouseholdId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "Member";
}

public class UpdateHouseholdMemberRequestDto
{
    public string? Role { get; set; }
}

public class JoinHouseholdRequestDto
{
    public string InviteCode { get; set; } = string.Empty;
}

public class HouseholdMemberQueryParamsDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? HouseholdId { get; set; }
    public string? SearchKeyword { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}
