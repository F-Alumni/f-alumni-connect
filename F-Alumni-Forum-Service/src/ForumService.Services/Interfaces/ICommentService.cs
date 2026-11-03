using ForumService.Services.DTOs.Requests;
using ForumService.Services.DTOs.Responses;

namespace ForumService.Services.Interfaces;

public interface ICommentService
{
    Task<CommentResponse> AddCommentAsync(Guid postId, CreateCommentRequest request, Guid authorId, string authorName, string authorAvatarUrl, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommentResponse>> GetCommentsByPostIdAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<CommentResponse> UpdateCommentAsync(Guid id, UpdateCommentRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(Guid id, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task<CommentResponse> AcceptAnswerAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default);
}
