using Application.Interfaces.Services.LL.Quests.Events;
using Application.MediatR.Markers;
using Application.UseCases.Quests.Events.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Quests.Events.Queries.GetEventQuestJournal;

public sealed record GetEventQuestJournalQuery(Guid CharacterId) : IQuery<EventQuestJournalDto>;

public sealed class GetEventQuestJournalQueryHandler(
    IEventQuestService service,
    IMapper mapper) : IRequestHandler<GetEventQuestJournalQuery, EventQuestJournalDto>
{
    public async Task<EventQuestJournalDto> Handle(
        GetEventQuestJournalQuery request,
        CancellationToken cancellationToken) =>
        mapper.Map<EventQuestJournalDto>(
            await service.GetJournalAsync(request.CharacterId, cancellationToken));
}
