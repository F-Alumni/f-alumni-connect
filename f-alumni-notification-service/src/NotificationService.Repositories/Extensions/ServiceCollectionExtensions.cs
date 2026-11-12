using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Repositories.Context;
using NotificationService.Repositories.Implementations;
using NotificationService.Repositories.Interfaces;

namespace NotificationService.Repositories.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var provider = configuration?["DatabaseProvider"] ?? "InMemory";
        var connectionString = configuration?.GetConnectionString("DefaultConnection");

        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<NotificationServiceDbContext>(options =>
                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3)));
        }
        else
        {
            var dbName = configuration?["InMemoryDatabaseName"] ?? "NotificationServiceInMemoryDb";
            services.AddDbContext<NotificationServiceDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        }

        services.AddScoped<INotificationRepository, NotificationRepository>();
        return services;
    }
}
