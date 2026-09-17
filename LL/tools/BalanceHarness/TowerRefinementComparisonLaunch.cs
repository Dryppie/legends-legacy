using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerRefinementLaunchRequest(TowerRefinementPreflightRequest Preflight, string RegistryRoot,
    string StudyRoot, int Master, int MaximumBindingSeconds, int MaximumSeconds, long MaximumBytes,
    string ProtocolHash, string ExecutionHash,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] string? ArchiveProfile = null,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<string, string>? PendingHistoryRecoveries = null);
public sealed record TowerRefinementLaunchAuthorization(string RequestHash, string ProtocolHash,
    int FreshValues, int MaximumAttempts, int Retries);
public sealed record TowerRefinementLiveHistory(IReadOnlyDictionary<string, string> Files, int[] Values);
internal sealed record TowerRefinementLaunchInputs(TowerRefinementPreflightResult Preflight,
    TowerBossDiscoveryDefinition Source, TowerRefinementLiveHistory History);
internal sealed record TowerRefinementBinding(string RequestHash, string AuthorizationHash, string PreflightHash,
    string SeedsHash, string ExecutionHash, int ChargedBindingSeconds, IReadOnlyDictionary<string, string> HistoryFiles);

/// <summary>Explicitly authorized reservation and launch. Check performs no allocation or combat.</summary>
public static class TowerRefinementComparisonLaunch
{
    internal const string BindingFiles = "binding-files.json";
    private const long ReserveBytes = 1048576;
    private static void Require(bool value, string message) => TowerRefinementComparisonModel.Require(value, message);
    private static string P(TowerRefinementLaunchRequest q, string name) => Path.Combine(q.StudyRoot, name);
    private static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value, HarnessJson.Options), HarnessJson.Options)!;

    internal static void Validate(TowerRefinementLaunchRequest q, TowerRefinementLaunchAuthorization? permit = null)
    {
        TowerSharedExecutable.ValidateProfile(q.ArchiveProfile);
        TowerRefinementComparisonModel.ResolveVersion(q.Preflight.ComparisonVersion);
        Require(q.MaximumBindingSeconds is >= 1 and <= 600 && q.MaximumSeconds > q.MaximumBindingSeconds
            && q.MaximumSeconds <= TowerRefinementComparisonRun.MaximumSeconds
            && q.MaximumBytes is >= 16 * 1048576 and <= TowerRefinementComparisonRun.MaximumBytes
            && TowerContractJson.Hash(q.ProtocolHash) && q.ExecutionHash == HarnessJson.Hash(ExecutionIdentity.Current()), "Changed launch identity or envelope.");
        Require(Path.IsPathFullyQualified(q.RegistryRoot) && Path.IsPathFullyQualified(q.StudyRoot)
            && string.Equals(Path.GetDirectoryName(Path.GetFullPath(q.StudyRoot)), Path.GetFullPath(q.RegistryRoot).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase),
            "Study must be a new direct child of the complete registry root.");
        Require(q.Preflight.Pins.Keys.All(n => !Path.GetFullPath(n).StartsWith(Path.GetFullPath(q.StudyRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)), "Study overlaps required input.");
        if (q.PendingHistoryRecoveries is { } recoveries)
            Require(recoveries.Count > 0 && recoveries.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() == recoveries.Count
                && recoveries.All(p => Path.IsPathFullyQualified(p.Key) && Path.IsPathFullyQualified(p.Value)
                    && Path.GetFileName(p.Key) == "history-input.json" && q.Preflight.Pins.ContainsKey(p.Key)
                    && q.Preflight.Pins.ContainsKey(p.Value)), "Recovery receipts and Pending inputs must be explicitly pinned.");
        for (var path = Path.GetFullPath(q.RegistryRoot); path is not null; path = Path.GetDirectoryName(path))
            Require((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0, "Linked registry ancestor.");
        if (permit is not null)
            Require(permit.RequestHash == HarnessJson.Hash(q) && permit.ProtocolHash == q.ProtocolHash
                && permit.FreshValues == 45 && permit.MaximumAttempts == TowerRefinementComparisonRun.MaximumAttempts
                && permit.Retries == 0, "Exact external 45-value/288-attempt authorization required.");
    }

    private static void BoundPreflight(TowerRefinementLaunchRequest q, TowerRefinementLaunchInputs inputs)
        => Require(inputs.Preflight.Version == TowerRefinementComparisonModel.ResolveVersion(q.Preflight.ComparisonVersion)
            && inputs.Preflight.RequestHash == HarnessJson.Hash(q.Preflight), "Preflight differs from requested comparison/version.");

    internal static TowerRefinementLiveHistory Refresh(string registry, string study,
        IReadOnlyDictionary<string, string> required, int[] expected, CancellationToken ct,
        IReadOnlyDictionary<string, string>? pendingRecoveries = null, Func<string, int, int>? recordedCandidate = null)
    {
        var paths = TowerHistoryRegistry.Scan(registry, study, ct);
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.Ordinal); var values = new HashSet<int>();
        var recoveries = pendingRecoveries is null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(pendingRecoveries, StringComparer.OrdinalIgnoreCase);
        var recovered = 0;
        foreach (var path in paths.Order(StringComparer.Ordinal)) {
            ct.ThrowIfCancellationRequested(); var hash = HarnessJson.FileHash(path); files.Add(path, hash);
            if (recoveries.TryGetValue(path, out var receipt)) {
                var version = HarnessJson.Read<JsonElement>(receipt).GetProperty("version").GetString();
                values.UnionWith(version switch {
                    TowerPracticalReservationRecovery.Version or TowerPracticalReservationRecovery.AllocationVersion
                        => TowerPracticalReservationRecovery.ReadPending(path, receipt, ct, recordedCandidate),
                    TowerRefinementReservationRecovery.Version => TowerRefinementReservationRecovery.ReadPending(path, receipt, ct, recordedCandidate),
                    _ => throw new InvalidDataException("Unknown Pending recovery receipt version.") }); recovered++;
            }
            else if (seen.Add(hash)) values.UnionWith(TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(path)));
            Require(HarnessJson.FileHash(path) == hash, "History changed while reading it.");
        }
        Require(recovered == recoveries.Count, "Recovery receipt does not identify a discovered Pending source.");
        Require(required.Count > 0 && required.All(p => files.TryGetValue(p.Key, out var h) && h == p.Value), "Lost required reservation source.");
        Require(values.Order().SequenceEqual(expected), "Live registry differs from the frozen authoritative ledger.");
        return new(files, values.Order().ToArray());
    }

    internal static void Recheck(string registry, string study, IReadOnlyDictionary<string, string> files, CancellationToken ct)
    {
        Require(TowerHistoryRegistry.Scan(registry, study, ct).SetEquals(files.Keys), "Registry membership changed during binding/launch.");
        foreach (var (path, hash) in files) { ct.ThrowIfCancellationRequested(); Require(HarnessJson.FileHash(path) == hash, "History bytes changed."); }
    }

    private static TowerRefinementLaunchInputs Inspect(TowerRefinementLaunchRequest q, CancellationToken ct)
    {
        var preflight = TowerRefinementComparisonPreflight.Check(q.Preflight, ct);
        var required = HarnessJson.Read<Dictionary<string, string>>(q.Preflight.RegistrySnapshotPath);
        required[q.Preflight.LedgerPath] = q.Preflight.Pins[q.Preflight.LedgerPath];
        var expected = TowerRefinementComparisonPreflight.History(HarnessJson.Read<JsonElement>(q.Preflight.LedgerPath), q.Preflight.ExpectedReservations);
        return new(preflight, TowerBossDiscovery.Read(q.Preflight.DefinitionPath), Refresh(q.RegistryRoot, q.StudyRoot, required, expected, ct, q.PendingHistoryRecoveries));
    }

    public static object Check(TowerRefinementLaunchRequest request, CancellationToken token = default)
    {
        var q = Copy(request); Validate(q); Require(!Path.Exists(q.StudyRoot), "New study required.");
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token); stop.CancelAfter(TimeSpan.FromSeconds(q.MaximumBindingSeconds));
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Check cannot fight.")).Activate();
        var inputs = Inspect(q, stop.Token); stop.Token.ThrowIfCancellationRequested();
        return new { status = "LiveInputsVerifiedNoAllocation", requestHash = HarnessJson.Hash(q), inputs.Preflight,
            historyFiles = inputs.History.Files, reservations = inputs.History.Values.Length, newSeeds = 0, fights = 0, runAuthorized = false };
    }

    public static void ReserveAndBind(TowerRefinementLaunchRequest request, TowerRefinementLaunchAuthorization authorization,
        CancellationToken token = default)
    {
        var q = Copy(request); var permit = Copy(authorization);
        BindCore(q, permit, ct => Inspect(q, ct), (files, ct) => {
            Recheck(q.RegistryRoot, q.StudyRoot, files, ct); TowerRefinementComparisonPreflight.VerifyPins(q.Preflight.Pins, [], ct);
        }, token);
    }

    internal static void BindCore(TowerRefinementLaunchRequest q, TowerRefinementLaunchAuthorization permit,
        Func<CancellationToken, TowerRefinementLaunchInputs> inspect,
        Action<IReadOnlyDictionary<string, string>, CancellationToken> recheck, CancellationToken token,
        Func<string, int, int>? candidate = null, Action<string>? boundary = null)
    {
        Require(permit is not null, "External authorization required."); Validate(q, permit); token.ThrowIfCancellationRequested();
        using var encoding = TowerSharedExecutable.Activate(q.ArchiveProfile);
        // Cooperate with the existing complete-family allocator. Other legacy allocators still need exclusive operation.
        using var registryLease = TowerCompactBundle.AcquireWriter(Path.Combine(q.RegistryRoot, "complete-family-allocation"));
        using var lease = TowerCompactBundle.AcquireWriter(q.StudyRoot);
        Require(!Path.Exists(q.StudyRoot), "No binding retry, resume or overwrite."); Directory.CreateDirectory(q.StudyRoot);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token); stop.CancelAfter(TimeSpan.FromSeconds(q.MaximumBindingSeconds)); var ct = stop.Token;
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Binding cannot fight.")).Activate();
        var storage = new TowerCompleteReservation.Storage(q.StudyRoot, Math.Min(64L * 1048576, q.MaximumBytes - ReserveBytes));
        storage.Put("binding-started.json", new { requestHash = HarnessJson.Hash(q), maximumSeconds = q.MaximumBindingSeconds });
        try {
            storage.Put("request.json", q); storage.Put("authorization.json", permit);
            var inputs = inspect(ct);
            BoundPreflight(q, inputs);
            var seeds = TowerRefinementReservation.Reserve(q.StudyRoot, inputs.History.Values, q.Master,
                Math.Min(64L * 1048576, q.MaximumBytes - ReserveBytes), ct, candidate, boundary);
            // Refresh the small top-level accounting after the allocator's own writes.
            storage = new TowerCompleteReservation.Storage(q.StudyRoot, Math.Min(64L * 1048576, q.MaximumBytes - ReserveBytes));
            var definitions = TowerRefinementComparisonModel.Policies.Select(p => TowerRefinementComparisonModel.Definition(inputs.Source, seeds, p, q.Preflight.ComparisonVersion)).ToArray();
            TowerRefinementComparisonModel.ValidatePair(definitions); storage.Put("definitions.json", definitions);
            TowerRefinementReservation.Verify(q.StudyRoot, seeds, q.Master, ct, candidate, false);
            recheck(inputs.History.Files, ct); boundary?.Invoke("before-complete"); ct.ThrowIfCancellationRequested();
            storage.Put("binding.json", new TowerRefinementBinding(HarnessJson.Hash(q), HarnessJson.Hash(permit), HarnessJson.Hash(inputs.Preflight),
                HarnessJson.Hash(seeds), q.ExecutionHash, q.MaximumBindingSeconds, inputs.History.Files));
            storage.Put("history-input.json", new { reservationState = "Complete", reserved = seeds.Generation.Concat(seeds.Discovery).Concat(seeds.Selection).Concat(seeds.Confirmation).ToArray() }, true);
            TowerRefinementReservation.Verify(q.StudyRoot, seeds, q.Master, ct, candidate);
            storage.Put(BindingFiles, Directory.EnumerateFiles(q.StudyRoot).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
            ct.ThrowIfCancellationRequested(); TowerBulkCampaign.VerifyFiles(q.StudyRoot, BindingFiles, true, ct);
        } catch (Exception e) { Failure(q, "binding-failure.json", e); throw; }
    }

    public static Task<TowerRefinementComparisonQuality> Run(TowerRefinementLaunchRequest request,
        TowerRefinementLaunchAuthorization authorization, CancellationToken token = default)
    {
        var q = Copy(request); var permit = Copy(authorization);
        return RunCore(q, permit, ct => Inspect(q, ct),
            (path, definitions, seconds, bytes, ct) => TowerRefinementComparisonRun.RunAsync(q.Preflight.ContentRoot, path, definitions, seconds, bytes, ct, q.ArchiveProfile),
            TowerRefinementComparisonRun.VerifyAsync, token);
    }

    internal static async Task<TowerRefinementComparisonQuality> RunCore(TowerRefinementLaunchRequest q, TowerRefinementLaunchAuthorization permit,
        Func<CancellationToken, TowerRefinementLaunchInputs> inspect,
        Func<string, TowerBossDiscoveryDefinition[], int, long, CancellationToken, Task<TowerRefinementComparisonQuality>> execute,
        Func<string, CancellationToken, Task<TowerRefinementComparisonQuality>> verify, CancellationToken token,
        Func<string, int, int>? candidate = null)
    {
        Require(permit is not null, "External authorization required."); Validate(q, permit); token.ThrowIfCancellationRequested();
        using var encoding = TowerSharedExecutable.Activate(q.ArchiveProfile);
        using var lease = TowerCompactBundle.AcquireWriter(q.StudyRoot);
        Require(!File.Exists(P(q, "binding-failure.json")) && !File.Exists(P(q, "launch-started.json")) && !Path.Exists(P(q, "run")), "No failed/started launch retry.");
        TowerBulkCampaign.VerifyFiles(q.StudyRoot, BindingFiles, true, token);
        var binding = HarnessJson.Read<TowerRefinementBinding>(P(q, "binding.json"));
        Require(binding.RequestHash == HarnessJson.Hash(q) && binding.AuthorizationHash == HarnessJson.Hash(permit)
            && binding.ExecutionHash == q.ExecutionHash && binding.ChargedBindingSeconds == q.MaximumBindingSeconds, "Changed bound authorization/accounting.");
        var clock = Stopwatch.StartNew(); using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        stop.CancelAfter(TimeSpan.FromSeconds(q.MaximumSeconds - binding.ChargedBindingSeconds)); var ct = stop.Token;
        Write(q, "launch-started.json", new { requestHash = HarnessJson.Hash(q), chargedBindingSeconds = binding.ChargedBindingSeconds });
        try {
            var inputs = inspect(ct);
            BoundPreflight(q, inputs);
            Require(HarnessJson.Hash(inputs.Preflight) == binding.PreflightHash && HarnessJson.Hash(inputs.History.Files) == HarnessJson.Hash(binding.HistoryFiles), "Launch inputs/history changed since binding.");
            var seeds = HarnessJson.Read<TowerRefinementComparisonSeeds>(P(q, "seeds.json"));
            Require(HarnessJson.Hash(seeds) == binding.SeedsHash && seeds.Historical.SequenceEqual(inputs.History.Values), "Changed reserved history/schedules.");
            TowerRefinementReservation.Verify(q.StudyRoot, seeds, q.Master, ct, candidate);
            var definitions = TowerRefinementComparisonModel.Policies.Select(p => TowerRefinementComparisonModel.Definition(inputs.Source, seeds, p, q.Preflight.ComparisonVersion)).ToArray();
            Require(HarnessJson.Hash(definitions) == HarnessJson.Hash(HarnessJson.Read<TowerBossDiscoveryDefinition[]>(P(q, "definitions.json"))), "Changed bound definitions.");
            var seconds = (int)Math.Floor(q.MaximumSeconds - binding.ChargedBindingSeconds - clock.Elapsed.TotalSeconds);
            var bytes = q.MaximumBytes - TowerBulkCampaign.StorageBytes(q.StudyRoot, ct) - ReserveBytes;
            Require(seconds > 0 && bytes >= 4 * 1048576, "No remaining execution envelope.");
            Directory.CreateDirectory(P(q, "run"));
            var result = await execute(P(q, "run"), definitions, seconds, bytes, ct);
            var verified = await verify(P(q, "run"), ct);
            Require(HarnessJson.Hash(result) == HarnessJson.Hash(verified), "Returned result differs from verified archives.");
            var started = HarnessJson.Read<JsonElement>(P(q, "run/started.json"));
            Require(started.GetProperty("version").GetString() == TowerRefinementComparisonModel.ResolveVersion(q.Preflight.ComparisonVersion),
                "Comparison version differs from its authorization.");
            Require((started.TryGetProperty("archiveProfile", out var profile) ? profile.GetString() : null) == q.ArchiveProfile,
                "Comparison archive profile differs from its authorization.");
            ct.ThrowIfCancellationRequested();
            Write(q, "launch-result.json", new { status = "Complete", qualityHash = HarnessJson.Hash(verified),
                chargedBindingSeconds = binding.ChargedBindingSeconds, launchSeconds = clock.Elapsed.TotalSeconds });
            var files = TowerBulkCampaign.Paths(q.StudyRoot).ToDictionary(p => Path.GetRelativePath(q.StudyRoot, p).Replace('\\', '/'), HarnessJson.FileHash);
            Write(q, "launch-files.json", files);
            TowerBulkCampaign.VerifyFiles(q.StudyRoot, "launch-files.json", true, ct); ct.ThrowIfCancellationRequested();
            return verified;
        } catch (Exception e) { Failure(q, "launch-failure.json", e); throw; }
    }

    private static void Write<T>(TowerRefinementLaunchRequest q, string name, T value, bool failure = false)
    {
        var length = JsonSerializer.SerializeToUtf8Bytes(value, HarnessJson.Options).LongLength;
        Require(length <= q.MaximumBytes - TowerBulkCampaign.StorageBytes(q.StudyRoot, default) - (failure ? 0 : 32768), "Launch output cap.");
        TowerCompleteFamilyRun.Durable(P(q, name), value);
    }
    private static void Failure(TowerRefinementLaunchRequest q, string name, Exception error)
    {
        try { Write(q, name, new { error = error.ToString()[..Math.Min(4000, error.ToString().Length)], noRetry = true }, true); }
        catch (Exception e) when (e is IOException or InvalidDataException) { } // Durable start/Pending already forbid reuse.
    }
}
