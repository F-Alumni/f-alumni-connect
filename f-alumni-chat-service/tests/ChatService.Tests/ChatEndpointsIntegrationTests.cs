using System.Net;
using System.Net.Http.Json;
using ChatService.Services.DTOs.Common;
using ChatService.Services.DTOs.Requests;
using ChatService.Services.DTOs.Responses;

namespace ChatService.Tests.Endpoints;

public class ChatEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly Guid SeededConversationId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserC = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public ChatEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Ping_And_Health_ReturnOk()
    {
        var client = _factory.CreateClient();

        var pingRes = await client.GetAsync("/ping");
        Assert.Equal(HttpStatusCode.OK, pingRes.StatusCode);

        var apiPingRes = await client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.OK, apiPingRes.StatusCode);

        var healthRes = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthRes.StatusCode);
    }

    [Fact]
    public async Task ChatEndpoints_WithoutToken_Return401Unauthorized()
    {
        var anonymousClient = _factory.CreateClient();

        var responses = new List<HttpResponseMessage>
        {
            await anonymousClient.GetAsync("/api/v1/chats/conversations"),
            await anonymousClient.PostAsJsonAsync("/api/v1/chats/conversations", new CreateConversationRequest { TargetUserId = UserB }),
            await anonymousClient.GetAsync($"/api/v1/chats/conversations/{SeededConversationId}"),
            await anonymousClient.GetAsync($"/api/v1/chats/conversations/{SeededConversationId}/messages"),
            await anonymousClient.PostAsJsonAsync($"/api/v1/chats/conversations/{SeededConversationId}/messages", new SendMessageRequest { Content = "Unauthenticated" }),
            await anonymousClient.PostAsync($"/api/v1/chats/conversations/{SeededConversationId}/read", null),
            await anonymousClient.PutAsJsonAsync($"/api/v1/chats/conversations/{SeededConversationId}/settings", new UpdateConversationSettingsRequest { IsMuted = true }),
            await anonymousClient.GetAsync($"/api/v1/chats/conversations/{SeededConversationId}/participants/{UserA}"),
            await anonymousClient.PostAsync("/hubs/chat/negotiate?negotiateVersion=1", null)
        };

        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            response.Dispose();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-valid-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task ChatEndpoints_WithMissingOrInvalidUserIdClaim_Return401Unauthorized(string? claimUserId)
    {
        var client = _factory.CreateAuthenticatedClient(claimUserId);

        var listRes = await client.GetAsync("/api/v1/chats/conversations");
        Assert.Equal(HttpStatusCode.Unauthorized, listRes.StatusCode);

        var detailRes = await client.GetAsync($"/api/v1/chats/conversations/{SeededConversationId}");
        Assert.Equal(HttpStatusCode.Unauthorized, detailRes.StatusCode);
    }

    [Fact]
    public async Task NonMember_AccessingSeededConversation_Returns403Forbidden_EvenWithSpoofedUserId()
    {
        var nonMemberClient = _factory.CreateAuthenticatedClient(UserC);
        nonMemberClient.DefaultRequestHeaders.Add("X-User-Id", UserA.ToString());

        var detailRes = await nonMemberClient.GetAsync($"/api/v1/chats/conversations/{SeededConversationId}");
        Assert.Equal(HttpStatusCode.Forbidden, detailRes.StatusCode);

        var messagesRes = await nonMemberClient.GetAsync($"/api/v1/chats/conversations/{SeededConversationId}/messages");
        Assert.Equal(HttpStatusCode.Forbidden, messagesRes.StatusCode);

        var sendRes = await nonMemberClient.PostAsJsonAsync(
            $"/api/v1/chats/conversations/{SeededConversationId}/messages",
            new SendMessageRequest { SenderUserId = UserA, Content = "Attempted IDOR message" });
        Assert.Equal(HttpStatusCode.Forbidden, sendRes.StatusCode);

        var readRes = await nonMemberClient.PostAsync(
            $"/api/v1/chats/conversations/{SeededConversationId}/read?userId={UserA}",
            null);
        Assert.Equal(HttpStatusCode.Forbidden, readRes.StatusCode);

        var settingsRes = await nonMemberClient.PutAsJsonAsync(
            $"/api/v1/chats/conversations/{SeededConversationId}/settings?userId={UserA}",
            new UpdateConversationSettingsRequest { IsMuted = true });
        Assert.Equal(HttpStatusCode.Forbidden, settingsRes.StatusCode);

        var participantRes = await nonMemberClient.GetAsync(
            $"/api/v1/chats/conversations/{SeededConversationId}/participants/{UserA}");
        Assert.Equal(HttpStatusCode.Forbidden, participantRes.StatusCode);
    }

    [Fact]
    public async Task GetConversations_IgnoresSpoofedUserIdQueryAndHeader()
    {
        var isolatedUserId = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(isolatedUserId);
        client.DefaultRequestHeaders.Add("X-User-Id", UserA.ToString());

        var listRes = await client.GetAsync($"/api/v1/chats/conversations?userId={UserA}");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);

        var body = await listRes.Content.ReadFromJsonAsync<PagedResponse<ConversationResponse>>();
        Assert.NotNull(body);
        Assert.DoesNotContain(body.Items, c => c.Id == SeededConversationId);
    }

    [Fact]
    public async Task GetParticipantProfile_ValidSeededUser_Returns200WithGrpcMetadata()
    {
        var client = _factory.CreateAuthenticatedClient(UserA);

        var response = await client.GetAsync($"/api/v1/chats/conversations/{SeededConversationId}/participants/{UserA}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ConversationParticipantResponse>();
        Assert.NotNull(body);
        Assert.Equal(UserA, body.UserId);
        Assert.Equal("Nguyen Van A", body.DisplayName);
        Assert.Equal("gRPC (HTTP/2 Protobuf)", body.ResolvedVia);
    }

    [Fact]
    public async Task GetParticipantProfile_UnknownUser_Returns404ProblemDetails()
    {
        var client = _factory.CreateAuthenticatedClient(UserA);
        var unknownId = Guid.Empty;

        var response = await client.GetAsync($"/api/v1/chats/conversations/{SeededConversationId}/participants/{unknownId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FullChatLifecycle_Create_SendMessage_Search_Read_And_Mute_WorksEndToEnd()
    {
        var clientA = _factory.CreateAuthenticatedClient(UserA);
        var clientC = _factory.CreateAuthenticatedClient(UserC);

        // 1. CH-01: Create 1-1 Conversation between authenticated UserA and UserC
        var createReq = new CreateConversationRequest
        {
            TargetUserId = UserC,
            IsGroup = false
        };

        var createRes = await clientA.PostAsJsonAsync("/api/v1/chats/conversations", createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var conversation = await createRes.Content.ReadFromJsonAsync<ConversationResponse>();
        Assert.NotNull(conversation);
        Assert.Equal(2, conversation.Participants.Count);

        // Verify participant can read conversation details
        var detailRes = await clientC.GetAsync($"/api/v1/chats/conversations/{conversation.Id}");
        Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);

        // 2. CH-02 & CH-07: Send message as UserA (SenderUserId in payload is ignored in favor of JWT identity)
        var sendReq = new SendMessageRequest
        {
            SenderUserId = UserC,
            Content = "Xin chào anh Lê Hoàng C, em muốn hỏi về cơ hội thực tập AI tại FPT Software!",
            AttachmentUrl = "https://cdn.falumni.local/chat/cv-nguyenvana.png",
            AttachmentType = "Image"
        };

        var sendRes = await clientA.PostAsJsonAsync($"/api/v1/chats/conversations/{conversation.Id}/messages", sendReq);
        Assert.Equal(HttpStatusCode.Created, sendRes.StatusCode);

        var sentMsg = await sendRes.Content.ReadFromJsonAsync<ChatMessageResponse>();
        Assert.NotNull(sentMsg);
        Assert.Equal(UserA, sentMsg.SenderUserId);
        Assert.Equal("Sent", sentMsg.Status);
        Assert.Equal("https://cdn.falumni.local/chat/cv-nguyenvana.png", sentMsg.AttachmentUrl);

        // 3. CH-04 & CH-08: Search messages in conversation as participant UserC
        var searchRes = await clientC.GetAsync($"/api/v1/chats/conversations/{conversation.Id}/messages?search=FPT%20Software");
        Assert.Equal(HttpStatusCode.OK, searchRes.StatusCode);

        var pagedMessages = await searchRes.Content.ReadFromJsonAsync<PagedResponse<ChatMessageResponse>>();
        Assert.NotNull(pagedMessages);
        Assert.Single(pagedMessages.Items);

        // 4. CH-05: Mark conversation as read by authenticated UserC
        var readRes = await clientC.PostAsync($"/api/v1/chats/conversations/{conversation.Id}/read", null);
        Assert.Equal(HttpStatusCode.OK, readRes.StatusCode);

        var readBody = await readRes.Content.ReadFromJsonAsync<MarkReadResponse>();
        Assert.NotNull(readBody);
        Assert.Equal(UserC, readBody.ReaderUserId);
        Assert.Equal(1, readBody.MarkedCount);

        // 5. CH-09: Mute conversation notifications for authenticated UserC
        var settingsReq = new UpdateConversationSettingsRequest
        {
            IsMuted = true,
            IsHidden = false
        };

        var settingsRes = await clientC.PutAsJsonAsync(
            $"/api/v1/chats/conversations/{conversation.Id}/settings",
            settingsReq);
        Assert.Equal(HttpStatusCode.OK, settingsRes.StatusCode);

        var updatedParticipant = await settingsRes.Content.ReadFromJsonAsync<ParticipantDto>();
        Assert.NotNull(updatedParticipant);
        Assert.Equal(UserC, updatedParticipant.UserId);
        Assert.True(updatedParticipant.IsMuted);

        // 6. CH-03: Get user conversations list for authenticated UserA
        var listRes = await clientA.GetAsync("/api/v1/chats/conversations");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var listBody = await listRes.Content.ReadFromJsonAsync<PagedResponse<ConversationResponse>>();
        Assert.NotNull(listBody);
        Assert.Contains(listBody.Items, c => c.Id == conversation.Id);
    }

    [Fact]
    public async Task UnknownConversationAndEmptyMessage_Return4xxNot500()
    {
        var client = _factory.CreateAuthenticatedClient(UserA);
        var missingId = Guid.NewGuid();

        var detailRes = await client.GetAsync($"/api/v1/chats/conversations/{missingId}");
        Assert.Equal(HttpStatusCode.NotFound, detailRes.StatusCode);

        var readRes = await client.PostAsync($"/api/v1/chats/conversations/{missingId}/read", null);
        Assert.Equal(HttpStatusCode.NotFound, readRes.StatusCode);

        var msgsRes = await client.GetAsync($"/api/v1/chats/conversations/{missingId}/messages?page=0&pageSize=-5");
        Assert.Equal(HttpStatusCode.NotFound, msgsRes.StatusCode);

        var settingsRes = await client.PutAsJsonAsync(
            $"/api/v1/chats/conversations/{missingId}/settings",
            new UpdateConversationSettingsRequest { IsMuted = true });
        Assert.Equal(HttpStatusCode.NotFound, settingsRes.StatusCode);

        var emptySendRes = await client.PostAsJsonAsync(
            $"/api/v1/chats/conversations/{missingId}/messages",
            new SendMessageRequest { Content = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, emptySendRes.StatusCode);
    }
}
