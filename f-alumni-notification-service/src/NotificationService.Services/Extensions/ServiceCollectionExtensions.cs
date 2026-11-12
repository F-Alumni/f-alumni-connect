using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Repositories.Extensions;
using NotificationService.Services.Implementations;
using NotificationService.Services.Interfaces;

namespace NotificationService.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddRepositories(configuration);
        services.AddScoped<INotificationAppService, NotificationAppService>();
        services.AddScoped<INotificationEventProcessor, NotificationKafkaProcessor>();
        return services;
    }
}
