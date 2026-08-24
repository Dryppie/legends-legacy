using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;

namespace EssenceSystem.Tests;

public sealed class EquipmentRarityScalingTests
{
    [Fact]
    public void RarityBoost_UsesTheAuthoredProgressionFromCommonToLegacy()
    {
        var rarities = Enum.GetValues<Rarity>().OrderBy(rarity => rarity).ToArray();
        var expectedBoosts = new[] { 1f, 1.1f, 1.3f, 1.6f, 2f, 2.5f, 3f };

        for (var index = 0; index < rarities.Length; index++)
        {
            Assert.Equal(
                expectedBoosts[index],
                EquipmentInstance.GetRarityBoost(rarities[index]),
                precision: 5);
        }

        Assert.Equal(1f, EquipmentInstance.GetRarityBoost(Rarity.Common));
        Assert.Equal(3f, EquipmentInstance.GetRarityBoost(Rarity.Legacy));

        var boosts = rarities
            .Select(EquipmentInstance.GetRarityBoost)
            .ToArray();
        var increments = boosts
            .Skip(1)
            .Select((boost, index) => boost - boosts[index])
            .ToArray();
        for (var index = 1; index < increments.Length; index++)
        {
            Assert.True(
                increments[index] >= increments[index - 1],
                $"Rarity step {index + 1} must not be smaller than step {index}.");
        }
    }

    [Fact]
    public void AuthoredBaseModifiers_ReceiveTheLinearRarityBoost()
    {
        var equipmentBase = new EquipmentBase
        {
            Id = "rarity-scaling-test",
            Name = "Rarity Scaling Test",
            EquipmentType = EquipmentType.Chest,
            AttributeModifiers =
            [
                new ItemAttributeModifier(
                    AttributeType.Power,
                    9,
                    ModifierType.Flat)
            ]
        };
        var expectedAmounts = new[] { 9, 10, 12, 15, 18, 23, 27 };

        foreach (var rarity in Enum.GetValues<Rarity>())
        {
            var equipment = new EquipmentInstance
            {
                ItemBaseId = equipmentBase.Id,
                ItemBase = equipmentBase,
                Rarity = rarity
            };

            Assert.Equal(expectedAmounts[(int)rarity], equipment.BaseModifiers.Single().Amount);
        }
    }
}
