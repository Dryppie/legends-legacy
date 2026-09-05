using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;

namespace EssenceSystem.Tests;

public sealed class EquipmentTests
{
    [Fact]
    public void Rarity_and_rank_scale_stats_independently_of_acquisition_source()
    {
        var budgets = new List<double>();
        foreach (var rarity in Enum.GetValues<EquipmentRarity>())
        {
            EquipmentEvaluation? expected = null;
            foreach (var kind in Enum.GetValues<EquipmentAwardKind>())
            {
                var evaluator = CreateEvaluator(rarity: rarity);
                var awarded = Award(evaluator, kind, rank: 3);
                var styled = EquipmentState.Restore(awarded.ToSnapshot() with { ActiveStyleId = "fury" });
                var actual = evaluator.Evaluate(styled);
                expected ??= actual;
                AssertStatsEqual(expected, actual);
            }
            budgets.Add(expected!.TargetBudget);
        }

        Assert.Equal(7, budgets.Count);
        Assert.True(budgets.SequenceEqual(budgets.Order()));
        Assert.Equal((int)Rarity.Rare, (int)EquipmentRarity.Rare);
        Assert.Equal(1.3f, EquipmentInstance.GetRarityBoost(Rarity.Rare));
    }

    [Fact]
    public void Style_adds_a_bonus_without_reducing_base_attributes()
    {
        var evaluator = CreateEvaluator();
        var plain = evaluator.Evaluate("plain", 1, 0, null);
        var styled = evaluator.Evaluate("plain", 1, 0, "fury");
        Assert.Equal(plain.TargetBudget * 1.15d, styled.TargetBudget, 8);
        Assert.Equal(plain.Stats[AttributeType.Power], styled.Stats[AttributeType.Power]);
        Assert.True(styled.Stats[AttributeType.CritChance] > 0);
        Assert.Equal("set.fury", styled.EquipmentSetId);
    }

    [Fact]
    public void Tier_and_rank_scale_authored_flat_stats()
    {
        var evaluator = CreateEvaluator();
        Assert.True(evaluator.Evaluate("plain", 2, 0, "fury").Stats[AttributeType.Power]
            > evaluator.Evaluate("plain", 1, 0, "fury").Stats[AttributeType.Power]);
        Assert.True(evaluator.Evaluate("plain", 1, 5, "fury").TargetBudget
            > evaluator.Evaluate("plain", 1, 0, "fury").TargetBudget);
    }

    [Fact]
    public void Progression_rarity_uses_the_original_seven_budget_multipliers()
    {
        var expected = new[] { 1d, 1.1d, 1.3d, 1.6d, 2d, 2.5d, 3d };
        var actual = Enum.GetValues<EquipmentRarity>()
            .Select(EquipmentBalance.GetRarityMultiplier)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Quality_uses_the_previous_stat_multipliers_and_combines_with_drop_variation()
    {
        var evaluator = CreateEvaluator();
        var expected = new[] { 0.90d, 1d, 1.12d, 1.26d, 1.42d };
        var actual = Enum.GetValues<ItemQuality>()
            .Select(EquipmentBalance.GetQualityMultiplier)
            .ToArray();

        Assert.Equal(expected, actual);
        var standard = evaluator.Evaluate("plain", 1, 0, null, ItemQuality.Standard, 1d);
        var crudeLow = evaluator.Evaluate("plain", 1, 0, null, ItemQuality.Crude, 0.95d);
        var masterpieceHigh = evaluator.Evaluate("plain", 1, 0, null, ItemQuality.Masterpiece, 1.05d);
        Assert.Equal(standard.BaselineBudget * 0.90d * 0.95d, crudeLow.TargetBudget, 10);
        Assert.Equal(standard.BaselineBudget * 1.42d * 1.05d, masterpieceHigh.TargetBudget, 10);
        Assert.True(masterpieceHigh.Stats[AttributeType.Power] > standard.Stats[AttributeType.Power]);
        Assert.True(standard.Stats[AttributeType.Power] > crudeLow.Stats[AttributeType.Power]);
    }

    [Fact]
    public void Combined_hand_budgets_remain_equivalent()
    {
        var balance = new EquipmentBalance(1);
        foreach (var tier in new[] { 1, 2, 10 })
        {
            var one = balance.GetBaselineBudget(tier, EquipmentType.OneHanded);
            var off = balance.GetBaselineBudget(tier, EquipmentType.OffHand);
            Assert.Equal(one * 2, balance.GetBaselineBudget(tier, EquipmentType.TwoHanded));
            Assert.Equal(one + off, balance.GetBaselineBudget(tier, EquipmentType.TwoHanded));
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void Invalid_ranks_are_rejected(int rank) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateEvaluator().Evaluate("plain", 1, rank, null));

    [Fact]
    public void Missing_or_incompatible_definitions_are_rejected()
    {
        var evaluator = CreateEvaluator();
        Assert.Throws<ArgumentException>(() => evaluator.Evaluate("missing", 1, 0, null));
        Assert.Throws<ArgumentException>(() => evaluator.Evaluate("plain", 1, 0, "missing"));
        Assert.Throws<ArgumentOutOfRangeException>(() => evaluator.Evaluate("plain", 0, 0, null));
    }

    [Fact]
    public void Awarded_state_preserves_authored_rank_style_and_ownership()
    {
        var evaluator = CreateEvaluator(nativeStyle: "fury");
        var owner = Guid.NewGuid();
        var item = EquipmentState.Award(Guid.NewGuid(), evaluator, "plain", 2, 4,
            new(EquipmentAwardKind.RandomDiscovery, "test", "receipt"),
            new(EquipmentOwnershipKind.UnboundPersonal, owner));

        Assert.Equal(4, item.Rank);
        Assert.Equal("fury", item.NativeStyleId);
        Assert.Equal("fury", item.ActiveStyleId);
        Assert.True(item.Ownership.CanTradeOrDonate);
        AssertStatsEqual(evaluator.Evaluate("plain", 2, 4, "fury"), evaluator.Evaluate(item));
    }

    [Fact]
    public void Binding_and_guild_transfer_preserve_equipment_identity()
    {
        var evaluator = CreateEvaluator(nativeStyle: "fury");
        var item = Award(evaluator, rank: 2);
        var bound = item.BindForPersonalUse();
        Assert.Equal(item.Id, bound.Id);
        Assert.False(bound.Ownership.CanTradeOrDonate);

        var guildId = Guid.NewGuid();
        var donated = item.DonateToGuild(guildId);
        Assert.Equal(guildId, donated.Ownership.OwnerId);
        Assert.Equal(item.Rank, donated.Rank);
        Assert.Equal(item.ActiveStyleId, donated.ActiveStyleId);
        Assert.Throws<InvalidOperationException>(() => bound.DonateToGuild(guildId));
    }

    [Fact]
    public void Balance_version_mismatch_is_rejected()
    {
        var item = Award(CreateEvaluator());
        Assert.Throws<InvalidOperationException>(() => CreateEvaluator(new EquipmentBalance(2)).Evaluate(item));
    }

    [Fact]
    public void Reinforcement_increases_rank_without_rerolling_identity()
    {
        var evaluator = CreateEvaluator(nativeStyle: "fury");
        var item = EquipmentState.Award(Guid.NewGuid(), evaluator, "plain", 1, 2,
            new(EquipmentAwardKind.RandomDiscovery, "test.source", "quality-roll"),
            new(EquipmentOwnershipKind.UnboundPersonal, Guid.NewGuid()),
            ItemQuality.Exceptional, 0.973d);

        var reinforced = item.Reinforce(evaluator);

        Assert.Equal(3, reinforced.Rank);
        Assert.Equal(item.Id, reinforced.Id);
        Assert.Equal(item.DefinitionId, reinforced.DefinitionId);
        Assert.Equal(item.Tier, reinforced.Tier);
        Assert.Equal(
            evaluator.Evaluate(item).Definition.Rarity,
            evaluator.Evaluate(reinforced).Definition.Rarity);
        Assert.Equal(item.ActiveStyleId, reinforced.ActiveStyleId);
        Assert.Equal(item.Quality, reinforced.Quality);
        Assert.Equal(item.AttributeRollMultiplier, reinforced.AttributeRollMultiplier);
        Assert.Equal(EquipmentOwnershipKind.BoundPersonal, reinforced.Ownership.Kind);
        Assert.True(evaluator.Evaluate(reinforced).TargetBudget > evaluator.Evaluate(item).TargetBudget);
    }

    [Fact]
    public void Version_one_state_loads_as_standard_with_a_neutral_attribute_roll()
    {
        var item = Award(CreateEvaluator());

        var restored = EquipmentState.Restore(item.ToSnapshot() with
        {
            ModelVersion = 1,
            Quality = ItemQuality.Masterpiece,
            AttributeRollMultiplier = 0.95d
        });

        Assert.Equal(ItemQuality.Standard, restored.Quality);
        Assert.Equal(1d, restored.AttributeRollMultiplier);
        Assert.Equal(EquipmentBalance.ModelVersion, restored.ToSnapshot().ModelVersion);
    }

    [Fact]
    public void Dismantle_value_includes_every_current_rank_even_when_rank_was_awarded()
    {
        var prices = new EquipmentUpgradePrices(
            1,
            EquipmentKeys.ReinforcementPartsItemBaseId,
            [new EquipmentUpgradeTierPrices(1, [5, 10, 20, 40, 80], [250, 500, 1000, 2000, 4000], 1)],
            0.5m);

        Assert.Equal(1, prices.GetDismantleParts(1, 0));
        Assert.Equal(3, prices.GetDismantleParts(1, 1));
        Assert.Equal(8, prices.GetDismantleParts(1, 2));
        Assert.Equal(78, prices.GetDismantleParts(1, 5));
    }

    [Fact]
    public void Older_frozen_equipment_can_be_reinforced_without_rerolling_its_stats()
    {
        var evaluator = CreateEvaluator();
        var state = Award(evaluator, rank: 1);
        var current = EquipmentData.Create(state, evaluator);
        var staleStats = current.Stats.ToDictionary(x => x.Key, x => x.Value);
        staleStats[AttributeType.Power] += 1;
        var stale = new EquipmentData(
            current.State,
            current.ItemBaseId,
            current.DisplayName,
            current.Rarity,
            current.EquipmentType,
            current.Behavior,
            staleStats,
            current.EquipmentSetId);
        var equipment = new EquipmentInstance
        {
            Id = state.Id,
            ItemBaseId = current.ItemBaseId
        };
        equipment.ApplyProgressionData(stale);
        var context = new EquipmentUpgradeContext(
            new Character { Id = state.Ownership.OwnerId, Cinders = 10_000 },
            new InventoryItem { Quantity = 1 },
            equipment,
            false,
            null,
            [new InventoryItem { Quantity = 1_000 }]);
        var prices = new EquipmentUpgradePrices(
            1,
            EquipmentKeys.ReinforcementPartsItemBaseId,
            [new EquipmentUpgradeTierPrices(1, [5, 10, 20, 40, 80], [250, 500, 1000, 2000, 4000], 1)],
            0.5m);

        var quote = new EquipmentUpgradePolicy(
                new EquipmentCatalog(evaluator),
                prices)
            .Quote(
                context,
                new EquipmentUpgradeRequest(EquipmentUpgradeOperationKind.Reinforce, state.Id),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow);

        Assert.True(quote.CanExecute, quote.UnavailableReason);
        Assert.NotNull(quote.Before);
        Assert.NotNull(quote.After);
        Assert.Equal(1, quote.Before.State.Rank);
        Assert.Equal(2, quote.After.State.Rank);
        Assert.True(
            quote.After.Stats[AttributeType.Power]
            > quote.Before.Stats[AttributeType.Power]);
    }

    private static EquipmentEvaluator CreateEvaluator(
        EquipmentBalance? balance = null,
        EquipmentRarity rarity = EquipmentRarity.Common,
        string? nativeStyle = null) =>
        new(balance ?? new EquipmentBalance(1),
            [new EquipmentArchetype("sword", "shortsword", EquipmentType.OneHanded,
                new EquipmentBehaviorDefinition { Handedness = "OneHanded", AttackCategory = "Physical", RangeCategory = "Melee" },
                Weights(AttributeType.Power))],
            [new EquipmentStyle("fury", ["sword"], Weights(AttributeType.CritChance), "set.fury")],
            [new EquipmentDefinition("plain", "Sword", "sword", rarity, nativeStyle)]);

    private static EquipmentState Award(
        EquipmentEvaluator evaluator,
        EquipmentAwardKind kind = EquipmentAwardKind.RandomDiscovery,
        int rank = 0) =>
        EquipmentState.Award(Guid.NewGuid(), evaluator, "plain", 1, rank,
            new EquipmentProvenance(kind, "test.source", Guid.NewGuid().ToString()),
            new EquipmentOwnership(EquipmentOwnershipKind.UnboundPersonal, Guid.NewGuid()));

    private static Dictionary<AttributeType, double> Weights(AttributeType attribute) => new() { [attribute] = 1d };

    private static void AssertStatsEqual(EquipmentEvaluation expected, EquipmentEvaluation actual) =>
        Assert.Equal(expected.Stats.OrderBy(x => x.Key), actual.Stats.OrderBy(x => x.Key));
}
