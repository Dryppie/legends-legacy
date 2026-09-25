using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Quests;
using Application.MediatR.Markers;
using Application.MediatR.Synchronization;
using Application.UseCases.Quests.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Quests.Queries.GetQuestJournal;

public sealed record GetQuestJournalQuery(Guid CharacterId) : IQuery<QuestJournalDto>;

public sealed class GetQuestJournalQueryHandler(
    IDbContext db,
    IQuestService questService,
    IMapper mapper) : IRequestHandler<GetQuestJournalQuery, QuestJournalDto>
{
    public async Task<QuestJournalDto> Handle(
        GetQuestJournalQuery request,
        CancellationToken cancellationToken)
    {
        if (db.CurrentTransaction is not null)
        {
            return await ExecuteAsync(request, cancellationToken);
        }

        // Commands already wait on this process-local lock before opening a
        // transaction. Join that queue here so a concurrent journal snapshot
        // does not spend its database command timeout waiting for the same
        // character's PostgreSQL advisory lock.
        using var commandLock = await CharacterCommandLockRegistry.Instance.AcquireAsync(
            request.CharacterId,
            cancellationToken);

        return await ExecuteAsync(request, cancellationToken);
    }

    private Task<QuestJournalDto> ExecuteAsync(
        GetQuestJournalQuery request,
        CancellationToken cancellationToken) =>
        db.ExecuteWithCharacterLockAsync(
            request.CharacterId,
            async ct => mapper.Map<QuestJournalDto>(
                await questService.GetJournalAsync(request.CharacterId, ct)),
            cancellationToken);
}
