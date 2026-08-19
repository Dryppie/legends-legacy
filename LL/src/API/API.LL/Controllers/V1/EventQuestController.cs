using Application.UseCases.Quests.Events.Commands.ClaimEventQuestReward;
using Application.UseCases.Quests.Events.Commands.ClaimEventQuestMilestone;
using Application.UseCases.Quests.Events.Commands.ClaimAllEventQuestMilestones;
using Application.UseCases.Quests.Events.Dtos;
using Application.UseCases.Quests.Events.Queries.GetEventQuestJournal;
using API.LL.Common;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.LL.Controllers.V1;

[Authorize(Policy = AuthorizationPolicies.MultiplayerAllowed)]
public sealed class EventQuestController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<EventQuestJournalDto>> Get() =>
        await Mediator.Send(new GetEventQuestJournalQuery(CurrentCharacterGuid));

    [HttpPost("{eventQuestId}/claim")]
    public async Task<ActionResult<Response<EventQuestJournalDto>>> Claim(string eventQuestId) =>
        await Mediator.Send(new ClaimEventQuestRewardCommand(CurrentCharacterGuid, eventQuestId));

    [HttpPost("{eventQuestId}/milestones/{milestoneKey}/claim")]
    public async Task<ActionResult<Response<EventQuestJournalDto>>> ClaimMilestone(
        string eventQuestId,
        string milestoneKey) =>
        await Mediator.Send(new ClaimEventQuestMilestoneCommand(
            CurrentCharacterGuid,
            eventQuestId,
            milestoneKey));

    [HttpPost("{eventQuestId}/milestones/claim-all")]
    public async Task<ActionResult<Response<EventQuestJournalDto>>> ClaimAllMilestones(
        string eventQuestId) =>
        await Mediator.Send(new ClaimAllEventQuestMilestonesCommand(
            CurrentCharacterGuid,
            eventQuestId));
}
