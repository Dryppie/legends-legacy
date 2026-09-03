using Domain.Models.Entities.Characters;

namespace Domain.Models.Soulstones;
public interface ISoulstoneUpgradeRepository
{
    Task<Character?> GetCharacterAsync(Guid characterId, CancellationToken cancellationToken);
    void Remove(Character character, IReadOnlyCollection<CharacterSoulstoneUpgrade> upgrades);
    Task<List<CharacterSoulstoneUpgrade>> GetSoulstoneUpgradesByCharacterIdAsync(Guid characterId, string[] upgrades, CancellationToken cancellationToken);
}
