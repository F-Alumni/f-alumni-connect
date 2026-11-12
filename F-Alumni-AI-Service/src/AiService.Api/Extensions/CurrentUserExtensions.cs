using System.Security.Claims;

namespace AiService.Api.Extensions;

public static class CurrentUserExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(sub))
        {
            sub = user.FindFirst("sub")?.Value;
        }

        if (Guid.TryParse(sub, out var guid) && guid != Guid.Empty)
        {
            return guid;
        }

        return Guid.Empty;
    }

    public static bool TryGetUserId(this ClaimsPrincipal user, out Guid userId)
    {
        userId = user.GetUserId();
        return userId != Guid.Empty;
    }

    public static string GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value
            ?? "anonymous@fpt.edu.vn";
    }

    public static string GetFullName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("name")?.Value
            ?? user.FindFirst("fullName")?.Value
            ?? user.GetEmail().Split('@')[0];
    }

    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("ADMIN") ||
               string.Equals(user.FindFirst(ClaimTypes.Role)?.Value, "ADMIN", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(user.FindFirst("role")?.Value, "ADMIN", StringComparison.OrdinalIgnoreCase);
    }
}
