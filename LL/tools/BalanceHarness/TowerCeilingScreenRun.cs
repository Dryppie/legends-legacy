using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerCeilingCellResult(string Factor, string CellId, int Wins, int Defeats, int Draws, RateEstimate Interval);
public sealed record TowerCeilingScreenResult(string Status, decimal? SelectedFactor,
    IReadOnlyList<TowerCeilingFactorAssessment> Factors, IReadOnlyList<TowerCeilingCellResult> Cells);
public sealed record TowerCeilingExecutionReceipt(string Status, int Started, int Completed, double SetupSeconds,
    double ExecuteSeconds, IReadOnlyList<string> CompletedFactors);

public static class TowerCeilingScreenRun
{
    // The same loop is exercised with synthetic writers in tests. Production always passes the fixed 253*128 envelope.
    internal static async Task<TowerCeilingExecutionReceipt> ExecuteFactorsAsync(string root, int perFactor, int maximumFights,
        double setupSeconds, double maximumSeconds, long maximumBytes,
        Func<int, string, TowerBulkOptions, CancellationToken, Task> run,
        Func<int, string, CancellationToken, Task> verify,
        Func<CancellationToken, Task> preflight, Func<CancellationToken, Task> finalize,
        CancellationToken token = default, Action<string>? progress = null)
    {
        token.ThrowIfCancellationRequested(); root = Path.GetFullPath(root); string P(string n) => Path.Combine(root, n);
        using var lease = TowerCompactBundle.AcquireWriter(root);
        if (!Directory.Exists(root) || File.Exists(P("started.json")) || File.Exists(P(TowerCeilingScreenInputs.FinalFiles)))
            throw new InvalidDataException("Requires unstarted prepared output; resume/retry is forbidden.");
        if (perFactor < 1 || maximumFights != checked(4 * perFactor) || !double.IsFinite(setupSeconds) || setupSeconds < 0
            || !double.IsFinite(maximumSeconds) || maximumSeconds <= setupSeconds || maximumBytes < 1048576)
            throw new InvalidDataException("Invalid global resource envelope.");
        var clock = Stopwatch.StartNew(); using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(maximumSeconds - setupSeconds)); var ct = deadline.Token;
        HarnessJson.WriteNew(P("started.json"), new { utc = DateTimeOffset.UtcNow, protocolHash = HarnessJson.FileHash(P("protocol.json")),
            setupChargeHash = HarnessJson.FileHash(P("setup-charge.json")), maximumFights, maximumSeconds, maximumBytes, setupSeconds });
        using var journal = new TowerRescreenAttempts(P("attempts.bin"), maximumFights);
        TowerStorageAccountant? storage = null; var factorBoundary = 0; var executionAllowed = false;
        var trace = new TowerPerformanceTrace(done => {
            if (!executionAllowed) throw new InvalidOperationException("Only the active factor execution may enter combat.");
            if (done) journal.Record(true); // An actual returned result stays charged even if cancellation arrived concurrently.
            ct.ThrowIfCancellationRequested();
            if (!done)
            {
                if (journal.Started >= factorBoundary) throw new InvalidDataException("Factor attempted more than its frozen schedule.");
                if (journal.Started % 128 == 0) storage!.Check(ct);
                journal.Record(false); // WriteThrough + Flush(true), strictly before engine entry.
            }
            if (done && journal.Completed % 1024 == 0) progress?.Invoke($"Ceiling screen: {journal.Completed}/{maximumFights}; {clock.Elapsed.TotalSeconds:F1}s.");
        });
        using var active = trace.Activate(); using var process = Process.GetCurrentProcess();
        var cpu = process.TotalProcessorTime; var allocated = GC.GetTotalAllocatedBytes(); var completed = new List<string>();
        object Performance(string status) => new { status, journal.Started, journal.Completed, setupSeconds,
            executeSeconds = clock.Elapsed.TotalSeconds, totalSeconds = setupSeconds + clock.Elapsed.TotalSeconds,
            cpuSeconds = (process.TotalProcessorTime - cpu).TotalSeconds, allocatedBytes = GC.GetTotalAllocatedBytes() - allocated,
            peakWorkingSetBytes = process.PeakWorkingSet64, timings = trace.Snapshot(),
            boundary = "Before final metadata serialization, inventory and verification; deadline remains active through return." };
        try
        {
            using (TowerPerformanceTrace.Measure("phase.preflight"))
                await preflight(ct);
            storage = new(root, maximumBytes, ["attempts.bin", "screen-result.json", "execution.json", "performance.json",
                "failure.json", "performance-failure.json", TowerCeilingScreenInputs.FinalFiles, TowerCeilingScreenInputs.FinalFiles + ".pending"], ct);
            for (var i = 0; i < 4; i++)
            {
                ct.ThrowIfCancellationRequested(); var id = TowerCeilingScreenContract.VariantId(i); var path = P(id);
                var remainingSeconds = (int)Math.Floor(maximumSeconds - setupSeconds - clock.Elapsed.TotalSeconds);
                var remainingBytes = maximumBytes - storage.Check(ct) - 1048576; // Reserve root results/failure metadata.
                if (remainingSeconds < 1 || remainingBytes < 1048576) throw new InvalidDataException("Global time/storage budget exhausted before next factor.");
                factorBoundary = (i + 1) * perFactor; storage.BeginDirectory(path, ct);
                executionAllowed = true;
                try
                {
                    using (TowerStorageOwnership.Activate(storage))
                    using (TowerPerformanceTrace.Measure("factor." + id))
                        await run(i, path, new(32, 0, remainingSeconds, remainingBytes, "prepared-v1", TowerStorageAccountant.Mode), ct);
                }
                finally { executionAllowed = false; }
                if (journal.Started != factorBoundary || journal.Completed != factorBoundary)
                    throw new InvalidDataException("Factor has missing or interrupted durable attempts.");
                using (TowerPerformanceTrace.Measure("factor.reconstruct")) await verify(i, path, ct);
                storage.SealDirectory(ct); completed.Add(id);
            }
            journal.Close(); TowerRescreenAttempts.Verify(P("attempts.bin"), maximumFights);
            using (TowerPerformanceTrace.Measure("phase.final-reconstruction"))
                await finalize(ct);
            ct.ThrowIfCancellationRequested(); storage.Audit(ct);
            var result = new TowerCeilingExecutionReceipt("Complete", journal.Started, journal.Completed, setupSeconds, clock.Elapsed.TotalSeconds, completed);
            HarnessJson.WriteNew(P("execution.json"), result); HarnessJson.WriteNew(P("performance.json"), Performance("Complete"));
            HarnessJson.WriteNew(P(TowerCeilingScreenInputs.FinalFiles + ".pending"), TowerCeilingScreenInputs.Inventory(root));
            storage.Audit(ct); ct.ThrowIfCancellationRequested();
            File.Move(P(TowerCeilingScreenInputs.FinalFiles + ".pending"), P(TowerCeilingScreenInputs.FinalFiles));
            TowerBulkCampaign.VerifyFiles(root, TowerCeilingScreenInputs.FinalFiles, true, ct);
            ct.ThrowIfCancellationRequested();
            if (setupSeconds + clock.Elapsed.TotalSeconds > maximumSeconds) throw new InvalidDataException("Global deadline exceeded during final publication.");
            return result;
        }
        catch (Exception e)
        {
            // Retain partial directories and any unmatched S. Never reset the ledger or launch a following factor.
            // Failure preservation is best effort if the filesystem itself rejects all writes; the original exception survives.
            try { HarnessJson.WriteNew(P("performance-failure.json"), Performance("Failed")); } catch (IOException) { }
            try { HarnessJson.WriteNew(P("failure.json"), new { error = e.ToString(), journal.Started, journal.Completed,
                setupSeconds, executeSeconds = clock.Elapsed.TotalSeconds, completedFactors = completed, noResume = true }); } catch (IOException) { }
            throw;
        }
    }

    public static async Task<TowerCeilingExecutionReceipt> RunAsync(string output, CancellationToken token = default, Action<string>? progress = null)
    {
        token.ThrowIfCancellationRequested();
        var protocol = HarnessJson.Read<TowerCeilingScreenProtocol>(Path.Combine(output, "protocol.json"));
        TowerCeilingScreenInputs.ValidateLimits(protocol);
        var definitions = Array.Empty<TowerBalanceDefinition>(); TowerCeilingPreparationReceipt? receipt = null;
        return await ExecuteFactorsAsync(output, 32384, 129536, TowerCeilingScreenInputs.SetupSeconds(output, protocol),
            protocol.MaximumSeconds, protocol.MaximumBytes - protocol.PriorSetupBytes,
            async (i, path, options, ct) => { await TowerCompactBalanceRun.RunAsync(Path.Combine(output, TowerCeilingScreenContract.VariantId(i) + "-input"), path, definitions[i], options, token: ct); },
            async (i, path, ct) => { await VerifyFactor(path, definitions[i], receipt!.Variants[i], protocol, ct); },
            ct => {
                var input = TowerCeilingScreenInputs.Inputs(output, ct, checkLiveHistory: true); definitions = input.Definitions; receipt = input.Receipt;
                var expected = protocol.FrozenFiles.Keys.Concat(new[] { "protocol.json", "setup-charge.json", "started.json", "attempts.bin" }).ToHashSet(StringComparer.Ordinal);
                if (!expected.SetEquals(TowerBulkCampaign.Paths(output).Select(f => Path.GetRelativePath(output, f).Replace('\\', '/'))))
                    throw new InvalidDataException("Unexpected or unbound prepared file.");
                return Task.CompletedTask;
            },
            ct => {
                var evidence = Enumerable.Range(0, 4).Select(i => (IReadOnlyList<TowerBalanceEvidence>)HarnessJson.Read<TowerBalanceEvidence[]>(Path.Combine(output, TowerCeilingScreenContract.VariantId(i), "evidence.json"))).ToArray();
                HarnessJson.WriteNew(Path.Combine(output, "screen-result.json"), Reconstruct(definitions, evidence)); return Task.CompletedTask;
            }, token, progress);
    }

    internal static TowerCeilingScreenResult Reconstruct(IReadOnlyList<TowerBalanceDefinition> definitions,
        IReadOnlyList<IReadOnlyList<TowerBalanceEvidence>> evidence)
    {
        if (definitions.Count != 4 || evidence.Count != 4) throw new InvalidDataException("Reconstruction requires all four complete factors.");
        var cells = new List<TowerCeilingCellResult>(); var wins = new List<IReadOnlyList<int>>();
        for (var i = 0; i < 4; i++)
        {
            var d = definitions[i];
            if (d.Id != TowerCeilingScreenContract.VariantId(i) || d.Cells.Count != 253 || d.MaximumBattles != 32384
                || d.Cells.Any(c => c.MinimumSamples != 128 || c.Scenario.Seeds.Count != 128)
                || !d.Cells.Select(c => c.Id).SequenceEqual(definitions[0].Cells.Select(c => c.Id))
                || !d.Cells[0].Scenario.Seeds.SequenceEqual(definitions[0].Cells[0].Scenario.Seeds))
                throw new InvalidDataException("Reconstruction family/schedule differs across factors.");
            var report = TowerBalanceEvaluator.Evaluate(d, evidence[i]);
            if (report.Issues.Count != 0 || report.Cells.Any(c => c.Issues.Count != 0 || c.Valid != 128))
                throw new InvalidDataException("Incomplete, duplicated or mismatched archived evidence.");
            wins.Add(report.Cells.Select(c => c.Wins).ToArray());
            cells.AddRange(report.Cells.Select(c => new TowerCeilingCellResult(d.Id, c.Id, c.Wins, c.Defeats, c.Draws, TowerCeilingScreenContract.ScreenInterval(c.Wins))));
        }
        var selected = TowerCeilingScreenContract.Select(wins);
        return new(selected.HasValue ? "CandidateForFullFamilyConfirmation" : "Unresolved", selected, TowerCeilingScreenContract.Assess(wins), cells);
    }

    private static async Task VerifyFactor(string path, TowerBalanceDefinition definition, TowerCeilingPreparedVariant prepared,
        TowerCeilingScreenProtocol protocol, CancellationToken token)
    {
        var contract = TowerContractJson.Read<TowerBulkContract>(Path.Combine(path, "campaign.json"));
        TowerPortfolioConfirmation.Equal(definition, contract.Definition, "executed factor definition");
        if (contract.Kind != TowerCompactBalanceRun.Kind || contract.Options.ChunkSize != 32 || contract.Options.RetryReserve != 0
            || contract.Options.ExecutionMode != "prepared-v1" || contract.Options.StorageAccounting != TowerStorageAccountant.Mode
            || contract.Options.MaximumSeconds is < 1 || contract.Options.MaximumSeconds > protocol.MaximumSeconds
            || contract.Options.MaximumBytes > protocol.MaximumBytes || contract.PlannedBattles != 32384 || contract.MaximumAttempts != 32384)
            throw new InvalidDataException("Executed factor limits differ.");
        await TowerCompactBalanceRun.VerifyAsync(path, token);
        // Native reconstruction verifies every recipe/seed/outcome and full compact inventory.
        // Additionally bind every archived prepared roster to the zero-combat reviewed roster.
        VerifyRosterRecords(path, prepared, token);
    }

    internal static void VerifyRosterRecords(string path, TowerCeilingPreparedVariant prepared, CancellationToken token)
    {
        var sources = HarnessJson.Read<TowerBalanceRunSource[]>(Path.Combine(path, "sources.json"));
        if (!sources.Select(s => s.CellId).SequenceEqual(prepared.Cells.Select(c => c.Id)))
            throw new InvalidDataException("Roster source family/order differs.");
        foreach (var group in sources.GroupBy(s => s.RunDirectory))
        {
            var expected = group.ToDictionary(s => s.CompactCaseId!, s => prepared.Cells.Single(c => c.Id == s.CellId).ParticipantsHash);
            var batch = Path.Combine(path, group.Key);
            var plan = HarnessJson.Read<TowerCompactPlan>(Path.Combine(batch, "bulk-plan.json"));
            var rows = 0;
            for (var chunk = 0; chunk < (plan.PlannedBattles + 31) / 32; chunk++)
            {
                token.ThrowIfCancellationRequested();
                using var file = File.OpenRead(Path.Combine(batch, "chunks", chunk.ToString("D6", System.Globalization.CultureInfo.InvariantCulture), "records.json.gz"));
                using var gzip = new GZipStream(file, CompressionMode.Decompress);
                var records = JsonSerializer.Deserialize<TowerCompactRecord[]>(gzip, HarnessJson.Options)!;
                foreach (var record in records)
                {
                    if (!expected.TryGetValue(record.CaseId, out var hash) || record.PreparedHash != hash)
                        throw new InvalidDataException("Archived participant roster differs from preparation.");
                    rows++;
                }
            }
            if (rows != plan.PlannedBattles) throw new InvalidDataException("Incomplete roster reconstruction.");
        }
    }

    public static async Task<TowerCeilingScreenResult> VerifyAsync(string output, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested(); using var lease = TowerCompactBundle.AcquireWriter(output);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Verification cannot fight.")).Activate();
        TowerBulkCampaign.VerifyFiles(output, TowerCeilingScreenInputs.FinalFiles, true, token);
        if (File.Exists(Path.Combine(output, "failure.json")) || File.Exists(Path.Combine(output, "performance-failure.json")))
            throw new InvalidDataException("Failed/partial screen cannot verify as complete.");
        var input = TowerCeilingScreenInputs.Inputs(output, token); var p = input.Protocol;
        var started = HarnessJson.Read<JsonElement>(Path.Combine(output, "started.json"));
        if (started.GetProperty("protocolHash").GetString() != HarnessJson.FileHash(Path.Combine(output, "protocol.json"))
            || started.GetProperty("setupChargeHash").GetString() != HarnessJson.FileHash(Path.Combine(output, "setup-charge.json"))
            || started.GetProperty("maximumFights").GetInt32() != p.MaximumFights || started.GetProperty("maximumSeconds").GetDouble() != p.MaximumSeconds
            || started.GetProperty("maximumBytes").GetInt64() != p.MaximumBytes - p.PriorSetupBytes || started.GetProperty("setupSeconds").GetDouble() != TowerCeilingScreenInputs.SetupSeconds(output, p))
            throw new InvalidDataException("Started protocol/setup differs.");
        var evidence = new List<IReadOnlyList<TowerBalanceEvidence>>();
        for (var i = 0; i < 4; i++)
        {
            var path = Path.Combine(output, TowerCeilingScreenContract.VariantId(i));
            await VerifyFactor(path, input.Definitions[i], input.Receipt.Variants[i], p, token);
            evidence.Add(HarnessJson.Read<TowerBalanceEvidence[]>(Path.Combine(path, "evidence.json")));
        }
        var result = Reconstruct(input.Definitions, evidence);
        TowerPortfolioConfirmation.Equal(result, HarnessJson.Read<TowerCeilingScreenResult>(Path.Combine(output, "screen-result.json")), "whole-screen result");
        TowerRescreenAttempts.Verify(Path.Combine(output, "attempts.bin"), 129536);
        var execution = HarnessJson.Read<TowerCeilingExecutionReceipt>(Path.Combine(output, "execution.json"));
        var performance = HarnessJson.Read<JsonElement>(Path.Combine(output, "performance.json"));
        if (execution.Status != "Complete" || execution.Started != 129536 || execution.Completed != 129536
            || !execution.CompletedFactors.SequenceEqual(Enumerable.Range(0, 4).Select(TowerCeilingScreenContract.VariantId))
            || execution.SetupSeconds != TowerCeilingScreenInputs.SetupSeconds(output, p)
            || !double.IsFinite(execution.ExecuteSeconds) || execution.ExecuteSeconds < 0 || execution.ExecuteSeconds + execution.SetupSeconds > p.MaximumSeconds
            || performance.GetProperty("status").GetString() != "Complete" || performance.GetProperty("started").GetInt32() != 129536
            || performance.GetProperty("completed").GetInt32() != 129536
            || !double.IsFinite(performance.GetProperty("totalSeconds").GetDouble()) || performance.GetProperty("totalSeconds").GetDouble() > p.MaximumSeconds
            || performance.GetProperty("totalSeconds").GetDouble() < execution.SetupSeconds + execution.ExecuteSeconds
            || p.PriorSetupBytes + TowerBulkCampaign.StorageBytes(output, token) > p.MaximumBytes) throw new InvalidDataException("Final global accounting differs.");
        TowerBulkCampaign.VerifyFiles(output, TowerCeilingScreenInputs.FinalFiles, true, token); return result;
    }
}
