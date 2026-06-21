using FluentValidation;
using MedCabinet.Application.Interfaces;
using MedCabinet.Application.Mappings;
using MedCabinet.Application.Services;
using MedCabinet.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace MedCabinet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 注册 Mapster 映射配置
        MapsterConfig.Configure();

        // 注册服务
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IHouseholdService, HouseholdService>();
        services.AddScoped<IHouseholdMemberService, HouseholdMemberService>();
        services.AddScoped<IMedicineService, MedicineService>();
        services.AddScoped<IMedUsageService, MedUsageService>();
        services.AddScoped<IMedAlertService, MedAlertService>();
        services.AddScoped<IStatisticsService, StatisticsService>();

        // 注册验证器
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        return services;
    }
}
