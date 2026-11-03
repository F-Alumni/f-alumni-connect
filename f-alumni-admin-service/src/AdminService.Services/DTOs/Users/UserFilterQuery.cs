namespace AdminService.Services.DTOs.Users;

public class UserFilterQuery
{
    public string? Search { get; set; }
    public string? Campus { get; set; }
    public string? Major { get; set; }
    public int? GraduationYear { get; set; }
    public string? Company { get; set; }
    public string? Status { get; set; }
    public string? Role { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
