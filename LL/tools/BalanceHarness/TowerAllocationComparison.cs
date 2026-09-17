using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerAllocationComparisonRequest(string Version, string ContentRoot, string TemplatePath,
    string TemplateHash, string RegistryRoot, string OutputRoot, IReadOnlyDictionary<string, string> RequiredHistory,
    IReadOnlyDictionary<string, string> PendingHistoryRecoveries, IReadOnlyDictionary<string, string> RecoveryReceiptHashes,
    int MaximumSeconds = 2400, long MaximumBytes = 2147483648);
public sealed record AllocationComparisonPair(int Restart, string BaselineParty, string RacingParty,
    int BaselineWins, int RacingWins, int Gains, int Losses, double Difference,
    int BaselineDiscoveryFights, int RacingDiscoveryFights);
public sealed record AllocationComparisonResult(string Version, string Status, string Decision,
    IReadOnlyList<AllocationComparisonPair> Pairs, double MeanDifference, double LowerBound,
    double CollisionAllowance, int Started, int Completed, double ElapsedSeconds);
internal sealed record AllocationComparisonReservation(int[] Selected, int[] Reserved, int HistoricalCollisions, int Duplicates);

/// <summary>A fixed three-restart experiment. Search outputs are all frozen before confirmation can start.</summary>
public static class TowerAllocationComparison
{
    public const string Version = "tower-allocation-comparison-v1";
    internal const int Restarts = 3, Samples = 1000, ValuesPerRestart = 1081, EntropyWords = 4096;
    internal const int MaximumFights = 20976;
    private static void Require(bool condition, string message) => TowerPracticalSearch.Require(condition, message);

    internal static AllocationComparisonReservation Classify(byte[] entropy, IReadOnlyList<int> historical,
        int selectedCount = Restarts * ValuesPerRestart)
    {
        Require(selectedCount is > 0 and <= EntropyWords, "Invalid comparison reservation size.");
        Require(entropy.Length == EntropyWords * 4, "One complete entropy batch is required; no refill.");
        var prior = historical.ToHashSet(); var seen = new HashSet<int>(); var fresh = new List<int>();
        var collisions = 0; var duplicates = 0;
        for (var offset = 0; offset < entropy.Length; offset += 4)
        {
            var value = BinaryPrimitives.ReadInt32LittleEndian(entropy.AsSpan(offset, 4));
            if (prior.Contains(value)) collisions++;
            else if (!seen.Add(value)) duplicates++;
            else fresh.Add(value);
        }
        return new(fresh.Take(selectedCount).ToArray(), fresh.Order().ToArray(), collisions, duplicates);
    }

    internal static TowerBossDiscoveryDefinition Bind(TowerBossDiscoveryDefinition template, int[] values, int restart, bool racing)
    {
        Require(values.Length == Restarts * ValuesPerRestart && values.Distinct().Count() == values.Length
            && restart is >= 0 and < Restarts && !values.Intersect(template.ExcludedCombatSeeds).Any(), "Invalid comparison allocation.");
        var panel = values.Skip(restart * ValuesPerRestart).Take(ValuesPerRestart).ToArray();
        var confirmation = panel.Skip(81).ToArray();
        var d = template with {
            // Keep the scenario label identical across methods in each paired restart.
            Id = "allocation-pair-" + (restart + 1),
            Generation = template.Generation with { Seeds = [panel[0]], CandidatesPerArm = racing ? 16 : 46,
                PolicyVersion = racing ? TowerEvaluationAllocationSearch.Version : TowerSuppliedCompositionSearch.IncumbentVersion },
            Stages = template.Stages with { Schedules = template.Stages.Schedules.ToDictionary(p => p.Key,
                _ => new BossDiscoverySchedule(panel.Skip(1).Take(racing ? 48 : 8).ToArray(), panel.Skip(49).Take(32).ToArray(), confirmation, [])) },
            MaximumBattles = MaximumFights / 6
        };
        var cost = TowerBossDiscovery.Validate(d);
        Require(cost.Discovery == 368 && cost.Selection == 128 && cost.Total == MaximumFights / 6, "Unequal comparison ceiling.");
        return d;
    }

    // There are no outcome inputs to this barrier. A missing/failed search cannot release it.
    internal sealed class FreezeBarrier(Action<IReadOnlyList<BossConfirmationFreeze>> publish)
    {
        private readonly object sync = new();
        private readonly BossConfirmationFreeze?[] freezes = new BossConfirmationFreeze?[6];
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Task Arrive(int index, BossConfirmationFreeze family, CancellationToken ct)
        {
            lock (sync)
            {
                Require(index is >= 0 and < 6 && freezes[index] is null, "Duplicate or unknown comparison freeze.");
                freezes[index] = family;
                if (freezes.All(f => f is not null))
                {
                    try { publish(freezes.Select(f => f!).ToArray()); ready.SetResult(); }
                    catch (Exception error) { ready.TrySetException(error); throw; }
                }
            }
            return ready.Task.WaitAsync(ct);
        }
    }

    internal static AllocationComparisonResult Assess(IReadOnlyList<BossStudyReport> reports, int historicalCount,
        int started, int completed, double elapsed, int searchValuesPerRestart = 81)
    {
        Require(reports.Count == 6 && reports.All(r => r.Status == "Complete") && started == completed
            && completed == reports.Sum(r => r.Accounting.Completed.Values.Sum()) && completed <= MaximumFights,
            "Every planned study and attempted fight must complete.");
        var pairs = new List<AllocationComparisonPair>(); var allSeeds = new HashSet<int>();
        for (var pair = 0; pair < Restarts; pair++)
        {
            var a = reports[pair * 2]; var b = reports[pair * 2 + 1];
            (string Party, TowerBalanceEvidence Evidence) Primary(BossStudyReport r)
            {
                var member = r.Confirmation!.Members.Single(m => m.Primary);
                var evidence = r.Evidence.Single(e => e.CellId == member.CellId);
                Require(evidence.Status == "Complete" && evidence.Trials.Count == Samples, "Incomplete primary confirmation.");
                return (member.GeneratedIds.Single(), evidence);
            }
            var x = Primary(a); var y = Primary(b);
            Require(x.Evidence.Trials.Select(t => t.Seed).SequenceEqual(y.Evidence.Trials.Select(t => t.Seed))
                && x.Evidence.Trials.All(t => allSeeds.Add(t.Seed)), "Unpaired or reused confirmation panel.");
            var gains = 0; var losses = 0; var winsA = 0; var winsB = 0;
            foreach (var (first, second) in x.Evidence.Trials.Zip(y.Evidence.Trials))
            {
                var oldWin = first.Outcome == BattleOutcome.Victory; var newWin = second.Outcome == BattleOutcome.Victory;
                if (oldWin) winsA++; if (newWin) winsB++;
                if (newWin && !oldWin) gains++; if (oldWin && !newWin) losses++;
            }
            pairs.Add(new(pair + 1, x.Party, y.Party, winsA, winsB, gains, losses, (gains - losses) / (double)Samples,
                a.Accounting.Completed["discovery"], b.Accounting.Completed["discovery"]));
        }
        // Conditional on all search observations, 3,000 paired differences lie in [-1,1].
        // Couple uniform sampling without replacement to independent draws. Charging the
        // birthday union bound to alpha makes the Hoeffding bound conservative for this batch.
        var n = Restarts * Samples;
        var population = 4294967296d - historicalCount - Restarts * searchValuesPerRestart;
        var collision = n * (n - 1d) / (2 * population);
        Require(historicalCount >= 0 && population > n && collision < .05, "Invalid confidence population.");
        var mean = pairs.Average(p => p.Difference);
        var lower = Math.Max(-1, mean - Math.Sqrt(2 * Math.Log(1 / (.05 - collision)) / n));
        // Compare the exact integer total at the five-point boundary; averaging
        // doubles such as .100, .025, .025 can round just below .05.
        var decision = pairs.Sum(p => p.Gains - p.Losses) >= n / 20 && lower > 0 && pairs.Count(p => p.Difference > 0) >= 2
            ? "SupportsRacingForFrozenOutputs" : "DoNotPromoteRacing";
        return new(Version, "Verified", decision, pairs, mean, lower, collision, started, completed, elapsed);
    }

    public static Task<AllocationComparisonResult> RunAsync(TowerAllocationComparisonRequest request, CancellationToken token = default)
        => RunCoreAsync(request, Version, TowerEvaluationAllocationSearch.Version, 16, 48, Restarts * ValuesPerRestart,
            Bind, (reports, historical, started, completed, elapsed) => Assess(reports, historical, started, completed, elapsed), token);

    // Version-specific binding and assessment remain explicit. The owned execution,
    // durable reservation, freeze barrier and archive reconstruction are shared.
    internal static async Task<TResult> RunCoreAsync<TResult>(TowerAllocationComparisonRequest request,
        string version, string policy, int candidates, int discoverySamples, int selectedCount,
        Func<TowerBossDiscoveryDefinition, int[], int, bool, TowerBossDiscoveryDefinition> bind,
        Func<IReadOnlyList<BossStudyReport>, int, int, int, double, TResult> assess, CancellationToken token)
    {
        var clock = Stopwatch.StartNew(); var q = request;
        Require(q.Version == version && q.MaximumSeconds is >= 60 and <= 2400
            && q.MaximumBytes is >= 16777216 and <= 2147483648, "Invalid comparison envelope or version.");
        var shape = new TowerPracticalRequest(TowerPracticalSearch.AllocationVersion, q.ContentRoot, q.TemplatePath, q.TemplateHash,
            q.RegistryRoot, q.OutputRoot, q.RequiredHistory, q.MaximumSeconds, q.MaximumBytes,
            PendingHistoryRecoveries: q.PendingHistoryRecoveries, RecoveryReceiptHashes: q.RecoveryReceiptHashes,
            Allocation: new(0, "comparison-shape-only", discoverySamples, 32, 1000));
        TowerPracticalSearch.ValidateRequest(shape);
        using var registryLease = TowerCompactBundle.AcquireWriter(Path.Combine(q.RegistryRoot, "complete-family-allocation"));
        using var outputLease = TowerCompactBundle.AcquireWriter(q.OutputRoot);
        Require(!Path.Exists(q.OutputRoot), "New comparison output required; retries and resume are forbidden.");
        Directory.CreateDirectory(q.OutputRoot);
        string P(string name) => Path.Combine(q.OutputRoot, name);
        void Save(string name, object value) => HarnessJson.WriteNew(P(name), value);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        stop.CancelAfter(TimeSpan.FromSeconds(q.MaximumSeconds - 15));
        // The hard deadline covers non-cooperative native work; the owned launcher bounds the entire process tree.
        using var deadline = new Timer(_ => Environment.Exit(130), null, TimeSpan.FromSeconds(q.MaximumSeconds - 5), Timeout.InfiniteTimeSpan);
        var lastStorage = -10d;
        void Check()
        {
            stop.Token.ThrowIfCancellationRequested();
            if (clock.Elapsed.TotalSeconds - lastStorage >= 5)
            {
                Require(TowerBulkCampaign.StorageBytes(q.OutputRoot, stop.Token) < q.MaximumBytes - 4 * 1048576,
                    "Comparison storage ceiling reached.");
                lastStorage = clock.Elapsed.TotalSeconds;
            }
        }
        try
        {
            Save("request.json", q);
            var inputs = TowerPracticalSearch.Inspect(shape, stop.Token);
            var template = inputs.Definition;
            Require(template.Generation.PolicyVersion == policy
                && template.Generation.CandidatesPerArm == candidates && template.Generation.MaximumAttemptsPerArm == 256,
                "Comparison template requires its declared policy, candidate count and fixed 256 proposal-attempt ceiling.");
            Save("template.json", template); Save("history-files.json", inputs.History.Files);
            var storage = new TowerCompleteReservation.Storage(q.OutputRoot, q.MaximumBytes - 4 * 1048576);
            storage.Put("history-input.json", new { reservationState = "Pending", version });
            storage.Put("entropy-intent.json", new { version, words = EntropyWords, selectedValues = selectedCount,
                historicalHash = HarnessJson.Hash(inputs.History.Values), retries = 0 });
            Check();
            var entropy = new byte[EntropyWords * 4]; RandomNumberGenerator.Fill(entropy);
            // No cancellation check between drawing and durably retaining every byte.
            storage.PutBytes("entropy.bin", entropy);
            var allocation = Classify(entropy, inputs.History.Values, selectedCount);
            storage.Put("allocation.json", allocation);
            storage.Put("seed-ledger.json", new { historical = inputs.History.Values, reserved = allocation.Reserved });
            TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, inputs.History.Files, stop.Token);
            storage.Put("history-input.json", new { reservationState = "Complete", reserved = allocation.Reserved }, true);
            Require(allocation.Selected.Length == selectedCount, "Entropy batch exhausted; no replacement draw.");
            var definitions = Enumerable.Range(0, 6).Select(i => bind(template, allocation.Selected, i / 2, i % 2 == 1)).ToArray();
            Save("definitions.json", definitions);
            using var attempts = new TowerPracticalSearch.Attempts(P("attempts.jsonl"), MaximumFights, Check);
            using var fights = new SemaphoreSlim(1);
            var barrier = new FreezeBarrier(freezes => {
                Check(); Save("all-outputs-frozen.json", new { freezes, afterAttempts = attempts.Completed });
                Console.WriteLine("All six outputs frozen; paired confirmation begins.");
            });
            async Task<BossStudyReport> Run(int index)
            {
                try
                {
                    var report = await TowerBossStudy.RunWithAttemptsAsync(q.ContentRoot, P("study-" + index), definitions[index], complete => {
                        if (!complete) fights.Wait(stop.Token);
                        try { attempts.Event(complete); }
                        finally { if (complete) fights.Release(); }
                    }, stop.Token, message => Console.WriteLine($"[{index}] {message}"),
                        (family, ct) => barrier.Arrive(index, family, ct));
                    Require(report.Status == "Complete", "Comparison study " + index + " did not complete: " + report.Error);
                    return report;
                }
                catch { stop.Cancel(); throw; }
            }
            var reports = await Task.WhenAll(Enumerable.Range(0, 6).Select(Run));
            var elapsedCombat = clock.Elapsed.TotalSeconds;
            for (var index = 0; index < 6; index++)
            {
                Check(); Console.WriteLine($"Verifying retained study {index + 1}/6 without combat.");
                var replayed = await TowerBossStudy.VerifyAsync(P("study-" + index), stop.Token);
                Require(HarnessJson.Hash(replayed) == HarnessJson.Hash(reports[index]), "Native study reconstruction differs.");
            }
            Require(HarnessJson.Hash(Classify(File.ReadAllBytes(P("entropy.bin")), inputs.History.Values, selectedCount)) == HarnessJson.Hash(allocation),
                "Reservation reconstruction differs.");
            TowerRefinementComparisonLaunch.Recheck(q.RegistryRoot, q.OutputRoot, inputs.History.Files, stop.Token);
            var result = assess(reports, inputs.History.Values.Length, attempts.Started, attempts.Completed, clock.Elapsed.TotalSeconds);
            Save("verification.json", new { studyHashes = reports.Select(HarnessJson.Hash).ToArray(), elapsedCombat,
                newFightsDuringVerification = attempts.Completed - reports.Sum(r => r.Accounting.Completed.Values.Sum()),
                confirmationFreezeHash = HarnessJson.FileHash(P("all-outputs-frozen.json")) });
            Save("result.json", result); Check();
            return result;
        }
        catch (Exception error)
        {
            Save("failure.json", new { status = "TerminalFailure", error = error.ToString(), retries = 0, elapsedSeconds = clock.Elapsed.TotalSeconds });
            throw;
        }
    }
}
