using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AiService.Repositories.Extensions;
using AiService.Services.Implementations;
using AiService.Services.Interfaces;

namespace AiService.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // 1. Register Repositories
        services.AddRepositories(configuration);

        // 2. Register HTTP Client for Gemini
        services.AddHttpClient();

        // 3. Register Services
        services.AddScoped<ISampleItemService, SampleItemService>();
        services.AddScoped<IRagIngestionService, RagIngestionService>();
        services.AddScoped<IRagSearchService, RagSearchService>();
        services.AddScoped<IAiChatService, AiChatService>();
        services.AddScoped<IMentorRecommendationService, MentorRecommendationService>();

        return services;
    }
}
