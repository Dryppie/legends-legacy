using System.Numerics;
using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

// A fixed follow-up to the floor-15 evaluation; the default suite never allocates real seeds or fights.
[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAffinityTeamConfirmationTests
{
    internal const string Version = "affinity-floor15-fixed-team-confirmation-v1";
    private const int Samples = 512, Fights = 1536;
    private const long MaximumBytes = 1073741824;
    private sealed record Request(string Version, string Source, string SourceManifestHash, string Output, string Registry,
        string Runtime, string CandidateId, IReadOnlyList<string> ReferenceIds, int Master,
        IReadOnlyDictionary<string, string> RequiredHistory, IReadOnlyDictionary<string, string> Recoveries,
        IReadOnlyDictionary<string, string> RecoveryHashes);
    private sealed record Team(PartyChoice Party, string Role, TowerScenario Scenario);
    internal sealed record Contrast(string ReferenceId, int GainedWins, int LostWins, double ObservedGain,
        string TailNumerator, string TailDenominator, double OneSidedPValue, bool Qualifies);
    private sealed class ConfirmationFactAttribute : FactAttribute
    {
        public ConfirmationFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_AFFINITY_TEAM_CONFIRMATION")))
                Skip = "Set LL_AFFINITY_TEAM_CONFIRMATION to a pinned one-attempt request; 512 fresh seeds and 1,536 fights.";
        }
    }

    internal static Contrast Compare(string referenceId, int gains, int losses)
    {
        if (gains < 0 || losses < 0 || gains + losses > Samples) throw new InvalidDataException("Invalid discordant pairs.");
        var discordant = gains + losses;
        BigInteger choose = 1, tail = 0;
        for (var k = 0; k <= discordant; k++)
        {
            if (k >= gains) tail += choose;
            if (k < discordant) choose = choose * (discordant - k) / (k + 1);
        }
        var denominator = BigInteger.One << discordant;
        // Bonferroni: one-sided alpha .025 for each of two predeclared references;
        // also require an observed gain of at least five percentage points (26/512).
        return new(referenceId, gains, losses, (gains - losses) / (double)Samples,
            tail.ToString(), denominator.ToString(), (double)tail / (double)denominator,
            gains - losses >= 26 && 40 * tail <= denominator);
    }

    internal static bool Confirmed(IReadOnlyList<Contrast> contrasts)
    {
        if (contrasts.Count != 2 || contrasts.Select(c => c.ReferenceId).Distinct().Count() != 2)
            throw new InvalidDataException("Both distinct references are required.");
        return contrasts.All(c => c.Qualifies);
    }

    internal static TowerCompleteAllocatorPlan Allocator(int master, int block, string domain = Version)
        => new("sha256-us-int32le-reject-v1", domain + $"/block-{block + 1}", master, 1, 255, 100000);

    internal static int[] ReservePanel(string output, int[] history, int master, CancellationToken token,
        Func<int, string, int, int>? literal = null, string domain = Version)
    {
        var storage = new TowerCompleteReservation.Storage(output, MaximumBytes);
        storage.Put("history-input.json", new { reservationState = "Pending", reserved = Array.Empty<int>() });
        var values = new List<int>();
        // Reuse the existing bounded allocator in two declared 256-value blocks.
        // Both blocks are reserved before any combat, and every accepted value is used.
        for (var block = 0; block < 2; block++)
        {
            var index = block; var path = Path.Combine(output, $"allocation-{block + 1}");
            Directory.CreateDirectory(path);
            Func<string, int, int>? candidate = literal is null ? null : (stage, ordinal) => literal(index, stage, ordinal);
            var plan = Allocator(master, block, domain);
            var reserved = TowerCompleteReservation.Reserve(path, plan, history.Concat(values).Order().ToArray(), MaximumBytes, token, candidate);
            TowerCompleteReservation.Verify(path, reserved.Seeds, plan, token, candidate);
            values.AddRange(reserved.Seeds.First.Concat(reserved.Seeds.Second));
            storage.Put("history-input.json", new { reservationState = "Pending", reserved = values.ToArray() }, true);
        }
        Assert.Equal(Samples, values.Distinct().Count());
        storage.Put("confirmation-seeds.json", values);
        storage.Put("seed-ledger.json", new { reservationState = "Complete", historical = history, reserved = values.ToArray() });
        storage.Put("history-input.json", new { reservationState = "Complete", reserved = values.ToArray() }, true);
        return values.ToArray();
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(6, 0, false)]
    [InlineData(25, 0, false)]
    [InlineData(26, 0, true)]
    [InlineData(26, 1, false)]
    [InlineData(27, 1, true)]
    [InlineData(200, 174, false)]
    [InlineData(512, 0, true)]
    public void Confirmation_requires_both_practical_gain_and_adjusted_exact_evidence(int gains, int losses, bool passes)
    {
        var contrast = Compare("reference-2", gains, losses);
        Assert.Equal(passes, contrast.Qualifies);
        Assert.InRange(contrast.OneSidedPValue, 0, 1);
        Assert.Equal(1.0 / 32, Compare("literal", 5, 0).OneSidedPValue);
        Assert.False(Confirmed([contrast, Compare("reference-3", 0, 0)]));
        Assert.Equal(passes, Confirmed([contrast, Compare("reference-3", 512, 0)]));
        Assert.Throws<InvalidDataException>(() => Confirmed([contrast]));
        Assert.Throws<InvalidDataException>(() => Confirmed([contrast, contrast]));
        Assert.Throws<InvalidDataException>(() => Compare("literal", -1, 0));
        Assert.Throws<InvalidDataException>(() => Compare("literal", 512, 1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Two_declared_literal_blocks_never_publish_partial_allocation_as_complete(bool interrupt)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Allocation cannot fight.")).Activate();
        var path = Path.Combine(Path.GetTempPath(), "affinity-confirmation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        try
        {
            int Candidate(int block, string stage, int ordinal) => interrupt && block == 1
                ? throw new IOException("Injected second-block interruption") : 1000 + block * 1000 + (stage == "first" ? 0 : ordinal + 1);
            if (interrupt)
            {
                Assert.Throws<IOException>(() => ReservePanel(path, [42], 7, default, Candidate));
                var delta = HarnessJson.Read<JsonElement>(Path.Combine(path, "history-input.json"));
                Assert.Equal("Pending", delta.GetProperty("reservationState").GetString());
                Assert.Equal(256, delta.GetProperty("reserved").GetArrayLength());
                Assert.False(File.Exists(Path.Combine(path, "seed-ledger.json")));
            }
            else
            {
                var panel = ReservePanel(path, [42], 7, default, Candidate);
                Assert.Equal(Enumerable.Range(1000, 256).Concat(Enumerable.Range(2000, 256)), panel);
                Assert.Equal(panel.Append(42).Order(), TowerSearchBenchmark.History(
                    HarnessJson.Read<JsonElement>(Path.Combine(path, "seed-ledger.json"))));
            }
        }
        finally { Directory.Delete(path, true); }
    }

    [ConfirmationFact]
    public async Task Fixed_candidate_and_both_references_complete_one_fresh_confirmation()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_AFFINITY_TEAM_CONFIRMATION")!);
        Assert.Equal(Version, q.Version);
        Assert.Equal(2, q.ReferenceIds.Count); Assert.Equal(3, q.ReferenceIds.Append(q.CandidateId).Distinct().Count());
        foreach (var path in new[] { q.Source, q.Output, q.Registry, q.Runtime }) TowerProposalStudy.Unlinked(path);
        Assert.Equal(Path.GetFullPath(q.Registry), Path.GetDirectoryName(Path.GetFullPath(q.Output)));
        Assert.False(Path.Exists(q.Output));
        Assert.Equal(q.SourceManifestHash, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        var sourceFiles = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        T Source<T>(string name)
        {
            var path = Path.Combine(q.Source, name); Assert.Equal(sourceFiles[name], HarnessJson.FileHash(path));
            return HarnessJson.Read<T>(path);
        }
        var captured = Source<LoadoutScope>("captured-scope.json") with { Algorithm = Version };
        Assert.Equal(HarnessJson.Hash(captured.Execution), HarnessJson.Hash(ExecutionIdentity.Current()));
        Assert.Equal(new TowerBalanceSelection(18, 4, "healing-v1"), captured.Settings.Balance);
        var sourcePlan = Source<TowerProposalRacingPlan>("plan.json");
        Assert.Equal(sourcePlan.Racing.Scope.Starts.Skip(1).Select(s => s.Party.Id), q.ReferenceIds);
        var saved = Source<Team[]>("heldout-freeze.json");
        var teams = q.ReferenceIds.Prepend(q.CandidateId).Select(id => saved.Single(t => t.Party.Id == id))
            .Select(t => t with { Scenario = t.Scenario with { Seeds = [] } }).ToArray();
        Assert.Equal("generated-finalist", teams[0].Role);
        Assert.All(teams.Skip(1), t => Assert.Equal("existing-reference", t.Role));
        foreach (var team in teams)
        {
            Assert.Equal(15, team.Scenario.FloorNumber);
            Assert.Equal(team.Party.Id, F.Composition(team.Scenario).Id);
            TowerBossDiscovery.ValidateEquipment(team.Scenario.Party, sourcePlan.Racing.Scope.Budget, 15);
        }
        Assert.Single(teams.Select(t => TowerBossDiscovery.EquipmentBudgetHash(t.Scenario.Party)).Distinct());
        var historical = TowerSearchBenchmark.History(Source<JsonElement>("seed-ledger.json"));
        foreach (var pin in q.RecoveryHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key));
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(840)); var token = stop.Token;
        using var registryLease = TowerCompactBundle.AcquireWriter(Path.Combine(q.Registry, "complete-family-allocation"));
        using var outputLease = TowerCompactBundle.AcquireWriter(q.Output);
        Assert.False(Path.Exists(q.Output)); Directory.CreateDirectory(q.Output);
        var clock = System.Diagnostics.Stopwatch.StartNew(); var attempts = 0; var completed = 0; var success = false;
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        void Check()
        {
            token.ThrowIfCancellationRequested();
            Assert.True(attempts <= Fights);
            if (completed % 32 == 0) Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < MaximumBytes);
        }
        try
        {
            Save("request.json", q); Save("scope.json", captured); Save("teams.json", teams);
            Save("protocol.json", new { version = Version, samples = Samples, teams = 3, maximumFights = Fights,
                maximumSeconds = 840, maximumBytes = MaximumBytes, retries = 0, q.CandidateId, q.ReferenceIds,
                decision = "Both comparisons: at least 26 net wins of 512 AND exact one-sided paired binomial p <= 1/40. No pooled discovery data, re-selection or extension.",
                allocation = "Two predeclared 256-value blocks, both before combat; all values are combat seeds." });
            var history = TowerRefinementComparisonLaunch.Refresh(q.Registry, q.Output, q.RequiredHistory, historical, token, q.Recoveries);
            Save("history-files.json", history.Files);
            var hashes = TowerBundle.CopyContent(Path.Combine(q.Source, "content"), Path.Combine(q.Output, "content"), token);
            Assert.Equal(HarnessJson.Hash(captured.ContentHashes), HarnessJson.Hash(hashes));
            TowerBundle.WriteSettings(Path.Combine(q.Output, "content/appsettings.json"), captured.Settings);
            Assert.Equal(HarnessJson.Hash(captured.Settings), HarnessJson.Hash(TowerBundle.ReadSettings(Path.Combine(q.Output, "content"))));
            foreach (var pin in captured.Execution.AssemblyHashes)
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Runtime, pin.Key + ".dll")));
            foreach (var file in Directory.EnumerateFiles(q.Runtime, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(q.Runtime, file);
                if (relative.StartsWith("Fixtures" + Path.DirectorySeparatorChar)) continue;
                var destination = Path.Combine(q.Output, "executable", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(file, destination, false);
            }
            Save("runtime-files.json", F.Inventory(Path.Combine(q.Output, "executable")));
            var runner = new TowerBattleRunner(Path.Combine(q.Output, "content"), OfflineContent.ForTower(Path.Combine(q.Output, "content"), captured.Settings));
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate())
                foreach (var team in teams)
                    _ = await runner.PrepareAsync(runner.CreateInput(team.Scenario with { Seeds = [0] }, 0,
                        captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks), token);
            Save("preflight.json", new { status = "PreparedNoFights", references = 2, candidates = 1, historyCount = historical.Length });
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token); Check();
            var panel = ReservePanel(q.Output, history.Values, q.Master, token);
            var study = Path.Combine(q.Output, "study");
            var archive = F.Archive(study, captured, Fights, q.Output, token);
            foreach (var team in teams)
            {
                var scenario = team.Scenario with { Seeds = panel };
                foreach (var seed in panel)
                {
                    Check(); Assert.True(++attempts <= Fights);
                    TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), $"{{\"attempt\":{attempts}}}\n");
                    _ = await archive.EvaluateAsync(Version, team.Party.Id, scenario, seed, token); completed++;
                }
            }
            Assert.Equal(Fights, completed); Assert.Equal(0, archive.CacheHits); F.Seal(study);
            var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(Fights, trials.Count);
            var observations = teams.ToDictionary(t => t.Party.Id, _ => new List<TowerBattleReport>());
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Audit cannot fight.")).Activate())
            for (var i = 0; i < trials.Count; i++)
            {
                Check(); var trial = trials[i]; var team = teams[i / Samples]; var scenario = team.Scenario with { Seeds = panel };
                Assert.Equal(team.Party.Id, trial.Stage); Assert.Equal(panel[i % Samples], trial.Seed);
                Assert.Equal(HarnessJson.Hash(scenario), trial.Recipe);
                var input = runner.CreateInput(scenario, trial.Seed, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks);
                Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(captured, Version, input), trial.CacheKey);
                var report = TowerLoadoutArchive.ReadBattle(study, trial.Id, captured.ReportStorage);
                Assert.Equal(trial.Seed, report.Battle.Seed); Assert.Equal(scenario.Id, report.Battle.ScenarioId);
                Assert.Equal(report.Battle.Summary.ContentOutcome == BattleOutcome.Victory, report.Succeeded);
                observations[team.Party.Id].Add(report);
            }
            for (var block = 0; block < 2; block++)
            {
                var path = Path.Combine(q.Output, $"allocation-{block + 1}");
                TowerCompleteReservation.Verify(path, HarnessJson.Read<TowerCompleteSeeds>(Path.Combine(path, "seeds.json")), Allocator(q.Master, block), token);
            }
            Assert.Equal(panel.Concat(historical).Order(), TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(q.Output, "seed-ledger.json"))));
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token);
            foreach (var pin in captured.ContentHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "content/Data", pin.Key)));
            foreach (var pin in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "executable", pin.Key)));
            var contrasts = q.ReferenceIds.Select(id => {
                var paired = observations[q.CandidateId].Zip(observations[id]).ToArray();
                return Compare(id, paired.Count(p => p.First.Succeeded && !p.Second.Succeeded), paired.Count(p => !p.First.Succeeded && p.Second.Succeeded)); }).ToArray();
            Save("result.json", new { version = Version, status = "Complete", fights = completed, freshValues = Samples,
                decision = Confirmed(contrasts) ? "StrongerFixedTeamConfirmed" : "StrengthNotDemonstrated", q.CandidateId,
                rows = teams.Select(t => new { id = t.Party.Id, t.Role, wins = observations[t.Party.Id].Count(r => r.Succeeded), samples = Samples,
                    meanGuardianHealth = observations[t.Party.Id].Average(r => r.GuardianHealthRemainingPercent) }), contrasts,
                retries = 0, seconds = clock.Elapsed.TotalSeconds,
                interpretation = "Fixed-team strength under the captured floor, gear and rules only. Does not alter the original search decision or establish algorithm reliability." });
            Check(); success = true;
        }
        finally
        {
            Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, seconds = clock.Elapsed.TotalSeconds, retries = 0 });
            F.Seal(q.Output);
        }
    }
}
