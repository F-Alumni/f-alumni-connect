using Microsoft.Extensions.Logging;
using AiService.Repositories.Interfaces;
using AiService.Services.DTOs.Requests;
using AiService.Services.DTOs.Responses;
using AiService.Services.Interfaces;

namespace AiService.Services.Implementations;

public class MentorRecommendationService : IMentorRecommendationService
{
    private readonly IRagRepository _ragRepository;
    private readonly ILogger<MentorRecommendationService> _logger;

    public MentorRecommendationService(IRagRepository ragRepository, ILogger<MentorRecommendationService> logger)
    {
        _ragRepository = ragRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MentorResponse>> RecommendMentorsAsync(RecommendMentorRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var allMentors = await _ragRepository.GetMentorsAsync(null, cancellationToken);
        var targetMajor = request.Major?.Trim() ?? string.Empty;
        var targetSkills = (request.TargetSkills ?? new List<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToLower())
            .ToList();

        var scored = new List<(Repositories.Entities.MentorProfile Mentor, int Score)>();

        foreach (var m in allMentors)
        {
            int score = 0;

            // Major match
            if (!string.IsNullOrWhiteSpace(targetMajor) &&
                string.Equals(m.Major, targetMajor, StringComparison.OrdinalIgnoreCase))
            {
                score += 50;
            }

            // Skill matches
            var mentorSkills = (m.Skills ?? string.Empty).ToLower();
            foreach (var skill in targetSkills)
            {
                if (mentorSkills.Contains(skill))
                {
                    score += 15;
                }
            }

            // Career goal match
            if (!string.IsNullOrWhiteSpace(request.CareerGoal))
            {
                var goal = request.CareerGoal.ToLower();
                var jobTitle = (m.JobTitle ?? string.Empty).ToLower();
                var bio = (m.Bio ?? string.Empty).ToLower();
                if (jobTitle.Contains(goal) || bio.Contains(goal))
                {
                    score += 20;
                }
            }

            scored.Add((m, Math.Min(100, Math.Max(score, 30))));
        }

        var results = scored
            .OrderByDescending(s => s.Score)
            .Select(s => new MentorResponse
            {
                Id = s.Mentor.Id,
                UserId = s.Mentor.UserId,
                FullName = s.Mentor.FullName,
                Email = s.Mentor.Email,
                Major = s.Mentor.Major,
                CurrentCompany = s.Mentor.CurrentCompany,
                JobTitle = s.Mentor.JobTitle,
                Skills = s.Mentor.Skills,
                Bio = s.Mentor.Bio,
                AvatarUrl = s.Mentor.AvatarUrl,
                MatchScore = s.Score
            })
            .ToList();

        _logger.LogInformation("Gợi ý mentor cho ngành {Major}: {Count} kết quả", targetMajor, results.Count);
        return results;
    }
}
