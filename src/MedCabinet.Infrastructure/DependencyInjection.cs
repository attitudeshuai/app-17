using MedCabinet.Domain.Interfaces;
using MedCabinet.Infrastructure.Data;
using MedCabinet.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedCabinet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 数据库连接
        var dbHost = configuration["DB_HOST"] ?? "localhost";
        var dbPort = configuration["DB_PORT"] ?? "13312";
        var dbName = configuration["DB_NAME"] ?? "medcabinet";
        var dbUser = configuration["DB_USER"] ?? "app_user";
        var dbPassword = configuration["DB_PASSWORD"] ?? "app_pass";

        var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};Uid={dbUser};Pwd={dbPassword};";

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        // 仓储
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }
}
