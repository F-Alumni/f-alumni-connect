using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserService.Repositories.Extensions;
using UserService.Services.Implementations;
using UserService.Services.Interfaces;

namespace UserService.Services.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Service layer dependencies and cascades down to Repository layer.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // 1. Register Repositories & DbContext
        services.AddRepositories(configuration);

        // 2. Register Business Services
        services.AddScoped<ISampleItemService, SampleItemService>();
        services.AddScoped<IKafkaEventPublisher, KafkaEventPublisher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserProfileAppService, UserProfileAppService>();

        return services;
    }
}
