using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerFeedbackBenchmarkTests() : BalanceHarnessTowerGenerationComparisonTests(0);

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerLoadoutRetentionBenchmarkTests() : BalanceHarnessTowerGenerationComparisonTests(1);

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerPartyLineageBenchmarkTests() : BalanceHarnessTowerGenerationComparisonTests(2);

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerSearchAllocationBenchmarkTests() : BalanceHarnessTowerGenerationComparisonTests(3);

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerLateAllocationBenchmarkTests() : BalanceHarnessTowerGenerationComparisonTests(4);

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerSearchPortfolioBenchmarkTests() : BalanceHarnessTowerGenerationComparisonTests(5);

public abstract class BalanceHarnessTowerGenerationComparisonTests
{
    internal sealed record Fixture(TowerBossDiscoveryDefinition Definition, BossDiscoveryRunReport Discovery,
        TowerFeedbackShortlist Shortlist, TowerSearchSelected[] Controls);
    private static readonly Lazy<Fixture> Feedback = new(() => Create(0));
    private static readonly Lazy<Fixture> Retention = new(() => Create(1));
    private static readonly Lazy<Fixture> Lineages = new(() => Create(2));
    private static readonly Lazy<Fixture> Allocations = new(() => Create(3));
    private static readonly Lazy<Fixture> LateAllocations = new(() => Create(4));
    private static readonly Lazy<Fixture> Portfolios = new(() => Create(5));
    internal static Fixture PortfolioFixture => Portfolios.Value;
    internal static Fixture LateAllocationFixture => LateAllocations.Value;
    internal static Fixture AllocationFixture => Allocations.Value;
    private readonly Lazy<Fixture> Data;
    protected BalanceHarnessTowerGenerationComparisonTests(int variant) => Data = variant switch { 0 => Feedback, 1 => Retention, 2 => Lineages, 3 => Allocations, 4 => LateAllocations, 5 => Portfolios, _ => throw new ArgumentOutOfRangeException(nameof(variant)) };

    private static Fixture Create(int variant)
    {
        var design = TowerGenerationComparisonDesign.FromPolicy(variant switch { 0 => TowerFeedbackBenchmark.Policy,
            1 => TowerGenerationComparisonDesign.RetentionPolicy, 2 => TowerGenerationComparisonDesign.LineagePolicy, 4 => TowerLateAllocation.Policy, 5 => TowerSearchPortfolio.Policy, _ => TowerSearchAllocation.Policy });
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(5, 5);
        d = d with { Generation = new(design.Methods, [-101, -102, -103], design.CandidatesPerArm, design.MaximumAttempts, 4,
            TowerBossDiscovery.Objective, design.GenerationVersion), ExcludedCombatSeeds = [-1, -2],
            Stages = new(12, 5, 0, 0, new Dictionary<string, BossDiscoverySchedule> { [d.Contexts[0].Id] = new(
                Enumerable.Range(201, 8).ToArray(), Enumerable.Range(301, 64).ToArray(), Enumerable.Range(10001, 512).ToArray(), [], design.FeedbackSamples == 0 ? null : Enumerable.Range(501, 32).ToArray()) }),
            MaximumBattles = design.MaximumFights };
        var pool = d.AllowedEssences.GroupBy(e => e.Family, StringComparer.OrdinalIgnoreCase).Select(g => g.First().Id).Take(20).ToArray();
        Assert.Equal(20, pool.Length);
        var input = TowerBossDiscovery.GenerationInputs(d); var arms = new List<BossGenerationArm>();
        foreach (var seed in d.Generation.Seeds)
        foreach (var method in d.Generation.Methods)
        {
            var proposals = new List<BossGeneratedProposal>(); var rows = new List<BossDiscoveryMeasurement>();
            for (var i = 0; i < (variant >= 4 && TowerSearchAllocation.IsComponent(method) ? 512 : TowerBossGeneration.CandidateBudget(d.Generation, method)); i++)
            {
                var builds = Enumerable.Range(1, d.RequiredPartySize).ToDictionary(slot => slot, slot => (IReadOnlyList<string>)
                    Enumerable.Range(0, 5).Select(j => pool[(j + (slot == 1 ? i % 20 : slot == 2 ? i / 20 : slot == 3 ? arms.Count : slot == 4 ? i / 400 : 0)) % 20]).ToArray());
                var party = TowerPartySelection.Choice("independent-generated", builds);
                var provenance = new BossDiscoveryProvenance($"{method}-{seed}-p-{i}", seed, method, "fresh-coverage", [], []);
                var proposal = new BossGeneratedProposal(provenance, party, "Synthetic legal selection fixture", null, "evaluated");
                if (method == TowerPartyLineages.Method) proposal = proposal with { Lineage = TowerPartyLineages.Describe(proposal, new Dictionary<string, BossGeneratedProposal>()) };
                proposals.Add(proposal);
                rows.Add(BalanceHarnessTowerBossGenerationTests.Measure(input, party, 0, i / (variant == 5 ? 16d : variant >= 3 ? 8d : 4d)));
            }
            List<BossFeedbackRound>? feedback = method == TowerGenerationFeedback.Method ? [] : null;
            if (feedback is not null)
                foreach (var checkpoint in TowerGenerationFeedback.Checkpoints)
                {
                    var ids = TowerGenerationFeedback.Choose(input, rows.Take(checkpoint).ToArray(), feedback);
                    feedback.Add(new(checkpoint, ids.Select(id => BalanceHarnessTowerBossGenerationTests.Measure(
                        input with { DiscoverySeeds = input.FeedbackSeeds! }, proposals.Single(p => p.Party!.Id == id).Party!, 0, 99)).ToArray()));
                }
            arms.Add(new(method, seed, "CandidateBudgetReached", proposals, rows, feedback));
        }
        List<TowerLateAllocationDecision>? decisions = variant >= 4 ? [] : null;
        if (decisions is not null)
            foreach (var seed in d.Generation.Seeds)
            {
                var a = arms.Single(a => a.Seed == seed && a.Method == TowerSearchAllocation.IsolatedA);
                var b = arms.Single(a => a.Seed == seed && a.Method == TowerSearchAllocation.IsolatedB);
                var decision = TowerLateAllocation.Decide(seed, a, b); decisions.Add(decision);
                var loser = decision.SelectedMethod == a.Method ? b : a;
                arms[arms.IndexOf(loser)] = loser with { Proposals = loser.Proposals.Take(256).ToArray(), Evaluations = loser.Evaluations.Take(256).ToArray() };
            }
        var report = new BossDiscoveryRunReport("Complete", design.DiscoveryFights, design.DiscoveryFights, 0, new(d.Generation.PolicyVersion, "Complete", arms, [], null, decisions), null);
        var shortlist = TowerFeedbackBenchmark.Freeze(d, report);
        var controls = arms[0].Proposals.Skip(300).Take(design.Controls).Select(p => {
            var scenario = TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, p.Party!, []);
            var id = TowerFeedbackBenchmark.Id(scenario); return new TowerSearchSelected(id, scenario, [new("saved-control", null, null, null, id)]);
        }).ToArray();
        return new(d, report, shortlist, controls);
    }

    internal static TowerBalanceEvidence Evidence(TowerBalanceDefinition d, TowerBalanceCellDefinition cell, int wins) => new(cell.Id, "Complete",
        HarnessJson.Hash(cell.Scenario), HarnessJson.Hash(d.ContentHashes), d.SettingsHash, d.ExecutionHash, d.Cohorts[0].RequiredPartySize,
        cell.Scenario.Seeds.Select((s, i) => new TowerBalanceTrial(s, i < wins ? BattleOutcome.Victory : BattleOutcome.Defeat)).ToArray(), new string('a', 64));

    private TowerFeedbackEvidence[] Rescreen(Func<TowerRescreenCandidate, int>? wins = null)
    {
        var f = Data.Value;
        return f.Shortlist.Arms.Select(arm => {
            var d = TowerFeedbackBenchmark.RescreenDefinition(f.Definition, arm);
            return new TowerFeedbackEvidence(arm.Method, arm.Seed, d.Cells.Select(c => Evidence(d, c,
                wins?.Invoke(arm.Candidates.Single(a => a.Id == c.Id)) ?? 0)).ToArray());
        }).ToArray();
    }

    [Fact]
    public void Freeze_reproduces_original_top_two_and_exact_top_32_without_mutating_discovery()
    {
        var f = Data.Value; var before = HarnessJson.Hash(f.Discovery);
        Assert.Equal(6, f.Shortlist.Arms.Count);
        Assert.Equal(TowerGenerationComparisonDesign.FromDefinition(f.Definition).DiscoveryFights, TowerBossDiscovery.Validate(f.Definition).Discovery);
        foreach (var arm in f.Shortlist.Arms)
        {
            Assert.Equal(Enumerable.Range(1, 32), arm.Candidates.Select(c => c.OriginalRank));
            var sources = TowerGenerationComparisonDesign.FromDefinition(f.Definition).IsAllocation
                ? TowerSearchAllocation.Components(f.Discovery.Generation!, arm.Method, arm.Seed)
                : [f.Discovery.Generation!.Arms.Single(a => a.Method == arm.Method && a.Seed == arm.Seed)];
            Assert.Equal(TowerBossGeneration.Rank(sources.SelectMany(source => TowerGenerationFeedback.Effective(TowerBossDiscovery.GenerationInputs(f.Definition), source.Evaluations, source.Feedback))).Take(32).Select(e => e.Id), arm.Candidates.Select(c => c.PartyId));
            Assert.All(arm.Candidates, c => Assert.Contains(sources.SelectMany(s => s.Proposals), p => p.Result == "evaluated" && p.Provenance.Id == c.ProposalId));
        }
        Assert.Equal(before, HarnessJson.Hash(f.Discovery));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(32)] [InlineData(64)]
    public void Ties_including_zero_wins_preserve_original_order_and_never_pool_discovery(int count)
    {
        var f = Data.Value; var evidence = Rescreen(_ => count);
        var selected = TowerFeedbackBenchmark.Select(f.Definition, f.Discovery, f.Shortlist, evidence.Reverse().ToArray());
        foreach (var arm in f.Shortlist.Arms)
        {
            var row = selected.Arms.Single(a => a.Seed == arm.Seed && a.Method == arm.Method);
            Assert.Equal(arm.Candidates[0].Id, row.Primary); Assert.Equal(arm.Candidates[1].Id, row.Secondary);
        }
        var changed = TowerFeedbackBenchmark.Select(f.Definition, f.Discovery, f.Shortlist, Rescreen(c => c.OriginalRank == 32 ? 7 : 0));
        Assert.All(changed.Arms, a => Assert.Equal(f.Shortlist.Arms.Single(s => s.Seed == a.Seed && s.Method == a.Method).Candidates[31].Id, a.Primary));
    }

    [Theory]
    [InlineData("missing")] [InlineData("duplicate")] [InlineData("unknown")]
    [InlineData("partial")] [InlineData("error")] [InlineData("recipe")] [InlineData("content")]
    [InlineData("execution")] [InlineData("settings")] [InlineData("party-size")]
    [InlineData("short")] [InlineData("extra")] [InlineData("reordered")] [InlineData("outcome")] [InlineData("artifact")]
    public void Rejects_incomplete_or_replaced_rescreen_evidence(string change)
    {
        var f = Data.Value; var evidence = Rescreen(); var row = evidence[0].Cells[0];
        var bad = change switch {
            "partial" => row with { Status = "Partial" }, "error" => row with { Error = "interrupted" },
            "recipe" => row with { ScenarioHash = new string('b', 64) }, "content" => row with { ContentHash = new string('b', 64) },
            "execution" => row with { ExecutionHash = new string('b', 64) }, "settings" => row with { SettingsHash = new string('b', 64) },
            "party-size" => row with { RequiredPartySize = 1 }, "short" => row with { Trials = row.Trials.Take(63).ToArray() },
            "extra" => row with { Trials = [..row.Trials, new(999, BattleOutcome.Victory)] },
            "reordered" => row with { Trials = row.Trials.Reverse().ToArray() },
            "outcome" => row with { Trials = [new(row.Trials[0].Seed, (BattleOutcome)999), ..row.Trials.Skip(1)] },
            "artifact" => row with { ArtifactHash = "bad" }, "unknown" => row with { CellId = "unknown" }, _ => row
        };
        evidence[0] = evidence[0] with { Cells = change == "missing" ? evidence[0].Cells.Skip(1).ToArray()
            : change == "duplicate" ? [..evidence[0].Cells, row] : [bad, ..evidence[0].Cells.Skip(1)] };
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Select(f.Definition, f.Discovery, f.Shortlist, evidence));
    }

    [Fact]
    public void Rejects_shortlist_substitution_incomplete_arms_reference_ancestry_and_reused_schedules()
    {
        var f = Data.Value; var arm = f.Shortlist.Arms[0];
        var bad = f.Shortlist with { Arms = [arm with { Candidates = arm.Candidates.Reverse().ToArray() }, ..f.Shortlist.Arms.Skip(1)] };
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Select(f.Definition, f.Discovery, bad, Rescreen()));
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Freeze(f.Definition, f.Discovery with { Status = "Cancelled" }));
        var g = f.Discovery.Generation!;var a = g.Arms[0];var p = a.Proposals[0];
        var changed = g with { Arms = [a with { Proposals = [p with { Provenance = p.Provenance with { ReferenceIds = ["saved"] } }, ..a.Proposals.Skip(1)] }, ..g.Arms.Skip(1)] };
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Freeze(f.Definition, f.Discovery with { Generation = changed }));
        var schedule = f.Definition.Stages.Schedules.Values.Single();
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Validate(f.Definition with {
            Stages = f.Definition.Stages with { Schedules = new Dictionary<string, BossDiscoverySchedule> {
                [f.Definition.Contexts[0].Id] = schedule with { Selection = [schedule.Discovery[0], ..schedule.Selection.Skip(1)] } } } }));
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Validate(f.Definition with { ExcludedCombatSeeds = [..f.Definition.ExcludedCombatSeeds, schedule.Selection[0]] }));
    }

    [Fact]
    public void Confirmation_retains_originals_controls_and_all_breaches_and_overflow_never_truncates()
    {
        var f = Data.Value; var evidence = Rescreen(c => c.OriginalRank == 32 ? 64 : 0);
        var selected = TowerFeedbackBenchmark.Select(f.Definition, f.Discovery, f.Shortlist, evidence);
        var comparison = TowerFeedbackBenchmark.Compare(f.Definition, f.Discovery, f.Shortlist, selected, evidence, f.Controls, f.Controls[0].Id, f.Controls[1].Id);
        Assert.Equal("Ready", comparison.Status);
        Assert.All(f.Shortlist.OriginalArms, a => { Assert.Contains(comparison.Family, c => c.Id == a.Primary); Assert.Contains(comparison.Family, c => c.Id == a.Secondary); });
        Assert.All(f.Controls, c => Assert.Contains(comparison.Family, row => row.Id == c.Id));
        Assert.All(selected.Arms, a => Assert.Contains(comparison.Family, c => c.Id == a.Primary));
        var overflowEvidence = Rescreen(_ => 33); var overflowSelected = TowerFeedbackBenchmark.Select(f.Definition, f.Discovery, f.Shortlist, overflowEvidence);
        var overflow = TowerFeedbackBenchmark.Compare(f.Definition, f.Discovery, f.Shortlist, overflowSelected, overflowEvidence, f.Controls, f.Controls[0].Id, f.Controls[1].Id);
        Assert.Equal("CapacityExceeded", overflow.Status); Assert.True(overflow.Family.Count > TowerGenerationComparisonDesign.FromDefinition(f.Definition).FamilyCapacity);
        Assert.All(f.Shortlist.Arms.SelectMany(a => a.Candidates), c => Assert.Contains(overflow.Family, row => row.Id == c.Id));
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.ConfirmationDefinition(f.Definition, overflow));
    }

    [Fact]
    public void Joint_quality_requires_two_of_three_restarts_and_reports_strong_control_separately()
    {
        var f = Data.Value; var evidence = Rescreen(c => c.OriginalRank == 32 ? 10 : 0);
        var selected = TowerFeedbackBenchmark.Select(f.Definition, f.Discovery, f.Shortlist, evidence);
        var comparison = TowerFeedbackBenchmark.Compare(f.Definition, f.Discovery, f.Shortlist, selected, evidence, f.Controls, f.Controls[0].Id, f.Controls[1].Id);
        var d = TowerFeedbackBenchmark.ConfirmationDefinition(f.Definition, comparison); var ids = selected.Arms.Where(a => a.Method == TowerGenerationComparisonDesign.FromDefinition(f.Definition).CandidateMethod).Select(a => a.Primary).ToHashSet();
        var confirmation = d.Cells.Select(c => Evidence(d, c, ids.Contains(c.Id) ? 150 : 0)).ToArray();
        var quality = TowerFeedbackBenchmark.Quality(f.Definition, comparison, confirmation);
        Assert.Equal("Pass", quality.Reliability); Assert.Equal("Eligible", quality.Adoption);
        Assert.All(quality.Rates.Values, r => Assert.Equal(1 - .025 / d.Cells.Count, r.Confidence, 12));
        Assert.All(quality.Primaries, p => Assert.InRange(p.StrongControl.Lower, .1, .3));
        for (var passing = 1; passing <= 2; passing++)
        {
            var winners = selected.Arms.Where(a => a.Method == TowerGenerationComparisonDesign.FromDefinition(f.Definition).CandidateMethod).Take(passing).Select(a => a.Primary).ToHashSet();
            var partial = d.Cells.Select(c => Evidence(d, c, winners.Contains(c.Id) ? 150 : 0)).ToArray();
            var check = TowerFeedbackBenchmark.Quality(f.Definition, comparison, partial);
            Assert.Equal(passing, check.Primaries.Count(p => p.Reliability.Pass));
            Assert.Equal(passing == 2 ? "Pass" : "Fail", check.Reliability);
        }
        var breach = confirmation.Select(e => ids.Contains(e.CellId) ? Evidence(d, d.Cells.Single(c => c.Id == e.CellId), 300) : e).ToArray();
        Assert.Equal("Fail", TowerFeedbackBenchmark.Quality(f.Definition, comparison, breach).JointFamilyAssessment);
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Quality(f.Definition, comparison, confirmation.Skip(1).ToArray()));
        var same = TowerFeedbackBenchmark.Pair(confirmation[0].Trials, confirmation[0].Trials);
        Assert.Equal(0, same.Difference); Assert.True(same.Lower <= 0);
        Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmark.Pair(confirmation[0].Trials, confirmation[0].Trials.Reverse().ToArray()));
    }

    [Fact]
    public void Attempt_journal_charges_interrupted_start_rejects_retries_and_enforces_cap()
    {
        using var temp = new DiscoveryTemp(); var path = Path.Combine(temp.Path, "attempts.bin");
        using (var journal = new TowerRescreenAttempts(path, 1))
        {
            Assert.Throws<InvalidDataException>(() => journal.Record(true));
            journal.Record(false); Assert.Throws<InvalidDataException>(() => journal.Record(false));
            journal.Record(true); Assert.Throws<InvalidDataException>(() => journal.Record(false));
        }
        TowerRescreenAttempts.Verify(path, 1);
        Assert.Throws<IOException>(() => new TowerRescreenAttempts(path, 1));
        var interrupted = Path.Combine(temp.Path, "interrupted.bin");
        using (var journal = new TowerRescreenAttempts(interrupted, 1)) journal.Record(false);
        Assert.Throws<InvalidDataException>(() => TowerRescreenAttempts.Verify(interrupted, 1));
    }

    [Fact]
    public async Task Prepared_package_is_read_only_checked_excludes_history_and_refuses_changed_inputs_or_a_second_start()
    {
        var f = Data.Value; using var temp = new DiscoveryTemp();
        var definition = Path.Combine(temp.Path, "definition.json"); var controls = Path.Combine(temp.Path, "controls.json");
        var history = Path.Combine(temp.Path, "history.json"); var plan = Path.Combine(temp.Path, "plan.md");
        HarnessJson.WriteNew(definition, f.Definition);
        HarnessJson.WriteNew(controls, new TowerRescreenComparison("Ready", [], [], f.Controls, f.Controls[0].Id));
        HarnessJson.WriteNew(history, new { historical = new[] { -77 }, nested = new { unused = new[] { -88 } } });
        File.WriteAllText(plan, "Fixture preparation; no campaign fights.");
        var output = Path.Combine(temp.Path, "prepared");
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Unexpected fixture campaign fight")).Activate();
        var protocol = TowerFeedbackBenchmarkRun.Prepare(TestContentPaths.FindApiRoot(), definition, controls, history, f.Controls[0].Id, f.Controls[1].Id, 971, plan, output, TowerGenerationComparisonDesign.FromDefinition(f.Definition).Policy);
        var design = TowerGenerationComparisonDesign.FromDefinition(f.Definition);
        Assert.Equal(design.MaximumSeconds, protocol.MaximumSeconds); Assert.Equal(design.MaximumFights, protocol.MaximumFights); Assert.Equal(0, protocol.CombatRetries);
        var frozen = TowerFeedbackBenchmarkRun.Inventory(output);
        TowerFeedbackBenchmarkRun.VerifyPrepared(output);
        Assert.Equal(HarnessJson.Hash(frozen), HarnessJson.Hash(TowerFeedbackBenchmarkRun.Inventory(output)));
        var ledger = HarnessJson.Read<JsonElement>(Path.Combine(output, "seed-ledger.json"));
        Assert.Contains(-88, ledger.GetProperty("historical").EnumerateArray().Select(v => v.GetInt32()));
        var fresh = new[] { "generation", "discovery", "rescreen", "confirmation", "feedback" }.SelectMany(k => ledger.GetProperty(k).EnumerateArray().Select(v => v.GetInt32())).ToArray();
        Assert.Equal(design.Reservations, fresh.Length); Assert.Equal(design.Reservations, fresh.Distinct().Count());
        Assert.DoesNotContain(-88, fresh); Assert.DoesNotContain(-77, fresh);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerFeedbackBenchmarkRun.RunAsync(output, cancelled.Token));
        Assert.False(File.Exists(Path.Combine(output, "started.json")));
        var dataPath = Path.Combine(output, "definition.json"); var originalBytes = File.ReadAllBytes(dataPath);
        File.AppendAllText(dataPath, " "); Assert.Throws<InvalidDataException>(() => TowerFeedbackBenchmarkRun.VerifyPrepared(output));
        File.WriteAllBytes(dataPath, originalBytes);
        File.WriteAllText(Path.Combine(output, "started.json"), "{}");
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerFeedbackBenchmarkRun.RunAsync(output));
    }
}
