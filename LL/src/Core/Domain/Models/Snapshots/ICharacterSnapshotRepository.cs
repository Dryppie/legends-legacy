namespace Domain.Models.Snapshots;

public interface ICharacterSnapshotRepository
{
    Task<CharacterSnapshot> CreateAsync(Guid characterId, CancellationToken ct);
    Task<CharacterSnapshot?> GetSnapshotByCharacterIdAsync(Guid characterId, CancellationToken ct);
}
