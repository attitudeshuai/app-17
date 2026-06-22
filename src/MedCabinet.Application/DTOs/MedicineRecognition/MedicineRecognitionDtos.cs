using MedCabinet.Domain.Enums;

namespace MedCabinet.Application.DTOs.MedicineRecognition;

public class RecognizedMedicineInfoDto
{
    public string? Name { get; set; }
    public string? Specification { get; set; }
    public string? ExpiryDate { get; set; }
    public string? Dosage { get; set; }
    public string? Manufacturer { get; set; }
    public double ConfidenceScore { get; set; }
    public List<string> MissingFields { get; set; } = new();
}

public class MedicineRecognitionResultDto
{
    public int RecordId { get; set; }
    public OcrRecognitionStatus Status { get; set; }
    public RecognizedMedicineInfoDto RecognizedInfo { get; set; } = new();
    public List<MatchedMedicineDto> MatchedMedicines { get; set; } = new();
    public string? RawOcrText { get; set; }
    public string? ImageUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class MatchedMedicineDto
{
    public int MedicineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Specification { get; set; }
    public string Category { get; set; } = string.Empty;
    public double MatchScore { get; set; }
    public bool IsExactMatch { get; set; }
}

public class ConfirmRecognitionRequestDto
{
    public int RecordId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Indication { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int StockQuantity { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    public string? Contraindications { get; set; }
    public string? PhotoUrl { get; set; }
    public int HouseholdId { get; set; }
    public bool IsModified { get; set; }
    public int? MatchedMedicineId { get; set; }
}

public class MedicineRecognitionRecordDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? HouseholdId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public OcrRecognitionStatus RecognitionStatus { get; set; }
    public RecognitionConfirmStatus ConfirmStatus { get; set; }

    public string? RecognizedName { get; set; }
    public string? RecognizedSpecification { get; set; }
    public string? RecognizedExpiryDate { get; set; }
    public string? RecognizedDosage { get; set; }
    public string? RecognizedManufacturer { get; set; }

    public double? ConfidenceScore { get; set; }
    public string? RecognitionError { get; set; }

    public int? MatchedMedicineId { get; set; }
    public double? MatchScore { get; set; }

    public int? FinalMedicineId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

public class RecognitionRecordQueryParamsDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? HouseholdId { get; set; }
    public OcrRecognitionStatus? RecognitionStatus { get; set; }
    public RecognitionConfirmStatus? ConfirmStatus { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}
