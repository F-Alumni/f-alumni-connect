using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace ChatService.Tests.Endpoints;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Deterministic signing key strictly reserved for automated testing (xUnit test fixture).
    /// WARNING: DO NOT USE IN RUNTIME, DEVELOPMENT, OR PRODUCTION ENVIRONMENTS.
    /// Real runtime signing keys must be randomly generated and supplied via User Secrets or environment variables.
    /// </summary>
    public const string TestJwtSecret = "TestFixtureDeterministicKey_AtLeast32BytesLong_PRN232_ASM_2026!";
    public const string TestJwtIssuer = "https://identity.falumni.local";
    public const string TestJwtAudience = "falumni-services";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("DatabaseProvider", "InMemory");
        builder.UseSetting("Jwt:Key", TestJwtSecret);
    }

    public HttpClient CreateAuthenticatedClient(Guid userId, string role = "USER")
    {
        return CreateAuthenticatedClient(userId.ToString(), role);
    }

    public HttpClient CreateAuthenticatedClient(string? userId, string role = "USER")
    {
        var client = CreateClient();
        var token = GenerateTestJwt(userId, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static string GenerateTestJwt(string? userId, string role = "USER", TimeSpan? lifetime = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestJwtSecret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new("role", role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(userId))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, $"{userId}@fpt.edu.vn"));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromHours(1)),
            Issuer = TestJwtIssuer,
            Audience = TestJwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
