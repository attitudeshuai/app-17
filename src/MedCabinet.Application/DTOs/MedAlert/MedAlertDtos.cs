using MedCabinet.Domain.Enums;

namespace MedCabinet.Application.DTOs.MedAlert;

public class MedAlertDto
{
    public int Id { get; set; }
    public int MedicineId { get; set; }
    public int UserId { get; set; }
    public AlertType AlertType { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class CreateMedAlertRequestDto
{
    public int MedicineId { get; set; }
    public AlertType AlertType { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UpdateMedAlertRequestDto
{
    public bool? IsRead { get; set; }
    public string? Message { get; set; }
}

public class MedAlertQueryParamsDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? UserId { get; set; }
    public AlertType? AlertType { get; set; }
    public bool? IsRead { get; set; }
    public string? SearchKeyword { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}
