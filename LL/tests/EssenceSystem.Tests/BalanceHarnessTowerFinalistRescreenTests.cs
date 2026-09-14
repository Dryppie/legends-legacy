using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerFinalistRescreenTests
{
    private sealed record Fixture(TowerBossDiscoveryDefinition Definition, BossDiscoveryRunReport Discovery,
        TowerRescreenShortlist Shortlist, TowerSearchSelected[] Controls);
    private static readonly Lazy<Fixture> Data = new(Create);

    private static Fixture Create()
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(5, 5);
        d = d with { Generation = new(TowerBossGeneration.LoadoutCompositionMethods, [-101, -102, -103], 384, 8192, 4,
            TowerBossDiscovery.Objective, TowerBossGeneration.LoadoutCompositionVersion), ExcludedCombatSeeds = [-1, -2],
            Stages = new(12, 5, 0, 0, new Dictionary<string, BossDiscoverySchedule> { [d.Contexts[0].Id] = new(
                Enumerable.Range(201, 8).ToArray(), Enumerable.Range(301, 64).ToArray(), Enumerable.Range(10001, 512).ToArray(), []) }),
            MaximumBattles = TowerFinalistRescreen.MaximumFights };
        var pool = d.AllowedEssences.GroupBy(e => e.Family, StringComparer.OrdinalIgnoreCase).Select(g => g.First().Id).Take(20).ToArray();
        Assert.Equal(20, pool.Length);
        var input = TowerBossDiscovery.GenerationInputs(d); var arms = new List<BossGenerationArm>();
        foreach (var seed in d.Generation.Seeds)
        foreach (var method in d.Generation.Methods)
        {
            var proposals = new List<BossGeneratedProposal>(); var rows = new List<BossDiscoveryMeasurement>();
            for (var i = 0; i < 384; i++)
            {
                var builds = Enumerable.Range(1, d.RequiredPartySize).ToDictionary(slot => slot, slot => (IReadOnlyList<string>)
                    Enumerable.Range(0, 5).Select(j => pool[(j + (slot == 1 ? i % 20 : slot == 2 ? i / 20 : slot == 3 ? arms.Count : 0)) % 20]).ToArray());
                var party = TowerPartySelection.Choice("independent-generated", builds);
                var provenance = new BossDiscoveryProvenance($"{method}-{seed}-p-{i}", seed, method, "fresh-coverage", [], []);
                proposals.Add(new(provenance, party, "Synthetic legal selection fixture", null, "evaluated"));
                rows.Add(BalanceHarnessTowerBossGenerationTests.Measure(input, party, 0, i / 4d));
            }
            arms.Add(new(method, seed, "CandidateBudgetReached", proposals, rows));
        }
        var report = new BossDiscoveryRunReport("Complete", 18432, 18432, 0, new(d.Generation.PolicyVersion, "Complete", arms, [], null), null);
        var shortlist = TowerFinalistRescreen.Freeze(d, report);
        var controls = arms[0].Proposals.Skip(300).Take(20).Select(p => {
            var scenario = TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, p.Party!, []);
            var id = TowerFinalistRescreen.Id(scenario); return new TowerSearchSelected(id, scenario, [new("saved-control", null, null, null, id)]);
        }).ToArray();
        return new(d, report, shortlist, controls);
    }

    private static TowerBalanceEvidence Evidence(TowerBalanceDefinition d, TowerBalanceCellDefinition cell, int wins) => new(cell.Id, "Complete",
        HarnessJson.Hash(cell.Scenario), HarnessJson.Hash(d.ContentHashes), d.SettingsHash, d.ExecutionHash, d.Cohorts[0].RequiredPartySize,
        cell.Scenario.Seeds.Select((s, i) => new TowerBalanceTrial(s, i < wins ? BattleOutcome.Victory : BattleOutcome.Defeat)).ToArray(), new string('a', 64));

    private static TowerRescreenEvidence[] Rescreen(Func<TowerRescreenCandidate, int>? wins = null)
    {
        var f = Data.Value;
        return f.Shortlist.Arms.Select(arm => {
            var d = TowerFinalistRescreen.RescreenDefinition(f.Definition, arm);
            return new TowerRescreenEvidence(arm.Seed, d.Cells.Select(c => Evidence(d, c,
                wins?.Invoke(arm.Candidates.Single(a => a.Id == c.Id)) ?? 0)).ToArray());
        }).ToArray();
    }

    [Fact]
    public void Freeze_reproduces_original_top_two_and_exact_top_32_without_mutating_discovery()
    {
        var f = Data.Value; var before = HarnessJson.Hash(f.Discovery);
        var old = TowerSearchBenchmark.Select(f.Definition, f.Discovery);
        Assert.Equal(HarnessJson.Hash(old.Arms), HarnessJson.Hash(f.Shortlist.OriginalArms));
        Assert.Equal(3, f.Shortlist.Arms.Count);
        foreach (var arm in f.Shortlist.Arms)
        {
            Assert.Equal(Enumerable.Range(1, 32), arm.Candidates.Select(c => c.OriginalRank));
            var source = f.Discovery.Generation!.Arms.Single(a => a.Method == TowerFinalistRescreen.CandidateMethod && a.Seed == arm.Seed);
            Assert.Equal(TowerBossGeneration.Rank(source.Evaluations).Take(32).Select(e => e.Id), arm.Candidates.Select(c => c.PartyId));
            Assert.All(arm.Candidates, c => Assert.Contains(source.Proposals, p => p.Result == "evaluated" && p.Provenance.Id == c.ProposalId));
        }
        Assert.Equal(before, HarnessJson.Hash(f.Discovery));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(32)] [InlineData(64)]
    public void Ties_including_zero_wins_preserve_original_order_and_never_pool_discovery(int count)
    {
        var f = Data.Value; var evidence = Rescreen(_ => count);
        var selected = TowerFinalistRescreen.Select(f.Definition, f.Discovery, f.Shortlist, evidence.Reverse().ToArray());
        foreach (var arm in f.Shortlist.Arms)
        {
            var row = selected.Arms.Single(a => a.Seed == arm.Seed);
            Assert.Equal(arm.Candidates[0].Id, row.Primary); Assert.Equal(arm.Candidates[1].Id, row.Secondary);
        }
        var changed = TowerFinalistRescreen.Select(f.Definition, f.Discovery, f.Shortlist, Rescreen(c => c.OriginalRank == 32 ? 7 : 0));
        Assert.All(changed.Arms, a => Assert.Equal(f.Shortlist.Arms.Single(s => s.Seed == a.Seed).Candidates[31].Id, a.Primary));
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
        Assert.Throws<InvalidDataException>(() => TowerFinalistRescreen.Select(f.Definition, f.Discovery, f.Shortlist, evidence));
    }

    [Fact]
    public void Rejects_shortlist_substitution_incomplete_arms_reference_ancestry_and_reused_schedules()
    {
        var f = Data.Value; var arm = f.Shortlist.Arms[0];
        var bad = f.Shortlist with { Arms = [arm with { Candidates = arm.Candidates.Reverse().ToArray() }, ..f.Shortlist.Arms.Skip(1)] };
        Assert.Throws<InvalidDataException>(() => TowerFinalistRescreen.Select(f.Definition, f.Discovery, bad, Rescreen()));
        Assert.Throws<InvalidDataException>(() => TowerFinalistRescreen.Freeze(f.Definition, f.Discovery with { Status = "Cancelled" }));
        var g = f.Discovery.Generation!;var a = g.Arms[0];var p = a.Proposals[0];
        var changed = g with { Arms = [a with { Proposals = [p with { Provenance = p.Provenance with { ReferenceIds = ["saved"] } }, ..a.Proposals.Skip(1)] }, ..g.Arms.Skip(1)] };
        Assert.Throws<InvalidDataException>(() => TowerFinalistRescreen.Freeze(f.Definition, f.Discovery with { Generation = changed }));
        var schedule = f.Definition.Stages.Schedules.Values.Single();
        Assert.Throws<InvalidDataException>(() => TowerFinalistRescreen.Validate(f.Definition with {
            Stages = f.Definition.Stages with { Schedules = new Dictionary<string, BossDiscoverySchedule> {
                [f.Definition.Contexts[0].Id] = schedule with { Selection = [schedule.Discovery[0], ..schedule.Selection.Skip(1)] } } } }));
        Assert.Throws<InvalidDataException>(() => TowerFinalistRescreen.Validate(f.Definition with { ExcludedCombatSeeds = [..f.Definition.ExcludedCombatSeeds, schedule.Selection[0]] }));
    }

    [Fact]
    public void Confirmation_retains_originals_controls_and_all_breaches_and_overflow_never_truncates()
    {
        var f = Data.Value; var evidence = Rescreen(c => c.OriginalRank == 32 ? 64 : 0);
        var selected = TowerFinalistRescreen.Select(f.Definition, f.Discovery, f.Shortlist, evidence);
        var comparison = TowerFinalistRescreen.Compare(f.Definition, f.Discovery, f.Shortlist, selected, evidence, f.Controls, f.Controls[0].Id);
        Assert.Equal("Ready", comparison.Status);
        Assert.All(f.Shortlist.OriginalArms, a => { Assert.Contains(comparison.Family, c => c.Id == a.Primary); Assert.Contains(comparison.Family, c => c.Id == a.Secondary); });
        Assert.All(f.Controls, c => Assert.Contains(comparison.Family, row => row.Id == c.Id));
        Assert.All(selected.Arms, a => Assert.Contains(comparison.Family, c => c.Id == a.Primary));
        var overflowEvidence = Rescreen(_ => 33); var overflowSelected = TowerFinalistRescreen.Select(f.Definition, f.Discovery, f.Shortlist, overflowEvidence);
        var overflow = TowerFinalistRescreen.Compare(f.Definition, f.Discovery, f.Shortlist, overflowSelected, overflowEvidence, f.Controls, f.Controls[0].Id);
        Assert.Equal("CapacityExceeded", overflow.Status); Assert.True(overflow.Family.Count > 64);
        Assert.All(f.Shortlist.Arms.SelectMany(a => a.Candidates), c => Assert.Contains(overflow.Family, row => row.Id == c.Id));
        Assert.Throws<InvalidDataException>(() => TowerFinalistRescreen.ConfirmationDefinition(f.Definition, overflow));
    }

    [Fact]
    public void Joint_quality_separates_reliability_selection_benefit_and_family_breaches()
    {
        var f = Data.Value; var evidence = Rescreen(c => c.OriginalRank == 32 ? 10 : 0);
        var selected = TowerFinalistRescreen.Select(f.Definition, f.Discovery, f.Shortlist, evidence);
        var comparison = TowerFinalistRescreen.Compare(f.Definition, f.Discovery, f.Shortlist, selected, evidence, f.Controls, f.Controls[0].Id);
        var d = TowerFinalistRescreen.ConfirmationDefinition(f.Definition, comparison); var ids = selected.Arms.Select(a => a.Primary).ToHashSet();
        var confirmation = d.Cells.Select(c => Evidence(d, c, ids.Contains(c.Id) ? 150 : 0)).ToArray();
        var quality = TowerFinalistRescreen.Quality(f.Definition, comparison, confirmation);
        Assert.Equal("Pass", quality.Reliability); Assert.Equal("Pass", quality.SelectionBenefit); Assert.Equal("Eligible", quality.Adoption);
        Assert.All(quality.Rates.Values, r => Assert.Equal(1 - .025 / d.Cells.Count, r.Confidence, 12));
        Assert.All(quality.Primaries, p => Assert.InRange(p.SelectionBenefit.Lower, .1, .3));
        var breach = confirmation.Select(e => ids.Contains(e.CellId) ? Evidence(d, d.Cells.Single(c => c.Id == e.CellId), 300) : e).ToArray();
        Assert.Equal("Fail", TowerFinalistRescreen.Quality(f.Definition, comparison, breach).JointFamilyAssessment);
        Assert.Throws<InvalidDataException>(() => TowerFinalistRescreen.Quality(f.Definition, comparison, confirmation.Skip(1).ToArray()));
        var same = TowerFinalistRescreen.Pair(confirmation[0].Trials, confirmation[0].Trials);
        Assert.Equal(0, same.Difference); Assert.True(same.Lower <= 0);
        Assert.Throws<InvalidDataException>(() => TowerFinalistRescreen.Pair(confirmation[0].Trials, confirmation[0].Trials.Reverse().ToArray()));
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
        HarnessJson.WriteNew(controls, new TowerSearchBenchmarkSelection([], f.Controls));
        HarnessJson.WriteNew(history, new { historical = new[] { -77 }, nested = new { unused = new[] { -88 } } });
        File.WriteAllText(plan, "Fixture preparation; no campaign fights.");
        var output = Path.Combine(temp.Path, "prepared");
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Unexpected fixture campaign fight")).Activate();
        var protocol = TowerFinalistRescreenStudy.Prepare(TestContentPaths.FindApiRoot(), definition, controls, history, f.Controls[0].Id, 971, plan, output);
        Assert.Equal(57344, protocol.MaximumFights); Assert.Equal(0, protocol.CombatRetries);
        var frozen = TowerFinalistRescreenStudy.Inventory(output);
        TowerFinalistRescreenStudy.VerifyPrepared(output);
        Assert.Equal(HarnessJson.Hash(frozen), HarnessJson.Hash(TowerFinalistRescreenStudy.Inventory(output)));
        var ledger = HarnessJson.Read<JsonElement>(Path.Combine(output, "seed-ledger.json"));
        Assert.Contains(-88, ledger.GetProperty("historical").EnumerateArray().Select(v => v.GetInt32()));
        var fresh = new[] { "generation", "discovery", "rescreen", "confirmation" }.SelectMany(k => ledger.GetProperty(k).EnumerateArray().Select(v => v.GetInt32())).ToArray();
        Assert.Equal(587, fresh.Length); Assert.Equal(587, fresh.Distinct().Count());
        Assert.DoesNotContain(-88, fresh); Assert.DoesNotContain(-77, fresh);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerFinalistRescreenStudy.RunAsync(output, cancelled.Token));
        Assert.False(File.Exists(Path.Combine(output, "started.json")));
        var dataPath = Path.Combine(output, "definition.json"); var originalBytes = File.ReadAllBytes(dataPath);
        File.AppendAllText(dataPath, " "); Assert.Throws<InvalidDataException>(() => TowerFinalistRescreenStudy.VerifyPrepared(output));
        File.WriteAllBytes(dataPath, originalBytes);
        File.WriteAllText(Path.Combine(output, "started.json"), "{}");
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerFinalistRescreenStudy.RunAsync(output));
    }
}
