using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AdminService.Repositories.Extensions;
using AdminService.Services.HttpClients;
using AdminService.Services.Implementations;
using AdminService.Services.Interfaces;

namespace AdminService.Services.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers both Service and Repository layers into the dependency injection container.
    /// Passes configuration to the repository layer for database connection string setup.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // 1. Register lower-level Repository layer dependencies
        services.AddRepositories(configuration);

        // 2. Register HTTP Clients for inter-service communication
        services.AddHttpClient<IUserServiceClient, UserServiceClient>();

        // 3. Register Service layer dependencies
        services.AddScoped<ISampleItemService, SampleItemService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IUserModerationService, UserModerationService>();
        services.AddScoped<IModerationReportService, ModerationReportService>();
        services.AddScoped<ISystemAnnouncementService, SystemAnnouncementService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDataExportService, DataExportService>();

        return services;
    }
}
