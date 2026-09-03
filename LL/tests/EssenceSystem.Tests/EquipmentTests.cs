using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Professions.Crafting.V2;

namespace EssenceSystem.Tests;

public sealed class EquipmentTests
{
    [Fact]
    public void Acquisition_and_rarity_do_not_change_mechanical_stats()
    {
        var expected = CreateEvaluator().Evaluate("plain", 1, 3, "fury");
        foreach (var rarity in Enum.GetValues<EquipmentRarity>())
        foreach (var kind in Enum.GetValues<EquipmentAwardKind>())
        {
            var evaluator = CreateEvaluator(rarity: rarity);
            var awarded = Award(evaluator, kind, rank: 3);
            var styled = EquipmentState.Restore(awarded.ToSnapshot() with { ActiveStyleId = "fury" });
            AssertStatsEqual(expected, evaluator.Evaluate(styled));
        }

        Assert.Equal((int)Rarity.Rare, (int)EquipmentRarity.Rare);
        Assert.Equal(1.3f, EquipmentInstance.GetRarityBoost(Rarity.Rare));
    }

    [Fact]
    public void Style_reallocates_budget_instead_of_adding_a_bonus()
    {
        var evaluator = CreateEvaluator();
        var plain = evaluator.Evaluate("plain", 1, 0, null);
        var styled = evaluator.Evaluate("plain", 1, 0, "fury");
        Assert.Equal(plain.TargetBudget, styled.TargetBudget);
        Assert.True(styled.Stats[AttributeType.Power] < plain.Stats[AttributeType.Power]);
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
