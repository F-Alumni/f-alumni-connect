using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AdminService.Services.DTOs.Announcements;
using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Dashboard;
using AdminService.Services.DTOs.Moderation;
using Xunit;

namespace AdminService.Tests.Endpoints;

public class AdminPhase4EndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string JwtSecret = CustomWebApplicationFactory.TestJwtSecret;
    private const string JwtIssuer = "https://identity.falumni.local";
    private const string JwtAudience = "falumni-services";

    public AdminPhase4EndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task DashboardSummary_WithoutToken_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/api/v1/admin/dashboard/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DashboardSummary_WithAdminRole_Returns200Ok()
    {
        var token = GenerateTestJwt(Guid.NewGuid().ToString(), "ADMIN");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/dashboard/summary");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<AdminDashboardSummaryDto>();
        Assert.NotNull(data);
    }

    [Fact]
    public async Task ModerationReports_WithAdminRole_Returns200Ok()
    {
        var token = GenerateTestJwt(Guid.NewGuid().ToString(), "ADMIN");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/moderation/reports");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<PagedResponse<ModerationReportResponse>>();
        Assert.NotNull(data);
    }

    [Fact]
    public async Task Announcements_CreateAndGet_WithAdminRole_ReturnsSuccess()
    {
        var token = GenerateTestJwt(Guid.NewGuid().ToString(), "ADMIN");

        // 1. Create announcement
        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/announcements")
        {
            Content = JsonContent.Create(new CreateAnnouncementRequest
            {
                Title = "Orientation Week 2026",
                Content = "Welcome all new students to FPT University!",
                Type = "Academic",
                Priority = "Normal",
                IsPublished = true
            })
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRes = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = await createRes.Content.ReadFromJsonAsync<SystemAnnouncementResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);

        // 2. Query list
        using var getReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/announcements");
        getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var getRes = await _client.SendAsync(getReq);
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
    }

    [Fact]
    public async Task ExportAuditLogs_WithAdminRole_ReturnsCsvFile()
    {
        var token = GenerateTestJwt(Guid.NewGuid().ToString(), "ADMIN");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/export/audit-logs");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Contains("text/csv", response.Content.Headers.ContentType.ToString());

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
    }
}
