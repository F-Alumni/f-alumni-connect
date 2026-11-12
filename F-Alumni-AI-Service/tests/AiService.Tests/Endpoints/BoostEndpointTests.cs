using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AiService.Services.DTOs.Requests;
using AiService.Services.DTOs.Responses;
using Xunit;

namespace AiService.Tests.Endpoints;

public class BoostEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string JwtSecret = CustomWebApplicationFactory.TestJwtSecret;
    private const string JwtIssuer = "https://identity.falumni.local";
    private const string JwtAudience = "falumni-services";

    public BoostEndpointTests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task BoostGetEndpoint_DefaultQuery_ReturnsDiagnosticsWith200()
    {
        // Act
        var response = await _client.GetAsync("/boost");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RagBoostDiagnosticsResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Query);
        Assert.Contains(result.DetectedIntents, i => i.StartsWith("Tuition"));
    }

    [Fact]
    public async Task BoostGetEndpoint_CustomParam_ReturnsMatchedIntents()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/ai/boost?q=vovinam%20nam%20nhat&campus=FU-HL");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RagBoostDiagnosticsResponse>();
        Assert.NotNull(result);
        Assert.Contains(result.DetectedIntents, i => i.StartsWith("FirstYearSubjects"));
    }

    [Fact]
    public async Task BoostPostEndpoint_JsonPayload_ReturnsMatchedIntents()
    {
        // Arrange
        var request = new BoostTestRequest
        {
            Query = "Kỹ thuật phần mềm BIT_SE cần bao nhiêu tín chỉ?",
            Major = "SE",
            Campus = "FU-HL",
            TopK = 3
        };

        // Act
        var response = await _client.PostAsJsonAsync("/boost", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RagBoostDiagnosticsResponse>();
        Assert.NotNull(result);
        Assert.Contains(result.DetectedIntents, i => i.StartsWith("Curriculum"));
    }

    [Fact]
    public async Task EndToEnd_Ingest_Then_Boost_And_Chat_ReturnsGroundedResults()
    {
        var adminToken = GenerateTestJwt(Guid.NewGuid().ToString(), "ADMIN");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // 1. Trigger ingestion into InMemory DB with ADMIN token
        var ingestResp = await _client.PostAsJsonAsync("/api/v1/ai/rag/ingest", new IngestRagRequest { ForceReindex = true });
        Assert.Equal(HttpStatusCode.OK, ingestResp.StatusCode);
        var ingestResult = await ingestResp.Content.ReadFromJsonAsync<RagIngestionResult>();
        Assert.NotNull(ingestResult);
        Assert.Equal(12, ingestResult.TotalFilesProcessed);

        // 2. Test /boost with short Major "SE" and campus "FU-HL" (previously broken!)
        var boostResp = await _client.PostAsJsonAsync("/boost", new BoostTestRequest
        {
            Query = "Kỹ thuật phần mềm học bao nhiêu tín chỉ?",
            Major = "SE",
            Campus = "FU-HL",
            TopK = 3
        });
        Assert.Equal(HttpStatusCode.OK, boostResp.StatusCode);
        var boostData = await boostResp.Content.ReadFromJsonAsync<RagBoostDiagnosticsResponse>();
        Assert.NotNull(boostData);
        Assert.NotEmpty(boostData.TopResults);
        Assert.Equal("BIT_SE", boostData.TopResults[0].ProgramCode);

        // 3. Test /api/v1/ai/chat unaccented query with authorized token
        using var chatReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/ai/chat");
        chatReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        chatReq.Content = JsonContent.Create(new AiChatRequest
        {
            Question = "hoc phi tai hola bao nhieu",
            Campus = "FU-HL",
            Major = "SE"
        });
        var chatResp = await _client.SendAsync(chatReq);
        Assert.Equal(HttpStatusCode.OK, chatResp.StatusCode);
        var chatData = await chatResp.Content.ReadFromJsonAsync<AiChatResponse>();
        Assert.NotNull(chatData);
        Assert.NotEmpty(chatData.Citations);
        Assert.Contains("Học phí", chatData.Citations[0].SourceTitle);

        // 4. Test /api/v1/ai/boost/test with FirstYearSubjects + unaccented query & campus alias normalization
        var firstYearResp = await _client.GetAsync("/api/v1/ai/boost/test?q=vovinam%20va%20tieng%20anh%20nam%20nhat&campus=Hola&topK=0");
        Assert.Equal(HttpStatusCode.OK, firstYearResp.StatusCode);
        var firstYearBoost = await firstYearResp.Content.ReadFromJsonAsync<RagBoostDiagnosticsResponse>();
        Assert.NotNull(firstYearBoost);
        Assert.Contains(firstYearBoost.DetectedIntents, i => i.StartsWith("FirstYearSubjects"));
        Assert.NotEmpty(firstYearBoost.TopResults);
        Assert.True(firstYearBoost.TopResults[0].Score >= 45.0);

        // 5. Test unmatched gibberish query returns 200 OK cleanly with empty TopResults
        var emptyBoostResp = await _client.GetAsync("/boost?q=xyzqwerty987654&campus=FU-HL");
        Assert.Equal(HttpStatusCode.OK, emptyBoostResp.StatusCode);
        var emptyBoost = await emptyBoostResp.Content.ReadFromJsonAsync<RagBoostDiagnosticsResponse>();
        Assert.NotNull(emptyBoost);
        Assert.Empty(emptyBoost.DetectedIntents);
        Assert.Empty(emptyBoost.TopResults);

        // 6. Tuition query mentioning a major (without Major param) MUST still rank Tuition #1, not Curriculum
        var tuitionWithMajorResp = await _client.GetAsync("/boost?q=" + Uri.EscapeDataString("Học phí ngành Kỹ thuật phần mềm tại Hola là bao nhiêu?"));
        Assert.Equal(HttpStatusCode.OK, tuitionWithMajorResp.StatusCode);
        var tuitionWithMajor = await tuitionWithMajorResp.Content.ReadFromJsonAsync<RagBoostDiagnosticsResponse>();
        Assert.NotNull(tuitionWithMajor);
        Assert.NotEmpty(tuitionWithMajor.TopResults);
        Assert.Equal("Tuition", tuitionWithMajor.TopResults[0].DocumentType);

        // 7. Unaccented curriculum queries WITHOUT Major param MUST match the exact ProgramCode
        var aiBoostResp = await _client.GetAsync("/boost?q=" + Uri.EscapeDataString("Ngành trí tuệ nhân tạo học những môn gì?"));
        Assert.Equal(HttpStatusCode.OK, aiBoostResp.StatusCode);
        var aiBoost = await aiBoostResp.Content.ReadFromJsonAsync<RagBoostDiagnosticsResponse>();
        Assert.NotNull(aiBoost);
        Assert.NotEmpty(aiBoost.TopResults);
        Assert.Equal("BIT_AI", aiBoost.TopResults[0].ProgramCode);
        Assert.Contains(aiBoost.DetectedIntents, i => i.StartsWith("Program:BIT_AI"));

        // 8. Facilities, Regulations, and Slang queries via /boost
        var ktxResp = await _client.GetAsync("/boost?q=" + Uri.EscapeDataString("ky tuc xa ktx o xavalo hcm nhu the nao"));
        Assert.Equal(HttpStatusCode.OK, ktxResp.StatusCode);
        var ktxBoost = await ktxResp.Content.ReadFromJsonAsync<RagBoostDiagnosticsResponse>();
        Assert.NotNull(ktxBoost);
        Assert.NotEmpty(ktxBoost.TopResults);
        Assert.Equal("Facilities", ktxBoost.TopResults[0].DocumentType);
        Assert.Contains(ktxBoost.DetectedIntents, i => i.StartsWith("Campus:FU-HCM"));

        var regResp = await _client.GetAsync("/boost?q=" + Uri.EscapeDataString("Quy định bảo lưu và thi thẩm định Coursera trên FAP"));
        Assert.Equal(HttpStatusCode.OK, regResp.StatusCode);
        var regBoost = await regResp.Content.ReadFromJsonAsync<RagBoostDiagnosticsResponse>();
        Assert.NotNull(regBoost);
        Assert.NotEmpty(regBoost.TopResults);
        Assert.Equal("Regulations", regBoost.TopResults[0].DocumentType);

        // 9. Multi-turn chat without explicit Major: Turn 1 infers BIT_AI, Turn 2 switches cleanly to Tuition
        var turn1Resp = await _client.PostAsJsonAsync("/api/v1/ai/chat", new AiChatRequest
        {
            Question = "Ngành AI học bao nhiêu tín chỉ?"
        });
        Assert.Equal(HttpStatusCode.OK, turn1Resp.StatusCode);
        var turn1 = await turn1Resp.Content.ReadFromJsonAsync<AiChatResponse>();
        Assert.NotNull(turn1);
        Assert.NotEmpty(turn1.Citations);
        Assert.Equal("BIT_AI", turn1.Citations[0].ProgramCode);

        var turn2Resp = await _client.PostAsJsonAsync("/api/v1/ai/chat", new AiChatRequest
        {
            Question = "Thế còn học phí của ngành này tại Hola?",
            ConversationId = turn1.ConversationId
        });
        Assert.Equal(HttpStatusCode.OK, turn2Resp.StatusCode);
        var turn2 = await turn2Resp.Content.ReadFromJsonAsync<AiChatResponse>();
        Assert.NotNull(turn2);
        Assert.NotEmpty(turn2.Citations);
        Assert.Contains("Học phí", turn2.Citations[0].SourceTitle);
    }
}
