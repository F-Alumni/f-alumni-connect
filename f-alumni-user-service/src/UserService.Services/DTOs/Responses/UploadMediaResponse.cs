namespace UserService.Services.DTOs.Responses;

public class UploadMediaResponse
{
    public string SecureUrl { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public long Bytes { get; set; }
}
