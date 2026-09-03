using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Professions.Crafting.V2;

namespace EssenceSystem.Tests;

public sealed class EquipmentTests
{
    [Fact]
    public void AcquisitionAndRarityDoNotChangeMechanicalStats()
    {
        var evaluator = CreateEvaluator();
        var expected = evaluator.Evaluate("plain", 1, 3, "fury");
        foreach (var rarity in Enum.GetValues<EquipmentRarity>())
        {
            var variant = CreateEvaluator(rarity: rarity);
            foreach (var kind in Enum.GetValues<EquipmentAwardKind>())
            {
                var awarded = Award(variant, kind: kind, rank: 3).ChangeStyle(variant, "fury", Learned("fury"));
                AssertStatsEqual(expected, variant.Evaluate(awarded));
                Assert.Empty(awarded.Investments);
                Assert.Equal(0, awarded.PaidScrap);
            }
        }
        Assert.Equal((int)Rarity.Rare, (int)EquipmentRarity.Rare);
        Assert.Equal((int)Rarity.Legendary, (int)EquipmentRarity.Legendary);
        Assert.Equal(1.3f, EquipmentInstance.GetRarityBoost(Rarity.Rare));
    }

    [Fact]
    public void StyleReallocatesBudgetInsteadOfAddingABonus()
    {
        var evaluator = CreateEvaluator();
        var plain = evaluator.Evaluate("plain", 1, 0, null);
        var styled = evaluator.Evaluate("plain", 1, 0, "fury");
        Assert.Equal(100d, plain.TargetBudget);
        Assert.Equal(plain.TargetBudget, styled.TargetBudget);
        Assert.Equal(AttributeValueQuantizer.Quantize(AttributeType.Power, 85d / 22.5d), styled.Stats[AttributeType.Power], 2);
        Assert.Equal(2.5f, styled.Stats[AttributeType.CritChance]);
        Assert.True(styled.Stats[AttributeType.Power] < plain.Stats[AttributeType.Power]);
        Assert.Equal("set.fury", styled.EquipmentSetId);
    }

    [Fact]
    public void TierScalingPreservesDirectPercentagesAndScalesFlatStats()
    {
        var evaluator = CreateEvaluator();
        var first = evaluator.Evaluate("plain", 1, 2, "fury");
        var later = evaluator.Evaluate("plain", 10, 2, "fury");
        Assert.Equal(first.Stats[AttributeType.CritChance], later.Stats[AttributeType.CritChance]);
        Assert.True(later.Stats[AttributeType.Power] > first.Stats[AttributeType.Power]);
    }

    [Fact]
    public void CombinedHandBudgetsRemainEquivalent()
    {
        var balance = new EquipmentBalance(1);
        foreach (var tier in new[] { 1, 2, 10 })
        {
            var one = balance.GetBaselineBudget(tier, EquipmentType.OneHanded);
            var off = balance.GetBaselineBudget(tier, EquipmentType.OffHand);
            var two = balance.GetBaselineBudget(tier, EquipmentType.TwoHanded);
            Assert.Equal(one * 2, two);
            Assert.Equal(one + off, two);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => balance.GetBaselineBudget(1, EquipmentType.Tool));
        Assert.Throws<ArgumentException>(() => new EquipmentArchetype("bad", "sword", EquipmentType.TwoHanded,
            new EquipmentBehaviorDefinition { Handedness = "OneHanded" }, Weights(AttributeType.Power)));
    }

    [Fact]
    public void EvaluationIsIndependentOfDictionaryOrderAndExternalMutation()
    {
        var input = new Dictionary<AttributeType, double> { [AttributeType.Power] = 70, [AttributeType.Armor] = 30 };
        var archetype = Archetype(weights: input);
        var evaluator = new EquipmentEvaluator(new(1), [archetype], [], [Definition()]);
        var before = evaluator.Evaluate("plain", 1, 0, null);
        input[AttributeType.Power] = 1_000_000;
        AssertStatsEqual(before, evaluator.Evaluate("plain", 1, 0, null));
        var reordered = new EquipmentEvaluator(new(1),
            [Archetype(weights: new Dictionary<AttributeType, double> { [AttributeType.Armor] = 30, [AttributeType.Power] = 70 })], [], [Definition()]);
        AssertStatsEqual(before, reordered.Evaluate("plain", 1, 0, null));
    }

    [Fact]
    public void CappedStatsRedistributeOnlyThroughAuthoredWeights()
    {
        var capped = Archetype(weights: Weights(AttributeType.CritChance), overflow: Weights(AttributeType.Power));
        var evaluator = new EquipmentEvaluator(new(1, baseTierBudget: 1000), [capped], [], [Definition()]);
        var item = Award(evaluator);
        var before = evaluator.Evaluate(item);
        Assert.Equal(EquipmentStatBudgetCatalog.Get(AttributeType.CritChance).PerItemHardCap, before.Stats[AttributeType.CritChance]);
        var improved = item.RecordPaidRankImprovement(evaluator, Guid.NewGuid(), 5, 250);
        Assert.True(evaluator.Evaluate(improved).Stats[AttributeType.Power] > before.Stats[AttributeType.Power]);

        var invalid = new EquipmentEvaluator(new(1, baseTierBudget: 1000),
            [Archetype(weights: Weights(AttributeType.CritChance))], [], [Definition()]);
        Assert.Throws<InvalidOperationException>(() => invalid.Evaluate("plain", 1, 0, null));
    }

    [Fact]
    public void TinyRankChangesCannotChargeForIdenticalQuantizedStats()
    {
        var evaluator = CreateEvaluator(balance: new(1, rankBudgetIncrement: 0.000000000001d));
        var item = Award(evaluator);
        Assert.Throws<InvalidOperationException>(() => item.RecordPaidRankImprovement(evaluator, Guid.NewGuid(), 5, 250));
        Assert.Equal(0, item.Rank);
        Assert.Equal(0, item.PaidScrap);
        Assert.True(item.Ownership.CanTradeOrDonate);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void InvalidRanksAreRejected(int rank) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateEvaluator().Evaluate("plain", 1, rank, null));

    [Fact]
    public void MissingOrIncompatibleDefinitionsDoNotFallBackToLegacyStats()
    {
        var evaluator = CreateEvaluator();
        Assert.Throws<ArgumentException>(() => evaluator.Evaluate("missing", 1, 0, null));
        Assert.Throws<ArgumentException>(() => evaluator.Evaluate("plain", 1, 0, "missing"));
        Assert.Throws<ArgumentOutOfRangeException>(() => evaluator.Evaluate("plain", 0, 0, null));
        Assert.Throws<ArgumentException>(() => new EquipmentEvaluator(new(1), [Archetype()],
            [new EquipmentStyle("bad", ["unknown-archetype"], Weights(AttributeType.Power))], [Definition()]));
        Assert.Throws<ArgumentException>(() => new EquipmentEvaluator(new(1), [Archetype()], [],
            [Definition(nativeStyle: "unknown-style")]));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void InvalidBudgetInputsFailAtDefinitionBoundary(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EquipmentBalance(1, baseTierBudget: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => Archetype(weights: new Dictionary<AttributeType, double> { [AttributeType.Power] = value }));
    }

    [Fact]
    public void UnsupportedAndEmptyProfilesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => Archetype(weights: new Dictionary<AttributeType, double>()));
        Assert.Throws<ArgumentException>(() => Archetype(weights: Weights((AttributeType)int.MaxValue)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EquipmentDefinition("bad", "Bad", "sword", (EquipmentRarity)1));
        Assert.Throws<ArgumentException>(() => new EquipmentEvaluator(new(1), [Archetype(), Archetype()], [], [Definition()]));
    }

    [Fact]
    public void BalanceVersionMismatchIsRejectedAndOldStateIsUnchanged()
    {
        var evaluator = CreateEvaluator();
        var original = Award(evaluator);
        var newBalance = CreateEvaluator(balance: new(2, baseTierBudget: 200));
        Assert.Throws<InvalidOperationException>(() => newBalance.Evaluate(original));
        Assert.Throws<InvalidOperationException>(() => original.RecordPaidRankImprovement(newBalance, Guid.NewGuid(), 5, 250));
        Assert.Equal(1, original.ModelVersion);
        Assert.Equal(1, original.BalanceVersion);
        Assert.Equal(100, evaluator.Evaluate(original).BaselineBudget);
    }

    [Fact]
    public void AwardedRanksNeverCreateRefundableInvestment()
    {
        var evaluator = CreateEvaluator();
        var random = Award(evaluator, rank: 1);
        var guaranteed = Award(evaluator, kind: EquipmentAwardKind.ProtectedReward, rank: 1);
        foreach (var item in new[] { random, guaranteed })
        {
            var rank2 = item.RecordPaidRankImprovement(evaluator, Guid.NewGuid(), 10, 500);
            var rank3 = rank2.RecordPaidRankImprovement(evaluator, Guid.NewGuid(), 20, 1000);
            Assert.Equal(30, rank3.PaidScrap);
            Assert.Equal(1500, rank3.PaidCinders);
            Assert.Equal(item.BaseSalvageScrap + 15, rank3.GetSalvageScrap());
            Assert.Equal(1, item.Rank);
            Assert.Empty(item.Investments);
        }
        Assert.True(random.Ownership.CanTradeOrDonate);
        Assert.False(guaranteed.Ownership.CanTradeOrDonate);
        Assert.Equal(0, guaranteed.GetSalvageScrap());
    }

    [Fact]
    public void RankReceiptsPreventDuplicateInvestmentAndRejectConflictingRetries()
    {
        var evaluator = CreateEvaluator();
        var operation = Guid.NewGuid();
        var rank1 = Award(evaluator).RecordPaidRankImprovement(evaluator, operation, 5, 250);
        var rank2 = rank1.RecordPaidRankImprovement(evaluator, Guid.NewGuid(), 10, 500);
        Assert.Same(rank2, rank2.RecordPaidRankImprovement(evaluator, operation, 5, 250));
        Assert.Throws<InvalidOperationException>(() => rank2.RecordPaidRankImprovement(evaluator, operation, 6, 250));
        Assert.Equal(15, rank2.PaidScrap);
        Assert.False(rank2.Ownership.CanTradeOrDonate);
        Assert.Equal(7 + rank2.BaseSalvageScrap, rank2.GetSalvageScrap());
    }

    [Fact]
    public void RankFiveCannotBeImprovedAndLedgerCannotOverflow()
    {
        var evaluator = CreateEvaluator();
        Assert.Throws<InvalidOperationException>(() => Award(evaluator, rank: 5)
            .RecordPaidRankImprovement(evaluator, Guid.NewGuid(), 80, 4000));
        var full = Award(evaluator).RecordPaidRankImprovement(evaluator, Guid.NewGuid(), long.MaxValue, 1);
        Assert.Throws<OverflowException>(() => full.RecordPaidRankImprovement(evaluator, Guid.NewGuid(), 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => full.GetSalvageScrap(1m));
        Assert.Throws<ArgumentException>(() => full.RecordPaidRankImprovement(evaluator, Guid.NewGuid(), 0, 1));
    }

    [Fact]
    public void NativeStyleCanBeRestoredWithoutLearningAndSetMembershipIsReplaced()
    {
        var evaluator = CreateEvaluator(nativeStyle: "fury");
        var item = Award(evaluator, rank: 1);
        Assert.Same(item, item.ChangeStyle(evaluator, "fury", Learned()));
        Assert.True(item.Ownership.CanTradeOrDonate);
        var plain = item.ChangeStyle(evaluator, null, Learned());
        Assert.Null(evaluator.Evaluate(plain).EquipmentSetId);
        Assert.False(plain.Ownership.CanTradeOrDonate);
        var changed = plain.ChangeStyle(evaluator, "ward", Learned("ward"));
        Assert.Equal("set.ward", evaluator.Evaluate(changed).EquipmentSetId);
        var restored = changed.ChangeStyle(evaluator, "fury", Learned());
        Assert.Equal("set.fury", evaluator.Evaluate(restored).EquipmentSetId);
        Assert.Equal(item.Provenance, restored.Provenance);
        Assert.Equal(item.DefinitionId, restored.DefinitionId);
        Assert.Equal(item.NativeStyleId, restored.NativeStyleId);
        Assert.Equal(item.Tier, restored.Tier);
        Assert.Equal(item.Rank, restored.Rank);
        AssertStatsEqual(evaluator.Evaluate(item), evaluator.Evaluate(restored));
        Assert.Throws<InvalidOperationException>(() => item.ChangeStyle(evaluator, "ward", Learned()));
    }

    [Fact]
    public void StyleChangesPreservePaidInvestmentAndNeverUnbind()
    {
        var evaluator = CreateEvaluator();
        var invested = Award(evaluator).RecordPaidRankImprovement(evaluator, Guid.NewGuid(), 5, 250);
        var changed = invested.ChangeStyle(evaluator, "fury", Learned("fury"));
        var plain = changed.ChangeStyle(evaluator, null, Learned());
        Assert.Equal(invested.Investments, plain.Investments);
        Assert.Equal(invested.GetSalvageScrap(), plain.GetSalvageScrap());
        Assert.False(plain.Ownership.CanTradeOrDonate);
    }

    [Fact]
    public void GuildPropertyCannotBePersonallyBoundImprovedRestyledOrSalvaged()
    {
        var evaluator = CreateEvaluator();
        var item = Award(evaluator);
        var guildId = Guid.NewGuid();
        var donated = item.DonateToGuild(guildId);
        Assert.Equal(guildId, donated.Ownership.OwnerId);
        Assert.Equal(EquipmentOwnershipKind.GuildOwned, donated.Ownership.Kind);
        Assert.Throws<InvalidOperationException>(() => donated.BindForPersonalUse());
        Assert.Throws<InvalidOperationException>(() => donated.ChangeStyle(evaluator, "fury", Learned("fury")));
        Assert.Throws<InvalidOperationException>(() => donated.RecordPaidRankImprovement(evaluator, Guid.NewGuid(), 5, 250));
        Assert.Throws<InvalidOperationException>(() => donated.GetSalvageScrap());
        Assert.Throws<InvalidOperationException>(() => item.BindForPersonalUse().DonateToGuild(guildId));
    }

    [Fact]
    public void CurrentAuthoredArchetypesAndStylesHaveDeterministicImprovingRanks()
    {
        var root = FindContentRoot();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        var recipes = JsonSerializer.Deserialize<CraftingRecipeDefinition[]>(File.ReadAllText(Path.Combine(root, "crafting/base-recipes.json")), options)!
            .Where(r => r.Enabled).ToArray();
        var blueprints = JsonSerializer.Deserialize<BlueprintDefinition[]>(File.ReadAllText(Path.Combine(root, "crafting/blueprints.json")), options)!
            .Where(b => b.Enabled).ToArray();
        var archetypes = recipes.Select(r => new EquipmentArchetype(r.Id, r.OutputItemId, r.OutputItemType,
            r.Behavior, r.InitialStatProfile, minimumTier: r.TierRange.Min, maximumTier: r.TierRange.Max)).ToArray();
        var styles = blueprints.Select(b => new EquipmentStyle(b.Id,
            recipes.Where(r => EquipmentCraftingDesignComposer.IsCompatible(r, b)).Select(r => r.Id), b.BonusStatProfile, b.EquipmentSetId)).ToArray();
        var evaluator = new EquipmentEvaluator(new(1), archetypes, styles,
            recipes.Select(r => new EquipmentDefinition(r.OutputItemId, r.Name, r.Id, EquipmentRarity.Common)));
        Assert.NotEmpty(archetypes);
        foreach (var archetype in archetypes)
        foreach (var tier in new[] { archetype.MinimumTier, archetype.MaximumTier }.Distinct())
        foreach (var styleId in styles.Where(s => s.CompatibleArchetypeIds.Contains(archetype.Id)).Select(s => s.Id).Prepend(null))
        {
            var item = Award(evaluator, definitionId: archetype.ItemBaseId, tier: tier);
            item = item.ChangeStyle(evaluator, styleId, styles.Select(s => s.Id).ToHashSet());
            for (var rank = 0; rank < 5; rank++)
            {
                var before = evaluator.Evaluate(item);
                item = item.RecordPaidRankImprovement(evaluator, Guid.NewGuid(), 5, 250);
                var after = evaluator.Evaluate(item);
                Assert.Equal(before.Archetype.Behavior, after.Archetype.Behavior);
                Assert.Equal(before.EquipmentSetId, after.EquipmentSetId);
                Assert.True(after.TargetBudget > before.TargetBudget);
                Assert.All(after.Stats, pair => Assert.Equal(AttributeValueQuantizer.Quantize(pair.Key, pair.Value), pair.Value));
            }
        }
    }

    private static EquipmentEvaluator CreateEvaluator(EquipmentBalance? balance = null,
        EquipmentRarity rarity = EquipmentRarity.Common, string? nativeStyle = null) =>
        new(balance ?? new(1), [Archetype()],
            [new("fury", ["sword"], Weights(AttributeType.CritChance), "set.fury"),
             new("ward", ["sword"], Weights(AttributeType.Armor), "set.ward")], [Definition(rarity, nativeStyle)]);

    private static EquipmentDefinition Definition(EquipmentRarity rarity = EquipmentRarity.Common,
        string? nativeStyle = null) => new("plain", "Sword", "sword", rarity, nativeStyle, randomDiscoveryBaseScrap: 2);

    private static EquipmentArchetype Archetype(IReadOnlyDictionary<AttributeType, double>? weights = null,
        IReadOnlyDictionary<AttributeType, double>? overflow = null) =>
        new("sword", "shortsword", EquipmentType.OneHanded,
            new EquipmentBehaviorDefinition { Handedness = "OneHanded", AttackCategory = "Physical", RangeCategory = "Melee" },
            weights ?? Weights(AttributeType.Power), overflow);

    private static Dictionary<AttributeType, double> Weights(AttributeType attribute) => new() { [attribute] = 1d };
    private static HashSet<string> Learned(params string[] styles) => styles.ToHashSet();

    private static EquipmentState Award(EquipmentEvaluator evaluator,
        EquipmentAwardKind kind = EquipmentAwardKind.RandomDiscovery, int rank = 0,
        string definitionId = "plain", int tier = 1) =>
        EquipmentState.Award(Guid.NewGuid(), evaluator, definitionId, tier, rank,
            new(kind, "test.source", Guid.NewGuid().ToString()), new(EquipmentOwnershipKind.UnboundPersonal, Guid.NewGuid()));

    private static void AssertStatsEqual(EquipmentEvaluation expected, EquipmentEvaluation actual) =>
        Assert.Equal(expected.Stats.OrderBy(x => x.Key), actual.Stats.OrderBy(x => x.Key));

    private static string FindContentRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "LL", "src", "API", "API.LL", "Data");
            if (Directory.Exists(candidate))
                return candidate;
        }
        throw new DirectoryNotFoundException("Unable to find the checked-in API content.");
    }
}
