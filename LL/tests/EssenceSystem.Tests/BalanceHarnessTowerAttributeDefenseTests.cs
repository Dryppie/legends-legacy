using System.Text.Json;
using BalanceHarness;
using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerAttributeDefenseTests
{
    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static TowerBossDiscoveryDefinition Definition(int floor = 5, int slots = 5)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(floor, slots);
        return d with { Generation = d.Generation with { PolicyVersion = TowerBossGeneration.DefenseVersion,
            Methods = TowerBossGeneration.DefenseMethods, CandidatesPerArm = 64, Seeds = [17], MaximumAttemptsPerArm = 1024 },
            Stages = d.Stages with { Shortlist = 8 } };
    }

    [Fact]
    public void Attribute_eligibility_follows_operation_value_semantics_and_keeps_conditions_as_hypotheses()
    {
        var a = new AbilitySpec { Kind = AbilitySpecKind.Passive };
        AbilityEffectSpec Effect() => new() { Id = "defense", Operation = AbilityEffectOperation.ModifyAttribute, Target = AbilityTargetSelector.Self,
            Attribute = AttributeType.Armor, BaseValue = 2 };
        Assert.True(TowerAttributeDefense.Eligible(a, Effect())); // Implicit OnCombatStart.
        foreach (var mutate in new Action<AbilityEffectSpec>[] {
            e => e.Target = AbilityTargetSelector.AllEnemies, e => e.ChancePercent = 0,
            e => e.BaseValue = -1, e => e.BaseValue = 0, e => e.Attribute = AttributeType.Power,
            e => e.Operation = AbilityEffectOperation.TransferAttributePercent,
            e => e.EventMagnitudeCoefficient = 1, e => e.ScalingCondition = StandardConditionType.Poison,
            e => e.ScalingCoefficient = -1, e => e.MaximumScalingCoefficient = -1,
            e => e.ScalingCoefficient = float.NaN, e => e.ScalingStatusId = "conditional-status",
            e => e.ScalingOwnedSummonId = "summon", e => e.LivingNonSummonedAllyDamagePercent = -100 })
        { var e = Effect(); mutate(e); Assert.False(TowerAttributeDefense.Eligible(a, e)); }
        var scaled = Effect(); scaled.BaseValue = 0; scaled.ScalingCoefficient = .3f;
        Assert.False(TowerAttributeDefense.Eligible(a, scaled)); // Generic scaling requires its attribute.
        scaled.ScalingAttribute = AttributeType.Resistance; Assert.True(TowerAttributeDefense.Eligible(a, scaled));
        scaled.Conditions = [new() { Type = AbilityConditionType.HealthAbovePercent, Value = 66 }];
        a.Triggers = [new() { Event = AbilityTriggerEvent.OnHealthChanged, EffectIds = [scaled.Id] }];
        Assert.True(TowerAttributeDefense.Eligible(a, scaled));
        a.Triggers[0].EffectIds = ["other"]; Assert.False(TowerAttributeDefense.Eligible(a, scaled));
        a.Triggers.Clear();
        var percent = Effect(); percent.Operation = AbilityEffectOperation.ModifyAttributePercentOfInitial;
        Assert.False(TowerAttributeDefense.Eligible(a, percent)); // BaseValue is ignored by this operation.
        percent.BaseValue = -50; percent.ScalingCoefficient = .3f; Assert.True(TowerAttributeDefense.Eligible(a, percent));
        percent.ScalingCoefficient = -.3f; Assert.False(TowerAttributeDefense.Eligible(a, percent));
        var ally = Effect(); ally.Operation = AbilityEffectOperation.SynchronizeAttributePerLivingNonSummonedAlly;
        Assert.False(TowerAttributeDefense.Eligible(a, ally));
        ally.MaximumCount = 1; Assert.True(TowerAttributeDefense.Eligible(a, ally));
        ally.ScalingCoefficient = -.1f; Assert.False(TowerAttributeDefense.Eligible(a, ally)); // Coefficient overrides positive base.
        ally.BaseValue = -5; ally.ScalingCoefficient = .1f; Assert.True(TowerAttributeDefense.Eligible(a, ally));
    }

    [Fact]
    public void Factory_preserves_legacy_features_and_adds_only_evidenced_protection_without_reweighting_intents()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition()); var inventory = Inventory.Value; var before = HarnessJson.Hash(inventory);
        var m = TowerBossPartyGenerator.FromInventory(input, inventory);
        Assert.Equal(HarnessJson.Hash(TowerPartyCoverage.Create(input, inventory)), HarnessJson.Hash(m.Coverage));
        var additions = m.DefenseCoverage!.Where(f => f.Limitation is not null).ToArray(); Assert.NotEmpty(additions);
        Assert.All(additions, f => { Assert.Equal("protection", f.Kind); Assert.Equal(TowerAttributeDefense.Limitation, f.Limitation);
            Assert.All(f.EvidenceKeys, k => Assert.Contains(inventory.Nodes, n => n.Key == k)); });
        Assert.Equal(HarnessJson.Hash(m.Coverage!.Where(f => f.Kind != "protection")), HarnessJson.Hash(m.DefenseCoverage!.Where(f => f.Kind != "protection")));
        Assert.All(m.Coverage!.Where(f => f.Kind == "protection"), old => Assert.All(old.EvidenceKeys,
            k => Assert.Contains(k, m.DefenseCoverage!.Single(f => f.EssenceId == old.EssenceId && f.Kind == old.Kind).EvidenceKeys)));
        var legacyInput = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.CollectiveVersion, Methods = TowerBossGeneration.CollectiveMethods } };
        var legacy = TowerBossPartyGenerator.FromInventory(legacyInput, inventory);
        Assert.Equal(HarnessJson.Hash(legacy), HarnessJson.Hash(m with { DefenseCoverage = null }));
        Assert.DoesNotContain("defenseCoverage", JsonSerializer.Serialize(legacy, HarnessJson.Options));
        Assert.DoesNotContain("limitation", JsonSerializer.Serialize(legacy.Coverage, HarnessJson.Options));
        Assert.Equal(before, HarnessJson.Hash(inventory));
        // Direct extraction must not borrow a child status effect merely because it is present in the inventory.
        var emptyDirect = inventory with { Essences = inventory.Essences.Select(e => e with { AbilityIds = [] }).ToArray() };
        Assert.Empty(TowerAttributeDefense.Create(input, emptyDirect));
    }

    [Theory]
    [InlineData(1, 4)] [InlineData(5, 5)] [InlineData(11, 7)] [InlineData(15, 10)]
    public void Defense_changes_only_coverage_and_preserves_legal_deterministic_uniform_construction(int floor, int slots)
    {
        var d = Definition(floor, slots); var input = TowerBossDiscovery.GenerationInputs(d); var m = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        var hash = HarnessJson.Hash(m); var defense = new TowerBossPartyGenerator(input, m, attributeDefense: true);
        var baseline = new TowerBossPartyGenerator(input, m); var sameCoverage = new TowerBossPartyGenerator(input, m with { DefenseCoverage = m.Coverage }, attributeDefense: true);
        var uniform = 0;
        for (var seed = 0; seed < 32; seed++)
        {
            var result = defense.FreshCoverage(new Random(seed)); Assert.Null(result.Rejection); TowerBossDiscovery.ValidateParty(d, result.Party!);
            Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(defense.FreshCoverage(new Random(seed))));
            Assert.Equal(HarnessJson.Hash(baseline.FreshCoverage(new Random(seed))), HarnessJson.Hash(sameCoverage.FreshCoverage(new Random(seed))));
            if (result.Intent == "coverage:uniform") { uniform++; Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(baseline.FreshCoverage(new Random(seed)))); }
        }
        Assert.True(uniform > 0); Assert.Equal(hash, HarnessJson.Hash(m));
    }

    [Fact]
    public void Defense_validates_feature_boundaries_and_obeys_scarce_ownership()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition()); var m = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, m with { DefenseCoverage = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, m with { Coverage = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, m with { Cores = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, m with { DefenseCoverage = [] }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, m with { DefenseCoverage = [.. m.DefenseCoverage!, m.DefenseCoverage![0]] }));
        var empty = new TowerBossPartyGenerator(input, m with { Coverage = [], DefenseCoverage = [] }, attributeDefense: true).FreshCoverage(new Random(0));
        Assert.Null(empty.Rejection); Assert.Contains("uniform", empty.Intent);
        input = input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var generator = new TowerBossPartyGenerator(input, m, attributeDefense: true); var accepted = 0;
        for (var seed = 0; seed < 24; seed++)
        {
            var result = generator.FreshCoverage(new Random(seed));
            if (result.Rejection is not null) { if (result.Party is not null) Assert.Equal(result.Rejection, generator.Invalid(result.Party)); continue; }
            accepted++; Assert.Null(generator.Invalid(result.Party!)); Assert.All(result.Party!.Builds.Values.SelectMany(x => x).GroupBy(x => x), x => Assert.Single(x));
        }
        Assert.True(accepted > 0);
    }

    [Fact]
    public async Task V8_preserves_v6_comparator_and_restricts_defense_provenance()
    {
        var d = Definition(); var input = TowerBossDiscovery.GenerationInputs(d); var m = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        Task<BossDiscoveryMeasurement> Score(PartyChoice p, string arm, CancellationToken token) => Task.FromResult(
            BalanceHarnessTowerBossGenerationTests.Measure(input, p, Convert.ToInt32(p.Id[..2], 16) % 9));
        var result = await TowerBossGeneration.RunAsync(input, m, Score); Assert.Equal("Complete", result.Status);
        var oldInput = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.CollectiveVersion, Methods = TowerBossGeneration.CollectiveMethods } };
        var old = await TowerBossGeneration.RunAsync(oldInput, TowerBossPartyGenerator.FromInventory(oldInput, Inventory.Value), Score);
        Assert.Equal(HarnessJson.Hash(old.Arms.Single(a => a.Method == "collective-joint")), HarnessJson.Hash(result.Arms[0]));
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await TowerBossGeneration.RunAsync(input, m, Score)));
        var all = result.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray(); TowerBossDiscovery.ValidateProvenance(d, all);
        var fresh = result.Arms[1].Proposals.First(p => p.Provenance.Operator == "fresh-defense").Provenance;
        Assert.Empty(fresh.ParentIds); Assert.All(all, p => Assert.Empty(p.ReferenceIds));
        Assert.DoesNotContain(result.Arms[0].Proposals, p => p.Provenance.Operator == "fresh-defense");
        Assert.DoesNotContain(result.Arms[1].Proposals, p => p.Provenance.Operator is "fresh-completion" or "fresh-coverage");
        Assert.Contains(result.Arms[1].Proposals, p => p.Provenance.Operator == "collective-provider");
        foreach (var altered in new[] { fresh with { Method = "collective-joint" }, fresh with { ParentIds = ["invalid"] }, fresh with { ReferenceIds = ["saved"] } })
            Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, all.Select(p => p.Id == fresh.Id ? altered : p).ToArray()));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Generation = d.Generation with { Methods = TowerBossGeneration.DefenseMethods.Reverse().ToArray() } }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(oldInput, m));
        Assert.Equal(TowerBossGeneration.Version, BalanceHarnessTowerBossDiscoveryContractTests.Definition().Generation.PolicyVersion);
    }
}
