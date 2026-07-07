using Application.UseCases.Tutorials.Commands.RecordCraftingPageVisited;
using Application.UseCases.Tutorials.Commands.StartTrainingBattle;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Application.UseCases.Tutorials.Dtos;
using Application.UseCases.Tutorials.Queries.GetTutorialState;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public sealed class TutorialController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<TutorialStateDto>> Get() =>
        await Mediator.Send(new GetTutorialStateQuery(CurrentCharacterGuid));

    [HttpPost("visit-crafting")]
    public async Task<ActionResult<TutorialStateDto>> VisitCrafting() =>
        await Mediator.Send(new RecordCraftingPageVisitedCommand(CurrentCharacterGuid));

    [HttpPost("start-training-battle")]
    public async Task<ActionResult<Response<CombatResultDto>>> StartTrainingBattle() =>
        await Mediator.Send(new StartTrainingBattleCommand(CurrentCharacterGuid));
}
