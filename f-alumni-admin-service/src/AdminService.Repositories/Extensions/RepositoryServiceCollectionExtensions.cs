using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AdminService.Repositories.Context;
using AdminService.Repositories.Implementations;
using AdminService.Repositories.Interfaces;

namespace AdminService.Repositories.Extensions;

public static class RepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core DbContext and repository implementations.
    /// Supports both SQL Server and In-Memory database providers for testing.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var connectionString = configuration?.GetConnectionString("DefaultConnection")
            ?? "Server=localhost\\SQLEXPRESS;Database=FAlumni_Admin_Db;Trusted_Connection=True;TrustServerCertificate=True;";

        var dbProvider = configuration?["DatabaseProvider"] ?? "SqlServer";
        var useInMemory = dbProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase)
            || string.Equals(configuration?["UseInMemoryDatabase"], "true", StringComparison.OrdinalIgnoreCase);

        if (useInMemory)
        {
            services.AddDbContext<AdminServiceDbContext>(options =>
            {
                options.UseInMemoryDatabase("AdminService_Db");
            });
        }
        else
        {
            services.AddDbContext<AdminServiceDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
        }

        services.AddScoped<ISampleItemRepository, SampleItemRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISystemAnnouncementRepository, SystemAnnouncementRepository>();
        services.AddScoped<IModerationReportRepository, ModerationReportRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();

        return services;
    }
}
