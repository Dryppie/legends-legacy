using Application.Interfaces.Services.LL.Achievements;
using Application.MediatR.Markers;
using Application.UseCases.Achievements.Dtos;
using Common.Primitives;
using Domain.Models.Achievements;
using MediatR;

namespace Application.UseCases.Titles.Queries.GetTitles;

public record GetTitlesQuery(
    Guid AccountId,
    Guid CharacterId,
    AchievementCategory? Category,
    TitleRarity? Rarity,
    bool? Unlocked,
    string? Search) : IQuery<Response<IReadOnlyList<TitleDto>>>;

public sealed class GetTitlesQueryHandler
    : IRequestHandler<GetTitlesQuery, Response<IReadOnlyList<TitleDto>>>
{
    private readonly IAchievementService _achievementService;

    public GetTitlesQueryHandler(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    public async Task<Response<IReadOnlyList<TitleDto>>> Handle(
        GetTitlesQuery request,
        CancellationToken cancellationToken)
    {
        var titles = await _achievementService.GetTitlesAsync(
            request.AccountId,
            request.CharacterId,
            new TitleFilters
            {
                Category = request.Category,
                Rarity = request.Rarity,
                Unlocked = request.Unlocked,
                Search = request.Search
            },
            cancellationToken);

        return Response<IReadOnlyList<TitleDto>>.Success(titles);
    }
}
