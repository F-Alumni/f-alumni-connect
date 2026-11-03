using AiService.Services.DTOs.Responses;

namespace AiService.Services.Interfaces;

public interface IRagIngestionService
{
    Task<RagIngestionResult> IngestDocumentsFromDirectoryAsync(string directoryPath, bool forceReindex = false, CancellationToken cancellationToken = default);
    Task IngestForumPostAsync(Guid postId, string title, string content, string authorName, string major, string campus, CancellationToken cancellationToken = default);
}
