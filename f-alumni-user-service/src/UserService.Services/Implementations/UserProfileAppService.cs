using Microsoft.Extensions.Logging;
using UserService.Repositories.Entities;
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

        if (!string.IsNullOrWhiteSpace(request.Campus))
        {
            user.Profile.Campus = request.Campus.Trim();
        }

        user.Profile.StudentCode = request.StudentCode.Trim();
        user.Profile.GraduationYear = request.GraduationYear;
        user.Profile.Major = request.Major.Trim();
        user.Profile.CurrentCompany = request.CurrentCompany.Trim();
        user.Profile.JobTitle = request.JobTitle.Trim();
        user.Profile.Bio = request.Bio.Trim();
        user.Profile.Skills = request.Skills.Trim();
        user.Profile.LinkedInUrl = request.LinkedInUrl.Trim();
        user.Profile.GitHubUrl = request.GitHubUrl.Trim();
        user.Profile.PhoneNumber = request.PhoneNumber.Trim();

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

        _ = await _userRepository.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), targetUserId);

        var block = await _userRepository.BlockUserAsync(
            blockerUserId,
            targetUserId,
            request?.Reason?.Trim() ?? string.Empty,
            cancellationToken);

        return new BlockedUserResponse
        {
            Id = block.Id,
            BlockerUserId = block.BlockerUserId,
            BlockedTargetUserId = block.BlockedTargetUserId,
            Reason = block.Reason,
            CreatedAtUtc = block.CreatedAtUtc
        };
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
        return list.Select(b => new BlockedUserResponse
        {
            Id = b.Id,
            BlockerUserId = b.BlockerUserId,
            BlockedTargetUserId = b.BlockedTargetUserId,
            Reason = b.Reason,
            CreatedAtUtc = b.CreatedAtUtc
        }).ToList();
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
            Items = items.Select(u => u.ToResponse(applyPublicPrivacyMask: true)).ToList(),
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

        user.Role = request.Role.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(request.AccountType))
        {
            user.AccountType = request.AccountType.Trim();
        }

        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Updated role for user {UserId} to {Role} ({AccountType})", userId, user.Role, user.AccountType);
        return user.ToResponse();
    }
}
