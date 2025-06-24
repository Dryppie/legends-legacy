namespace Domain.Models.Guilds;
public class GuildBuildingUpgrade
{
    public Guid GuildId { get; init; }
    public Guild Guild { get; init; } = null!;
    public string BuildingUpgradeDefinitionId { get; init; } = string.Empty;
    public int Level { get; set; }
}
