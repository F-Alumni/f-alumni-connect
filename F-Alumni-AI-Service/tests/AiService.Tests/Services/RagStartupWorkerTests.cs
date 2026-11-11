using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using AiService.Api.Data;
using AiService.Api.Workers;
using AiService.Repositories.Context;
using AiService.Repositories.Implementations;
using AiService.Repositories.Interfaces;
using AiService.Services.Implementations;
using AiService.Services.Interfaces;

namespace AiService.Tests.Services;

public class RagStartupWorkerTests
{
    [Fact]
    public async Task EnsureDocumentsIngestedAsync_WhenSeederAlreadyRan_StillIngestsMissingDocxFilesIdempotently()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AiServiceDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddLogging();
        services.AddScoped<IRagRepository, RagRepository>();
        services.AddScoped<IRagIngestionService, RagIngestionService>();

        var serviceProvider = services.BuildServiceProvider();

        // Simulate Program.cs startup sequence: AiDatabaseSeeder runs BEFORE RagStartupWorker
        await AiDatabaseSeeder.SeedAsync(serviceProvider);

        using (var checkScope = serviceProvider.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AiServiceDbContext>();
            Assert.Equal(3, await db.RagDocuments.CountAsync());
            Assert.Equal(4, await db.RagChunks.CountAsync());
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Rag:DataDirectory"] = "data/rag"
            })
            .Build();

        var worker = new RagStartupWorker(
            serviceProvider,
            config,
            NullLogger<RagStartupWorker>.Instance);

        // Act 1: First worker run should detect missing .docx files and ingest all 12 despite seed chunks existing
        await worker.EnsureDocumentsIngestedAsync();

        int firstRunDocsCount;
        int firstRunChunksCount;
        using (var checkScope = serviceProvider.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AiServiceDbContext>();
            firstRunDocsCount = await db.RagDocuments.CountAsync();
            firstRunChunksCount = await db.RagChunks.CountAsync();

            // 3 seed docs + 12 .docx files = 15 documents
            Assert.Equal(15, firstRunDocsCount);
            Assert.True(firstRunChunksCount > 4);
            Assert.Contains(await db.RagDocuments.ToListAsync(), d => d.ProgramCode == "BIT_SE");
            Assert.Contains(await db.RagDocuments.ToListAsync(), d => d.DocumentType == "Tuition");
        }

        // Act 2: Second worker run should be idempotent (no duplicate documents or chunks)
        await worker.EnsureDocumentsIngestedAsync();

        using (var checkScope = serviceProvider.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AiServiceDbContext>();
            Assert.Equal(firstRunDocsCount, await db.RagDocuments.CountAsync());
            Assert.Equal(firstRunChunksCount, await db.RagChunks.CountAsync());
        }
    }
}
