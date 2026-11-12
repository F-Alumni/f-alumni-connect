using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AiService.Api.Data;
using AiService.Services.DTOs.Requests;
using AiService.Services.DTOs.Responses;
using Xunit;

namespace AiService.Tests.Endpoints;

public class AiSecurityEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string JwtSecret = CustomWebApplicationFactory.TestJwtSecret;
    private const string JwtIssuer = "https://identity.falumni.local";
    private const string JwtAudience = "falumni-services";

    public AiSecurityEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string GenerateTestJwt(string? userId, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new("role", role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (userId is not null)
        {
            claims.Add(new(JwtRegisteredClaimNames.Sub, userId));
            claims.Add(new(ClaimTypes.NameIdentifier, userId));
            claims.Add(new(JwtRegisteredClaimNames.Email, $"{userId}@fpt.edu.vn"));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        return request;
    }

    [Fact]
    public async Task Chat_Anonymous_Returns401Unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/ai/chat", new AiChatRequest
        {
            Question = "Học phí FPTU bao nhiêu?"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConversations_Anonymous_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/api/v1/ai/conversations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConversationById_Anonymous_Returns401Unauthorized()
    {
        var response = await _client.GetAsync($"/api/v1/ai/conversations/{AiDatabaseSeeder.Conversation1Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RecommendMentors_Anonymous_Returns401Unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/ai/mentors/recommend", new RecommendMentorRequest
        {
            Major = "SE"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RagIngest_Anonymous_Returns401Unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/ai/rag/ingest", new IngestRagRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task ProtectedAiEndpoints_MissingOrInvalidUserIdClaim_Return401Unauthorized(string? userIdClaim)
    {
        var token = GenerateTestJwt(userIdClaim, "USER");

        using var chatReq = CreateAuthorizedRequest(HttpMethod.Post, "/api/v1/ai/chat", token, new AiChatRequest { Question = "Hello?" });
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(chatReq)).StatusCode);

        using var convsReq = CreateAuthorizedRequest(HttpMethod.Get, "/api/v1/ai/conversations", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(convsReq)).StatusCode);

        using var convReq = CreateAuthorizedRequest(HttpMethod.Get, $"/api/v1/ai/conversations/{AiDatabaseSeeder.Conversation1Id}", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(convReq)).StatusCode);

        using var mentorReq = CreateAuthorizedRequest(HttpMethod.Post, "/api/v1/ai/mentors/recommend", token, new RecommendMentorRequest { Major = "SE" });
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(mentorReq)).StatusCode);
    }

    [Fact]
    public async Task AuthenticatedUser_CanChatAndListOwnConversations_WithoutSeeingOtherUsersConversations()
    {
        var userId = Guid.NewGuid().ToString();
        var userToken = GenerateTestJwt(userId, "USER");

        // 1. Ask a chat question as authenticated user
        using var chatReq = CreateAuthorizedRequest(HttpMethod.Post, "/api/v1/ai/chat", userToken, new AiChatRequest
        {
            Question = "Điều kiện tốt nghiệp ngành SE là gì?",
            Major = "SE"
        });
        var chatRes = await _client.SendAsync(chatReq);
        Assert.Equal(HttpStatusCode.OK, chatRes.StatusCode);

        var chatBody = await chatRes.Content.ReadFromJsonAsync<AiChatResponse>();
        Assert.NotNull(chatBody);
        Assert.NotEqual(Guid.Empty, chatBody.ConversationId);

        // 2. List own conversations - should contain new conversation, not UserA's seeded conversation
        using var listReq = CreateAuthorizedRequest(HttpMethod.Get, "/api/v1/ai/conversations", userToken);
        var listRes = await _client.SendAsync(listReq);
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);

        var conversations = await listRes.Content.ReadFromJsonAsync<List<AiConversationResponse>>();
        Assert.NotNull(conversations);
        Assert.Contains(conversations, c => c.Id == chatBody.ConversationId);
        Assert.DoesNotContain(conversations, c => c.Id == AiDatabaseSeeder.Conversation1Id);

        // 3. Get own conversation succeeds, accessing UserA's seeded conversation fails with 404
        using var ownConvReq = CreateAuthorizedRequest(HttpMethod.Get, $"/api/v1/ai/conversations/{chatBody.ConversationId}", userToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(ownConvReq)).StatusCode);

        using var otherConvReq = CreateAuthorizedRequest(HttpMethod.Get, $"/api/v1/ai/conversations/{AiDatabaseSeeder.Conversation1Id}", userToken);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(otherConvReq)).StatusCode);
    }

    [Fact]
    public async Task RecommendMentors_AuthenticatedUser_Returns200Ok()
    {
        var userToken = GenerateTestJwt(Guid.NewGuid().ToString(), "USER");
        using var req = CreateAuthorizedRequest(HttpMethod.Post, "/api/v1/ai/mentors/recommend", userToken, new RecommendMentorRequest
        {
            Major = "SE",
            TargetSkills = new List<string> { "C#", ".NET" }
        });

        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var mentors = await res.Content.ReadFromJsonAsync<List<MentorResponse>>();
        Assert.NotNull(mentors);
        Assert.NotEmpty(mentors);
    }

    [Theory]
    [InlineData("USER")]
    [InlineData("STUDENT")]
    [InlineData("ALUMNI")]
    public async Task RagIngest_NonAdminUser_Returns403Forbidden(string role)
    {
        var userToken = GenerateTestJwt(Guid.NewGuid().ToString(), role);
        using var req = CreateAuthorizedRequest(HttpMethod.Post, "/api/v1/ai/rag/ingest", userToken, new IngestRagRequest());

        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Theory]
    [InlineData(@"C:\")]
    [InlineData("/etc")]
    [InlineData("../../")]
    [InlineData(@"..\..\")]
    [InlineData("data/rag/../../")]
    [InlineData("data/rag/../other")]
    [InlineData("data/rag_evil")]
    [InlineData("   ")]
    public async Task RagIngest_AdminWithOutsideOrTraversalDirectoryPath_Returns400BadRequest(string maliciousPath)
    {
        var adminToken = GenerateTestJwt(Guid.NewGuid().ToString(), "ADMIN");
        using var req = CreateAuthorizedRequest(HttpMethod.Post, "/api/v1/ai/rag/ingest", adminToken, new IngestRagRequest
        {
            DirectoryPath = maliciousPath,
            ForceReindex = false
        });

        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("data/rag")]
    [InlineData("data/rag/Curriculumm")]
    public async Task RagIngest_AdminWithAllowedDirectoryPath_Returns200Ok(string? allowedPath)
    {
        var adminToken = GenerateTestJwt(Guid.NewGuid().ToString(), "ADMIN");
        using var req = CreateAuthorizedRequest(HttpMethod.Post, "/api/v1/ai/rag/ingest", adminToken, new IngestRagRequest
        {
            DirectoryPath = allowedPath,
            ForceReindex = false
        });

        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var result = await res.Content.ReadFromJsonAsync<RagIngestionResult>();
        Assert.NotNull(result);
    }
}
