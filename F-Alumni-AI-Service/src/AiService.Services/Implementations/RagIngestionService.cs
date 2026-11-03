using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AiService.Repositories.Entities;
using AiService.Repositories.Interfaces;
using AiService.Services.DTOs.Responses;
using AiService.Services.Interfaces;

namespace AiService.Services.Implementations;

public class RagIngestionService : IRagIngestionService
{
    private readonly IRagRepository _ragRepository;
    private readonly ILogger<RagIngestionService> _logger;

    public RagIngestionService(IRagRepository ragRepository, ILogger<RagIngestionService> logger)
    {
        _ragRepository = ragRepository;
        _logger = logger;
    }

    public async Task<RagIngestionResult> IngestDocumentsFromDirectoryAsync(string directoryPath, bool forceReindex = false, CancellationToken cancellationToken = default)
    {
        var result = new RagIngestionResult();

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning("Thư mục RAG không tồn tại tại: {Path}", directoryPath);
            result.Message = $"Thư mục RAG không tồn tại: {directoryPath}";
            return result;
        }

        var files = Directory.GetFiles(directoryPath, "*.docx", SearchOption.AllDirectories);
        _logger.LogInformation("Tìm thấy {Count} tệp .docx trong {Path}", files.Length, directoryPath);

        foreach (var file in files)
        {
            try
            {
                var relativePath = Path.GetRelativePath(directoryPath, file);
                var fileName = Path.GetFileName(file);

                var existing = await _ragRepository.GetDocumentByPathAsync(file, cancellationToken);
                if (existing != null && !forceReindex)
                {
                    _logger.LogDebug("Tệp {FileName} đã tồn tại trong tri thức, bỏ qua.", fileName);
                    continue;
                }

                if (existing != null && forceReindex)
                {
                    await _ragRepository.DeleteDocumentByPathAsync(file, cancellationToken);
                }

                var text = ExtractTextFromDocx(file);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var doc = new RagDocument
                {
                    Title = GetDocumentTitle(fileName),
                    SourcePath = file,
                    DocumentType = DetermineDocumentType(fileName, file),
                    ProgramCode = DetermineProgramCode(fileName),
                    Campus = DetermineCampus(fileName, text),
                    CreatedAt = DateTime.UtcNow
                };

                var savedDoc = await _ragRepository.AddDocumentAsync(doc, cancellationToken);
                var chunks = ParseChunks(savedDoc, text);

                if (chunks.Count > 0)
                {
                    await _ragRepository.AddChunksAsync(chunks, cancellationToken);
                    result.TotalChunksCreated += chunks.Count;
                }

                result.TotalFilesProcessed++;
                result.IngestedFiles.Add(fileName);
                _logger.LogInformation("Đã nạp thành công {FileName} ({Chunks} chunks)", fileName, chunks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi nạp tệp RAG: {File}", file);
            }
        }

        // Seed initial Mentor profiles if database is empty
        await SeedMentorsIfEmptyAsync(cancellationToken);

        result.Message = $"Đã nạp thành công {result.TotalFilesProcessed} tài liệu với {result.TotalChunksCreated} đoạn tri thức.";
        return result;
    }

    public async Task IngestForumPostAsync(Guid postId, string title, string content, string authorName, string major, string campus, CancellationToken cancellationToken = default)
    {
        var sourcePath = $"forum/post/{postId}";
        await _ragRepository.DeleteDocumentByPathAsync(sourcePath, cancellationToken);

        var doc = new RagDocument
        {
            Title = $"[Forum Post] {title}",
            SourcePath = sourcePath,
            DocumentType = "ForumPost",
            ProgramCode = major,
            Campus = campus,
            CreatedAt = DateTime.UtcNow
        };

        var savedDoc = await _ragRepository.AddDocumentAsync(doc, cancellationToken);

        var chunk = new RagChunk
        {
            DocumentId = savedDoc.Id,
            ChunkIdentifier = $"post_{postId}",
            Section = "Kinh nghiệm từ cộng đồng FPTU",
            Content = $"Tác giả: {authorName}\nChuyên ngành: {major} | Cơ sở: {campus}\nTiêu đề: {title}\nNội dung: {content}",
            Keywords = $"{title} {major} {campus} {authorName}"
        };

        await _ragRepository.AddChunksAsync(new[] { chunk }, cancellationToken);
        _logger.LogInformation("Đã đồng bộ bài viết {PostId} vào cơ sở tri thức AI", postId);
    }

    private static string ExtractTextFromDocx(string filePath)
    {
        using var zip = ZipFile.OpenRead(filePath);
        var entry = zip.GetEntry("word/document.xml");
        if (entry == null) return string.Empty;

        using var reader = new StreamReader(entry.Open());
        var rawXml = reader.ReadToEnd();

        // Convert <w:p> to newlines and strip remaining XML tags
        var formatted = Regex.Replace(rawXml, @"</w:p>", "\n");
        var decoded = WebUtility.HtmlDecode(Regex.Replace(formatted, @"<[^>]+>", " "));
        return Regex.Replace(decoded, @"[ \t]+", " ").Trim();
    }

    private static List<RagChunk> ParseChunks(RagDocument doc, string text)
    {
        var chunks = new List<RagChunk>();

        // 1. If content is pre-chunked JSON array
        var trimmed = text.Trim();
        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(trimmed);
                int index = 1;
                foreach (var el in jsonDoc.RootElement.EnumerateArray())
                {
                    var id = el.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? $"chunk_{index}" : $"chunk_{index}";
                    string section = "Chương trình đào tạo";
                    if (el.TryGetProperty("metadata", out var meta))
                    {
                        if (meta.TryGetProperty("section", out var secProp))
                        {
                            section = secProp.GetString() ?? section;
                        }
                    }

                    string chunkText = string.Empty;
                    if (el.TryGetProperty("text", out var textProp))
                    {
                        chunkText = textProp.GetString() ?? string.Empty;
                    }
                    else if (el.TryGetProperty("content", out var contentProp))
                    {
                        chunkText = contentProp.GetString() ?? string.Empty;
                    }

                    if (!string.IsNullOrWhiteSpace(chunkText))
                    {
                        chunks.Add(new RagChunk
                        {
                            DocumentId = doc.Id,
                            ChunkIdentifier = id,
                            Section = section,
                            Content = chunkText,
                            Keywords = $"{doc.ProgramCode} {section}"
                        });
                    }
                    index++;
                }

                if (chunks.Count > 0)
                {
                    return chunks;
                }
            }
            catch
            {
                // Fallback to text chunking if JSON parsing failed
            }
        }

        // 2. Structured text chunking by sections or paragraphs
        var sections = Regex.Split(text, @"(?=(?:===|---|\b(?:\d+\.|[A-Z\s]{4,}:)\s))");
        int chunkIdx = 1;

        foreach (var sec in sections)
        {
            var content = sec.Trim();
            if (content.Length < 40) continue;

            // Extract section title from first line
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var sectionTitle = lines.Length > 0 ? lines[0].Trim().Trim('=', '-', '#') : $"Phần {chunkIdx}";
            if (sectionTitle.Length > 200) sectionTitle = sectionTitle[..200];

            chunks.Add(new RagChunk
            {
                DocumentId = doc.Id,
                ChunkIdentifier = $"chunk_{chunkIdx}",
                Section = string.IsNullOrWhiteSpace(sectionTitle) ? "Thông tin chung" : sectionTitle,
                Content = content,
                Keywords = $"{doc.Title} {doc.Campus} {sectionTitle}"
            });
            chunkIdx++;
        }

        if (chunks.Count == 0 && !string.IsNullOrWhiteSpace(text))
        {
            chunks.Add(new RagChunk
            {
                DocumentId = doc.Id,
                ChunkIdentifier = "chunk_1",
                Section = "Thông tin chung",
                Content = text,
                Keywords = doc.Title
            });
        }

        return chunks;
    }

    private static string GetDocumentTitle(string fileName)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        return nameWithoutExt switch
        {
            "BIT_SE_K19B" => "Khung chương trình Kỹ thuật phần mềm (BIT_SE_K19B)",
            "BIT_AI_K18D-19A" => "Khung chương trình Trí tuệ nhân tạo (BIT_AI)",
            "BIT_IA_K18D-19A" => "Khung chương trình An toàn thông tin (BIT_IA)",
            "BIT_GD_K18D19A" => "Khung chương trình Thiết kế đồ họa (BIT_GD)",
            "BBA_IB_K18B" => "Khung chương trình Kinh doanh quốc tế (BBA_IB)",
            "BBA_MC_K16DK17A" => "Khung chương trình Truyền thông đa phương tiện (BBA_MC)",
            "BBA_MKT_K16-D17A" => "Khung chương trình Marketing (BBA_MKT)",
            _ => nameWithoutExt
        };
    }

    private static string DetermineDocumentType(string fileName, string fullPath)
    {
        if (fileName.Contains("Học phí", StringComparison.OrdinalIgnoreCase)) return "Tuition";
        if (fileName.Contains("vật chất", StringComparison.OrdinalIgnoreCase)) return "Facilities";
        if (fileName.Contains("năm nhất", StringComparison.OrdinalIgnoreCase)) return "FirstYearSubjects";
        if (fileName.Contains("thông tin khác", StringComparison.OrdinalIgnoreCase)) return "Slang";
        if (fullPath.Contains("Curriculum", StringComparison.OrdinalIgnoreCase)) return "Curriculum";
        return "Regulations";
    }

    private static string? DetermineProgramCode(string fileName)
    {
        if (fileName.Contains("BIT_SE", StringComparison.OrdinalIgnoreCase)) return "BIT_SE";
        if (fileName.Contains("BIT_AI", StringComparison.OrdinalIgnoreCase)) return "BIT_AI";
        if (fileName.Contains("BIT_IA", StringComparison.OrdinalIgnoreCase)) return "BIT_IA";
        if (fileName.Contains("BIT_GD", StringComparison.OrdinalIgnoreCase)) return "BIT_GD";
        if (fileName.Contains("BBA_IB", StringComparison.OrdinalIgnoreCase)) return "BBA_IB";
        if (fileName.Contains("BBA_MC", StringComparison.OrdinalIgnoreCase)) return "BBA_MC";
        if (fileName.Contains("BBA_MKT", StringComparison.OrdinalIgnoreCase)) return "BBA_MKT";
        return null;
    }

    private static string DetermineCampus(string fileName, string text)
    {
        if (text.Contains("Hola", StringComparison.OrdinalIgnoreCase) && text.Contains("Xavalo", StringComparison.OrdinalIgnoreCase)) return "All";
        if (text.Contains("Hoa Lac", StringComparison.OrdinalIgnoreCase) || text.Contains("Hola", StringComparison.OrdinalIgnoreCase)) return "FU-HL";
        if (text.Contains("Xavalo", StringComparison.OrdinalIgnoreCase) || text.Contains("Ho Chi Minh", StringComparison.OrdinalIgnoreCase)) return "FU-HCM";
        if (text.Contains("Da Nang", StringComparison.OrdinalIgnoreCase)) return "FU-DN";
        if (text.Contains("Can Tho", StringComparison.OrdinalIgnoreCase)) return "FU-CT";
        if (text.Contains("Quy Nhon", StringComparison.OrdinalIgnoreCase)) return "FU-QN";
        return "All";
    }

    private async Task SeedMentorsIfEmptyAsync(CancellationToken cancellationToken)
    {
        var existingMentors = await _ragRepository.GetMentorsAsync(null, cancellationToken);
        if (existingMentors.Count > 0) return;

        var mentors = new[]
        {
            new MentorProfile
            {
                FullName = "Nguyễn Hữu Trí (Alumni K13)",
                Email = "tri.nh@alumni.fpt.edu.vn",
                Major = "SE",
                CurrentCompany = "FPT Software",
                JobTitle = "Senior Software Engineer / Tech Lead",
                Skills = "C#, .NET 10, Microservices, Docker, Kafka, Clean Architecture",
                Bio = "Cựu sinh viên K13 FPTU HCM. Hỗ trợ đồ án tốt nghiệp, định hướng kiến trúc Microservices và chuẩn bị phỏng vấn OJT.",
                AvatarUrl = "https://api.dicebear.com/7.x/bottts/svg?seed=tri"
            },
            new MentorProfile
            {
                FullName = "Lê Hoàng Yến (Alumni K14)",
                Email = "yen.lh@alumni.fpt.edu.vn",
                Major = "AI",
                CurrentCompany = "VinAI Research",
                JobTitle = "AI Research Engineer",
                Skills = "Python, PyTorch, LLM, RAG, Computer Vision, MLOps",
                Bio = "Cựu sinh viên K14 FPTU Hola. Chuyên gia về ứng dụng mô hình ngôn ngữ lớn (LLM) và hệ thống tìm kiếm tăng cường truy xuất (RAG).",
                AvatarUrl = "https://api.dicebear.com/7.x/bottts/svg?seed=yen"
            },
            new MentorProfile
            {
                FullName = "Phạm Quang Minh (Alumni K15)",
                Email = "minh.pq@alumni.fpt.edu.vn",
                Major = "IA",
                CurrentCompany = "Viettel Cyber Security",
                JobTitle = "Security Consultant",
                Skills = "Penetration Testing, SOC, DevSecOps, Network Security",
                Bio = "Cựu sinh viên K15 ngành An toàn thông tin. Hỗ trợ giải CTF, tư vấn chứng chỉ OSCP/CISSP.",
                AvatarUrl = "https://api.dicebear.com/7.x/bottts/svg?seed=minh"
            }
        };

        foreach (var m in mentors)
        {
            await _ragRepository.AddMentorAsync(m, cancellationToken);
        }
    }
}
