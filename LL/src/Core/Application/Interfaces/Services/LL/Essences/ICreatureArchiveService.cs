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

    Task<CreatureArchive> GetCreatureArchiveAsync(Guid characterId, CancellationToken cancellationToken);
    Task<EssenceCodex> GetEssenceCodexAsync(Guid characterId, CancellationToken cancellationToken);
    Task<CreatureArchive> SetEssenceFocusAsync(Guid characterId, string? creatureId, CancellationToken cancellationToken);
    Task<string?> GetEssenceFocusCreatureIdAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> IsEssenceFocusAsync(Guid characterId, string creatureId, CancellationToken cancellationToken);
}
