using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Common.Randomness;

namespace BalanceHarness;

public sealed record TowerFeedbackProtocol(int SchemaVersion, string Version, string AnchorId, string StrongControlId, DateTimeOffset FrozenUtc,
    string HarnessHash, int MaximumFights, int MaximumSeconds, long MaximumBytes, int CombatRetries,
    IReadOnlyDictionary<string, string> FrozenFiles,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] string? StorageAccounting = null);
public sealed record TowerFeedbackSummary(string Status, int Started, int Completed, double ExecuteSeconds,
    int ConfirmationRecipes, TowerFeedbackQuality? Quality, string Scope);

/// <summary>Prepare, execute once, or reconstruct a separately frozen equal-budget generation-feedback comparison.</summary>
public static class TowerFeedbackBenchmarkRun
{
    public const int MaximumSeconds = 5400;
    public const long MaximumBytes = 4294967296;
    private const string FinalFiles = "final-files.json";

    public static TowerFeedbackProtocol Prepare(string root, string templatePath, string controlsPath, string historyPath,
        string anchorId, string strongControlId, int seed, string planPath, string output, string policy = TowerFeedbackBenchmark.Policy,
        string? storageAccounting = null)
    {
        if (storageAccounting is not null && storageAccounting != TowerStorageAccountant.Mode)
            throw new InvalidDataException("Unknown feedback storage accounting contract.");
        output = Path.GetFullPath(output);
        using var lease = TowerCompactBundle.AcquireWriter(output);
        if (Path.Exists(output)) throw new IOException("Choose a new rescreen output directory.");
        if (!File.Exists(planPath)) throw new InvalidDataException("A declared comparison plan must freeze before execution.");
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        var design = TowerGenerationComparisonDesign.FromPolicy(policy);
        var template = TowerBossDiscovery.Read(templatePath); var settings = TowerBundle.ReadSettings(root);
        var controls = HarnessJson.Read<JsonElement>(controlsPath).GetProperty("family").Deserialize<TowerSearchSelected[]>(HarnessJson.Options)!;
        var history = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(historyPath));
        var historical = history.Concat(template.ExcludedCombatSeeds).Concat(template.Generation.Seeds)
            .Concat(template.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Diagnostics).Concat(s.Feedback ?? [])))
            .Distinct().Order().ToArray();
        if (historical.Length > TowerStudyLimits.HistoricalSeeds - design.Reservations) throw new InvalidDataException("Complete history leaves no room for the frozen schedules.");
        var used = historical.ToHashSet();
        int[] Allocate(string label, int count)
        {
            var result = new List<int>();
            for (var attempt = 0; result.Count < count; attempt++)
            {
                if (attempt >= 100000) throw new InvalidDataException("Seed allocation exhausted its bounded attempts.");
                var value = StableRandom.Seed(design.Policy, seed.ToString(CultureInfo.InvariantCulture), label, attempt.ToString(CultureInfo.InvariantCulture));
                if (used.Add(value)) result.Add(value);
            }
            return result.ToArray();
        }
        var generation = Allocate("generation", 3);
        var schedule = new BossDiscoverySchedule(Allocate("discovery", 8), Allocate("rescreen", 64), Allocate("confirmation", 512), [], design.FeedbackSamples == 0 ? null : Allocate("feedback", design.FeedbackSamples));
        var d = template with {
            Mode = TowerBossDiscovery.Independent, References = [], Starts = [],
            Generation = new(design.Methods, generation, design.CandidatesPerArm, design.MaximumAttempts, 4,
                TowerBossDiscovery.Objective, design.GenerationVersion),
            Stages = new(12, 5, 0, 0, template.Contexts.ToDictionary(c => c.Id, _ => schedule)),
            ExcludedCombatSeeds = historical, MaximumBattles = design.MaximumFights,
            SettingsHash = HarnessJson.Hash(settings), ExecutionHash = HarnessJson.Hash(ExecutionIdentity.Current())
        };
        TowerFeedbackBenchmark.Validate(d); TowerBossDiscovery.Validate(root, d); TowerFeedbackBenchmark.ValidateControls(d, controls, anchorId, strongControlId);
        // Check every external recipe with production preparation before reserving an executable campaign.
        TowerBossDiscovery.Validate(root, d with { References = controls.Select(c => new BossBenchmarkReference(c.Id,
            d.Contexts[0].Id, c.Scenario, "Explicit external control", HarnessJson.Hash(c))).ToArray() });
        Directory.CreateDirectory(output); string P(string n) => Path.Combine(output, n);
        if (HarnessJson.Hash(TowerBundle.CopyContent(root, P("content"), CancellationToken.None)) != HarnessJson.Hash(d.ContentHashes))
            throw new InvalidDataException("Content changed during preparation.");
        HarnessJson.WriteNew(P("content/appsettings.json"), new Dictionary<string, object> {
            ["Combat"] = new Dictionary<string, object> { ["ThreatAndTanking"] = settings.Threat,
                ["IdleProgression"] = new Dictionary<string, object> { ["EncounterCadenceSeconds"] = RunBundle.ReadCombatSettings(root).Cadence } },
            ["WorldTower"] = new Dictionary<string, object> { ["CombatTicksPerFrame"] = settings.CheckpointIntervalTicks }
        });
        File.Copy(planPath, P("study-plan.md"));
        HarnessJson.WriteNew(P("definition.json"), d); HarnessJson.WriteNew(P("controls.json"), controls);
        HarnessJson.WriteNew(P("seed-ledger.json"), new { historical, generation, discovery = schedule.Discovery,
            rescreen = schedule.Selection, confirmation = schedule.Confirmation, feedback = schedule.Feedback ?? [] });
        HarnessJson.WriteNew(P("input-provenance.json"), new {
            Template = Path.GetFullPath(templatePath), TemplateHash = HarnessJson.FileHash(templatePath),
            Controls = Path.GetFullPath(controlsPath), ControlsHash = HarnessJson.FileHash(controlsPath),
            History = Path.GetFullPath(historyPath), HistoryHash = HarnessJson.FileHash(historyPath),
            HistoricalReservations = historical.Length, NewReservations = design.Reservations, TotalReservations = used.Count
        });
        var inputs = TowerBossDiscovery.GenerationInputs(d);
        HarnessJson.WriteNew(P("generation-inputs.json"), inputs);
        HarnessJson.WriteNew(P("generation-mechanics.json"), TowerBossPartyGenerator.FromInventory(inputs, TowerBossInventory.Create(root, settings.Threat)));
        HarnessJson.WriteNew(P("executable-files.json"), TowerBossStudy.RetainExecutable(output, ExecutionIdentity.Current()));
        var protocol = new TowerFeedbackProtocol(storageAccounting is null ? 1 : 2, design.Policy, anchorId, strongControlId, DateTimeOffset.UtcNow,
            HarnessJson.FileHash(typeof(TowerFeedbackBenchmarkRun).Assembly.Location), design.MaximumFights,
            design.MaximumSeconds, design.MaximumBytes, 0, Inventory(output), storageAccounting);
        HarnessJson.WriteNew(P("protocol.json"), protocol);
        CheckSize(output, protocol.MaximumBytes); return protocol;
    }

    public static TowerFeedbackProtocol VerifyPrepared(string output)
    {
        output = Path.GetFullPath(output);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Prepared verification cannot fight.")).Activate();
        var protocol = Inputs(output);
        if (File.Exists(Path.Combine(output, "started.json")) || File.Exists(Path.Combine(output, FinalFiles)))
            throw new InvalidDataException("This package has already started or completed; use completed verification or retain the interrupted evidence.");
        var expected = protocol.FrozenFiles.Keys.Append("protocol.json").ToHashSet(StringComparer.Ordinal);
        if (!expected.SetEquals(Inventory(output).Keys)) throw new InvalidDataException("Prepared package inventory differs.");
        CheckSize(output, protocol.MaximumBytes); return protocol;
    }

    private static TowerFeedbackProtocol Inputs(string output)
    {
        string P(string n) => Path.Combine(output, n);
        var p = HarnessJson.Read<TowerFeedbackProtocol>(P("protocol.json"));
        var design = TowerGenerationComparisonDesign.FromPolicy(p.Version);
        if (!(p.SchemaVersion == 1 && p.StorageAccounting is null || p.SchemaVersion == 2 && p.StorageAccounting == TowerStorageAccountant.Mode)
            || p.MaximumFights != design.MaximumFights
            || p.MaximumSeconds != design.MaximumSeconds || p.MaximumBytes != design.MaximumBytes || p.CombatRetries != 0
            || p.HarnessHash != HarnessJson.FileHash(typeof(TowerFeedbackBenchmarkRun).Assembly.Location))
            throw new InvalidDataException("Rescreen needs its captured executable and original protocol limits.");
        foreach (var (n, h) in p.FrozenFiles)
        {
            var path = Path.GetFullPath(n, output);
            if (!path.StartsWith(output + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || HarnessJson.FileHash(path) != h)
                throw new InvalidDataException("Frozen rescreen input changed: " + n);
        }
        var d = TowerBossDiscovery.Read(P("definition.json"));
        if (TowerGenerationComparisonDesign.FromDefinition(d).Policy != p.Version) throw new InvalidDataException("Protocol and generation version differ.");
        TowerFeedbackBenchmark.Validate(d); TowerBossDiscovery.Validate(P("content"), d);
        TowerFeedbackBenchmark.ValidateControls(d, HarnessJson.Read<TowerSearchSelected[]>(P("controls.json")), p.AnchorId, p.StrongControlId);
        var s = d.Stages.Schedules.Values.Single();
        var ledger = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(P("seed-ledger.json")));
        if (!ledger.SequenceEqual(d.ExcludedCombatSeeds.Concat(d.Generation.Seeds).Concat(s.Discovery).Concat(s.Selection).Concat(s.Confirmation).Concat(s.Feedback ?? []).Distinct().Order())
            || HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(d)) != HarnessJson.Hash(HarnessJson.Read<BossDiscoveryInputs>(P("generation-inputs.json"))))
            throw new InvalidDataException("Rescreen ledger or independent inputs differ.");
        return p;
    }

    internal static Dictionary<string, string> Inventory(string output) => Directory.GetFiles(output, "*", SearchOption.AllDirectories)
        .Where(p => Path.GetRelativePath(output, p) != FinalFiles).Order(StringComparer.Ordinal)
        .ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash);
    private static void CheckSize(string output, long maximumBytes)
    {
        using var timing = TowerPerformanceTrace.Measure("outer.check-storage");
        if (TowerBulkCampaign.StorageBytes(output) > maximumBytes) throw new InvalidDataException("Rescreen storage cap reached.");
    }

    public static async Task<TowerFeedbackSummary> RunAsync(string output, CancellationToken token = default, Action<string>? progress = null)
    {
        output = Path.GetFullPath(output); using var lease = TowerCompactBundle.AcquireWriter(output);
        string P(string n) => Path.Combine(output, n);
        var protocol = VerifyPrepared(output); var d = TowerBossDiscovery.Read(P("definition.json"));
        token.ThrowIfCancellationRequested();
        HarnessJson.WriteNew(P("started.json"), new { utc = DateTimeOffset.UtcNow, protocolHash = HarnessJson.FileHash(P("protocol.json")) });
        var clock = Stopwatch.StartNew(); using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(protocol.MaximumSeconds));
        using var journal = new TowerRescreenAttempts(P("attempts.bin"), protocol.MaximumFights);
        TowerStorageAccountant? storage = null;
        var trace = new TowerPerformanceTrace(done => {
            timeout.Token.ThrowIfCancellationRequested();
            if (clock.Elapsed.TotalSeconds >= protocol.MaximumSeconds) throw new InvalidDataException("Rescreen time cap reached.");
            if (!done && journal.Started % 128 == 0) CheckStorage();
            journal.Record(done);
        });
        using var active = trace.Activate();
        var process = Process.GetCurrentProcess(); var cpu = process.TotalProcessorTime; var allocated = GC.GetTotalAllocatedBytes();
        void CheckStorage() { if (storage is null) CheckSize(output, protocol.MaximumBytes); else storage.Check(timeout.Token); }
        void SavePerformance(string status, string? error = null)
        {
            if (protocol.StorageAccounting is not null) HarnessJson.WriteNew(P(status == "Failed" ? "performance-failure.json" : "performance.json"), new {
                status, error, seconds = clock.Elapsed.TotalSeconds, cpuSeconds = (process.TotalProcessorTime - cpu).TotalSeconds,
                allocatedBytes = GC.GetTotalAllocatedBytes() - allocated, peakWorkingSetBytes = process.PeakWorkingSet64,
                journal.Started, journal.Completed, timings = trace.Snapshot(), storageAccounting = protocol.StorageAccounting,
                snapshotBoundary = "Before trace serialization and completion inventory publication; final sealing overhead excluded." });
        }
        async Task<T> Phase<T>(string name, int expected, Func<Task<T>> run)
        {
            var before = journal.Started; var completed = journal.Completed; var watch = Stopwatch.StartNew();
            try
            {
                using var timing = TowerPerformanceTrace.Measure("phase." + name);
                storage?.BeginDirectory(P(name), timeout.Token);
                var result = await run();
                if (journal.Started - before != expected || journal.Completed - completed != expected)
                    throw new InvalidDataException("Phase attempt accounting differs: " + name);
                storage?.SealDirectory(timeout.Token);
                return result;
            }
            finally
            {
                var metric = new TowerSearchBenchmarkMetric(name, watch.Elapsed.TotalSeconds, journal.Started - before, journal.Completed - completed);
                HarnessJson.WriteNew(P("metric-" + name + ".json"), metric);
                progress?.Invoke($"{name}: {metric.Started} starts, {metric.Completed} completions, {metric.Seconds:F2}s.");
            }
        }
        TowerBulkOptions Options() => new(32, 0, Math.Max(1, (int)(protocol.MaximumSeconds - clock.Elapsed.TotalSeconds)),
            Math.Max(1048576, protocol.MaximumBytes - (storage?.Check(timeout.Token) ?? TowerBulkCampaign.StorageBytes(output))),
            StorageAccounting: protocol.StorageAccounting);
        try
        {
            if (protocol.StorageAccounting is not null)
            {
                storage = new(output, protocol.MaximumBytes, ["attempts.bin", "summary.json", "failure.json", "performance.json", "performance-failure.json",
                    "shortlist.json", "all-evaluated-recipes.json", "selected.json", "comparison.json", "quality.json", FinalFiles, FinalFiles + ".pending"], timeout.Token);
                foreach (var name in Enumerable.Range(0, 6).Select(i => "rescreen-" + i).Concat(["discovery", "confirmation"]))
                { storage.AllowMetadata("metric-" + name + ".json"); storage.AllowMetadata(name + "-definition.json"); }
            }
            using var ownership = TowerStorageOwnership.Activate(storage);
            var discovery = await Phase("discovery", TowerGenerationComparisonDesign.FromDefinition(d).DiscoveryFights, () => TowerCompactDiscovery.RunAsync(
                P("content"), P("discovery"), d, Options(), token: timeout.Token, progress: progress));
            var shortlist = TowerFeedbackBenchmark.Freeze(d, discovery);
            HarnessJson.WriteNew(P("shortlist.json"), shortlist); SaveAllRecipes(output, d, discovery, verify: false);
            var evidence = new List<TowerFeedbackEvidence>();
            for (var i = 0; i < shortlist.Arms.Count; i++)
            {
                var arm = shortlist.Arms[i]; var name = "rescreen-" + i;
                var definition = TowerFeedbackBenchmark.RescreenDefinition(d, arm);
                HarnessJson.WriteNew(P(name + "-definition.json"), definition);
                await Phase(name, 2048, () => TowerCompactBalanceRun.RunAsync(P("content"), P(name), definition, Options(), token: timeout.Token));
                evidence.Add(new(arm.Method, arm.Seed, HarnessJson.Read<TowerBalanceEvidence[]>(P(name + "/evidence.json"))));
            }
            var selected = TowerFeedbackBenchmark.Select(d, discovery, shortlist, evidence);
            HarnessJson.WriteNew(P("selected.json"), selected);
            var comparison = TowerFeedbackBenchmark.Compare(d, discovery, shortlist, selected, evidence,
                HarnessJson.Read<TowerSearchSelected[]>(P("controls.json")), protocol.AnchorId, protocol.StrongControlId);
            HarnessJson.WriteNew(P("comparison.json"), comparison);
            TowerFeedbackQuality? quality = null;
            if (comparison.Status == "Ready")
            {
                var definition = TowerFeedbackBenchmark.ConfirmationDefinition(d, comparison);
                HarnessJson.WriteNew(P("confirmation-definition.json"), definition);
                await Phase("confirmation", definition.MaximumBattles, () => TowerCompactBalanceRun.RunAsync(
                    P("content"), P("confirmation"), definition, Options(), token: timeout.Token));
                quality = TowerFeedbackBenchmark.Quality(d, comparison, HarnessJson.Read<TowerBalanceEvidence[]>(P("confirmation/evidence.json")));
                HarnessJson.WriteNew(P("quality.json"), quality);
            }
            var summary = new TowerFeedbackSummary(comparison.Status == "Ready" ? "Complete" : comparison.Status,
                journal.Started, journal.Completed, clock.Elapsed.TotalSeconds, comparison.Family.Count, quality,
                "Equal-budget generation comparison; all initial and feedback measurements retained. No pooling, automatic promotion or complete generated-family acceptance.");
            if (summary.Started != summary.Completed || summary.ExecuteSeconds > protocol.MaximumSeconds) throw new InvalidDataException("Final attempt or time accounting differs.");
            Inputs(output); CheckStorage(); HarnessJson.WriteNew(P("summary.json"), summary);
            journal.Close(); storage?.Audit(timeout.Token); SavePerformance(summary.Status);
            if (storage is null) { HarnessJson.WriteNew(P(FinalFiles), Inventory(output)); CheckSize(output, protocol.MaximumBytes); }
            else
            {
                HarnessJson.WriteNew(P(FinalFiles + ".pending"), Inventory(output));
                storage.Audit(timeout.Token);
                File.Move(P(FinalFiles + ".pending"), P(FinalFiles));
            }
            return summary;
        }
        catch (Exception e)
        {
            SavePerformance("Failed", e.ToString());
            HarnessJson.WriteNew(P("failure.json"), new { journal.Started, journal.Completed, seconds = clock.Elapsed.TotalSeconds, error = e.ToString(), noImplicitRetry = true });
            throw;
        }
    }

    public static async Task<TowerFeedbackSummary> VerifyAsync(string output, CancellationToken token = default, Action<string>? progress = null)
    {
        output = Path.GetFullPath(output); using var lease = TowerCompactBundle.AcquireWriter(output);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Rescreen reconstruction must not fight.")).Activate();
        string P(string n) => Path.Combine(output, n);
        var protocol = Inputs(output); var manifest = HarnessJson.Read<Dictionary<string, string>>(P(FinalFiles));
        Equal(manifest, Inventory(output), "complete package inventory");
        var start = HarnessJson.Read<JsonElement>(P("started.json"));
        if (start.GetProperty("protocolHash").GetString() != HarnessJson.FileHash(P("protocol.json"))) throw new InvalidDataException("Start protocol differs.");
        var d = TowerBossDiscovery.Read(P("definition.json"));
        progress?.Invoke("Reconstructing complete saved discovery; zero new fights.");
        var discovery = await TowerCompactDiscovery.VerifyAsync(P("discovery"), token);
        var shortlist = TowerFeedbackBenchmark.Freeze(d, discovery); Equal(shortlist, HarnessJson.Read<TowerFeedbackShortlist>(P("shortlist.json")), "shortlist");
        SaveAllRecipes(output, d, discovery, verify: true);
        var evidence = new List<TowerFeedbackEvidence>();
        for (var i = 0; i < shortlist.Arms.Count; i++)
        {
            var name = "rescreen-" + i;
            Equal(TowerFeedbackBenchmark.RescreenDefinition(d, shortlist.Arms[i]), TowerBalanceEvaluator.Read(P(name + "-definition.json")), name + " definition");
            await TowerCompactBalanceRun.VerifyAsync(P(name), token);
            evidence.Add(new(shortlist.Arms[i].Method, shortlist.Arms[i].Seed, HarnessJson.Read<TowerBalanceEvidence[]>(P(name + "/evidence.json"))));
        }
        var selected = TowerFeedbackBenchmark.Select(d, discovery, shortlist, evidence);
        Equal(selected, HarnessJson.Read<TowerFeedbackSelection>(P("selected.json")), "selection");
        var comparison = TowerFeedbackBenchmark.Compare(d, discovery, shortlist, selected, evidence,
            HarnessJson.Read<TowerSearchSelected[]>(P("controls.json")), protocol.AnchorId, protocol.StrongControlId);
        Equal(comparison, HarnessJson.Read<TowerFeedbackComparison>(P("comparison.json")), "comparison family");
        TowerFeedbackQuality? quality = null;
        if (comparison.Status == "Ready")
        {
            Equal(TowerFeedbackBenchmark.ConfirmationDefinition(d, comparison), TowerBalanceEvaluator.Read(P("confirmation-definition.json")), "confirmation definition");
            await TowerCompactBalanceRun.VerifyAsync(P("confirmation"), token);
            quality = TowerFeedbackBenchmark.Quality(d, comparison, HarnessJson.Read<TowerBalanceEvidence[]>(P("confirmation/evidence.json")));
            Equal(quality, HarnessJson.Read<TowerFeedbackQuality>(P("quality.json")), "quality");
        }
        else if (Directory.Exists(P("confirmation")) || File.Exists(P("confirmation-definition.json")) || File.Exists(P("quality.json")))
            throw new InvalidDataException("Overflow must not execute confirmation.");
        var summary = HarnessJson.Read<TowerFeedbackSummary>(P("summary.json"));
        var expected = TowerGenerationComparisonDesign.FromDefinition(d).DiscoveryFights + TowerFeedbackBenchmark.RescreenFights + (quality is null ? 0 : comparison.Family.Count * 512);
        TowerRescreenAttempts.Verify(P("attempts.bin"), expected);
        var phases = new Dictionary<string, int> { ["discovery"] = TowerGenerationComparisonDesign.FromDefinition(d).DiscoveryFights, ["rescreen-0"] = 2048, ["rescreen-1"] = 2048, ["rescreen-2"] = 2048, ["rescreen-3"] = 2048, ["rescreen-4"] = 2048, ["rescreen-5"] = 2048 };
        if (quality is not null) phases.Add("confirmation", comparison.Family.Count * 512);
        foreach (var (name, count) in phases)
        {
            var metric = HarnessJson.Read<TowerSearchBenchmarkMetric>(P("metric-" + name + ".json"));
            if (metric.Stage != name || metric.Started != count || metric.Completed != count || !double.IsFinite(metric.Seconds) || metric.Seconds < 0)
                throw new InvalidDataException("Phase accounting differs.");
        }
        if (summary.Started != expected || summary.Completed != expected || expected > protocol.MaximumFights
            || !double.IsFinite(summary.ExecuteSeconds) || summary.ExecuteSeconds < 0 || summary.ExecuteSeconds > protocol.MaximumSeconds
            || summary.ConfirmationRecipes != comparison.Family.Count || summary.Status != (quality is null ? "CapacityExceeded" : "Complete"))
            throw new InvalidDataException("Final rescreen accounting differs.");
        Equal(quality, summary.Quality, "summary quality"); CheckSize(output, protocol.MaximumBytes);
        Equal(manifest, Inventory(output), "unchanged package"); return summary;
    }

    private static void SaveAllRecipes(string output, TowerBossDiscoveryDefinition d, BossDiscoveryRunReport discovery, bool verify)
    {
        var all = discovery.Generation!.Arms.SelectMany(arm => {
            var proposals = arm.Proposals.Where(p => p.Result == "evaluated").ToDictionary(p => p.Party!.Id);
            return arm.Evaluations.Select(row => new {
                arm.Method, arm.Seed, PartyId = row.Id, Wins = row.Cells.Single().Clears.Count(w => w),
                Scenario = TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, proposals[row.Id].Party!, [])
            });
        }).ToArray();
        var path = Path.Combine(output, "all-evaluated-recipes.json");
        if (verify) Equal(all, HarnessJson.Read<JsonElement>(path), "all evaluated recipes"); else HarnessJson.WriteNew(path, all);
    }
    private static void Equal<T, U>(T expected, U actual, string label)
    {
        if (HarnessJson.Hash(expected) != HarnessJson.Hash(actual)) throw new InvalidDataException("Saved " + label + " differs.");
    }
}
