using Application.Interfaces.Services.LL.Quests.Events;
using Application.MediatR.Markers;
using Application.UseCases.Quests.Events.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Quests.Events.Commands.ClaimAllEventQuestMilestones;

public sealed record ClaimAllEventQuestMilestonesCommand(Guid CharacterId, string EventQuestId)
    : ICommand<Response<EventQuestJournalDto>>;

public sealed class ClaimAllEventQuestMilestonesCommandHandler(
    IEventQuestService service,
    IMapper mapper) : IRequestHandler<ClaimAllEventQuestMilestonesCommand, Response<EventQuestJournalDto>>
{
    public async Task<Response<EventQuestJournalDto>> Handle(
        ClaimAllEventQuestMilestonesCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var journal = await service.ClaimAllMilestonesAsync(
                request.CharacterId,
                request.EventQuestId,
                cancellationToken);
            return Response<EventQuestJournalDto>.Success(mapper.Map<EventQuestJournalDto>(journal));
        }
        catch (InvalidOperationException exception)
        {
            return Response<EventQuestJournalDto>.Fail(exception.Message);
        }
    }
}
