using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;

namespace Application.Interfaces.Services.LL.Essences;

public interface ICreatureArchiveService
{
    Task RecordDefeatedCreaturesAsync(
        Guid characterId,
        IReadOnlyCollection<Creature> creatures,
        DateTimeOffset defeatedAtUtc,
        CancellationToken cancellationToken);

    async Task RecordDefeatedCreatureBatchesAsync(
        Guid characterId,
        IReadOnlyList<CreatureDefeatBatch> batches,
        CancellationToken cancellationToken)
    {
        foreach (var batch in batches)
        {
            await RecordDefeatedCreaturesAsync(
                characterId,
                batch.Creatures,
                batch.DefeatedAtUtc,
                cancellationToken);
        }
    }

    Task<CreatureArchive> GetCreatureArchiveAsync(Guid characterId, CancellationToken cancellationToken);
    Task<EssenceCodex> GetEssenceCodexAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CreatureArchive> SetEssenceFocusAsync(Guid characterId, string? creatureId, CancellationToken cancellationToken);
    Task<string?> GetEssenceFocusCreatureIdAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> IsEssenceFocusAsync(Guid characterId, string creatureId, CancellationToken cancellationToken);
}

public sealed record CreatureDefeatBatch(
    IReadOnlyCollection<Creature> Creatures,
    DateTimeOffset DefeatedAtUtc);
