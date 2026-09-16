using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using J = EssenceSystem.Tests.BalanceHarnessJoinedMechanicsFixture;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessJointStructuralSearchTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Structural test entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    private static BossDiscoveryInputs Input(int owners = 2, int candidates = 4, int attempts = 4)
    {
        var d = F.Input(owners: owners, candidates: candidates, attempts: attempts);
        return d with { Generation = d.Generation with { PolicyVersion = TowerJointStructuralSearch.Version, Methods = [TowerJointStructuralSearch.Method] } };
    }
    private static BossCoverageFeature Feature(string id, string kind) => new(id, kind, ["Effect:" + id]);
    private static BossGenerationMechanics Mechanics(BossDiscoveryInputs d, bool single = false) => F.Mechanics(d) with {
        Cores = [J.Core("e00", "e01")], Coverage = new[] {
            Feature("e00", "recurring-control"), Feature("e00", "enemy-pressure"), Feature("e01", "attack-enabler") }
            .Concat((single ? new[] { 2 } : new[] { 2, 4, 6, 8 }).Select(i => Feature("e" + i.ToString("D2"), "protection")))
            .Concat((single ? new[] { 3 } : new[] { 3, 5, 7, 9 }).Select(i => Feature("e" + i.ToString("D2"), "recovery"))).ToArray() };
    private static Task<BossGenerationResult> Run(BossDiscoveryInputs d, BossGenerationMechanics? m = null) =>
        TowerBossGeneration.RunAsync(d, m ?? Mechanics(d), (p, _, ct) => { ct.ThrowIfCancellationRequested(); return Task.FromResult(F.Measure(d, p)); });

    [Fact] public void Registration_requires_explicit_policy_and_fixed_bounds()
    {
        var d = Input(); TowerBossGeneration.ValidateInputs(d); TowerBossDiscovery.Validate(F.Definition(d));
        foreach (var g in new[] { d.Generation with { Methods = [TowerCompositionSearch.Method] }, d.Generation with { Seeds = [17, 18] },
            d.Generation with { CandidatesPerArm = 17, MaximumAttemptsPerArm = 17 }, d.Generation with { MaximumAttemptsPerArm = 17 } })
            Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(d with { Generation = g }));
        Assert.Throws<InvalidDataException>(() => TowerBossGeneration.ValidateInputs(Input(11)));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, Mechanics(d) with { Cores = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(d, Mechanics(d) with { Coverage = null }));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(F.Input(), F.Mechanics(F.Input())).FreshJointStructural(0, default));
    }

    [Fact] public async Task Complete_search_is_deterministic_diverse_and_canonical()
    {
        var d = Input(); var a = await Run(d); var b = await Run(d);
        Assert.Equal("Complete", a.Status); Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b));
        var arm = Assert.Single(a.Arms); Assert.Equal(4, arm.Evaluations.Count); Assert.Equal(4, arm.Proposals.Count);
        Assert.Equal(4, arm.Proposals.Select(p => p.Party!.Id).Distinct().Count());
        Assert.All(arm.Proposals, p => {
            Assert.Equal(2, p.Party!.Builds.Values.Select(ids => HarnessJson.Hash(ids)).Distinct().Count());
            Assert.All(p.Party.Builds.Values, ids => { Assert.Equal(4, ids.Count); Assert.True(TowerCompositionSearch.IsCanonical(ids)); });
            Assert.Equal(TowerJointStructuralSearch.Operator, p.Provenance.Operator); Assert.Empty(p.Provenance.ParentIds);
        });
    }

    [Fact] public async Task Empty_pool_charges_every_attempt_without_evaluation()
    {
        var d = Input(); var r = await Run(d, Mechanics(d) with { Coverage = [] }); var arm = Assert.Single(r.Arms);
        Assert.Equal("Incomplete", r.Status); Assert.Equal("ProposalBudgetExhausted", arm.StopReason); Assert.Empty(arm.Evaluations);
        Assert.Equal(4, arm.Proposals.Count); Assert.All(arm.Proposals, p => { Assert.Null(p.Party); Assert.Equal("joint-structural-pool-unavailable", p.Result); Assert.Equal(0, p.JointStructural!.FullRecipes); });
    }

    [Fact] public async Task Single_roster_is_evaluated_once_and_exhaustion_is_charged()
    {
        var d = Input(1, 2, 3); var r = await Run(d, Mechanics(d, single: true)); var arm = Assert.Single(r.Arms);
        Assert.Equal("Incomplete", r.Status); Assert.Single(arm.Evaluations); Assert.Equal(3, arm.Proposals.Count);
        Assert.Equal(new[] { 0, 1, 2 }, arm.Proposals.Select(p => p.JointStructural!.ProposalIndex));
        Assert.All(arm.Proposals.Skip(1), p => Assert.Equal("joint-structural-pool-unavailable", p.Result));
    }

    [Fact] public async Task Evaluator_cancellation_retains_charged_proposal_and_trace()
    {
        var d = Input(); using var stop = new CancellationTokenSource(); BossGenerationResult? saved = null;
        var r = await TowerBossGeneration.RunAsync(d, Mechanics(d), (_, _, ct) => { stop.Cancel(); ct.ThrowIfCancellationRequested(); throw new Exception(); }, stop.Token, x => saved = x);
        Assert.Equal("Cancelled", r.Status); var arm = Assert.Single(r.Arms); Assert.Empty(arm.Evaluations);
        Assert.Equal("Cancelled", Assert.Single(arm.Proposals).Result); Assert.NotNull(arm.Proposals[0].JointStructural);
        Assert.Equal(HarnessJson.Hash(r), HarnessJson.Hash(saved));
    }

    [Fact] public async Task Evaluator_failure_keeps_attempt_and_does_not_retry()
    {
        var d = Input(); var calls = 0;
        var r = await TowerBossGeneration.RunAsync(d, Mechanics(d), (_, _, _) => { calls++; throw new InvalidDataException("fixture failure"); });
        Assert.Equal(1, calls); Assert.Equal("Invalid", r.Status); Assert.Empty(r.Arms.Single().Evaluations);
        Assert.Equal("Invalid", Assert.Single(r.Arms.Single().Proposals).Result);
    }

    [Fact] public async Task Cancellation_before_construction_does_no_work()
    {
        var d = Input(); using var stop = new CancellationTokenSource(); stop.Cancel();
        var r = await TowerBossGeneration.RunAsync(d, Mechanics(d), (_, _, _) => throw new Exception("Evaluator called."), stop.Token);
        Assert.Equal("Cancelled", r.Status); Assert.Empty(r.Arms);
    }

    [Fact] public async Task Inputs_and_mechanics_remain_immutable()
    {
        var d = Input(); var m = Mechanics(d); var before = HarnessJson.Hash(new { d, m }); await Run(d, m);
        Assert.Equal(before, HarnessJson.Hash(new { d, m }));
    }

    [Fact] public async Task Shared_copy_shortage_cannot_emit_a_partial_or_illegal_roster()
    {
        var d = Input(); d = d with { OwnedCopies = d.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var r = await Run(d); Assert.Equal("Incomplete", r.Status); Assert.Empty(r.Arms.Single().Evaluations);
        Assert.All(r.Arms.Single().Proposals, p => Assert.Null(p.Party));
    }

    [Fact] public async Task Metadata_order_does_not_change_proposals_or_nominations()
    {
        var d = Input(); var m = Mechanics(d); var a = await Run(d, m);
        var b = await Run(d with { AllowedEssences = d.AllowedEssences.Reverse().ToArray() },
            m with { Cores = m.Cores!.Reverse().ToArray(), Coverage = m.Coverage!.Reverse().ToArray(), Essences = m.Essences.Reverse().ToArray() });
        Assert.Equal(HarnessJson.Hash(a), HarnessJson.Hash(b));
    }

    [Fact] public async Task Optional_trace_preserves_old_serialization_and_roundtrips()
    {
        var p = (await Run(Input())).Arms.Single().Proposals[0]; var json = JsonSerializer.Serialize(p, HarnessJson.Options);
        Assert.Equal(HarnessJson.Hash(p), HarnessJson.Hash(JsonSerializer.Deserialize<BossGeneratedProposal>(json, HarnessJson.Options)));
        using var old = JsonDocument.Parse(JsonSerializer.Serialize(p with { JointStructural = null }, HarnessJson.Options));
        Assert.False(old.RootElement.TryGetProperty("jointStructural", out _));
    }

    [Fact] public async Task Checkpoint_charges_attempt_before_evaluator_and_provenance_validates()
    {
        var d = Input(); BossGenerationResult? saved = null; var calls = 0;
        var r = await TowerBossGeneration.RunAsync(d, Mechanics(d), (p, _, _) => {
            var arm = Assert.Single(saved!.Arms); Assert.Equal(++calls, arm.Proposals.Count); Assert.Equal(calls - 1, arm.Evaluations.Count);
            Assert.Equal("evaluating", arm.Proposals[^1].Result); Assert.NotNull(arm.Proposals[^1].JointStructural);
            return Task.FromResult(F.Measure(d, p));
        }, checkpoint: x => saved = x);
        Assert.Equal(4, calls); Assert.Equal("Complete", r.Status);
        TowerBossDiscovery.ValidateProvenance(F.Definition(d), r.Arms.Single().Proposals.Select(p => p.Provenance).ToArray());
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(F.Definition(d),
            [new("bad", 17, TowerJointStructuralSearch.Method, "order", [], [])]));
    }
}
