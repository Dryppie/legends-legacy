using Application.Interfaces.Services.LL.PowerRatings;
using Application.Interfaces.Services.LL.Essences;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Combat.Engine;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.PowerRatings;

namespace EssenceSystem.Tests;

public sealed class PowerRatingCoreTests
{
    [Fact]
    public void AttributeCombatRating_UsesReferenceWeightsWithoutPrimaryDoubleCounting()
    {
        var baseAttributes = new[]
        {
            new EntityAttribute { AttributeType = AttributeType.Power, Value = 10 },
            new EntityAttribute { AttributeType = AttributeType.MaxHealth, Value = 100 }
        };

        var direct = CombatRatingCalculator.ProjectDirectAttributes(baseAttributes, []);
        var rating = CombatRatingCalculator.CalculateCanonical(
            direct,
            new Dictionary<AttributeType, double>(),
            1);

        Assert.Equal(244, rating.Overall);
    }

    [Fact]
    public void AttributeCombatRating_EquipmentIncreaseIsImmediateAndNotQuantizedToTens()
    {
        var baseline = CombatRatingCalculator.CalculateCanonical(
            new Dictionary<AttributeType, float> { [AttributeType.Power] = 10 },
            new Dictionary<AttributeType, double>(),
            1);
        var upgraded = CombatRatingCalculator.CalculateCanonical(
            new Dictionary<AttributeType, float> { [AttributeType.Power] = 13 },
            new Dictionary<AttributeType, double>(),
            1);

        Assert.Equal(68, upgraded.Overall - baseline.Overall);
    }

    [Fact]
    public void AttributeCombatRating_UsesReferenceWeightsAndDeduplicatesTwoHandedInstances()
    {
        var item = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            Tier = 5,
            InstanceModifiers =
            [
                new InstanceAttributeModifier(AttributeType.Armor, 10)
            ]
        };

        var once = CombatRatingCalculator.Calculate([], [item]);
        var duplicatedSlotReference = CombatRatingCalculator.Calculate([], [item, item]);

        Assert.Equal(9, once.Overall);
        Assert.Equal(once.Overall, duplicatedSlotReference.Overall);
    }

    [Fact]
    public void AttributeCombatRating_ValuesAuthoredBaseModifiersAtAStableReferenceTier()
    {
        var itemBase = new EquipmentBase
        {
            Id = "stable-base",
            Name = "Stable Base",
            AttributeModifiers =
            [
                new ItemAttributeModifier(AttributeType.Power, 10)
            ]
        };
        var tierOne = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            ItemBase = itemBase,
            Tier = 1
        };
        var tierTen = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            ItemBase = itemBase,
            Tier = 10
        };

        Assert.Equal(
            CombatRatingCalculator.Calculate([], [tierOne]).Overall,
            CombatRatingCalculator.Calculate([], [tierTen]).Overall);
    }

    [Fact]
    public void AttributeCombatRating_ValuesIdenticalFinalStatsEquallyAcrossSourcesAndTiers()
    {
        var baseModifierItem = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            ItemBase = new EquipmentBase
            {
                Id = "base-power",
                Name = "Base Power",
                AttributeModifiers =
                [
                    new ItemAttributeModifier(AttributeType.Power, 10)
                ]
            },
            Tier = 10
        };
        var generatedModifierItem = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            Tier = 10,
            InstanceModifiers =
            [
                new InstanceAttributeModifier(AttributeType.Power, 10)
            ]
        };
        var additionalModifierRating = CombatRatingCalculator.Calculate(
            [],
            [],
            [
                new CombatRatingModifierSource(
                    5,
                    [new AbilityAttributeModifier(AttributeType.Power, 10)])
            ]);

        Assert.Equal(225, CombatRatingCalculator.Calculate([], [baseModifierItem]).Overall);
        Assert.Equal(225, CombatRatingCalculator.Calculate([], [generatedModifierItem]).Overall);
        Assert.Equal(225, additionalModifierRating.Overall);
    }

    [Fact]
    public void AttributeCombatRating_AppliesModifierSemanticsBeforeValuation()
    {
        var rating = CombatRatingCalculator.Calculate(
            [new EntityAttribute { AttributeType = AttributeType.Power, Value = 10 }],
            [],
            [
                new CombatRatingModifierSource(
                    10,
                    [
                        new AbilityAttributeModifier(
                            AttributeType.Power,
                            50,
                            ModifierType.Additive)
                    ])
            ]);

        Assert.Equal(338, rating.Overall);
    }

    [Fact]
    public void CanonicalCombatRating_DoesNotChangeWithEquipmentSourceTier()
    {
        var tierOne = CombatRatingCalculator.CalculateCanonical(
            new Dictionary<AttributeType, float>(),
            new Dictionary<AttributeType, double> { [AttributeType.Power] = 10 },
            1);
        var tierTen = CombatRatingCalculator.CalculateCanonical(
            new Dictionary<AttributeType, float>(),
            new Dictionary<AttributeType, double> { [AttributeType.Power] = 10 },
            10);

        Assert.Equal(225, tierOne.Overall);
        Assert.Equal(tierOne.Overall, tierTen.Overall);
    }

    [Fact]
    public void AttributeCombatRating_AddsExplicitAttributesAtReferenceWeights()
    {
        var withoutTemporarySource = CombatRatingCalculator.Calculate([], []);
        var withTemporarySource = CombatRatingCalculator.Calculate(
            [],
            [],
            [
                new CombatRatingModifierSource(
                    5,
                    [new AbilityAttributeModifier(AttributeType.Armor, 10)])
            ]);

        Assert.Equal(9, withTemporarySource.Overall - withoutTemporarySource.Overall);
        Assert.Equal(9, withTemporarySource.PhysicalDurability);
        Assert.Equal(0, withTemporarySource.MagicalDurability);
        Assert.Equal(0, withTemporarySource.ControlUtility);
    }

    [Fact]
    public void AttributeCombatRating_DoesNotValueTemporaryAttributesBeyondCombinedCap()
    {
        var baseAttributes = new[]
        {
            new EntityAttribute { AttributeType = AttributeType.DodgeChance, Value = 45 }
        };
        var rating = CombatRatingCalculator.Calculate(
            baseAttributes,
            [],
            [
                new CombatRatingModifierSource(
                    1,
                    [new AbilityAttributeModifier(AttributeType.DodgeChance, 10)])
            ]);

        Assert.Equal(1_200, rating.Overall);
    }

    [Fact]
    public void AttributeCombatRating_ClampsFixedCapOverflow()
    {
        var capped = CombatRatingCalculator.CalculateCanonical(
            new Dictionary<AttributeType, float> { [AttributeType.DodgeChance] = 50 },
            new Dictionary<AttributeType, double>(),
            1);
        var overflow = CombatRatingCalculator.CalculateCanonical(
            new Dictionary<AttributeType, float> { [AttributeType.DodgeChance] = 500 },
            new Dictionary<AttributeType, double>(),
            1);

        Assert.Equal(capped.Overall, overflow.Overall);
    }

    [Fact]
    public void Build_fingerprint_tracks_combat_inputs_but_ignores_currency()
    {
        var character = CreateCharacter();
        var original = PowerBuildSnapshotFactory.CreateFingerprint(character);

        character.Cinders += 10_000;
        character.Soulstones += 500;
        var afterCurrency = PowerBuildSnapshotFactory.CreateFingerprint(character);

        character.BaseAttributes.Single(x => x.AttributeType == AttributeType.Power).Value += 1;
        var afterPower = PowerBuildSnapshotFactory.CreateFingerprint(character);

        Assert.Equal(original, afterCurrency);
        Assert.NotEqual(original, afterPower);
    }

    [Fact]
    public void Build_fingerprint_is_deterministic_when_attribute_order_changes()
    {
        var first = CreateCharacter();
        var second = CreateCharacter();
        second.BaseAttributes = second.BaseAttributes.Reverse().ToList();

        Assert.Equal(
            PowerBuildSnapshotFactory.CreateFingerprint(first),
            PowerBuildSnapshotFactory.CreateFingerprint(second));
    }

    private static Character CreateCharacter() => new()
    {
        Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
        Name = "Power Fixture",
        Level = 12,
        BaseAttributes =
        [
            new EntityAttribute { AttributeType = AttributeType.Power, Value = 25 },
            new EntityAttribute { AttributeType = AttributeType.MaxHealth, Value = 250 },
            new EntityAttribute { AttributeType = AttributeType.Resistance, Value = 15 }
        ]
    };

}
