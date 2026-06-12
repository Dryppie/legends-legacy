using Domain.Models.Entities.Characters;

namespace Domain.Models.Essences;

public interface IEssenceRepository
{
    Task<List<EssenceLoadoutSlot>> GetActiveSlotsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<Character?> GetCharacterWithEssenceLoadoutsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<EssenceLoadout?> GetActiveLoadoutAsync(Guid characterId, CancellationToken cancellationToken);
    Task<int> GetCharacterLevelAsync(Guid characterId, CancellationToken cancellationToken);

    Task<List<PlayerEssence>> GetPlayerEssencesAsync(Guid characterId, CancellationToken cancellationToken);
    Task<PlayerEssence?> GetPlayerEssenceAsync(Guid characterId, Guid playerEssenceId, CancellationToken cancellationToken);
    Task<bool> HasPlayerEssenceAsync(Guid characterId, string essenceDefinitionId, CancellationToken cancellationToken);
    Task<int> CountOwnedPlayerEssencesAsync(Guid characterId, IReadOnlyCollection<Guid> playerEssenceIds, CancellationToken cancellationToken);
    Task AddPlayerEssenceAsync(PlayerEssence essence, CancellationToken cancellationToken);
    Task<CreatureResonance?> GetMonsterResonanceAsync(Guid characterId, string creatureId, CancellationToken cancellationToken);
    Task AddMonsterResonanceAsync(CreatureResonance resonance, CancellationToken cancellationToken);

    Task<EssenceLoadout?> GetLoadoutWithSlotsAsync(Guid characterId, Guid loadoutId, CancellationToken cancellationToken);
    Task<List<EssenceLoadout>> GetLoadoutsWithSlotsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<int> CountLoadoutsAsync(Guid characterId, CancellationToken cancellationToken);
    Task AddLoadoutAsync(EssenceLoadout loadout, CancellationToken cancellationToken);
    Task<EssenceLoadout?> GetLoadoutAsync(Guid characterId, Guid loadoutId, CancellationToken cancellationToken);
    void RemoveLoadout(EssenceLoadout loadout);
}
