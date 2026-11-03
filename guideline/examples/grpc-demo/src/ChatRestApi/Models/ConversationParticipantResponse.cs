namespace ChatRestApi.Models;

/// <summary>
/// Combined response model returned by Chat REST API to client (Web/Mobile),
/// combining conversation context with participant profile data retrieved synchronously via gRPC.
/// </summary>
public record ConversationParticipantResponse(
    Guid ConversationId,
    Guid UserId,
    string DisplayName,
    string AvatarUrl,
    string Status,
    string ResolvedVia,
    DateTime RetrievedAtUtc
);
