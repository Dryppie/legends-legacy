using Domain.Models.Snapshots;
using Services.LL.Interfaces;

namespace Services.LL.Snapshots;

public class CharacterSnapshotService : ICharacterSnapshotService
{
    private readonly ICharacterSnapshotRepository _repository;

    public CharacterSnapshotService(ICharacterSnapshotRepository repository)
    {
        _repository = repository;
    }

    public async Task<CharacterSnapshot> CreateAsync(Guid characterId, CancellationToken ct)
    {
        return await _repository.CreateAsync(characterId, ct);
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
