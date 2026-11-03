using Contracts.Events;

namespace UserService.Services.Interfaces;

public interface IKafkaEventPublisher
{
    Task PublishUserRegisteredAsync(UserRegisteredEventV1 eventData, string? correlationId = null, CancellationToken cancellationToken = default);
    Task PublishDomainEventAsync<T>(string topic, string eventType, string partitionKey, T data, string? correlationId = null, CancellationToken cancellationToken = default);
}
