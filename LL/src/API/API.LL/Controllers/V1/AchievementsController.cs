using Application.UseCases.Achievements.Commands.RecalculateAchievements;
using Application.UseCases.Achievements.Dtos;
using Application.UseCases.Achievements.Queries.GetAchievementOverview;
using Application.UseCases.Achievements.Queries.GetAchievements;
using Common.Primitives;
using Domain.Models.Achievements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public sealed class AchievementsController : BaseController
{
    [HttpGet("overview")]
    public async Task<ActionResult<Response<AchievementOverviewDto>>> Overview() =>
        await Mediator.Send(new GetAchievementOverviewQuery(CurrentUserId, CurrentCharacterGuid));

    [HttpGet]
    public async Task<ActionResult<Response<IReadOnlyList<AchievementDto>>>> Get(
        [FromQuery] AchievementCategory? category,
        [FromQuery] AchievementVisibility? visibility,
        [FromQuery] bool? completed,
        [FromQuery] string? search) =>
        await Mediator.Send(new GetAchievementsQuery(
            CurrentUserId,
            CurrentCharacterGuid,
            category,
            visibility,
            completed,
            search));

    [HttpPost("recalculate")]
    public async Task<ActionResult<Response<AchievementRecalculationResultDto>>> Recalculate() =>
        await Mediator.Send(new RecalculateAchievementsCommand(CurrentUserId, CurrentCharacterGuid));
}
