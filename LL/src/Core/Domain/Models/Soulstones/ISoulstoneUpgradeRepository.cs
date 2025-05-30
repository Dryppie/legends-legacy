namespace Domain.Models.Soulstones;
public interface ISoulstoneUpgradeRepository
{
    Task<List<CharacterSoulstoneUpgrade>> GetSoulstoneUpgradesByCharacterIdAsync(Guid characterId, string[] upgrades, CancellationToken cancellationToken);
}
