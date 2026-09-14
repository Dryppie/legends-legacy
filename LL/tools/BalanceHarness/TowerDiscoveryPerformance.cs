using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerDiscoveryPerformanceDefinition(int SchemaVersion, int[] Scales, int SamplesPerScale,
    int MaximumSeconds, long MaximumBytes, int MaximumFights, string ContentRoot, string ReferenceExecutable,
    string HistoricalRun, TowerPerformanceDefinition Combat, IReadOnlyDictionary<string, string> FrozenFiles);

/// <summary>One frozen, execute-once diagnostic: neutral archive shapes, saved-measurement reconstruction,
/// then paired existing recipes/seeds. No seed allocation or candidate-search experiment.</summary>
public static class TowerDiscoveryPerformance
{
    private static readonly string[] Shape = ["bulk-attempts.jsonl", "bulk-manifest.json", "bulk-plan.json", "bulk-resume.json",
        "bulk-scope.json", "bulk-status.json", "chunks/000000/receipt.json", "chunks/000000/records.json.gz",
        "inputs/input.json", "prepared/participants.json.gz", "recipes/recipe.json"];

    public static async Task RunAsync(string definitionPath, string output, CancellationToken token = default, Action<string>? progress = null)
    {
        var d = HarnessJson.Read<TowerDiscoveryPerformanceDefinition>(definitionPath);
        if (d.SchemaVersion != 1 || !d.Scales.SequenceEqual(new[] { 0, 1024, 4608, 9216 }) || d.SamplesPerScale != 16
            || d.MaximumSeconds is < 1 or > 1800 || d.MaximumBytes is < 1048576 or > 4294967296L
            || d.MaximumFights != 2 * TowerPerformanceBenchmark.Validate(d.Combat) || d.MaximumFights > 512
            || !d.Combat.WorkerCounts.SequenceEqual(new[] { 1 }) || d.Combat.Repetitions != 2
            || d.Combat.Cases.Count != 2 || d.Combat.Cases.Any(c => c.Scenario.Seeds.Count != 8))
            throw new InvalidDataException("Diagnostic must freeze four scales, sixteen writes, and two paired eight-seed cases with two passes and replays.");
        foreach (var (path, hash) in d.FrozenFiles)
            if (HarnessJson.FileHash(path) != hash) throw new InvalidDataException("Frozen diagnostic input changed: " + path);
        var savedSeeds = HarnessJson.Read<JsonElement>(Path.Combine(d.HistoricalRun, "seed-ledger.json")).GetProperty("discovery").Deserialize<int[]>(HarnessJson.Options)!;
        foreach (var item in d.Combat.Cases)
        {
            if (!d.FrozenFiles.ContainsKey(item.Provenance) || !item.Scenario.Seeds.SequenceEqual(savedSeeds))
                throw new InvalidDataException("Diagnostics require the frozen saved recipes and existing discovery seeds.");
            var savedRecipe = HarnessJson.Read<JsonElement>(item.Provenance).GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)!;
            if (HarnessJson.Hash(savedRecipe with { Seeds = savedSeeds }) != HarnessJson.Hash(item.Scenario))
                throw new InvalidDataException("Diagnostic recipe differs from its saved origin.");
        }
        output = Path.GetFullPath(output);
        if (Path.Exists(output)) throw new IOException("Diagnostics cannot retry or overwrite an existing result.");
        Directory.CreateDirectory(output);
        File.Copy(definitionPath, Path.Combine(output, "definition.json"));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(d.MaximumSeconds)); var ct = timeout.Token;
        var clock = Stopwatch.StartNew(); var process = Process.GetCurrentProcess(); var cpu = process.TotalProcessorTime;
        var allocated = GC.GetTotalAllocatedBytes(); var rows = new List<object>();
        var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Zero-combat diagnostic guard."));
        string status = "Failed"; string? error = null;
        try
        {
            using (trace.Activate())
            {
                Scale(d, output, rows, ct, progress);
                await Reconstruct(d.HistoricalRun, output, ct);
            }
            CheckSize(d, output, ct);
            // Use the captured pre-change executable, its own bounded benchmark, and an identical definition.
            var combatDefinition = Path.Combine(output, "combat-definition.json"); HarnessJson.WriteNew(combatDefinition, d.Combat);
            var reference = Path.Combine(output, "reference");
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var arg in new[] { d.ReferenceExecutable, "tower-performance", "--definition", combatDefinition, "--output", reference,
                "--content-root", d.ContentRoot, "--archive-format", TowerCompactBundle.Format, "--execution-mode", TowerPreparedBattle.Mode }) start.ArgumentList.Add(arg);
            progress?.Invoke("Frozen reference: 32 repeated trials plus two detailed replays; zero retries.");
            using (var worker = Process.Start(start) ?? throw new IOException("Reference failed to start."))
            {
                var stdout = worker.StandardOutput.ReadToEndAsync(); var stderr = worker.StandardError.ReadToEndAsync();
                try { await worker.WaitForExitAsync(ct); }
                finally
                {
                    if (!worker.HasExited) { worker.Kill(entireProcessTree: true); await worker.WaitForExitAsync(); }
                    File.WriteAllText(Path.Combine(output, "reference-stdout.log"), await stdout);
                    File.WriteAllText(Path.Combine(output, "reference-stderr.log"), await stderr);
                    HarnessJson.WriteNew(Path.Combine(output, "reference-exit.json"), new { worker.ExitCode });
                }
                if (worker.ExitCode != 0) throw new InvalidDataException("Frozen reference failed; no retry or candidate launch.");
            }
            var report = HarnessJson.Read<TowerPerformanceReport>(Path.Combine(reference, "performance.json"));
            var referenceScope = HarnessJson.Read<TowerPerformanceScope>(Path.Combine(reference, "scope.json"));
            var execution = ExecutionIdentity.Current();
            if (report.Status != "Complete" || report.StartedBattles != d.MaximumFights / 2 || report.CompletedBattles != report.StartedBattles
                || referenceScope.Execution.Runtime != execution.Runtime || referenceScope.Execution.OperatingSystem != execution.OperatingSystem
                || referenceScope.Execution.Architecture != execution.Architecture
                || !referenceScope.Execution.AssemblyHashes.Where(p => p.Key != "BalanceHarness").OrderBy(p => p.Key)
                    .SequenceEqual(execution.AssemblyHashes.Where(p => p.Key != "BalanceHarness").OrderBy(p => p.Key)))
                throw new InvalidDataException("Reference incomplete or gameplay assemblies/runtime differ.");
            CheckSize(d, output, ct);
            progress?.Invoke("Owned campaign: the same 32 trials and two replays, with exact report-digest comparison.");
            await Combat(d.Combat, d.ContentRoot, output, referenceScope, report, ct, reference);
            CheckSize(d, output, ct);
            status = "Complete";
        }
        catch (Exception e) { error = e.ToString(); throw; }
        finally
        {
            HarnessJson.WriteNew(Path.Combine(output, "diagnostic.json"), new { status, error, seconds = clock.Elapsed.TotalSeconds,
                parentCpuSeconds = (process.TotalProcessorTime - cpu).TotalSeconds, parentAllocatedBytes = GC.GetTotalAllocatedBytes() - allocated,
                parentPeakWorkingSetBytes = process.PeakWorkingSet64, rows, zeroCombatTimings = trace.Snapshot(),
                note = "OS cache uncontrolled; scale order ascending, legacy then owned at each scale. Child CPU/memory are in reference/performance.json. No balance evidence." });
        }
    }

    private static void Scale(TowerDiscoveryPerformanceDefinition d, string output, List<object> rows, CancellationToken token, Action<string>? progress)
    {
        var fixture = Path.Combine(output, "fixture"); var batches = Path.Combine(fixture, "batches"); Directory.CreateDirectory(batches);
        var prepared = 0;
        foreach (var scale in d.Scales)
        {
            var setup = Stopwatch.StartNew();
            using (TowerPerformanceTrace.Measure("fixture.prepare"))
                for (; prepared < scale; prepared++) WriteShape(Path.Combine(batches, "base-" + prepared.ToString("D6")), token);
            var setupSeconds = setup.Elapsed.TotalSeconds;
            foreach (var owned in new[] { false, true })
            {
                token.ThrowIfCancellationRequested(); var mode = owned ? TowerStorageAccountant.Mode : "legacy";
                var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Fixture cannot fight."));
                var samples = new List<double>(); var initialization = Stopwatch.StartNew();
                double initSeconds, auditSeconds;
                using (trace.Activate())
                {
                    var storage = owned ? new TowerStorageAccountant(fixture, d.MaximumBytes, [], token) : null;
                    initSeconds = initialization.Elapsed.TotalSeconds;
                    for (var sample = 0; sample < d.SamplesPerScale; sample++)
                    {
                        var watch = Stopwatch.StartNew();
                        using (TowerPerformanceTrace.Measure("fixture.candidate"))
                        {
                            Check(); // Inner pre-batch boundary.
                            var path = Path.Combine(batches, "sample-" + sample.ToString("D6")); storage?.BeginDirectory(path, token);
                            WriteShape(path, token);
                            if (sample == 0) Check(); // One outer boundary per 128 starts / sixteen eight-fight candidates.
                            Check(); // Inner chunk-completion boundary.
                            storage?.SealDirectory(token);
                        }
                        samples.Add(watch.Elapsed.TotalMilliseconds);
                    }
                    var audit = Stopwatch.StartNew();
                    if (storage is not null) storage.Audit(token); else TowerBulkCampaign.StorageBytes(fixture, token);
                    auditSeconds = audit.Elapsed.TotalSeconds;
                    void Check() { if (storage is not null) storage.Check(token); else if (TowerBulkCampaign.StorageBytes(fixture, token) > d.MaximumBytes) throw new InvalidDataException("Fixture cap."); }
                }
                var sorted = samples.Order().ToArray();
                var row = new { scale, mode, setupSeconds, initSeconds, auditSeconds, sampleMilliseconds = samples,
                    medianMilliseconds = (sorted[7] + sorted[8]) / 2, p95Milliseconds = sorted[15], timings = trace.Snapshot() };
                rows.Add(row); HarnessJson.WriteNew(Path.Combine(output, $"scale-{scale}-{mode}.json"), row);
                progress?.Invoke($"{scale} archives, {mode}: median {row.medianMilliseconds:F3} ms, tail {row.p95Milliseconds:F3} ms.");
                // Retain measured writes outside the fixture so both modes start at exactly the same prefix.
                var retained = Path.Combine(output, "samples", scale + "-" + mode); Directory.CreateDirectory(retained);
                for (var sample = 0; sample < d.SamplesPerScale; sample++)
                {
                    var name = "sample-" + sample.ToString("D6");
                    var source = Path.GetFullPath(Path.Combine(batches, name));
                    var destination = Path.GetFullPath(Path.Combine(retained, name));
                    if (!source.StartsWith(output + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                        || !destination.StartsWith(output + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Fixture move escaped output.");
                    Directory.Move(source, destination);
                }
                CheckSize(d, output, token);
            }
        }
    }

    internal static long FixtureArchiveBytes => Shape.Length * "neutral-fixture\n"u8.Length;

    internal static void WriteShape(string path, CancellationToken token)
    {
        foreach (var name in Shape)
        {
            token.ThrowIfCancellationRequested(); var file = Path.Combine(path, name); Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            using var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write); stream.Write("neutral-fixture\n"u8);
        }
    }

    private static async Task Reconstruct(string historical, string output, CancellationToken token)
    {
        using var timing = TowerPerformanceTrace.Measure("search.saved-reconstruction");
        var saved = HarnessJson.Read<BossDiscoveryRunReport>(Path.Combine(historical, "discovery/discovery.json"));
        var inputs = HarnessJson.Read<BossDiscoveryInputs>(Path.Combine(historical, "generation-inputs.json"));
        var mechanics = HarnessJson.Read<BossGenerationMechanics>(Path.Combine(historical, "generation-mechanics.json"));
        var generation = saved.Generation ?? throw new InvalidDataException("Missing historical generation.");
        var remaining = generation.Arms.SelectMany(a => a.Evaluations).ToDictionary(e => e.Id);
        var parties = generation.Arms.SelectMany(a => a.Proposals).Where(p => p.Result == "evaluated").ToDictionary(p => p.Party!.Id, p => p.Party!);
        var rebuilt = await TowerBossGeneration.RunAsync(inputs, mechanics, (party, _, ct) => {
            ct.ThrowIfCancellationRequested();
            if (!remaining.Remove(party.Id, out var measurement) || HarnessJson.Hash(party) != HarnessJson.Hash(parties[party.Id]))
                throw new InvalidDataException("Saved candidate trajectory or exact recipe changed.");
            return Task.FromResult(measurement);
        }, token);
        if (remaining.Count != 0 || HarnessJson.Hash(rebuilt) != HarnessJson.Hash(saved.Generation))
            throw new InvalidDataException("Full saved search, charges, rankings or nominations changed.");
        var d = TowerBossDiscovery.Read(Path.Combine(historical, "definition.json"));
        var shortlist = TowerFeedbackBenchmark.Freeze(d, saved);
        if (HarnessJson.Hash(shortlist) != HarnessJson.Hash(HarnessJson.Read<JsonElement>(Path.Combine(historical, "shortlist.json"))))
            throw new InvalidDataException("Saved shortlist changed.");
        var evidence = shortlist.Arms.Select((a, i) => new TowerFeedbackEvidence(a.Method, a.Seed,
            HarnessJson.Read<TowerBalanceEvidence[]>(Path.Combine(historical, $"rescreen-{i}/evidence.json")))).ToArray();
        var selected = TowerFeedbackBenchmark.Select(d, saved, shortlist, evidence);
        if (HarnessJson.Hash(selected) != HarnessJson.Hash(HarnessJson.Read<JsonElement>(Path.Combine(historical, "selected.json"))))
            throw new InvalidDataException("Saved screened nominations changed.");
        HarnessJson.WriteNew(Path.Combine(output, "saved-reconstruction.json"), new { status = "Exact", evaluations = parties.Count,
            generationHash = HarnessJson.Hash(rebuilt), shortlistHash = HarnessJson.Hash(shortlist), selectedHash = HarnessJson.Hash(selected), fights = 0 });
    }

    internal static async Task Combat(TowerPerformanceDefinition combat, string contentRoot, string output, TowerPerformanceScope reference,
        TowerPerformanceReport report, CancellationToken token, string referenceDirectory)
    {
        var destination = Path.Combine(output, "candidate"); Directory.CreateDirectory(destination);
        var maximum = TowerPerformanceBenchmark.Validate(combat);
        using var journal = new TowerRescreenAttempts(Path.Combine(destination, "attempts.bin"), maximum);
        TowerStorageAccountant? outer = null;
        var trace = new TowerPerformanceTrace(done => {
            token.ThrowIfCancellationRequested();
            if (!done && journal.Started % 128 == 0) outer?.Check(token);
            journal.Record(done);
        });
        using var active = trace.Activate(); var clock = Stopwatch.StartNew(); var process = Process.GetCurrentProcess();
        var cpu = process.TotalProcessorTime; var allocated = GC.GetTotalAllocatedBytes();
        var comparisons = new List<object>(); string status = "Failed"; string? error = null;
        var replays = new List<object>(); var reconstructed = false;
        try
        {
            var settings = TowerBundle.ReadSettings(contentRoot); var hashes = TowerCompactBundle.ContentHashes(contentRoot, token);
            if (HarnessJson.Hash(settings) != HarnessJson.Hash(reference.Settings) || HarnessJson.Hash(hashes) != HarnessJson.Hash(reference.ContentHashes))
                throw new InvalidDataException("Reference/candidate content or settings differ.");
            var options = new TowerBulkOptions(32, 0, combat.MaxSeconds, combat.MaxOutputBytes, StorageAccounting: TowerStorageAccountant.Mode);
            var campaignPath = Path.Combine(destination, "campaign"); var batch = 0; var planned = maximum - combat.Cases.Count;
            outer = new TowerStorageAccountant(destination, combat.MaxOutputBytes, ["attempts.bin", "performance.json"], token);
            foreach (var item in combat.Cases) outer.AllowMetadata(item.Id + "-replay.json");
            outer.BeginDirectory(campaignPath, token);
            using (TowerStorageOwnership.Activate(outer))
            using (var campaign = TowerBulkCampaign.Open(contentRoot, campaignPath, TowerCompactDiscovery.Kind, combat, hashes,
                HarnessJson.Hash(settings), HarnessJson.Hash(ExecutionIdentity.Current()), planned, planned, options, false, false, token, null))
            {
                foreach (var pass in report.Workers.Single().Passes.OrderBy(p => p.Repetition))
                foreach (var item in combat.Cases)
                {
                    var watch = Stopwatch.StartNew();
                    var saved = await campaign.BatchAsync("discovery-" + (batch++).ToString("D6"), [new(item.Id, item.Scenario)]);
                    var expected = pass.Cases.Single(c => c.CaseId == item.Id);
                    if (saved.ResultDigests[item.Id] != expected.ResultDigest) throw new InvalidDataException("Exact combat report digest differs.");
                    comparisons.Add(new { pass.Repetition, item.Id, expected.ResultDigest, milliseconds = watch.Elapsed.TotalMilliseconds });
                    outer.Check(token);
                }
                campaign.Finish(planned);
            }
            outer.SealDirectory(token); outer.Audit(token);
            // Full historical campaign reconstruction with an explicit no-combat guard.
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Reconstruction cannot fight.")).Activate())
            using (var campaign = TowerBulkCampaign.Open(Path.Combine(campaignPath, "content"), campaignPath, TowerCompactDiscovery.Kind, combat,
                hashes, HarnessJson.Hash(settings), HarnessJson.Hash(ExecutionIdentity.Current()), planned, planned, options, true, true, token, null))
            {
                for (var i = 0; i < batch; i++) await campaign.BatchAsync("discovery-" + i.ToString("D6"),
                    [new(combat.Cases[i % combat.Cases.Count].Id, combat.Cases[i % combat.Cases.Count].Scenario)]);
                campaign.Finish(planned);
                reconstructed = true;
            }
            for (var i = 0; i < combat.Cases.Count; i++)
            {
                var item = combat.Cases[i];
                var replay = await TowerCompactBundle.ReplayAsync(Path.Combine(campaignPath, "batches", "discovery-" + i.ToString("D6")), item.Id, "tower.0001", true, token);
                HarnessJson.WriteNew(Path.Combine(destination, item.Id + "-replay.json"), replay);
                var expected = HarnessJson.Read<TowerBattleReport>(Path.Combine(referenceDirectory, "workers-1", item.Id + "-replay.json"));
                if (HarnessJson.Hash(replay) != HarnessJson.Hash(expected)) throw new InvalidDataException("Full detailed replay differs from the retained reference.");
                replays.Add(new { item.Id, digest = HarnessJson.Hash(replay) });
                outer.Check(token);
            }
            journal.Close(); TowerRescreenAttempts.Verify(Path.Combine(destination, "attempts.bin"), maximum);
            if (journal.Started != maximum || journal.Completed != journal.Started) throw new InvalidDataException("Diagnostic fight accounting differs.");
            outer.Audit(token);
            status = "Exact";
        }
        catch (Exception e) { error = e.ToString(); throw; }
        finally
        {
            HarnessJson.WriteNew(Path.Combine(destination, "performance.json"), new { status, error, journal.Started, journal.Completed,
                seconds = clock.Elapsed.TotalSeconds, cpuSeconds = (process.TotalProcessorTime - cpu).TotalSeconds,
                allocatedBytes = GC.GetTotalAllocatedBytes() - allocated, peakWorkingSetBytes = process.PeakWorkingSet64,
                comparisons, replays, reconstructed, timings = trace.Snapshot() });
        }
    }

    private static void CheckSize(TowerDiscoveryPerformanceDefinition d, string output, CancellationToken token)
    {
        if (TowerBulkCampaign.StorageBytes(output, token) > d.MaximumBytes) throw new InvalidDataException("Total diagnostic output cap exceeded.");
    }
}
