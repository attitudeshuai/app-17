using Mapster;
using MedCabinet.Application.DTOs.Auth;
using MedCabinet.Application.DTOs.Household;
using MedCabinet.Application.DTOs.HouseholdMember;
using MedCabinet.Application.DTOs.Medicine;
using MedCabinet.Application.DTOs.MedUsage;
using MedCabinet.Application.DTOs.MedAlert;
using MedCabinet.Application.DTOs.ProcurementSuggestion;
using MedCabinet.Application.DTOs.HealthProfile;
using MedCabinet.Domain.Entities;

namespace MedCabinet.Application.Mappings;

public static class MapsterConfig
{
    public static void Configure()
    {
        TypeAdapterConfig.GlobalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.Flexible);

        // User 映射
        TypeAdapterConfig<User, UserDto>.NewConfig();

        // Household 映射
        TypeAdapterConfig<Household, HouseholdDto>.NewConfig();

        // HouseholdMember 映射
        TypeAdapterConfig<HouseholdMember, HouseholdMemberDto>.NewConfig()
            .Map(dest => dest.Username, src => src.User != null ? src.User.Username : string.Empty)
            .Map(dest => dest.Avatar, src => src.User != null ? src.User.Avatar : null)
            .Map(dest => dest.Email, src => src.User != null ? src.User.Email : string.Empty);

        // Medicine 映射
        TypeAdapterConfig<Medicine, MedicineDto>.NewConfig()
            .Map(dest => dest.DaysUntilExpiry, src => (int)(src.ExpiryDate - DateTime.Now).TotalDays);

        // MedUsage 映射
        TypeAdapterConfig<MedUsage, MedUsageDto>.NewConfig()
            .Map(dest => dest.MedicineName, src => src.Medicine != null ? src.Medicine.Name : string.Empty)
            .Map(dest => dest.Username, src => src.User != null ? src.User.Username : string.Empty);

        // MedAlert 映射
        TypeAdapterConfig<MedAlert, MedAlertDto>.NewConfig()
            .Map(dest => dest.MedicineName, src => src.Medicine != null ? src.Medicine.Name : string.Empty)
            .Map(dest => dest.Username, src => src.User != null ? src.User.Username : string.Empty);

        // Create 请求映射
        TypeAdapterConfig<CreateHouseholdRequestDto, Household>.NewConfig();
        TypeAdapterConfig<CreateMedicineRequestDto, Medicine>.NewConfig();
        TypeAdapterConfig<CreateMedUsageRequestDto, MedUsage>.NewConfig();
        TypeAdapterConfig<CreateMedAlertRequestDto, MedAlert>.NewConfig();

        // ProcurementSuggestion 映射
        TypeAdapterConfig<ProcurementSuggestion, ProcurementSuggestionDto>.NewConfig()
            .Map(dest => dest.MedicineName, src => src.Medicine != null ? src.Medicine.Name : string.Empty)
            .Map(dest => dest.Username, src => src.User != null ? src.User.Username : null)
            .Map(dest => dest.HouseholdName, src => src.Household != null ? src.Household.Name : string.Empty);

        TypeAdapterConfig<CreateProcurementSuggestionRequestDto, ProcurementSuggestion>.NewConfig();

        // HealthProfile 映射
        TypeAdapterConfig<HealthProfile, HealthProfileDto>.NewConfig()
            .Map(dest => dest.Username, src => src.User != null ? src.User.Username : string.Empty)
            .Map(dest => dest.HouseholdName, src => src.Household != null ? src.Household.Name : string.Empty);

        TypeAdapterConfig<CreateHealthProfileRequestDto, HealthProfile>.NewConfig();

        // HealthProfileAuditLog 映射
        TypeAdapterConfig<HealthProfileAuditLog, HealthProfileAuditLogDto>.NewConfig();
    }
}
