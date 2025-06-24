namespace Domain.Models.Guilds.Buildings;
public record BuildingUpgradeView(BuildingUpgradeDefinition Definition, int Level, Dictionary<GuildResourceType, int>? NextCost);

