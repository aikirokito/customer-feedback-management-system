using CFMS.Infrastructure.Persistence;
using CFMS.Infrastructure.Repositories.Implementations;
using CFMS.Application.Common.Interfaces;
using CFMS.Infrastructure.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CFMS.Infrastructure.Extensions;

/// <summary>
/// IServiceCollection extensions for Infrastructure layer registration.
/// Called from CFMS.Api Program.cs.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // SQL Server 2022 theo baseline của dự án.
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlServer => sqlServer.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

        // Repositories (also registered individually for targeted injection)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IFeedbackCategoryRepository, FeedbackCategoryRepository>();

        // Infrastructure Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<IGoogleAuthService, GoogleAuthService>();
        services.AddHttpClient<ISupabaseStorageService, SupabaseStorageService>();

        return services;
    }
}
