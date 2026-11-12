using Contracts.Events;

namespace ForumService.Services.Interfaces;

public interface IKafkaProducerService
{
    Task PublishPostCreatedAsync(PostCreatedEventV1 eventData, CancellationToken cancellationToken = default);
    Task PublishPostReactedAsync(PostReactedEventV1 eventData, CancellationToken cancellationToken = default);
    Task PublishCommentCreatedAsync(CommentCreatedEventV1 eventData, CancellationToken cancellationToken = default);
    Task PublishReportCreatedAsync(ReportCreatedEventV1 eventData, CancellationToken cancellationToken = default);
}
