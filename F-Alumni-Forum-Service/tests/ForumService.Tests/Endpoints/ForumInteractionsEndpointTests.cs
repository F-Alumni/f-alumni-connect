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

public class ForumInteractionsEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string JwtSecret = CustomWebApplicationFactory.TestJwtSecret;
    private const string JwtIssuer = "https://identity.falumni.local";
    private const string JwtAudience = "falumni-services";

    public ForumInteractionsEndpointTests(CustomWebApplicationFactory factory)
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
            new(ClaimTypes.Name, $"Tester_{userId[..6]}"),
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
    public async Task Comments_NestedReplies_And_CascadeDelete_WorksEndToEnd()
    {
        var userA = Guid.NewGuid().ToString();
        var userB = Guid.NewGuid().ToString();
        var tokenA = GenerateTestJwt(userA);
        var tokenB = GenerateTestJwt(userB);

        // 1. User A creates post
        using var postReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts");
        postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        postReq.Content = JsonContent.Create(new CreatePostRequest
        {
            Title = "Test nested comment tree & cascade delete",
            Content = "Checking deep replies and safe parent deletion.",
            Major = "SE",
            Campus = "FU-HL"
        });
        var postResp = await _client.SendAsync(postReq);
        Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);
        var post = await postResp.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(post);

        // 2. User B adds level-1 comment
        using var c1Req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/posts/{post.Id}/comments");
        c1Req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        c1Req.Content = JsonContent.Create(new CreateCommentRequest { Content = "Level 1 comment" });
        var c1Resp = await _client.SendAsync(c1Req);
        Assert.Equal(HttpStatusCode.Created, c1Resp.StatusCode);
        var c1 = await c1Resp.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.NotNull(c1);

        // 3. User A adds level-2 reply to c1
        using var c2Req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/posts/{post.Id}/comments");
        c2Req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        c2Req.Content = JsonContent.Create(new CreateCommentRequest
        {
            Content = "Level 2 reply",
            ParentCommentId = c1.Id
        });
        var c2Resp = await _client.SendAsync(c2Req);
        Assert.Equal(HttpStatusCode.Created, c2Resp.StatusCode);
        var c2 = await c2Resp.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.NotNull(c2);

        // 4. User B adds level-3 reply to c2
        using var c3Req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/posts/{post.Id}/comments");
        c3Req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        c3Req.Content = JsonContent.Create(new CreateCommentRequest
        {
            Content = "Level 3 nested reply",
            ParentCommentId = c2.Id
        });
        var c3Resp = await _client.SendAsync(c3Req);
        Assert.Equal(HttpStatusCode.Created, c3Resp.StatusCode);

        // 5. GET comments returns nested tree of 3 levels
        var treeResp = await _client.GetAsync($"/api/v1/posts/{post.Id}/comments");
        Assert.Equal(HttpStatusCode.OK, treeResp.StatusCode);
        var tree = await treeResp.Content.ReadFromJsonAsync<List<CommentResponse>>();
        Assert.NotNull(tree);
        Assert.Single(tree);
        Assert.Single(tree[0].Replies);
        Assert.Single(tree[0].Replies[0].Replies);
        Assert.Equal("Level 3 nested reply", tree[0].Replies[0].Replies[0].Content);

        // 6. User B deletes parent comment c1 -> must cleanly remove all 3 comments and decrement CommentCount to 0!
        using var delReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/comments/{c1.Id}");
        delReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var delResp = await _client.SendAsync(delReq);
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        var treeAfter = await _client.GetFromJsonAsync<List<CommentResponse>>($"/api/v1/posts/{post.Id}/comments");
        Assert.NotNull(treeAfter);
        Assert.Empty(treeAfter);

        var postAfter = await _client.GetFromJsonAsync<PostResponse>($"/api/v1/posts/{post.Id}");
        Assert.NotNull(postAfter);
        Assert.Equal(0, postAfter.CommentCount);
    }

    [Fact]
    public async Task Reaction_And_Bookmark_Syncs_Flags_And_Creates_Notification()
    {
        var authorId = Guid.NewGuid().ToString();
        var likerId = Guid.NewGuid().ToString();
        var authorToken = GenerateTestJwt(authorId);
        var likerToken = GenerateTestJwt(likerId);

        // 1. Author creates post
        using var postReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts");
        postReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        postReq.Content = JsonContent.Create(new CreatePostRequest
        {
            Title = "Test heart reaction & bookmark state",
            Content = "Verify HasLiked and IsBookmarked flags are persisted.",
            Major = "AI",
            Campus = "FU-HCM"
        });
        var postResp = await _client.SendAsync(postReq);
        var post = await postResp.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(post);

        // 2. Liker likes & bookmarks the post
        using var reactReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/posts/{post.Id}/react");
        reactReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", likerToken);
        var reactResp = await _client.SendAsync(reactReq);
        Assert.Equal(HttpStatusCode.OK, reactResp.StatusCode);

        using var bookmarkReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/posts/{post.Id}/bookmark");
        bookmarkReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", likerToken);
        var bookmarkResp = await _client.SendAsync(bookmarkReq);
        Assert.Equal(HttpStatusCode.OK, bookmarkResp.StatusCode);

        // 3. Liker gets post detail -> both HasLiked & IsBookmarked must be true
        using var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post.Id}");
        getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", likerToken);
        var getResp = await _client.SendAsync(getReq);
        var detail = await getResp.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(detail);
        Assert.True(detail.HasLiked);
        Assert.True(detail.IsBookmarked);
        Assert.Equal(1, detail.LikeCount);

        // 4. Author received POST_REACTION notification
        using var notifReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/notifications");
        notifReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        var notifResp = await _client.SendAsync(notifReq);
        var notifs = await notifResp.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>();
        Assert.NotNull(notifs);
        Assert.Contains(notifs.Items, n => n.Type == "POST_REACTION" && n.PostId == post.Id);

        // 5. Check /api/v1/bookmarks/me returns post with HasLiked & IsBookmarked
        using var bookmarksReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bookmarks/me");
        bookmarksReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", likerToken);
        var bookmarksResp = await _client.SendAsync(bookmarksReq);
        Assert.Equal(HttpStatusCode.OK, bookmarksResp.StatusCode);
        var bookmarks = await bookmarksResp.Content.ReadFromJsonAsync<PagedResponse<PostResponse>>();
        Assert.NotNull(bookmarks);
        Assert.Contains(bookmarks.Items, p => p.Id == post.Id && p.HasLiked && p.IsBookmarked);

        // 6. Unlike toggles LikeCount back to 0 and HasLiked to false
        using var unlikeReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/posts/{post.Id}/react");
        unlikeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", likerToken);
        var unlikeResp = await _client.SendAsync(unlikeReq);
        Assert.Equal(HttpStatusCode.OK, unlikeResp.StatusCode);

        using var getReq2 = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/posts/{post.Id}");
        getReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", likerToken);
        var detail2 = await (await _client.SendAsync(getReq2)).Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(detail2);
        Assert.False(detail2.HasLiked);
        Assert.Equal(0, detail2.LikeCount);
    }

    [Fact]
    public async Task Post_Update_WithImagesAndTags_And_AcceptAnswer_And_ReplyNotifications_WorkEndToEnd()
    {
        var authorId = Guid.NewGuid().ToString();
        var userB = Guid.NewGuid().ToString();
        var userC = Guid.NewGuid().ToString();
        var authorToken = GenerateTestJwt(authorId);
        var tokenB = GenerateTestJwt(userB);
        var tokenC = GenerateTestJwt(userC);

        // 1. Author creates post with initial image & tag
        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/posts");
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        createReq.Content = JsonContent.Create(new CreatePostRequest
        {
            Title = "Initial Post Title for Update Test",
            Content = "Initial content with more than 10 characters.",
            PostType = "Question",
            Major = "SE",
            Campus = "FU-HL",
            ImageUrls = new List<string> { "https://example.com/old.jpg" },
            Tags = new List<string> { "oldtag" }
        });
        var createResp = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var post = await createResp.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(post);

        // 2. Author updates post with new images and tags (previously threw DbUpdateConcurrencyException!)
        using var updateReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/posts/{post.Id}");
        updateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        updateReq.Content = JsonContent.Create(new UpdatePostRequest
        {
            Title = "Updated Post Title Successfully",
            Content = "Updated content with new images and tags.",
            PostType = "Question",
            Major = "SE",
            Campus = "FU-HL",
            ImageUrls = new List<string> { "https://example.com/new1.jpg", "https://example.com/new2.jpg" },
            Tags = new List<string> { "newtag1", "newtag2" }
        });
        var updateResp = await _client.SendAsync(updateReq);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        var updated = await updateResp.Content.ReadFromJsonAsync<PostResponse>();
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Images.Count);
        Assert.Equal(2, updated.Tags.Count);
        Assert.Contains("newtag1", updated.Tags);

        // 3. User B comments, User C replies to User B -> User B gets COMMENT_REPLY, Author gets POST_COMMENT
        using var c1Req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/posts/{post.Id}/comments");
        c1Req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        c1Req.Content = JsonContent.Create(new CreateCommentRequest { Content = "Answer from User B" });
        var c1 = await (await _client.SendAsync(c1Req)).Content.ReadFromJsonAsync<CommentResponse>();
        Assert.NotNull(c1);

        using var c2Req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/posts/{post.Id}/comments");
        c2Req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenC);
        c2Req.Content = JsonContent.Create(new CreateCommentRequest { Content = "Reply from User C", ParentCommentId = c1.Id });
        var c2Resp = await _client.SendAsync(c2Req);
        Assert.Equal(HttpStatusCode.Created, c2Resp.StatusCode);

        using var bNotifReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/notifications");
        bNotifReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var bNotifs = await (await _client.SendAsync(bNotifReq)).Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>();
        Assert.NotNull(bNotifs);
        Assert.Contains(bNotifs.Items, n => n.Type == "COMMENT_REPLY" && n.PostId == post.Id);

        // 4. Author accepts User B's answer twice -> only 1 COMMENT_ACCEPTED notification, IsAnswered becomes true
        using var acceptReq1 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/comments/{c1.Id}/accept");
        acceptReq1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(acceptReq1)).StatusCode);

        using var acceptReq2 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/comments/{c1.Id}/accept");
        acceptReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(acceptReq2)).StatusCode);

        using var bNotifReq2 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/notifications");
        bNotifReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var bNotifs2 = await (await _client.SendAsync(bNotifReq2)).Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>();
        Assert.NotNull(bNotifs2);
        Assert.Equal(1, bNotifs2.Items.Count(n => n.Type == "COMMENT_ACCEPTED" && n.CommentId == c1.Id));

        // 5. Deleting the accepted comment c1 cascades to c2 and resets IsAnswered to false
        using var delCommentReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/comments/{c1.Id}");
        delCommentReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delCommentReq)).StatusCode);

        var postAfterCommentDelete = await _client.GetFromJsonAsync<PostResponse>($"/api/v1/posts/{post.Id}");
        Assert.NotNull(postAfterCommentDelete);
        Assert.False(postAfterCommentDelete.IsAnswered);
        Assert.Equal(0, postAfterCommentDelete.CommentCount);

        // 6. Deleting the post returns 204 NoContent and subsequent GET returns 404
        using var delPostReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/posts/{post.Id}");
        delPostReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authorToken);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(delPostReq)).StatusCode);

        var getDeletedResp = await _client.GetAsync($"/api/v1/posts/{post.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResp.StatusCode);
    }
}
