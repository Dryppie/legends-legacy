using Application.UseCases.Achievements.Dtos;
using Application.UseCases.Titles.Commands.EquipTitle;
using Application.UseCases.Titles.Commands.UnequipTitle;
using Application.UseCases.Titles.Queries.GetTitles;
using Common.Primitives;
using Domain.Models.Achievements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public sealed class TitlesController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<Response<IReadOnlyList<TitleDto>>>> Get(
        [FromQuery] AchievementCategory? category,
        [FromQuery] TitleRarity? rarity,
        [FromQuery] bool? unlocked,
        [FromQuery] string? search) =>
        await Mediator.Send(new GetTitlesQuery(
            CurrentUserId,
            CurrentCharacterGuid,
            category,
            rarity,
            unlocked,
            search));

    [HttpPost("equip")]
    public async Task<ActionResult<Response<EquippedTitleDto>>> Equip([FromBody] EquipTitleRequest request) =>
        await Mediator.Send(new EquipTitleCommand(
            CurrentUserId,
            CurrentCharacterGuid,
            request.TitleKey));

    [HttpPost("unequip")]
    public async Task<ActionResult<Response<EquippedTitleDto?>>> Unequip() =>
        await Mediator.Send(new UnequipTitleCommand(CurrentUserId, CurrentCharacterGuid));
}
