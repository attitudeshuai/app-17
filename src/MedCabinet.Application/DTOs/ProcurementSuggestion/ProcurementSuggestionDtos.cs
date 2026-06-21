using MedCabinet.Domain.Enums;

namespace MedCabinet.Application.DTOs.ProcurementSuggestion;

public class ProcurementSuggestionDto
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public int MedicineId { get; set; }
    public int? UserId { get; set; }
    public int SuggestedQuantity { get; set; }
    public DateTime SuggestedPurchaseDate { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public ProcurementStatus Status { get; set; }
    public decimal UsageFrequency { get; set; }
    public int CurrentStock { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime? PurchasedAt { get; set; }
    public int? PurchasedQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string HouseholdName { get; set; } = string.Empty;
}

public class GenerateProcurementSuggestionsRequestDto
{
    public int HouseholdId { get; set; }
}

public class MarkProcurementSuggestionRequestDto
{
    public ProcurementStatus Status { get; set; }
    public int? PurchasedQuantity { get; set; }
    public string? Notes { get; set; }
}

public class CreateProcurementSuggestionRequestDto
{
    public int HouseholdId { get; set; }
    public int MedicineId { get; set; }
    public int? UserId { get; set; }
    public int SuggestedQuantity { get; set; }
    public DateTime SuggestedPurchaseDate { get; set; }
    public UrgencyLevel UrgencyLevel { get; set; }
    public string? Notes { get; set; }
}

public class UpdateProcurementSuggestionRequestDto
{
    public int? SuggestedQuantity { get; set; }
    public DateTime? SuggestedPurchaseDate { get; set; }
    public UrgencyLevel? UrgencyLevel { get; set; }
    public string? Notes { get; set; }
}

public class ProcurementSuggestionQueryParamsDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? HouseholdId { get; set; }
    public int? MedicineId { get; set; }
    public int? UserId { get; set; }
    public ProcurementStatus? Status { get; set; }
    public UrgencyLevel? UrgencyLevel { get; set; }
    public string? SearchKeyword { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}

public class ProcurementStatsDto
{
    public int TotalSuggestions { get; set; }
    public int PendingCount { get; set; }
    public int PurchasedCount { get; set; }
    public int IgnoredCount { get; set; }
    public int LowUrgencyCount { get; set; }
    public int MediumUrgencyCount { get; set; }
    public int HighUrgencyCount { get; set; }
    public int CriticalUrgencyCount { get; set; }
    public List<ProcurementByMedicineDto> ByMedicine { get; set; } = new();
    public List<ProcurementByMemberDto> ByMember { get; set; } = new();
    public List<ProcurementMonthlyTrendDto> MonthlyTrend { get; set; } = new();
}

public class ProcurementByMedicineDto
{
    public int MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public int SuggestionCount { get; set; }
    public int TotalSuggestedQuantity { get; set; }
    public int PurchasedCount { get; set; }
    public int PendingCount { get; set; }
}

public class ProcurementByMemberDto
{
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int SuggestionCount { get; set; }
    public int TotalSuggestedQuantity { get; set; }
    public int PurchasedCount { get; set; }
    public int PendingCount { get; set; }
}

public class ProcurementMonthlyTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int GeneratedCount { get; set; }
    public int PurchasedCount { get; set; }
}

public class ProcurementExportQueryParamsDto
{
    public int? HouseholdId { get; set; }
    public string? Dimension { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
