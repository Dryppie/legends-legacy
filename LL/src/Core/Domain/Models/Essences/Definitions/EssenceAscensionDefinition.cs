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
            RequiredItemId = EssenceProgressionConstants.LesserAscensionStoneItemId,
            RequiredItemAmount = EssenceProgressionConstants.TierOneAscensionStoneCost
        },
        new()
        {
            Tier = 2,
            MinLevel = 31,
            MaxLevel = 60,
            RequiredItemId = EssenceProgressionConstants.GreaterAscensionStoneItemId,
            RequiredItemAmount = EssenceProgressionConstants.TierTwoAscensionStoneCost
        },
        new()
        {
            Tier = 3,
            MinLevel = 60,
            MaxLevel = 60,
            RequiredItemId = EssenceProgressionConstants.PrimalAscensionStoneItemId,
            RequiredItemAmount = EssenceProgressionConstants.TierThreeAscensionStoneCost
        }
    ];
}
