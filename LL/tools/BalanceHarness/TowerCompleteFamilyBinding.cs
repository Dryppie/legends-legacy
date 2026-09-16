using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerCompleteBindingRequest(string Action, string StudyRoot, string PreservationRoot,
    TowerCompleteSetupRequest Setup, string HarnessHash, string ExecutionHash, double PriorSetupSeconds,
    int MaximumBindingSeconds, long MaximumBindingBytes, IReadOnlyDictionary<string, string> SetupFiles);

/// <summary>External reservation and protocol publication. Never invokes combat; Check never derives a seed.</summary>
public static class TowerCompleteFamilyBinding
{
    public const string PreservationSeal = "0ce4820d50c8eaa670fda1b570c36957a69870df153bedad7b31016561afdd42";
    // Bind the exact bytes that were parsed, without requiring the request to contain its own hash.
    // The external original and the in-study copy are both charged to their respective inventories.
    internal static TowerCompleteBindingRequest ReadRequest(string requestPath)
    {
        var path = Path.GetFullPath(requestPath);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked binding request.");
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > 67108864) throw new InvalidDataException("Binding request exceeds the setup envelope.");
        var q = JsonSerializer.Deserialize<TowerCompleteBindingRequest>(stream, HarnessJson.Options)
            ?? throw new InvalidDataException("Empty binding request.");
        if (q.SetupFiles is not { Count: > 0 } || q.SetupFiles.Keys.Any(n => string.Equals(Path.GetFullPath(n), path, StringComparison.OrdinalIgnoreCase))
            || path.StartsWith(Path.GetFullPath(q.StudyRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Request must be an external input without a self-reference.");
        stream.Position = 0; var hash = Convert.ToHexStringLower(SHA256.HashData(stream));
        var files = q.SetupFiles.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        files.Add(path, hash);
        return q with { SetupFiles = files };
    }

    internal static void Validate(TowerCompleteBindingRequest q, string action)
    {
        if (q.Action != action || q.HarnessHash != TowerCompleteFamilyInputs.HarnessHash
            || q.ExecutionHash != HarnessJson.Hash(ExecutionIdentity.Current())
            || !double.IsFinite(q.PriorSetupSeconds) || q.PriorSetupSeconds < 0
            || q.MaximumBindingSeconds is < 1 or > 600 || q.PriorSetupSeconds + q.MaximumBindingSeconds >= TowerCompleteFamily.MaximumSeconds
            || q.MaximumBindingBytes is < 1048576 or > 67108864 || q.SetupFiles is not { Count: > 0 })
            throw new InvalidDataException("Changed binding action, executable or bounded setup envelope.");
        TowerCompleteFamilySetup.ValidateAllocator(q.Setup.Allocator);
        var study = Path.GetFullPath(q.StudyRoot); var source = Path.GetFullPath(q.Setup.SourceRoot);
        if (!Path.IsPathFullyQualified(q.StudyRoot) || Directory.GetParent(study)?.FullName != Directory.GetParent(source)?.FullName || study == source)
            throw new InvalidDataException("Use a new direct-child study beside the sealed source.");
    }

    internal static long SetupBytes(string root, IReadOnlyDictionary<string, string> files, CancellationToken ct)
    {
        if (files.Count is < 1 or > 100000 || files.Keys.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Count)
            throw new InvalidDataException("Duplicate or excessive setup inventory.");
        long bytes = 0; var inside = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        foreach (var (path, hash) in files)
        {
            ct.ThrowIfCancellationRequested();
            if (!Path.IsPathFullyQualified(path) || Path.GetFullPath(path).StartsWith(inside, StringComparison.OrdinalIgnoreCase)
                || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 || !TowerContractJson.Hash(hash) || HarnessJson.FileHash(path) != hash)
                throw new InvalidDataException("Changed, linked, in-study or invalid setup input.");
            bytes = checked(bytes + new FileInfo(path).Length);
        }
        if (bytes >= TowerCompleteFamily.MaximumBytes - 67108864) throw new InvalidDataException("No remaining binding storage capacity.");
        return bytes;
    }

    private static double Prerequisite(TowerCompleteBindingRequest q, CancellationToken ct)
    {
        var root = Path.GetFullPath(q.PreservationRoot);
        if (HarnessJson.FileHash(Path.Combine(root, "evidence-files.json")) != PreservationSeal)
            throw new InvalidDataException("Use the sealed completed preservation closure.");
        var roots = new[] { root, Path.Combine(Directory.GetParent(root)!.FullName, "tower-complete-family-setup-20260914") };
        var seals = HarnessJson.Read<JsonElement>(Path.Combine(root, "scope.json")).GetProperty("seals");
        foreach (var package in roots)
        {
            ct.ThrowIfCancellationRequested(); var manifest = Path.Combine(package, "evidence-files.json");
            var expected = package == root ? PreservationSeal : seals.GetProperty(Path.GetFileName(package)).GetString();
            if (HarnessJson.FileHash(manifest) != expected) throw new InvalidDataException("Changed prior setup seal.");
            var required = HarnessJson.Read<Dictionary<string, string>>(manifest)
                .ToDictionary(p => Path.GetFullPath(Path.Combine(package, p.Key)), p => p.Value, StringComparer.OrdinalIgnoreCase);
            required.Add(manifest, expected!);
            foreach (var (name, hash) in required)
                if (!q.SetupFiles.TryGetValue(name, out var bound) || bound != hash) throw new InvalidDataException("Omitted prior setup artifact.");
        }
        var previous = HarnessJson.Read<JsonElement>(Path.Combine(root, "final-verification.json"));
        var seconds = previous.GetProperty("combinedSetupTimeBasisSeconds").GetDouble();
        if (previous.GetProperty("status").GetString() != "SetupPreservationVerified" || !double.IsFinite(seconds)
            || q.PriorSetupSeconds < seconds) throw new InvalidDataException("Lost prior setup time charge.");
        return seconds;
    }

    private static async Task<(TowerCompleteSource Source, TowerCompleteHistory History, long Bytes)> Preflight(TowerCompleteBindingRequest q, CancellationToken ct)
    {
        long bytes;
        using (TowerPerformanceTrace.Measure("binding.setup-inputs"))
        { _ = Prerequisite(q, ct); bytes = SetupBytes(q.StudyRoot, q.SetupFiles, ct); }
        var source = await TowerCompleteFamilyInputs.Source(q.Setup.SourceRoot, ct);
        TowerCompleteHistory history;
        using (TowerPerformanceTrace.Measure("binding.history-inputs"))
            history = TowerCompleteFamilySetup.History(Directory.GetParent(source.Root)!.FullName, q.StudyRoot,
                source.RequiredHistory, q.Setup.HistoryFiles, q.Setup.AuthoritativeLedger, ct);
        if (history.Values.Length < 481603 || history.Values.Length > TowerStudyLimits.HistoricalSeeds - 331)
            throw new InvalidDataException("Historical reservation capacity exhausted.");
        return (source, history, bytes);
    }

    public static async Task<object> Check(string requestPath, string output, CancellationToken token = default)
    {
        var q = ReadRequest(requestPath); Validate(q, "CheckOnly");
        if (File.Exists(output) || Directory.Exists(q.StudyRoot)) throw new InvalidDataException("No overwrite or reuse of a study root.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(q.MaximumBindingSeconds));
        var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Binding checks cannot fight.")); using var guard = trace.Activate();
        var clock = Stopwatch.StartNew(); var validated = await Preflight(q, deadline.Token);
        var result = new { status = "BindingInputsVerifiedNoAllocation", requestHash = q.SetupFiles[Path.GetFullPath(requestPath)],
            harnessHash = q.HarnessHash, execution = ExecutionIdentity.Current(), sourceSeal = TowerCompleteFamily.SourceSeal,
            orderedCellsHash = HarnessJson.Hash(validated.Source.Cells), capacity = TowerCompleteFamily.Capacity(validated.Source.Cells, deadline.Token),
            reservations = validated.History.Values.Length, historyHash = HarnessJson.Hash(validated.History.Values),
            setupFiles = q.SetupFiles.Count, priorSetupBytes = validated.Bytes, q.PriorSetupSeconds, seconds = clock.Elapsed.TotalSeconds,
            preparations = 0, fights = 0, newSeeds = 0, executionAuthorized = false, timings = trace.Snapshot() };
        deadline.Token.ThrowIfCancellationRequested(); HarnessJson.WriteNew(output, result); return result;
    }

    internal static TowerCompleteProtocol Publish(string root, TowerCompleteBindingRequest q, long externalBytes,
        TowerCompleteSeeds seeds, double chargedSetupSeconds, CancellationToken ct)
    {
        if (!double.IsFinite(chargedSetupSeconds) || chargedSetupSeconds < q.PriorSetupSeconds
            || chargedSetupSeconds >= TowerCompleteFamily.MaximumSeconds) throw new InvalidDataException("Lost or exhausted setup time.");
        TowerCompleteFamilyInputs.Seeds(seeds, seeds.Historical.ToArray());
        TowerCompleteReservation.Verify(root, seeds, q.Setup.Allocator, ct);
        if (File.Exists(Path.Combine(root, "binding-failure.json"))) throw new InvalidDataException("Failed binding cannot publish.");
        var storage = new TowerCompleteReservation.Storage(root, q.MaximumBindingBytes);
        storage.Put("setup-charge.json", new { seconds = chargedSetupSeconds, bytes = externalBytes });
        var files = Directory.EnumerateFiles(root).ToDictionary(p => Path.GetFileName(p), HarnessJson.FileHash, StringComparer.Ordinal);
        if (files.Keys.Any(n => n.EndsWith(".pending", StringComparison.Ordinal)) || !files.ContainsKey("history-input.json"))
            throw new InvalidDataException("Incomplete binding publication.");
        var protocol = new TowerCompleteProtocol(TowerCompleteFamily.Version, q.Setup.SourceRoot, TowerCompleteFamily.SourceSeal,
            q.HarnessHash, q.ExecutionHash, TowerCompleteFamily.MaximumFights, TowerCompleteFamily.MaximumSeconds,
            TowerCompleteFamily.MaximumBytes, 0, TowerStorageAccountant.Mode, chargedSetupSeconds, externalBytes,
            q.SetupFiles, q.Setup.HistoryFiles, files);
        TowerCompleteFamilyInputs.Limits(protocol); ct.ThrowIfCancellationRequested();
        storage.Put("protocol.json", protocol); return protocol; // Published last; a failed or partial binding cannot launch.
    }

    public static async Task<object> ReserveAndBind(string requestPath, CancellationToken token = default)
    {
        var q = ReadRequest(requestPath); Validate(q, "ReserveAndBind");
        var root = Path.GetFullPath(q.StudyRoot); var registry = Directory.GetParent(root)!.FullName;
        using var registryLease = TowerCompactBundle.AcquireWriter(Path.Combine(registry, "complete-family-allocation"));
        using var lease = TowerCompactBundle.AcquireWriter(root);
        if (Directory.Exists(root)) throw new InvalidDataException("No reservation retry, resume or overwrite.");
        Directory.CreateDirectory(root); var clock = Stopwatch.StartNew();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(q.MaximumBindingSeconds)); var ct = deadline.Token;
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Binding cannot fight.")).Activate();
        var storage = new TowerCompleteReservation.Storage(root, q.MaximumBindingBytes);
        storage.Put("binding-started.json", new { requestHash = q.SetupFiles[Path.GetFullPath(requestPath)], utc = DateTimeOffset.UtcNow });
        storage.Put("binding-request.json", q);
        try
        {
            var validated = await Preflight(q, ct);
            var reserved = TowerCompleteReservation.Reserve(root, q.Setup.Allocator, validated.History.Values, q.MaximumBindingBytes, ct);
            // Detect another writer before publication; all accepted reservations stay registered even if this fails.
            if (!TowerCompleteFamilyInputs.HistoryRegistry(registry, root, ct).SetEquals(q.Setup.HistoryFiles.Keys))
                throw new InvalidDataException("Registered history changed during reservation; retain all accepted values.");
            foreach (var (path, hash) in q.Setup.HistoryFiles)
                if (HarnessJson.FileHash(path) != hash) throw new InvalidDataException("History changed during reservation.");
            // Conservatively charge the whole binding allowance, including final publication, to the study deadline.
            var protocol = Publish(root, q, validated.Bytes, reserved.Seeds, q.PriorSetupSeconds + q.MaximumBindingSeconds, ct);
            ct.ThrowIfCancellationRequested(); return new { status = "ReservedAndBound", reservations = reserved.Seeds.Historical.Count + 288,
                reserved.Candidates, reserved.Rejections, protocolHash = HarnessJson.FileHash(Path.Combine(root, "protocol.json")), seconds = clock.Elapsed.TotalSeconds, fights = 0 };
        }
        catch (Exception e)
        {
            var error = e.ToString();
            TowerCompleteFamilyRun.Durable(Path.Combine(root, "binding-failure.json"), new { error = error[..Math.Min(error.Length, 4000)], seconds = clock.Elapsed.TotalSeconds, noRetry = true });
            throw;
        }
    }
}
