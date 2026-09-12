using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat.Abilities;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerMechanicCoreTests
{
    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static TowerBossDiscoveryDefinition Definition(int floor = 5, int slots = 5)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(floor, slots);
        return d with { Generation = d.Generation with { PolicyVersion = TowerBossGeneration.MechanicsVersion,
            Methods = TowerBossGeneration.MechanicsMethods, CandidatesPerArm = 64, Seeds = [17], MaximumAttemptsPerArm = 1024 },
            Stages = d.Stages with { Shortlist = 8 } };
    }

    [Fact]
    public void Core_extraction_retains_effect_evidence_and_recipient_scope_without_changing_legacy_mechanics()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition());
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        Assert.NotEmpty(mechanics.Cores!);
        Assert.Contains(mechanics.Cores!, c => c.Kind == "chain"
            && c.EssenceIds.Contains("essence.venomous_spiderling") && c.EssenceIds.Contains("essence.web_weaver_spider")
            && c.EssenceIds.Contains("essence.pack_howler"));
        Assert.Contains(mechanics.Cores!, c => c.Kind == "basic-attack" && c.EssenceIds.Contains("essence.spider_queen_royal_venom"));
        Assert.All(mechanics.Cores!, c => {
            Assert.All(c.EvidenceKeys, key => Assert.Contains(Inventory.Value.Nodes, n => n.Key == key));
            Assert.Equal(c.EssenceIds.Count, c.EssenceIds.Select(id => input.AllowedEssences.Single(e => e.Id == id).Family).Distinct().Count());
        });
        var legacy = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.CoordinatedVersion, Methods = TowerBossGeneration.CoordinatedMethods } };
        var old = TowerBossPartyGenerator.FromInventory(legacy, Inventory.Value);
        Assert.Null(old.Cores); Assert.DoesNotContain("\"cores\"", JsonSerializer.Serialize(old, HarnessJson.Options));
        Assert.Equal(HarnessJson.Hash(old), HarnessJson.Hash(mechanics with { Cores = null }));
    }

    [Fact]
    public void Enemy_condition_is_not_an_enabler_for_a_negative_or_self_target_predicate()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition()); var original = Inventory.Value;
        var selected = TowerMechanicCores.Create(input, original).First(c => c.Kind == "condition");
        var key = selected.EvidenceKeys.Single(k => original.Nodes.Single(n => n.Key == k).Definition.Deserialize<AbilityEffectSpec>(HarnessJson.Options)!.Conditions.Count > 0);
        var node = original.Nodes.Single(n => n.Key == key);
        foreach (var negative in new[] { true, false })
        {
            var effect = node.Definition.Deserialize<AbilityEffectSpec>(HarnessJson.Options)!;
            effect.Target = AbilityTargetSelector.Self;
            foreach (var condition in effect.Conditions)
            {
                condition.Type = negative ? AbilityConditionType.NoEnemyHasCondition : AbilityConditionType.HasCondition;
                condition.Subject = AbilityConditionSubject.Target;
            }
            var changed = original with { Nodes = original.Nodes.Select(n => n.Key == key
                ? n with { Definition = JsonSerializer.SerializeToElement(effect, HarnessJson.Options) } : n).ToArray() };
            Assert.DoesNotContain(TowerMechanicCores.Create(input, changed), c => c.EvidenceKeys.Contains(key));
        }
    }

    [Theory]
    [InlineData(1, 4)] [InlineData(5, 5)] [InlineData(11, 7)] [InlineData(15, 10)]
    public void Complete_mechanic_parties_and_replacements_are_legal_and_leave_parents_unchanged(int floor, int slots)
    {
        var d = Definition(floor, slots); var input = TowerBossDiscovery.GenerationInputs(d);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value); var generator = new TowerBossPartyGenerator(input, mechanics);
        for (var seed = 0; seed < 12; seed++)
        {
            var party = generator.FreshMechanics(new Random(seed)); Assert.Null(party.Rejection);
            TowerBossDiscovery.ValidateParty(d, party.Party!);
            Assert.All(party.Party!.Builds.Values, ids => Assert.Contains(mechanics.Cores!, c => c.EssenceIds.All(ids.Contains)));
            var before = HarnessJson.Hash(party.Party); var changed = generator.ReplaceMechanicCore(new Random(seed), party.Party);
            Assert.Null(changed.Rejection); TowerBossDiscovery.ValidateParty(d, changed.Party!);
            Assert.Equal(before, HarnessJson.Hash(party.Party));
        }
    }

    [Fact]
    public void Scarce_ownership_limits_cores_and_empty_core_pool_has_an_explicit_fallback()
    {
        var d = Definition(); var input = TowerBossDiscovery.GenerationInputs(d);
        input = input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value); var generator = new TowerBossPartyGenerator(input, mechanics);
        var valid = 0;
        for (var seed = 0; seed < 20; seed++)
        {
            var result = generator.FreshMechanics(new Random(seed));
            if (result.Rejection is null) { valid++; Assert.Null(generator.Invalid(result.Party!)); }
        }
        Assert.True(valid > 0);
        var fallback = new TowerBossPartyGenerator(input with { OwnedCopies = null }, mechanics with { Cores = [] }).FreshMechanics(new Random(7));
        Assert.Equal("no-compatible-core:fallback-coordinated", fallback.Intent); Assert.Null(fallback.Rejection);
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, mechanics with { Cores = null }));
    }

    [Fact]
    public async Task V3_preserves_v2_comparator_and_records_independent_core_ancestry()
    {
        var d = Definition(); var input = TowerBossDiscovery.GenerationInputs(d);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        Task<BossDiscoveryMeasurement> Score(PartyChoice p, string arm, CancellationToken token) => Task.FromResult(
            BalanceHarnessTowerBossGenerationTests.Measure(input, p, Convert.ToInt32(p.Id[..2], 16) % 9));
        var result = await TowerBossGeneration.RunAsync(input, mechanics, Score);
        Assert.Equal("Complete", result.Status);
        var old = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.CoordinatedVersion, Methods = TowerBossGeneration.CoordinatedMethods } };
        var baseline = await TowerBossGeneration.RunAsync(old, mechanics with { Cores = null }, Score);
        Assert.Equal(HarnessJson.Hash(baseline.Arms.Single(a => a.Method == "coordinated-joint")), HarnessJson.Hash(result.Arms[0]));
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await TowerBossGeneration.RunAsync(input, mechanics, Score)));
        TowerBossDiscovery.ValidateProvenance(d, result.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
        var arm = result.Arms.Single(a => a.Method == "mechanics-joint");
        Assert.Contains(arm.Proposals, p => p.Provenance.Operator == "mechanic-core" && p.Provenance.ParentIds.Count == 1);
        Assert.Contains(arm.Proposals, p => p.Provenance.Operator == "fresh-mechanics" && p.Provenance.ParentIds.Count == 0);
        Assert.All(arm.Proposals, p => Assert.Empty(p.Provenance.ReferenceIds));
        var fresh = arm.Proposals.First(p => p.Provenance.Operator == "fresh-mechanics").Provenance;
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, [fresh with { Method = "coordinated-joint" }]));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, [fresh with { ReferenceIds = ["saved"] }]));
    }
}
