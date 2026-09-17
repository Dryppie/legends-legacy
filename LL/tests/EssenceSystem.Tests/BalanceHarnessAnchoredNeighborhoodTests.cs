using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAnchoredNeighborhoodTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Anchored fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    private static string Json(object? value) => JsonSerializer.Serialize(value, HarnessJson.Options);

    internal static TowerBossDiscoveryDefinition Definition()
    {
        var source = F.Definition(F.Input(owners: 10, poolSize: 20, candidates: 46, attempts: 46));
        source = source with {
            Budget = source.Budget with { EssenceSlots = 5, CharacterLevel = 40, PriorityFloor = 5 },
            Contexts = source.Contexts.Select(c => c with { CharacterTemplates = c.CharacterTemplates.Select(p =>
                p with { Build = p.Build with { CharacterLevel = 40 } }).ToArray() }).ToArray(),
            Stages = I.Definition().Stages, MaximumBattles = 1264
        };
        var builds = Enumerable.Range(1, 10).ToDictionary(s => s, _ => (IReadOnlyList<string>)new[] { "e00", "e01", "e02", "e03", "e04" });
        var first = TowerPartySelection.Choice("fixture", builds);
        var second = TowerPartySelection.Choice("fixture", builds.ToDictionary(p => p.Key,
            p => p.Key == 1 ? (IReadOnlyList<string>)new[] { "e00", "e01", "e02", "e03", "e05" } : p.Value));
        source = source with { References = new[] { first, second }.Select((p, i) => new BossBenchmarkReference("anchor-" + i, "fixture",
            TowerBossDiscovery.Scenario(source, "fixture", p, []), "Synthetic anchored fixture", new string('d', 64))).ToArray() };
        return TowerSuppliedCompositionSearch.Prepare(source, ["anchor-0", "anchor-1"], TowerAnchoredNeighborhoodSearch.Version, "anchor-0");
    }

    private static Task<BossGenerationResult> Run(TowerBossDiscoveryDefinition d,
        Func<PartyChoice, string, CancellationToken, Task<BossDiscoveryMeasurement>>? evaluate = null,
        CancellationToken token = default, Action<BossGenerationResult>? checkpoint = null)
    {
        var input = TowerBossImprovement.Inputs(d);
        return TowerBossImprovement.ExecuteAsync(d, input, F.Mechanics(input), evaluate ?? ((p, _, _) =>
            Task.FromResult(I.Measure(input, p, Convert.ToInt32(p.Id[..4], 16) % 9))), token, checkpoint);
    }

    [Fact]
    public async Task Frozen_batch_covers_every_character_and_excludes_duplicate_supplied_recipes()
    {
        var d = Definition(); var input = TowerBossImprovement.Inputs(d); BossGenerationResult? frozen = null; var calls = 0;
        var result = await Run(d, (p, _, _) => {
            Assert.NotNull(frozen); Assert.Equal(46, frozen!.Arms.Single().Proposals.Count);
            Assert.Empty(frozen.Arms.Single().Evaluations);
            calls++; return Task.FromResult(I.Measure(input, p));
        }, checkpoint: r => { if (r.Arms.Single().StopReason == "BatchFrozen") frozen = r; });
        Assert.Equal("Complete", result.Status); Assert.Null(result.Error); Assert.Equal(46, calls);
        var arm = Assert.Single(result.Arms); var batch = arm.AnchoredBatch!;
        Assert.Equal("anchor-0", batch.PrimaryReferenceId);
        Assert.Equal(Enumerable.Range(1, 10), batch.CharacterOrder.Order());
        Assert.Equal(74, batch.LegalOptionsPerSlot[1]); // 5*15 minus the secondary supplied recipe.
        Assert.All(batch.LegalOptionsPerSlot.Where(p => p.Key != 1), p => Assert.Equal(75, p.Value));
        Assert.Equal(46, arm.Proposals.Select(p => p.Party!.Id).Distinct().Count());
        Assert.All(arm.Proposals, p => { Assert.Equal("evaluated", p.Result); TowerBossDiscovery.ValidateParty(d, p.Party!); });
        var primary = d.Starts.Single(s => s.ReferenceId == "anchor-0").Party;
        var neighbors = arm.Proposals.Skip(2).ToArray();
        Assert.Equal(44, neighbors.Length);
        Assert.Equal(Enumerable.Range(0, 44).Select(i => batch.CharacterOrder[i % 10]), neighbors.Select(p => p.AnchoredEdit!.PartySlot));
        foreach (var slot in batch.CharacterOrder)
            Assert.Equal(batch.CharacterOrder.Take(4).Contains(slot) ? 5 : 4, neighbors.Count(p => p.AnchoredEdit!.PartySlot == slot));
        Assert.All(neighbors, p => {
            var edit = p.AnchoredEdit!;
            Assert.Equal(new[] { edit.PartySlot }, p.Party!.Builds.Where(b => !b.Value.SequenceEqual(primary.Builds[b.Key])).Select(b => b.Key));
            Assert.Equal(new[] { edit.Removed }, primary.Builds[edit.PartySlot].Except(p.Party.Builds[edit.PartySlot]));
            Assert.Equal(new[] { edit.Added }, p.Party.Builds[edit.PartySlot].Except(primary.Builds[edit.PartySlot]));
            Assert.All(p.Party.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids)));
            Assert.Equal(new[] { "anchor-0" }, p.Provenance.ReferenceIds);
        });
        TowerBossDiscovery.ValidateProvenance(d, arm.Proposals.Select(p => p.Provenance).ToArray());
        Assert.All(d.Starts, s => Assert.Contains(result.DiscoveryShortlist, p => p.Id == s.Party.Id));
        Assert.Equal(4, result.DiscoveryShortlist.Count);
        Assert.Equal(frozen!.Arms.Single().Proposals.Select(p => p.Party!.Id), arm.Proposals.Select(p => p.Party!.Id));
    }

    [Fact]
    public async Task Reproduction_ignores_feedback_collection_order_and_caller_mutation()
    {
        var d = Definition(); var input = TowerBossImprovement.Inputs(d);
        var baseline = await Run(d);
        Assert.Equal(Json(baseline), Json(await Run(d)));
        var reversed = d with { AllowedEssences = d.AllowedEssences.Reverse().ToArray(), Starts = d.Starts.Reverse().ToArray() };
        Assert.Equal(Json(baseline), Json(await Run(reversed)));
        var changed = await Run(d, (p, _, _) => {
            ((int[])d.Generation.Seeds)[0] = 9999;
            return Task.FromResult(I.Measure(input, p, 8, 0));
        });
        Assert.Equal(baseline.Arms.Single().Proposals.Select(p => p.Party!.Id), changed.Arms.Single().Proposals.Select(p => p.Party!.Id));
        Assert.Equal(17, changed.Arms.Single().Seed);
    }

    [Fact]
    public async Task Root_rotates_extra_characters_and_explicit_primary_changes_parentage()
    {
        var d = Definition(); var first = await Run(d);
        var second = await Run(d with { Generation = d.Generation with { Seeds = [18] } });
        Assert.NotEqual(Json(first.Arms.Single().AnchoredBatch), Json(second.Arms.Single().AnchoredBatch));
        Assert.False(first.Arms.Single().AnchoredBatch!.CharacterOrder.Take(4).ToHashSet()
            .SetEquals(second.Arms.Single().AnchoredBatch!.CharacterOrder.Take(4)));
        var alternate = await Run(d with { PrimaryReferenceId = "anchor-1" });
        Assert.Equal("Complete", alternate.Status);
        Assert.All(alternate.Arms.Single().Proposals.Skip(2), p => Assert.Equal(new[] { "anchor-1" }, p.Provenance.ReferenceIds));
    }

    [Fact]
    public async Task Family_and_owned_copy_limits_filter_options_before_sampling()
    {
        var d = Definition();
        d = d with { AllowedEssences = d.AllowedEssences.Select(e => e.Id == "e06" ? e with { Family = "FAMILY0" } : e).ToArray(),
            OwnedCopies = d.AllowedEssences.ToDictionary(e => e.Id, e => e.Id == "e07" ? 0 : 10) };
        var result = await Run(d);
        Assert.Equal("Complete", result.Status);
        Assert.Equal(66, result.Arms.Single().AnchoredBatch!.LegalOptionsPerSlot[2]); // 75 minus four family conflicts and five unavailable copies.
        Assert.All(result.Arms.Single().Proposals, p => {
            TowerBossDiscovery.ValidateParty(d, p.Party!);
            Assert.DoesNotContain("e07", p.Party!.Builds.Values.SelectMany(x => x));
        });
        // e06 can replace only e00; all other removals leave the same family present.
        Assert.All(result.Arms.Single().Proposals.Where(p => p.AnchoredEdit?.Added == "e06"), p => Assert.Equal("e00", p.AnchoredEdit!.Removed));
    }

    [Fact]
    public async Task Impossible_quota_rejects_the_whole_batch_without_combats_or_redistribution()
    {
        var d = Definition();
        d = d with { OwnedCopies = new Dictionary<string, int> { ["e00"] = 10, ["e01"] = 10, ["e02"] = 10, ["e03"] = 10, ["e04"] = 10, ["e05"] = 1 } };
        // Only five edits per character exist, and slot 1 has four after excluding the secondary.
        // Find a deterministic root whose fifth-edit quota includes slot 1.
        var checkedRoots = 0;
        for (var seed = 17; seed < 37; seed++)
        {
            var result = await Run(d with { Generation = d.Generation with { Seeds = [seed] } });
            if (result.Status != "Invalid") continue;
            Assert.Contains("quota", result.Error); Assert.Empty(result.Arms.Single().Evaluations);
            Assert.Empty(result.Arms.Single().Proposals); Assert.Empty(result.DiscoveryShortlist); checkedRoots++; break;
        }
        Assert.Equal(1, checkedRoots);
    }

    [Theory]
    [InlineData("missing-primary")]
    [InlineData("unknown-primary")]
    [InlineData("wrong-candidates")]
    [InlineData("wrong-discovery")]
    [InlineData("wrong-selection")]
    [InlineData("legacy-primary")]
    public void Invalid_contracts_are_rejected(string mutation)
    {
        var d = Definition();
        d = mutation switch {
            "missing-primary" => d with { PrimaryReferenceId = null },
            "unknown-primary" => d with { PrimaryReferenceId = "unknown" },
            "wrong-candidates" => d with { Generation = d.Generation with { CandidatesPerArm = 45 } },
            "legacy-primary" => d with { Generation = d.Generation with { PolicyVersion = TowerSuppliedCompositionSearch.IncumbentVersion } },
            _ => d with { Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(p => p.Key,
                p => mutation == "wrong-discovery" ? p.Value with { Discovery = [101] } : p.Value with { Selection = [201] }) } }
        };
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task Cancellation_preserves_the_frozen_batch_and_completed_measurements(int completed)
    {
        var d = Definition(); var input = TowerBossImprovement.Inputs(d); using var cancellation = new CancellationTokenSource(); var calls = 0;
        if (completed == 0) cancellation.Cancel();
        var result = await Run(d, (p, _, ct) => {
            if (calls++ == completed) { cancellation.Cancel(); ct.ThrowIfCancellationRequested(); }
            return Task.FromResult(I.Measure(input, p));
        }, cancellation.Token);
        Assert.Equal("Cancelled", result.Status); Assert.Empty(result.DiscoveryShortlist);
        Assert.Equal(completed, result.Arms.Single().Evaluations.Count);
        Assert.Equal(completed == 0 ? 0 : 46, result.Arms.Single().Proposals.Count);
        if (completed > 0) Assert.Equal("evaluating", result.Arms.Single().Proposals[completed].Result);
    }

    [Fact]
    public async Task Invalid_measurement_retains_partial_evidence_without_nomination()
    {
        var d = Definition(); var input = TowerBossImprovement.Inputs(d); var calls = 0;
        var result = await Run(d, (p, _, _) => Task.FromResult(calls++ == 2 ? I.Measure(input, p) with { Id = "wrong" } : I.Measure(input, p)));
        Assert.Equal("Invalid", result.Status); Assert.Equal(2, result.Arms.Single().Evaluations.Count);
        Assert.Equal(46, result.Arms.Single().Proposals.Count); Assert.Empty(result.DiscoveryShortlist);
    }

    [Fact]
    public async Task Failure_to_save_the_batch_prevents_evaluation()
    {
        var calls = 0;
        var result = await Run(Definition(), (_, _, _) => { calls++; throw new InvalidOperationException("Unexpected evaluation."); },
            checkpoint: r => { if (TowerAnchoredNeighborhoodSearch.IsFrozenBatch(r)) throw new IOException("Cannot persist frozen batch."); });
        Assert.Equal(0, calls); Assert.Equal("Invalid", result.Status);
        Assert.Contains("Cannot persist frozen batch", result.Error);
        Assert.Equal(46, result.Arms.Single().Proposals.Count); Assert.Empty(result.DiscoveryShortlist);
    }

    [Fact]
    public async Task Mid_panel_cancellation_keeps_actual_fight_accounting_without_a_completed_measurement()
    {
        using var cancellation = new CancellationTokenSource(); var completed = 0;
        var report = await BalanceHarnessPracticalSearchTests.Study(Definition(), "improved", done => {
            if (done && ++completed == 3) cancellation.Cancel();
        }, token: cancellation.Token);
        Assert.Equal("Cancelled", report.Status);
        Assert.Equal(3, report.Accounting.Attempted["discovery"]); Assert.Equal(3, report.Accounting.Completed["discovery"]);
        Assert.Equal(0, report.Accounting.Attempted["selection"]);
        Assert.Equal(46, report.Discovery!.Arms.Single().Proposals.Count);
        Assert.Empty(report.Discovery.Arms.Single().Evaluations); Assert.Empty(report.Discovery.DiscoveryShortlist);
        Assert.Null(report.Confirmation);
    }

    [Fact]
    public async Task Practical_study_accounts_for_fixed_panels_and_freezes_one_finalist()
    {
        var d = Definition(); var attempts = 0; var completed = 0;
        var report = await BalanceHarnessPracticalSearchTests.Study(d, "improved", done => { if (done) completed++; else attempts++; });
        Assert.Equal("Complete", report.Status); Assert.Null(report.Error);
        Assert.Equal(1264, attempts); Assert.Equal(attempts, completed);
        Assert.Equal(368, report.Accounting.Attempted["discovery"]); Assert.Equal(128, report.Accounting.Attempted["selection"]);
        Assert.Equal(attempts, TowerBossDiscovery.Validate(d).Total);
        Assert.All(d.Starts, s => Assert.Contains(report.Discovery!.DiscoveryShortlist, p => p.Id == s.Party.Id));
        var result = TowerPracticalSearch.Assess(d, report, new string('a', 64));
        Assert.Equal("Verified", result.IntegrityStatus);
        Assert.All(TowerPracticalSearch.Export(d, report, result).Teams, t => Assert.Empty(t.Scenario.Seeds));
        Assert.Equal(Json(report), Json(await BalanceHarnessPracticalSearchTests.Study(d, "improved")));
    }

    [Fact]
    public void Allocation_binds_policy_without_exposing_reference_designation_to_generation_inputs()
    {
        var d = Definition();
        var template = d with { ExcludedCombatSeeds = [-987], Generation = d.Generation with { Seeds = [] },
            Stages = d.Stages with { Schedules = new Dictionary<string, BossDiscoverySchedule> { ["fixture"] = new([], [], [], []) } } };
        var root = Path.Combine(Path.GetTempPath(), "anchored-shape-only");
        var request = new TowerPracticalRequest(TowerPracticalSearch.AllocationVersion, root, Path.Combine(root, "definition.json"),
            new string('a', 64), root, Path.Combine(root, "output"), new Dictionary<string, string>(), 60, 32 * 1048576,
            Allocation: new(123, "anchored-fixture", 8, 32, 256));
        var bound = TowerPracticalSearch.ValidateAllocationTemplate(request, template);
        Assert.Equal("anchor-0", bound.PrimaryReferenceId); Assert.Equal(368, TowerBossDiscovery.Validate(bound).Discovery);
        Assert.DoesNotContain("primaryReferenceId", Json(TowerBossImprovement.Inputs(bound)));
        Assert.DoesNotContain("primaryReferenceId", Json(I.Definition()));
        Assert.DoesNotContain("anchoredBatch", Json(new BossGenerationArm("legacy", 1, "Complete", [], [])));
        Assert.DoesNotContain("anchoredEdit", Json(new BossGeneratedProposal(new("p", 1, "legacy", "single", [], []), null, "", null, "rejected")));
    }
}
