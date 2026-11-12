using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ForumService.Api.Data;
using ForumService.Api.Serialization;
using ForumService.Services.DTOs.Responses;
using Xunit;

namespace ForumService.Tests.Endpoints;

public class VietnamDateTimeJsonConverterTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public VietnamDateTimeJsonConverterTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new VietnamDateTimeJsonConverter());
    }

    [Fact]
    public void Serialize_UnspecifiedUtcTimestampFromDatabase_WritesPlusSevenOffset()
    {
        // Arrange: Simulate SQL Server datetime2 value read back with Kind == Unspecified (20:15:30 UTC -> 03:15:30 next day GMT+7)
        var dbTimestamp = new DateTime(2026, 11, 9, 20, 15, 30, DateTimeKind.Unspecified);
        var response = new PostResponse
        {
            Id = Guid.NewGuid(),
            Title = "Timestamp Check",
            CreatedAt = dbTimestamp,
            UpdatedAt = null
        };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        using var doc = JsonDocument.Parse(json);
        var createdAt = doc.RootElement.GetProperty("createdAt").GetString();
        var updatedAt = doc.RootElement.GetProperty("updatedAt");

        // Assert
        Assert.Equal("2026-11-10T03:15:30+07:00", createdAt);
        Assert.Equal(JsonValueKind.Null, updatedAt.ValueKind);
    }

    [Fact]
    public void Serialize_UtcAndNullableUpdatedAt_WritesPlusSevenOffset()
    {
        // Arrange
        var createdUtc = new DateTime(2026, 11, 9, 15, 47, 45, DateTimeKind.Utc);
        var updatedUtc = new DateTime(2026, 11, 9, 16, 0, 0, DateTimeKind.Utc);
        var response = new PostResponse
        {
            Id = Guid.NewGuid(),
            CreatedAt = createdUtc,
            UpdatedAt = updatedUtc
        };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        using var doc = JsonDocument.Parse(json);

        // Assert
        Assert.Equal("2026-11-09T22:47:45+07:00", doc.RootElement.GetProperty("createdAt").GetString());
        Assert.Equal("2026-11-09T23:00:00+07:00", doc.RootElement.GetProperty("updatedAt").GetString());
    }

    [Fact]
    public void Deserialize_NormalizesOffsetsAndUnspecifiedInputsToUtc()
    {
        // Arrange
        const string json = """
            {
              "utc": "2026-11-18T10:00:00Z",
              "vn": "2026-11-18T17:00:00+07:00",
              "other": "2026-11-18T05:00:00-05:00",
              "unspecified": "2026-11-18T17:00:00"
            }
            """;

        // Act
        var dict = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json, _jsonOptions);

        // Assert
        Assert.NotNull(dict);
        var expected = new DateTime(2026, 11, 18, 10, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expected, dict["utc"]);
        Assert.Equal(expected, dict["vn"]);
        Assert.Equal(expected, dict["other"]);
        Assert.Equal(expected, dict["unspecified"]);
        Assert.All(dict.Values, dt => Assert.Equal(DateTimeKind.Utc, dt.Kind));
    }

    [Fact]
    public void Serialize_MaxValue_DoesNotThrowOverflow()
    {
        var json = JsonSerializer.Serialize(DateTime.MaxValue, _jsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(json));
    }

    [Fact]
    public async Task GetPostsEndpoint_SerializesCreatedAtInVietnamGmtPlusSeven()
    {
        // Ensure at least one post exists in the database
        using (var scope = _factory.Services.CreateScope())
        {
            await ForumDatabaseSeeder.SeedAsync(scope.ServiceProvider);
        }

        // Act
        var response = await _client.GetAsync("/api/v1/posts?pageNumber=1&pageSize=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var items = doc.RootElement.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1);

        foreach (var item in items.EnumerateArray())
        {
            var createdAtRaw = item.GetProperty("createdAt").GetString();
            Assert.NotNull(createdAtRaw);
            Assert.EndsWith("+07:00", createdAtRaw);
            Assert.True(DateTimeOffset.TryParse(createdAtRaw, out var dto));
            Assert.Equal(TimeSpan.FromHours(7), dto.Offset);
        }

        // Seeded Post1 must be present with +07:00 offset
        Assert.Contains(
            items.EnumerateArray(),
            x => x.GetProperty("id").GetGuid() == ForumDatabaseSeeder.Post1Id);
    }
}
