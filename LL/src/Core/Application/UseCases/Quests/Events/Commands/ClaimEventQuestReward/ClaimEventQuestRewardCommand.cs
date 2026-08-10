using Application.Interfaces.Services.LL.Quests.Events;
using Application.MediatR.Markers;
using Application.UseCases.Quests.Events.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Quests.Events.Commands.ClaimEventQuestReward;

public sealed record ClaimEventQuestRewardCommand(Guid CharacterId, string EventQuestId)
    : ICommand<Response<EventQuestJournalDto>>;

public sealed class ClaimEventQuestRewardCommandHandler(
    IEventQuestService service,
    IMapper mapper) : IRequestHandler<ClaimEventQuestRewardCommand, Response<EventQuestJournalDto>>
{
    public async Task<Response<EventQuestJournalDto>> Handle(
        ClaimEventQuestRewardCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var journal = await service.ClaimAsync(
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
