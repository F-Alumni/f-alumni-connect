using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AdminService.Services.HttpClients;

public interface IForumModerationClient
{
    Task<bool> HidePostAsync(Guid postId, string? bearerToken = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteCommentAsync(Guid commentId, string? bearerToken = null, CancellationToken cancellationToken = default);
}

public class ForumModerationClient : IForumModerationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ForumModerationClient> _logger;

    public ForumModerationClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ForumModerationClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = configuration["Services:ForumService:Url"] ?? "http://localhost:5003";
        if (!baseUrl.EndsWith('/'))
        {
            baseUrl += "/";
        }
        _httpClient.BaseAddress = new Uri(baseUrl);

        var timeoutSeconds = int.TryParse(configuration["Services:ForumService:TimeoutSeconds"], out var t) ? t : 5;
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public async Task<bool> HidePostAsync(Guid postId, string? bearerToken = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Patch, $"api/v1/posts/{postId}/visibility")
            {
                Content = JsonContent.Create(new { IsHidden = true })
            };
            AttachAuthHeader(req, bearerToken);
            var response = await _httpClient.SendAsync(req, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ForumService returned {StatusCode} while hiding post {PostId}", response.StatusCode, postId);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to call ForumService to hide post {PostId}", postId);
            return false;
        }
    }

    public async Task<bool> DeleteCommentAsync(Guid commentId, string? bearerToken = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/comments/{commentId}");
            AttachAuthHeader(req, bearerToken);
            var response = await _httpClient.SendAsync(req, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ForumService returned {StatusCode} while deleting comment {CommentId}", response.StatusCode, commentId);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to call ForumService to delete comment {CommentId}", commentId);
            return false;
        }
    }

    private static void AttachAuthHeader(HttpRequestMessage request, string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            var rawToken = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? token["Bearer ".Length..]
                : token;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);
        }
    }
}
