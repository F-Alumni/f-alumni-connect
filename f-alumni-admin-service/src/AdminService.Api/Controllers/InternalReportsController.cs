using System.Text;
using AdminService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Api.Controllers;

/// <summary>
/// Internal event ingestion bridge so ForumService can deliver report events synchronously
/// over HTTP when Kafka is disabled in local/dev environments.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/internal/moderation/reports")]
public class InternalReportsController : ControllerBase
{
    private readonly IReportEventProcessor _processor;
    private readonly ILogger<InternalReportsController> _logger;

    public InternalReportsController(
        IReportEventProcessor processor,
        ILogger<InternalReportsController> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    [HttpPost("events/{topic}")]
    public async Task<IActionResult> IngestEvent(
        [FromRoute] string topic,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawJson = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return BadRequest(new { message = "Empty event payload." });
        }

        var inserted = await _processor.ProcessRawEventAsync(topic, rawJson, cancellationToken);
        _logger.LogInformation("Processed internal HTTP report event for topic {Topic}: inserted={Inserted}", topic, inserted);
        return Ok(new { topic, inserted });
    }
}
