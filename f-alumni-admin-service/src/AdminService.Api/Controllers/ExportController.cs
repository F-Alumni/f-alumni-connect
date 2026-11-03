using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminService.Api.Extensions;
using AdminService.Services.DTOs.AuditLogs;
using AdminService.Services.DTOs.Users;
using AdminService.Services.Interfaces;

namespace AdminService.Api.Controllers;

/// <summary>
/// Platform Data Export Endpoints (AD-09).
/// Generates compliant CSV spreadsheets of audit trails and alumni directory records.
/// Requires ADMIN role.
/// </summary>
[ApiController]
[Route("api/v1/admin/export")]
[Produces("text/csv")]
[Authorize(Policy = "RequireAdminRole")]
public class ExportController : ControllerBase
{
    private readonly IDataExportService _exportService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        IDataExportService exportService,
        ILogger<ExportController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Exports platform administrative audit logs matching query criteria as a UTF-8 CSV spreadsheet (AD-09).
    /// </summary>
    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] AuditLogFilterQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin requested CSV export of platform audit logs.");
        var csvBytes = await _exportService.ExportAuditLogsCsvAsync(query, cancellationToken);
        var filename = $"falumni-audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(csvBytes, "text/csv; charset=utf-8", filename);
    }

    /// <summary>
    /// Exports current alumni and student user summary directory as a UTF-8 CSV spreadsheet (AD-09).
    /// </summary>
    [HttpGet("users-summary")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportUsersSummary(
        [FromQuery] UserFilterQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin requested CSV export of user directory summary.");
        var token = Request.GetBearerToken();
        var csvBytes = await _exportService.ExportUsersSummaryCsvAsync(query, token, cancellationToken);
        var filename = $"falumni-users-summary-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(csvBytes, "text/csv; charset=utf-8", filename);
    }
}
