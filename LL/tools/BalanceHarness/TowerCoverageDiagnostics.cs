using System.Text.Json;

namespace BalanceHarness;

public sealed record CoverageHashedFile(string Path, string Hash);
public sealed record CoverageArchiveInput(string Id, string Directory, string ManifestHash, string ProducingExecutionHash,
    string Compatibility);
public sealed record CoverageReplayInput(string Id, string ArchiveId, string BatchId, string CaseId, int Seed, CoverageHashedFile File);
public sealed record TowerCoverageDiagnosticsDefinition(int SchemaVersion, IReadOnlyList<CoverageArchiveInput> Archives,
    IReadOnlyList<CoverageReplayInput> Replays, CoverageHashedFile SeedLedger, string RepositoryRoot,
    IReadOnlyDictionary<string, string> SourceHashes, string ReportingExecutionHash,
    int MaximumSeconds = 300, long MaximumBytes = 268435456, int WindowSeconds = 10);

/// <summary>Read-only diagnostics over completed compact archives. No search, seed allocation, replay or battle executor.</summary>
public static class TowerCoverageDiagnostics
{
    public const string Schema = "tower-coverage-diagnostics-v1";
    public const string HistoricalCompatibility = "same-game-assemblies-v1";
    public static readonly string[] Limitations = [
        "Descriptive saved evidence only; no new combats, party-constructor calls, seed allocation, fitness input or confidence calculation.",
        "Authored routes and equipped providers do not establish recipient reach, uptime, potency, marginal prevention or causal contribution.",
        "Nonzero application logs are observations. Missing detail differs from no observed application; neither establishes inactivity.",
        "Direct equipped ability routes compete for attribution by credited owner. Nested status/summon/equipment effects are not attributed to an Essence; unmatched and ambiguous applications remain visible.",
        "Applied stagger is capped contribution, not a completed break or denied action. Broken/recovering targets reject contributions; recovery, threshold growth and maximum breaks are separate gates. No aggregate chance is inferred.",
        "Non-periodic ApplyCondition has its authored chance gate; non-guaranteed Stun/Freeze additionally has an 80% runtime gate. Unstoppable, Ward, predicates and target availability can prevent application. Guaranteed application bypasses only the runtime control gate/Unstoppable, not every authored gate.",
        "Healing and regeneration report actual restored health separately. Regeneration modifiers receive no per-provider marginal credit. Damage may include overkill.",
        "Windows are elapsed time with exclusive upper bounds; before-first-death excludes equal-tick events. Different first-death times are unequal exposure durations.",
        "Saved generation intents contain category counts, not nominated provider IDs. No provider identity is inferred from placements or final recipes.",
        "Historical compatibility is explicit: identical game assemblies, runtime, OS and architecture; the reporting harness may differ. Retained producing executables are verified, never executed. No generator reconstruction or renewed scientific gate validation is performed."
    ];

    public static void Run(string definitionPath, string output, CancellationToken token = default)
    {
        definitionPath = Path.GetFullPath(definitionPath); output = Path.GetFullPath(output);
        var d = TowerContractJson.Read<TowerCoverageDiagnosticsDefinition>(definitionPath);
        if (d.SchemaVersion != 1 || d.Archives is not { Count: >= 1 and <= 8 } || d.Replays is null || d.Replays.Count > 64
            || d.Archives.Select(a => a.Id).Distinct().Count() != d.Archives.Count || d.Replays.Select(r => r.Id).Distinct().Count() != d.Replays.Count
            || d.MaximumSeconds is < 1 or > 3600 || d.MaximumBytes is < 1048576 or > 1073741824 || d.WindowSeconds is < 1 or > 60
            || d.SourceHashes is not { Count: > 0 } || d.ReportingExecutionHash != HarnessJson.Hash(ExecutionIdentity.Current()))
            throw new InvalidDataException("Invalid bounded coverage diagnostics definition or reporting execution.");
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        cancellation.CancelAfter(TimeSpan.FromSeconds(d.MaximumSeconds)); token = cancellation.Token;
        using var lease = TowerCompactBundle.AcquireWriter(output);
        if (Path.Exists(output)) throw new IOException("Choose a new coverage diagnostics output directory.");
        var baseDirectory = Path.GetDirectoryName(definitionPath)!;
        string Resolve(string path) => Path.GetFullPath(path, baseDirectory);
        var repository = Resolve(d.RepositoryRoot);
        foreach (var (path, hash) in d.SourceHashes) VerifyFile(new(SafeChild(repository, path), hash));
        var definitionHash = HarnessJson.FileHash(definitionPath);
        var ledgerPath = Resolve(d.SeedLedger.Path); VerifyFile(d.SeedLedger with { Path = ledgerPath });
        var ledger = TowerContractJson.Read<Dictionary<string, int[]>>(ledgerPath);
        if (ledger.Count == 0 || ledger.Values.Any(v => v == null)) throw new InvalidDataException("Expected an all-array seed ledger.");
        var excluded = ledger.Values.SelectMany(v => v).ToHashSet();
        var contracts = new Dictionary<string, TowerBulkContract>(StringComparer.Ordinal);
        var archiveRoots = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var a in d.Archives)
        {
            token.ThrowIfCancellationRequested();
            var root = Resolve(a.Directory); archiveRoots.Add(a.Id, root);
            if (output.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || root == output)
                throw new InvalidDataException("Diagnostics cannot write inside an input archive.");
            VerifyFile(new(Path.Combine(root, "campaign-manifest.json"), a.ManifestHash));
            TowerBulkCampaign.VerifyFiles(root, "campaign-manifest.json", true, token);
            TowerBulkCampaign.VerifyFiles(root, "campaign-frozen-files.json", false, token);
            var c = TowerContractJson.Read<TowerBulkContract>(Path.Combine(root, TowerBulkCampaign.ContractFile));
            if (c.SchemaVersion != 1 || c.Kind is not (TowerCompactDiscovery.Kind or TowerCompactBalanceRun.Kind)
                || c.Options.ExecutionMode != TowerPreparedBattle.Mode || c.Scope.ReportStorage != TowerCompactBundle.Format)
                throw new InvalidDataException("Only completed compact discovery/balance schema 1 with prepared-v1 is supported.");
            if (HarnessJson.Hash(c.Definition) != HarnessJson.Hash(TowerContractJson.Read<JsonElement>(Path.Combine(root, "definition.json"))))
                throw new InvalidDataException("Campaign definition copy differs.");
            VerifyExecution(c.Scope.Execution, ExecutionIdentity.Current(), a.ProducingExecutionHash, a.Compatibility);
            var executable = TowerContractJson.Read<Dictionary<string, string>>(Path.Combine(root, "executable-files.json"));
            foreach (var (name, hash) in executable) VerifyFile(new(SafeChild(Path.Combine(root, "executable"), name), hash));
            if (c.Scope.Execution.AssemblyHashes.Any(p => executable.GetValueOrDefault(p.Key + ".dll") != p.Value))
                throw new InvalidDataException("Retained producing executable does not match its scope.");
            if (HarnessJson.Hash(TowerCompactBundle.ContentHashes(Path.Combine(root, "content"), token)) != HarnessJson.Hash(c.Scope.ContentHashes)
                || c.Definition.GetProperty("contentHashes").Deserialize<Dictionary<string, string>>(HarnessJson.Options) is not { } hashes
                || HarnessJson.Hash(hashes) != HarnessJson.Hash(c.Scope.ContentHashes)
                || c.Definition.GetProperty("executionHash").GetString() != HarnessJson.Hash(c.Scope.Execution)
                || c.Definition.GetProperty("settingsHash").GetString() != HarnessJson.Hash(c.Scope.Settings))
                throw new InvalidDataException("Campaign frozen content, execution or settings mismatch.");
            contracts.Add(a.Id, c);
        }
        if (contracts.Values.Select(c => HarnessJson.Hash(new { c.Scope.ContentHashes, c.Scope.Settings, c.Scope.Execution })).Distinct().Count() != 1)
            throw new InvalidDataException("This schema requires one shared frozen content/settings/producing identity across archives.");
        var discovery = contracts.Where(p => p.Value.Kind == TowerCompactDiscovery.Kind).ToArray();
        if (discovery.Length != 1) throw new InvalidDataException("Exactly one saved discovery definition supplies the eligible content budget.");
        var discoveryDefinition = discovery[0].Value.Definition.Deserialize<TowerBossDiscoveryDefinition>(HarnessJson.Options)!;
        var inputs = TowerBossDiscovery.GenerationInputs(discoveryDefinition); // Detached metadata only; no generator construction.
        var contentRoot = Path.Combine(archiveRoots[discovery[0].Key], "content");
        var inventory = TowerBossInventory.Create(contentRoot, discovery[0].Value.Scope.Settings.Threat);
        var baseline = TowerAttributeDefense.Create(inputs, inventory);
        var compatible = TowerProtectionCompatibility.Create(inputs, inventory);
        var routes = TowerCoverageDiagnosticMechanics.Routes(inventory, baseline, compatible.Coverage);
        var generation = TowerContractJson.Read<JsonElement>(Path.Combine(archiveRoots[discovery[0].Key], "discovery.json")).GetProperty("generation");
        var origins = generation.GetProperty("arms").EnumerateArray().SelectMany(arm => arm.GetProperty("proposals").EnumerateArray()
            .Where(p => p.GetProperty("result").GetString() == "evaluated").Select(p => new {
                Method = arm.GetProperty("method").GetString(), GenerationSeed = arm.GetProperty("seed").GetInt32(),
                PartyId = p.GetProperty("party").GetProperty("id").GetString(), Intent = p.GetProperty("intent").GetString(),
                Provenance = p.GetProperty("provenance"), Builds = p.GetProperty("party").GetProperty("builds").Deserialize<Dictionary<int, string[]>>(HarnessJson.Options)!
            })).ToArray();
        var originKeysSeen = new HashSet<string>();
        var replayTrials = new Dictionary<string, TowerBattleReport>(StringComparer.Ordinal);
        var cases = new List<object>(); var archiveSummaries = new List<object>(); var trialCount = 0;
        foreach (var a in d.Archives.OrderBy(a => a.Id, StringComparer.Ordinal))
        {
            var root = archiveRoots[a.Id]; var contract = contracts[a.Id]; var count = 0; var caseCount = 0;
            var sources = contract.Kind == TowerCompactBalanceRun.Kind
                ? TowerContractJson.Read<TowerBalanceRunSource[]>(Path.Combine(root, "sources.json")) : [];
            var sourceKeysSeen = new HashSet<string>();
            var batches = Directory.GetDirectories(Path.Combine(root, "batches")).Order(StringComparer.Ordinal).ToArray();
            var stem = contract.Kind == TowerCompactDiscovery.Kind ? "discovery-" : "confirmation-";
            if (!batches.Select(Path.GetFileName).SequenceEqual(Enumerable.Range(0, batches.Length).Select(i => stem + i.ToString("D6", System.Globalization.CultureInfo.InvariantCulture))))
                throw new InvalidDataException("Campaign batch sequence changed.");
            foreach (var batch in batches)
            {
                token.ThrowIfCancellationRequested();
                var batchId = Path.GetFileName(batch); var metrics = new Dictionary<string, List<CoverageTrialMetrics>>();
                var saved = TowerCompactBundle.Verify(batch, token, visit: (id, trial) => {
                    if (!excluded.Contains(trial.Seed)) throw new InvalidDataException("Saved combat seed missing from supplied all-array exclusion ledger.");
                    if (!metrics.TryGetValue(id, out var rows)) metrics.Add(id, rows = []);
                    rows.Add(TowerCoverageDiagnosticReplay.Metrics(trial.Report));
                    foreach (var replay in d.Replays.Where(r => r.ArchiveId == a.Id && r.BatchId == batchId && r.CaseId == id && r.Seed == trial.Seed))
                        replayTrials.Add(replay.Id, trial.Report);
                });
                if (saved.Plan.SharedContentPath != "../../content" || saved.Plan.ExecutionMode != contract.Options.ExecutionMode
                    || HarnessJson.Hash(new { saved.Scope.ContentHashes, saved.Scope.Settings, saved.Scope.Execution })
                        != HarnessJson.Hash(new { contract.Scope.ContentHashes, contract.Scope.Settings, contract.Scope.Execution }))
                    throw new InvalidDataException("Batch scope differs from verified campaign.");
                foreach (var c in saved.Plan.Cases)
                {
                    var scenario = saved.Scenarios[c.Id]; var rows = metrics[c.Id]; count += rows.Count; caseCount++;
                    var partyOrigins = origins.Where(o =>
                        BuildKey(o.Builds) == BuildKey(scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds.ToArray()))).ToArray();
                    if (contract.Kind == TowerCompactDiscovery.Kind && partyOrigins.Length == 0)
                        throw new InvalidDataException("Saved discovery recipe has no recorded evaluated proposal.");
                    if (contract.Kind == TowerCompactDiscovery.Kind)
                        foreach (var o in partyOrigins) originKeysSeen.Add(HarnessJson.Hash(o));
                    var mapped = sources.Where(s => s.RunDirectory == "batches/" + batchId && s.CompactCaseId == c.Id).ToArray();
                    if (contract.Kind == TowerCompactBalanceRun.Kind && mapped.Length != 1) throw new InvalidDataException("Validation source mapping is missing or ambiguous.");
                    JsonElement? cell = null;
                    if (mapped.Length == 1)
                    {
                        sourceKeysSeen.Add(mapped[0].CellId);
                        cell = contract.Definition.GetProperty("cells").EnumerateArray().Single(x => x.GetProperty("id").GetString() == mapped[0].CellId);
                        if (HarnessJson.Hash(cell.Value.GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)) != HarnessJson.Hash(scenario))
                            throw new InvalidDataException("Validation source recipe differs from its frozen family cell.");
                    }
                    cases.Add(new { ArchiveId = a.Id, BatchId = batchId, CaseId = c.Id, scenario.Id, scenario.FloorNumber,
                        RecipeHash = c.RecipeHash, Origins = partyOrigins, ValidationCell = cell,
                        Samples = rows.Count, Wins = rows.Count(r => r.Won), MedianFirstDeathSeconds = Median(rows.Where(r => r.FirstDeathSeconds != null).Select(r => r.FirstDeathSeconds!.Value)),
                        BaselineExposure = TowerCoverageDiagnosticMechanics.Exposure(scenario, routes, false),
                        CompatibleExposure = TowerCoverageDiagnosticMechanics.Exposure(scenario, routes, true), Trials = rows,
                        DetailedEvidence = d.Replays.Where(r => r.ArchiveId == a.Id && r.BatchId == batchId && r.CaseId == c.Id).Select(r => r.Id).Order(StringComparer.Ordinal).ToArray(),
                        MissingDetailSamples = rows.Count - d.Replays.Where(r => r.ArchiveId == a.Id && r.BatchId == batchId && r.CaseId == c.Id).Select(r => r.Seed).Distinct().Count() });
                }
            }
            if (sources.Length != sourceKeysSeen.Count || contract.Kind == TowerCompactBalanceRun.Kind
                && contract.Definition.GetProperty("cells").GetArrayLength() != sourceKeysSeen.Count)
                throw new InvalidDataException("Unvisited or duplicate validation source/cell.");
            var accounting = TowerContractJson.Read<TowerBulkAccounting>(Path.Combine(root, "campaign-accounting.json"));
            if (count != contract.PlannedBattles || accounting.LogicalTrials != count || accounting.ChargedAttempts != batches.Sum(TowerCompactBundle.AttemptCount)
                || accounting.RetryOrUncommittedAttempts != accounting.ChargedAttempts - count || accounting.MaximumAttempts != contract.MaximumAttempts)
                throw new InvalidDataException("Campaign accounting differs from verified records/journals.");
            archiveSummaries.Add(new { a.Id, contract.Kind, a.ManifestHash, a.ProducingExecutionHash, a.Compatibility,
                contract.Scope, SavedCases = caseCount, SavedTrials = count, Accounting = accounting }); trialCount += count;
        }
        if (originKeysSeen.Count != origins.Length) throw new InvalidDataException("Some evaluated discovery proposals have no saved records.");
        if (replayTrials.Count != d.Replays.Count) throw new InvalidDataException("A requested replay has no matching saved trial.");
        var replays = new List<object>();
        foreach (var r in d.Replays.OrderBy(r => r.Id, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested(); var path = Resolve(r.File.Path); VerifyFile(r.File with { Path = path });
            var replay = TowerContractJson.Read<TowerBattleReport>(path);
            TowerCoverageDiagnosticReplay.VerifyReplay(replayTrials[r.Id], replay);
            replays.Add(new { r.Id, r.ArchiveId, r.BatchId, r.CaseId, r.File.Hash,
                Evidence = TowerCoverageDiagnosticReplay.Analyze(replay, inventory, routes, d.WindowSeconds) });
        }
        // Recheck immutable inputs after interpretation, before publishing a complete receipt.
        foreach (var a in d.Archives) TowerBulkCampaign.VerifyFiles(archiveRoots[a.Id], "campaign-manifest.json", true, token);
        foreach (var r in d.Replays) VerifyFile(r.File with { Path = Resolve(r.File.Path) });
        foreach (var (path, hash) in d.SourceHashes) VerifyFile(new(SafeChild(repository, path), hash));
        VerifyFile(d.SeedLedger with { Path = ledgerPath }); VerifyFile(new(definitionPath, definitionHash));
        Directory.CreateDirectory(output);
        void Write<T>(string name, T value)
        {
            token.ThrowIfCancellationRequested(); HarnessJson.WriteNew(Path.Combine(output, name), value);
            if (TowerBulkCampaign.StorageBytes(output, token) > d.MaximumBytes) throw new InvalidDataException("Diagnostic output cap exceeded; no complete receipt written.");
        }
        Write("definition.json", d);
        File.Copy(ledgerPath, Path.Combine(output, "seed-ledger.json"), false); // Preserve exact bytes and every array, including unused reservations.
        Write("mechanics.json", new { BaselineFeatures = baseline, Compatibility = compatible,
            BaselineCategories = TowerCoverageDiagnosticMechanics.Categories(routes, false), CompatibleCategories = TowerCoverageDiagnosticMechanics.Categories(routes, true),
            Routes = routes, Boss = inventory.Bosses.Single(b => b.FloorNumber == inputs.Floor), Inventory = inventory });
        Write("cases.json", cases); Write("replays.json", replays); Write("saved-generation.json", generation);
        Write("report.json", new { Schema, Status = "VerifiedDescriptiveEvidence", NewCombats = 0, ConstructorCalls = 0, NewSeeds = 0,
            DefinitionHash = definitionHash, ReportingExecution = ExecutionIdentity.Current(), d.SourceHashes,
            SeedLedgerHash = d.SeedLedger.Hash, ExclusionUnion = excluded.Count, SavedTrials = trialCount, SavedDetailedReplays = replays.Count,
            Archives = archiveSummaries, Limits = new { d.MaximumSeconds, d.MaximumBytes, d.WindowSeconds }, Limitations });
        Write("manifest.json", Directory.GetFiles(output).Order(StringComparer.Ordinal).ToDictionary(p => Path.GetFileName(p), HarnessJson.FileHash));
    }

    internal static double? Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        return sorted.Length == 0 ? null : (sorted[(sorted.Length - 1) / 2] + sorted[sorted.Length / 2]) / 2;
    }
    private static string BuildKey(IReadOnlyDictionary<int, string[]> builds) => HarnessJson.Hash(builds.OrderBy(p => p.Key).Select(p => new { Slot = p.Key, Ids = p.Value }));
    internal static string SafeChild(string root, string relative)
    {
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (Path.IsPathRooted(relative) || !path.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Diagnostic manifest path escapes its root.");
        return path;
    }
    internal static void VerifyFile(CoverageHashedFile file)
    {
        if (!TowerContractJson.Hash(file.Hash) || !File.Exists(file.Path) || HarnessJson.FileHash(file.Path) != file.Hash)
            throw new InvalidDataException("Changed or missing diagnostic input: " + file.Path);
    }
    internal static void VerifyExecution(ExecutionIdentity producing, ExecutionIdentity reporting, string expectedHash, string compatibility)
    {
        if (HarnessJson.Hash(producing) != expectedHash || compatibility != HistoricalCompatibility
            || producing.Runtime != reporting.Runtime || producing.OperatingSystem != reporting.OperatingSystem || producing.Architecture != reporting.Architecture
            || !producing.AssemblyHashes.Keys.Order(StringComparer.Ordinal).SequenceEqual(reporting.AssemblyHashes.Keys.Order(StringComparer.Ordinal))
            || producing.AssemblyHashes.Any(p => !TowerContractJson.Hash(p.Value) || p.Key != "BalanceHarness" && reporting.AssemblyHashes[p.Key] != p.Value))
            throw new InvalidDataException("Unsupported historical execution; expected identical game assemblies/runtime/platform and explicit reporting-harness compatibility.");
    }
}
