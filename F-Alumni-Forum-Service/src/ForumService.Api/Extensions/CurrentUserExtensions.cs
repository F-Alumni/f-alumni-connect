using System.Security.Claims;

namespace ForumService.Api.Extensions;

public static class CurrentUserExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        if (Guid.TryParse(sub, out var guid))
        {
            return guid;
        }

        // Return empty or deterministic test Guid if unauthenticated or testing
        return Guid.Empty;
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

    public static string GetAvatarUrl(this ClaimsPrincipal user)
    {
        return user.FindFirst("avatarUrl")?.Value
            ?? $"https://api.dicebear.com/7.x/bottts/svg?seed={user.GetUserId()}";
    }

    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("ADMIN") ||
               string.Equals(user.FindFirst(ClaimTypes.Role)?.Value, "ADMIN", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(user.FindFirst("role")?.Value, "ADMIN", StringComparison.OrdinalIgnoreCase);
    }
}
