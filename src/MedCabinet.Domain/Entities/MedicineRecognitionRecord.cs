using MedCabinet.Domain.Enums;

namespace MedCabinet.Domain.Entities;

public class MedicineRecognitionRecord
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

    public string? RawOcrText { get; set; }
    public double? ConfidenceScore { get; set; }
    public string? RecognitionError { get; set; }

    public int? MatchedMedicineId { get; set; }
    public double? MatchScore { get; set; }

    public string? CorrectedName { get; set; }
    public string? CorrectedSpecification { get; set; }
    public string? CorrectedExpiryDate { get; set; }
    public string? CorrectedDosage { get; set; }
    public string? CorrectedManufacturer { get; set; }

    public int? FinalMedicineId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public virtual User? User { get; set; }
    public virtual Household? Household { get; set; }
    public virtual Medicine? MatchedMedicine { get; set; }
    public virtual Medicine? FinalMedicine { get; set; }
}
