using Application.Interfaces.Services.LL.Quests;
using Application.MediatR.Markers;
using Application.UseCases.Quests.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Quests.Queries.GetQuestJournal;

public sealed record GetQuestJournalQuery(Guid CharacterId) : IQuery<QuestJournalDto>;

public sealed class GetQuestJournalQueryHandler(
    IQuestService questService,
    IMapper mapper) : IRequestHandler<GetQuestJournalQuery, QuestJournalDto>
{
    public async Task<QuestJournalDto> Handle(
        GetQuestJournalQuery request,
        CancellationToken cancellationToken) =>
        mapper.Map<QuestJournalDto>(
            await questService.GetJournalAsync(request.CharacterId, cancellationToken));
}
