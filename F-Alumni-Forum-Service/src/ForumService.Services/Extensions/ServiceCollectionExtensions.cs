using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ForumService.Repositories.Extensions;
using ForumService.Services.Implementations;
using ForumService.Services.Interfaces;

namespace ForumService.Services.Extensions;

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

        // 2. Register Service layer dependencies
        services.AddScoped<ISampleItemService, SampleItemService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IInteractionService, InteractionService>();
        services.AddScoped<IKafkaProducerService, KafkaProducerService>();

        return services;
    }
}
