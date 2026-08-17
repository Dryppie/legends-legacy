using Domain.Models.Items;
using Domain.Models.Items.Equipments.Tools;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Inventories;

internal static class ToolAffixGenerator
{
    private static readonly ToolAffixDefinition[] Definitions =
    [
        new("Abundant", ToolBonusType.GatheringYieldPercent, 4, 9),
        new("Reliable", ToolBonusType.NodeSuccessChancePercent, 3, 7),
        new("Prospector's", ToolBonusType.RareMaterialChancePercent, 2, 5),
        new("Duplicating", ToolBonusType.DoubleGatherChancePercent, 1, 4),
        new("Opportunist's", ToolBonusType.BonusRollChancePercent, 1, 3),
    ];

    public static List<ToolBonusModifier> RollAffixes(Rarity rarity, IResolutionRandomSource? random = null)
    {
        var affixCount = GetAffixCount(rarity);
        if (affixCount <= 0)
        {
            return [];
        }

        return Definitions
            .OrderBy(_ => random?.NextInt(int.MaxValue) ?? Random.Shared.Next())
            .Take(affixCount)
            .Select(definition => new ToolBonusModifier
            {
                Id = random?.NextGuid() ?? Guid.NewGuid(),
                Name = definition.Name,
                BonusType = definition.BonusType,
                Amount = RollAmount(definition, rarity, random)
            })
            .ToList();
    }

    private static int GetAffixCount(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => 0,
            Rarity.Uncommon => 1,
            Rarity.Rare => 1,
            Rarity.Epic => 2,
            Rarity.Unique => 2,
            Rarity.Legendary => 3,
            Rarity.Legacy => 3,
            _ => 0
        };
    }

    private static double RollAmount(
        ToolAffixDefinition definition,
        Rarity rarity,
        IResolutionRandomSource? random)
    {
        var rarityMultiplier = rarity switch
        {
            Rarity.Common => 1.0,
            Rarity.Uncommon => 1.2,
            Rarity.Rare => 1.45,
            Rarity.Epic => 1.8,
            Rarity.Unique => 2.2,
            Rarity.Legendary => 2.7,
            Rarity.Legacy => 3.25,
            _ => 1.0
        };

        var amount = (random?.NextDouble() ?? Random.Shared.NextDouble()) *
            (definition.MaxAmount - definition.MinAmount) +
            definition.MinAmount;

        return Math.Round(amount * rarityMultiplier, 2);
    }

    private sealed record ToolAffixDefinition(
        string Name,
        ToolBonusType BonusType,
        double MinAmount,
        double MaxAmount);
}
