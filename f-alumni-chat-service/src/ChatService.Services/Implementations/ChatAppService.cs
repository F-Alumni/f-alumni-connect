using ChatService.Repositories.Entities;
using ChatService.Repositories.Interfaces;
using ChatService.Services.DTOs.Common;
using ChatService.Services.DTOs.Requests;
using ChatService.Services.DTOs.Responses;
using ChatService.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatService.Services.Implementations;

public class ChatAppService : IChatAppService
{
    private readonly IChatRepository _chatRepository;
    private readonly IUserProfileLookupService _profileLookupService;
    private readonly IChatKafkaPublisher _kafkaPublisher;
    private readonly ILogger<ChatAppService> _logger;

    public ChatAppService(
        IChatRepository chatRepository,
        IUserProfileLookupService profileLookupService,
        IChatKafkaPublisher kafkaPublisher,
        ILogger<ChatAppService> logger)
    {
        _chatRepository = chatRepository;
        _profileLookupService = profileLookupService;
        _kafkaPublisher = kafkaPublisher;
        _logger = logger;
    }

    public async Task<ConversationResponse> CreateOrGetConversationAsync(
        Guid creatorUserId,
        CreateConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        var participantIds = new HashSet<Guid> { creatorUserId };
        if (request.TargetUserId.HasValue && request.TargetUserId.Value != Guid.Empty)
        {
            participantIds.Add(request.TargetUserId.Value);
        }

        foreach (var id in request.ParticipantUserIds.Where(x => x != Guid.Empty))
        {
            participantIds.Add(id);
        }

        if (participantIds.Count < 2)
        {
            throw new ArgumentException("A conversation requires at least 2 distinct participants.");
        }

        var isGroup = request.IsGroup || participantIds.Count > 2;

        if (!isGroup && participantIds.Count == 2)
        {
            var pair = participantIds.ToArray();
            var existing = await _chatRepository.GetDirectConversationAsync(pair[0], pair[1], cancellationToken);
            if (existing is not null)
            {
                return MapToConversationResponse(existing);
            }
        }

        var conversationId = Guid.NewGuid();
        var participants = new List<ConversationParticipant>();

        foreach (var userId in participantIds)
        {
            var profile = await _profileLookupService.GetParticipantSummaryAsync(
                conversationId,
                userId,
                strictGrpc: false,
                cancellationToken);

            participants.Add(new ConversationParticipant
            {
                ConversationId = conversationId,
                UserId = userId,
                DisplayName = profile.DisplayName,
                AvatarUrl = profile.AvatarUrl,
                JoinedAtUtc = DateTime.UtcNow
            });
        }

        var title = !string.IsNullOrWhiteSpace(request.Title)
            ? request.Title.Trim()
            : string.Join(", ", participants.Select(p => p.DisplayName));

        var conversation = new Conversation
        {
            Id = conversationId,
            Title = title,
            IsGroup = isGroup,
            CreatedByUserId = creatorUserId,
            CreatedAtUtc = DateTime.UtcNow,
            LastMessageAtUtc = DateTime.UtcNow,
            Participants = participants
        };

        await _chatRepository.CreateConversationAsync(conversation, cancellationToken);

        _logger.LogInformation("Created conversation {ConversationId} with {Count} participants.", conversation.Id, participants.Count);
        return MapToConversationResponse(conversation);
    }

    public async Task<ConversationResponse?> GetConversationByIdAsync(
        Guid conversationId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _chatRepository.GetConversationByIdAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            return null;
        }

        EnsureParticipant(conversation, currentUserId);
        return MapToConversationResponse(conversation);
    }

    public async Task<PagedResponse<ConversationResponse>> GetUserConversationsAsync(
        Guid userId,
        bool includeHidden = false,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var allConversations = await _chatRepository.GetUserConversationsAsync(
            userId, includeHidden, cancellationToken);

        var totalCount = allConversations.Count;
        var pagedItems = allConversations
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToConversationResponse)
            .ToList();

        return new PagedResponse<ConversationResponse>(
            pagedItems,
            totalCount,
            page,
            pageSize);
    }

    public async Task<ChatMessageResponse> SendMessageAsync(
        Guid conversationId,
        Guid senderUserId,
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.AttachmentUrl))
        {
            throw new ArgumentException("Message must contain either text content or an attachment URL.");
        }

        var conversation = await GetExistingConversationOrThrowAsync(conversationId, cancellationToken);
        var senderParticipant = EnsureParticipant(conversation, senderUserId);
        var senderProfile = await _profileLookupService.GetParticipantSummaryAsync(
            conversationId,
            senderUserId,
            strictGrpc: false,
            cancellationToken);

        var displayName = !string.IsNullOrWhiteSpace(senderParticipant.DisplayName)
            ? senderParticipant.DisplayName
            : senderProfile.DisplayName;
        var avatarUrl = !string.IsNullOrWhiteSpace(senderParticipant.AvatarUrl)
            ? senderParticipant.AvatarUrl
            : senderProfile.AvatarUrl;

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderUserId,
            Content = request.Content?.Trim() ?? string.Empty,
            AttachmentUrl = request.AttachmentUrl?.Trim(),
            AttachmentType = request.AttachmentType?.Trim() ?? (!string.IsNullOrWhiteSpace(request.AttachmentUrl) ? "Image" : null),
            Status = "Sent",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _chatRepository.AddMessageAsync(message, cancellationToken);

        var response = new ChatMessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderUserId = message.SenderId,
            SenderDisplayName = displayName,
            SenderAvatarUrl = avatarUrl,
            Content = message.Content,
            AttachmentUrl = message.AttachmentUrl,
            AttachmentType = message.AttachmentType,
            Status = message.Status,
            ReadAt = message.ReadAtUtc,
            SentAt = message.CreatedAtUtc
        };

        var recipientIds = conversation.Participants
            .Where(p => p.UserId != senderUserId && !p.IsMuted)
            .Select(p => p.UserId)
            .ToList();

        await _kafkaPublisher.PublishMessageSentAsync(response, recipientIds, cancellationToken);
        return response;
    }

    public async Task<PagedResponse<ChatMessageResponse>> GetMessagesAsync(
        Guid conversationId,
        Guid currentUserId,
        string? search = null,
        int page = 1,
        int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var conversation = await GetExistingConversationOrThrowAsync(conversationId, cancellationToken);
        EnsureParticipant(conversation, currentUserId);

        var participantMap = BuildParticipantMap(conversation.Participants);
        var (items, totalCount) = await _chatRepository.GetMessagesPagedAsync(
            conversationId, search, page, pageSize, cancellationToken);

        var mapped = items.Select(m =>
        {
            participantMap.TryGetValue(m.SenderId, out var sender);
            return new ChatMessageResponse
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderUserId = m.SenderId,
                SenderDisplayName = sender?.DisplayName ?? string.Empty,
                SenderAvatarUrl = sender?.AvatarUrl ?? string.Empty,
                Content = m.Content,
                AttachmentUrl = m.AttachmentUrl,
                AttachmentType = m.AttachmentType,
                Status = m.Status,
                ReadAt = m.ReadAtUtc,
                SentAt = m.CreatedAtUtc
            };
        }).ToList();

        return new PagedResponse<ChatMessageResponse>(mapped, totalCount, page, pageSize);
    }

    public async Task<MarkReadResponse> MarkConversationReadAsync(
        Guid conversationId,
        Guid readerUserId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await GetExistingConversationOrThrowAsync(conversationId, cancellationToken);
        EnsureParticipant(conversation, readerUserId);

        var markedCount = await _chatRepository.MarkMessagesAsReadAsync(conversationId, readerUserId, cancellationToken);
        var now = DateTime.UtcNow;

        return new MarkReadResponse
        {
            ConversationId = conversationId,
            ReaderUserId = readerUserId,
            MarkedCount = markedCount,
            ReadAtUtc = now
        };
    }

    public async Task<ParticipantDto> UpdateConversationSettingsAsync(
        Guid conversationId,
        Guid userId,
        UpdateConversationSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var conversation = await GetExistingConversationOrThrowAsync(conversationId, cancellationToken);
        var participant = EnsureParticipant(conversation, userId);

        if (request.IsMuted.HasValue)
        {
            participant.IsMuted = request.IsMuted.Value;
        }

        if (request.IsHidden.HasValue)
        {
            participant.IsHidden = request.IsHidden.Value;
        }

        await _chatRepository.UpdateParticipantAsync(participant, cancellationToken);

        return new ParticipantDto
        {
            UserId = participant.UserId,
            DisplayName = participant.DisplayName,
            AvatarUrl = participant.AvatarUrl,
            Status = "Active",
            IsMuted = participant.IsMuted,
            IsHidden = participant.IsHidden,
            LastReadAt = participant.LastReadAtUtc,
            JoinedAt = participant.JoinedAtUtc
        };
    }

    public async Task EnsureParticipantAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await GetExistingConversationOrThrowAsync(conversationId, cancellationToken);
        EnsureParticipant(conversation, userId);
    }

    private async Task<Conversation> GetExistingConversationOrThrowAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await _chatRepository.GetConversationByIdAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            throw new KeyNotFoundException($"Conversation '{conversationId}' was not found.");
        }

        return conversation;
    }

    private static ConversationParticipant EnsureParticipant(Conversation conversation, Guid userId)
    {
        var participant = conversation.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant is null)
        {
            throw new UnauthorizedAccessException(
                $"User '{userId}' is not a participant in conversation '{conversation.Id}'.");
        }

        return participant;
    }

    private static Dictionary<Guid, ConversationParticipant> BuildParticipantMap(IEnumerable<ConversationParticipant> participants)
        => participants.GroupBy(p => p.UserId).ToDictionary(g => g.Key, g => g.First());

    private static ConversationResponse MapToConversationResponse(Conversation conversation)
    {
        var participantMap = BuildParticipantMap(conversation.Participants);
        var latestMsg = conversation.Messages.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();

        ChatMessageResponse? lastMessageDto = null;
        if (latestMsg is not null)
        {
            participantMap.TryGetValue(latestMsg.SenderId, out var sender);
            lastMessageDto = new ChatMessageResponse
            {
                Id = latestMsg.Id,
                ConversationId = latestMsg.ConversationId,
                SenderUserId = latestMsg.SenderId,
                SenderDisplayName = sender?.DisplayName ?? string.Empty,
                SenderAvatarUrl = sender?.AvatarUrl ?? string.Empty,
                Content = latestMsg.Content,
                AttachmentUrl = latestMsg.AttachmentUrl,
                AttachmentType = latestMsg.AttachmentType,
                Status = latestMsg.Status,
                ReadAt = latestMsg.ReadAtUtc,
                SentAt = latestMsg.CreatedAtUtc
            };
        }

        return new ConversationResponse
        {
            Id = conversation.Id,
            Title = conversation.Title,
            IsGroup = conversation.IsGroup,
            CreatedAt = conversation.CreatedAtUtc,
            LastMessageAt = conversation.LastMessageAtUtc,
            LastMessage = lastMessageDto,
            Participants = conversation.Participants.Select(p => new ParticipantDto
            {
                UserId = p.UserId,
                DisplayName = p.DisplayName,
                AvatarUrl = p.AvatarUrl,
                Status = "Active",
                IsMuted = p.IsMuted,
                IsHidden = p.IsHidden,
                LastReadAt = p.LastReadAtUtc,
                JoinedAt = p.JoinedAtUtc
            }).ToList()
        };
    }
}
