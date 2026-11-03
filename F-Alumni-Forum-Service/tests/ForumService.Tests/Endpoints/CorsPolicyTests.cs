using System.Net;
using Xunit;

namespace ForumService.Tests.Endpoints;

public class CorsPolicyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CorsPolicyTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OptionsPreflight_WithOriginHeader_ReturnsCorsAllowOriginAndCredentials()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/posts");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"), "Missing Access-Control-Allow-Origin header");
        Assert.Equal("http://localhost:3000", response.Headers.GetValues("Access-Control-Allow-Origin").First());

        Assert.True(response.Headers.Contains("Access-Control-Allow-Credentials"), "Missing Access-Control-Allow-Credentials header");
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").First());
    }

    [Fact]
    public async Task GetEndpoint_WithOriginHeader_ReturnsCorsHeaders()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/posts");
        request.Headers.Add("Origin", "http://localhost:5173");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"), "Missing Access-Control-Allow-Origin header");
        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").First());
    }
}
