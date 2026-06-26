using Application.Interfaces.Services.LL.Achievements;
using Application.MediatR.Markers;
using Application.UseCases.Achievements.Dtos;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Achievements.Queries.GetAchievementOverview;

public record GetAchievementOverviewQuery(Guid AccountId, Guid CharacterId) : IQuery<Response<AchievementOverviewDto>>;

public sealed class GetAchievementOverviewQueryHandler
    : IRequestHandler<GetAchievementOverviewQuery, Response<AchievementOverviewDto>>
{
    private readonly IAchievementService _achievementService;

    public GetAchievementOverviewQueryHandler(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    public async Task<Response<AchievementOverviewDto>> Handle(
        GetAchievementOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var overview = await _achievementService.GetOverviewAsync(
            request.AccountId,
            request.CharacterId,
            cancellationToken);

        return Response<AchievementOverviewDto>.Success(overview);
    }
}
