using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserService.Services.DTOs.Common;
using UserService.Services.DTOs.Requests;
using UserService.Services.DTOs.Responses;
using Xunit;

namespace UserService.Tests.Endpoints;

public class AuthAndUsersEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthAndUsersEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PingEndpoints_Return200Awake()
    {
        var pingResponse = await _client.GetAsync("/ping");
        Assert.Equal(HttpStatusCode.OK, pingResponse.StatusCode);

        var apiPingResponse = await _client.GetAsync("/api/v1/ping");
        Assert.Equal(HttpStatusCode.OK, apiPingResponse.StatusCode);
    }

    [Fact]
    public async Task Register_VerifyEmail_Login_UpdateProfile_FullFlow_Works()
    {
        var email = $"trung.{Guid.NewGuid():N}@fpt.edu.vn";
        var registerReq = new RegisterRequest
        {
            Email = email,
            Password = "StrongPassword@123",
            FullName = "Chi Trung FPT",
            StudentCode = "SE180999",
            GraduationYear = 2026,
            Major = "Software Engineering",
            AccountType = "Student"
        };

        // 1. Register (US-01)
        var regRes = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        Assert.Equal(HttpStatusCode.Created, regRes.StatusCode);
        var regBody = await regRes.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(regBody);
        Assert.False(string.IsNullOrWhiteSpace(regBody.VerificationOtpPreview));

        // 2. Login before verification should return 403 Forbidden
        var earlyLoginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = "StrongPassword@123"
        });
        Assert.Equal(HttpStatusCode.Forbidden, earlyLoginRes.StatusCode);

        // 3. Verify Email (US-02)
        var verifyRes = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new VerifyEmailRequest
        {
            Email = email,
            OtpCode = regBody.VerificationOtpPreview!
        });
        Assert.Equal(HttpStatusCode.OK, verifyRes.StatusCode);
        var verifyAuth = await verifyRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(verifyAuth);
        Assert.False(string.IsNullOrWhiteSpace(verifyAuth.AccessToken));
        Assert.Equal("Active", verifyAuth.User.Status);

        // 4. Login (US-03)
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = "StrongPassword@123"
        });
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        var loginAuth = await loginRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(loginAuth);

        // 5. Refresh Token (US-11)
        var refreshRes = await _client.PostAsJsonAsync("/api/v1/auth/refresh-token", new RefreshTokenRequest
        {
            RefreshToken = loginAuth.RefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, refreshRes.StatusCode);
        var refreshedAuth = await refreshRes.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(refreshedAuth);

        // 6. Get & Update My Profile (US-05, US-06)
        using var getMeReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        getMeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedAuth.AccessToken);
        var getMeRes = await _client.SendAsync(getMeReq);
        Assert.Equal(HttpStatusCode.OK, getMeRes.StatusCode);

        using var updateMeReq = new HttpRequestMessage(HttpMethod.Put, "/api/v1/users/me")
        {
            Content = JsonContent.Create(new UpdateUserProfileRequest
            {
                FullName = "Chi Trung Updated",
                DisplayName = "Trung Backend",
                StudentCode = "SE180999",
                GraduationYear = 2026,
                Major = "Software Engineering",
                CurrentCompany = "FPT Software",
                JobTitle = ".NET Developer",
                Bio = "Building User Service",
                Skills = "C#, .NET 10, gRPC, Kafka"
            })
        };
        updateMeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedAuth.AccessToken);
        var updateMeRes = await _client.SendAsync(updateMeReq);
        Assert.Equal(HttpStatusCode.OK, updateMeRes.StatusCode);
        var updatedProfile = await updateMeRes.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(updatedProfile);
        Assert.Equal("FPT Software", updatedProfile.CurrentCompany);
    }

    [Fact]
    public async Task Register_NonFptEmail_Returns400BadRequest()
    {
        var registerReq = new RegisterRequest
        {
            Email = "someone@gmail.com",
            Password = "StrongPassword@123",
            FullName = "Non FPT User"
        };

        var res = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task SeededUsers_CanBeQueriedAndLoggedIn()
    {
        // Seeded user 11111111-1111-1111-1111-111111111111
        var getRes = await _client.GetAsync("/api/v1/users/11111111-1111-1111-1111-111111111111");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        var profile = await getRes.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal("Nguyen Van A", profile.FullName);

        // Directory search (US-09)
        var dirRes = await _client.GetAsync("/api/v1/users/directory?search=Nguyen");
        Assert.Equal(HttpStatusCode.OK, dirRes.StatusCode);
        var paged = await dirRes.Content.ReadFromJsonAsync<PagedResponse<UserProfileResponse>>();
        Assert.NotNull(paged);
        Assert.True(paged.TotalCount >= 1);

        // Seeded Admin Login & Suspend User (AD-03)
        var adminLogin = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "admin@fpt.edu.vn",
            Password = "Password@123"
        });
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);
        var adminTokens = await adminLogin.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(adminTokens);

        using var suspendReq = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/internal/users/22222222-2222-2222-2222-222222222222/suspend")
        {
            Content = JsonContent.Create(new SuspendUserRequest { Reason = "Policy test" })
        };
        suspendReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        var suspendRes = await _client.SendAsync(suspendReq);
        Assert.Equal(HttpStatusCode.OK, suspendRes.StatusCode);

        // Unsuspend back
        using var unsuspendReq = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/internal/users/22222222-2222-2222-2222-222222222222/unsuspend");
        unsuspendReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        var unsuspendRes = await _client.SendAsync(unsuspendReq);
        Assert.Equal(HttpStatusCode.OK, unsuspendRes.StatusCode);
    }
}
