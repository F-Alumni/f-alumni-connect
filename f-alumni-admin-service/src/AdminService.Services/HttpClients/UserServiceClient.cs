using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Users;

namespace AdminService.Services.HttpClients;

public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserServiceClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UserServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<UserServiceClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var baseUrl = _configuration["Services:UserService:Url"] ?? "http://localhost:5001";
        if (!baseUrl.EndsWith("/"))
        {
            baseUrl += "/";
        }
        _httpClient.BaseAddress = new Uri(baseUrl);

        var timeoutSeconds = int.TryParse(_configuration["Services:UserService:TimeoutSeconds"], out var t) ? t : 5;
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public async Task<PagedResponse<AdminUserSummaryDto>> GetUsersDirectoryAsync(
        UserFilterQuery query,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        var queryString = $"api/v1/users?pageNumber={query.PageNumber}&pageSize={query.PageSize}";
        if (!string.IsNullOrWhiteSpace(query.Search)) queryString += $"&search={Uri.EscapeDataString(query.Search)}";
        if (!string.IsNullOrWhiteSpace(query.Campus)) queryString += $"&campus={Uri.EscapeDataString(query.Campus)}";
        if (!string.IsNullOrWhiteSpace(query.Major)) queryString += $"&major={Uri.EscapeDataString(query.Major)}";
        if (query.GraduationYear.HasValue) queryString += $"&graduationYear={query.GraduationYear.Value}";
        if (!string.IsNullOrWhiteSpace(query.Company)) queryString += $"&company={Uri.EscapeDataString(query.Company)}";
        if (!string.IsNullOrWhiteSpace(query.Status)) queryString += $"&status={Uri.EscapeDataString(query.Status)}";
        if (!string.IsNullOrWhiteSpace(query.Role)) queryString += $"&role={Uri.EscapeDataString(query.Role)}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, queryString);
            AttachAuthHeader(request, bearerToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var pagedData = await response.Content.ReadFromJsonAsync<PagedResponse<AdminUserSummaryDto>>(JsonOptions, cancellationToken);
                if (pagedData is not null)
                {
                    return pagedData;
                }
            }

            _logger.LogWarning("User Service returned status {StatusCode} on directory query.", response.StatusCode);
            throw new HttpRequestException($"User Service returned HTTP {(int)response.StatusCode} while loading users.");
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to User Service at {Url}.", _httpClient.BaseAddress);
            throw new HttpRequestException($"Cannot connect to User Service at {_httpClient.BaseAddress}: {ex.Message}", ex);
        }
    }

    public async Task<AdminUserSummaryDto?> GetUserByIdAsync(
        Guid userId,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/users/{userId}");
            AttachAuthHeader(request, bearerToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AdminUserSummaryDto>(JsonOptions, cancellationToken);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            throw new HttpRequestException($"User Service returned HTTP {(int)response.StatusCode} while loading user '{userId}'.");
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query User Service for userId {UserId}.", userId);
            throw new HttpRequestException($"Cannot connect to User Service: {ex.Message}", ex);
        }
    }

    public async Task<bool> SuspendUserAsync(
        Guid userId,
        SuspendUserRequest request,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/internal/users/{userId}/suspend")
            {
                Content = JsonContent.Create(new { Reason = request.Reason })
            };
            AttachAuthHeader(req, bearerToken);

            var response = await _httpClient.SendAsync(req, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("User Service returned {StatusCode} while suspending user {UserId}", response.StatusCode, userId);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call User Service suspend endpoint for {UserId}.", userId);
            return false;
        }
    }

    public async Task<bool> UnsuspendUserAsync(
        Guid userId,
        UnsuspendUserRequest? request = null,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/internal/users/{userId}/unsuspend")
            {
                Content = JsonContent.Create(new { Reason = request?.Reason ?? "Account unsuspended by admin" })
            };
            AttachAuthHeader(req, bearerToken);

            var response = await _httpClient.SendAsync(req, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("User Service returned {StatusCode} while unsuspending user {UserId}", response.StatusCode, userId);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call User Service unsuspend endpoint for {UserId}.", userId);
            return false;
        }
    }

    private static void AttachAuthHeader(HttpRequestMessage request, string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            var rawToken = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? token["Bearer ".Length..]
                : token;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);
        }
    }
}
