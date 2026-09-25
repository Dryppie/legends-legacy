using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerPracticalRecoveryRequest(string Version, string StudyRoot, string ManifestPath,
    string ManifestHash, string ReceiptPath, int MaximumSeconds);
public sealed record TowerPracticalRecoveryRecord(string Version, string Status, TowerPracticalRecoveryRequest Request,
    string SourceRequestHash, int[] Historical, int[] Reserved, int StartedAttempts, int CompletedAttempts,
    double PriorSeconds, long PriorBytes, int ForfeitedMaximumSeconds, long ForfeitedMaximumBytes, long RetainedSourceBytes);

/// <summary>Close out a stopped pre-combat reservation as permanent exclusions. Never resume or refund it.</summary>
public static partial class TowerPracticalReservationRecovery
{
    public const string Version = "tower-practical-abandoned-reservation-v1";
    public const string AllocationVersion = "tower-practical-abandoned-allocation-v1";
    internal const string WorkerVersion = "tower-practical-worker-registration-v1";
    private static void Require(bool value, string message) => TowerPracticalSearch.Require(value, message);
    private static bool Same(string a, string b) => string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
    private static bool Inside(string path, string root) => Same(path, root)
        || Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static void Unlinked(string path)
    {
        for (var p = Path.GetFullPath(path); p is not null; p = Path.GetDirectoryName(p))
            Require((File.GetAttributes(p) & FileAttributes.ReparsePoint) == 0, "Linked recovery input or ancestor.");
    }

    private static void Validate(TowerPracticalRecoveryRequest q)
    {
        Require(q.Version is Version or AllocationVersion && q.MaximumSeconds is >= 1 and <= 600 && TowerContractJson.Hash(q.ManifestHash)
            && new[] { q.StudyRoot, q.ManifestPath, q.ReceiptPath }.All(Path.IsPathFullyQualified), "Invalid practical recovery request.");
        Require(q.StudyRoot == Path.TrimEndingDirectorySeparator(Path.GetFullPath(q.StudyRoot)), "Use a normalized study directory without a trailing separator.");
        Require(!Inside(q.ManifestPath, q.StudyRoot) && !Inside(q.ReceiptPath, q.StudyRoot)
            && !Same(q.ManifestPath, q.ReceiptPath)
            && new[] { q.ManifestPath, q.ReceiptPath }.All(p => Path.GetFileName(p) is not ("history-input.json" or "seed-ledger.json" or "prior-seed-ledger.json")),
            "Recovery manifest and receipt must be separate external files, not historical ledgers.");
        Unlinked(q.StudyRoot); Unlinked(q.ManifestPath); Unlinked(Path.GetDirectoryName(q.ReceiptPath)!);
    }

    private static void RequireExited(int id, long startedTicks)
    {
        Require(id > 0 && startedTicks > 0 && startedTicks <= DateTimeOffset.UtcNow.UtcTicks, "Missing or invalid recorded process ownership.");
        try
        {
            using var process = Process.GetProcessById(id);
            Require(process.HasExited || process.StartTime.ToUniversalTime().Ticks != startedTicks,
                "Recorded practical launcher or worker is still active.");
        }
        catch (ArgumentException) { } // No process with this ID; PID reuse is checked by start time above.
        // Access/identity failures propagate. They are not evidence of process exit.
    }

    private static TowerPracticalRecoveryRecord Audit(TowerPracticalRecoveryRequest q, CancellationToken ct, Func<string, int, int>? candidate)
    {
        ct.ThrowIfCancellationRequested(); Validate(q);
        string P(string name) => Path.Combine(q.StudyRoot, name);
        Dictionary<string, string> Inventory()
        {
            ct.ThrowIfCancellationRequested();
            Require(HarnessJson.FileHash(q.ManifestPath) == q.ManifestHash, "Changed practical recovery manifest.");
            var manifest = TowerContractJson.Read<Dictionary<string, string>>(q.ManifestPath);
            var actual = Directory.EnumerateFileSystemEntries(q.StudyRoot).ToArray();
            Require(actual.All(p => (File.GetAttributes(p) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0),
                "Practical Pending recovery permits only pre-combat registration files.");
            Require(actual.Select(Path.GetFileName).Order(StringComparer.Ordinal).SequenceEqual(manifest.Keys.Order(StringComparer.Ordinal)),
                "Practical recovery requires the exact pinned failed-source inventory.");
            string[] required = ["request.json", "launch.json", "worker-start.json", "source-definition.json", "history-files.json", "history-input.json",
                q.Version == AllocationVersion ? "allocation-intent.json" : "definition.json"];
            string[] optional = ["seed-ledger.json", "failure.json", "worker-failure.json", "seed-ledger.json.pending", "history-input.json.pending"];
            if (q.Version == AllocationVersion)
                optional = [.. optional, "allocation-journal.jsonl", "definition.json", "definition.json.pending", "allocation.json", "allocation.json.pending"];
            Require(required.All(manifest.ContainsKey) && manifest.Keys.All(n => required.Contains(n) || optional.Contains(n)),
                "Missing registration evidence or evidence of later execution; recovery cannot infer zero attempts.");
            foreach (var (name, hash) in manifest)
            {
                ct.ThrowIfCancellationRequested();
                Require(TowerContractJson.Hash(hash) && HarnessJson.FileHash(P(name)) == hash, "Changed practical recovery source artifact.");
            }
            return manifest;
        }
        var files = Inventory();
        var source = TowerContractJson.Read<TowerPracticalRequest>(P("request.json"));
        Require(q.Version == Version ? TowerPracticalSearch.IsDeclaredVersion(source.Version) && source.Allocation is null
            : TowerPracticalSearch.IsAllocatedVersion(source.Version) && source.Allocation is not null,
            "Practical recovery version does not match the source reservation contract.");
        TowerPracticalSearch.ValidateRequestContract(source);
        Require(Same(source.OutputRoot, q.StudyRoot) && Same(source.RegistryRoot, Path.GetDirectoryName(q.StudyRoot)!), "Recovery belongs to another study.");
        Unlinked(source.RegistryRoot);
        var launch = TowerContractJson.Read<TowerPracticalLaunch>(P("launch.json"));
        var worker = TowerContractJson.Read<TowerPracticalWorkerStart>(P("worker-start.json"));
        Require(launch.RequestHash == HarnessJson.Hash(source) && worker.RequestHash == launch.RequestHash
            && worker.Version == WorkerVersion && launch.Deadline == launch.StartedAt.AddSeconds(source.MaximumSeconds - source.PriorSeconds)
            && string.Equals(worker.MachineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase)
            && worker.StartedAt >= launch.StartedAt && worker.StartedAt < launch.Deadline
            && launch.ParentStartedUtcTicks <= launch.StartedAt.UtcTicks && worker.ProcessStartedUtcTicks <= worker.StartedAt.UtcTicks
            && worker.ProcessId != launch.ParentProcessId, "Changed practical request, registration version or launch binding.");
        RequireExited(launch.ParentProcessId, launch.ParentStartedUtcTicks);
        RequireExited(worker.ProcessId, worker.ProcessStartedUtcTicks);
        Require(HarnessJson.FileHash(P("source-definition.json")) == source.DefinitionHash, "Changed frozen practical source definition.");
        void Partial(string name, object expected)
        {
            if (files.ContainsKey(name + ".pending"))
            {
                // An atomic write may be truncated. Only a byte prefix of the exact
                // expected write is recoverable, never arbitrary extra values.
                var partial = File.ReadAllBytes(P(name + ".pending"));
                var expectedBytes = JsonSerializer.SerializeToUtf8Bytes(expected, HarnessJson.Options);
                Require(partial.Length <= expectedBytes.Length && partial.AsSpan().SequenceEqual(expectedBytes.AsSpan(0, partial.Length)),
                    "Unrecognized interrupted registration write: " + name);
            }
        }
        void Matches(string name, object expected)
        {
            if (files.ContainsKey(name))
                Require(HarnessJson.Hash(HarnessJson.Read<JsonElement>(P(name))) == HarnessJson.Hash(expected), "Changed registration artifact: " + name);
        }
        int[] historical; int[] reserved;
        if (q.Version == AllocationVersion)
            (historical, reserved) = AuditAllocationRegistration(q, source, files, Matches, Partial, ct, candidate);
        else
        {
            var definition = TowerPracticalSearch.Prepare(TowerBossDiscovery.Read(P("source-definition.json")));
            TowerPracticalSearch.ValidateRequestDefinition(source, definition);
            historical = definition.ExcludedCombatSeeds.Order().ToArray();
            reserved = TowerPracticalSearch.Reserved(definition);
            Matches("history-input.json", new { reservationState = "Pending", reserved });
            Partial("history-input.json", new { reservationState = "Complete", reserved });
            Matches("definition.json", definition);
            Matches("seed-ledger.json", new { reservationState = "Complete", historical, reserved });
            Partial("seed-ledger.json", new { reservationState = "Complete", historical, reserved });
            Require(!(files.ContainsKey("seed-ledger.json") && files.ContainsKey("seed-ledger.json.pending"))
                && (!files.ContainsKey("history-input.json.pending") || files.ContainsKey("seed-ledger.json")), "Registration artifacts are out of order.");
        }
        var history = TowerContractJson.Read<Dictionary<string, string>>(P("history-files.json"));
        Require(source.RequiredHistory.All(p => history.TryGetValue(p.Key, out var h) && h == p.Value)
            && history.All(p => Path.IsPathFullyQualified(p.Key) && Inside(p.Key, source.RegistryRoot)
                && !Inside(p.Key, q.StudyRoot) && TowerContractJson.Hash(p.Value)), "Changed registration history pins.");
        if (files.ContainsKey("failure.json"))
        {
            var failure = TowerContractJson.Read<TowerPracticalResult>(P("failure.json"));
            Require(HarnessJson.Hash(failure) == HarnessJson.Hash(TowerPracticalSearch.Unverified(failure.ExecutionStatus, "Failed", failure.StopReason))
                && failure.ExecutionStatus is "Invalid" or "Cancelled", "Changed practical failure evidence.");
        }
        if (files.ContainsKey("worker-failure.json"))
        {
            var failure = HarnessJson.Read<JsonElement>(P("worker-failure.json"));
            Require(failure.GetProperty("status").GetString() is "Invalid" or "Cancelled"
                && failure.GetProperty("retries").GetInt32() == 0 && failure.GetProperty("error").ValueKind == JsonValueKind.String,
                "Changed worker failure evidence.");
        }
        var bytes = files.Keys.Sum(n => new FileInfo(P(n)).Length);
        Inventory(); ct.ThrowIfCancellationRequested();
        return new(q.Version, "AbandonedPermanentlyReserved", q, launch.RequestHash, historical, reserved, 0, 0,
            source.PriorSeconds, source.PriorBytes, source.MaximumSeconds, source.MaximumBytes, bytes);
    }

    public static TowerPracticalRecoveryRecord Recover(TowerPracticalRecoveryRequest request, CancellationToken token = default)
        => RecoverCore(request, token);

    // Literal candidate injection is internal to synthetic fixtures; the public
    // commands always reconstruct recorded results with the production function.
    internal static TowerPracticalRecoveryRecord RecoverCore(TowerPracticalRecoveryRequest request,
        CancellationToken token = default, Func<string, int, int>? candidate = null)
    {
        token.ThrowIfCancellationRequested(); Validate(request);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token); stop.CancelAfter(TimeSpan.FromSeconds(request.MaximumSeconds));
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Recovery cannot fight.")).Activate();
        using var registry = TowerCompactBundle.AcquireWriter(Path.Combine(Path.GetDirectoryName(request.StudyRoot)!, "complete-family-allocation"));
        using var source = TowerCompactBundle.AcquireWriter(request.StudyRoot);
        using var receipt = TowerCompactBundle.AcquireWriter(request.ReceiptPath);
        Require(!Path.Exists(request.ReceiptPath) && !Path.Exists(request.ReceiptPath + ".pending"), "Recovery receipt already exists or publication was interrupted; no overwrite.");
        var record = Audit(request, stop.Token, candidate);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(record, HarnessJson.Options);
        Require(bytes.Length <= 16 * 1048576, "Practical recovery receipt exceeds its 16 MiB bound.");
        stop.Token.ThrowIfCancellationRequested();
        using (var output = new FileStream(request.ReceiptPath + ".pending", FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough))
        { output.Write(bytes); output.Flush(true); }
        stop.Token.ThrowIfCancellationRequested();
        File.Move(request.ReceiptPath + ".pending", request.ReceiptPath);
        return record;
    }

    public static TowerPracticalRecoveryRecord Verify(string receiptPath, CancellationToken token = default)
        => VerifyCore(receiptPath, token);

    internal static TowerPracticalRecoveryRecord VerifyCore(string receiptPath,
        CancellationToken token = default, Func<string, int, int>? candidate = null)
    {
        token.ThrowIfCancellationRequested(); Unlinked(receiptPath);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Recovery verification cannot fight.")).Activate();
        var hash = HarnessJson.FileHash(receiptPath);
        var saved = TowerContractJson.Read<TowerPracticalRecoveryRecord>(receiptPath);
        Validate(saved.Request);
        Require(Same(saved.Request.ReceiptPath, receiptPath) && !Path.Exists(receiptPath + ".pending"), "Moved or interrupted recovery receipt.");
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token); stop.CancelAfter(TimeSpan.FromSeconds(saved.Request.MaximumSeconds));
        using var source = TowerCompactBundle.AcquireWriter(saved.Request.StudyRoot);
        var actual = Audit(saved.Request, stop.Token, candidate);
        Require(HarnessJson.Hash(actual) == HarnessJson.Hash(saved) && HarnessJson.FileHash(receiptPath) == hash, "Changed practical recovery receipt.");
        return actual;
    }

    internal static int[] ReadPending(string pendingPath, string receiptPath, CancellationToken ct, Func<string, int, int>? candidate = null)
    {
        var record = VerifyCore(receiptPath, ct, candidate);
        Require(Same(pendingPath, Path.Combine(record.Request.StudyRoot, "history-input.json")), "Recovery receipt belongs to another Pending file.");
        return record.Historical.Concat(record.Reserved).Distinct().Order().ToArray();
    }
}
