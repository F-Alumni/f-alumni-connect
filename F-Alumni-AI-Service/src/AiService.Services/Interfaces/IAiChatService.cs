using AiService.Services.DTOs.Requests;
using AiService.Services.DTOs.Responses;

namespace AiService.Services.Interfaces;

public interface IAiChatService
{
    Task<AiChatResponse> AskAsync(AiChatRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<AiConversationResponse> GetConversationHistoryAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiConversationResponse>> GetUserConversationsAsync(Guid userId, CancellationToken cancellationToken = default);
}
