using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Services.Interfaces;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Internal event ingestion bridge so sibling microservices can deliver domain events
/// synchronously over HTTP when Kafka is disabled in local/dev environments.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/internal/notifications")]
public class InternalNotificationsController : ControllerBase
{
    private readonly INotificationEventProcessor _processor;
    private readonly ILogger<InternalNotificationsController> _logger;

    public InternalNotificationsController(
        INotificationEventProcessor processor,
        ILogger<InternalNotificationsController> logger)
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

        var count = await _processor.ProcessRawEventAsync(topic, rawJson, cancellationToken);
        _logger.LogInformation("Processed internal HTTP notification event for topic {Topic}: created={Count}", topic, count);
        return Ok(new { topic, createdCount = count });
    }
}
