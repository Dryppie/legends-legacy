using Application.Interfaces.Services.LL.Achievements;
using Application.MediatR.Markers;
using Application.UseCases.Achievements.Dtos;
using Common.Primitives;
using Domain.Models.Achievements;
using MediatR;

namespace Application.UseCases.Achievements.Queries.GetAchievements;

public record GetAchievementsQuery(
    Guid AccountId,
    Guid CharacterId,
    AchievementCategory? Category,
    AchievementVisibility? Visibility,
    bool? Completed,
    string? Search) : IQuery<Response<IReadOnlyList<AchievementDto>>>;

public sealed class GetAchievementsQueryHandler
    : IRequestHandler<GetAchievementsQuery, Response<IReadOnlyList<AchievementDto>>>
{
    private readonly IAchievementService _achievementService;

    public GetAchievementsQueryHandler(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    public async Task<Response<IReadOnlyList<AchievementDto>>> Handle(
        GetAchievementsQuery request,
        CancellationToken cancellationToken)
    {
        var achievements = await _achievementService.GetAchievementsAsync(
            request.AccountId,
            request.CharacterId,
            new AchievementFilters
            {
                Category = request.Category,
                Visibility = request.Visibility,
                Completed = request.Completed,
                Search = request.Search
            },
            cancellationToken);

        return Response<IReadOnlyList<AchievementDto>>.Success(achievements);
    }
}
