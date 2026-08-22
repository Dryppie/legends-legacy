namespace Domain.Models.Snapshots;

public interface ICharacterSnapshotRepository
{
    Task<CharacterSnapshot> CreateAsync(Guid characterId, CancellationToken ct);
    Task<CharacterSnapshot> CreateAsync(
        Guid characterId,
        Domain.Models.Essences.EssenceCombatActivity activity,
        CancellationToken ct) =>
        CreateAsync(characterId, ct);
    Task<CharacterSnapshot?> GetSnapshotByCharacterIdAsync(Guid characterId, CancellationToken ct);
    Task<CharacterSnapshot?> GetSnapshotByIdAsync(Guid snapshotId, CancellationToken ct);
}
