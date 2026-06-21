namespace MedCabinet.Application.DTOs.Statistics;

public class OverviewStatsDto
{
    public int TotalHouseholds { get; set; }
    public int TotalMembers { get; set; }
    public int TotalMedicines { get; set; }
    public int TotalUsages { get; set; }
    public int TotalAlerts { get; set; }
    public int ExpiredMedicines { get; set; }
    public int NearExpiryMedicines { get; set; }
    public int ValidMedicines { get; set; }
    public int EmptyMedicines { get; set; }
    public int UnreadAlerts { get; set; }
    public List<MedicineCategoryStatDto> CategoryStats { get; set; } = new();
    public List<MonthlyUsageStatDto> MonthlyUsageStats { get; set; } = new();
}

public class MedicineCategoryStatDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MonthlyUsageStatDto
{
    public string Month { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}

public class TrendQueryParamsDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? HouseholdId { get; set; }
}
