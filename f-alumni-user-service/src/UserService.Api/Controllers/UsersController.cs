using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Services.DTOs.Common;
using UserService.Services.DTOs.Requests;
using UserService.Services.DTOs.Responses;
using UserService.Services.Interfaces;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserProfileAppService _userProfileService;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserProfileAppService userProfileService,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<UsersController> logger)
    {
        _userProfileService = userProfileService;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// US-06 (Sheet) / US-09 (Guideline) &amp; AD-01/AD-02: Search and filter alumni directory by name, campus, major, graduationYear, company.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<UserProfileResponse>>> GetDirectory(
        [FromQuery] string? search = null,
        [FromQuery] string? campus = null,
        [FromQuery] int? graduationYear = null,
        [FromQuery] string? major = null,
        [FromQuery] string? company = null,
        [FromQuery] string? status = null,
        [FromQuery] string? role = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _userProfileService.GetDirectoryPagedAsync(
            search,
            campus,
            graduationYear,
            major,
            company,
            status,
            role,
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// US-06 (Sheet) / US-09 (Guideline) Alias: Search alumni directory.
    /// </summary>
    [HttpGet("directory")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<PagedResponse<UserProfileResponse>>> GetDirectoryAlias(
        [FromQuery] string? search = null,
        [FromQuery] string? campus = null,
        [FromQuery] int? graduationYear = null,
        [FromQuery] string? major = null,
        [FromQuery] string? company = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return GetDirectory(search, campus, graduationYear, major, company, "Active", null, pageNumber, pageSize, cancellationToken);
    }

    /// <summary>
    /// Get current authenticated user's full profile (extracted strictly from JWT claims).
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> GetMyProfile(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var profile = await _userProfileService.GetByIdAsync(userId, isOwnerOrAdmin: true, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// US-04 (Sheet) / US-06 (Guideline): Update current authenticated user's profile (name, avatar, campus, major, graduationYear, bio, calendar link).
    /// </summary>
    [HttpPut("me")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyProfile(
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var updated = await _userProfileService.UpdateProfileAsync(userId, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Update current authenticated user's avatar URL.
    /// </summary>
    [HttpPut("me/avatar")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyAvatar(
        [FromBody] UpdateAvatarRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var updated = await _userProfileService.UpdateAvatarAsync(userId, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Secure Backend-Mediated Image Upload to Cloudinary.
    /// Frontend sends multipart/form-data with valid JWT Bearer token.
    /// Cloudinary API Secret is strictly kept on the backend.
    /// </summary>
    [HttpPost("media/upload")]
    [Authorize]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(UploadMediaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadMediaResponse>> UploadMedia(
        IFormFile? file,
        [FromQuery] string? folder = "falumni/uploads",
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Tệp tin rỗng",
                Detail = "Vui lòng chọn tệp tin hình ảnh hợp lệ để tải lên.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // 1. Max size: 5 MB
        const long maxSizeBytes = 5 * 1024 * 1024;
        if (file.Length > maxSizeBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new ProblemDetails
            {
                Title = "Dung lượng tệp quá lớn",
                Detail = "Dung lượng tệp hình ảnh không được vượt quá 5 MB.",
                Status = StatusCodes.Status413PayloadTooLarge
            });
        }

        // 2. Extension validation
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp" };
        var blockedExtensions = new HashSet<string> { ".svg", ".exe", ".bat", ".cmd", ".sh", ".php", ".js", ".ts", ".html", ".htm", ".dll", ".jar", ".vbs" };

        if (blockedExtensions.Contains(ext) || !allowedExtensions.Contains(ext))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Định dạng tệp không được hỗ trợ",
                Detail = "Hệ thống chỉ chấp nhận định dạng ảnh JPEG, PNG hoặc WEBP. Tệp SVG và tệp thực thi bị cấm hoàn toàn.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // 3. MIME validation
        var allowedMimes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };
        if (!allowedMimes.Contains(file.ContentType))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "MIME type không hợp lệ",
                Detail = "Content-Type của tệp không khớp với định dạng ảnh cho phép (image/jpeg, image/png, image/webp).",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // 4. Magic bytes verification
        byte[] headerBytes = new byte[12];
        using (var stream = file.OpenReadStream())
        {
            var read = await stream.ReadAsync(headerBytes.AsMemory(0, 12), cancellationToken);
            if (read < 4)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Tệp không hợp lệ",
                    Detail = "Không thể đọc chữ ký tệp tin.",
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        bool isJpeg = headerBytes[0] == 0xFF && headerBytes[1] == 0xD8 && headerBytes[2] == 0xFF;
        bool isPng = headerBytes[0] == 0x89 && headerBytes[1] == 0x50 && headerBytes[2] == 0x4E && headerBytes[3] == 0x47;
        bool isWebp = headerBytes[0] == 0x52 && headerBytes[1] == 0x49 && headerBytes[2] == 0x46 && headerBytes[3] == 0x46 &&
                      headerBytes[8] == 0x57 && headerBytes[9] == 0x45 && headerBytes[10] == 0x42 && headerBytes[11] == 0x50;

        if (!isJpeg && !isPng && !isWebp)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Chữ ký tệp không hợp lệ",
                Detail = "Nội dung tệp không khớp với chữ ký nhị phân của ảnh JPEG, PNG hoặc WEBP.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // 5. Cloudinary credentials from backend configuration
        var cloudName = _configuration["Cloudinary:CloudName"] ?? "fxco0zeu";
        var apiKey = _configuration["Cloudinary:ApiKey"] ?? "954633752654961";
        var apiSecret = _configuration["Cloudinary:ApiSecret"] ?? "kM__nc9y6TuAgEnRkUv50pClHdI";

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var cleanFolder = string.IsNullOrWhiteSpace(folder) ? "falumni/uploads" : folder.Trim();

        var parameters = new Dictionary<string, string>
        {
            { "folder", cleanFolder },
            { "timestamp", timestamp }
        };
        var signature = ComputeCloudinarySignature(parameters, apiSecret);

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            using var form = new MultipartFormDataContent();

            using var fileStream = file.OpenReadStream();
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "file", file.FileName);

            form.Add(new StringContent(apiKey), "api_key");
            form.Add(new StringContent(timestamp), "timestamp");
            form.Add(new StringContent(signature), "signature");
            form.Add(new StringContent(cleanFolder), "folder");

            var cloudinaryUrl = $"https://api.cloudinary.com/v1_1/{cloudName}/image/upload";
            var response = await httpClient.PostAsync(cloudinaryUrl, form, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Cloudinary upload failed with status {StatusCode}: {Response}", response.StatusCode, responseBody);
                return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
                {
                    Title = "Lỗi lưu trữ hình ảnh",
                    Detail = "Dịch vụ lưu trữ hình ảnh Cloudinary không thể hoàn tất yêu cầu.",
                    Status = StatusCodes.Status502BadGateway
                });
            }

            using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            var secureUrl = root.TryGetProperty("secure_url", out var su) ? su.GetString() ?? string.Empty : string.Empty;
            var publicId = root.TryGetProperty("public_id", out var pi) ? pi.GetString() ?? string.Empty : string.Empty;
            var format = root.TryGetProperty("format", out var fmt) ? fmt.GetString() ?? string.Empty : string.Empty;
            var bytes = root.TryGetProperty("bytes", out var b) ? b.GetInt64() : file.Length;

            return Ok(new UploadMediaResponse
            {
                SecureUrl = secureUrl,
                PublicId = publicId,
                Format = format,
                Bytes = bytes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error uploading media to Cloudinary.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Lỗi máy chủ",
                Detail = "Đã xảy ra lỗi trong quá trình xử lý tải lên tệp tin.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Upload avatar image and immediately persist it to current user's profile.
    /// </summary>
    [HttpPost("me/avatar/upload")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileResponse>> UploadAndSetMyAvatar(
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var uploadResult = await UploadMedia(file, "falumni/avatars", cancellationToken);
        if (uploadResult.Result is not OkObjectResult okObj || okObj.Value is not UploadMediaResponse media)
        {
            return uploadResult.Result ?? StatusCode(StatusCodes.Status500InternalServerError);
        }

        var profile = await _userProfileService.UpdateAvatarAsync(
            userId,
            new UpdateAvatarRequest { AvatarUrl = media.SecureUrl },
            cancellationToken);

        return Ok(profile);
    }

    private static string ComputeCloudinarySignature(IDictionary<string, string> parameters, string apiSecret)
    {
        var sorted = parameters
            .Where(p => p.Key != "file" && p.Key != "api_key" && p.Key != "resource_type" && !string.IsNullOrWhiteSpace(p.Value))
            .OrderBy(p => p.Key)
            .Select(p => $"{p.Key}={p.Value}");
        var toSign = string.Join("&", sorted) + apiSecret;
        using var sha1 = SHA1.Create();
        var hashBytes = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(toSign));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// US-11 (Sheet): Add or update Google Calendar booking link on user profile.
    /// </summary>
    [HttpPut("me/calendar")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyCalendarLink(
        [FromBody] UpdateCalendarLinkRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var updated = await _userProfileService.UpdateCalendarLinkAsync(userId, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// US-09 (Sheet): Customize which fields are publicly visible on user profile.
    /// </summary>
    [HttpPut("me/privacy")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyPrivacy(
        [FromBody] UpdatePrivacySettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var updated = await _userProfileService.UpdatePrivacySettingsAsync(userId, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// US-10 (Sheet): Get list of users blocked by the current user.
    /// </summary>
    [HttpGet("me/blocks")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(IReadOnlyList<BlockedUserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BlockedUserResponse>>> GetMyBlockedUsers(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var list = await _userProfileService.GetBlockedUsersAsync(userId, cancellationToken);
        return Ok(list);
    }

    /// <summary>
    /// US-10 (Sheet): Block another user account.
    /// </summary>
    [HttpPost("{id:guid}/block")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(BlockedUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BlockedUserResponse>> BlockUser(
        Guid id,
        [FromBody] BlockUserRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var blocked = await _userProfileService.BlockUserAsync(userId, id, request ?? new BlockUserRequest(), cancellationToken);
        return Ok(blocked);
    }

    /// <summary>
    /// US-10 (Sheet): Unblock a previously blocked user account.
    /// </summary>
    [HttpDelete("{id:guid}/block")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnblockUser(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        await _userProfileService.UnblockUserAsync(userId, id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// US-05 (Sheet) / US-08 (Guideline): Get public profile of a user by ID (respects US-09 privacy settings).
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var isOwnerOrAdmin = User.IsInRole("ADMIN") || (TryGetCurrentUserId(out var currentId) && currentId == id);
        var profile = await _userProfileService.GetByIdAsync(id, isOwnerOrAdmin, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// AD-03: Authorized endpoint for Admin / Admin Service to suspend a user account.
    /// </summary>
    [HttpPost("{id:guid}/suspend")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> SuspendUser(
        Guid id,
        [FromBody] SuspendUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userProfileService.SuspendUserAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// AD-03 / AD-04: Authorized endpoint for Admin / Admin Service to unsuspend a user account.
    /// </summary>
    [HttpPost("{id:guid}/unsuspend")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> UnsuspendUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _userProfileService.UnsuspendUserAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update user role (USER / ADMIN) and account type (Student / Alumni / Admin).
    /// </summary>
    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> UpdateRole(
        Guid id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userProfileService.UpdateUserRoleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var claimVal = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claimVal, out userId);
    }
}
