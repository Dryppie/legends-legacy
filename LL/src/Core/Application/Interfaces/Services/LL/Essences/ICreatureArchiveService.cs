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
}
