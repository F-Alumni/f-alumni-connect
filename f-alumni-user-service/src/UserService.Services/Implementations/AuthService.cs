using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using UserService.Repositories.Entities;
using UserService.Repositories.Helpers;
using UserService.Repositories.Interfaces;
using UserService.Services.DTOs.Requests;
using UserService.Services.DTOs.Responses;
using UserService.Services.Exceptions;
using UserService.Services.Interfaces;

namespace UserService.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IKafkaEventPublisher _kafkaPublisher;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IKafkaEventPublisher kafkaPublisher,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _kafkaPublisher = kafkaPublisher;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }
        var allowNonFptEmail = bool.TryParse(_configuration["Auth:AllowNonFptEmail"], out var allowAll) && allowAll;
        if (!allowNonFptEmail && !IsAllowedFptEmail(email))
        {
            throw new ArgumentException("Email must belong to FPT domain (@fpt.edu.vn or @fe.edu.vn).");
        }

        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"Account with email '{email}' already exists.");
        }

        var autoActivate = bool.TryParse(_configuration["Auth:AutoActivateOnRegister"], out var autoAct) && autoAct;

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new ArgumentException("Full name is required.");
        }

        var userId = Guid.NewGuid();
        var (resolvedRole, resolvedAccountType) = ResolveRoleAndAccountType(request.Role, request.AccountType, request.GraduationYear);
        var campus = CampusNormalizer.NormalizeOrDefault(request.Campus);
        var user = new User
        {
            Id = userId,
            Email = email,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Role = resolvedRole,
            AccountType = resolvedAccountType,
            Status = autoActivate ? "Active" : "PendingVerification",
            EmailVerified = autoActivate,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DisplayName = request.FullName.Trim(),
                AvatarUrl = "https://cdn.falumni.local/avatars/default.jpg",
                StudentCode = request.StudentCode?.Trim() ?? string.Empty,
                Campus = campus,
                GraduationYear = request.GraduationYear,
                Major = request.Major?.Trim() ?? string.Empty,
                UpdatedAtUtc = DateTime.UtcNow
            }
        };

        await _userRepository.CreateAsync(user, cancellationToken);

        var otpCode = GenerateOtpCode();
        var verificationToken = new VerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OtpCode = otpCode,
            Purpose = "EmailVerification",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        await _userRepository.CreateVerificationTokenAsync(verificationToken, cancellationToken);

        // Send OTP directly to the user's email / Gmail via SMTP
        await _emailService.SendOtpEmailAsync(
            toEmail: user.Email,
            fullName: user.FullName,
            otpCode: otpCode,
            purpose: "EmailVerification",
            cancellationToken: cancellationToken);

        // Publish Kafka Event: user.registered.v1
        var eventData = new UserRegisteredEventV1(
            userId: user.Id,
            email: user.Email,
            fullName: user.FullName,
            verificationCode: otpCode);

        await _kafkaPublisher.PublishUserRegisteredAsync(eventData, cancellationToken: cancellationToken);

        _logger.LogInformation("Registered new user {UserId} ({Email}) with Role={Role}, AccountType={AccountType}", user.Id, user.Email, user.Role, user.AccountType);

        return new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Status = user.Status,
            Message = "Registration successful. Please verify your email using the 6-digit OTP code sent to your email.",
            VerificationOtpPreview = otpCode
        };
    }

    public async Task<AuthTokenResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new NotFoundException(nameof(User), email);

        if (user.Status == "Suspended")
        {
            throw new ForbiddenException("Account has been suspended.");
        }

        var token = await _userRepository.GetValidVerificationTokenAsync(
            user.Id,
            request.OtpCode?.Trim() ?? string.Empty,
            "EmailVerification",
            cancellationToken);

        if (token is null)
        {
            throw new ArgumentException("Invalid or expired OTP verification code.");
        }

        await _userRepository.MarkVerificationTokenUsedAsync(token, cancellationToken);

        user.Status = "Active";
        user.EmailVerified = true;
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Verified email for user {UserId} ({Email})", user.Id, user.Email);

        return await GenerateAuthTokensAsync(user, cancellationToken);
    }

    public async Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (user.Status.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
        {
            var reason = string.IsNullOrWhiteSpace(user.SuspensionReason)
                ? "Account is suspended due to policy violation."
                : $"Account is suspended: {user.SuspensionReason}";
            throw new ForbiddenException(reason);
        }

        if (user.Status.Equals("PendingVerification", StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Account email is not verified yet. Please verify your email with the OTP code.");
        }

        _logger.LogInformation("User {UserId} ({Email}) logged in successfully.", user.Id, user.Email);

        return await GenerateAuthTokensAsync(user, cancellationToken);
    }

    public async Task<AuthTokenResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var avatarUrl = request.AvatarUrl?.Trim() ?? string.Empty;

        // If IdToken is a JWT, extract email/name claims when not explicitly passed
        if (!string.IsNullOrWhiteSpace(request.IdToken))
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(request.IdToken))
                {
                    var jwt = handler.ReadJwtToken(request.IdToken);
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        email = (jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty).ToLowerInvariant();
                    }
                    if (string.IsNullOrWhiteSpace(fullName))
                    {
                        fullName = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty;
                    }
                    if (string.IsNullOrWhiteSpace(avatarUrl))
                    {
                        avatarUrl = jwt.Claims.FirstOrDefault(c => c.Type == "picture")?.Value ?? string.Empty;
                    }
                }
            }
            catch
            {
                // Fallback to request properties
            }
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Google login requires a valid email or Google ID Token.");
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            var userId = Guid.NewGuid();
            var resolvedName = string.IsNullOrWhiteSpace(fullName) ? email.Split('@')[0] : fullName;
            user = new User
            {
                Id = userId,
                Email = email,
                PasswordHash = PasswordHasher.HashPassword(Guid.NewGuid().ToString("N")),
                FullName = resolvedName,
                Role = "ALUMNI",
                AccountType = "Alumni",
                Status = "Active",
                EmailVerified = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Profile = new UserProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DisplayName = resolvedName,
                    AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl)
                        ? "https://cdn.falumni.local/avatars/default.jpg"
                        : avatarUrl,
                    Campus = CampusNormalizer.NormalizeOrDefault(request.Campus),
                    UpdatedAtUtc = DateTime.UtcNow
                }
            };

            await _userRepository.CreateAsync(user, cancellationToken);
            _logger.LogInformation("Created new user {UserId} ({Email}) via Google SSO.", user.Id, user.Email);
        }
        else if (user.Status.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Account has been suspended.");
        }
        else if (user.Status.Equals("PendingVerification", StringComparison.OrdinalIgnoreCase))
        {
            user.Status = "Active";
            user.EmailVerified = true;
            await _userRepository.UpdateAsync(user, cancellationToken);
        }

        return await GenerateAuthTokensAsync(user, cancellationToken);
    }

    public async Task<AuthTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var storedToken = await _userRepository.GetRefreshTokenAsync(request.RefreshToken?.Trim() ?? string.Empty, cancellationToken);
        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAtUtc < DateTime.UtcNow || storedToken.User is null)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        var user = storedToken.User;
        if (user.Status.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Account has been suspended.");
        }

        await _userRepository.RevokeRefreshTokenAsync(storedToken, cancellationToken);

        return await GenerateAuthTokensAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(Guid userId, LogoutRequest? request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            var token = await _userRepository.GetRefreshTokenAsync(request.RefreshToken.Trim(), cancellationToken);
            if (token is not null && token.UserId == userId)
            {
                await _userRepository.RevokeRefreshTokenAsync(token, cancellationToken);
                return;
            }
        }

        await _userRepository.RevokeAllUserRefreshTokensAsync(userId, cancellationToken);
        _logger.LogInformation("User {UserId} logged out and revoked refresh tokens.", userId);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new ArgumentException("Current password is incorrect.");
        }

        user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.RevokeAllUserRefreshTokensAsync(userId, cancellationToken);

        _logger.LogInformation("Changed password for user {UserId}", userId);
    }

    public async Task<RegisterResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new NotFoundException(nameof(User), email);

        var otpCode = GenerateOtpCode();
        var resetToken = new VerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OtpCode = otpCode,
            Purpose = "PasswordReset",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _userRepository.CreateVerificationTokenAsync(resetToken, cancellationToken);

        await _emailService.SendOtpEmailAsync(
            toEmail: user.Email,
            fullName: user.FullName,
            otpCode: otpCode,
            purpose: "PasswordReset",
            cancellationToken: cancellationToken);

        return new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Status = user.Status,
            Message = "Password reset OTP has been generated and sent to your email.",
            VerificationOtpPreview = otpCode
        };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.");
        }
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new NotFoundException(nameof(User), email);

        var token = await _userRepository.GetValidVerificationTokenAsync(
            user.Id,
            request.OtpCode?.Trim() ?? string.Empty,
            "PasswordReset",
            cancellationToken);

        if (token is null)
        {
            throw new ArgumentException("Invalid or expired password reset OTP code.");
        }

        await _userRepository.MarkVerificationTokenUsedAsync(token, cancellationToken);

        user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.RevokeAllUserRefreshTokensAsync(user.Id, cancellationToken);

        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);
    }

    private async Task<AuthTokenResponse> GenerateAuthTokensAsync(User user, CancellationToken cancellationToken)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT signing key ('Jwt:Key') is not configured.");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "https://identity.falumni.local";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "falumni-services";

        var now = DateTime.UtcNow;
        var expires = now.AddHours(1);

        var primaryRole = NormalizeRole(user.Role, user.AccountType);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new("role", primaryRole),
            new(ClaimTypes.Role, primaryRole),
            new("status", user.Status),
            new("accountType", user.AccountType),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Also include "USER" in ClaimTypes.Role for ALUMNI/STUDENT so any service checking [Authorize(Roles = "USER,ADMIN")] continues to work seamlessly
        if (!string.Equals(primaryRole, "USER", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(primaryRole, "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, "USER"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwtToken = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        var refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAtUtc = now.AddDays(7),
            IsRevoked = false,
            CreatedAtUtc = now
        };

        await _userRepository.CreateRefreshTokenAsync(refreshToken, cancellationToken);

        return new AuthTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresIn = 3600,
            User = user.ToResponse()
        };
    }

    public static (string Role, string AccountType) ResolveRoleAndAccountType(
        string? requestedRole,
        string? requestedAccountType,
        int? graduationYear = null)
    {
        var rawRole = requestedRole?.Trim() ?? string.Empty;
        var rawType = requestedAccountType?.Trim() ?? string.Empty;

        if (rawRole.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
            rawType.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return ("ADMIN", "Admin");
        }

        if (rawRole.Equals("STUDENT", StringComparison.OrdinalIgnoreCase) ||
            rawType.Equals("Student", StringComparison.OrdinalIgnoreCase) ||
            rawType.Equals("Sinh viên", StringComparison.OrdinalIgnoreCase))
        {
            return ("STUDENT", "Student");
        }

        if (rawRole.Equals("ALUMNI", StringComparison.OrdinalIgnoreCase) ||
            rawType.Equals("Alumni", StringComparison.OrdinalIgnoreCase) ||
            rawType.Equals("Cựu sinh viên", StringComparison.OrdinalIgnoreCase))
        {
            return ("ALUMNI", "Alumni");
        }

        if (graduationYear.HasValue && graduationYear.Value > DateTime.UtcNow.Year)
        {
            return ("STUDENT", "Student");
        }

        return ("ALUMNI", "Alumni");
    }

    private static string NormalizeRole(string? role, string? accountType)
    {
        if (string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(accountType, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return "ADMIN";
        }

        if (string.Equals(role, "STUDENT", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(accountType, "Student", StringComparison.OrdinalIgnoreCase))
        {
            return "STUDENT";
        }

        if (string.Equals(role, "ALUMNI", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(accountType, "Alumni", StringComparison.OrdinalIgnoreCase))
        {
            return "ALUMNI";
        }

        return string.IsNullOrWhiteSpace(role) ? "ALUMNI" : role.ToUpperInvariant();
    }

    private static bool IsAllowedFptEmail(string email)
    {
        return email.EndsWith("@fpt.edu.vn", StringComparison.OrdinalIgnoreCase)
            || email.EndsWith("@fe.edu.vn", StringComparison.OrdinalIgnoreCase)
            || email.EndsWith("@falumni.local", StringComparison.OrdinalIgnoreCase);
    }

    private static string GenerateOtpCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }
}
