using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class CraftingEquipmentScalingTests
{
    [Fact]
    public void Hybrid_rolls_keep_direct_percentages_stable_while_tier_anchors_grow()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase
        {
            Id = "hybrid-sword",
            Name = "Hybrid Sword",
            EquipmentType = EquipmentType.OneHanded
        };
        var recipe = new CraftingRecipeDefinition
        {
            Id = "recipe.hybrid-sword",
            Name = "Hybrid Sword",
            OutputItemId = equipment.Id,
            OutputItemType = equipment.EquipmentType,
            InitialStatProfile = new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.7d,
                [AttributeType.CritChance] = 0.3d
            }
        };
        var design = EquipmentCraftingDesignComposer.Compose(recipe, null);

        var tierOne = service.RollBaseStats(
            equipment, design, 1, ItemQuality.Standard, new FixedRandom(0.5d));
        var tierTen = service.RollBaseStats(
            equipment, design, 10, ItemQuality.Standard, new FixedRandom(0.5d));

        Assert.True(
            tierTen.Single(x => x.AttributeType == AttributeType.Power).Amount
            > tierOne.Single(x => x.AttributeType == AttributeType.Power).Amount);
        Assert.Equal(
            tierOne.Single(x => x.AttributeType == AttributeType.CritChance).Amount,
            tierTen.Single(x => x.AttributeType == AttributeType.CritChance).Amount);
    }

    [Fact]
    public void RollBaseStats_HigherTierBudgetCreatesStrongerEquipmentBase()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase { Id = "iron_helm", Name = "Iron Helm", EquipmentType = EquipmentType.Head };
        var recipe = CreateSingleStatDesign();

        var tierOne = service.RollBaseStats(equipment, recipe, 1, ItemQuality.Standard, new FixedRandom(0.5d)).Single();
        var tierTwo = service.RollBaseStats(equipment, recipe, 2, ItemQuality.Standard, new FixedRandom(0.5d)).Single();

        Assert.True(tierTwo.Amount > tierOne.Amount);
    }

    [Fact]
    public void RollBaseStats_QualityMultiplierIncreasesCraftedStatBudget()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase { Id = "iron_helm", Name = "Iron Helm", EquipmentType = EquipmentType.Head };
        var recipe = CreateSingleStatDesign();

        var standard = service.RollBaseStats(equipment, recipe, 10, ItemQuality.Standard, new FixedRandom(0.5d)).Single();
        var fine = service.RollBaseStats(equipment, recipe, 10, ItemQuality.Fine, new FixedRandom(0.5d)).Single();

        Assert.True(fine.Amount > standard.Amount);
    }

    [Fact]
    public void RollBaseStats_AdjacentQualityBandsDoNotOverlap()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase
        {
            Id = "quality-band-ring",
            Name = "Quality Band Ring",
            EquipmentType = EquipmentType.Ring
        };
        var design = CreateSingleStatDesign();
        var qualities = Enum.GetValues<ItemQuality>().OrderBy(quality => quality).ToArray();

        for (var index = 0; index < qualities.Length - 1; index++)
        {
            var lowerQuality = qualities[index];
            var higherQuality = qualities[index + 1];
            var lowerMaximum = service.RollBaseStats(
                    equipment,
                    design,
                    10,
                    lowerQuality,
                    new FixedRandom(1d))
                .Single();
            var higherMinimum = service.RollBaseStats(
                    equipment,
                    design,
                    10,
                    higherQuality,
                    new FixedRandom(0d))
                .Single();

            Assert.True(
                higherMinimum.Amount > lowerMaximum.Amount,
                $"{higherQuality} minimum {higherMinimum.Amount} must exceed "
                + $"{lowerQuality} maximum {lowerMaximum.Amount}.");
        }
    }

    [Fact]
    public void GetBaseStatRanges_EnclosesEveryPossibleCurrentMasteryRoll()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase { Id = "iron_helm", Name = "Iron Helm", EquipmentType = EquipmentType.Head };
        var recipe = CreateSingleStatDesign();
        var possibleQualities = new[]
        {
            ItemQuality.Crude,
            ItemQuality.Standard,
            ItemQuality.Fine,
            ItemQuality.Exceptional
        };

        var preview = service.GetBaseStatRanges(equipment, recipe, 1, possibleQualities).Single();
        var minimumRoll = service.RollBaseStats(
            equipment,
            recipe,
            1,
            ItemQuality.Crude,
            new FixedRandom(0d)).Single();
        var maximumRoll = service.RollBaseStats(
            equipment,
            recipe,
            1,
            ItemQuality.Exceptional,
            new FixedRandom(1d)).Single();

        Assert.Equal(minimumRoll.Amount, preview.MinimumAmount);
        Assert.Equal(maximumRoll.Amount, preview.MaximumAmount);
    }

    [Fact]
    public void HeavyHelmRoll_CannotFallBelowItsAdvertisedCrowdControlResistanceRange()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase
        {
            Id = "heavy_helm",
            Name = "Heavy Helm",
            EquipmentType = EquipmentType.Head
        };
        var recipe = new CraftingRecipeDefinition
        {
            Id = "recipe.armor.head.heavy_helm",
            Name = "Heavy Helm",
            OutputItemId = equipment.Id,
            OutputItemType = equipment.EquipmentType,
            InitialStatProfile = new Dictionary<AttributeType, double>
            {
                [AttributeType.Armor] = 0.35d,
                [AttributeType.MaxHealth] = 0.30d,
                [AttributeType.BlockChance] = 0.20d,
                [AttributeType.CrowdControlResistance] = 0.15d
            }
        };
        var design = EquipmentCraftingDesignComposer.Compose(recipe, null);
        var qualities = new[]
        {
            ItemQuality.Crude,
            ItemQuality.Standard,
            ItemQuality.Fine,
            ItemQuality.Exceptional
        };
        var advertised = service.GetBaseStatRanges(
                equipment,
                design,
                1,
                qualities)
            .Single(range =>
                range.AttributeType == AttributeType.CrowdControlResistance);

        var lowestStandardRoll = service.RollBaseStats(
                equipment,
                design,
                1,
                ItemQuality.Standard,
                new FixedRandom(0d))
            .Single(modifier =>
                modifier.AttributeType == AttributeType.CrowdControlResistance);

        Assert.InRange(
            lowestStandardRoll.Amount,
            advertised.MinimumAmount,
            advertised.MaximumAmount);
        Assert.True(
            lowestStandardRoll.Amount > 1f,
            $"Heavy Helm rolled {lowestStandardRoll.Amount} Crowd Control Resistance " +
            $"despite advertising {advertised.MinimumAmount}-{advertised.MaximumAmount}.");
        Assert.Equal(
            AttributeValueQuantizer.Quantize(
                AttributeType.CrowdControlResistance,
                lowestStandardRoll.Amount),
            lowestStandardRoll.Amount);
        Assert.Equal(
            AttributeUnit.PercentagePoints,
            AttributeCatalog.Get(AttributeType.CrowdControlResistance).Unit);
        Assert.Equal(
            "%",
            AttributeCatalog.Get(AttributeType.CrowdControlResistance).DisplaySuffix);
    }

    [Fact]
    public void RollBaseStats_DoesNotCountAuthoredBaseModifiersAgainstRecipeBudget()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var plain = new EquipmentBase
        {
            Id = "plain-helm",
            Name = "Plain Helm",
            EquipmentType = EquipmentType.Head
        };
        var authored = new EquipmentBase
        {
            Id = "authored-helm",
            Name = "Authored Helm",
            EquipmentType = EquipmentType.Head,
            AttributeModifiers =
            [
                new Domain.Models.Attributes.Modifiers.ItemAttributeModifier(
                    AttributeType.Power,
                    500,
                    Domain.Models.Attributes.Modifiers.ModifierType.Flat)
            ]
        };
        var design = CreateSingleStatDesign();

        var plainRoll = service.RollBaseStats(
            plain,
            design,
            1,
            ItemQuality.Standard,
            new FixedRandom(0.5d));
        var authoredRoll = service.RollBaseStats(
            authored,
            design,
            1,
            ItemQuality.Standard,
            new FixedRandom(0.5d));

        Assert.Equal(
            plainRoll.Select(x => (x.AttributeType, x.Amount)),
            authoredRoll.Select(x => (x.AttributeType, x.Amount)));
    }

    [Fact]
    public void Blueprint_roll_preserves_base_recipe_stats_and_adds_a_separate_bonus()
    {
        var service = new ItemStatRollService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase
        {
            Id = "cloth_cowl",
            Name = "Cloth Cowl",
            EquipmentType = EquipmentType.Head
        };
        var recipe = new CraftingRecipeDefinition
        {
            Id = "recipe.armor.head.cloth",
            Name = "Cloth Cowl",
            OutputItemId = equipment.Id,
            OutputItemType = equipment.EquipmentType,
            InitialStatProfile = new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.3d,
                [AttributeType.HealingPowerPercent] = 0.3d,
                [AttributeType.MaxHealth] = 0.25d,
                [AttributeType.Resistance] = 0.15d
            }
        };
        var blueprint = new BlueprintDefinition
        {
            Id = "blueprint_aegis",
            Name = "Blueprint: Aegis",
            BonusStatBudgetMultiplier = 0.2d,
            BonusStatProfile = new Dictionary<AttributeType, double>
            {
                [AttributeType.Armor] = 0.6d,
                [AttributeType.DamageReduction] = 0.4d
            }
        };
        var baseDesign = EquipmentCraftingDesignComposer.Compose(recipe, null);
        var blueprintDesign = EquipmentCraftingDesignComposer.Compose(recipe, blueprint);

        var baseStats = service.RollBaseStats(
                equipment,
                baseDesign,
                1,
                ItemQuality.Standard,
                new FixedRandom(0.5d))
            .ToDictionary(modifier => modifier.AttributeType, modifier => modifier.Amount);
        var blueprintStats = service.RollBaseStats(
                equipment,
                blueprintDesign,
                1,
                ItemQuality.Standard,
                new FixedRandom(0.5d))
            .ToDictionary(modifier => modifier.AttributeType, modifier => modifier.Amount);

        Assert.All(baseStats, stat => Assert.Equal(stat.Value, blueprintStats[stat.Key]));
        Assert.True(blueprintStats[AttributeType.Armor] > 0);
        Assert.True(blueprintStats[AttributeType.DamageReduction] > 0);
    }

    [Fact]
    public void GetQualityChances_ReportsTheActualMasteryZeroDistribution()
    {
        var service = new ItemQualityRollService();

        var chances = service.GetQualityChances(0);

        Assert.Equal(100d, chances.Values.Sum());
        Assert.Equal(25d, chances[ItemQuality.Crude]);
        Assert.Equal(60d, chances[ItemQuality.Standard]);
        Assert.Equal(14d, chances[ItemQuality.Fine]);
        Assert.Equal(1d, chances[ItemQuality.Exceptional]);
        Assert.Equal(0d, chances[ItemQuality.Masterwork]);
    }

    [Fact]
    public void CalculateStartingPotential_DefaultBalanceDefinesExpectedTemperingActionBudget()
    {
        var potentialService = new ItemPotentialService(Options.Create(new CraftingBalanceOptions()));
        var equipment = new EquipmentBase { Id = "iron_helm", Name = "Iron Helm", EquipmentType = EquipmentType.Head };
        var instance = new EquipmentInstance
        {
            ItemBaseId = equipment.Id,
            ItemBase = equipment,
            Tier = 1,
            Quality = ItemQuality.Standard,
            Potential = potentialService.CalculateStartingPotential(
                equipment,
                targetTier: 1,
                quality: ItemQuality.Standard,
                masteryLevel: 0,
                craftingLevel: 0)
        };
        Assert.Equal(200, instance.Potential);
    }

    [Fact]
    public void CalculateStartingPotential_UsesConfiguredPotentialMultipliers()
    {
        var service = new ItemPotentialService(Options.Create(new CraftingBalanceOptions
        {
            PotentialSlotWeights = new Dictionary<EquipmentType, double>
            {
                [EquipmentType.Head] = 1d
            },
            PotentialQualityMultipliers = new Dictionary<ItemQuality, double>
            {
                [ItemQuality.Standard] = 2d
            }
        }));
        var equipment = new EquipmentBase { Id = "iron_helm", Name = "Iron Helm", EquipmentType = EquipmentType.Head };

        var potential = service.CalculateStartingPotential(
            equipment,
            targetTier: 1,
            quality: ItemQuality.Standard,
            masteryLevel: 0,
            craftingLevel: 0);

        Assert.Equal(400, potential);
    }

    private static EquipmentCraftingDesign CreateSingleStatDesign()
    {
        var recipe = new CraftingRecipeDefinition
        {
            Id = "recipe.armor.head.test",
            Name = "Head Armor",
            OutputItemId = "iron_helm",
            OutputItemType = EquipmentType.Head,
            InitialStatProfile = new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 1d
            }
        };
        return EquipmentCraftingDesignComposer.Compose(recipe, null);
    }

    private sealed class FixedRandom(double nextDouble) : Random
    {
        public override double NextDouble() => nextDouble;
    }
}
