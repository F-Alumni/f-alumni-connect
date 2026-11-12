using System.ComponentModel.DataAnnotations;

namespace AiService.Services.DTOs.Requests;

public class AiChatRequest
{
    [Required(ErrorMessage = "Câu hỏi không được để trống.")]
    public string Question { get; set; } = string.Empty;

    public Guid? ConversationId { get; set; }
    public string? Campus { get; set; } = "All";
    public string? Major { get; set; } = "All";
}

public class RecommendMentorRequest
{
    [Required(ErrorMessage = "Chuyên ngành không được để trống.")]
    public string Major { get; set; } = "SE";

    public List<string> TargetSkills { get; set; } = new();
    public string? CareerGoal { get; set; }
}

public class IngestRagRequest
{
    public string? DirectoryPath { get; set; }
    public bool ForceReindex { get; set; } = false;
}

public class BoostTestRequest
{
    public string Query { get; set; } = string.Empty;
    public string? Q { get; set; }
    public string? ProgramCode { get; set; }
    public string? Major { get; set; }
    public string? Campus { get; set; } = "All";
    public int TopK { get; set; } = 5;
}
