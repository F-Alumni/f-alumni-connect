using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AiService.Repositories.Interfaces;
using AiService.Services.Interfaces;

namespace AiService.Api.Workers;

public class RagStartupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RagStartupWorker> _logger;
    private readonly IConfiguration _configuration;

    public RagStartupWorker(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<RagStartupWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 1 second after startup
        await Task.Delay(1000, stoppingToken);

        await EnsureDocumentsIngestedAsync(stoppingToken);
    }

    /// <summary>
    /// Ensures all .docx documents from the configured RAG directory are ingested into the database,
    /// even if seed data or Kafka forum posts already exist.
    /// </summary>
    public async Task EnsureDocumentsIngestedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedDir = ResolveRagDirectory();
            if (string.IsNullOrWhiteSpace(resolvedDir))
            {
                _logger.LogWarning("Không tìm thấy thư mục tài liệu RAG hợp lệ. Vui lòng kiểm tra cấu hình 'Rag:DataDirectory'.");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var ragRepo = scope.ServiceProvider.GetRequiredService<IRagRepository>();
            var docxFiles = Directory.GetFiles(resolvedDir, "*.docx", SearchOption.AllDirectories);

            var hasMissingFiles = false;
            foreach (var file in docxFiles)
            {
                if (await ragRepo.GetDocumentByPathAsync(file, cancellationToken) is null)
                {
                    hasMissingFiles = true;
                    break;
                }
            }

            if (hasMissingFiles)
            {
                _logger.LogInformation("Phát hiện tài liệu RAG chưa được nạp từ: {Dir}. Bắt đầu tự động nạp...", resolvedDir);
                var ingestionService = scope.ServiceProvider.GetRequiredService<IRagIngestionService>();
                var res = await ingestionService.IngestDocumentsFromDirectoryAsync(resolvedDir, forceReindex: false, cancellationToken);
                var totalChunks = await ragRepo.GetChunkCountAsync(cancellationToken);
                _logger.LogInformation("Tự động nạp hoàn tất: {Msg} (Tổng số đoạn tri thức hiện có: {TotalChunks})", res.Message, totalChunks);
            }
            else
            {
                var totalChunks = await ragRepo.GetChunkCountAsync(cancellationToken);
                _logger.LogInformation("Hệ thống RAG đã có đầy đủ {FileCount} tài liệu từ {Dir} (Tổng {TotalChunks} đoạn tri thức).",
                    docxFiles.Length, resolvedDir, totalChunks);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Không thể tự động nạp dữ liệu RAG khi khởi động: {Message}", ex.Message);
        }
    }

    private string? ResolveRagDirectory()
    {
        var configured = _configuration["Rag:DataDirectory"]
            ?? _configuration["Rag:DefaultDirectory"]
            ?? "data/rag";

        var candidates = new List<string>();

        if (Path.IsPathRooted(configured))
        {
            candidates.Add(configured);
        }
        else
        {
            candidates.Add(Path.Combine(AppContext.BaseDirectory, configured));
            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), configured));
            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "data", "rag"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "data", "rag"));
        }

        foreach (var dir in candidates)
        {
            var normalized = Path.GetFullPath(dir);
            if (Directory.Exists(normalized) && Directory.GetFiles(normalized, "*.docx", SearchOption.AllDirectories).Length > 0)
            {
                return normalized;
            }
        }

        return null;
    }
}
