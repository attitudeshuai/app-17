namespace MedCabinet.Application.DTOs.Household;

public class HouseholdDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public int MedicineCount { get; set; }
}

public class CreateHouseholdRequestDto
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateHouseholdRequestDto
{
    public string? Name { get; set; }
}

public class HouseholdQueryParamsDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchKeyword { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}
