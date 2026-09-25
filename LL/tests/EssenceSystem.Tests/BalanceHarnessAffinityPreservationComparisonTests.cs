using System.Buffers.Binary;
using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAffinityPreservationComparisonTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preservation comparison entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    private static (TowerProposalContext Context, TowerProposalComparisonPlan Plan, int[] History) Fixture() =>
        BalanceHarnessProposalStudyTests.Fixture(preservation: true);
    private static int[] Values() => Enumerable.Range(200000, 4380).ToArray();

    [Fact]
    public void Design_changes_only_the_proposer_with_shared_search_values_and_isolated_heldout()
    {
        var (context, plan, _) = Fixture(); var before = HarnessJson.Hash(context);
        Assert.Equal((12, 256, 528, 4380, 21888, 10800, 6442450944L),
            (plan.Roots, plan.HeldoutSamples, plan.SearchFightsPerArm, plan.RequiredFreshValues, plan.MaximumFights, plan.MaximumSeconds, plan.MaximumBytes));
        Assert.Equal(new TowerProposalSelectionContrast(TowerBenchmarkValidation.Version, TowerBenchmarkValidation.Version), plan.SelectionContrast);
        var binding = TowerProposalComparison.Bind(plan, context, Values());
        Assert.Equal(HarnessJson.Hash(binding), HarnessJson.Hash(TowerProposalComparison.Bind(plan, context, Values())));
        Assert.Equal(before, HarnessJson.Hash(context)); Assert.Equal(12, binding.Pairs.Count);
        foreach (var pair in binding.Pairs)
        {
            TowerProposalComparison.ValidatePair(pair); TowerProposalComparison.ValidatePair(pair, plan.Version);
            Assert.Equal(TowerProposalPolicies.BenchmarkValidationRacingVersion, pair.Control.Version);
            Assert.Equal(TowerProposalPolicies.PreservingValidationRacingVersion, pair.Candidate.Version);
            Assert.Equal(HarnessJson.Hash(pair.Control.Racing), HarnessJson.Hash(pair.Candidate.Racing));
            Assert.Equal(new[] { 8, 8, 8, 8, 16, 60 }, pair.Control.Racing.Panels.Select(p => p.Seeds.Count));
            Assert.Equal(plan.Control.CreatedDamageAffinityIds, plan.Candidate.CreatedDamageAffinityIds);
            Assert.Equal(Values().Skip(1308 + (pair.Root - 1) * 256).Take(256), pair.HeldoutSeeds);
            Assert.Empty(pair.HeldoutSeeds.Intersect(pair.Control.Racing.Panels.SelectMany(p => p.Seeds)));
            Assert.All(pair.HeldoutSeeds, s => Assert.Contains(s, pair.Candidate.Racing.Scope.ExcludedCombatSeeds));
            Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidatePair(pair, TowerProposalComparison.ValidationVersion));
        }
        Assert.Equal(Values(), binding.Pairs.SelectMany(p => p.Control.Racing.Panels.SelectMany(x => x.Seeds)
            .Append(p.Control.Racing.RootSeed).Concat(p.HeldoutSeeds)).Order());
        var old = TowerProposalComparison.CreateValidationPlan(context, plan.Control);
        Assert.Equal(HarnessJson.Hash(old.Analysis), HarnessJson.Hash(plan.Analysis));
        Assert.Equal(4668, old.RequiredFreshValues);
        Assert.Equal(TowerProposalPolicies.CreationPolicyVersion, old.Candidate.Version);
    }

    [Theory]
    [InlineData("control")]
    [InlineData("affinities")]
    [InlineData("selector")]
    [InlineData("allocation")]
    [InlineData("pairing")]
    [InlineData("resources")]
    [InlineData("analysis")]
    [InlineData("version")]
    public void Rejects_changes_to_the_frozen_single_factor_design(string change)
    {
        var (_, p, _) = Fixture();
        p = change switch {
            "control" => p with { Control = p.Candidate },
            "affinities" => p with { Candidate = TowerProposalPolicies.BenchmarkPreservingAffinityCreation(p.Candidate.CreatedDamageAffinityIds!.Take(1)) },
            "selector" => p with { SelectionContrast = p.SelectionContrast! with { Control = TowerProposalPolicies.BenchmarkTieSelectionVersion } },
            "allocation" => p with { RequiredFreshValues = 4668 },
            "pairing" => p with { ValidationProtocol = p.ValidationProtocol! with { ValidationValues = 59 } },
            "resources" => p with { MaximumFights = 21889 },
            "analysis" => p with { Analysis = p.Analysis with { GoMethodAtLeast = 0 } },
            _ => p with { Version = TowerProposalComparison.ValidationVersion }
        };
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Validate(p));
    }

    [Theory]
    [InlineData("nomination")]
    [InlineData("validation")]
    [InlineData("heldout")]
    [InlineData("racing")]
    public void Rejects_unpaired_panels_and_cross_version_pairs(string change)
    {
        var (c, p, _) = Fixture(); var pair = TowerProposalComparison.Bind(p, c, Values()).Pairs[0];
        var panels = pair.Candidate.Racing.Panels.ToArray();
        if (change is "nomination" or "validation")
        {
            var i = change == "nomination" ? 4 : 5; panels[i] = panels[i] with { Seeds = panels[i].Seeds.Reverse().ToArray() };
            pair = pair with { Candidate = pair.Candidate with { Racing = pair.Candidate.Racing with { Panels = panels } } };
        }
        if (change == "heldout") pair = pair with { HeldoutSeeds = panels[5].Seeds.Concat(pair.HeldoutSeeds.Skip(60)).ToArray() };
        if (change == "racing") pair = pair with { Control = pair.Control with { Version = TowerProposalPolicies.PreservingValidationRacingVersion } };
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidatePair(pair, p.Version));
    }

    [Fact]
    public void Allocation_rejects_missing_duplicate_and_historical_values_and_keeps_the_exposed_tail()
    {
        var (c, p, history) = Fixture();
        foreach (var values in new[] { Values()[..^1], Values().Select((s,i) => i == 1 ? 200000 : s).ToArray(),
            Values().Select((s,i) => i == 0 ? c.RootSeed : s).ToArray() })
            Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Bind(p, c, values));
        var entropy = new byte[65536];
        for (var i = 0; i < 16384; i++) BinaryPrimitives.WriteInt32LittleEndian(entropy.AsSpan(i * 4, 4), 200000 + i);
        var allocation = TowerProposalStudy.Classify(entropy, history, p.Version);
        Assert.Equal(Values(), allocation.Selected); Assert.Equal(16384, allocation.Reserved.Count);
        Assert.Contains(216383, allocation.Reserved);
    }

    [Fact]
    public void Feasibility_rejects_a_preserved_neighborhood_with_fewer_than_seventeen_recipes()
    {
        var (c, _, _) = Fixture();
        var ids = TowerDamageSourceAffinities.Create(c.DamageAffinityInventory!).Affinities
            .Where(a => a.ModifierEssenceId == "e10" && new[] { "e00", "e01", "e02", "e09", "e04", "e05", "e06", "e07" }.Contains(a.ProducerEssenceId))
            .Select(a => a.Id).ToArray();
        Assert.Contains("17 distinct", Assert.Throws<InvalidDataException>(() => TowerProposalComparison.CreatePreservationPlan(c,
            TowerProposalPolicies.BenchmarkPreservingAffinityCreation(ids))).Message);
    }

    private static Task<TowerProposalRacingReport> Run(TowerProposalRacingPlan p) => TowerProposalPolicies.RunAsync(p,
        (r, _) => Task.FromResult(new TowerPanelOutcome(HarnessJson.Hash(r), $"literal-{r.Ordinal:D6}", r.Seed,
            BattleOutcome.Victory, 0, 100, 1)), default, null, true);

    [Fact]
    public async Task V6_keeps_the_validation_gate_reconstructs_metadata_and_rejects_shared_observation_drift()
    {
        var (c, p, _) = Fixture(); var pair = TowerProposalComparison.Bind(p, c, Values()).Pairs[0];
        var a = await Run(pair.Control); var b = await Run(pair.Candidate);
        Assert.Equal(528, b.Evaluation.ChargedEvaluations); Assert.Equal(6, b.Evaluation.Panels.Count);
        Assert.False(b.Evaluation.ValidationDecision!.Passed); Assert.Equal(p.BenchmarkPartyId, b.Evaluation.RawSelectedId);
        Assert.Equal(HarnessJson.Hash(b), HarnessJson.Hash(await TowerProposalPolicies.ReconstructAsync(pair.Candidate, b)));
        TowerProposalComparison.ValidatePreservationTrajectories(a, b);
        var panels = b.Evaluation.Panels.ToArray(); var observations = panels[0].Observations.ToArray();
        observations[0] = observations[0] with { Outcome = observations[0].Outcome with { GuardianHealth = 1 } };
        panels[0] = panels[0] with { Observations = observations };
        var drifted = b with { Evaluation = b.Evaluation with { Panels = panels } };
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidatePreservationTrajectories(a, drifted));
        var batches = b.Batches.ToArray(); var proposals = batches[0].Proposals.ToArray();
        var at = Array.FindIndex(proposals, t => t.AffinityCreation is not null);
        var step = proposals[at].AffinityCreation!;
        proposals[at] = proposals[at] with { AffinityCreation = step with { RemovalSelection = step.RemovalSelection! with { EligibleEdits = 999 } } };
        batches[0] = batches[0] with { Proposals = proposals };
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.ReconstructAsync(pair.Candidate, b with { Batches = batches }));
        Assert.DoesNotContain("controlValidation", JsonSerializer.Serialize(a, HarnessJson.Options));
    }

    [Fact]
    public async Task Incomplete_pair_or_changed_shared_results_cannot_reach_heldout()
    {
        var (c, p, _) = Fixture(); var calls = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalStudy.Execute(p, c, Values(),
            pair => Task.FromResult(new TowerProposalComparisonSearch(HarnessJson.Hash(p), HarnessJson.Hash(pair), "Incomplete", null!, null)),
            (_, _) => { calls++; throw new InvalidOperationException(); }, (_, _) => { }, () => new string('a', 64), _ => { }, default));
        Assert.Equal(0, calls);
        var pair = TowerProposalComparison.Bind(p, c, Values()).Pairs[0]; var preflights = 0; var runs = 0;
        await Assert.ThrowsAsync<IOException>(() => TowerProposalComparison.ExecutePairAsync(p, pair,
            (_, _) => { if (++preflights == 2) throw new IOException("candidate preflight"); },
            (_, _) => { runs++; throw new InvalidOperationException(); }));
        Assert.Equal(0, runs);
    }

    [Fact]
    public async Task Full_literal_study_verifies_both_gates_global_barrier_and_native_archives()
    {
        using var fixture = new BalanceHarnessProposalStudyTests();
        await fixture.FullNativeFixture(creation: true, preservation: true);
    }
}
