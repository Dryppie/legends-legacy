using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness;

// The injected fixture transport cannot allocate seeds or enter the engine. Production uses the
// same controller, accounting and publication boundaries with the existing compact archive APIs.
internal interface ITowerRefinementComparisonRuntime
{
    void CreateSharedExecutable(string path, long bytes, CancellationToken token);
    void VerifySharedExecutable(string path, CancellationToken token);
    Task<BossDiscoveryRunReport> Discover(TowerBossDiscoveryDefinition definition, string path, TowerBulkOptions options, CancellationToken token);
    Task<BossDiscoveryRunReport> VerifyDiscovery(TowerBossDiscoveryDefinition definition, string path, CancellationToken token);
    Task Balance(TowerBalanceDefinition definition, string path, TowerBulkOptions options, CancellationToken token);
    Task<IReadOnlyList<TowerBalanceEvidence>> VerifyBalance(TowerBalanceDefinition definition, string path, CancellationToken token);
    Task<IReadOnlyList<TowerRefinementSelectionHealth>> VerifySelectionHealth(TowerBalanceDefinition definition, string path, CancellationToken token)
        => throw new InvalidDataException("This runtime does not provide verified selection health.");
}

public static class TowerRefinementComparisonRun
{
    private sealed record Started(string Version, int Seconds, long Bytes, int MaximumAttempts,
        [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] string? ArchiveProfile);
    private static string? Profile(string output)
    {
        var start = HarnessJson.Read<JsonElement>(Path.Combine(output, "started.json"));
        var profile = start.TryGetProperty("archiveProfile", out var p) ? p.GetString() : null;
        TowerSharedExecutable.ValidateProfile(profile); return profile;
    }
    public const string FinalFiles = "comparison-files.json";
    public const int MaximumAttempts = 288;
    public const int MaximumSeconds = 900;
    public const long MaximumBytes = 384L * 1024 * 1024;
    private static void Require(bool value, string message) => TowerRefinementComparisonModel.Require(value, message);

    // A separately frozen launcher must bind authorized seeds, historical exclusions, content and
    // producing binaries before calling this opt-in API. This method never reserves seed values.
    public static Task<TowerRefinementComparisonQuality> RunAsync(string contentRoot, string output,
        TowerBossDiscoveryDefinition[] definitions, int seconds, long bytes, CancellationToken token = default, string? archiveProfile = null)
        => Execute(output, definitions, seconds, bytes, new CompactRuntime(contentRoot, archiveProfile), token, archiveProfile);

    public static Task<TowerRefinementComparisonQuality> VerifyAsync(string output, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Reconstruct(output, new CompactRuntime("", Profile(output)), token);
    }

    internal static async Task<TowerRefinementComparisonQuality> Reconstruct(string output,
        ITowerRefinementComparisonRuntime runtime, CancellationToken token = default)
    {
        string P(string n) => Path.Combine(output, n);
        token.ThrowIfCancellationRequested();
        var profile = Profile(output); using var encoding = TowerSharedExecutable.Activate(profile);
        token.ThrowIfCancellationRequested();
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Verification must not fight.")).Activate();
        Require(!File.Exists(P("failure.json")), "Failed comparison cannot be verified complete.");
        TowerBulkCampaign.VerifyFiles(output, FinalFiles, true, token);
        if (profile is not null) runtime.VerifySharedExecutable(output, token);
        var d = HarnessJson.Read<TowerBossDiscoveryDefinition[]>(P("definitions.json"));
        TowerRefinementComparisonModel.ValidatePair(d);
        var pair = new List<TowerDiscoveryComparisonInput>();
        for (var i = 0; i < 2; i++) pair.Add(new(TowerBossImprovement.Inputs(d[i]), await runtime.VerifyDiscovery(d[i], P("discovery-" + i), token)));
        var gate = TowerDiscoveryComparisonGate.Inspect(pair);
        Require(gate.Status == "Ready" && HarnessJson.Hash(gate) == HarnessJson.Hash(HarnessJson.Read<TowerDiscoveryComparisonDecision>(P("discovery-gate.json"))), "Changed discovery decision.");
        var nominees = pair.SelectMany(item => {
            var arm = item.Report.Generation!.Arms.Single(); var choices = arm.Proposals.ToDictionary(p => p.Party!.Id, p => p.Party!);
            return TowerBossGeneration.Rank(arm.Evaluations).Take(2).Select((e,i) => new TowerDiscoveryComparisonNomination(item.Inputs.Generation.PolicyVersion, i+1, choices[e.Id]));
        }).ToArray();
        void Equal<T>(string name, T value) => Require(HarnessJson.Hash(value) == HarnessJson.Hash(HarnessJson.Read<T>(P(name))), "Changed comparison artifact: " + name);
        var nominations = TowerRefinementComparisonModel.Nominations(d, nominees); Equal("nominations.json", nominations);
        var screen = TowerRefinementComparisonModel.Screen(nominations); Equal("screen.json", screen);
        var selectionDefinition = TowerRefinementComparisonModel.Balance(d[0], screen, false);
        var selection = await runtime.VerifyBalance(selectionDefinition, P("selection"), token);
        var health = d[0].Id == TowerRefinementComparisonModel.ZeroWinVersion
            ? await runtime.VerifySelectionHealth(selectionDefinition, P("selection"), token) : null;
        var finalists = TowerRefinementComparisonModel.Finalists(selectionDefinition, nominations, selection, d[0].Id, health); Equal("finalists.json", finalists);
        var family = TowerRefinementComparisonModel.Family(d[0], finalists); Equal("family.json", family);
        var confirmationDefinition = TowerRefinementComparisonModel.Balance(d[0], family, true);
        var confirmation = await runtime.VerifyBalance(confirmationDefinition, P("confirmation"), token);
        var quality = TowerRefinementComparisonModel.Assess(confirmationDefinition, family, confirmation); Equal("quality.json", quality);
        var expected = 128 + screen.Length * 8 + family.Length * 32;
        TowerRescreenAttempts.Verify(P("attempts.bin"), expected);
        var start = HarnessJson.Read<JsonElement>(P("started.json"));
        var execution = HarnessJson.Read<JsonElement>(P("execution.json"));
        var performance = HarnessJson.Read<JsonElement>(P("performance.json"));
        Require(start.GetProperty("version").GetString() == d[0].Id
            && start.GetProperty("maximumAttempts").GetInt32() == MaximumAttempts
            && start.GetProperty("seconds").GetInt32() is >= 1 and <= MaximumSeconds
            && start.GetProperty("bytes").GetInt64() is >= 4 * 1024 * 1024 and <= MaximumBytes
            && execution.GetProperty("status").GetString() == "Complete"
            && execution.GetProperty("started").GetInt32() == expected && execution.GetProperty("completed").GetInt32() == expected
            && performance.GetProperty("status").GetString() == "Complete"
            && performance.GetProperty("started").GetInt32() == expected && performance.GetProperty("completed").GetInt32() == expected
            && double.IsFinite(performance.GetProperty("seconds").GetDouble()) && performance.GetProperty("seconds").GetDouble() >= 0
            && performance.GetProperty("seconds").GetDouble() <= start.GetProperty("seconds").GetInt32()
            && TowerBulkCampaign.StorageBytes(output, token) <= start.GetProperty("bytes").GetInt64(), "Changed global comparison accounting.");
        TowerBulkCampaign.VerifyFiles(output, FinalFiles, true, token); return quality;
    }

    internal static async Task<TowerRefinementComparisonQuality> Execute(string output,
        TowerBossDiscoveryDefinition[] definitions, int seconds, long bytes,
        ITowerRefinementComparisonRuntime runtime, CancellationToken token = default, string? archiveProfile = null)
    {
        using var encoding = TowerSharedExecutable.Activate(archiveProfile);
        token.ThrowIfCancellationRequested();
        TowerRefinementComparisonModel.ValidatePair(definitions);
        Require(seconds is >= 1 and <= MaximumSeconds && bytes is >= 4 * 1024 * 1024 and <= MaximumBytes, "Invalid comparison envelope.");
        output = Path.GetFullPath(output);
        using var lease = TowerCompactBundle.AcquireWriter(output);
        Require(Directory.Exists(output) && !Directory.EnumerateFileSystemEntries(output).Any(), "New empty output required; no retry or resume.");
        string P(string n) => Path.Combine(output, n);
        var clock = Stopwatch.StartNew();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(seconds)); var ct = deadline.Token;
        TowerCompleteFamilyRun.Durable(P("started.json"), new Started(definitions[0].Id, seconds, bytes, MaximumAttempts, archiveProfile));
        TowerCompleteFamilyRun.Durable(P("definitions.json"), definitions);
        using var journal = new TowerRescreenAttempts(P("attempts.bin"), MaximumAttempts);
        TowerStorageAccountant? storage = null; var allowed = false; var boundary = 0;
        var trace = new TowerPerformanceTrace(done => {
            if (!allowed) throw new InvalidOperationException("Only the active comparison stage may fight.");
            if (done) journal.Record(true); // A returned outcome remains charged if cancellation arrives here.
            ct.ThrowIfCancellationRequested();
            if (!done) {
                Require(journal.Started < boundary, "Stage exceeded its frozen attempt schedule.");
                storage!.Check(ct);
                journal.Record(false); // Durable charge strictly precedes engine entry.
            }
        });
        using var active = trace.Activate(); using var process = Process.GetCurrentProcess();
        var cpu = process.TotalProcessorTime; var allocated = GC.GetTotalAllocatedBytes();
        object Performance(string status) => new { status, journal.Started, journal.Completed, seconds = clock.Elapsed.TotalSeconds,
            cpuSeconds = (process.TotalProcessorTime - cpu).TotalSeconds, allocatedBytes = GC.GetTotalAllocatedBytes() - allocated,
            peakWorkingSetBytes = process.PeakWorkingSet64, timings = trace.Snapshot() };
        try {
            if (archiveProfile is not null) runtime.CreateSharedExecutable(output,
                bytes - TowerBulkCampaign.StorageBytes(output, ct) - 1048576, ct);
            storage = new(output, bytes, ["attempts.bin", "discovery-gate.json", "nominations.json", "screen.json", "finalists.json",
                "family.json", "quality.json", "execution.json", "performance.json", "failure.json", "performance-failure.json", FinalFiles, FinalFiles + ".pending"], ct);
            var reports = definitions.Select(_ => new BossDiscoveryRunReport("NotRun", 64, 0, 0, null, "Discovery not started.")).ToArray();
            TowerDiscoveryComparisonInput[] Pair() => definitions.Select((d, i) => new TowerDiscoveryComparisonInput(TowerBossImprovement.Inputs(d), reports[i])).ToArray();
            async Task<T> Stage<T>(string name, int maximum, Func<string, TowerBulkOptions, Task<T>> run,
                Func<string, T, Task<T>> verify, Func<T, int> actual)
            {
                ct.ThrowIfCancellationRequested(); var path = P(name);
                var remaining = (int)Math.Floor(seconds - clock.Elapsed.TotalSeconds);
                var available = bytes - storage.Check(ct) - 1048576;
                Require(remaining >= 1 && available >= 1048576, "Global time/storage limit reached.");
                var before = journal.Started; boundary = checked(before + maximum);
                Require(before == journal.Completed && boundary <= MaximumAttempts, "Interrupted or exhausted global attempt schedule.");
                storage.BeginDirectory(path, ct);
                T result; allowed = true;
                try {
                    using (TowerStorageOwnership.Activate(storage))
                    using (TowerPerformanceTrace.Measure("comparison." + name))
                        result = await run(path, new(32, 0, remaining, available, "prepared-v1", TowerStorageAccountant.Mode,
                            archiveProfile is null ? null : TowerSharedExecutable.RelativePath));
                }
                finally { allowed = false; }
                ct.ThrowIfCancellationRequested();
                using (TowerPerformanceTrace.Measure("comparison.verify-" + name)) result = await verify(path, result);
                var count = actual(result);
                Require(count >= 0 && count <= maximum && journal.Started - before == count && journal.Completed == journal.Started,
                    "Archived results differ from durable attempt charges.");
                storage.SealDirectory(ct); return result;
            }
            for (var i = 0; i < 2; i++) {
                var d = definitions[i];
                reports[i] = await Stage("discovery-" + i, 64,
                    (path, options) => runtime.Discover(d, path, options, ct),
                    async (path, result) => {
                        var verified = await runtime.VerifyDiscovery(d, path, ct);
                        Require(HarnessJson.Hash(result) == HarnessJson.Hash(verified), "Discovery archive differs from returned report.");
                        return verified;
                    }, r => r.ActualBattles);
                // A partial baseline stops before even starting the second arm. The explicit NotRun
                // report records that boundary; it never supplies synthetic successful nominations.
                if (TowerDiscoveryComparisonGate.Inspect(Pair()).Arms[i].Issues.Count != 0)
                    TowerDiscoveryComparisonGate.Nominate(Pair(), P("discovery-gate.json"), ct);
            }
            var nominees = TowerDiscoveryComparisonGate.Nominate(Pair(), P("discovery-gate.json"), ct);
            var nominations = TowerRefinementComparisonModel.Nominations(definitions, nominees);
            TowerCompleteFamilyRun.Durable(P("nominations.json"), nominations);
            var screen = TowerRefinementComparisonModel.Screen(nominations);
            TowerCompleteFamilyRun.Durable(P("screen.json"), screen);
            async Task<IReadOnlyList<TowerBalanceEvidence>> Measure(string name, TowerBalanceDefinition d)
                => await Stage<IReadOnlyList<TowerBalanceEvidence>>(name, d.MaximumBattles,
                    async (path, options) => { await runtime.Balance(d, path, options, ct); return []; },
                    async (path, _) => {
                        var evidence = await runtime.VerifyBalance(d, path, ct);
                        TowerFeedbackBenchmark.RequireEvidence(d, evidence); return evidence;
                    }, e => e.Sum(r => r.Trials.Count));
            var screenDefinition = TowerRefinementComparisonModel.Balance(definitions[0], screen, false);
            var screening = await Measure("selection", screenDefinition);
            var health = definitions[0].Id == TowerRefinementComparisonModel.ZeroWinVersion
                ? await runtime.VerifySelectionHealth(screenDefinition, P("selection"), ct) : null;
            var finalists = TowerRefinementComparisonModel.Finalists(screenDefinition, nominations, screening, definitions[0].Id, health);
            TowerCompleteFamilyRun.Durable(P("finalists.json"), finalists);
            var family = TowerRefinementComparisonModel.Family(definitions[0], finalists);
            TowerCompleteFamilyRun.Durable(P("family.json"), family);
            var confirmationDefinition = TowerRefinementComparisonModel.Balance(definitions[0], family, true);
            var confirmation = await Measure("confirmation", confirmationDefinition);
            var quality = TowerRefinementComparisonModel.Assess(confirmationDefinition, family, confirmation);
            journal.Close(); TowerRescreenAttempts.Verify(P("attempts.bin"), 128 + screen.Length * 8 + family.Length * 32);
            TowerCompleteFamilyRun.Durable(P("quality.json"), quality);
            TowerCompleteFamilyRun.Durable(P("execution.json"), new { status = "Complete", journal.Started, journal.Completed });
            TowerCompleteFamilyRun.Durable(P("performance.json"), Performance("Complete"));
            storage.Audit(ct);
            TowerCompleteFamilyRun.Durable(P(FinalFiles + ".pending"), TowerBulkCampaign.Paths(output)
                .ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash, StringComparer.Ordinal));
            storage.Audit(ct); ct.ThrowIfCancellationRequested(); File.Move(P(FinalFiles + ".pending"), P(FinalFiles));
            TowerBulkCampaign.VerifyFiles(output, FinalFiles, true, ct); ct.ThrowIfCancellationRequested();
            Require(clock.Elapsed.TotalSeconds <= seconds, "Deadline exceeded during final verification.");
            return quality;
        }
        catch (Exception error) {
            try { TowerCompleteFamilyRun.Durable(P("performance-failure.json"), Performance("Failed")); } catch (IOException) { }
            try { TowerCompleteFamilyRun.Durable(P("failure.json"), new { error = error.ToString(), journal.Started, journal.Completed, noRetry = true }); } catch (IOException) { }
            throw;
        }
    }

    private sealed class CompactRuntime(string contentRoot, string? profile) : ITowerRefinementComparisonRuntime
    {
        public void CreateSharedExecutable(string path, long bytes, CancellationToken ct)
            => TowerSharedExecutable.Create(path, ExecutionIdentity.Current(), bytes, TowerBossStudy.RetainExecutable, ct);
        public void VerifySharedExecutable(string path, CancellationToken ct)
            => TowerSharedExecutable.Verify(path, ExecutionIdentity.Current(), ct);
        private void Contract<T>(T definition, string path, string kind)
        {
            var c = TowerContractJson.Read<TowerBulkContract>(Path.Combine(path, TowerBulkCampaign.ContractFile));
            Require(c.SchemaVersion == (profile is null ? 1 : 2) && c.Kind == kind && HarnessJson.Hash(c.Definition.Deserialize<T>(HarnessJson.Options)!) == HarnessJson.Hash(definition)
                && c.Options.ChunkSize == 32 && c.Options.RetryReserve == 0 && c.Options.ExecutionMode == "prepared-v1"
                && c.Options.StorageAccounting == TowerStorageAccountant.Mode
                && c.Options.SharedExecutablePath == (profile is null ? null : TowerSharedExecutable.RelativePath), "Archived stage contract differs.");
        }
        public Task<BossDiscoveryRunReport> Discover(TowerBossDiscoveryDefinition d, string path, TowerBulkOptions options, CancellationToken ct)
            => TowerCompactDiscovery.RunAsync(contentRoot, path, d, options, token: ct);
        public Task<BossDiscoveryRunReport> VerifyDiscovery(TowerBossDiscoveryDefinition d, string path, CancellationToken ct)
        {
            Contract(d, path, TowerCompactDiscovery.Kind); return TowerCompactDiscovery.VerifyAsync(path, ct);
        }
        public async Task Balance(TowerBalanceDefinition d, string path, TowerBulkOptions options, CancellationToken ct)
            => await TowerCompactBalanceRun.RunAsync(contentRoot, path, d, options, token: ct);
        public async Task<IReadOnlyList<TowerBalanceEvidence>> VerifyBalance(TowerBalanceDefinition d, string path, CancellationToken ct)
        {
            Contract(d, path, TowerCompactBalanceRun.Kind); await TowerCompactBalanceRun.VerifyAsync(path, ct);
            return HarnessJson.Read<TowerBalanceEvidence[]>(Path.Combine(path, "evidence.json"));
        }
        public Task<IReadOnlyList<TowerRefinementSelectionHealth>> VerifySelectionHealth(TowerBalanceDefinition d, string path, CancellationToken ct)
        {
            path = Path.GetFullPath(path);
            Contract(d, path, TowerCompactBalanceRun.Kind);
            Require(d.Id == "comparison-screen", "Health is read only from the frozen selection archive.");
            var sources = HarnessJson.Read<TowerBalanceRunSource[]>(Path.Combine(path, "sources.json"));
            Require(sources.Length == d.Cells.Count && sources.Select(s => s.CellId).Distinct().Count() == sources.Length
                && sources.Select(s => s.CellId).Order(StringComparer.Ordinal).SequenceEqual(d.Cells.Select(c => c.Id).Order(StringComparer.Ordinal)),
                "Selection archive mappings differ from the frozen family.");
            var result = new List<TowerRefinementSelectionHealth>();
            var root = Path.GetFullPath(path) + Path.DirectorySeparatorChar;
            foreach (var group in sources.GroupBy(s => s.RunDirectory, StringComparer.Ordinal)) {
                ct.ThrowIfCancellationRequested();
                var directory = Path.GetFullPath(group.Key, path);
                Require(directory.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal),
                    "Selection archive mapping escapes its campaign.");
                var trials = new Dictionary<string, List<TowerRefinementSelectionTrial>>(StringComparer.Ordinal);
                // Visitor observations become usable only after full archive verification succeeds.
                var saved = TowerCompactBundle.Verify(directory, ct, visit: (caseId, trial) => {
                    if (!trials.TryGetValue(caseId, out var list)) trials.Add(caseId, list = []);
                    list.Add(new(trial.Seed, (double)trial.Report.GuardianHealthRemainingPercent));
                });
                foreach (var source in group) {
                    Require(source.CompactCaseId is not null && trials.ContainsKey(source.CompactCaseId), "Missing selection health case.");
                    var evidence = TowerCompactBundle.Evidence(source.CellId, saved, source.CompactCaseId);
                    result.Add(new(source.CellId, HarnessJson.Hash(evidence), trials[source.CompactCaseId!]));
                }
            }
            return Task.FromResult<IReadOnlyList<TowerRefinementSelectionHealth>>(result);
        }
    }
}
