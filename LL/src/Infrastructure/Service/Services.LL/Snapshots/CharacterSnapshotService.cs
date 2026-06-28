using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.Snapshots;
using Services.LL.Interfaces;

namespace Services.LL.Snapshots;

public class CharacterSnapshotService : ICharacterSnapshotService
{
    private readonly ICharacterSnapshotRepository _repository;
    private readonly ICombatStyleService _combatStyles;

    public CharacterSnapshotService(
        ICharacterSnapshotRepository repository,
        ICombatStyleService combatStyles)
    {
        _repository = repository;
        _combatStyles = combatStyles;
    }

    public async Task<CharacterSnapshot> CreateAsync(Guid characterId, CancellationToken ct)
    {
        var snapshot = await _repository.CreateAsync(characterId, ct);
        snapshot.CombatStyle = await _combatStyles.GetActiveSnapshotAsync(characterId, ct);
        return snapshot;
    }

    public async Task<CharacterSnapshot?> GetSnapshotByCharacterIdAsync(Guid characterId, CancellationToken ct)
    {
        return await _repository.GetSnapshotByCharacterIdAsync(characterId, ct);
    }

    public async Task<CharacterSnapshot?> GetSnapshotByIdAsync(Guid snapshotId, CancellationToken ct)
    {
        return await _repository.GetSnapshotByIdAsync(snapshotId, ct);
    }
}
