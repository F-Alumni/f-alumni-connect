namespace AdminService.Services.DTOs.Users;

public class AdminUserSummaryDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? SuspensionReason { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? StudentCode { get; set; }
    public string? Major { get; set; }
    public string? Campus { get; set; }
    public int? GraduationYear { get; set; }
    public string? CurrentCompany { get; set; }
    public string? JobTitle { get; set; }
}
