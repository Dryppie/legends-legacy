using Domain.Models.Essences;

namespace Domain.Models.Essences.Definitions;

public sealed class EssenceAscensionDefinition
{
    public List<EssenceAscensionTierDefinition> Tiers { get; set; } =
    [
        new() { Tier = 0, MinLevel = 1, MaxLevel = 10 },
        new()
        {
            Tier = 1,
            MinLevel = 11,
            MaxLevel = 30,
            RequiredItemId = EssenceProgressionConstants.LesserMonsterCoreItemId,
            RequiredItemAmount = EssenceProgressionConstants.TierOneMonsterCoreCost
        },
        new()
        {
            Tier = 2,
            MinLevel = 31,
            MaxLevel = 60,
            RequiredItemId = EssenceProgressionConstants.GreaterMonsterCoreItemId,
            RequiredItemAmount = EssenceProgressionConstants.TierTwoMonsterCoreCost
        },
        new()
        {
            Tier = 3,
            MinLevel = 60,
            MaxLevel = 60,
            RequiredItemId = EssenceProgressionConstants.PrimalMonsterCoreItemId,
            RequiredItemAmount = EssenceProgressionConstants.TierThreeMonsterCoreCost
        }
    ];
}
