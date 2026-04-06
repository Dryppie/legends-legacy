using Domain.Models.Snapshots;

namespace Services.LL.Interfaces;

public interface ICharacterSnapshotService
{
    Task<CharacterSnapshot> CreateAsync(Guid characterId, CancellationToken ct);
    Task<CharacterSnapshot?> GetSnapshotByCharacterIdAsync(Guid characterId, CancellationToken ct);
}