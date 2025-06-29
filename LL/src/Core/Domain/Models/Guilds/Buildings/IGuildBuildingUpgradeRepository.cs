namespace Domain.Models.Guilds.Buildings;
public interface IGuildBuildingUpgradeRepository
{
    Task<List<GuildBuildingUpgrade>> GetGuildBuildingUpgradesByCharacterIdAsync(Guid characterId, string[] upgrades, CancellationToken cancellationToken);
}
