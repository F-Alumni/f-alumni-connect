using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AdminService.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value
            ?? principal.FindFirst("id")?.Value;

        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    public static string GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value
            ?? principal.FindFirst(ClaimTypes.Name)?.Value
            ?? "admin@falumni.local";
    }

    public static string? GetBearerToken(this HttpRequest request)
    {
        return request.Headers.TryGetValue("Authorization", out var val) ? val.ToString() : null;
    }

    public static string? GetClientIpAddress(this HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            return forwarded.ToString().Split(',')[0].Trim();
        }
        return context.Connection.RemoteIpAddress?.ToString();
    }
}
