using CFMS.Application.Mappings;
using CFMS.Application.Services.Implementations;
using CFMS.Application.Services.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CFMS.Application.Extensions;

/// <summary>
/// IServiceCollection extensions for the Application layer.
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper — scan this assembly for all profiles
        services.AddAutoMapper(typeof(UserMappingProfile).Assembly);

        // FluentValidation — scan this assembly for all AbstractValidator<T>
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceExtensions).Assembly);

        // Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IFeedbackAssignmentService, FeedbackAssignmentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        services.AddScoped<IFeedbackResponseService, FeedbackResponseService>();
        services.AddScoped<IFeedbackCommentService, FeedbackCommentService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
