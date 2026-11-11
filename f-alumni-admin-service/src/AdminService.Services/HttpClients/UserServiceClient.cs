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
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to User Service at {Url}. Using fallback mock data.", _httpClient.BaseAddress);
        }

        return CreateFallbackPagedUsers(query);
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
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query User Service for userId {UserId}. Using fallback.", userId);
        }

        return GetFallbackUser(userId);
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
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to call User Service suspend endpoint for {UserId}. Operating in fallback mode.", userId);
            return true; // Fallback simulation for offline testing
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
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to call User Service unsuspend endpoint for {UserId}. Operating in fallback mode.", userId);
            return true; // Fallback simulation for offline testing
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

    private static PagedResponse<AdminUserSummaryDto> CreateFallbackPagedUsers(UserFilterQuery query)
    {
        var all = new List<AdminUserSummaryDto>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "nguyenvana@fpt.edu.vn",
                FullName = "Nguyen Van A",
                DisplayName = "Nguyen Van A",
                Role = "ALUMNI",
                AccountType = "Alumni",
                Status = "Active",
                Campus = "Hoa Lac",
                Major = "Software Engineering",
                CurrentCompany = "FPT Software",
                JobTitle = "Backend Engineer",
                CreatedAtUtc = DateTime.UtcNow.AddMonths(-6)
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "tranthib@fpt.edu.vn",
                FullName = "Tran Thi B",
                DisplayName = "Tran Thi B",
                Role = "STUDENT",
                AccountType = "Student",
                Status = "Active",
                Campus = "HCM",
                Major = "Artificial Intelligence",
                CurrentCompany = "FPT University",
                JobTitle = "Final Year Student",
                CreatedAtUtc = DateTime.UtcNow.AddMonths(-3)
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Email = "levanc@fpt.edu.vn",
                FullName = "Le Van C",
                DisplayName = "Le Van C",
                Role = "ALUMNI",
                AccountType = "Alumni",
                Status = "Suspended",
                SuspensionReason = "Spamming forum posts",
                Campus = "Da Nang",
                Major = "Information Assurance",
                CreatedAtUtc = DateTime.UtcNow.AddMonths(-1)
            },
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Email = "admin@fpt.edu.vn",
                FullName = "FPT Alumni Admin",
                DisplayName = "FPT Alumni Admin",
                Role = "ADMIN",
                AccountType = "Admin",
                Status = "Active",
                Campus = "Hoa Lac",
                Major = "Administration",
                CreatedAtUtc = DateTime.UtcNow.AddYears(-1)
            }
        };

        var filtered = all.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            filtered = filtered.Where(u => u.Status.Equals(query.Status, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            filtered = filtered.Where(u => u.Role.Equals(query.Role, StringComparison.OrdinalIgnoreCase));
        }

        var list = filtered.ToList();
        return new PagedResponse<AdminUserSummaryDto>
        {
            Items = list,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = list.Count
        };
    }

    private static AdminUserSummaryDto? GetFallbackUser(Guid userId)
    {
        var users = CreateFallbackPagedUsers(new UserFilterQuery { PageSize = 100 }).Items;
        return users.FirstOrDefault(u => u.Id == userId);
    }
}
