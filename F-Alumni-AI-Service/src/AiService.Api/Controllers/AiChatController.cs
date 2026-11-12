using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiService.Api.Extensions;
using AiService.Services.DTOs.Requests;
using AiService.Services.DTOs.Responses;
using AiService.Services.Interfaces;

namespace AiService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/ai")]
public class AiChatController : ControllerBase
{
    private readonly IAiChatService _aiChatService;
    private readonly IRagIngestionService _ragIngestionService;
    private readonly IConfiguration _configuration;

    public AiChatController(
        IAiChatService aiChatService,
        IRagIngestionService ragIngestionService,
        IConfiguration configuration)
    {
        _aiChatService = aiChatService;
        _ragIngestionService = ragIngestionService;
        _configuration = configuration;
    }

    /// <summary>
    /// AI-01, AI-02, AI-03, AI-04: Ask AI question with multi-turn conversation and source citations.
    /// </summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(AiChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var response = await _aiChatService.AskAsync(request, userId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// AI-04: Get conversation history for a multi-turn chat session.
    /// </summary>
    [HttpGet("conversations/{id:guid}")]
    [ProducesResponseType(typeof(AiConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var conv = await _aiChatService.GetConversationHistoryAsync(id, userId, cancellationToken);
        return Ok(conv);
    }

    /// <summary>
    /// List all conversation sessions of current user.
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(IReadOnlyList<AiConversationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var convs = await _aiChatService.GetUserConversationsAsync(userId, cancellationToken);
        return Ok(convs);
    }

    /// <summary>
    /// Ingest or re-index RAG documents from directory. Requires ADMIN role.
    /// </summary>
    [HttpPost("rag/ingest")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(RagIngestionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Ingest([FromBody] IngestRagRequest? request, CancellationToken cancellationToken = default)
    {
        var targetDir = ResolveAndValidateRagDirectory(request?.DirectoryPath);

        var forceReindex = request?.ForceReindex ?? false;
        var result = await _ragIngestionService.IngestDocumentsFromDirectoryAsync(targetDir, forceReindex, cancellationToken);
        return Ok(result);
    }

    private string ResolveAndValidateRagDirectory(string? requestedDirectoryPath)
    {
        var configuredDefault = _configuration["Rag:DataDirectory"]
            ?? _configuration["Rag:DefaultDirectory"]
            ?? "data/rag";

        var allowedRoots = GetAllowedRagRoots(configuredDefault);

        if (requestedDirectoryPath is null)
        {
            return ResolveDefaultRagDirectory(configuredDefault);
        }

        if (string.IsNullOrWhiteSpace(requestedDirectoryPath))
        {
            throw new ArgumentException("DirectoryPath cannot be empty or whitespace.");
        }

        string resolvedTarget;
        try
        {
            if (!Path.IsPathRooted(requestedDirectoryPath))
            {
                var baseDirCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, requestedDirectoryPath));
                var currentDirCandidate = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), requestedDirectoryPath));
                resolvedTarget = Directory.Exists(baseDirCandidate) ? baseDirCandidate : currentDirCandidate;
            }
            else
            {
                resolvedTarget = Path.GetFullPath(requestedDirectoryPath);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ArgumentException("DirectoryPath is invalid.", nameof(requestedDirectoryPath), ex);
        }

        if (!IsWithinAllowedRoots(resolvedTarget, allowedRoots))
        {
            throw new ArgumentException("DirectoryPath must be within the allowed RAG data directory.");
        }

        if (Directory.Exists(resolvedTarget))
        {
            var dirInfo = new DirectoryInfo(resolvedTarget);
            if (dirInfo.LinkTarget != null)
            {
                var linkTarget = dirInfo.ResolveLinkTarget(returnFinalTarget: true)?.FullName;
                if (linkTarget == null || !IsWithinAllowedRoots(linkTarget, allowedRoots))
                {
                    throw new ArgumentException("DirectoryPath must not resolve outside the allowed RAG data directory.");
                }
            }
        }

        return resolvedTarget;
    }

    private static string ResolveDefaultRagDirectory(string configuredDefault)
    {
        if (Path.IsPathRooted(configuredDefault))
        {
            return Path.GetFullPath(configuredDefault);
        }

        var baseDirCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredDefault));
        if (Directory.Exists(baseDirCandidate))
        {
            return baseDirCandidate;
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configuredDefault));
    }

    private static IReadOnlyList<string> GetAllowedRagRoots(string configuredDefault)
    {
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var roots = new HashSet<string>(comparer)
        {
            NormalizeDirectoryPath(Path.Combine(AppContext.BaseDirectory, "data", "rag")),
            NormalizeDirectoryPath(Path.Combine(Directory.GetCurrentDirectory(), "data", "rag"))
        };

        if (Path.IsPathRooted(configuredDefault))
        {
            roots.Add(NormalizeDirectoryPath(configuredDefault));
        }
        else
        {
            roots.Add(NormalizeDirectoryPath(Path.Combine(AppContext.BaseDirectory, configuredDefault)));
            roots.Add(NormalizeDirectoryPath(Path.Combine(Directory.GetCurrentDirectory(), configuredDefault)));
        }

        return roots.ToList();
    }

    private static bool IsWithinAllowedRoots(string candidatePath, IReadOnlyList<string> allowedRoots)
    {
        var normalizedTarget = NormalizeDirectoryPath(candidatePath);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        return allowedRoots.Any(root =>
            string.Equals(normalizedTarget, root, comparison) ||
            normalizedTarget.StartsWith(root + Path.DirectorySeparatorChar, comparison));
    }

    private static string NormalizeDirectoryPath(string path)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }
}
