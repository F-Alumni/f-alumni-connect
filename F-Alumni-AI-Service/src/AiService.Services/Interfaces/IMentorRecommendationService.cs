using AiService.Services.DTOs.Requests;
using AiService.Services.DTOs.Responses;

namespace AiService.Services.Interfaces;

public interface IMentorRecommendationService
{
    Task<IReadOnlyList<MentorResponse>> RecommendMentorsAsync(RecommendMentorRequest request, CancellationToken cancellationToken = default);
}
