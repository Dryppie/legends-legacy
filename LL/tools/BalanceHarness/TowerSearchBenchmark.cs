using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Common.Randomness;

namespace BalanceHarness;

public sealed record TowerSearchParityInput(string Id, TowerScenario Scenario, int Seed, string Reference, bool DetailedParity);
public sealed record TowerSearchParity(string Id, TowerScenario Scenario, int Seed, string ExpectedPath);
public sealed record TowerSearchBenchmarkProtocol(int SchemaVersion, string Version, DateTimeOffset FrozenUtc,
    string AnchorId, string HarnessHash, IReadOnlyList<TowerSearchParity> Parity,
    IReadOnlyDictionary<string, string> FrozenFiles, int MaximumFights, int MaximumSeconds, long MaximumBytes,
    string Selection, string Screen, string Reliability, string Behavior, string Diagnostics);
public sealed record TowerSearchBenchmarkMetric(string Stage, double Seconds, int Started, int Completed);
public sealed record TowerSearchBenchmarkSummary(string Status, int Started, int Completed, double ExecuteSeconds,
    long RetainedBytes, TowerSearchScreen Screen, TowerSearchQuality? Quality, IReadOnlyList<TowerSearchBenchmarkMetric> Metrics,
    string Scope = "Independent search benchmark only. ScreenFailed is a planned early failure, not formal validation. No calibration, default promotion, complete Tower-family acceptance or near-optimality claim.");

/// <summary>One frozen three-arm search/screen/validation experiment over the existing compact execution machinery.</summary>
public static partial class TowerSearchBenchmark
{
    public const string Version = "tower-search-benchmark-v1";
    public const int MaximumFights = 28424;
    public const int MaximumSeconds = 1800;
    public const long MaximumBytes = 2147483648;
    public const string CompositionVersion = "tower-loadout-benchmark-v1";
    public const string CompositionReplicationVersion = "tower-loadout-replication-v1";
    private sealed record Design(string Version, string Policy, string[] Methods, int Discovery, int Shortlist, int Controls, int Fights, int Seconds);
    private static Design Study(bool composition, bool replication = false) => composition
        ? new(replication ? CompositionReplicationVersion : CompositionVersion, TowerBossGeneration.LoadoutCompositionVersion,
            TowerBossGeneration.LoadoutCompositionMethods, 18432, 12, replication ? 8 : 6, replication ? 24840 : 24200, 2700)
        : new(Version, TowerBossGeneration.DepthBehaviorVersion, TowerBossGeneration.DepthBehaviorMethods, 20736, 18, 6, MaximumFights, MaximumSeconds);
    private static Design Study(TowerBossDiscoveryDefinition d) => d.Generation.PolicyVersion switch {
        TowerBossGeneration.LoadoutCompositionVersion => Study(true, d.References.Count == 8),
        TowerBossGeneration.DepthBehaviorVersion => Study(false),
        _ => throw new InvalidDataException("Unsupported search benchmark policy.")
    };
    private static bool Composition(TowerSearchBenchmarkSelection selection) =>
        selection.Arms.Any(a => a.Method == TowerBossGeneration.LoadoutCompositionMethods[1]);
    private static string[] Methods(TowerSearchBenchmarkSelection selection) => Composition(selection)
        ? TowerBossGeneration.LoadoutCompositionMethods : TowerBossGeneration.DepthBehaviorMethods;
    private const string ProtocolFile = "protocol.json";
    private const string FinalFiles = "final-files.json";

    internal static void Validate(TowerBossDiscoveryDefinition d, string anchor)
    {
        var cost = TowerBossDiscovery.Validate(d); var design = Study(d);
        if (d.Mode != TowerBossDiscovery.Independent
            || d.Generation.CandidatesPerArm != 384 || d.Generation.Seeds.Count != 3 || d.Contexts.Count != 1
            || d.References.Count != design.Controls || d.References.All(r => r.Id != anchor) || d.MaximumBattles != design.Fights
            || cost.Discovery != design.Discovery || d.Stages.Shortlist != design.Shortlist || d.Stages.DiagnosticCandidates != 0 || d.Stages.ReplayReserve != 8
            || d.Stages.Schedules.Values.Any(s => s.Discovery.Count != 8 || s.Selection.Count != 64
                || s.Confirmation.Count != 256 || s.Diagnostics.Count != 0)
            || d.Generation.Seeds.Intersect(d.ExcludedCombatSeeds).Any()
            || d.Generation.Seeds.Intersect(d.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Confirmation))).Any())
            throw new InvalidDataException("Benchmark requires its declared candidate/control allocation, three restarts, 8/64/256 schedules and disjoint seeds.");
    }

    internal static int[] History(JsonElement ledger)
    {
        var seeds = new HashSet<int>();
        void Visit(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
                foreach (var child in element.EnumerateArray())
                {
                    if (child.ValueKind == JsonValueKind.Number) seeds.Add(child.GetInt32());
                    else Visit(child);
                }
            else if (element.ValueKind == JsonValueKind.Object)
                foreach (var property in element.EnumerateObject()) Visit(property.Value);
        }
        Visit(ledger);
        if (seeds.Count == 0 || seeds.Count > TowerStudyLimits.HistoricalSeeds - 331)
            throw new InvalidDataException("An explicit nonempty complete seed ledger within capacity is required.");
        return seeds.Order().ToArray();
    }

    public static TowerSearchBenchmarkProtocol Prepare(string root, string templatePath, string historyPath,
        string parityPath, string anchorId, int seed, string output, bool loadoutComposition = false, string? planPath = null,
        bool loadoutReplication = false)
    {
        output = Path.GetFullPath(output);
        using var lease = TowerCompactBundle.AcquireWriter(output);
        if (Path.Exists(output)) throw new IOException("Choose a new benchmark output directory.");
        if (loadoutReplication && !loadoutComposition) throw new InvalidDataException("Replication requires the unchanged loadout composition policy.");
        var design = Study(loadoutComposition, loadoutReplication);
        var template = TowerBossDiscovery.Read(templatePath);
        if (template.References.Count != design.Controls) throw new InvalidDataException("Template must contain every declared external control.");
        var settings = TowerBundle.ReadSettings(root);
        var historical = History(HarnessJson.Read<JsonElement>(historyPath)).Concat(template.ExcludedCombatSeeds)
            .Concat(template.Generation.Seeds).Concat(template.Stages.Schedules.Values.SelectMany(s =>
                s.Discovery.Concat(s.Selection).Concat(s.Confirmation).Concat(s.Diagnostics))).Distinct().Order().ToArray();
        var used = historical.ToHashSet();
        int[] Allocate(string label, int count)
        {
            var result = new List<int>();
            for (var attempt = 0; result.Count < count; attempt++)
            {
                if (attempt >= 100000) throw new InvalidDataException("Seed allocation exhausted its fixed limit.");
                var value = StableRandom.Seed(design.Version, seed.ToString(CultureInfo.InvariantCulture), label, attempt.ToString(CultureInfo.InvariantCulture));
                if (used.Add(value)) result.Add(value);
            }
            return result.ToArray();
        }
        var generation = Allocate("generation", 3);
        var schedule = new BossDiscoverySchedule(Allocate("discovery", 8), Allocate("screen", 64), Allocate("validation", 256), []);
        var d = template with {
            Mode = TowerBossDiscovery.Independent, Starts = [],
            Generation = new(design.Methods, generation, 384, 8192, 4,
                TowerBossDiscovery.Objective, design.Policy),
            Stages = new(design.Shortlist, 5, 0, 8, template.Contexts.ToDictionary(c => c.Id, _ => schedule)),
            ExcludedCombatSeeds = historical, MaximumBattles = design.Fights,
            SettingsHash = HarnessJson.Hash(settings), ExecutionHash = HarnessJson.Hash(ExecutionIdentity.Current())
        };
        Validate(d, anchorId); TowerBossDiscovery.Validate(root, d);
        var parity = HarnessJson.Read<TowerSearchParityInput[]>(parityPath).Where(p => p.DetailedParity).ToArray();
        if (parity.Length != 4 || parity.Select(p => p.Id).Distinct().Count() != 4
            || parity.Any(p => !d.References.Any(r => r.Id == p.Id && TowerBossDiscovery.RecipeHash(r.Scenario.Party) == TowerBossDiscovery.RecipeHash(p.Scenario.Party))))
            throw new InvalidDataException("Declare exactly four historical parity cases from the external controls.");
        if (loadoutComposition && (planPath is null || !File.Exists(planPath))) throw new InvalidDataException("Freeze the coordinated loadout study plan before combat.");
        Directory.CreateDirectory(output);
        string P(string name) => Path.Combine(output, name);
        if (planPath is not null) File.Copy(planPath, P("study-plan.md"));
        var copied = TowerBundle.CopyContent(root, P("content"), CancellationToken.None);
        if (HarnessJson.Hash(copied) != HarnessJson.Hash(d.ContentHashes)) throw new InvalidDataException("Content changed during preparation.");
        // Preserve only the non-secret settings consumed by offline combat readers.
        HarnessJson.WriteNew(P("content/appsettings.json"), new Dictionary<string, object> {
            ["Combat"] = new Dictionary<string, object> { ["ThreatAndTanking"] = settings.Threat,
                ["IdleProgression"] = new Dictionary<string, object> { ["EncounterCadenceSeconds"] = RunBundle.ReadCombatSettings(root).Cadence } },
            ["WorldTower"] = new Dictionary<string, object> { ["CombatTicksPerFrame"] = settings.CheckpointIntervalTicks }
        });
        HarnessJson.WriteNew(P("definition.json"), d);
        HarnessJson.WriteNew(P("seed-ledger.json"), new { historical, generation, discovery = schedule.Discovery,
            screen = schedule.Selection, validation = schedule.Confirmation });
        HarnessJson.WriteNew(P("input-provenance.json"), new { Template = Path.GetFullPath(templatePath), TemplateHash = HarnessJson.FileHash(templatePath),
            History = Path.GetFullPath(historyPath), HistoryHash = HarnessJson.FileHash(historyPath),
            Parity = Path.GetFullPath(parityPath), ParityHash = HarnessJson.FileHash(parityPath), HistoricalSeeds = historical.Length, TotalReservedSeeds = used.Count });
        var inputs = TowerBossDiscovery.GenerationInputs(d);
        if (HarnessJson.Hash(inputs) != HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(d with { References = [] })))
            throw new InvalidDataException("References crossed the independent generation boundary.");
        HarnessJson.WriteNew(P("generation-inputs.json"), inputs);
        HarnessJson.WriteNew(P("generation-mechanics.json"), TowerBossPartyGenerator.FromInventory(inputs, TowerBossInventory.Create(root, settings.Threat)));
        HarnessJson.WriteNew(P("executable-files.json"), TowerBossStudy.RetainExecutable(output, ExecutionIdentity.Current()));
        Directory.CreateDirectory(P("parity"));
        var retainedParity = parity.Select((p, i) => {
            var relative = "parity/expected-" + i + ".json";
            File.Copy(p.Reference, P(relative));
            return new TowerSearchParity(p.Id, p.Scenario, p.Seed, relative);
        }).ToArray();
        var protocol = new TowerSearchBenchmarkProtocol(1, design.Version, DateTimeOffset.UtcNow, anchorId,
            HarnessJson.FileHash(typeof(TowerSearchBenchmark).Assembly.Location), retainedParity, Inventory(output), design.Fights, design.Seconds, MaximumBytes,
            "Two finalists per arm by unchanged discovery rank; rank one primary, rank two exploratory. Exact canonical recipes deduplicate with every origin preserved; all six controls stay external. At most 24 cells.",
            "Proceed only if B or C has at least two frozen primaries with >=7/64 wins and observed difference from the fixed anchor >=-0.10. No primary replacement. Otherwise stop; do not spend or reassign validation reserve.",
            "All 24-or-fewer cells on 256 fresh validation seeds. B versus A and anchor, C versus B and anchor: 12 paired comparisons. Joint alpha .025/full rate family plus .025/24 discordance intervals. At least 2/3 primaries per candidate method must have rate lower>=.10, comparator difference lower>0, anchor difference lower>=-.10. Ordinary and joint family assessments remain separate; all observed >.50 are breaches.",
            "32 cells: four equal health-deficit bins [0,.25),[.25,.5),[.5,.75),[.75,1]; eight denial bins with inclusive upper bounds 0,30,100,300,1000,3000,10000,+infinity ticks. Best original rank in each cell, uniform parent sampling, no extra exploration pool. Denial includes hostile summons and duration exposure; bins are descriptors, not causal targets. A/B preserve v4 RNG streams at their respective budgets; C uses its own stream and unchanged v4 construction/operators.",
            "Four supplied historical detailed parity checks, then the three C primaries and fixed anchor on the first SCREEN seed, regardless of the screen decision. Exactly eight repeated diagnostic fights; no adaptive replays or retries.");
        if (loadoutComposition) protocol = protocol with {
            Selection = $"Two discovery-ranked finalists per arm, three paired restarts, 384 candidates per arm on eight paired discovery seeds. Up to {design.Shortlist + design.Controls} complete cells including all {design.Controls} external controls; rank-one primaries frozen before held-out combat.",
            Screen = "Measure all finalists and controls on 64 fresh seeds for descriptive comparison. Always execute the separately frozen 256-seed validation family; no adaptive early stopping or primary replacement.",
            Reliability = "Joint alpha .025 over the complete rate family and .025 over six paired comparisons (12 discordance intervals). At least two of three new primaries require rate lower>=.10, paired lower versus same-restart deep v4>0, paired lower versus fixed anchor>=-.10. Every observed rate>.50 remains a ceiling breach.",
            Behavior = "128 distinct ordered character loadouts retained by source-party discovery rank and ascending source slot, without standalone character fitness. Alternate coordinated distribution/composition/shared refinement/whole-loadout placement with v4 operators. Retain v4 initial construction, fresh frequency, parent selection, fitness and eight-seed sampling; both methods start with the same v4 RNG stream. Library contains only this arm's fully measured independent proposals. References, held-out results and saved counts cannot enter it.",
            Diagnostics = "Four historical detailed parity fights and first-screen-seed replays of the three new primaries plus fixed anchor. Eight fixed repeated checks, no adaptive diagnostics or retries."
        };
        HarnessJson.WriteNew(P(ProtocolFile), protocol);
        if (TowerBulkCampaign.StorageBytes(output) > MaximumBytes) throw new InvalidDataException("Preparation exceeds retained-storage cap.");
        return protocol;
    }

    private static Dictionary<string, string> Inventory(string output) => Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories)
        .Where(p => Path.GetRelativePath(output, p) != FinalFiles)
        .Order(StringComparer.Ordinal).ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash);

    private static TowerSearchBenchmarkProtocol VerifyInputs(string output)
    {
        var protocol = HarnessJson.Read<TowerSearchBenchmarkProtocol>(Path.Combine(output, ProtocolFile));
        var design = protocol.Version switch { Version => Study(false), CompositionVersion => Study(true), CompositionReplicationVersion => Study(true, true),
            _ => throw new InvalidDataException("Unsupported benchmark protocol.") };
        if (protocol.SchemaVersion != 1 || protocol.MaximumFights != design.Fights
            || protocol.MaximumSeconds != design.Seconds || protocol.MaximumBytes != MaximumBytes
            || protocol.Parity.Count != 4 || protocol.HarnessHash != HarnessJson.FileHash(typeof(TowerSearchBenchmark).Assembly.Location))
            throw new InvalidDataException("Benchmark requires its captured producing harness and supported frozen protocol.");
        foreach (var (name, hash) in protocol.FrozenFiles)
        {
            var path = Path.GetFullPath(name, output);
            if (!path.StartsWith(output + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || HarnessJson.FileHash(path) != hash) throw new InvalidDataException("Frozen benchmark input changed: " + name);
        }
        var d = TowerBossDiscovery.Read(Path.Combine(output, "definition.json"));
        if (Study(d) != design) throw new InvalidDataException("Protocol and generation policy differ.");
        Validate(d, protocol.AnchorId); TowerBossDiscovery.Validate(Path.Combine(output, "content"), d);
        var ledger = History(HarnessJson.Read<JsonElement>(Path.Combine(output, "seed-ledger.json")));
        var allocation = d.ExcludedCombatSeeds.Concat(d.Generation.Seeds).Concat(d.Stages.Schedules.Values.SelectMany(s =>
            s.Discovery.Concat(s.Selection).Concat(s.Confirmation))).Distinct().Order().ToArray();
        if (HarnessJson.Hash(ledger) != HarnessJson.Hash(allocation))
            throw new InvalidDataException("Seed ledger differs from the complete frozen allocation.");
        return protocol;
    }

    public static async Task<TowerSearchBenchmarkSummary> RunAsync(string output, CancellationToken token = default, Action<string>? progress = null)
    {
        output = Path.GetFullPath(output);
        using var lease = TowerCompactBundle.AcquireWriter(output);
        string P(string name) => Path.Combine(output, name);
        var protocol = VerifyInputs(output);
        var d = TowerBossDiscovery.Read(P("definition.json")); var design = Study(d);
        // Exclusive start marker forbids accidental retries and silent continuation after interruption.
        HarnessJson.WriteNew(P("started.json"), new { Utc = DateTimeOffset.UtcNow, ProtocolHash = HarnessJson.FileHash(P(ProtocolFile)) });
        var clock = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(protocol.MaximumSeconds));
        var started = 0; var completed = 0; var phase = "initialization";
        var metrics = new List<TowerSearchBenchmarkMetric>();
        void CheckStorage()
        {
            timeout.Token.ThrowIfCancellationRequested();
            if (TowerBulkCampaign.StorageBytes(output, timeout.Token) > MaximumBytes)
                throw new InvalidDataException("Benchmark retained-storage cap reached.");
        }
        var trace = new TowerPerformanceTrace(done => {
            if (!done)
            {
                timeout.Token.ThrowIfCancellationRequested();
                if (started >= protocol.MaximumFights) throw new InvalidDataException("Benchmark combat cap exhausted.");
                if (started % 128 == 0) CheckStorage();
            }
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { stage = phase, kind = done ? "completed" : "started", ordinal = done ? completed + 1 : started + 1 }) + "\n");
            using var journal = new FileStream(P("battle-attempts.jsonl"), FileMode.Append, FileAccess.Write, FileShare.Read);
            journal.Write(bytes); journal.Flush(true);
            if (done) completed++; else started++;
        });
        using var active = trace.Activate();
        async Task<T> Phase<T>(string name, Func<Task<T>> work)
        {
            phase = name; var beforeStarted = started; var beforeCompleted = completed; var watch = Stopwatch.StartNew();
            try { return await work(); }
            finally
            {
                var metric = new TowerSearchBenchmarkMetric(name, watch.Elapsed.TotalSeconds, started - beforeStarted, completed - beforeCompleted);
                metrics.Add(metric); HarnessJson.WriteNew(P("metric-" + name + ".json"), metric);
                progress?.Invoke($"{name}: {metric.Started} fights, {metric.Seconds:F2} seconds.");
            }
        }
        TowerBulkOptions Options() => new(32, 0, Math.Max(1, (int)(protocol.MaximumSeconds - clock.Elapsed.TotalSeconds)),
            Math.Max(1048576, MaximumBytes - TowerBulkCampaign.StorageBytes(output)));
        try
        {
            var settings = TowerBundle.ReadSettings(P("content"));
            var runner = new TowerBattleRunner(P("content"), new OfflineContent(P("content"), settings.Threat));
            foreach (var check in protocol.Parity)
            {
                var report = await Phase("parity-" + check.Id, () => runner.RunAsync(runner.CreateInput(
                    check.Scenario with { Seeds = [check.Seed] }, check.Seed, settings.Threat, settings.CheckpointIntervalTicks), true, timeout.Token));
                if (HarnessJson.Hash(report) != HarnessJson.Hash(HarnessJson.Read<TowerBattleReport>(P(check.ExpectedPath))))
                    throw new InvalidDataException("Historical detailed combat parity differs.");
            }
            var discovery = await Phase("discovery", () => TowerCompactDiscovery.RunAsync(P("content"), P("discovery"), d, Options(),
                token: timeout.Token, progress: message => {
                    if (message.StartsWith("Compact discovery:", StringComparison.Ordinal)
                        && int.Parse(message.Split(' ')[2], CultureInfo.InvariantCulture) % 64 == 0) progress?.Invoke(message);
                }));
            if (discovery.ActualBattles != design.Discovery) throw new InvalidDataException("Discovery did not complete its frozen allocation.");
            var selection = Select(d, discovery); HarnessJson.WriteNew(P("selected-family.json"), selection);
            var screenDefinition = FamilyDefinition(d, selection, validation: false);
            var validationDefinition = FamilyDefinition(d, selection, validation: true);
            // Freeze both before seeing any screen outcome, even if validation will remain unused.
            HarnessJson.WriteNew(P("screen-definition.json"), screenDefinition);
            HarnessJson.WriteNew(P("validation-definition.json"), validationDefinition);
            var screenReport = await Phase("screen", () => TowerCompactBalanceRun.RunAsync(P("content"), P("screen"), screenDefinition, Options(), token: timeout.Token));
            var screen = Screen(selection, protocol.AnchorId, screenReport); HarnessJson.WriteNew(P("screen-decision.json"), screen);
            var replayIds = selection.Arms.Where(a => a.Method == design.Methods[^1]).Select(a => a.Primary)
                .Append(Anchor(selection, protocol.AnchorId)).ToArray();
            HarnessJson.WriteNew(P("replay-selection.json"), replayIds);
            var sources = HarnessJson.Read<TowerBalanceRunSource[]>(P("screen/sources.json"));
            for (var i = 0; i < replayIds.Length; i++)
            {
                var source = sources.Single(s => s.CellId == replayIds[i]);
                var replay = await Phase("screen-replay-" + i, () => TowerCompactBundle.ReplayAsync(
                    Path.GetFullPath(source.RunDirectory, P("screen")), source.CompactCaseId!, "tower.0001", true, timeout.Token));
                HarnessJson.WriteNew(P("screen-replay-" + i + ".json"), replay);
            }
            TowerSearchQuality? quality = null;
            if (screen.ContinueValidation)
            {
                await Phase("validation", () => TowerCompactBalanceRun.RunAsync(P("content"), P("validation"), validationDefinition, Options(), token: timeout.Token));
                quality = Quality(selection, protocol.AnchorId, HarnessJson.Read<TowerBalanceEvidence[]>(P("validation/evidence.json")));
                HarnessJson.WriteNew(P("quality.json"), quality);
            }
            var expected = design.Discovery + selection.Family.Count * (64 + (screen.ContinueValidation ? 256 : 0)) + 8;
            if (started != expected || completed != started) throw new InvalidDataException("Global battle accounting differs.");
            VerifyInputs(output); CheckStorage();
            var summary = new TowerSearchBenchmarkSummary(screen.ContinueValidation ? "Complete" : "ScreenFailed", started, completed,
                clock.Elapsed.TotalSeconds, TowerBulkCampaign.StorageBytes(output), screen, quality, metrics);
            HarnessJson.WriteNew(P("summary.json"), summary);
            HarnessJson.WriteNew(P("saved-builds.json"), new { selection.Family, Screen = screenReport.Cells,
                Validation = screen.ContinueValidation ? HarnessJson.Read<TowerBalanceReport>(P("validation/assessment.json")).Cells : null,
                Quality = quality, d.ContentHashes, d.SettingsHash, d.ExecutionHash });
            File.WriteAllText(P("report.md"), Markdown(summary, selection, protocol.MaximumFights), new UTF8Encoding(false));
            HarnessJson.WriteNew(P(FinalFiles), Inventory(output));
            CheckStorage(); return summary;
        }
        catch (Exception error)
        {
            HarnessJson.WriteNew(P("failure.json"), new { Status = "InvalidOrInterrupted", Started = started, Completed = completed,
                Error = error.Message, Metrics = metrics, NoRetry = true });
            throw;
        }
    }

    public static async Task<TowerSearchBenchmarkSummary> VerifyAsync(string output, CancellationToken token = default)
    {
        output = Path.GetFullPath(output); string P(string name) => Path.Combine(output, name);
        using var lease = TowerCompactBundle.AcquireWriter(output);
        var protocol = VerifyInputs(output);
        if (HarnessJson.Hash(Inventory(output)) != HarnessJson.Hash(HarnessJson.Read<Dictionary<string, string>>(P(FinalFiles))))
            throw new InvalidDataException("Completed benchmark inventory changed.");
        var marker = HarnessJson.Read<JsonElement>(P("started.json"));
        if (marker.GetProperty("protocolHash").GetString() != HarnessJson.FileHash(P(ProtocolFile))) throw new InvalidDataException("Started protocol hash differs.");
        using var noCombat = new TowerPerformanceTrace(_ => throw new InvalidDataException("Verification must not run combat.")).Activate();
        var d = TowerBossDiscovery.Read(P("definition.json")); var design = Study(d);
        var discovery = await TowerCompactDiscovery.VerifyAsync(P("discovery"), token);
        var selection = Select(d, discovery);
        void Equal<T>(string name, T result)
        { if (HarnessJson.Hash(result) != HarnessJson.Hash(HarnessJson.Read<T>(P(name)))) throw new InvalidDataException("Reconstruction differs: " + name); }
        Equal("selected-family.json", selection);
        Equal("screen-definition.json", FamilyDefinition(d, selection, false));
        Equal("validation-definition.json", FamilyDefinition(d, selection, true));
        var screenReport = await TowerCompactBalanceRun.VerifyAsync(P("screen"), token);
        var screen = Screen(selection, protocol.AnchorId, screenReport); Equal("screen-decision.json", screen);
        var replayIds = selection.Arms.Where(a => a.Method == design.Methods[^1]).Select(a => a.Primary)
            .Append(Anchor(selection, protocol.AnchorId)).ToArray();
        Equal("replay-selection.json", replayIds);
        var replaySources = HarnessJson.Read<TowerBalanceRunSource[]>(P("screen/sources.json"));
        for (var i = 0; i < replayIds.Length; i++)
        {
            var source = replaySources.Single(s => s.CellId == replayIds[i]); TowerBattleReport? compact = null;
            TowerCompactBundle.Verify(Path.GetFullPath(source.RunDirectory, P("screen")), token, visit: (caseId, trial) => {
                if (caseId == source.CompactCaseId && trial.Id == "tower.0001") compact = trial.Report;
            });
            var replay = HarnessJson.Read<TowerBattleReport>(P("screen-replay-" + i + ".json"));
            if (compact is null || replay.Battle.Seed != compact.Battle.Seed || replay.Battle.ScenarioId != compact.Battle.ScenarioId
                || replay.Succeeded != compact.Succeeded || replay.GuardianHealthRemainingPercent != compact.GuardianHealthRemainingPercent
                || replay.DisplayDurationSeconds != compact.DisplayDurationSeconds || HarnessJson.Hash(replay.Battle.Summary) != HarnessJson.Hash(compact.Battle.Summary))
                throw new InvalidDataException("Fixed detailed replay differs from its recorded compact trial.");
        }
        TowerSearchQuality? quality = null;
        if (screen.ContinueValidation)
        {
            await TowerCompactBalanceRun.VerifyAsync(P("validation"), token);
            quality = Quality(selection, protocol.AnchorId, HarnessJson.Read<TowerBalanceEvidence[]>(P("validation/evidence.json")));
            Equal("quality.json", quality);
        }
        else if (Directory.Exists(P("validation")) || File.Exists(P("quality.json")))
            throw new InvalidDataException("Failed screen must leave validation unused.");
        var summary = HarnessJson.Read<TowerSearchBenchmarkSummary>(P("summary.json"));
        var expected = design.Discovery + selection.Family.Count * (64 + (screen.ContinueValidation ? 256 : 0)) + 8;
        if (summary.Status != (screen.ContinueValidation ? "Complete" : "ScreenFailed") || summary.Started != expected
            || summary.Completed != expected || expected > protocol.MaximumFights || summary.ExecuteSeconds > protocol.MaximumSeconds
            || HarnessJson.Hash(summary.Screen) != HarnessJson.Hash(screen) || HarnessJson.Hash(summary.Quality) != HarnessJson.Hash(quality)
            || TowerBulkCampaign.StorageBytes(output) > MaximumBytes)
            throw new InvalidDataException("Summary, screen decision or resource accounting differs.");
        var started = 0; var completed = 0; string? activeStage = null;
        foreach (var line in File.ReadLines(P("battle-attempts.jsonl")))
        {
            token.ThrowIfCancellationRequested();
            using var row = JsonDocument.Parse(line); var value = row.RootElement;
            var stage = value.GetProperty("stage").GetString(); var ordinal = value.GetProperty("ordinal").GetInt32();
            if (value.GetProperty("kind").GetString() == "started" && started == completed && ordinal == started + 1)
            { started++; activeStage = stage; }
            else if (value.GetProperty("kind").GetString() == "completed" && started == completed + 1 && ordinal == completed + 1 && activeStage == stage)
                completed++;
            else throw new InvalidDataException("Durable battle journal contains an invalid, repeated or unfinished attempt.");
        }
        if (started != expected || completed != expected || summary.Metrics.Sum(m => m.Started) != expected || summary.Metrics.Sum(m => m.Completed) != expected)
            throw new InvalidDataException("Durable journal does not reconcile with phase and total accounting.");
        var expectedPhases = protocol.Parity.ToDictionary(p => "parity-" + p.Id, _ => 1);
        expectedPhases.Add("discovery", design.Discovery); expectedPhases.Add("screen", selection.Family.Count * 64);
        for (var i = 0; i < 4; i++) expectedPhases.Add("screen-replay-" + i, 1);
        if (screen.ContinueValidation) expectedPhases.Add("validation", selection.Family.Count * 256);
        if (!summary.Metrics.Select(m => m.Stage).Order().SequenceEqual(expectedPhases.Keys.Order())
            || summary.Metrics.Any(m => m.Started != expectedPhases[m.Stage] || m.Completed != m.Started || !double.IsFinite(m.Seconds) || m.Seconds < 0))
            throw new InvalidDataException("Phase accounting differs from the declared experiment.");
        Equal("saved-builds.json", new { selection.Family, Screen = screenReport.Cells,
            Validation = screen.ContinueValidation ? HarnessJson.Read<TowerBalanceReport>(P("validation/assessment.json")).Cells : null,
            Quality = quality, d.ContentHashes, d.SettingsHash, d.ExecutionHash });
        if (File.ReadAllText(P("report.md")) != Markdown(summary, selection, protocol.MaximumFights)) throw new InvalidDataException("Markdown report differs from reconstruction.");
        return summary;
    }

    private static string Markdown(TowerSearchBenchmarkSummary summary, TowerSearchBenchmarkSelection selection, int maximumFights)
    {
        var text = new StringBuilder(Composition(selection) ? "# Coordinated loadout search comparison\n\n" : "# Sustained independent search comparison\n\n");
        text.AppendLine($"Execution result: **{summary.Status}**. Actual fights: **{summary.Completed:N0} / {maximumFights:N0}**. {summary.Scope}\n");
        text.AppendLine("| Method | Restart | Screen primary wins / 64 | Screen qualifies |\n| --- | ---: | ---: | --- |");
        foreach (var arm in summary.Screen.Arms) text.AppendLine($"| {arm.Method} | {arm.Seed} | {arm.Wins}/64 | {arm.Qualifies} |");
        if (summary.Quality is { } quality)
        {
            text.AppendLine("\n| Method | Passing restarts | Reliability |\n| --- | ---: | --- |");
            foreach (var method in quality.Methods) text.AppendLine($"| {method.Method} | {method.PassingRestarts}/3 | {method.Reliability} |");
            text.AppendLine($"\nJoint family: **{quality.JointFamilyAssessment}**. {quality.Scope}");
        }
        else text.AppendLine("\nThe predeclared screen failed. Validation was not run; there is no new formal reliability or validation-family assessment.");
        text.AppendLine($"\nAll {selection.Family.Count} seed-free recipes and source associations are in `selected-family.json`; measurements are in `saved-builds.json`. Raw discovery, screen and any validation evidence remain in their compact campaigns. Existing ceiling breaches remain unresolved.\n");
        text.AppendLine("Verify without new fights using the captured executable: `dotnet executable/BalanceHarness.dll tower-search-benchmark-verify --run <this-directory>`.");
        return text.ToString();
    }
}
