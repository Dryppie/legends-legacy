using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace BalanceHarness;

internal sealed record TowerCompleteBatch(int Ordinal, int Stage, IReadOnlyList<TowerConfirmationCell> Cells)
{
    public string DirectoryName => "batch-" + Ordinal.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
    public int Samples => Stage == 1 ? TowerCompleteFamily.FirstSamples : TowerCompleteFamily.SecondSamples;
    public int Attempts => checked(Cells.Count * Samples);
}
public sealed record TowerCompleteBatchReceipt(int Ordinal, int Stage, IReadOnlyList<string> Cells, string EvidenceHash);
public sealed record TowerCompleteExecution(string Status, int Started, int Completed, double SetupSeconds,
    double ExecuteSeconds, IReadOnlyList<TowerCompleteBatchReceipt> Batches);
internal sealed record TowerCompleteRunContext(TowerCompleteProtocol Protocol, double SetupSeconds, long StudyBytes,
    TowerCompleteIdentity? ReservedIdentity = null, string? LaunchHash = null);

public static class TowerCompleteFamilyRun
{
    public const string FinalFiles = "complete-family-files.json";
    internal static void Durable<T>(string path, T value)
    {
        using var f = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        JsonSerializer.Serialize(f, value, HarnessJson.Options); f.Flush(true);
    }
    private static Dictionary<string, string> Inventory(string root) => TowerBulkCampaign.Paths(root)
        .Where(p => Path.GetFileName(p) != FinalFiles && Path.GetFileName(p) != FinalFiles + ".pending")
        .ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'), HarnessJson.FileHash, StringComparer.Ordinal);

    // Production and injected zero-engine fixtures share every accounting, selection and publication boundary.
    internal static async Task<TowerCompleteAssessment> Execute(string root, int maximum, int capacity, double setup,
        double seconds, long bytes, Func<CancellationToken, Task<IReadOnlyList<TowerConfirmationCell>>> preflight,
        Func<TowerCompleteBatch, string, TowerBulkOptions, CancellationToken, Task> run,
        Func<TowerCompleteBatch, string, CancellationToken, Task<IReadOnlyList<TowerCompleteObservation>>> verify,
        Func<TowerCompleteExecution, TowerCompleteAssessment, CancellationToken, Task> reconstruct,
        CancellationToken token = default, Action<string>? progress = null, string? launchHash = null)
    {
        token.ThrowIfCancellationRequested(); root = Path.GetFullPath(root); using var lease = TowerCompactBundle.AcquireWriter(root);
        string P(string n) => Path.Combine(root, n);
        if (!Directory.Exists(root) || File.Exists(P("started.json")) || File.Exists(P(FinalFiles))) throw new InvalidDataException("No retry, resume or overwrite.");
        if (maximum is < 1 or > TowerCompleteFamily.MaximumFights || capacity is < 1 or > TowerCompleteFamily.MaximumSecondCells
            || !double.IsFinite(setup) || setup < 0 || !double.IsFinite(seconds) || seconds <= setup || seconds > TowerCompleteFamily.MaximumSeconds
            || bytes < 1048576 || bytes > TowerCompleteFamily.MaximumBytes) throw new InvalidDataException("Invalid global resource envelope.");
        var clock = Stopwatch.StartNew(); using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(seconds-setup)); var ct = deadline.Token;
        var start = new Dictionary<string, object> { ["utc"] = DateTimeOffset.UtcNow, ["protocolHash"] = HarnessJson.FileHash(P("protocol.json")),
            ["maximum"] = maximum, ["capacity"] = capacity, ["setup"] = setup, ["seconds"] = seconds, ["bytes"] = bytes };
        if (launchHash is not null) start.Add("launchHash", launchHash);
        Durable(P("started.json"), start);
        using var journal = new TowerRescreenAttempts(P("attempts.bin"), maximum);
        TowerStorageAccountant? storage = null; var allowed = false; var boundary = 0;
        var trace = new TowerPerformanceTrace(done => {
            if (!allowed) throw new InvalidOperationException("Only the active complete-family batch may fight.");
            if (done) journal.Record(true); // A returned outcome remains charged even when cancellation arrives here.
            ct.ThrowIfCancellationRequested();
            if (!done)
            {
                if (journal.Started >= boundary) throw new InvalidDataException("Batch attempted beyond its exact schedule.");
                if (journal.Started % 128 == 0) storage!.Check(ct);
                journal.Record(false); // WriteThrough and Flush(true) strictly precede engine entry.
            }
        });
        using var active = trace.Activate(); using var process = Process.GetCurrentProcess();
        var cpu = process.TotalProcessorTime; var allocated = GC.GetTotalAllocatedBytes();
        var receipts = new List<TowerCompleteBatchReceipt>();
        object Performance(string status) => new { status, journal.Started, journal.Completed, setupSeconds = setup,
            executeSeconds = clock.Elapsed.TotalSeconds, totalSeconds = setup+clock.Elapsed.TotalSeconds,
            cpuSeconds = (process.TotalProcessorTime-cpu).TotalSeconds, allocatedBytes = GC.GetTotalAllocatedBytes()-allocated,
            peakWorkingSetBytes = process.PeakWorkingSet64, timings = trace.Snapshot(), boundary = "Before final publication; global deadline remains active." };
        try
        {
            storage = new(root, bytes, ["attempts.bin", "preparation.json", "stage-selection.json", "assessment.json", "execution.json",
                "performance.json", "failure.json", "performance-failure.json", FinalFiles, FinalFiles+".pending"], ct);
            IReadOnlyList<TowerConfirmationCell> cells;
            using (TowerPerformanceTrace.Measure("complete.preflight")) cells = await preflight(ct);
            if (cells.Count == 0 || cells.Count > TowerCompleteFamily.Cells || cells.Any(c => c is null)
                || !cells.Select(c => c.CellHash).SequenceEqual(cells.Select(c => c.CellHash).Distinct().Order(StringComparer.Ordinal))
                || cells.All(c => c.AnchorReasons.Count == 0) || cells.Count(c => c.AnchorReasons.Count > 0) > capacity)
                throw new InvalidDataException("Incomplete, unordered or invalid execution family.");
            var firstCells = cells.Where(c => c.AnchorReasons.Count == 0).ToArray();
            if ((long)firstCells.Length * 32 + capacity * 256L > maximum) throw new InvalidDataException("Fixed stages exceed the attempt reservation.");
            storage.Check(ct); ct.ThrowIfCancellationRequested();
            async Task<List<TowerCompleteObservation>> Stage(IReadOnlyList<TowerConfirmationCell> selected, int stage)
            {
                var rows = new List<TowerCompleteObservation>();
                foreach (var chunk in selected.Chunk(TowerCompleteFamily.BatchCells))
                {
                    ct.ThrowIfCancellationRequested(); var batch = new TowerCompleteBatch(receipts.Count, stage, chunk); var path = P(batch.DirectoryName);
                    var remainingSeconds = (int)Math.Floor(seconds-setup-clock.Elapsed.TotalSeconds);
                    var remainingBytes = bytes-storage.Check(ct)-1048576;
                    if (remainingSeconds < 1 || remainingBytes < 1048576) throw new InvalidDataException("Global time/storage budget exhausted.");
                    boundary = checked(journal.Completed + batch.Attempts); storage.BeginDirectory(path, ct); allowed = true;
                    try
                    {
                        using (TowerStorageOwnership.Activate(storage))
                        using (TowerPerformanceTrace.Measure("complete.stage-" + stage))
                            await run(batch, path, new(32, 0, remainingSeconds, remainingBytes, "prepared-v1", TowerStorageAccountant.Mode), ct);
                    }
                    finally { allowed = false; }
                    if (journal.Started != boundary || journal.Completed != boundary) throw new InvalidDataException("Missing or interrupted batch attempts.");
                    IReadOnlyList<TowerCompleteObservation> verified;
                    using (TowerPerformanceTrace.Measure("complete.batch-reconstruction")) verified = await verify(batch, path, ct);
                    TowerCompleteFamily.CheckObservations(chunk, verified, batch.Samples); storage.SealDirectory(ct);
                    receipts.Add(new(batch.Ordinal, stage, chunk.Select(c => c.CellHash).ToArray(), HarnessJson.Hash(verified))); rows.AddRange(verified);
                    progress?.Invoke($"Complete-family stage {stage}: {rows.Count}/{selected.Count} cells; {journal.Completed} charged completions.");
                }
                return rows;
            }
            var first = await Stage(firstCells, 1);
            var selection = TowerCompleteFamily.Select(cells, first, capacity);
            Durable(P("stage-selection.json"), selection); // Complete first-stage evidence hash is frozen before any stage-two attempt.
            storage.Check(ct); ct.ThrowIfCancellationRequested();
            var selected = selection.SecondCells.ToHashSet(StringComparer.Ordinal);
            var second = await Stage(selection.Status == "Proceed" ? cells.Where(c => selected.Contains(c.CellHash)).ToArray() : [], 2);
            var result = TowerCompleteFamily.Assess(cells, first, second, capacity);
            if (journal.Started != result.LogicalTrials || journal.Completed != result.LogicalTrials) throw new InvalidDataException("Global schedule accounting differs.");
            journal.Close(); TowerRescreenAttempts.Verify(P("attempts.bin"), result.LogicalTrials);
            var execution = new TowerCompleteExecution("Complete", journal.Started, journal.Completed, setup, clock.Elapsed.TotalSeconds, receipts);
            using (TowerPerformanceTrace.Measure("complete.final-reconstruction")) await reconstruct(execution, result, ct);
            Durable(P("assessment.json"), result);
            execution = execution with { ExecuteSeconds = clock.Elapsed.TotalSeconds }; Durable(P("execution.json"), execution);
            Durable(P("performance.json"), Performance("Complete"));
            storage.Audit(ct); ct.ThrowIfCancellationRequested();
            Durable(P(FinalFiles+".pending"), Inventory(root)); storage.Audit(ct); ct.ThrowIfCancellationRequested();
            File.Move(P(FinalFiles+".pending"), P(FinalFiles)); TowerBulkCampaign.VerifyFiles(root, FinalFiles, true, ct);
            ct.ThrowIfCancellationRequested(); if (setup+clock.Elapsed.TotalSeconds > seconds) throw new InvalidDataException("Deadline exceeded during final verification.");
            return result;
        }
        catch (Exception e)
        {
            try { Durable(P("performance-failure.json"), Performance("Failed")); } catch (IOException) { }
            try { Durable(P("failure.json"), new { error = e.ToString(), journal.Started, journal.Completed, seconds = clock.Elapsed.TotalSeconds, noRetry = true }); } catch (IOException) { }
            throw;
        }
    }

    internal static TowerBalanceDefinition Definition(TowerCompleteSource source, TowerCompleteSeeds seeds, TowerCompleteBatch batch) =>
        new(1, batch.DirectoryName, TowerBalanceEvaluator.IntervalPolicy, source.ContentHashes, source.SettingsHash,
            HarnessJson.Hash(ExecutionIdentity.Current()), [source.Cohort], batch.Cells.Select(c => new TowerBalanceCellDefinition(
                c.CellHash, source.Cohort.Id, c.AnchorReasons.Count > 0 ? "reference" : "generated",
                source.Scenarios[c.CellHash] with { Seeds = batch.Stage == 1 ? seeds.First : seeds.Second }, batch.Samples)).ToArray(),
            seeds.Historical, batch.Attempts);

    internal static async Task<IReadOnlyList<TowerCompleteObservation>> VerifyBatch(TowerCompleteSource source, TowerCompleteSeeds seeds,
        TowerCompleteBatch batch, string path, CancellationToken ct)
    {
        var definition = Definition(source, seeds, batch); var contract = TowerContractJson.Read<TowerBulkContract>(Path.Combine(path, "campaign.json"));
        TowerPortfolioConfirmation.Equal(definition, contract.Definition, "complete-family storage batch");
        if (contract.Kind != TowerCompactBalanceRun.Kind || contract.PlannedBattles != batch.Attempts || contract.MaximumAttempts != batch.Attempts
            || contract.Options.ChunkSize != 32 || contract.Options.RetryReserve != 0 || contract.Options.ExecutionMode != "prepared-v1"
            || contract.Options.StorageAccounting != TowerStorageAccountant.Mode || contract.Options.MaximumSeconds is < 1 or > TowerCompleteFamily.MaximumSeconds
            || contract.Options.MaximumBytes is < 1048576 or > TowerCompleteFamily.MaximumBytes) throw new InvalidDataException("Changed storage batch envelope.");
        await TowerCompactBalanceRun.VerifyAsync(path, ct);
        var sources = HarnessJson.Read<TowerBalanceRunSource[]>(Path.Combine(path, "sources.json"));
        if (!sources.Select(s => s.CellId).SequenceEqual(batch.Cells.Select(c => c.CellHash))) throw new InvalidDataException("Changed archived cell order.");
        var expected = batch.Cells.ToDictionary(c => c.CellHash, c => c.ParticipantsHash);
        foreach (var group in sources.GroupBy(s => s.RunDirectory))
        {
            var rosters = group.ToDictionary(s => s.CompactCaseId!, s => expected[s.CellId]);
            var archive = Path.Combine(path, group.Key); var plan = HarnessJson.Read<TowerCompactPlan>(Path.Combine(archive, "bulk-plan.json"));
            var count = 0;
            for (var chunk = 0; chunk < (plan.PlannedBattles+31)/32; chunk++)
            {
                ct.ThrowIfCancellationRequested();
                using var file = File.OpenRead(Path.Combine(archive, "chunks", chunk.ToString("D6", System.Globalization.CultureInfo.InvariantCulture), "records.json.gz"));
                using var gzip = new GZipStream(file, CompressionMode.Decompress);
                foreach (var record in JsonSerializer.Deserialize<TowerCompactRecord[]>(gzip, HarnessJson.Options)!)
                {
                    if (!rosters.TryGetValue(record.CaseId, out var hash) || record.PreparedHash != hash)
                        throw new InvalidDataException("Prepared archive roster differs from the complete audited family.");
                    count++;
                }
            }
            if (count != plan.PlannedBattles) throw new InvalidDataException("Incomplete roster records.");
        }
        var evidence = HarnessJson.Read<TowerBalanceEvidence[]>(Path.Combine(path, "evidence.json"));
        if (evidence.Length != batch.Cells.Count) throw new InvalidDataException("Incomplete batch evidence.");
        return batch.Cells.Select((c, i) => TowerCompleteFamily.Observation(c, definition.Cells[i].Scenario, source.Cohort,
            source.ContentHashes, source.SettingsHash, definition.ExecutionHash, evidence[i])).ToArray();
    }

    internal static async Task<TowerCompleteAssessment> Reconstruct(string root, TowerCompleteSource source, TowerCompleteSeeds seeds,
        TowerCompleteExecution execution, CancellationToken ct)
    {
        var receipts = execution.Batches; var ordinal = 0;
        async Task<List<TowerCompleteObservation>> Stage(IReadOnlyList<TowerConfirmationCell> cells, int stage)
        {
            var rows = new List<TowerCompleteObservation>();
            foreach (var chunk in cells.Chunk(TowerCompleteFamily.BatchCells))
            {
                ct.ThrowIfCancellationRequested(); var batch = new TowerCompleteBatch(ordinal, stage, chunk);
                if (ordinal >= receipts.Count || receipts[ordinal].Ordinal != ordinal || receipts[ordinal].Stage != stage
                    || !receipts[ordinal].Cells.SequenceEqual(chunk.Select(c => c.CellHash))) throw new InvalidDataException("Changed complete stage batch schedule.");
                var verified = await VerifyBatch(source, seeds, batch, Path.Combine(root, batch.DirectoryName), ct);
                if (HarnessJson.Hash(verified) != receipts[ordinal++].EvidenceHash) throw new InvalidDataException("Reconstructed batch evidence changed.");
                rows.AddRange(verified);
            }
            return rows;
        }
        var first = await Stage(source.Cells.Where(c => c.AnchorReasons.Count == 0).ToArray(), 1);
        var selection = TowerCompleteFamily.Select(source.Cells, first);
        TowerPortfolioConfirmation.Equal(selection, HarnessJson.Read<TowerCompleteSelection>(Path.Combine(root, "stage-selection.json")), "frozen whole-stage selection");
        var selected = selection.SecondCells.ToHashSet(StringComparer.Ordinal);
        var second = await Stage(selection.Status == "Proceed" ? source.Cells.Where(c => selected.Contains(c.CellHash)).ToArray() : [], 2);
        if (ordinal != receipts.Count || !Directory.GetDirectories(root).Select(Path.GetFileName).Order(StringComparer.Ordinal)
            .SequenceEqual(receipts.Select(r => "batch-"+r.Ordinal.ToString("D4", System.Globalization.CultureInfo.InvariantCulture)).Order(StringComparer.Ordinal)))
            throw new InvalidDataException("Extra, missing or unvisited stage directory.");
        var result = TowerCompleteFamily.Assess(source.Cells, first, second);
        if (execution.Status != "Complete" || execution.Started != result.LogicalTrials || execution.Completed != result.LogicalTrials)
            throw new InvalidDataException("Incomplete global accounting.");
        TowerRescreenAttempts.Verify(Path.Combine(root, "attempts.bin"), result.LogicalTrials); return result;
    }

    internal static async Task Prepare(string root, TowerCompleteSource source, TowerCompleteSeeds seeds, CancellationToken token)
    {
        using var phase = TowerPerformanceTrace.Measure("complete.prepare-family");
        var settings = TowerBundle.ReadSettings(source.ContentRoot);
        var runner = new TowerBattleRunner(source.ContentRoot, new OfflineContent(source.ContentRoot, settings.Threat));
        var preparation = new List<object>();
        foreach (var cell in source.Cells)
        {
            token.ThrowIfCancellationRequested(); var scenario = source.Scenarios[cell.CellHash] with { Seeds = seeds.First };
            var input = runner.CreateInput(scenario, seeds.First[0], settings.Threat, settings.CheckpointIntervalTicks);
            var runtime = await runner.PrepareAsync(input, token); var participants = HarnessJson.Hash(IdleBattleRunner.DescribeParticipants(runtime));
            if (participants != cell.ParticipantsHash) throw new InvalidDataException("Prelaunch materialization differs from saved participants.");
            preparation.Add(new { cell.CellHash, inputHash = HarnessJson.Hash(input), planHash = HarnessJson.Hash(runtime.Plan), participants });
            TowerPerformanceTrace.Count("prepared-cells");
        }
        Durable(Path.Combine(root, "preparation.json"), preparation);
    }

    internal static async Task RunBatch(TowerCompleteSource source, TowerCompleteSeeds seeds, TowerCompleteBatch batch,
        string path, TowerBulkOptions options, CancellationToken token) =>
        await TowerCompactBalanceRun.RunAsync(source.ContentRoot, path, Definition(source, seeds, batch), options, token: token);

    public static async Task<TowerCompleteAssessment> Run(string root, CancellationToken ct = default, Action<string>? progress = null)
    {
        ct.ThrowIfCancellationRequested(); var p = HarnessJson.Read<TowerCompleteProtocol>(Path.Combine(root, "protocol.json")); TowerCompleteFamilyInputs.Limits(p);
        return await RunCore(root, new(p, p.PriorSetupSeconds, p.MaximumBytes-p.PriorSetupBytes), ct, progress);
    }

    internal static async Task<TowerCompleteAssessment> RunCore(string root, TowerCompleteRunContext context,
        CancellationToken ct, Action<string>? progress = null)
    {
        var p = context.Protocol;
        TowerCompleteSource? source = null; TowerCompleteSeeds? seeds = null;
        return await Execute(root, p.MaximumAttempts, TowerCompleteFamily.MaximumSecondCells, context.SetupSeconds, p.MaximumSeconds,
            context.StudyBytes, async token => {
                source = await TowerCompleteFamilyInputs.Source(p.SourceRoot, token); TowerCompleteFamilyInputs.Frozen(root, p, true, source, token, context.ReservedIdentity);
                var expected = p.FrozenFiles.Keys.Concat(new[] { "protocol.json", "started.json", "attempts.bin" }).ToHashSet(StringComparer.Ordinal);
                if (!expected.SetEquals(TowerBulkCampaign.Paths(root).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')))) throw new InvalidDataException("Unexpected prelaunch file.");
                seeds = HarnessJson.Read<TowerCompleteSeeds>(Path.Combine(root, "seeds.json"));
                // Prepare the entire family before its first engine call; this is inside the single global deadline.
                await Prepare(root, source, seeds, token); return source.Cells;
            }, (batch, path, options, token) => RunBatch(source!, seeds!, batch, path, options, token),
            (batch, path, token) => VerifyBatch(source!, seeds!, batch, path, token),
            async (execution, result, token) => {
                TowerCompleteFamilyInputs.Frozen(root, p, false, source!, token, context.ReservedIdentity);
                TowerPortfolioConfirmation.Equal(result, await Reconstruct(root, source!, seeds!, execution, token), "whole-family reconstruction");
            }, ct, progress, context.LaunchHash);
    }

    public static async Task<TowerCompleteAssessment> Verify(string root, CancellationToken ct = default)
    {
        var p = HarnessJson.Read<TowerCompleteProtocol>(Path.Combine(root, "protocol.json")); TowerCompleteFamilyInputs.Limits(p);
        return await VerifyCore(root, new(p, p.PriorSetupSeconds, p.MaximumBytes-p.PriorSetupBytes), ct);
    }

    internal static async Task<TowerCompleteAssessment> VerifyCore(string root, TowerCompleteRunContext context, CancellationToken ct)
    {
        using var lease = TowerCompactBundle.AcquireWriter(root);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Verification cannot fight.")).Activate();
        TowerBulkCampaign.VerifyFiles(root, FinalFiles, true, ct);
        if (File.Exists(Path.Combine(root, "failure.json")) || File.Exists(Path.Combine(root, "performance-failure.json"))) throw new InvalidDataException("Failed execution cannot verify.");
        var p = context.Protocol; var source = await TowerCompleteFamilyInputs.Source(p.SourceRoot, ct);
        TowerCompleteFamilyInputs.Frozen(root, p, false, source, ct, context.ReservedIdentity);
        var e = HarnessJson.Read<TowerCompleteExecution>(Path.Combine(root, "execution.json"));
        var start = HarnessJson.Read<JsonElement>(Path.Combine(root, "started.json")); var performance = HarnessJson.Read<JsonElement>(Path.Combine(root, "performance.json"));
        VerifyStart(start, HarnessJson.FileHash(Path.Combine(root, "protocol.json")), context);
        if (e.SetupSeconds != context.SetupSeconds
            || !double.IsFinite(e.ExecuteSeconds) || e.ExecuteSeconds < 0 || e.ExecuteSeconds+e.SetupSeconds > p.MaximumSeconds
            || performance.GetProperty("status").GetString() != "Complete" || performance.GetProperty("started").GetInt32() != e.Started
            || performance.GetProperty("completed").GetInt32() != e.Completed || !double.IsFinite(performance.GetProperty("totalSeconds").GetDouble())
            || performance.GetProperty("totalSeconds").GetDouble() < e.ExecuteSeconds+e.SetupSeconds || performance.GetProperty("totalSeconds").GetDouble() > p.MaximumSeconds
            || TowerBulkCampaign.StorageBytes(root, ct) > context.StudyBytes) throw new InvalidDataException("Global accounting differs.");
        var result = await Reconstruct(root, source, HarnessJson.Read<TowerCompleteSeeds>(Path.Combine(root, "seeds.json")), e, ct);
        TowerPortfolioConfirmation.Equal(result, HarnessJson.Read<TowerCompleteAssessment>(Path.Combine(root, "assessment.json")), "complete-family assessment");
        TowerBulkCampaign.VerifyFiles(root, FinalFiles, true, ct); return result;
    }

    internal static void VerifyStart(JsonElement start, string protocolHash, TowerCompleteRunContext context)
    {
        var p = context.Protocol;
        if (start.GetProperty("protocolHash").GetString() != protocolHash
            || start.GetProperty("maximum").GetInt32() != p.MaximumAttempts || start.GetProperty("capacity").GetInt32() != TowerCompleteFamily.MaximumSecondCells
            || (start.TryGetProperty("launchHash", out var launch) ? launch.GetString() : null) != context.LaunchHash
            || start.GetProperty("seconds").GetDouble() != p.MaximumSeconds || start.GetProperty("bytes").GetInt64() != context.StudyBytes
            || start.GetProperty("setup").GetDouble() != context.SetupSeconds) throw new InvalidDataException("Global start accounting differs.");
    }
}
