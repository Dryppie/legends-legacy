using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using BalanceHarness.ProcessFixture;
using Domain.Models.Combat;
using Services.LL.Combat.Engine;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessThreeReferenceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-three-reference-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Three-reference fixture entered combat.")).Activate();
    public BalanceHarnessThreeReferenceTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private static string Json(object value) => JsonSerializer.Serialize(value, HarnessJson.Options);

    internal static TowerBossDiscoveryDefinition Definition(int owners = 2)
    {
        var d = I.Definition(owners: owners);
        var third = TowerPartySelection.Choice("third", d.Starts[0].Party.Builds.ToDictionary(p => p.Key,
            p => p.Key == 1 ? (IReadOnlyList<string>)["e00", "e01", "e02", "e09"] : p.Value));
        var source = d with {
            Mode = TowerBossDiscovery.Independent, Starts = [], Generation = d.Generation with { PolicyVersion = TowerBossGeneration.Version,
                Methods = TowerBossDiscovery.Methods },
            References = d.References.Append(new BossBenchmarkReference("anchor-2", "fixture",
                TowerBossDiscovery.Scenario(d, "fixture", third, []), "Third literal reference", new string('e', 64))).ToArray(),
            Stages = d.Stages with { Shortlist = 5 }, MaximumBattles = 10000
        };
        return TowerSuppliedCompositionSearch.Prepare(source, ["anchor-0", "anchor-1", "anchor-2"],
            TowerSuppliedCompositionSearch.ThreeReferenceVersion) with { MaximumBattles = 1696, ExcludedCombatSeeds = [-987] };
    }

    internal static Task<BossStudyReport> Study(TowerBossDiscoveryDefinition d, string mode = "improved", Action<bool>? attempt = null)
    {
        var input = TowerBossImprovement.Inputs(d); var ordinal = 0;
        return TowerBossStudy.ExecuteAsync(d, F.Mechanics(input), (_, stage, scenario, seed, _) => {
            var party = TowerPartySelection.Choice("literal", scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds));
            var index = d.Starts.ToList().FindIndex(s => s.Party.Id == party.Id);
            var incumbent = index >= 0;
            var panel = d.Stages.Schedules.Single().Value;
            var sample = (stage == "discovery" ? panel.Discovery : stage == "selection" ? panel.Selection : panel.Confirmation).ToList().IndexOf(seed);
            var won = stage == "discovery" ? sample < (incumbent ? 0 : 5)
                : stage == "selection" ? sample < (mode == "retain-third" ? index == 2 ? 28 : 23 : incumbent ? 23 : 26)
                : !incumbent || (index == 2 && mode == "third-blocks") || sample < 100;
            var outcome = won ? BattleOutcome.Victory : BattleOutcome.Defeat;
            var summary = new BattleSummary(outcome, outcome, "Literal three-reference fixture", 1, 1,
                [new SimpleCombatEntity("f", "f", "", 10, 0)], [], [], new CompactCombatTelemetry());
            var report = new TowerBattleReport(new BattleReport(1, scenario.Id, seed, 1,
                JsonSerializer.SerializeToElement(new { fixture = true }), summary, null), won,
                Convert.ToInt32(party.Id[..4], 16) / 655.35m, 1);
            return Task.FromResult((new LoadoutTrial($"fixture-{++ordinal:D6}", stage, HarnessJson.Hash(scenario), seed,
                new string('a', 64), new string('b', 64)), report));
        }, (_, _, _) => throw new InvalidOperationException("No replay"), (_, _) => { }, default, attempt: attempt);
    }

    [Fact]
    public async Task All_three_weak_incumbents_survive_nomination_and_full_confirmation()
    {
        var d = Definition(); var report = await Study(d);
        Assert.Equal("Complete", report.Status); Assert.Null(report.Error);
        Assert.Equal(5, report.Discovery!.DiscoveryShortlist.Count);
        Assert.Equal(5, report.Selection.Count);
        Assert.All(d.Starts, s => Assert.Contains(report.Discovery.DiscoveryShortlist, p => p.Id == s.Party.Id));
        Assert.Equal(4, report.Confirmation!.Members.Count);
        Assert.Equal(d.References.Select(r => r.Id).Order(), report.Confirmation.Members.SelectMany(m => m.ReferenceIds).Order());
        Assert.Equal(1696, report.Accounting.Completed.Values.Sum());
        var result = TowerPracticalSearch.Assess(d, report, new string('a', 64));
        Assert.Equal(TowerPracticalSearch.ThreeReferenceVersion, result.Version);
        Assert.Equal("DemonstratedImprovement", result.StrengthDecision);
        Assert.Equal(3, result.Contrasts.Count);
        Assert.Equal(TowerBalanceEvaluator.Wilson(256, 256, 10), result.SelectedRate);
        Assert.Equal(4, result.Rates!.Count);
        Assert.All(result.Rates, r => Assert.Equal(TowerBalanceEvaluator.Wilson(r.Wins, 256, 10), r.Estimate));
        var export = TowerPracticalSearch.Export(d, report, result);
        Assert.Equal(4, export.Teams.Count); Assert.Equal(3, export.Teams.Count(t => t.Role == "Reference"));
        Assert.All(export.Teams, t => Assert.Empty(t.Scenario.Seeds));
        Assert.Contains("ten quantities", TowerPracticalSearch.Markdown(result, export));
        Assert.Contains("all three references", TowerPracticalSearch.Markdown(result, export));
    }

    [Fact]
    public async Task Passing_old_controls_alone_cannot_qualify_and_retains_every_reference()
    {
        var d = Definition(); var report = await Study(d, "third-blocks");
        var result = TowerPracticalSearch.Assess(d, report, new string('a', 64));
        Assert.All(result.Contrasts.Take(2), c => { Assert.True(c.ObservedGain >= .05); Assert.True(c.Lower > 0); });
        Assert.Equal(0, result.Contrasts[2].ObservedGain); Assert.True(result.Contrasts[2].Lower < 0);
        Assert.Equal("ImprovementNotDemonstrated", result.StrengthDecision);
        Assert.Equal(d.Starts.Select(s => s.Party.Id), result.RecommendedPartyIds);
    }

    [Fact]
    public async Task Selected_reference_is_measured_once_and_keeps_self_contrast_zero()
    {
        var d = Definition(); var report = await Study(d, "retain-third");
        Assert.Equal("Complete", report.Status); Assert.Equal(3, report.Confirmation!.Members.Count);
        Assert.Equal(1440, report.Accounting.Completed.Values.Sum());
        var result = TowerPracticalSearch.Assess(d, report, new string('a', 64));
        Assert.Equal("IncumbentRetained", result.StrengthDecision); Assert.Equal(d.Starts[2].Party.Id, result.SelectedPartyId);
        Assert.Equal(0, result.Contrasts[2].Gains); Assert.Equal(0, result.Contrasts[2].Losses);
        Assert.Equal(3, result.RecommendedPartyIds.Count);
    }

    [Theory]
    [InlineData("shortlist")]
    [InlineData("missing-control")]
    [InlineData("old-policy")]
    [InlineData("cap")]
    [InlineData("roots")]
    [InlineData("diagnostics")]
    [InlineData("ability-order")]
    public void Unsupported_shapes_fail_before_any_execution(string change)
    {
        var d = Definition();
        var invalid = change switch {
            "shortlist" => d with { Stages = d.Stages with { Shortlist = 4 } },
            "missing-control" => d with { References = d.References.Take(2).ToArray(), Starts = d.Starts.Take(2).ToArray() },
            "old-policy" => d with { Generation = d.Generation with { PolicyVersion = TowerSuppliedCompositionSearch.IncumbentVersion } },
            "cap" => d with { MaximumBattles = 1695 },
            "roots" => d with { Generation = d.Generation with { Seeds = [17, 19] } },
            "diagnostics" => d with { Stages = d.Stages with { DiagnosticCandidates = 1 } },
            _ => d with { Starts = d.Starts.Select((s, i) => i != 2 ? s : s with { Party = TowerPartySelection.Choice("bad",
                s.Party.Builds.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.Reverse().ToArray())) }).ToArray() }
        };
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.Prepare(invalid));
    }

    [Fact]
    public async Task Missing_confirmation_control_or_changed_outcome_cannot_publish_strength()
    {
        var d = Definition(); var report = await Study(d);
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.Assess(d, report with {
            Confirmation = report.Confirmation! with { Members = report.Confirmation.Members.Take(3).ToArray() } }, new string('a', 64)));
        var evidence = report.Evidence.ToArray(); evidence[0] = evidence[0] with { Trials = evidence[0].Trials.Skip(1).ToArray() };
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.Assess(d, report with { Evidence = evidence }, new string('a', 64)));
        var failed = TowerPracticalSearch.Assess(d, report with { Status = "Incomplete" }, new string('a', 64));
        Assert.Equal(TowerPracticalSearch.ThreeReferenceVersion, failed.Version); Assert.Empty(failed.RecommendedPartyIds);
    }

    [Theory]
    [InlineData("anchor-0")]
    [InlineData("anchor-1")]
    [InlineData("anchor-2")]
    public async Task Explicit_positive_tie_designation_can_name_any_of_the_three_references(string id)
    {
        var d = Definition(); var input = TowerBossImprovement.Inputs(d);
        var report = await Study(d);
        d = d with { Stages = d.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.IncumbentTieVersion, SelectionPrimaryReferenceId = id } };
        var rows = report.Discovery!.DiscoveryShortlist.Select(p => I.Measure(input with {
            DiscoverySeeds = d.Stages.Schedules.ToDictionary(s => s.Key, s => s.Value.Selection) }, p, 23)).ToArray();
        var chosen = TowerBossStudyPolicy.Select(d, F.Mechanics(input), report.Discovery.DiscoveryShortlist, rows);
        Assert.Equal(d.Starts.Single(s => s.ReferenceId == id).Party.Id, chosen.Single().Party.Id);
        Assert.Throws<InvalidDataException>(() => TowerBossStudyPolicy.Select(d, F.Mechanics(input), report.Discovery.DiscoveryShortlist.Take(4).ToArray(), rows.Take(4).ToArray()));
    }

    private (TowerPracticalRequest Q, TowerBossDiscoveryDefinition Template, TowerRefinementLiveHistory History) AllocationFixture(bool third = true, string? explorationVersion = null)
    {
        var d = explorationVersion is not null ? BalanceHarnessReferenceExplorationTests.Definition(policyVersion: explorationVersion)
            : third ? Definition() : I.Definition() with { ExcludedCombatSeeds = [-987] };
        var template = BalanceHarnessPracticalAllocationTests.Template(d);
        var source = Path.Combine(root, "template.json"); HarnessJson.WriteNew(source, template);
        var content = Path.Combine(root, "content"); Directory.CreateDirectory(content);
        var historyRoot = Path.Combine(root, "history"); Directory.CreateDirectory(historyRoot);
        var prior = Path.Combine(historyRoot, "prior-seed-ledger.json"); HarnessJson.WriteNew(prior, new { historical = new[] { -987 } });
        var pins = new Dictionary<string, string> { [prior] = HarnessJson.FileHash(prior) };
        var q = new TowerPracticalRequest(third ? TowerPracticalSearch.ThreeReferenceAllocationVersion : TowerPracticalSearch.AllocationVersion,
            content, source, HarnessJson.FileHash(source), historyRoot, Path.Combine(historyRoot, "run"), pins, 300, 64 * 1048576,
            3, 128, Allocation: new(19, "three-reference-fixture", 8, 32, 256));
        return (q, template, TowerRefinementComparisonLaunch.Refresh(historyRoot, q.OutputRoot, pins, [-987], default));
    }

    [Theory] [InlineData(null)] [InlineData(TowerReferenceExploration.Version)] [InlineData(TowerReferenceExploration.OffsetVersion)]
    public void Allocation_versions_cannot_be_mixed_and_new_domain_is_reconstructed(string? explorationVersion)
    {
        var (q, template, history) = AllocationFixture(explorationVersion: explorationVersion);
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.ValidateAllocationTemplate(q with { Version = TowerPracticalSearch.AllocationVersion }, template));
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.ValidateAllocationTemplate(q,
            BalanceHarnessPracticalAllocationTests.Template(I.Definition() with { ExcludedCombatSeeds = [-987] })));
        Directory.CreateDirectory(q.OutputRoot); HarnessJson.WriteNew(Path.Combine(q.OutputRoot, "request.json"), q);
        var bound = TowerPracticalSearch.AllocateAndRegister(q, new(template, history), default, () => { });
        Assert.Equal(Common.Randomness.StableRandom.Seed(q.Version, q.Allocation!.Domain, "19", "construction", "0"), bound.Definition.Generation.Seeds.Single());
        Assert.NotEqual(Common.Randomness.StableRandom.Seed(TowerPracticalSearch.AllocationVersion, q.Allocation.Domain, "19", "construction", "0"), bound.Definition.Generation.Seeds.Single());
        TowerPracticalSearch.VerifyAllocation(q.OutputRoot, q, bound.Definition, default);
        var receipt = HarnessJson.Read<TowerPracticalAllocationReceipt>(Path.Combine(q.OutputRoot, "allocation.json"));
        Assert.Equal(q.Version, receipt.Version);
        Assert.Equal(297, TowerPracticalSearch.Reserved(bound.Definition).Length);
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.VerifyAllocation(q.OutputRoot,
            q with { Version = TowerPracticalSearch.AllocationVersion }, bound.Definition, default));
    }

    [Theory] [InlineData(null)] [InlineData(TowerReferenceExploration.Version)] [InlineData(TowerReferenceExploration.OffsetVersion)]
    public async Task Full_allocated_worker_publication_reconstructs_and_rejects_changed_request_version(string? explorationVersion)
    {
        var (q, template, history) = AllocationFixture(explorationVersion: explorationVersion); Directory.CreateDirectory(q.OutputRoot);
        var started = DateTimeOffset.UtcNow; var launch = new TowerPracticalLaunch(HarnessJson.Hash(q), started, started.AddSeconds(q.MaximumSeconds - q.PriorSeconds));
        HarnessJson.WriteNew(Path.Combine(q.OutputRoot, "request.json"), q); HarnessJson.WriteNew(Path.Combine(q.OutputRoot, "launch.json"), launch);
        BossStudyReport? saved = null;
        var result = await TowerPracticalSearch.RunOperation(q, launch, _ => new(template, history), async (d, output, attempt, _) => {
            Directory.CreateDirectory(output); saved = await Study(d, attempt: attempt);
            HarnessJson.WriteNew(Path.Combine(output, "definition.json"), d); HarnessJson.WriteNew(Path.Combine(output, "study.json"), saved);
            HarnessJson.WriteNew(Path.Combine(output, "boss-profiles.json"), new TowerBossInventoryReport(1, new Dictionary<string, string>(), [],
                F.Mechanics(TowerBossImprovement.Inputs(d)).Essences, [], [], [], [], ["Literal fixture"]));
            HarnessJson.WriteNew(Path.Combine(output, "files.json"), Directory.GetFiles(output).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
            return saved;
        }, (_, _) => Task.FromResult(saved!), default, allocationCandidate: FixtureHost.AllocationCandidate);
        Assert.Equal("Verified", result.IntegrityStatus);
        HarnessJson.WriteNew(Path.Combine(q.OutputRoot, "worker-result.json"), result);
        var published = TowerPracticalSearch.Publish(q, launch, () => 10, default, allocationCandidate: FixtureHost.AllocationCandidate);
        Assert.Equal("Verified", published.IntegrityStatus);
        var verified = await TowerPracticalSearch.VerifyPublication(q.OutputRoot, (_, _) => Task.FromResult(saved!), allocationCandidate: FixtureHost.AllocationCandidate);
        Assert.Equal(Json(published), Json(verified));
        File.WriteAllText(Path.Combine(q.OutputRoot, "request.json"), Json(q with { Version = TowerPracticalSearch.AllocationVersion }));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.VerifyPublication(q.OutputRoot, (_, _) => Task.FromResult(saved!)));
    }

    private (string Request, string Bundle, string Pin, TowerPracticalRequest Q, TowerBossDiscoveryDefinition Source) PresetFixture(int cap = 1696)
    {
        var (q, source, _) = AllocationFixture(false);
        var settings = new TowerSettings(new ThreatAndTankingOptions(), 10);
        source = source with { MaximumBattles = cap, SettingsHash = HarnessJson.Hash(settings) };
        File.WriteAllText(q.DefinitionPath, Json(source)); q = q with { DefinitionHash = HarnessJson.FileHash(q.DefinitionPath) };
        var request = Path.Combine(root, "request.json"); HarnessJson.WriteNew(request, q);
        var third = Definition().References[2]; var pid = Definition().Starts[2].Party.Id;
        var ids = source.Starts.Select(s => s.Party.Id).ToArray(); var bundle = Path.Combine(root, "reuse"); Directory.CreateDirectory(bundle);
        HarnessJson.WriteNew(Path.Combine(bundle, "teams.json"), new { version = "tower-confirmed-team-reuse-v1", selectedPartyId = pid, controlPartyIds = ids,
            teams = source.References.Select((r, i) => new { partyId = ids[i], scenario = r.Scenario, qualifies = false, control = true })
                .Append(new { partyId = pid, scenario = third.Scenario, qualifies = true, control = false }).ToArray() });
        HarnessJson.WriteNew(Path.Combine(bundle, "scope.json"), new LoadoutScope("fixture", settings, ExecutionIdentity.Current(), source.ContentHashes, "gzip-json-v1"));
        HarnessJson.WriteNew(Path.Combine(bundle, "reuse.json"), new { version = "tower-confirmed-team-reuse-v1", status = "ReadyForExplicitReuse", selectedPartyId = pid, controlPartyIds = ids });
        HarnessJson.WriteNew(Path.Combine(bundle, "files.json"), Directory.GetFiles(bundle).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
        return (request, bundle, HarnessJson.FileHash(Path.Combine(bundle, "files.json")), q, source);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pinned_reuse_bundle_converts_to_a_real_allocatable_three_reference_request(bool designated)
    {
        var i = PresetFixture(); var output = Path.Combine(root, "preset");
        if (designated)
        {
            var source = TowerPracticalSearch.ApplyIncumbentTiePreset(i.Q, i.Source, i.Source.References[0].Id);
            File.WriteAllText(i.Q.DefinitionPath, Json(source));
            var qSource = i.Q with { DefinitionHash = HarnessJson.FileHash(i.Q.DefinitionPath) };
            File.WriteAllText(i.Request, Json(qSource));
            i = (i.Request, i.Bundle, i.Pin, qSource, source);
        }
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-practical-search-three-reference-preset", i.Request, i.Bundle, i.Pin, output]));
        var q = HarnessJson.Read<TowerPracticalRequest>(Path.Combine(output, "request.json")); var d = TowerBossDiscovery.Read(q.DefinitionPath);
        Assert.Equal(TowerPracticalSearch.ThreeReferenceAllocationVersion, q.Version); Assert.Equal(3, d.Starts.Count); Assert.Equal(5, d.Stages.Shortlist);
        Assert.Equal(TowerSuppliedCompositionSearch.ThreeReferenceVersion, d.Generation.PolicyVersion);
        Assert.Equal(Json(i.Source.References), Json(d.References.Take(2).ToArray()));
        Assert.Equal(i.Source.Stages.SelectionPrimaryReferenceId, d.Stages.SelectionPrimaryReferenceId);
        Assert.Equal(i.Source.Stages.SelectionPolicyVersion, d.Stages.SelectionPolicyVersion);
        Assert.Equal(i.Source.MaximumBattles, d.MaximumBattles); Assert.Equal(i.Q.Allocation, q.Allocation);
        Assert.Equal(i.Q.PriorSeconds, q.PriorSeconds); Assert.Equal(i.Q.MaximumBytes, q.MaximumBytes);
        Assert.Equal(i.Q.DefinitionHash, HarnessJson.FileHash(i.Q.DefinitionPath)); Assert.False(Path.Exists(q.OutputRoot));
        Assert.Equal(1696, TowerBossDiscovery.Validate(TowerPracticalSearch.ValidateAllocationTemplate(q, d)).Total);
        Assert.Equal(new[] { "preset.json", "request.json", "template.json" }, Directory.GetFiles(output).Select(Path.GetFileName).Order());
    }

    [Theory]
    [InlineData("settings")]
    [InlineData("content")]
    [InlineData("control")]
    [InlineData("selected-control")]
    [InlineData("unqualified")]
    [InlineData("recipe")]
    [InlineData("seeded")]
    public void Pinned_but_semantically_incompatible_reuse_cannot_be_imported(string invalid)
    {
        var i = PresetFixture();
        var bundle = JsonNode.Parse(File.ReadAllText(Path.Combine(i.Bundle, "teams.json")))!;
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(i.Bundle, "scope.json"));
        var source = invalid == "settings" ? i.Source with { SettingsHash = new string('f', 64) } : i.Source;
        if (invalid == "content") scope = scope with { ContentHashes = new Dictionary<string, string> { ["different.json"] = new string('f', 64) } };
        if (invalid == "control") bundle["teams"]![0]!["control"] = false;
        if (invalid == "selected-control") bundle["selectedPartyId"] = i.Source.Starts[0].Party.Id;
        if (invalid == "unqualified") bundle["teams"]![2]!["qualifies"] = false;
        if (invalid == "recipe") bundle["teams"]![2]!["scenario"]!["party"]![0]!["build"]!["essenceIds"]![0] = "changed-essence";
        if (invalid == "seeded") bundle["teams"]![2]!["scenario"]!["seeds"] = new JsonArray(99);
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.ApplyThreeReferencePreset(i.Q, source,
            JsonSerializer.SerializeToElement(bundle, HarnessJson.Options), scope, i.Pin));
        Assert.False(Path.Exists(i.Q.OutputRoot));
    }

    [Theory]
    [InlineData("changed-bundle")]
    [InlineData("changed-source")]
    [InlineData("small-budget")]
    [InlineData("existing-output")]
    [InlineData("cancelled")]
    public void Preset_rejects_unbound_or_unfunded_changes_without_publishing(string invalid)
    {
        var i = PresetFixture(invalid == "small-budget" ? 1408 : 1696); var output = Path.Combine(root, "preset");
        if (invalid == "changed-bundle") File.AppendAllText(Path.Combine(i.Bundle, "teams.json"), " ");
        if (invalid == "changed-source") File.AppendAllText(i.Q.DefinitionPath, " ");
        if (invalid == "existing-output") Directory.CreateDirectory(output);
        using var stop = new CancellationTokenSource(); if (invalid == "cancelled") stop.Cancel();
        Assert.ThrowsAny<Exception>(() => TowerPracticalSearch.CreateThreeReferencePreset(i.Request, i.Bundle, i.Pin, output, stop.Token));
        Assert.False(File.Exists(Path.Combine(output, "preset.json"))); Assert.False(Path.Exists(i.Q.OutputRoot));
    }
}
