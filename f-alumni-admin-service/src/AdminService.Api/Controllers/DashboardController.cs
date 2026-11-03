using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminService.Api.Extensions;
using AdminService.Services.DTOs.Dashboard;
using AdminService.Services.Interfaces;

namespace AdminService.Api.Controllers;

/// <summary>
/// Administrative Dashboard Analytics & KPI Endpoints (AD-01).
/// Provides high-level operational overview across users, moderation items, and system events.
/// Requires ADMIN role.
/// </summary>
[ApiController]
[Route("api/v1/admin/dashboard")]
[Produces("application/json")]
[Authorize(Policy = "RequireAdminRole")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves current system operational summary metrics for the Admin Dashboard (AD-01).
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AdminDashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminDashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var token = Request.GetBearerToken();
        var summary = await _dashboardService.GetSummaryAsync(token, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Retrieves administrative activity count and event breakdown for the specified period (AD-01).
    /// </summary>
    /// <param name="days">Number of past days to query (default 30, max 90).</param>
    [HttpGet("activity-timeline")]
    [ProducesResponseType(typeof(IReadOnlyList<ActivityTimelineItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ActivityTimelineItemDto>>> GetActivityTimeline(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var timeline = await _dashboardService.GetActivityTimelineAsync(days, cancellationToken);
        return Ok(timeline);
    }
}
