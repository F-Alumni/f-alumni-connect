using Microsoft.AspNetCore.Mvc;
using AiService.Services.DTOs.Requests;
using AiService.Services.DTOs.Responses;
using AiService.Services.Interfaces;

namespace AiService.Api.Controllers;

[ApiController]
[Route("api/v1/ai/mentors")]
public class MentorsController : ControllerBase
{
    private readonly IMentorRecommendationService _mentorService;

    public MentorsController(IMentorRecommendationService mentorService)
    {
        _mentorService = mentorService;
    }

    /// <summary>
    /// Recommend Alumni mentors matching student's major, target skills, and career aspirations.
    /// </summary>
    [HttpPost("recommend")]
    [ProducesResponseType(typeof(IReadOnlyList<MentorResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Recommend([FromBody] RecommendMentorRequest request, CancellationToken cancellationToken = default)
    {
        var mentors = await _mentorService.RecommendMentorsAsync(request, cancellationToken);
        return Ok(mentors);
    }
}
