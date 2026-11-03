using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminService.Services.DTOs.AuditLogs;
using AdminService.Services.DTOs.Common;
using AdminService.Services.Interfaces;

namespace AdminService.Api.Controllers;

/// <summary>
/// Administrative Audit Trail Endpoints (AD-07).
/// Provides immutable historical records of all administrator actions across the platform.
/// Requires ADMIN role.
/// </summary>
[ApiController]
[Route("api/v1/admin/audit-logs")]
[Produces("application/json")]
[Authorize(Policy = "RequireAdminRole")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(
        IAuditLogService auditLogService,
        ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves paginated audit log entries with multi-filter query support (AD-07).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<AuditLogResponse>>> GetLogs(
        [FromQuery] AuditLogFilterQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogService.GetAuditLogsAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a quick list of recent administrative actions for the dashboard activity feed.
    /// </summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> GetRecent(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _auditLogService.GetRecentActionsAsync(Math.Clamp(count, 1, 50), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves full details of a specific audit log record.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AuditLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditLogResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var log = await _auditLogService.GetAuditLogByIdAsync(id, cancellationToken);
        if (log is null)
        {
            return NotFound(new { message = $"Audit log with ID '{id}' was not found." });
        }

        return Ok(log);
    }
}
