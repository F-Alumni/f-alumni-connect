using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Users;
using AdminService.Services.DTOs.AuditLogs;
using Xunit;

namespace AdminService.Tests.Endpoints;

public class AdminModerationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string JwtSecret = CustomWebApplicationFactory.TestJwtSecret;
    private const string JwtIssuer = "https://identity.falumni.local";
    private const string JwtAudience = "falumni-services";

    public AdminModerationEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string GenerateTestJwt(string userId, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecret);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
            new("role", role),
            new(JwtRegisteredClaimNames.Email, $"{userId}@fpt.edu.vn"),
            new(ClaimTypes.Email, $"{userId}@fpt.edu.vn"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

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

    [Fact]
    public async Task GetUsers_WithoutToken_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/admin/users");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithUserRole_Returns403Forbidden()
    {
        // Arrange
        var token = GenerateTestJwt(Guid.NewGuid().ToString(), "USER");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/users");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithAdminRole_Returns200OkWithPagedUsers()
    {
        // Arrange
        var token = GenerateTestJwt(Guid.NewGuid().ToString(), "ADMIN");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/users?pageSize=10");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<PagedResponse<AdminUserSummaryDto>>();
        Assert.NotNull(data);
        Assert.NotEmpty(data.Items);
    }

    [Fact]
    public async Task GetAuditLogs_WithAdminRole_Returns200OkWithPagedLogs()
    {
        // Arrange
        var token = GenerateTestJwt(Guid.NewGuid().ToString(), "ADMIN");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/audit-logs");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<PagedResponse<AuditLogResponse>>();
        Assert.NotNull(data);
    }

    [Fact]
    public async Task SuspendUser_WithAdminRole_Returns200Ok()
    {
        // Arrange
        var adminId = Guid.NewGuid().ToString();
        var token = GenerateTestJwt(adminId, "ADMIN");
        var targetUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/users/{targetUserId}/suspend")
        {
            Content = JsonContent.Create(new SuspendUserRequest
            {
                Reason = "Posting inappropriate spam messages",
                DurationDays = 7
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
