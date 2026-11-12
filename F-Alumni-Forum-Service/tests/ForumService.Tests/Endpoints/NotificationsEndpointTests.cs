using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ForumService.Services.DTOs.Common;
using ForumService.Services.DTOs.Requests;
using ForumService.Services.DTOs.Responses;
using Xunit;

namespace ForumService.Tests.Endpoints;

public class NotificationsEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string JwtSecret = CustomWebApplicationFactory.TestJwtSecret;
    private const string JwtIssuer = "https://identity.falumni.local";
    private const string JwtAudience = "falumni-services";

    public NotificationsEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string GenerateTestJwt(string userId, string role = "USER")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecret);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new("nameid", userId),
            new(ClaimTypes.Name, $"User_{userId[..6]}"),
            new("name", $"User_{userId[..6]}"),
            new(ClaimTypes.Role, role),
            new("role", role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }

    [Fact]
    public async Task Notifications_Unauthenticated_Returns401Unauthorized()
    {
        // Act (No token)
        var response = await _client.GetAsync("/api/v1/notifications");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Notifications_Flow_FullCycle()
    {
        var authorId = Guid.NewGuid().ToString();
        var commenterId = Guid.NewGuid().ToString();

        var authorToken = GenerateTestJwt(authorId);
        var commenterToken = GenerateTestJwt(commenterId);

        // 1. Author creates a post
        using var createPostReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts");
        createPostReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        createPostReq.Content = JsonContent.Create(new CreatePostRequest
        {
            Title = "Bài viết thảo luận notification " + Guid.NewGuid().ToString()[..6],
            Content = "Nội dung bài viết để kiểm tra tự động tạo thông báo",
            Major = "SE",
            Campus = "FU-HL"
        });
        var createPostResp = await _client.SendAsync(createPostReq);
        Assert.Equal(HttpStatusCode.Created, createPostResp.StatusCode);
        var post = await createPostResp.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(post);

        // Author unread count before comment
        using var unreadReq1 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/notifications/unread-count");
        unreadReq1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        var unreadResp1 = await _client.SendAsync(unreadReq1);
        Assert.Equal(HttpStatusCode.OK, unreadResp1.StatusCode);
        var unreadCount1 = await unreadResp1.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        var initialCount = unreadCount1?["unreadCount"] ?? 0;

        // 2. Commenter adds a comment
        using var commentReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/posts/{post.Id}/comments");
        commentReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", commenterToken);
        commentReq.Content = JsonContent.Create(new CreateCommentRequest
        {
            Content = "Bình luận thử nghiệm kích hoạt notification"
        });
        var commentResp = await _client.SendAsync(commentReq);
        Assert.Equal(HttpStatusCode.Created, commentResp.StatusCode);

        // 3. Author unread count should increment by 1
        using var unreadReq2 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/notifications/unread-count");
        unreadReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        var unreadResp2 = await _client.SendAsync(unreadReq2);
        Assert.Equal(HttpStatusCode.OK, unreadResp2.StatusCode);
        var unreadCount2 = await unreadResp2.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.Equal(initialCount + 1, unreadCount2?["unreadCount"]);

        // 4. Author retrieves notifications list
        using var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/notifications?page=1&pageSize=10");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        var listResp = await _client.SendAsync(listReq);
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var pagedNotifs = await listResp.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>();
        Assert.NotNull(pagedNotifs);
        Assert.NotEmpty(pagedNotifs.Items);
        var latestNotif = pagedNotifs.Items[0];
        Assert.Equal("POST_COMMENT", latestNotif.Type);
        Assert.Equal(post.Id, latestNotif.PostId);
        Assert.False(latestNotif.IsRead);

        // 5. Author marks notification as read
        using var markReadReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/notifications/{latestNotif.Id}/read");
        markReadReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        var markReadResp = await _client.SendAsync(markReadReq);
        Assert.Equal(HttpStatusCode.OK, markReadResp.StatusCode);

        // 6. Author marks all as read
        using var readAllReq = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/notifications/read-all");
        readAllReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        var readAllResp = await _client.SendAsync(readAllReq);
        Assert.Equal(HttpStatusCode.OK, readAllResp.StatusCode);

        // 7. Author deletes notification
        using var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/notifications/{latestNotif.Id}");
        deleteReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        var deleteResp = await _client.SendAsync(deleteReq);
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }
}
