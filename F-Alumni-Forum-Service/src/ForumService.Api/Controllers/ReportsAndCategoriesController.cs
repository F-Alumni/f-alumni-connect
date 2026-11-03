using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ForumService.Api.Extensions;
using ForumService.Services.DTOs.Requests;
using ForumService.Services.DTOs.Responses;
using ForumService.Services.Interfaces;

namespace ForumService.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly IInteractionService _interactionService;

    public ReportsController(IInteractionService interactionService)
    {
        _interactionService = interactionService;
    }

    /// <summary>
    /// FO-11: Report an offensive or policy-violating post or comment.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var fullName = User.GetFullName();

        await _interactionService.CreateReportAsync(request, userId, fullName, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { Message = "Báo cáo của bạn đã được ghi nhận và gửi đến Ban Quản Trị." });
    }
}

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IInteractionService _interactionService;

    public CategoriesController(IInteractionService interactionService)
    {
        _interactionService = interactionService;
    }

    /// <summary>
    /// Get all forum discussion categories.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken = default)
    {
        var result = await _interactionService.GetCategoriesAsync(cancellationToken);
        return Ok(result);
    }
}
