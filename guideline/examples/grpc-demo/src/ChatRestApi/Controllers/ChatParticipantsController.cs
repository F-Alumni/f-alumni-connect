using Contracts.Grpc.UserProfile.V1;
using ChatRestApi.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace ChatRestApi.Controllers;

[ApiController]
[Route("api/v1/chats")]
public class ChatParticipantsController : ControllerBase
{
    private readonly UserProfileService.UserProfileServiceClient _userProfileClient;
    private readonly ILogger<ChatParticipantsController> _logger;
    private readonly int _timeoutSeconds;

    public ChatParticipantsController(
        UserProfileService.UserProfileServiceClient userProfileClient,
        IConfiguration configuration,
        ILogger<ChatParticipantsController> logger)
    {
        _userProfileClient = userProfileClient;
        _logger = logger;
        _timeoutSeconds = configuration.GetValue("GrpcServices:UserProfile:TimeoutSeconds", 3);
    }

    /// <summary>
    /// Looks up participant summary profile for a conversation by synchronously calling User Service via gRPC.
    /// </summary>
    /// <param name="conversationId">The chat conversation identifier.</param>
    /// <param name="userId">The participant user identifier to look up.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Combined conversation and user profile details.</returns>
    [HttpGet("conversations/{conversationId:guid}/participants/{userId:guid}")]
    [ProducesResponseType(typeof(ConversationParticipantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> GetParticipantProfile(
        [FromRoute] Guid conversationId,
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Looking up participant profile: ConversationId={ConversationId}, UserId={UserId}",
            conversationId, userId);

        var request = new GetUserSummaryRequest { UserId = userId.ToString() };
        var deadline = DateTime.UtcNow.AddSeconds(_timeoutSeconds);

        try
        {
            var grpcResponse = await _userProfileClient.GetUserSummaryAsync(
                request,
                deadline: deadline,
                cancellationToken: cancellationToken);

            var response = new ConversationParticipantResponse(
                ConversationId: conversationId,
                UserId: Guid.Parse(grpcResponse.UserId),
                DisplayName: grpcResponse.DisplayName,
                AvatarUrl: grpcResponse.AvatarUrl,
                Status: grpcResponse.Status,
                ResolvedVia: "gRPC (HTTP/2 Protobuf)",
                RetrievedAtUtc: DateTime.UtcNow
            );

            return Ok(response);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            _logger.LogWarning("Participant profile not found via gRPC: {Detail}", ex.Status.Detail);
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Participant Profile Not Found",
                detail: ex.Status.Detail,
                type: "https://httpstatuses.com/404"
            );
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unavailable)
        {
            _logger.LogError(ex, "User gRPC Service is unavailable at target endpoint.");
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "User Profile Service Unavailable",
                detail: "The upstream User Service gRPC endpoint is unreachable or currently unavailable.",
                type: "https://httpstatuses.com/503"
            );
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded)
        {
            _logger.LogError("gRPC call to User Service exceeded deadline of {Timeout}s.", _timeoutSeconds);
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "User Profile Service Timeout",
                detail: $"The upstream User Service failed to respond within the {_timeoutSeconds}s deadline.",
                type: "https://httpstatuses.com/504"
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Participant profile request was canceled by the caller.");
            return StatusCode(499); // Client Closed Request
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Unexpected gRPC error: StatusCode={StatusCode}, Detail={Detail}",
                ex.StatusCode, ex.Status.Detail);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Upstream Communication Error",
                detail: ex.Status.Detail,
                type: "https://httpstatuses.com/500"
            );
        }
    }
}
