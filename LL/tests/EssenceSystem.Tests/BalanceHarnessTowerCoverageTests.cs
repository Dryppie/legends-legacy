using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat.Abilities;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerCoverageTests
{
    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static TowerBossDiscoveryDefinition Definition(int floor = 5, int slots = 5)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(floor, slots);
        return d with { Generation = d.Generation with { PolicyVersion = TowerBossGeneration.CoverageVersion,
            Methods = TowerBossGeneration.CoverageMethods, CandidatesPerArm = 64, Seeds = [17], MaximumAttemptsPerArm = 1024 },
            Stages = d.Stages with { Shortlist = 8 } };
    }

    [Fact]
    public void Coverage_uses_direct_effects_with_evidence_and_excludes_nonrecurring_or_wrong_target_control()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition()); var inventory = Inventory.Value;
        var features = TowerPartyCoverage.Create(input, inventory);
        Assert.Equal(TowerPartyCoverage.Kinds.Order(), features.Select(f => f.Kind).Distinct().Order());
        Assert.All(features, f => Assert.All(f.EvidenceKeys, key => Assert.Contains(inventory.Nodes, n => n.Key == key)));
        var control = features.First(f => f.Kind == "recurring-control");
        var abilityKey = control.EvidenceKeys.First(k => k.StartsWith("Ability:"));
        var node = inventory.Nodes.Single(n => n.Key == abilityKey);
        foreach (var change in new[] { "target", "one-shot", "zero-chance" })
        {
            var ability = node.Definition.Deserialize<AbilitySpec>(HarnessJson.Options)!;
            if (change == "one-shot") { ability.Kind = AbilitySpecKind.Passive; ability.Triggers = [new() { Event = AbilityTriggerEvent.OnCombatStart }]; }
            foreach (var effect in ability.Effects)
            {
                if (change == "target") effect.Target = AbilityTargetSelector.Self;
                if (change == "zero-chance") effect.ChancePercent = 0;
            }
            var altered = inventory with { Nodes = inventory.Nodes.Select(n => n.Key == abilityKey
                ? n with { Definition = JsonSerializer.SerializeToElement(ability, HarnessJson.Options) } : n).ToArray() };
            Assert.DoesNotContain(TowerPartyCoverage.Create(input, altered), f => f.Kind == "recurring-control" && f.EvidenceKeys.Contains(abilityKey));
        }
    }

    [Theory]
    [InlineData(1, 4)] [InlineData(5, 5)] [InlineData(11, 7)] [InlineData(15, 10)]
    public void Coverage_varies_counts_and_placement_preserves_inventory_and_parents(int floor, int slots)
    {
        var d = Definition(floor, slots); var input = TowerBossDiscovery.GenerationInputs(d);
        var generator = new TowerBossPartyGenerator(input, TowerBossPartyGenerator.FromInventory(input, Inventory.Value));
        var signatures = new HashSet<string>(); var successfulSwaps = 0; var uniform = 0;
        for (var seed = 0; seed < 40; seed++)
        {
            var fresh = generator.FreshCoverage(new Random(seed)); Assert.Null(fresh.Rejection);
            var party = fresh.Party!; TowerBossDiscovery.ValidateParty(d, party);
            signatures.Add(fresh.Intent); if (fresh.Intent == "coverage:uniform") uniform++;
            var hash = HarnessJson.Hash(party);
            var changed = generator.ChangeCoverage(new Random(seed), party); Assert.Null(changed.Rejection);
            TowerBossDiscovery.ValidateParty(d, changed.Party!);
            var placed = generator.ChangePlacement(new Random(seed), party);
            if (placed.Rejection is null)
            {
                successfulSwaps++; TowerBossDiscovery.ValidateParty(d, placed.Party!);
                Assert.NotEqual(party.Id, placed.Party!.Id);
                Assert.Equal(party.Builds.Values.SelectMany(v => v).Order(), placed.Party.Builds.Values.SelectMany(v => v).Order());
            }
            Assert.Equal(hash, HarnessJson.Hash(party));
        }
        Assert.True(signatures.Count > 20); Assert.True(uniform > 0); Assert.True(successfulSwaps > 20);
    }

    [Fact]
    public void Scarce_ownership_is_respected_and_absent_or_invalid_features_do_not_silently_pass()
    {
        var input = TowerBossDiscovery.GenerationInputs(Definition());
        input = input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        var generator = new TowerBossPartyGenerator(input, mechanics); var valid = 0;
        for (var seed = 0; seed < 20; seed++)
        {
            var fresh = generator.FreshCoverage(new Random(seed));
            if (fresh.Rejection is not null) continue;
            valid++; Assert.All(fresh.Party!.Builds.Values.SelectMany(v => v).GroupBy(id => id), g => Assert.Single(g));
            var change = generator.ChangeCoverage(new Random(seed), fresh.Party);
            Assert.Equal(change.Rejection, generator.Invalid(change.Party!));
        }
        Assert.True(valid > 0);
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, mechanics with { Coverage = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, mechanics with { Coverage = [mechanics.Coverage![0] with { Kind = "unknown" }] }));
        var fallback = new TowerBossPartyGenerator(input with { OwnedCopies = null }, mechanics with { Coverage = [] }).FreshCoverage(new Random(0));
        Assert.Null(fallback.Rejection); Assert.Contains("uniform", fallback.Intent);
    }

    [Fact]
    public async Task V4_preserves_v3_arm_and_enforces_operator_ancestry_and_reproducibility()
    {
        var d = Definition(); var input = TowerBossDiscovery.GenerationInputs(d);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        Task<BossDiscoveryMeasurement> Score(PartyChoice p, string arm, CancellationToken token) => Task.FromResult(
            BalanceHarnessTowerBossGenerationTests.Measure(input, p, Convert.ToInt32(p.Id[..2], 16) % 9));
        var result = await TowerBossGeneration.RunAsync(input, mechanics, Score); Assert.Equal("Complete", result.Status);
        var previous = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.MechanicsVersion, Methods = TowerBossGeneration.MechanicsMethods } };
        var oldMechanics = TowerBossPartyGenerator.FromInventory(previous, Inventory.Value);
        Assert.Null(oldMechanics.Coverage); Assert.DoesNotContain("\"coverage\"", JsonSerializer.Serialize(oldMechanics, HarnessJson.Options));
        Assert.Equal(HarnessJson.Hash(oldMechanics), HarnessJson.Hash(mechanics with { Coverage = null }));
        var baseline = await TowerBossGeneration.RunAsync(previous, oldMechanics, Score);
        Assert.Equal(HarnessJson.Hash(baseline.Arms.Single(a => a.Method == "mechanics-joint")), HarnessJson.Hash(result.Arms[0]));
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await TowerBossGeneration.RunAsync(input, mechanics, Score)));
        TowerBossDiscovery.ValidateProvenance(d, result.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
        var arm = result.Arms.Single(a => a.Method == "coverage-joint");
        foreach (var op in new[] { "coverage-count", "placement" }) Assert.Contains(arm.Proposals, p => p.Provenance.Operator == op && p.Provenance.ParentIds.Count == 1);
        var fresh = arm.Proposals.First(p => p.Provenance.Operator == "fresh-coverage").Provenance;
        Assert.Empty(fresh.ParentIds); Assert.All(arm.Proposals, p => Assert.Empty(p.Provenance.ReferenceIds));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, [fresh with { Method = "mechanics-joint" }]));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(d, [fresh with { ReferenceIds = ["saved"] }]));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Mode = TowerBossDiscovery.Improve }));
    }
}
