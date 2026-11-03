using Contracts.Grpc.UserProfile.V1;
using Grpc.Core;
using UserService.Services.Exceptions;
using UserService.Services.Interfaces;

namespace UserService.Api.Grpc;

/// <summary>
/// CH-04: gRPC Server implementation for synchronous profile lookup from Chat Service.
/// Conforms strictly to contracts/grpc/user-profile/v1/user_profile.proto.
/// </summary>
public class UserProfileGrpcService : Contracts.Grpc.UserProfile.V1.UserProfileService.UserProfileServiceBase
{
    private readonly IUserProfileAppService _userProfileService;
    private readonly ILogger<UserProfileGrpcService> _logger;

    public UserProfileGrpcService(
        IUserProfileAppService userProfileService,
        ILogger<UserProfileGrpcService> logger)
    {
        _userProfileService = userProfileService;
        _logger = logger;
    }

    public override async Task<GetUserSummaryResponse> GetUserSummary(
        GetUserSummaryRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetUserSummary called for UserId: {UserId}", request.UserId);

        if (string.IsNullOrWhiteSpace(request.UserId) || !Guid.TryParse(request.UserId, out var userGuid))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"UserId '{request.UserId}' is not a valid UUID."));
        }

        context.CancellationToken.ThrowIfCancellationRequested();

        try
        {
            var profile = await _userProfileService.GetByIdAsync(userGuid, isOwnerOrAdmin: false, context.CancellationToken);

            var statusLabel = profile.Status.Equals("Suspended", StringComparison.OrdinalIgnoreCase)
                ? "Suspended"
                : profile.AccountType;

            return new GetUserSummaryResponse
            {
                UserId = profile.Id.ToString(),
                DisplayName = profile.DisplayName,
                AvatarUrl = profile.AvatarUrl,
                Status = statusLabel
            };
        }
        catch (NotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"User profile with ID '{userGuid}' was not found."));
        }
    }
}
