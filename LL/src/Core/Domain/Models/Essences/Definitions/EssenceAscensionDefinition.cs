namespace Domain.Models.Essences.Definitions;

public sealed class EssenceAscensionDefinition
{
    public List<EssenceAscensionTierDefinition> Tiers { get; set; } =
    [
        new() { Tier = 0, MinLevel = 1, MaxLevel = 10 },
        new() { Tier = 1, MinLevel = 11, MaxLevel = 20, RequiredCoreItemId = "item.monster_core.tier_1" },
        new() { Tier = 2, MinLevel = 21, MaxLevel = 30, RequiredCoreItemId = "item.monster_core.tier_2" },
        new() { Tier = 3, MinLevel = 31, MaxLevel = 40, RequiredCoreItemId = "item.monster_core.tier_3" }
    ];
}
