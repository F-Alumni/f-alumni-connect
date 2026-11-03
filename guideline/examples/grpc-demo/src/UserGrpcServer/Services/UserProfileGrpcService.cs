using Contracts.Grpc.UserProfile.V1;
using Grpc.Core;

namespace UserGrpcServer.Services;

/// <summary>
/// Implementation of UserProfileService gRPC contract.
/// In a full service, this would query FAlumni_User_Db.
/// For this minimal demo, it queries an in-memory collection of sample profiles.
/// </summary>
public class UserProfileGrpcService : UserProfileService.UserProfileServiceBase
{
    private readonly ILogger<UserProfileGrpcService> _logger;

    // In-memory sample profiles for demo and automated test verification
    private static readonly Dictionary<Guid, (string DisplayName, string AvatarUrl, string Status)> SampleProfiles = new()
    {
        [Guid.Parse("11111111-1111-1111-1111-111111111111")] =
            ("Nguyen Van A", "https://cdn.falumni.local/avatars/nguyenvana.jpg", "Alumni"),
        [Guid.Parse("22222222-2222-2222-2222-222222222222")] =
            ("Tran Thi B", "https://cdn.falumni.local/avatars/tranthib.jpg", "Student"),
        [Guid.Parse("33333333-3333-3333-3333-333333333333")] =
            ("Le Van C", "https://cdn.falumni.local/avatars/levanc.jpg", "Suspended")
    };

    public UserProfileGrpcService(ILogger<UserProfileGrpcService> logger)
    {
        _logger = logger;
    }

    public override Task<GetUserSummaryResponse> GetUserSummary(
        GetUserSummaryRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetUserSummary called for UserId: {UserId}", request.UserId);

        // 1. Validate request payload
        if (string.IsNullOrWhiteSpace(request.UserId) || !Guid.TryParse(request.UserId, out var userGuid))
        {
            _logger.LogWarning("Invalid or malformed UserId: '{UserId}'", request.UserId);
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"UserId '{request.UserId}' is not a valid UUID."));
        }

        // 2. Check cancellation
        context.CancellationToken.ThrowIfCancellationRequested();

        // 3. Query in-memory profiles
        if (!SampleProfiles.TryGetValue(userGuid, out var profile))
        {
            _logger.LogWarning("User profile with ID {UserId} was not found.", userGuid);
            throw new RpcException(new Status(StatusCode.NotFound, $"User profile with ID '{userGuid}' was not found."));
        }

        // 4. Return minimal public summary
        _logger.LogInformation("Found profile for {UserId}: DisplayName={DisplayName}, Status={Status}",
            userGuid, profile.DisplayName, profile.Status);

        return Task.FromResult(new GetUserSummaryResponse
        {
            UserId = userGuid.ToString(),
            DisplayName = profile.DisplayName,
            AvatarUrl = profile.AvatarUrl,
            Status = profile.Status
        });
    }
}
