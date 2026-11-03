using System.Security.Cryptography;
using UserService.Repositories.Entities;
using UserService.Services.DTOs.Responses;

namespace UserService.Services.Implementations;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2")
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expectedHash = Convert.FromBase64String(parts[3]);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}

public static class UserMappingExtensions
{
    public static UserProfileResponse ToResponse(this User user, bool applyPublicPrivacyMask = false)
    {
        var p = user.Profile;
        var showEmail = p?.ShowEmailPublicly ?? true;
        var showPhone = p?.ShowPhonePublicly ?? false;
        var showCompany = p?.ShowCompanyPublicly ?? true;
        var showCalendar = p?.ShowCalendarPublicly ?? true;

        return new UserProfileResponse
        {
            Id = user.Id,
            Email = (!applyPublicPrivacyMask || showEmail) ? user.Email : string.Empty,
            FullName = user.FullName,
            DisplayName = !string.IsNullOrWhiteSpace(p?.DisplayName) ? p.DisplayName : user.FullName,
            AvatarUrl = !string.IsNullOrWhiteSpace(p?.AvatarUrl) ? p.AvatarUrl : "https://cdn.falumni.local/avatars/default.jpg",
            Role = user.Role,
            AccountType = user.AccountType,
            Status = user.Status,
            EmailVerified = user.EmailVerified,
            StudentCode = p?.StudentCode ?? string.Empty,
            Campus = !string.IsNullOrWhiteSpace(p?.Campus) ? p.Campus : "HCM",
            GraduationYear = p?.GraduationYear,
            Major = p?.Major ?? string.Empty,
            CurrentCompany = (!applyPublicPrivacyMask || showCompany) ? (p?.CurrentCompany ?? string.Empty) : string.Empty,
            JobTitle = (!applyPublicPrivacyMask || showCompany) ? (p?.JobTitle ?? string.Empty) : string.Empty,
            Bio = p?.Bio ?? string.Empty,
            Skills = p?.Skills ?? string.Empty,
            LinkedInUrl = p?.LinkedInUrl ?? string.Empty,
            GitHubUrl = p?.GitHubUrl ?? string.Empty,
            PhoneNumber = (!applyPublicPrivacyMask || showPhone) ? (p?.PhoneNumber ?? string.Empty) : string.Empty,
            GoogleCalendarUrl = (!applyPublicPrivacyMask || showCalendar) ? (p?.GoogleCalendarUrl ?? string.Empty) : string.Empty,
            ShowEmailPublicly = showEmail,
            ShowPhonePublicly = showPhone,
            ShowCompanyPublicly = showCompany,
            ShowCalendarPublicly = showCalendar,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        };
    }
}
