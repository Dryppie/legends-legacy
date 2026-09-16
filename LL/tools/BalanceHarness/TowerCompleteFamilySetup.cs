using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerCompleteAllocatorPlan(string Algorithm, string Domain, int Master,
    int FirstCount, int SecondCount, int MaximumCandidatesPerStage);
public sealed record TowerCompleteSetupRequest(string SourceRoot, TowerCompleteAllocatorPlan Allocator,
    string AuthoritativeLedger, IReadOnlyDictionary<string, string> HistoryFiles);
internal sealed record TowerCompleteHistory(int Files, int DistinctHashes, long Bytes, int[] Values);

/// <summary>Seed-free external setup inspection. This command never allocates or launches a study.</summary>
public static class TowerCompleteFamilySetup
{
    public static TowerCompleteAllocatorPlan Allocator { get; } = new("sha256-us-int32le-reject-v1",
        TowerCompleteFamily.Version, 2026091423, 32, 256, 100000);

    internal static void ValidateAllocator(TowerCompleteAllocatorPlan plan)
    {
        if (plan != Allocator) throw new InvalidDataException("Changed complete-family allocator plan.");
    }

    // Pure external allocation primitive, exercised only with a separate fixture domain in this scope.
    // The setup inspector does not call it. A future reservation must be durably recorded externally.
    internal static int Candidate(string domain, int master, string label, int ordinal)
    {
        var text = string.Join('\u001f', domain, master.ToString(CultureInfo.InvariantCulture),
            label, ordinal.ToString(CultureInfo.InvariantCulture));
        return BinaryPrimitives.ReadInt32LittleEndian(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    internal static int[] Draw(string domain, int master, string label, int count, int maximumCandidates,
        HashSet<int> used, CancellationToken ct, Func<int, int>? candidate = null)
    {
        if (count < 1 || maximumCandidates < count || maximumCandidates > 100000)
            throw new InvalidDataException("Invalid bounded allocation arithmetic.");
        var accepted = new List<int>(count);
        for (var ordinal = 0; ordinal < maximumCandidates && accepted.Count < count; ordinal++)
        {
            ct.ThrowIfCancellationRequested();
            var value = candidate is null ? Candidate(domain, master, label, ordinal) : candidate(ordinal);
            if (used.Add(value)) accepted.Add(value);
        }
        if (accepted.Count != count) throw new InvalidDataException("Allocator candidate limit reached; retain attempt evidence.");
        return accepted.ToArray();
    }

    internal static TowerCompleteHistory History(string registryRoot, string excludedStudy,
        IReadOnlyDictionary<string, string> required, IReadOnlyDictionary<string, string> files,
        string authoritativeLedger, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (files is not { Count: > 0 } || files.Count > 10000
            || files.Keys.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Count
            || !TowerCompleteFamilyInputs.HistoryRegistry(registryRoot, excludedStudy, ct).SetEquals(files.Keys))
            throw new InvalidDataException("Missing, aliased or newly registered history input.");
        foreach (var (name, hash) in required)
            if (!files.TryGetValue(name, out var bound) || bound != hash)
                throw new InvalidDataException("Lost required historical reservation source.");
        if (!files.ContainsKey(authoritativeLedger)) throw new InvalidDataException("Authoritative ledger is not bound.");
        var seen = new HashSet<string>(StringComparer.Ordinal); var values = new HashSet<int>(); long bytes = 0;
        foreach (var (name, hash) in files)
        {
            ct.ThrowIfCancellationRequested();
            if (!Path.IsPathFullyQualified(name) || !TowerContractJson.Hash(hash) || HarnessJson.FileHash(name) != hash)
                throw new InvalidDataException("Changed or invalid history binding.");
            bytes = checked(bytes + new FileInfo(name).Length);
            if (seen.Add(hash)) values.UnionWith(TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(name)));
        }
        var ordered = values.Order().ToArray();
        if (!ordered.SequenceEqual(TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(authoritativeLedger))))
            throw new InvalidDataException("Authoritative ledger differs from complete registered history.");
        return new(files.Count, seen.Count, bytes, ordered);
    }

    public static async Task<object> Inspect(string requestPath, string output, CancellationToken ct = default)
    {
        if (File.Exists(output)) throw new IOException("Setup output already exists; no overwrite or retry.");
        var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Seed-free setup cannot fight."));
        using var guard = trace.Activate();
        var q = HarnessJson.Read<TowerCompleteSetupRequest>(requestPath); ValidateAllocator(q.Allocator);
        var sourceRoot = Path.GetFullPath(q.SourceRoot);
        var outputRoot = Path.GetDirectoryName(Path.GetFullPath(output))!;
        if (Directory.GetParent(outputRoot)?.FullName != Directory.GetParent(sourceRoot)?.FullName || outputRoot == sourceRoot)
            throw new InvalidDataException("Use a separate direct-child setup package beside the sealed source.");
        var source = await TowerCompleteFamilyInputs.Source(q.SourceRoot, ct);
        var history = History(Directory.GetParent(source.Root)!.FullName, Path.GetDirectoryName(Path.GetFullPath(output))!,
            source.RequiredHistory, q.HistoryFiles, q.AuthoritativeLedger, ct);
        if (history.Values.Length < 481603 || history.Values.Length > TowerStudyLimits.HistoricalSeeds - 288)
            throw new InvalidDataException("Reservation history outside complete-family capacity.");
        var result = new { status = "ExternalSetupVerifiedNoSeedsNoCombat", version = TowerCompleteFamily.Version,
            requestHash = HarnessJson.FileHash(requestPath), sourceSeal = TowerCompleteFamily.SourceSeal,
            sourceCellsHash = TowerCompleteFamily.SourceCells, orderedCellsHash = HarnessJson.Hash(source.Cells),
            capacity = TowerCompleteFamily.Capacity(source.Cells, ct), allocator = q.Allocator,
            encoding = "UTF-8(domain + U+001F + invariant master + U+001F + label + U+001F + zero-based ordinal); SHA-256 first four bytes signed Int32 little-endian",
            reservationOrder = "first then second; shared exclusion set; reject historical and already accepted values; persist all 288 before any fight",
            historyFiles = history.Files, historyDistinctHashes = history.DistinctHashes,
            historyInputBytes = history.Bytes, reservations = history.Values.Length, historyHash = HarnessJson.Hash(history.Values),
            execution = ExecutionIdentity.Current(), harnessHash = TowerCompleteFamilyInputs.HarnessHash,
            preparations = 0, fights = 0, newSeeds = 0, executionAuthorized = false, timings = trace.Snapshot() };
        ct.ThrowIfCancellationRequested(); HarnessJson.WriteNew(output, result); return result;
    }
}
