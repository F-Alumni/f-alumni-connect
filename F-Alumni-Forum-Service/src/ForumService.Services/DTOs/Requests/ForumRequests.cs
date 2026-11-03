using System.ComponentModel.DataAnnotations;

namespace ForumService.Services.DTOs.Requests;

public class CreatePostRequest
{
    [Required(ErrorMessage = "Tiêu đề bài viết không được để trống.")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "Tiêu đề phải từ 5 đến 300 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung bài viết không được để trống.")]
    [MinLength(10, ErrorMessage = "Nội dung bài viết phải có ít nhất 10 ký tự.")]
    public string Content { get; set; } = string.Empty;

    public string PostType { get; set; } = "Sharing"; // Question, Sharing, Recruitment
    public string Major { get; set; } = "All"; // SE, AI, IA, GD, IB, MC, MKT, All
    public string Campus { get; set; } = "All"; // FU-HCM, FU-HL, FU-DN, FU-CT, FU-QN, All

    public List<string> ImageUrls { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public class UpdatePostRequest
{
    [Required(ErrorMessage = "Tiêu đề bài viết không được để trống.")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "Tiêu đề phải từ 5 đến 300 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung bài viết không được để trống.")]
    [MinLength(10, ErrorMessage = "Nội dung bài viết phải có ít nhất 10 ký tự.")]
    public string Content { get; set; } = string.Empty;

    public string PostType { get; set; } = "Sharing";
    public string Major { get; set; } = "All";
    public string Campus { get; set; } = "All";
    public bool? IsAnswered { get; set; }

    public List<string> ImageUrls { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public class CreateCommentRequest
{
    [Required(ErrorMessage = "Nội dung bình luận không được để trống.")]
    public string Content { get; set; } = string.Empty;

    public Guid? ParentCommentId { get; set; }
}

public class UpdateCommentRequest
{
    [Required(ErrorMessage = "Nội dung bình luận không được để trống.")]
    public string Content { get; set; } = string.Empty;
}

public class CreateReportRequest
{
    [Required]
    public string TargetType { get; set; } = "Post"; // Post, Comment

    [Required]
    public Guid TargetId { get; set; }

    [Required(ErrorMessage = "Lý do báo cáo không được để trống.")]
    public string Reason { get; set; } = string.Empty;

    public string? Details { get; set; }
}

public class CreateEventRequest
{
    [Required(ErrorMessage = "Tiêu đề sự kiện không được để trống.")]
    [StringLength(300, MinimumLength = 3, ErrorMessage = "Tiêu đề phải từ 3 đến 300 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả sự kiện không được để trống.")]
    public string Description { get; set; } = string.Empty;

    public string? BannerUrl { get; set; }

    [Required(ErrorMessage = "Địa điểm tổ chức không được để trống.")]
    public string Location { get; set; } = string.Empty;

    public string Campus { get; set; } = "All";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? MaxAttendees { get; set; }
}

public class RsvpEventRequest
{
    [Required(ErrorMessage = "Trạng thái tham gia không được để trống.")]
    public string Status { get; set; } = "Going"; // Going, Interested, NotGoing
}
