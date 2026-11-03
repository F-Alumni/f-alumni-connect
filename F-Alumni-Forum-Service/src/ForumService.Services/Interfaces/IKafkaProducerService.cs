using Contracts.Events;

namespace ForumService.Services.Interfaces;

public interface IKafkaProducerService
{
    Task PublishPostCreatedAsync(PostCreatedEventV1 eventData, CancellationToken cancellationToken = default);
}
