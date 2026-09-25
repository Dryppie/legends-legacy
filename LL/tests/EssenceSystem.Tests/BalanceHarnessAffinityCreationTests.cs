using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAffinityCreationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-affinity-creation-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Creation entered combat.")).Activate();
    public BalanceHarnessAffinityCreationTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    private static TowerProposalExportRequest Request(params string[] endpoints)
    {
        var (p, inventory) = BalanceHarnessDamageAffinityTests.Fixture(allSlots: true);
        if (endpoints.Length == 0) endpoints = ["e10", "e11"];
        var ids = TowerDamageSourceAffinities.Create(inventory).Affinities
            .Where(a => endpoints.Contains(a.ProducerEssenceId) && endpoints.Contains(a.ModifierEssenceId)).Select(a => a.Id);
        return new(TowerProposalPolicies.CreationExportVersion,
            new(p.Scope, p.Mechanics, p.BenchmarkReferenceId, p.RootSeed, inventory),
            [TowerProposalPolicies.BenchmarkSmallEdits(), TowerProposalPolicies.BenchmarkAffinityCreation(ids)]);
    }

    [Theory]
    [InlineData("e00", "e10", 1)]
    [InlineData("e10", "e11", 2)]
    public void Creates_missing_endpoints_at_minimal_distance_and_covers_multiple_owners(string first, string second, int distance)
    {
        var q = Request(first, second); var before = HarnessJson.Hash(q);
        var result = TowerProposalPolicies.Export(q); var arm = result.Arms[1];
        var parent = q.Context.Scope.Starts.Single(s => s.ReferenceId == q.Context.BenchmarkReferenceId).Party;
        Assert.Equal("Complete", result.Status); Assert.Equal(0, result.NewFights); Assert.Equal(0, result.NewReservedValues);
        Assert.Equal(before, HarnessJson.Hash(q)); Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(TowerProposalPolicies.Export(q)));
        Assert.Empty(result.IdenticalFirstWaveRecipes);
        Assert.Equal(9, arm.Batch.Candidates.Count);
        Assert.All(arm.Batch.Proposals.Where(p => p.Rejection is null), p => {
            var owner = Assert.Single(p.ChangedOwners); var step = Assert.IsType<TowerAffinityCreationStep>(p.AffinityCreation);
            var missing = new[] { first, second }.Count(id => !parent.Builds[owner].Contains(id));
            Assert.Equal(missing, p.ReplacementDistance); Assert.Equal(missing, step.Added.Count);
            Assert.Equal(missing, step.Removed.Count); Assert.Null(p.Fallback);
            Assert.Contains(first, p.Party!.Builds[owner]); Assert.Contains(second, p.Party.Builds[owner]);
            Assert.Equal(step.TargetAffinityIds, step.NewlyActivatedAffinityIds);
            Assert.NotEqual(parent.Id, p.Party.Id); TowerBossDiscovery.ValidateParty(q.Context.Scope, p.Party);
            Assert.All(parent.Builds.Keys.Where(o => o != owner), o => Assert.Equal(parent.Builds[o], p.Party.Builds[o]));
        });
        Assert.Contains(arm.Batch.Proposals, p => p.Rejection is null && p.ReplacementDistance == distance);
        var coverage = Assert.IsType<TowerAffinityCreationCoverage>(arm.AffinityCreationCoverage);
        Assert.Single(coverage.Pairs); Assert.Equal(2, coverage.Pairs[0].AffinityIds.Count); // Two directions, one pair weight.
        Assert.Equal(5, coverage.EligiblePairOwners); Assert.Equal(5, coverage.AcceptedParentOwners);
        Assert.Equal(9, coverage.AcceptedCreations); Assert.Equal(0, coverage.UnchangedRecipes);
        Assert.All(arm.Teams, t => Assert.Empty(t.Scenario.Seeds));
        var reversed = TowerProposalPolicies.Export(q with { Policies = q.Policies.Reverse().ToArray() });
        Assert.Equal(HarnessJson.Hash(arm), HarnessJson.Hash(reversed.Arms[0]));
    }

    [Fact]
    public void Active_pairs_and_exhausted_copies_are_explicit_bounded_rejections_without_fallback()
    {
        var active = TowerProposalPolicies.Export(Request("e00", "e01")).Arms[1];
        Assert.Equal("Complete", active.Status);
        Assert.Contains(active.Batch.Proposals, p => p.ScheduledOwners.Single() == 1 && p.Rejection == "affinities-already-active");
        Assert.All(active.Batch.Proposals, p => Assert.Null(p.Fallback));
        Assert.Equal(1, active.AffinityCreationCoverage!.ActivePairOwners);
        Assert.Equal(4, active.AffinityCreationCoverage.EligiblePairOwners);
        var q = Request();
        var copies = q.Context.Scope.AllowedEssences.ToDictionary(e => e.Id, _ => q.Context.Scope.RequiredPartySize);
        copies["e10"] = 0;
        q = q with { Context = q.Context with { Scope = q.Context.Scope with { OwnedCopies = copies } } };
        var blocked = TowerProposalPolicies.Export(q).Arms[1];
        Assert.Empty(blocked.Batch.Candidates); Assert.Equal(128, blocked.Batch.Proposals.Count);
        Assert.All(blocked.Batch.Proposals, p => Assert.Equal("no-legal-affinity-creation", p.Rejection));
        Assert.Equal(0, blocked.AffinityCreationCoverage!.EligiblePairOwners);
    }

    [Fact]
    public void Family_conflicts_must_be_removed_and_protected_conflicts_cannot_be_released()
    {
        var q = Request("e00", "e10");
        var input = TowerBossDiscovery.CopyGenerationInputs(q.Context.Scope);
        var parent = q.Context.Scope.Starts.Single(s => s.ReferenceId == q.Context.BenchmarkReferenceId).Party;
        input = input with { AllowedEssences = input.AllowedEssences.Select(e => e.Id == "e10" ? e with { Family = "family1" } : e).ToArray() };
        var affinities = TowerDamageSourceAffinities.Create(q.Context.DamageAffinityInventory!).Affinities
            .Where(a => q.Policies[1].CreatedDamageAffinityIds!.Contains(a.Id)).ToArray();
        var creation = new TowerAffinityCreation(input, affinities, (_, _) => []);
        var opportunity = Assert.Single(creation.Opportunities(parent, 1, default));
        Assert.Equal("e01", Assert.Single(Assert.Single(opportunity.LegalEdits).Removed));
        var protectedCreation = new TowerAffinityCreation(input, affinities, (_, _) => ["e01"]);
        Assert.Empty(Assert.Single(protectedCreation.Opportunities(parent, 1, default)).LegalEdits);
        Assert.Equal("no-legal-affinity-creation", protectedCreation.Create(parent, 1, new Random(1), default).Rejection);
    }

    [Fact]
    public void Existing_selected_affinities_are_preserved_and_second_wave_remains_deterministic()
    {
        var q = Request();
        var active = TowerDamageSourceAffinities.Create(q.Context.DamageAffinityInventory!).Affinities
            .Where(a => new[] { "e00", "e01" }.Contains(a.ProducerEssenceId) && new[] { "e00", "e01" }.Contains(a.ModifierEssenceId))
            .Select(a => a.Id).Order(StringComparer.Ordinal).ToArray();
        var policy = q.Policies[1] with { PreservedDamageAffinityIds = active };
        q = q with { Policies = [q.Policies[0], policy] };
        var export = TowerProposalPolicies.Export(q).Arms[1];
        Assert.All(export.Batch.Candidates, p => Assert.All(export.ProtectedDamageAffinities!, a => {
            Assert.Contains(a.ProducerEssenceId, p.Builds[a.Owner]); Assert.Contains(a.ModifierEssenceId, p.Builds[a.Owner]);
        }));
        TowerAdaptiveBatch Second()
        {
            var g = new TowerAdaptiveRacingGenerator(q.Context, policy);
            var seen = q.Context.Scope.Starts.Select(s => s.Party.Id).ToHashSet();
            var first = g.Generate(1, [], seen, [], default);
            seen.UnionWith(first.Candidates.Select(p => p.Id));
            return g.Generate(2, first.Candidates.Take(4).ToArray(), seen, [], default);
        }
        var second = Second();
        Assert.Equal(8, second.Candidates.Count);
        Assert.Equal(HarnessJson.Hash(second), HarnessJson.Hash(Second()));
        Assert.InRange(second.Proposals.Count, 8, 128);
        Assert.All(second.Candidates, p => Assert.DoesNotContain(p.Id, export.Batch.Candidates.Select(c => c.Id)));
    }

    [Theory]
    [InlineData("old-export")]
    [InlineData("old-policy")]
    [InlineData("empty")]
    [InlineData("unknown")]
    [InlineData("duplicate")]
    [InlineData("missing-inventory")]
    public void Invalid_bindings_reject_before_generation(string change)
    {
        var q = Request(); var p = q.Policies[1];
        q = change switch {
            "old-export" => q with { Version = TowerProposalPolicies.DamageExportVersion },
            "old-policy" => q with { Policies = [q.Policies[0], p with { Version = TowerProposalPolicies.DamagePolicyVersion }] },
            "missing-inventory" => q with { Context = q.Context with { DamageAffinityInventory = null } },
            _ => q with { Policies = [q.Policies[0], p with { CreatedDamageAffinityIds = change switch {
                "empty" => [], "unknown" => [new string('f', 64)], _ => [p.CreatedDamageAffinityIds![0], p.CreatedDamageAffinityIds[0]] } }] }
        };
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.Export(q));
    }

    [Fact]
    public async Task Creation_policy_cannot_enter_an_older_racing_version()
    {
        var q = Request(); var (racing, _) = BalanceHarnessDamageAffinityTests.Fixture(allSlots: true);
        foreach (var version in new[] { TowerProposalPolicies.RacingVersion, TowerProposalPolicies.DamageRacingVersion })
            await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.RunAsync(
                new(version, racing, q.Policies[1], q.Context.DamageAffinityInventory), (_, _) => throw new InvalidOperationException("No evaluator calls")));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.Throws<OperationCanceledException>(() => TowerProposalPolicies.Export(q, cancelled.Token));
    }

    [Fact]
    public void Coverage_is_reconstructed_and_rehashed_tampering_and_overwrite_are_rejected()
    {
        var q = Request(); var output = Path.Combine(root, "export");
        var pin = TowerProposalPolicies.WriteExport(q, output);
        Assert.Equal(HarnessJson.Hash(TowerProposalPolicies.Export(q)), HarnessJson.Hash(TowerProposalPolicies.VerifyExport(output, pin)));
        Assert.Throws<IOException>(() => TowerProposalPolicies.WriteExport(q, output));
        var path = Path.Combine(output, "batches.json");
        var saved = JsonNode.Parse(File.ReadAllText(path))!;
        saved["arms"]![1]!["affinityCreationCoverage"]!["eligiblePairOwners"] = 999;
        File.WriteAllText(path, saved.ToJsonString(HarnessJson.Options));
        File.Delete(Path.Combine(output, "files.json"));
        HarnessJson.WriteNew(Path.Combine(output, "files.json"), new Dictionary<string, string> {
            ["request.json"] = HarnessJson.FileHash(Path.Combine(output, "request.json")), ["batches.json"] = HarnessJson.FileHash(path) });
        Assert.Throws<InvalidDataException>(() => TowerProposalPolicies.VerifyExport(output, HarnessJson.FileHash(Path.Combine(output, "files.json"))));
    }
}
