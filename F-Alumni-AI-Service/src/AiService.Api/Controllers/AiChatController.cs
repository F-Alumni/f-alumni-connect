using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiService.Api.Extensions;
using AiService.Services.DTOs.Requests;
using AiService.Services.DTOs.Responses;
using AiService.Services.Interfaces;

namespace AiService.Api.Controllers;

[ApiController]
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
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : Guid.Parse("11111111-1111-1111-1111-111111111111");
        var response = await _aiChatService.AskAsync(request, userId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// AI-04: Get conversation history for a multi-turn chat session.
    /// </summary>
    [HttpGet("conversations/{id:guid}")]
    [ProducesResponseType(typeof(AiConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : Guid.Parse("11111111-1111-1111-1111-111111111111");
        var conv = await _aiChatService.GetConversationHistoryAsync(id, userId, cancellationToken);
        return Ok(conv);
    }

    /// <summary>
    /// List all conversation sessions of current user.
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(IReadOnlyList<AiConversationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken = default)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : Guid.Parse("11111111-1111-1111-1111-111111111111");
        var convs = await _aiChatService.GetUserConversationsAsync(userId, cancellationToken);
        return Ok(convs);
    }

    /// <summary>
    /// Ingest or re-index RAG documents from directory.
    /// </summary>
    [HttpPost("rag/ingest")]
    [ProducesResponseType(typeof(RagIngestionResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Ingest([FromBody] IngestRagRequest? request, CancellationToken cancellationToken = default)
    {
        var targetDir = request?.DirectoryPath 
            ?? _configuration["Rag:DataDirectory"]
            ?? _configuration["Rag:DefaultDirectory"] 
            ?? "data/rag";

        if (!Path.IsPathRooted(targetDir))
        {
            var baseDirCandidate = Path.Combine(AppContext.BaseDirectory, targetDir);
            var currentDirCandidate = Path.Combine(Directory.GetCurrentDirectory(), targetDir);
            targetDir = Path.GetFullPath(Directory.Exists(baseDirCandidate) ? baseDirCandidate : currentDirCandidate);
        }
        else
        {
            targetDir = Path.GetFullPath(targetDir);
        }

        var forceReindex = request?.ForceReindex ?? false;
        var result = await _ragIngestionService.IngestDocumentsFromDirectoryAsync(targetDir, forceReindex, cancellationToken);
        return Ok(result);
    }
}
