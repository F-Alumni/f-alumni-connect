using Microsoft.Extensions.Logging;
using UserService.Repositories.Entities;
using UserService.Repositories.Helpers;
using UserService.Repositories.Interfaces;
using UserService.Services.DTOs.Common;
using UserService.Services.DTOs.Requests;
using UserService.Services.DTOs.Responses;
using UserService.Services.Exceptions;
using UserService.Services.Interfaces;

namespace UserService.Services.Implementations;

public class UserProfileAppService : IUserProfileAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IKafkaEventPublisher _kafkaPublisher;
    private readonly ILogger<UserProfileAppService> _logger;

    public UserProfileAppService(
        IUserRepository userRepository,
        IKafkaEventPublisher kafkaPublisher,
        ILogger<UserProfileAppService> logger)
    {
        _userRepository = userRepository;
        _kafkaPublisher = kafkaPublisher;
        _logger = logger;
    }

    public async Task<UserProfileResponse> GetByIdAsync(
        Guid userId,
        bool isOwnerOrAdmin = false,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        return user.ToResponse(applyPublicPrivacyMask: !isOwnerOrAdmin);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(
        Guid userId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new ArgumentException("Full name is required.");
        }

        user.FullName = request.FullName.Trim();
        user.Profile ??= new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id
        };

        user.Profile.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? user.FullName
            : request.DisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
        {
            user.Profile.AvatarUrl = request.AvatarUrl.Trim();
        }

        var normalizedCampus = CampusNormalizer.NormalizeOrNull(request.Campus);
        if (normalizedCampus is not null)
        {
            user.Profile.Campus = normalizedCampus;
        }

        user.Profile.StudentCode = request.StudentCode?.Trim() ?? string.Empty;
        user.Profile.GraduationYear = request.GraduationYear;
        user.Profile.Major = request.Major?.Trim() ?? string.Empty;
        user.Profile.CurrentCompany = request.CurrentCompany?.Trim() ?? string.Empty;
        user.Profile.JobTitle = request.JobTitle?.Trim() ?? string.Empty;
        user.Profile.Bio = request.Bio?.Trim() ?? string.Empty;
        user.Profile.Skills = request.Skills?.Trim() ?? string.Empty;
        user.Profile.LinkedInUrl = request.LinkedInUrl?.Trim() ?? string.Empty;
        user.Profile.GitHubUrl = request.GitHubUrl?.Trim() ?? string.Empty;
        user.Profile.PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(request.GoogleCalendarUrl))
        {
            user.Profile.GoogleCalendarUrl = request.GoogleCalendarUrl.Trim();
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Updated profile for user {UserId}", userId);
        return user.ToResponse();
    }

    public async Task<UserProfileResponse> UpdateAvatarAsync(
        Guid userId,
        UpdateAvatarRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        user.Profile ??= new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DisplayName = user.FullName
        };

        if (string.IsNullOrWhiteSpace(request.AvatarUrl))
        {
            throw new ArgumentException("Avatar URL is required.");
        }

        user.Profile.AvatarUrl = request.AvatarUrl.Trim();
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Updated avatar for user {UserId}", userId);
        return user.ToResponse();
    }

    public async Task<UserProfileResponse> UpdateCalendarLinkAsync(
        Guid userId,
        UpdateCalendarLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        user.Profile ??= new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DisplayName = user.FullName
        };

        if (string.IsNullOrWhiteSpace(request.GoogleCalendarUrl))
        {
            throw new ArgumentException("Google Calendar URL is required.");
        }

        user.Profile.GoogleCalendarUrl = request.GoogleCalendarUrl.Trim();
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Updated Google Calendar link for user {UserId}", userId);
        return user.ToResponse();
    }

    public async Task<UserProfileResponse> UpdatePrivacySettingsAsync(
        Guid userId,
        UpdatePrivacySettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        user.Profile ??= new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DisplayName = user.FullName
        };

        user.Profile.ShowEmailPublicly = request.ShowEmailPublicly;
        user.Profile.ShowPhonePublicly = request.ShowPhonePublicly;
        user.Profile.ShowCompanyPublicly = request.ShowCompanyPublicly;
        user.Profile.ShowCalendarPublicly = request.ShowCalendarPublicly;

        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Updated privacy settings for user {UserId}", userId);
        return user.ToResponse();
    }

    public async Task<BlockedUserResponse> BlockUserAsync(
        Guid blockerUserId,
        Guid targetUserId,
        BlockUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (blockerUserId == targetUserId)
        {
            throw new ArgumentException("You cannot block yourself.");
        }

        var target = await _userRepository.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), targetUserId);

        var block = await _userRepository.BlockUserAsync(
            blockerUserId,
            targetUserId,
            request?.Reason?.Trim() ?? string.Empty,
            cancellationToken);

        return MapBlockedUser(block, target);
    }

    public async Task UnblockUserAsync(
        Guid blockerUserId,
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        var removed = await _userRepository.UnblockUserAsync(blockerUserId, targetUserId, cancellationToken);
        if (!removed)
        {
            throw new NotFoundException("BlockedUser", targetUserId);
        }
    }

    public async Task<IReadOnlyList<BlockedUserResponse>> GetBlockedUsersAsync(
        Guid blockerUserId,
        CancellationToken cancellationToken = default)
    {
        var list = await _userRepository.GetBlockedUsersAsync(blockerUserId, cancellationToken);
        var results = new List<BlockedUserResponse>(list.Count);
        foreach (var b in list)
        {
            var target = await _userRepository.GetByIdAsync(b.BlockedTargetUserId, cancellationToken);
            results.Add(MapBlockedUser(b, target));
        }
        return results;
    }

    private static BlockedUserResponse MapBlockedUser(BlockedUser b, User? target)
    {
        var avatar = target?.Profile?.AvatarUrl;
        if (!string.IsNullOrWhiteSpace(avatar) && avatar.Contains("cdn.falumni.local", StringComparison.OrdinalIgnoreCase))
        {
            avatar = string.Empty;
        }
        return new BlockedUserResponse
        {
            Id = b.Id,
            BlockerUserId = b.BlockerUserId,
            BlockedTargetUserId = b.BlockedTargetUserId,
            TargetFullName = target?.FullName,
            TargetEmail = target?.Profile?.ShowEmailPublicly == false ? null : target?.Email,
            TargetAvatarUrl = avatar,
            TargetRole = target?.Role,
            TargetMajor = target?.Profile?.Major,
            TargetCampus = target?.Profile?.Campus,
            Reason = b.Reason,
            CreatedAtUtc = b.CreatedAtUtc
        };
    }

    public async Task<PagedResponse<UserProfileResponse>> GetDirectoryPagedAsync(
        string? search,
        string? campus,
        int? graduationYear,
        string? major,
        string? company,
        string? status,
        string? role,
        int pageNumber,
        int pageSize,
        bool includeAdminFields = false,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _userRepository.GetDirectoryPagedAsync(
            search,
            campus,
            graduationYear,
            major,
            company,
            status,
            role,
            pageNumber,
            pageSize,
            cancellationToken);

        return new PagedResponse<UserProfileResponse>
        {
            Items = items.Select(u => u.ToResponse(applyPublicPrivacyMask: !includeAdminFields)).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<UserProfileResponse> SuspendUserAsync(
        Guid userId,
        SuspendUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Suspension reason is required.");
        }

        user.Status = "Suspended";
        user.SuspensionReason = request.Reason.Trim();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.RevokeAllUserRefreshTokensAsync(userId, cancellationToken);

        await _kafkaPublisher.PublishDomainEventAsync(
            topic: "user.suspended.v1",
            eventType: "user.suspended.v1",
            partitionKey: user.Id.ToString(),
            data: new
            {
                userId = user.Id,
                email = user.Email,
                reason = user.SuspensionReason,
                suspendedAtUtc = DateTime.UtcNow
            },
            cancellationToken: cancellationToken);

        _logger.LogWarning("Suspended user {UserId}. Reason: {Reason}", userId, user.SuspensionReason);
        return user.ToResponse();
    }

    public async Task<UserProfileResponse> UnsuspendUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        user.Status = "Active";
        user.SuspensionReason = null;
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Unsuspended user {UserId}", userId);
        return user.ToResponse();
    }

    public async Task<UserProfileResponse> UpdateUserRoleAsync(
        Guid userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        var (resolvedRole, resolvedAccountType) = AuthService.ResolveRoleAndAccountType(
            request.Role,
            request.AccountType,
            user.Profile?.GraduationYear);

        user.Role = resolvedRole;
        user.AccountType = resolvedAccountType;

        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Updated role for user {UserId} to {Role} ({AccountType})", userId, user.Role, user.AccountType);
        return user.ToResponse();
    }
}
