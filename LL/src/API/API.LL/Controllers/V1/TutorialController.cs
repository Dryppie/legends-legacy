using Application.UseCases.Tutorials.Commands.CompleteClientTutorialStep;
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
    public async Task<ActionResult<TutorialStateDto?>> Get() =>
        await Mediator.Send(new GetTutorialStateQuery(CurrentCharacterGuid));

    [HttpPost("client-step")]
    public async Task<ActionResult<TutorialStateDto?>> CompleteClientStep(CompleteClientTutorialStepRequest request) =>
        await Mediator.Send(new CompleteClientTutorialStepCommand(CurrentCharacterGuid, request));

    [HttpPost("start-training-battle")]
    public async Task<ActionResult<Response<CombatResultDto>>> StartTrainingBattle() =>
        await Mediator.Send(new StartTrainingBattleCommand(CurrentCharacterGuid));
}
