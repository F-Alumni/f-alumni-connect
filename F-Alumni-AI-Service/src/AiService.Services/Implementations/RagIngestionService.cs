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
    private static readonly SemaphoreSlim IngestionLock = new(1, 1);
    private readonly IRagRepository _ragRepository;
    private readonly ILogger<RagIngestionService> _logger;

    public RagIngestionService(IRagRepository ragRepository, ILogger<RagIngestionService> logger)
    {
        _ragRepository = ragRepository;
        _logger = logger;
    }

    public async Task<RagIngestionResult> IngestDocumentsFromDirectoryAsync(string directoryPath, bool forceReindex = false, CancellationToken cancellationToken = default)
    {
        await IngestionLock.WaitAsync(cancellationToken);
        try
        {
            return await IngestDocumentsInternalAsync(directoryPath, forceReindex, cancellationToken);
        }
        finally
        {
            IngestionLock.Release();
        }
    }

    private async Task<RagIngestionResult> IngestDocumentsInternalAsync(string directoryPath, bool forceReindex, CancellationToken cancellationToken)
    {
        var result = new RagIngestionResult();

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning("Thư mục RAG không tồn tại tại: {Path}", directoryPath);
            result.Message = $"Thư mục RAG không tồn tại: {directoryPath}";
            return result;
        }

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".docx", ".md", ".txt" };
        var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .Where(f => allowedExtensions.Contains(Path.GetExtension(f)))
            .ToArray();
        _logger.LogInformation("Tìm thấy {Count} tệp tài liệu (.docx/.md/.txt) trong {Path}", files.Length, directoryPath);

        foreach (var file in files)
        {
            try
            {
                var relativePath = Path.GetRelativePath(directoryPath, file).Replace('\\', '/');
                var fileName = Path.GetFileName(file);
                var ext = Path.GetExtension(file);

                var existing = await _ragRepository.GetDocumentByPathAsync(relativePath, cancellationToken)
                    ?? await _ragRepository.GetDocumentByPathAsync(file, cancellationToken);
                if (existing != null && !forceReindex)
                {
                    _logger.LogDebug("Tệp {FileName} đã tồn tại trong tri thức, bỏ qua.", fileName);
                    continue;
                }

                if (existing != null && forceReindex)
                {
                    await _ragRepository.DeleteDocumentByPathAsync(existing.SourcePath, cancellationToken);
                }

                var text = string.Equals(ext, ".docx", StringComparison.OrdinalIgnoreCase)
                    ? ExtractTextFromDocx(file)
                    : await File.ReadAllTextAsync(file, System.Text.Encoding.UTF8, cancellationToken);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var docType = DetermineDocumentType(fileName, file);
                var doc = new RagDocument
                {
                    Title = GetDocumentTitle(fileName),
                    SourcePath = relativePath,
                    DocumentType = docType,
                    ProgramCode = DetermineProgramCode(fileName),
                    Campus = DetermineCampus(docType),
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

        var docTitle = $"[Forum Post] {title}";
        if (docTitle.Length > 300) docTitle = docTitle[..300];
        var safeMajor = string.IsNullOrWhiteSpace(major) ? null : (major.Length > 50 ? major[..50] : major);
        var safeCampus = string.IsNullOrWhiteSpace(campus) ? "All" : (campus.Length > 50 ? campus[..50] : campus);

        var doc = new RagDocument
        {
            Title = docTitle,
            SourcePath = sourcePath.Length > 500 ? sourcePath[..500] : sourcePath,
            DocumentType = "ForumPost",
            ProgramCode = safeMajor,
            Campus = safeCampus,
            CreatedAt = DateTime.UtcNow
        };

        var savedDoc = await _ragRepository.AddDocumentAsync(doc, cancellationToken);

        var chunkId = $"post_{postId}";
        var chunk = new RagChunk
        {
            DocumentId = savedDoc.Id,
            ChunkIdentifier = chunkId.Length > 150 ? chunkId[..150] : chunkId,
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

        // Convert paragraph, row, and line-break tags to newlines and strip remaining XML tags
        var formatted = Regex.Replace(rawXml, @"</w:p>|</w:tr>|<w:br[^>]*/?>", "\n");
        var decoded = WebUtility.HtmlDecode(Regex.Replace(formatted, @"<[^>]+>", " "));
        var lines = decoded
            .Split('\n')
            .Select(l => Regex.Replace(l, @"[ \t]+", " ").Trim());
        return string.Join("\n", lines).Trim();
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
                var shortCode = (doc.ProgramCode ?? string.Empty).Replace("BIT_", "").Replace("BBA_", "");
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
                            ChunkIdentifier = id.Length > 150 ? id[..150] : id,
                            Section = section.Length > 300 ? section[..300] : section,
                            Content = chunkText.Trim(),
                            Keywords = $"{doc.Title} {doc.ProgramCode} {shortCode} {section}".Trim()
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

        // 2. Structured text chunking preserving DOCUMENT and === SECTION === headers without dropping content
        var normalized = Regex.Replace(text, @"={5,}\s*\r?\n\s*([^\r\n]+?)\s*\r?\n\s*={5,}", "\n<<<SECTION>>> $1\n");
        normalized = Regex.Replace(normalized, @"-{5,}\s*\r?\n\s*([^\r\n]+?)\s*\r?\n\s*-{5,}", "\n<<<SUBSECTION>>> $1\n");
        normalized = Regex.Replace(normalized, @"^[=\-]{5,}\s*$", string.Empty, RegexOptions.Multiline);

        var subDocs = Regex.Split(normalized, @"(?=^DOCUMENT:\s*)", RegexOptions.Multiline)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (subDocs.Count == 0)
        {
            subDocs.Add(normalized);
        }

        int chunkIdx = 1;
        foreach (var subDoc in subDocs)
        {
            var subDocLines = subDoc.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string subDocTitle = string.Empty;
            if (subDocLines.Length > 0 && subDocLines[0].StartsWith("DOCUMENT:", StringComparison.OrdinalIgnoreCase))
            {
                subDocTitle = subDocLines[0]["DOCUMENT:".Length..].Trim();
                if (string.IsNullOrWhiteSpace(subDocTitle) && subDocLines.Length > 1 && !subDocLines[1].StartsWith("<<<SECTION>>>"))
                {
                    subDocTitle = subDocLines[1].Trim();
                }
            }

            var sections = Regex.Split(subDoc, @"(?=<<<SECTION>>>)")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            foreach (var rawSec in sections)
            {
                var secText = rawSec.Trim();
                string sectionTitle = "Thông tin chung";

                if (secText.StartsWith("<<<SECTION>>>", StringComparison.Ordinal))
                {
                    var firstNewline = secText.IndexOf('\n');
                    if (firstNewline > 0)
                    {
                        sectionTitle = secText["<<<SECTION>>>".Length..firstNewline].Trim();
                        secText = secText[(firstNewline + 1)..].Trim();
                    }
                    else
                    {
                        sectionTitle = secText["<<<SECTION>>>".Length..].Trim();
                        secText = string.Empty;
                    }
                }
                else
                {
                    var lines = secText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0)
                    {
                        var first = lines[0].Replace("DOCUMENT:", "").Trim();
                        if (string.IsNullOrWhiteSpace(first) && lines.Length > 1)
                        {
                            first = lines[1].Trim();
                        }
                        if (!string.IsNullOrWhiteSpace(first))
                        {
                            sectionTitle = first.StartsWith("Liên kết một số tên gọi", StringComparison.OrdinalIgnoreCase)
                                ? "Thuật ngữ & Biệt danh cơ sở FPTU (Hola, Xavalo, Hovilo, Fuda)"
                                : (first.Length > 150 ? first[..150] : first);
                        }
                    }
                }

                secText = secText.Replace("<<<SUBSECTION>>> ", string.Empty).Replace("<<<SUBSECTION>>>", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(secText))
                {
                    continue;
                }

                var fullSectionLabel = !string.IsNullOrWhiteSpace(subDocTitle) && !sectionTitle.Equals(subDocTitle, StringComparison.OrdinalIgnoreCase)
                    ? $"{subDocTitle} - {sectionTitle}"
                    : sectionTitle;

                if (fullSectionLabel.Length > 300)
                {
                    fullSectionLabel = fullSectionLabel[..300];
                }

                var fullContent = $"{fullSectionLabel}\n{secText}".Trim();

                // If tiny orphan header/fragment (< 40 chars) and previous chunk exists in same doc, append rather than dropping
                if (secText.Length < 25 && chunks.Count > 0)
                {
                    chunks[^1].Content += "\n" + fullContent;
                    continue;
                }

                // Split very large chunks (> 2200 chars) on paragraph boundaries while keeping section header context
                if (fullContent.Length > 2200)
                {
                    var paragraphs = fullContent.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
                    if (paragraphs.Length <= 1)
                    {
                        paragraphs = fullContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    }

                    var buffer = new System.Text.StringBuilder();
                    int part = 1;
                    foreach (var p in paragraphs)
                    {
                        if (buffer.Length > 0 && buffer.Length + p.Length + 1 > 1800)
                        {
                            var partTitle = part == 1 ? fullSectionLabel : $"{fullSectionLabel} (Phần {part})";
                            if (partTitle.Length > 300) partTitle = partTitle[..300];
                            chunks.Add(new RagChunk
                            {
                                DocumentId = doc.Id,
                                ChunkIdentifier = $"chunk_{chunkIdx++}",
                                Section = partTitle,
                                Content = buffer.ToString().Trim(),
                                Keywords = $"{doc.Title} {doc.Campus} {partTitle}"
                            });
                            buffer.Clear();
                            buffer.AppendLine(fullSectionLabel);
                            part++;
                        }
                        buffer.AppendLine(p);
                    }

                    if (buffer.Length > 0)
                    {
                        var partTitle = part == 1 ? fullSectionLabel : $"{fullSectionLabel} (Phần {part})";
                        if (partTitle.Length > 300) partTitle = partTitle[..300];
                        chunks.Add(new RagChunk
                        {
                            DocumentId = doc.Id,
                            ChunkIdentifier = $"chunk_{chunkIdx++}",
                            Section = partTitle,
                            Content = buffer.ToString().Trim(),
                            Keywords = $"{doc.Title} {doc.Campus} {partTitle}"
                        });
                    }
                }
                else
                {
                    chunks.Add(new RagChunk
                    {
                        DocumentId = doc.Id,
                        ChunkIdentifier = $"chunk_{chunkIdx++}",
                        Section = string.IsNullOrWhiteSpace(fullSectionLabel) ? "Thông tin chung" : fullSectionLabel,
                        Content = fullContent,
                        Keywords = $"{doc.Title} {doc.Campus} {fullSectionLabel}"
                    });
                }
            }
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
        if (fileName.Contains("thông tin khác", StringComparison.OrdinalIgnoreCase)) return "Regulations";
        if (fullPath.Contains("Curriculum", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("BIT_", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("BBA_", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Tổng quan", StringComparison.OrdinalIgnoreCase)) return "Curriculum";
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

    private static string DetermineCampus(string docType)
    {
        // All institutional files (curriculums, first-year subjects, tuition, facilities, regulations)
        // apply university-wide or cover multiple campuses inside their chunk content.
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
