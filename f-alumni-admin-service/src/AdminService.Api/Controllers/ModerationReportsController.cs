using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminService.Api.Extensions;
using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Moderation;
using AdminService.Services.Interfaces;

namespace AdminService.Api.Controllers;

/// <summary>
/// Administrative Moderation Queue Endpoints (AD-05, AD-06).
/// Manages reported inappropriate posts, comments, or abusive member accounts.
/// Requires ADMIN role.
/// </summary>
[ApiController]
[Route("api/v1/admin/moderation/reports")]
[Produces("application/json")]
[Authorize(Policy = "RequireAdminRole")]
public class ModerationReportsController : ControllerBase
{
    private readonly IModerationReportService _reportService;
    private readonly ILogger<ModerationReportsController> _logger;

    public ModerationReportsController(
        IModerationReportService reportService,
        ILogger<ModerationReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of moderation reports (AD-05).
    /// </summary>
    [HttpGet]
    [HttpGet("/api/v1/admin/reports")]
    [ProducesResponseType(typeof(PagedResponse<ModerationReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<ModerationReportResponse>>> GetReports(
        [FromQuery] ModerationReportFilterQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetReportsAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves full details of a specific moderation report.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ModerationReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModerationReportResponse>> GetReportById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var report = await _reportService.GetReportByIdAsync(id, cancellationToken);
        if (report is null)
        {
            return NotFound(new { message = $"Moderation report with ID '{id}' was not found." });
        }

        return Ok(report);
    }

    /// <summary>
    /// Resolves a moderation report by taking disciplinary/administrative action (AD-06).
    /// Automatically records an immutable audit log entry.
    /// </summary>
    [HttpPost("{id:guid}/resolve")]
    [ProducesResponseType(typeof(ModerationReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModerationReportResponse>> ResolveReport(
        Guid id,
        [FromBody] ResolveReportRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId();
        var adminEmail = User.GetUserEmail();
        var ipAddress = HttpContext.GetClientIpAddress();
        var bearerToken = Request.GetBearerToken();

        var result = await _reportService.ResolveReportAsync(
            adminId, adminEmail, id, request, ipAddress, cancellationToken, bearerToken);

        return Ok(result);
    }

    /// <summary>
    /// Dismisses a reported violation if no action is warranted (AD-06).
    /// </summary>
    [HttpPost("{id:guid}/dismiss")]
    [ProducesResponseType(typeof(ModerationReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModerationReportResponse>> DismissReport(
        Guid id,
        [FromBody] DismissReportRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId();
        var adminEmail = User.GetUserEmail();
        var ipAddress = HttpContext.GetClientIpAddress();

        var result = await _reportService.DismissReportAsync(
            adminId, adminEmail, id, request, ipAddress, cancellationToken);

        return Ok(result);
    }
}
