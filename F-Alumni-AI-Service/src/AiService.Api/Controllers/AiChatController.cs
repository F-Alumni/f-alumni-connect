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
    private readonly IRagSearchService _ragSearchService;
    private readonly IRagIngestionService _ragIngestionService;
    private readonly IConfiguration _configuration;

    public AiChatController(
        IAiChatService aiChatService,
        IRagSearchService ragSearchService,
        IRagIngestionService ragIngestionService,
        IConfiguration configuration)
    {
        _aiChatService = aiChatService;
        _ragSearchService = ragSearchService;
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
    /// Diagnostic endpoint to test Intent Boosting and RAG retrieval scoring (/boost).
    /// </summary>
    [HttpGet("boost")]
    [HttpGet("boost/test")]
    [HttpGet("/boost")]
    [HttpGet("/boost/test")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RagBoostDiagnosticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestBoostGet(
        [FromQuery] string? q = null,
        [FromQuery] string? query = null,
        [FromQuery] string? major = null,
        [FromQuery] string? programCode = null,
        [FromQuery] string campus = "All",
        [FromQuery] int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var rawQuery = !string.IsNullOrWhiteSpace(q) ? q : (!string.IsNullOrWhiteSpace(query) ? query : "Học phí tại Hola là bao nhiêu?");
        var resolvedMajor = !string.IsNullOrWhiteSpace(major) ? major : programCode;
        var resolvedTopK = topK <= 0 ? 5 : Math.Min(topK, 50);
        var diagnostics = await _ragSearchService.TestBoostAsync(rawQuery, resolvedMajor, campus, resolvedTopK, cancellationToken);
        return Ok(diagnostics);
    }

    /// <summary>
    /// Diagnostic endpoint to test Intent Boosting with JSON payload (/boost).
    /// </summary>
    [HttpPost("boost")]
    [HttpPost("/boost")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RagBoostDiagnosticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestBoostPost(
        [FromBody] BoostTestRequest? request,
        CancellationToken cancellationToken = default)
    {
        var rawQuery = !string.IsNullOrWhiteSpace(request?.Query)
            ? request!.Query
            : (!string.IsNullOrWhiteSpace(request?.Q) ? request!.Q : "Học phí tại Hola là bao nhiêu?");
        var program = request?.ProgramCode ?? request?.Major;
        var campus = request?.Campus ?? "All";
        var requestedTopK = (request?.TopK ?? 5) <= 0 ? 5 : Math.Min(request!.TopK, 50);
        var diagnostics = await _ragSearchService.TestBoostAsync(rawQuery, program, campus, requestedTopK, cancellationToken);
        return Ok(diagnostics);
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

    [HttpPost("rag/upload")]
    [Authorize(Roles = "ADMIN")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    [ProducesResponseType(typeof(RagIngestionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadAndIngest(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is required and must not be empty.");
        }

        const long maxFileSizeBytes = 15 * 1024 * 1024;
        if (file.Length > maxFileSizeBytes)
        {
            throw new ArgumentException("File exceeds maximum allowed size of 15 MB.");
        }

        var originalName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(originalName);
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".docx", ".md", ".txt" };
        if (string.IsNullOrWhiteSpace(ext) || !allowedExtensions.Contains(ext))
        {
            throw new ArgumentException("Only .docx, .md, and .txt files are supported.");
        }

        var safeFileName = string.Join("_", originalName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = $"rag-upload-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{ext.ToLowerInvariant()}";
        }

        var targetDir = ResolveAndValidateRagDirectory(null);
        var uploadsDir = Path.Combine(targetDir, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var destinationPath = Path.Combine(uploadsDir, safeFileName);

        await using (var stream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var result = await _ragIngestionService.IngestDocumentsFromDirectoryAsync(targetDir, forceReindex: true, cancellationToken);
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
