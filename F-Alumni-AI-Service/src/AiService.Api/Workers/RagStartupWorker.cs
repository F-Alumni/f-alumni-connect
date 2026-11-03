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

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ragRepo = scope.ServiceProvider.GetRequiredService<IRagRepository>();
            var count = await ragRepo.GetChunkCountAsync(stoppingToken);

            var resolvedDir = ResolveRagDirectory();
            if (count == 0 && !string.IsNullOrWhiteSpace(resolvedDir))
            {
                _logger.LogInformation("Cơ sở dữ liệu tri thức RAG đang trống. Bắt đầu tự động nạp từ: {Dir}", resolvedDir);
                var ingestionService = scope.ServiceProvider.GetRequiredService<IRagIngestionService>();
                var res = await ingestionService.IngestDocumentsFromDirectoryAsync(resolvedDir, forceReindex: false, stoppingToken);
                _logger.LogInformation("Tự động nạp hoàn tất: {Msg}", res.Message);
            }
            else if (count > 0)
            {
                _logger.LogInformation("Hệ thống RAG đã có sẵn {Count} đoạn tri thức.", count);
            }
            else
            {
                _logger.LogWarning("Không tìm thấy thư mục tài liệu RAG hợp lệ. Vui lòng kiểm tra cấu hình 'Rag:DataDirectory'.");
            }
        }
        catch (Exception ex)
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
            if (Directory.Exists(dir) && Directory.GetFiles(dir, "*.docx", SearchOption.AllDirectories).Length > 0)
            {
                return dir;
            }
        }

        return null;
    }
}
