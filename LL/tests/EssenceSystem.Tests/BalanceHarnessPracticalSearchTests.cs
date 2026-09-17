using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessPracticalSearchTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-practical-fixture-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Practical fixture entered combat.")).Activate();
    public BalanceHarnessPracticalSearchTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private static string Json(object value) => JsonSerializer.Serialize(value, HarnessJson.Options);
    private sealed record Outcomes(string Source, string SourceSha256, string Selected, string[] Anchors);
    private static Outcomes Pilot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "LL/tests/EssenceSystem.Tests/Fixtures/tower-practical-pilot-outcomes.json");
            if (File.Exists(path)) return HarnessJson.Read<Outcomes>(path);
        }
        throw new FileNotFoundException("Practical arithmetic fixture.");
    }

    // Literal in-memory reports exercise the real unchanged search/study state machine, never gameplay preparation or combat.
    internal static async Task<BossStudyReport> Study(TowerBossDiscoveryDefinition d, string outcomes = "pilot", Action<bool>? attempt = null,
        Func<BossConfirmationFreeze, CancellationToken, Task>? beforeConfirmation = null, CancellationToken token = default)
    {
        var input = TowerBossImprovement.Inputs(d); var fixture = Pilot(); var ordinal = 0;
        return await TowerBossStudy.ExecuteAsync(d, F.Mechanics(input), (_, stage, scenario, seed, _) => {
            var party = TowerPartySelection.Choice("fixture", scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds));
            var reference = d.Starts.Select((s, i) => (s, i)).SingleOrDefault(p => p.s.Party.Id == party.Id);
            var incumbent = reference.s is not null;
            var panel = d.Stages.Schedules.Single().Value;
            var index = (stage == "discovery" ? panel.Discovery : stage == "selection" ? panel.Selection : panel.Confirmation).ToList().IndexOf(seed);
            var won = stage switch {
                "discovery" => index < (incumbent ? 3 : 5),
                "selection" => index < ((outcomes == "incumbent" ? incumbent : !incumbent) ? 26 : 23),
                _ => outcomes == "improved" ? (!incumbent || index < 100)
                    : (incumbent ? fixture.Anchors[reference.i] : fixture.Selected)[index] == '1'
            };
            var outcome = won ? BattleOutcome.Victory : BattleOutcome.Defeat;
            var summary = new BattleSummary(outcome, outcome, "Literal fixture", 1, 1,
                [new SimpleCombatEntity("f", "f", "", 10, 0)], [], [], new CompactCombatTelemetry());
            var report = new TowerBattleReport(new BattleReport(1, scenario.Id, seed, 1,
                JsonSerializer.SerializeToElement(new { fixture = true }), summary, null), won,
                Convert.ToInt32(party.Id[..4], 16) / 655.35m, 1);
            var trial = new LoadoutTrial($"fixture-{++ordinal:D6}", stage, HarnessJson.Hash(scenario), seed, new string('a', 64), new string('b', 64));
            return Task.FromResult((trial, report));
        }, (_, _, _) => throw new InvalidOperationException("No replay"), (_, _) => { }, token, attempt: attempt, beforeConfirmation: beforeConfirmation);
    }

    private (TowerPracticalRequest Request, TowerPracticalInputs Inputs) Request()
    {
        var ledger = Path.Combine(root, "prior-seed-ledger.json"); HarnessJson.WriteNew(ledger, new { historical = new[] { -987 } });
        var d = I.Definition() with { ExcludedCombatSeeds = [-987] };
        var definition = Path.Combine(root, "input.json"); HarnessJson.WriteNew(definition, d);
        var content = Path.Combine(root, "content"); Directory.CreateDirectory(content);
        var files = new Dictionary<string, string> { [ledger] = HarnessJson.FileHash(ledger) };
        var q = new TowerPracticalRequest(TowerPracticalSearch.Version, content, definition, HarnessJson.FileHash(definition),
            root, Path.Combine(root, "result"), files, 300, 32 * 1048576);
        return (q, new(d, new(files, [-987])));
    }

    // Exercise the real operation, publication and audit boundaries with a fabricated study archive.
    // The injected verifier below substitutes only for native gameplay input reconstruction.
    private async Task<(TowerPracticalRequest Request, TowerPracticalLaunch Launch, BossStudyReport Report)> CompletedWorker(string outcomes = "pilot")
    {
        var (q, input) = Request(); q = q with { PriorSeconds = 3, PriorBytes = 128 };
        Directory.CreateDirectory(q.OutputRoot);
        var started = DateTimeOffset.UtcNow;
        var launch = new TowerPracticalLaunch(HarnessJson.Hash(q), started, started.AddSeconds(q.MaximumSeconds - q.PriorSeconds));
        HarnessJson.WriteNew(Path.Combine(q.OutputRoot, "request.json"), q);
        HarnessJson.WriteNew(Path.Combine(q.OutputRoot, "launch.json"), launch);
        BossStudyReport? saved = null;
        var result = await TowerPracticalSearch.RunOperation(q, launch, _ => input, async (d, output, attempt, _) => {
            Directory.CreateDirectory(output);
            saved = await Study(d, outcomes, attempt);
            HarnessJson.WriteNew(Path.Combine(output, "definition.json"), d);
            HarnessJson.WriteNew(Path.Combine(output, "study.json"), saved);
            HarnessJson.WriteNew(Path.Combine(output, "boss-profiles.json"), new TowerBossInventoryReport(1,
                new Dictionary<string, string>(), [], F.Mechanics(TowerBossImprovement.Inputs(d)).Essences, [], [], [], [], ["Synthetic fixture"]));
            Rehash(output);
            return saved;
        }, (_, _) => Task.FromResult(saved!), default);
        Assert.Equal("Verified", result.IntegrityStatus);
        HarnessJson.WriteNew(Path.Combine(q.OutputRoot, "worker-result.json"), result);
        return (q, launch, saved!);
    }

    private static void Rehash(string output) => File.WriteAllText(Path.Combine(output, "files.json"), Json(
        Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories)
            .Where(p => p != Path.Combine(output, "files.json"))
            .ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash)));

    private static void EditJson(string path, string property, object value)
    {
        var document = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        document[property] = JsonSerializer.SerializeToNode(value, HarnessJson.Options);
        File.WriteAllText(path, document.ToJsonString(HarnessJson.Options));
    }

    [Fact]
    public async Task Entry_contract_preserves_the_exact_kernel_and_incumbent_nomination()
    {
        var d = I.Definition(); var prepared = TowerPracticalSearch.Prepare(d); Assert.Equal(Json(d), Json(prepared));
        var input = TowerBossImprovement.Inputs(d);
        Task<BossGenerationResult> Run(TowerBossDiscoveryDefinition definition) => TowerBossImprovement.ExecuteAsync(definition,
            TowerBossImprovement.Inputs(definition), F.Mechanics(input),
            (party, _, _) => Task.FromResult(I.Measure(input, party, 0, Convert.ToUInt32(party.Id[..6], 16) / (double)0xffffff * 100)), default);
        var before = await Run(d); var after = await Run(prepared);
        Assert.Equal("Complete", after.Status); Assert.Equal(Json(before), Json(after));
        Assert.Equal(4, after.DiscoveryShortlist.Count);
        Assert.All(d.Starts, s => Assert.Contains(after.DiscoveryShortlist, p => p.Id == s.Party.Id));
        Assert.All(after.Arms.Single().Proposals.Where(p => p.Party is not null),
            p => Assert.All(p.Party!.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids))));
    }

    [Theory]
    [InlineData("old-policy")] [InlineData("reused-root")] [InlineData("shared-root")] [InlineData("short-confirmation")]
    public void Unsupported_or_reused_schedules_fail_before_execution(string change)
    {
        var d = I.Definition(); var panel = d.Stages.Schedules.Single();
        d = change switch {
            "old-policy" => d with { Generation = d.Generation with { PolicyVersion = TowerSuppliedCompositionSearch.StandaloneVersion } },
            "reused-root" => d with { ExcludedCombatSeeds = d.Generation.Seeds },
            "shared-root" => d with { Generation = d.Generation with { Seeds = [panel.Value.Discovery[0]] } },
            _ => d with { Stages = d.Stages with { Schedules = new Dictionary<string, BossDiscoverySchedule> {
                [panel.Key] = panel.Value with { Confirmation = panel.Value.Confirmation.Take(32).ToArray() } } } }
        };
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.Prepare(d));
    }

    [Fact]
    public async Task Retained_pilot_outcomes_reproduce_the_failed_gate_and_preserve_both_anchors()
    {
        var outcomes = Pilot();
        Assert.All(outcomes.Anchors.Append(outcomes.Selected), row => Assert.Equal(256, row.Length));
        Assert.Equal(167, outcomes.Selected.Count(c => c == '1'));
        Assert.All(outcomes.Anchors, row => Assert.Equal(161, row.Count(c => c == '1')));
        var d = I.Definition(); var report = await Study(d); Assert.Equal("Complete", report.Status);
        var result = TowerPracticalSearch.Assess(d, report, new string('a', 64));
        Assert.Equal("ImprovementNotDemonstrated", result.StrengthDecision); Assert.Equal("Verified", result.IntegrityStatus);
        Assert.Equal(167 / 256d, result.SelectedRate!.Rate); Assert.Equal(GoalOutcome.Fail, result.BalanceAssessment);
        Assert.Equal(d.Starts.Select(s => s.Party.Id), result.RecommendedPartyIds);
        Assert.All(result.Contrasts, c => Assert.Equal(6 / 256d, c.ObservedGain));
        Assert.Contains(result.Contrasts, c => c.Gains == 66 && c.Losses == 60 && Math.Abs(c.Lower - -.120654) < .000001);
        Assert.Contains(result.Contrasts, c => c.Gains == 54 && c.Losses == 48 && Math.Abs(c.Lower - -.110599) < .000001);
        var export = TowerPracticalSearch.Export(d, report, result);
        Assert.Equal(3, export.Teams.Count); Assert.Equal(2, export.Teams.Count(t => t.Recommended));
        Assert.All(export.Teams, t => { Assert.Empty(t.Scenario.Seeds); Assert.Equal(d.RequiredPartySize * d.Budget.EssenceSlots, t.RequiredCopies.Values.Sum()); });
        Assert.NotEmpty(result.SelectedReferenceAncestry);
        var markdown = TowerBossStudy.Markdown(report, d);
        Assert.DoesNotContain("References never enter", markdown); Assert.DoesNotContain("Independent generated viability", markdown);
        Assert.Contains("Positive-win ties use frozen discovery rank", markdown);
        Assert.Contains("Reference-derived search viability", markdown);
        Assert.Contains("Independent generated viability", TowerBossStudy.ArchivedMarkdown(report, d, null));
        Assert.Equal(markdown, TowerBossStudy.ArchivedMarkdown(report, d, TowerBossStudy.ReportFormat));
        Assert.Throws<InvalidDataException>(() => TowerBossStudy.ArchivedMarkdown(report, d, "unknown-format"));
    }

    [Theory]
    [InlineData("improved", "DemonstratedImprovement", 1)] [InlineData("incumbent", "IncumbentRetained", 2)]
    public async Task Improvement_and_retained_incumbent_have_distinct_decisions(string outcomes, string expected, int recommended)
    {
        var d = I.Definition(); var report = await Study(d, outcomes);
        var result = TowerPracticalSearch.Assess(d, report, new string('a', 64));
        Assert.Equal(expected, result.StrengthDecision); Assert.Equal(recommended, result.RecommendedPartyIds.Count);
        Assert.Equal(GoalOutcome.Fail, result.BalanceAssessment); // Successful strength can breach encounter balance.
    }

    [Theory]
    [InlineData("Incomplete")] [InlineData("Cancelled")] [InlineData("Invalid")]
    public async Task Noncomplete_execution_cannot_promote_or_export(string status)
    {
        var d = I.Definition(); var report = await Study(d);
        var result = TowerPracticalSearch.Assess(d, report with { Status = status }, new string('a', 64));
        Assert.NotEqual("Verified", result.IntegrityStatus); Assert.Empty(result.RecommendedPartyIds);
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.Export(d, report, result));
    }

    [Theory]
    [InlineData("missing")] [InlineData("reordered")] [InlineData("unknown")]
    public async Task Malformed_paired_outcomes_cannot_establish_improvement(string kind)
    {
        var d = I.Definition(); var report = await Study(d); var rows = report.Evidence.ToArray(); var row = rows[0];
        rows[0] = row with { Trials = kind == "missing" ? row.Trials.Skip(1).ToArray() : kind == "reordered"
            ? row.Trials.Reverse().ToArray() : row.Trials.Select((r, i) => i == 0 ? r with { Outcome = (BattleOutcome)999 } : r).ToArray() };
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.Assess(d, report with { Evidence = rows }, new string('a', 64)));
    }

    [Fact]
    public void Reservation_is_visible_before_execution_and_preserves_all_declared_values()
    {
        var (q, input) = Request(); Directory.CreateDirectory(q.OutputRoot);
        TowerPracticalSearch.Register(q, input, default, boundary => {
            if (boundary == "pending") Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(
                HarnessJson.Read<JsonElement>(Path.Combine(q.OutputRoot, "history-input.json"))));
        });
        var values = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(q.OutputRoot, "seed-ledger.json")));
        Assert.Equal(input.History.Values.Concat(TowerPracticalSearch.Reserved(input.Definition)).Order(), values);
        Assert.Throws<IOException>(() => TowerPracticalSearch.Register(q, input, default));
    }

    [Fact]
    public void Already_cancelled_registration_creates_no_reservation()
    {
        var (q, input) = Request(); Directory.CreateDirectory(q.OutputRoot);
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.Throws<OperationCanceledException>(() => TowerPracticalSearch.Register(q, input, stop.Token));
        Assert.Empty(Directory.EnumerateFileSystemEntries(q.OutputRoot));
        var history = TowerRefinementComparisonLaunch.Refresh(root, q.OutputRoot, q.RequiredHistory, input.History.Values, default);
        Assert.Equal(input.History.Values, history.Values);
    }

    [Theory]
    [InlineData("pending")] [InlineData("before-complete")]
    public void Cancellation_after_reservation_preserves_every_declared_value_in_Pending(string cancellationBoundary)
    {
        var (q, input) = Request(); Directory.CreateDirectory(q.OutputRoot);
        using var stop = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() => TowerPracticalSearch.Register(q, input, stop.Token, boundary => {
            if (boundary == cancellationBoundary) stop.Cancel();
        }));
        var pendingPath = Path.Combine(q.OutputRoot, "history-input.json");
        var pending = HarnessJson.Read<JsonElement>(pendingPath);
        Assert.Equal("Pending", pending.GetProperty("reservationState").GetString());
        Assert.Equal(TowerPracticalSearch.Reserved(input.Definition), pending.GetProperty("reserved").EnumerateArray().Select(v => v.GetInt32()));
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(pending));
        Assert.False(File.Exists(Path.Combine(q.OutputRoot, "attempts.jsonl")));
        Assert.Throws<IOException>(() => TowerPracticalSearch.Register(q, input, default));
        Assert.Equal(Json(pending), Json(HarnessJson.Read<JsonElement>(pendingPath)));
    }

    [Theory]
    [InlineData("changed")] [InlineData("new-ledger")] [InlineData("cancelled")]
    public void History_changes_or_interruption_leave_Pending_and_forbid_reuse(string kind)
    {
        var (q, input) = Request(); Directory.CreateDirectory(q.OutputRoot); using var stop = new CancellationTokenSource();
        Assert.ThrowsAny<Exception>(() => TowerPracticalSearch.Register(q, input, stop.Token, boundary => {
            if (boundary != "pending") return;
            if (kind == "cancelled") stop.Cancel();
            else if (kind == "changed") File.AppendAllText(input.History.Files.Keys.Single(), " ");
            else { Directory.CreateDirectory(Path.Combine(root, "other")); HarnessJson.WriteNew(Path.Combine(root, "other", "history-input.json"), new { reserved = new[] { -986 } }); }
        }));
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(q.OutputRoot, "history-input.json"))));
    }

    [Fact]
    public void Complete_registry_detects_new_ledgers_and_unrecovered_Pending_sources()
    {
        var (q, input) = Request();
        var refreshed = TowerRefinementComparisonLaunch.Refresh(root, q.OutputRoot, q.RequiredHistory, input.History.Values, default);
        Assert.Equal(input.History.Values, refreshed.Values);
        Directory.CreateDirectory(Path.Combine(root, "other")); var pending = Path.Combine(root, "other", "history-input.json");
        HarnessJson.WriteNew(pending, new { reservationState = "Pending", reserved = new[] { -986 } });
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Refresh(root, q.OutputRoot, q.RequiredHistory, input.History.Values, default));
    }

    [Fact]
    public void Durable_failed_attempt_is_never_retried_or_refunded()
    {
        var path = Path.Combine(root, "attempts.jsonl");
        using (var counter = new TowerPracticalSearch.Attempts(path, 1, () => { }))
        {
            counter.Event(false);
            Assert.Throws<InvalidDataException>(() => counter.Event(false));
            Assert.Equal(1, counter.Started); Assert.Equal(0, counter.Completed);
        }
        Assert.Single(File.ReadAllLines(path)); Assert.Contains("Started", File.ReadAllText(path));
        Assert.Throws<IOException>(() => new TowerPracticalSearch.Attempts(path, 1, () => { }));
    }

    [Fact]
    public async Task Preparation_failure_is_durably_charged_before_the_engine_can_start()
    {
        var d = I.Definition(); var input = TowerBossImprovement.Inputs(d); var path = Path.Combine(root, "attempts.jsonl");
        using var counter = new TowerPracticalSearch.Attempts(path, 1, () => { });
        var report = await TowerBossStudy.ExecuteAsync(d, F.Mechanics(input), (_, _, _, _, _) =>
            throw new InvalidDataException("Fixture preparation failure before combat"),
            (_, _, _) => throw new InvalidOperationException("No replay"), (_, _) => { }, default, attempt: counter.Event);
        Assert.Equal("Invalid", report.Status);
        Assert.Equal(1, report.Accounting.Attempted.Values.Sum()); Assert.Equal(0, report.Accounting.Completed.Values.Sum());
        Assert.Equal(1, counter.Started); Assert.Equal(0, counter.Completed);
        counter.Dispose();
        Assert.Contains("Started", Assert.Single(File.ReadAllLines(path)));
    }

    [Fact]
    public void Request_rejects_overlap_missing_recovery_pins_and_exhausted_prior_costs()
    {
        var (q, _) = Request(); TowerPracticalSearch.ValidateRequest(q);
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.ValidateRequest(q with { OutputRoot = Path.Combine(q.ContentRoot, "result") }));
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.ValidateRequest(q with { PriorSeconds = q.MaximumSeconds }));
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.ValidateRequest(q with { PriorBytes = q.MaximumBytes }));
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.ValidateRequest(q with {
            PendingHistoryRecoveries = new Dictionary<string, string> { [q.RequiredHistory.Keys.Single()] = Path.Combine(root, "recovery.json") } }));
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.Inspect(q with { DefinitionHash = new string('0', 64) }, default));
        Assert.False(Directory.Exists(q.OutputRoot));
    }

    [Fact]
    public void Cumulative_envelope_charges_prior_preparation_and_verification_without_reset()
    {
        var (q, _) = Request(); q = q with { MaximumSeconds = 10, PriorSeconds = 4, PriorBytes = 10 * 1048576 };
        TowerPracticalSearch.CheckEnvelope(q, 3, 1048576);
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.CheckEnvelope(q, 4, 1048576));
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.CheckEnvelope(q, 0, q.MaximumBytes - q.PriorBytes));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Whole_operation_requires_matching_verified_evidence_before_producing_teams(bool tampered)
    {
        var (q, input) = Request(); Directory.CreateDirectory(q.OutputRoot); BossStudyReport? saved = null; var verified = false;
        var launch = new TowerPracticalLaunch(HarnessJson.Hash(q), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5));
        var operation = TowerPracticalSearch.RunOperation(q, launch, _ => input, async (d, output, attempt, _) => {
            Directory.CreateDirectory(output); HarnessJson.WriteNew(Path.Combine(output, "files.json"), new { fixture = true });
            return saved = await Study(d, attempt: attempt);
        }, (_, _) => { verified = true; return Task.FromResult(tampered ? saved! with { Error = "changed" } : saved!); }, default);
        if (tampered) { await Assert.ThrowsAsync<InvalidDataException>(() => operation); Assert.False(File.Exists(Path.Combine(q.OutputRoot, "proposed-teams.json"))); }
        else { var result = await operation; Assert.Equal("Verified", result.IntegrityStatus); Assert.True(File.Exists(Path.Combine(q.OutputRoot, "proposed-teams.json"))); }
        Assert.True(verified);
        Assert.Equal(1408 * 2, File.ReadLines(Path.Combine(q.OutputRoot, "attempts.jsonl")).Count());
        TowerPracticalSearch.VerifyAttempts(Path.Combine(q.OutputRoot, "attempts.jsonl"), saved!);
        File.AppendAllText(Path.Combine(q.OutputRoot, "attempts.jsonl"), "{\"kind\":\"Started\",\"ordinal\":1409}\n");
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.VerifyAttempts(Path.Combine(q.OutputRoot, "attempts.jsonl"), saved!));
    }

    [Fact]
    public async Task Incomplete_study_is_not_verified_and_keeps_its_stop_status()
    {
        var (q, input) = Request(); Directory.CreateDirectory(q.OutputRoot);
        var launch = new TowerPracticalLaunch(HarnessJson.Hash(q), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5));
        var result = await TowerPracticalSearch.RunOperation(q, launch, _ => input, async (d, _, attempt, _) =>
            (await Study(d, attempt: attempt)) with { Status = "Incomplete", Error = "ProposalExhausted" },
            (_, _) => throw new InvalidOperationException("Incomplete evidence must not reach the verifier."), default);
        Assert.Equal("Incomplete", result.ExecutionStatus); Assert.Equal("IncompleteEvidence", result.StrengthDecision);
        Assert.Equal("ProposalExhausted", result.StopReason); Assert.Empty(result.RecommendedPartyIds);
        Assert.False(File.Exists(Path.Combine(q.OutputRoot, "proposed-teams.json")));
    }

    [Fact]
    public async Task Tampered_or_missing_publication_is_rejected_before_native_verification()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.Verify(root));
        HarnessJson.WriteNew(Path.Combine(root, "completion.json"), new { status = "Complete" });
        HarnessJson.WriteNew(Path.Combine(root, "teams.json"), new { fixture = true });
        HarnessJson.WriteNew(Path.Combine(root, "files.json"), Directory.EnumerateFiles(root).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
        File.AppendAllText(Path.Combine(root, "teams.json"), " ");
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.Verify(root));
    }

    [Theory]
    [InlineData("pilot", "ImprovementNotDemonstrated", 2)]
    [InlineData("improved", "DemonstratedImprovement", 1)]
    [InlineData("incumbent", "IncumbentRetained", 2)]
    public async Task Complete_publication_and_audit_agree_without_requiring_original_live_inputs(string outcomes, string decision, int recommendations)
    {
        var (q, launch, report) = await CompletedWorker(outcomes);
        var result = TowerPracticalSearch.Publish(q, launch, () => 1, default);
        Assert.Equal("Verified", result.IntegrityStatus); Assert.Equal(decision, result.StrengthDecision);
        Assert.Equal(GoalOutcome.Fail, result.BalanceAssessment);
        Assert.Equal(recommendations, result.RecommendedPartyIds.Count);
        TowerBulkCampaign.VerifyFiles(q.OutputRoot, "files.json", true, default);
        var teams = HarnessJson.Read<TowerPracticalTeams>(Path.Combine(q.OutputRoot, "teams.json"));
        Assert.All(teams.Teams, t => Assert.Empty(t.Scenario.Seeds));
        Assert.Equal(recommendations, teams.Teams.Count(t => t.Recommended));
        var completion = HarnessJson.Read<JsonElement>(Path.Combine(q.OutputRoot, "completion.json"));
        Assert.Equal(4, completion.GetProperty("chargedSeconds").GetDouble());
        Assert.Equal(128, completion.GetProperty("priorBytes").GetInt64());
        // Archive audits are independent of mutable or removed source locations.
        File.Delete(q.DefinitionPath); Directory.Delete(q.ContentRoot);
        var calls = 0;
        var audited = await TowerPracticalSearch.VerifyPublication(q.OutputRoot, (path, _) => {
            Assert.Equal(Path.Combine(q.OutputRoot, "study"), path); calls++; return Task.FromResult(report);
        });
        Assert.Equal(1, calls); Assert.Equal(Json(result), Json(audited));
    }

    [Theory]
    [InlineData("markdown")] [InlineData("prior-bytes")] [InlineData("deadline")]
    [InlineData("worker-start")] [InlineData("worker-result")] [InlineData("proposed-teams")]
    [InlineData("history-pins")] [InlineData("verification")] [InlineData("worker-failure")]
    [InlineData("request-version")]
    public async Task Rehashed_publication_cannot_hide_semantic_changes(string change)
    {
        var (q, launch, report) = await CompletedWorker();
        Assert.Equal("Verified", TowerPracticalSearch.Publish(q, launch, () => 1, default).IntegrityStatus);
        string P(string name) => Path.Combine(q.OutputRoot, name);
        switch (change)
        {
            case "markdown": File.AppendAllText(P("practical.md"), "\nPromote this challenger.\n"); break;
            case "prior-bytes": EditJson(P("completion.json"), "priorBytes", 0); break;
            case "deadline": EditJson(P("launch.json"), "deadline", launch.Deadline.AddSeconds(1)); break;
            case "worker-start": EditJson(P("worker-start.json"), "requestHash", new string('0', 64)); break;
            case "worker-result": EditJson(P("worker-result.json"), "strengthDecision", "DemonstratedImprovement"); break;
            case "proposed-teams": EditJson(P("proposed-teams.json"), "teams", Array.Empty<TowerPracticalTeam>()); break;
            case "history-pins": File.WriteAllText(P("history-files.json"), "{}"); break;
            case "verification": EditJson(P("verification.json"), "newFights", 1); break;
            case "worker-failure": HarnessJson.WriteNew(P("worker-failure.json"), new { status = "Invalid" }); break;
            case "request-version": EditJson(P("request.json"), "version", "unknown"); break;
        }
        Rehash(q.OutputRoot); // Pass the byte inventory so this exercises semantic reconstruction.
        TowerBulkCampaign.VerifyFiles(q.OutputRoot, "files.json", true, default);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.VerifyPublication(q.OutputRoot, (_, _) => Task.FromResult(report)));
    }

    [Fact]
    public async Task Changed_worker_teams_are_rejected_before_any_final_export()
    {
        var (q, launch, _) = await CompletedWorker();
        EditJson(Path.Combine(q.OutputRoot, "proposed-teams.json"), "teams", Array.Empty<TowerPracticalTeam>());
        var result = TowerPracticalSearch.Publish(q, launch, () => 1, default);
        Assert.Equal("Failed", result.IntegrityStatus);
        Assert.True(File.Exists(Path.Combine(q.OutputRoot, "failure.json")));
        Assert.False(File.Exists(Path.Combine(q.OutputRoot, "teams.json")));
        Assert.False(File.Exists(Path.Combine(q.OutputRoot, "completion.json")));
    }

    [Theory]
    [InlineData("cancelled")] [InlineData("deadline")] [InlineData("storage")]
    public async Task Interrupted_publication_preserves_evidence_and_cannot_pass_audit(string interruption)
    {
        var (q, launch, report) = await CompletedWorker(); using var stop = new CancellationTokenSource();
        var elapsed = 1d;
        var result = TowerPracticalSearch.Publish(q, launch, () => elapsed, stop.Token, boundary => {
            if (boundary != "after-inventory") return;
            if (interruption == "cancelled") stop.Cancel();
            else if (interruption == "deadline") elapsed = q.MaximumSeconds - q.PriorSeconds;
            else using (var file = new FileStream(Path.Combine(q.OutputRoot, "overflow.bin"), FileMode.CreateNew)) file.SetLength(q.MaximumBytes);
        });
        Assert.Equal("Failed", result.IntegrityStatus); Assert.Empty(result.RecommendedPartyIds);
        Assert.Equal(interruption == "cancelled" ? "Cancelled" : "Invalid", result.ExecutionStatus);
        Assert.True(File.Exists(Path.Combine(q.OutputRoot, "completion.json")));
        Assert.True(File.Exists(Path.Combine(q.OutputRoot, "failure.json")));
        Assert.True(File.Exists(Path.Combine(q.OutputRoot, "seed-ledger.json")));
        TowerPracticalSearch.VerifyAttempts(Path.Combine(q.OutputRoot, "attempts.jsonl"), report);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.VerifyPublication(q.OutputRoot,
            (_, _) => throw new InvalidOperationException("Failed publication must not invoke native verification.")));
    }

    [Fact]
    public async Task Public_launcher_rejects_non_gameplay_fixture_before_reserving_anything()
    {
        var (q, _) = Request();
        var result = await TowerPracticalSearch.Run(q);
        Assert.Equal("Failed", result.IntegrityStatus); Assert.Empty(result.RecommendedPartyIds);
        Assert.True(File.Exists(Path.Combine(q.OutputRoot, "worker-start.json")));
        Assert.True(File.Exists(Path.Combine(q.OutputRoot, "failure.json")));
        Assert.False(File.Exists(Path.Combine(q.OutputRoot, "history-input.json")));
        Assert.False(File.Exists(Path.Combine(q.OutputRoot, "attempts.jsonl")));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.Run(q));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Already_cancelled_public_launch_never_acquires_a_lease_or_starts_a_worker(bool registryBusy)
    {
        var (q, _) = Request();
        using var lease = registryBusy ? TowerCompactBundle.AcquireWriter(Path.Combine(q.RegistryRoot, "complete-family-allocation")) : null;
        var before = Directory.EnumerateFileSystemEntries(root).Order().ToArray();
        using var stop = new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerPracticalSearch.Run(q, stop.Token));
        Assert.False(Path.Exists(q.OutputRoot));
        Assert.Equal(before, Directory.EnumerateFileSystemEntries(root).Order().ToArray());
    }

    [Theory]
    [InlineData("check")] [InlineData("run")] [InlineData("verify")]
    public async Task Already_cancelled_command_stops_before_reading_input(string command)
    {
        using var stop = new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerPracticalSearch.Command(
            ["tower-practical-search-" + command, Path.Combine(root, "does-not-exist.json")], stop.Token));
        Assert.Empty(Directory.EnumerateFileSystemEntries(root));
    }

    [Fact]
    public async Task Public_command_rejects_unsupported_options_without_creating_outputs()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.Command(["tower-practical-search-run", "anything", "--resume"], default));
        Assert.Empty(Directory.EnumerateFileSystemEntries(root));
    }
}
