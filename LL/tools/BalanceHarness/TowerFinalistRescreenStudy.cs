using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Common.Randomness;

namespace BalanceHarness;

public sealed record TowerRescreenProtocol(int SchemaVersion, string Version, string AnchorId, DateTimeOffset FrozenUtc,
    string HarnessHash, int MaximumFights, int MaximumSeconds, long MaximumBytes, int CombatRetries,
    IReadOnlyDictionary<string, string> FrozenFiles);
public sealed record TowerRescreenSummary(string Status, int Started, int Completed, double ExecuteSeconds,
    int ConfirmationRecipes, TowerRescreenQuality? Quality, string Scope);

/// <summary>Prepare, execute once, or reconstruct a separately frozen v13 finalist-selection comparison.</summary>
public static class TowerFinalistRescreenStudy
{
    public const int MaximumSeconds = 5400;
    public const long MaximumBytes = 4294967296;
    private const string FinalFiles = "final-files.json";

    public static TowerRescreenProtocol Prepare(string root, string templatePath, string controlsPath, string historyPath,
        string anchorId, int seed, string planPath, string output)
    {
        output = Path.GetFullPath(output);
        using var lease = TowerCompactBundle.AcquireWriter(output);
        if (Path.Exists(output)) throw new IOException("Choose a new rescreen output directory.");
        if (!File.Exists(planPath)) throw new InvalidDataException("A declared comparison plan must freeze before execution.");
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        var template = TowerBossDiscovery.Read(templatePath); var settings = TowerBundle.ReadSettings(root);
        var controls = HarnessJson.Read<TowerSearchBenchmarkSelection>(controlsPath).Family;
        var history = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(historyPath));
        var historical = history.Concat(template.ExcludedCombatSeeds).Concat(template.Generation.Seeds)
            .Concat(template.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Diagnostics)))
            .Distinct().Order().ToArray();
        if (historical.Length > TowerStudyLimits.HistoricalSeeds - 587) throw new InvalidDataException("Complete history leaves no room for the frozen schedules.");
        var used = historical.ToHashSet();
        int[] Allocate(string label, int count)
        {
            var result = new List<int>();
            for (var attempt = 0; result.Count < count; attempt++)
            {
                if (attempt >= 100000) throw new InvalidDataException("Seed allocation exhausted its bounded attempts.");
                var value = StableRandom.Seed(TowerFinalistRescreen.Policy, seed.ToString(CultureInfo.InvariantCulture), label, attempt.ToString(CultureInfo.InvariantCulture));
                if (used.Add(value)) result.Add(value);
            }
            return result.ToArray();
        }
        var generation = Allocate("generation", 3);
        var schedule = new BossDiscoverySchedule(Allocate("discovery", 8), Allocate("rescreen", 64), Allocate("confirmation", 512), []);
        var d = template with {
            Mode = TowerBossDiscovery.Independent, References = [], Starts = [],
            Generation = new(TowerBossGeneration.LoadoutCompositionMethods, generation, 384, 8192, 4,
                TowerBossDiscovery.Objective, TowerBossGeneration.LoadoutCompositionVersion),
            Stages = new(12, 5, 0, 0, template.Contexts.ToDictionary(c => c.Id, _ => schedule)),
            ExcludedCombatSeeds = historical, MaximumBattles = TowerFinalistRescreen.MaximumFights,
            SettingsHash = HarnessJson.Hash(settings), ExecutionHash = HarnessJson.Hash(ExecutionIdentity.Current())
        };
        TowerFinalistRescreen.Validate(d); TowerBossDiscovery.Validate(root, d); TowerFinalistRescreen.ValidateControls(d, controls, anchorId);
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
            rescreen = schedule.Selection, confirmation = schedule.Confirmation });
        HarnessJson.WriteNew(P("input-provenance.json"), new {
            Template = Path.GetFullPath(templatePath), TemplateHash = HarnessJson.FileHash(templatePath),
            Controls = Path.GetFullPath(controlsPath), ControlsHash = HarnessJson.FileHash(controlsPath),
            History = Path.GetFullPath(historyPath), HistoryHash = HarnessJson.FileHash(historyPath),
            HistoricalReservations = historical.Length, NewReservations = 587, TotalReservations = used.Count
        });
        var inputs = TowerBossDiscovery.GenerationInputs(d);
        HarnessJson.WriteNew(P("generation-inputs.json"), inputs);
        HarnessJson.WriteNew(P("generation-mechanics.json"), TowerBossPartyGenerator.FromInventory(inputs, TowerBossInventory.Create(root, settings.Threat)));
        HarnessJson.WriteNew(P("executable-files.json"), TowerBossStudy.RetainExecutable(output, ExecutionIdentity.Current()));
        var protocol = new TowerRescreenProtocol(1, TowerFinalistRescreen.Policy, anchorId, DateTimeOffset.UtcNow,
            HarnessJson.FileHash(typeof(TowerFinalistRescreenStudy).Assembly.Location), TowerFinalistRescreen.MaximumFights,
            MaximumSeconds, MaximumBytes, 0, Inventory(output));
        HarnessJson.WriteNew(P("protocol.json"), protocol);
        CheckSize(output); return protocol;
    }

    public static TowerRescreenProtocol VerifyPrepared(string output)
    {
        output = Path.GetFullPath(output);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Prepared verification cannot fight.")).Activate();
        var protocol = Inputs(output);
        if (File.Exists(Path.Combine(output, "started.json")) || File.Exists(Path.Combine(output, FinalFiles)))
            throw new InvalidDataException("This package has already started or completed; use completed verification or retain the interrupted evidence.");
        var expected = protocol.FrozenFiles.Keys.Append("protocol.json").ToHashSet(StringComparer.Ordinal);
        if (!expected.SetEquals(Inventory(output).Keys)) throw new InvalidDataException("Prepared package inventory differs.");
        CheckSize(output); return protocol;
    }

    private static TowerRescreenProtocol Inputs(string output)
    {
        string P(string n) => Path.Combine(output, n);
        var p = HarnessJson.Read<TowerRescreenProtocol>(P("protocol.json"));
        if (p.SchemaVersion != 1 || p.Version != TowerFinalistRescreen.Policy || p.MaximumFights != TowerFinalistRescreen.MaximumFights
            || p.MaximumSeconds != MaximumSeconds || p.MaximumBytes != MaximumBytes || p.CombatRetries != 0
            || p.HarnessHash != HarnessJson.FileHash(typeof(TowerFinalistRescreenStudy).Assembly.Location))
            throw new InvalidDataException("Rescreen needs its captured executable and original protocol limits.");
        foreach (var (n, h) in p.FrozenFiles)
        {
            var path = Path.GetFullPath(n, output);
            if (!path.StartsWith(output + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || HarnessJson.FileHash(path) != h)
                throw new InvalidDataException("Frozen rescreen input changed: " + n);
        }
        var d = TowerBossDiscovery.Read(P("definition.json"));
        TowerFinalistRescreen.Validate(d); TowerBossDiscovery.Validate(P("content"), d);
        TowerFinalistRescreen.ValidateControls(d, HarnessJson.Read<TowerSearchSelected[]>(P("controls.json")), p.AnchorId);
        var s = d.Stages.Schedules.Values.Single();
        var ledger = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(P("seed-ledger.json")));
        if (!ledger.SequenceEqual(d.ExcludedCombatSeeds.Concat(d.Generation.Seeds).Concat(s.Discovery).Concat(s.Selection).Concat(s.Confirmation).Distinct().Order())
            || HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(d)) != HarnessJson.Hash(HarnessJson.Read<BossDiscoveryInputs>(P("generation-inputs.json"))))
            throw new InvalidDataException("Rescreen ledger or independent inputs differ.");
        return p;
    }

    internal static Dictionary<string, string> Inventory(string output) => Directory.GetFiles(output, "*", SearchOption.AllDirectories)
        .Where(p => Path.GetRelativePath(output, p) != FinalFiles).Order(StringComparer.Ordinal)
        .ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash);
    private static void CheckSize(string output)
    {
        if (TowerBulkCampaign.StorageBytes(output) > MaximumBytes) throw new InvalidDataException("Rescreen storage cap reached.");
    }

    public static async Task<TowerRescreenSummary> RunAsync(string output, CancellationToken token = default, Action<string>? progress = null)
    {
        output = Path.GetFullPath(output); using var lease = TowerCompactBundle.AcquireWriter(output);
        string P(string n) => Path.Combine(output, n);
        var protocol = VerifyPrepared(output); var d = TowerBossDiscovery.Read(P("definition.json"));
        token.ThrowIfCancellationRequested();
        HarnessJson.WriteNew(P("started.json"), new { utc = DateTimeOffset.UtcNow, protocolHash = HarnessJson.FileHash(P("protocol.json")) });
        var clock = Stopwatch.StartNew(); using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(MaximumSeconds));
        using var journal = new TowerRescreenAttempts(P("attempts.bin"), protocol.MaximumFights);
        using var active = new TowerPerformanceTrace(done => {
            timeout.Token.ThrowIfCancellationRequested();
            if (clock.Elapsed.TotalSeconds >= MaximumSeconds) throw new InvalidDataException("Rescreen time cap reached.");
            if (!done && journal.Started % 128 == 0) CheckSize(output);
            journal.Record(done);
        }).Activate();
        async Task<T> Phase<T>(string name, int expected, Func<Task<T>> run)
        {
            var before = journal.Started; var completed = journal.Completed; var watch = Stopwatch.StartNew();
            try
            {
                var result = await run();
                if (journal.Started - before != expected || journal.Completed - completed != expected)
                    throw new InvalidDataException("Phase attempt accounting differs: " + name);
                return result;
            }
            finally
            {
                var metric = new TowerSearchBenchmarkMetric(name, watch.Elapsed.TotalSeconds, journal.Started - before, journal.Completed - completed);
                HarnessJson.WriteNew(P("metric-" + name + ".json"), metric);
                progress?.Invoke($"{name}: {metric.Started} starts, {metric.Completed} completions, {metric.Seconds:F2}s.");
            }
        }
        TowerBulkOptions Options() => new(32, 0, Math.Max(1, (int)(MaximumSeconds - clock.Elapsed.TotalSeconds)),
            Math.Max(1048576, MaximumBytes - TowerBulkCampaign.StorageBytes(output)));
        try
        {
            var discovery = await Phase("discovery", TowerFinalistRescreen.DiscoveryFights, () => TowerCompactDiscovery.RunAsync(
                P("content"), P("discovery"), d, Options(), token: timeout.Token, progress: progress));
            var shortlist = TowerFinalistRescreen.Freeze(d, discovery);
            HarnessJson.WriteNew(P("shortlist.json"), shortlist); SaveAllRecipes(output, d, discovery, verify: false);
            var evidence = new List<TowerRescreenEvidence>();
            for (var i = 0; i < shortlist.Arms.Count; i++)
            {
                var arm = shortlist.Arms[i]; var name = "rescreen-" + i;
                var definition = TowerFinalistRescreen.RescreenDefinition(d, arm);
                HarnessJson.WriteNew(P(name + "-definition.json"), definition);
                await Phase(name, 2048, () => TowerCompactBalanceRun.RunAsync(P("content"), P(name), definition, Options(), token: timeout.Token));
                evidence.Add(new(arm.Seed, HarnessJson.Read<TowerBalanceEvidence[]>(P(name + "/evidence.json"))));
            }
            var selected = TowerFinalistRescreen.Select(d, discovery, shortlist, evidence);
            HarnessJson.WriteNew(P("selected.json"), selected);
            var comparison = TowerFinalistRescreen.Compare(d, discovery, shortlist, selected, evidence,
                HarnessJson.Read<TowerSearchSelected[]>(P("controls.json")), protocol.AnchorId);
            HarnessJson.WriteNew(P("comparison.json"), comparison);
            TowerRescreenQuality? quality = null;
            if (comparison.Status == "Ready")
            {
                var definition = TowerFinalistRescreen.ConfirmationDefinition(d, comparison);
                HarnessJson.WriteNew(P("confirmation-definition.json"), definition);
                await Phase("confirmation", definition.MaximumBattles, () => TowerCompactBalanceRun.RunAsync(
                    P("content"), P("confirmation"), definition, Options(), token: timeout.Token));
                quality = TowerFinalistRescreen.Quality(d, comparison, HarnessJson.Read<TowerBalanceEvidence[]>(P("confirmation/evidence.json")));
                HarnessJson.WriteNew(P("quality.json"), quality);
            }
            var summary = new TowerRescreenSummary(comparison.Status == "Ready" ? "Complete" : comparison.Status,
                journal.Started, journal.Completed, clock.Elapsed.TotalSeconds, comparison.Family.Count, quality,
                "Finalist selection comparison only; original nominations retained. No pooling, automatic promotion or complete generated-family acceptance.");
            if (summary.Started != summary.Completed || summary.ExecuteSeconds > MaximumSeconds) throw new InvalidDataException("Final attempt or time accounting differs.");
            Inputs(output); CheckSize(output); HarnessJson.WriteNew(P("summary.json"), summary);
            journal.Close(); HarnessJson.WriteNew(P(FinalFiles), Inventory(output)); CheckSize(output);
            return summary;
        }
        catch (Exception e)
        {
            HarnessJson.WriteNew(P("failure.json"), new { journal.Started, journal.Completed, seconds = clock.Elapsed.TotalSeconds, error = e.ToString(), noImplicitRetry = true });
            throw;
        }
    }

    public static async Task<TowerRescreenSummary> VerifyAsync(string output, CancellationToken token = default, Action<string>? progress = null)
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
        var shortlist = TowerFinalistRescreen.Freeze(d, discovery); Equal(shortlist, HarnessJson.Read<TowerRescreenShortlist>(P("shortlist.json")), "shortlist");
        SaveAllRecipes(output, d, discovery, verify: true);
        var evidence = new List<TowerRescreenEvidence>();
        for (var i = 0; i < shortlist.Arms.Count; i++)
        {
            var name = "rescreen-" + i;
            Equal(TowerFinalistRescreen.RescreenDefinition(d, shortlist.Arms[i]), TowerBalanceEvaluator.Read(P(name + "-definition.json")), name + " definition");
            await TowerCompactBalanceRun.VerifyAsync(P(name), token);
            evidence.Add(new(shortlist.Arms[i].Seed, HarnessJson.Read<TowerBalanceEvidence[]>(P(name + "/evidence.json"))));
        }
        var selected = TowerFinalistRescreen.Select(d, discovery, shortlist, evidence);
        Equal(selected, HarnessJson.Read<TowerRescreenSelection>(P("selected.json")), "selection");
        var comparison = TowerFinalistRescreen.Compare(d, discovery, shortlist, selected, evidence,
            HarnessJson.Read<TowerSearchSelected[]>(P("controls.json")), protocol.AnchorId);
        Equal(comparison, HarnessJson.Read<TowerRescreenComparison>(P("comparison.json")), "comparison family");
        TowerRescreenQuality? quality = null;
        if (comparison.Status == "Ready")
        {
            Equal(TowerFinalistRescreen.ConfirmationDefinition(d, comparison), TowerBalanceEvaluator.Read(P("confirmation-definition.json")), "confirmation definition");
            await TowerCompactBalanceRun.VerifyAsync(P("confirmation"), token);
            quality = TowerFinalistRescreen.Quality(d, comparison, HarnessJson.Read<TowerBalanceEvidence[]>(P("confirmation/evidence.json")));
            Equal(quality, HarnessJson.Read<TowerRescreenQuality>(P("quality.json")), "quality");
        }
        else if (Directory.Exists(P("confirmation")) || File.Exists(P("confirmation-definition.json")) || File.Exists(P("quality.json")))
            throw new InvalidDataException("Overflow must not execute confirmation.");
        var summary = HarnessJson.Read<TowerRescreenSummary>(P("summary.json"));
        var expected = TowerFinalistRescreen.DiscoveryFights + TowerFinalistRescreen.RescreenFights + (quality is null ? 0 : comparison.Family.Count * 512);
        TowerRescreenAttempts.Verify(P("attempts.bin"), expected);
        var phases = new Dictionary<string, int> { ["discovery"] = 18432, ["rescreen-0"] = 2048, ["rescreen-1"] = 2048, ["rescreen-2"] = 2048 };
        if (quality is not null) phases.Add("confirmation", comparison.Family.Count * 512);
        foreach (var (name, count) in phases)
        {
            var metric = HarnessJson.Read<TowerSearchBenchmarkMetric>(P("metric-" + name + ".json"));
            if (metric.Stage != name || metric.Started != count || metric.Completed != count || !double.IsFinite(metric.Seconds) || metric.Seconds < 0)
                throw new InvalidDataException("Phase accounting differs.");
        }
        if (summary.Started != expected || summary.Completed != expected || expected > protocol.MaximumFights
            || !double.IsFinite(summary.ExecuteSeconds) || summary.ExecuteSeconds is < 0 or > MaximumSeconds
            || summary.ConfirmationRecipes != comparison.Family.Count || summary.Status != (quality is null ? "CapacityExceeded" : "Complete"))
            throw new InvalidDataException("Final rescreen accounting differs.");
        Equal(quality, summary.Quality, "summary quality"); CheckSize(output);
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

internal sealed class TowerRescreenAttempts : IDisposable
{
    private readonly FileStream stream; private readonly int maximum;
    public int Started { get; private set; }
    public int Completed { get; private set; }
    public TowerRescreenAttempts(string path, int maximum)
    {
        this.maximum = maximum;
        stream = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 1, FileOptions.WriteThrough);
    }
    public void Record(bool done)
    {
        using var timing = TowerPerformanceTrace.Measure("outer.attempt-journal");
        if (done ? Started != Completed + 1 : Started != Completed || Started >= maximum)
            throw new InvalidDataException("Unexpected completion, concurrent start or exhausted attempt budget.");
        stream.WriteByte(done ? (byte)'C' : (byte)'S');
        using (TowerPerformanceTrace.Measure("outer.attempt-flush")) stream.Flush(true);
        if (done) Completed++; else Started++;
    }
    public void Close() => stream.Dispose();
    public void Dispose() => Close();
    public static void Verify(string path, int expected)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length != expected * 2 || bytes.Where((b, i) => b != (i % 2 == 0 ? (byte)'S' : (byte)'C')).Any())
            throw new InvalidDataException("Missing, interrupted or repeated diagnostic attempt.");
    }
}
