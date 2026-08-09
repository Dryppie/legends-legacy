using Application.UseCases.Quests.Commands.AcknowledgeQuestWelcome;
using Application.UseCases.Quests.Commands.PinQuest;
using Application.UseCases.Quests.Commands.SelectQuestChoice;
using Application.UseCases.Quests.Commands.StartQuestEncounter;
using Application.UseCases.Quests.Dtos;
using Application.UseCases.Quests.Queries.GetCombatAreaAccess;
using Application.UseCases.Quests.Queries.GetQuestJournal;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize]
public sealed class QuestController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<QuestJournalDto>> Get() =>
        await Mediator.Send(new GetQuestJournalQuery(CurrentCharacterGuid));

    [HttpGet("area-access")]
    public async Task<ActionResult<IReadOnlyList<CombatAreaAccessDto>>> GetAreaAccess() =>
        Ok(await Mediator.Send(new GetCombatAreaAccessQuery(CurrentCharacterGuid)));

    [HttpPost("welcome/acknowledge")]
    public async Task<ActionResult<Response<QuestJournalDto>>> AcknowledgeWelcome() =>
        await Mediator.Send(new AcknowledgeQuestWelcomeCommand(CurrentCharacterGuid));

    [HttpPut("pinned")]
    public async Task<ActionResult<Response<QuestJournalDto>>> Pin(PinQuestRequest request) =>
        await Mediator.Send(new PinQuestCommand(CurrentCharacterGuid, request.QuestId));

    [HttpPost("{questId}/choice")]
    public async Task<ActionResult<Response<QuestJournalDto>>> SelectChoice(
        string questId,
        SelectQuestChoiceRequest request) =>
        await Mediator.Send(
            new SelectQuestChoiceCommand(CurrentCharacterGuid, questId, request.OptionKey));

    [HttpPost("{questId}/encounters/{encounterKey}/start")]
    public async Task<ActionResult<Response<Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos.CombatResultDto>>> StartEncounter(
        string questId,
        string encounterKey) =>
        await Mediator.Send(
            new StartQuestEncounterCommand(CurrentCharacterGuid, questId, encounterKey));
}
