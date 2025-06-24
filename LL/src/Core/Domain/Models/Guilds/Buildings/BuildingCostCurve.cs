namespace Domain.Models.Guilds.Buildings;
public record BuildingCostCurve
(
    GuildResourceType Resource,     // e.g., "cinders", "soulstones", "temperedScrap", "soulDust", "wood", "ore"
    int Base,
    int Increment,
    int? IncrementCap = null
);
