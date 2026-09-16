using System.IO.Compression;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerCompleteSeeds(string Version, IReadOnlyList<int> Historical,
    IReadOnlyList<int> First, IReadOnlyList<int> Second);
public sealed record TowerCompleteProtocol(string Version, string SourceRoot, string SourceSeal, string HarnessHash,
    string ExecutionHash, int MaximumAttempts, int MaximumSeconds, long MaximumBytes, int Retries,
    string StorageAccounting, double PriorSetupSeconds, long PriorSetupBytes,
    IReadOnlyDictionary<string, string> SetupFiles, IReadOnlyDictionary<string, string> HistoryFiles,
    IReadOnlyDictionary<string, string> FrozenFiles);
internal sealed record TowerCompleteSource(string Root, IReadOnlyList<TowerConfirmationCell> Cells,
    IReadOnlyDictionary<string, TowerScenario> Scenarios, TowerBalanceCohort Cohort, string ContentRoot,
    IReadOnlyDictionary<string, string> ContentHashes, string SettingsHash,
    IReadOnlyDictionary<string, string> RequiredHistory);
internal sealed record TowerCompleteIdentity(string HarnessHash, string ExecutionHash);

/// <summary>Read the sealed seed-free source and validate an externally reserved future schedule. Never allocates.</summary>
public static class TowerCompleteFamilyInputs
{
    internal static string HarnessHash => HarnessJson.FileHash(typeof(TowerCompleteFamily).Assembly.Location);
    internal static void Limits(TowerCompleteProtocol p, TowerCompleteIdentity? reservedIdentity = null)
    {
        if (p.Version != TowerCompleteFamily.Version || p.SourceSeal != TowerCompleteFamily.SourceSeal
            || p.HarnessHash != (reservedIdentity?.HarnessHash ?? HarnessHash)
            || p.ExecutionHash != (reservedIdentity?.ExecutionHash ?? HarnessJson.Hash(ExecutionIdentity.Current()))
            || p.MaximumAttempts != TowerCompleteFamily.MaximumFights || p.MaximumSeconds != TowerCompleteFamily.MaximumSeconds
            || p.MaximumBytes != TowerCompleteFamily.MaximumBytes || p.Retries != 0 || p.StorageAccounting != TowerStorageAccountant.Mode
            || !double.IsFinite(p.PriorSetupSeconds) || p.PriorSetupSeconds < 0 || p.PriorSetupSeconds >= p.MaximumSeconds
            || p.PriorSetupBytes < 0 || p.PriorSetupBytes >= p.MaximumBytes - 1048576
            || p.SetupFiles is not { Count: > 0 } || p.HistoryFiles is not { Count: > 0 } || p.FrozenFiles is not { Count: > 0 })
            throw new InvalidDataException("Changed complete-family identity, limits, retry policy or missing setup/history binding.");
    }

    internal static void Seeds(TowerCompleteSeeds s, IReadOnlyCollection<int> required)
    {
        if (s.Version != TowerCompleteFamily.Version || s.First is not { Count: TowerCompleteFamily.FirstSamples }
            || s.Second is not { Count: TowerCompleteFamily.SecondSamples } || s.Historical is null
            || s.Historical.Count < 481603 || s.Historical.Count > TowerStudyLimits.HistoricalSeeds - 288
            || !s.Historical.SequenceEqual(required.Distinct().Order())
            || !s.Historical.SequenceEqual(s.Historical.Distinct().Order())
            || s.First.Concat(s.Second).Distinct().Count() != 288 || s.First.Concat(s.Second).Intersect(s.Historical).Any())
            throw new InvalidDataException("Requires 32 + 256 distinct external reservations outside the complete refreshed history.");
    }

    internal static async Task<TowerCompleteSource> Source(string root, CancellationToken ct)
    {
        using var phase = TowerPerformanceTrace.Measure("complete.source-binding");
        ct.ThrowIfCancellationRequested(); root = Path.GetFullPath(root);
        if (HarnessJson.FileHash(Path.Combine(root, "evidence-files.json")) != TowerCompleteFamily.SourceSeal
            || HarnessJson.FileHash(Path.Combine(root, "cells.json")) != TowerCompleteFamily.SourceCells)
            throw new InvalidDataException("Use the sealed complete UTC family binding.");
        TowerBulkCampaign.VerifyFiles(root, "evidence-files.json", true, ct);
        var q = HarnessJson.Read<JsonElement>(Path.Combine(root, "request.json"));
        foreach (var pair in q.GetProperty("inputs").EnumerateObject())
        { ct.ThrowIfCancellationRequested(); if (HarnessJson.FileHash(pair.Name) != pair.Value.GetString()) throw new InvalidDataException("Changed source input."); }
        var execution = ExecutionIdentity.Current();
        foreach (var pair in q.GetProperty("gameplayHashes").EnumerateObject())
            if (!execution.AssemblyHashes.TryGetValue(pair.Name, out var hash) || hash != pair.Value.GetString())
                throw new InvalidDataException("Use captured-v19 gameplay assemblies.");
        var cells = HarnessJson.Read<TowerConfirmationCell[]>(Path.Combine(root, "cells.json"));
        _ = TowerCompleteFamily.Capacity(cells, ct);
        var audit = q.GetProperty("auditRoot").GetString()!; var midpoint = q.GetProperty("midpointRoot").GetString()!;
        var inventory = Path.Combine(audit, "retained-family.json.gz");
        if (HarnessJson.FileHash(inventory) != TowerRetainedFamilyAudit.InventoryHash) throw new InvalidDataException("Changed retained inventory.");
        var definition = TowerBalanceEvaluator.Read(Path.Combine(midpoint, "definition.json"));
        var cohort = definition.Cohorts.Single(); var target = definition.Cells[0].Scenario;
        var byKey = cells.ToDictionary(c => c.InventoryKey, StringComparer.Ordinal); var scenarios = new Dictionary<string, TowerScenario>(StringComparer.Ordinal);
        using var file = File.OpenRead(inventory); using var zip = new GZipStream(file, CompressionMode.Decompress);
        var count = 0; var origins = 0;
        await foreach (var entry in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(zip, HarnessJson.Options, ct))
        {
            ct.ThrowIfCancellationRequested(); count++; origins += entry.GetProperty("sources").GetArrayLength();
            var scenario = TowerRetainedFamilyAudit.Scenario(entry.GetProperty("scenario"), cohort, target);
            if (!byKey.TryGetValue(entry.GetProperty("key").GetString()!, out var cell)) throw new InvalidDataException("Unbound recipe.");
            var legacy = TowerRetainedFamilyAudit.Identity(scenario); var identity = TowerConfirmationContext.Identity(scenario);
            if (legacy.CellHash != cell.LegacyCellHash || identity.CellHash != cell.CellHash || identity.ContextHash != cell.ContextHash
                || identity.RecipeHash != cell.RecipeHash || !scenarios.TryAdd(cell.CellHash, scenario))
                throw new InvalidDataException("Recipe, original timestamp or complete UTC identity differs.");
        }
        if (count != TowerCompleteFamily.Cells || origins != 47834 || scenarios.Count != cells.Length) throw new InvalidDataException("Incomplete source family.");
        var content = Path.Combine(midpoint, "materialized/content");
        TowerPortfolioConfirmation.Equal(definition.ContentHashes, TowerCompactBundle.ContentHashes(content, ct), "complete-family content");
        if (HarnessJson.Hash(TowerBundle.ReadSettings(content)) != definition.SettingsHash) throw new InvalidDataException("Changed settings.");
        var required = HarnessJson.Read<JsonElement>(Path.Combine(audit, "protocol.json")).GetProperty("historyFiles")
            .EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.OrdinalIgnoreCase);
        return new(root, cells.OrderBy(c => c.CellHash, StringComparer.Ordinal).ToArray(), scenarios, cohort, content, definition.ContentHashes, definition.SettingsHash, required);
    }

    // Check registry membership once at launch, including newly appeared ledgers. Never performed per fight.
    internal static HashSet<string> HistoryRegistry(string root, string excludedStudy, CancellationToken ct)
        => TowerHistoryRegistry.Scan(root, excludedStudy, ct);

    internal static void Frozen(string root, TowerCompleteProtocol p, bool liveHistory, TowerCompleteSource source, CancellationToken ct,
        TowerCompleteIdentity? reservedIdentity = null)
    {
        Limits(p, reservedIdentity);
        if (File.Exists(Path.Combine(root, "binding-failure.json"))) throw new InvalidDataException("Failed binding cannot execute or verify.");
        if (!p.FrozenFiles.ContainsKey("seeds.json") || !p.FrozenFiles.ContainsKey("setup-charge.json")) throw new InvalidDataException("Unbound seeds/setup charge.");
        foreach (var (name, hash) in p.FrozenFiles)
        {
            var path = Path.GetFullPath(Path.Combine(root, name));
            if (Path.IsPathFullyQualified(name) || !path.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || !TowerContractJson.Hash(hash) || HarnessJson.FileHash(path) != hash) throw new InvalidDataException("Changed or escaping frozen file.");
        }
        var setup = HarnessJson.Read<JsonElement>(Path.Combine(root, "setup-charge.json"));
        if (setup.GetProperty("seconds").GetDouble() != p.PriorSetupSeconds || setup.GetProperty("bytes").GetInt64() != p.PriorSetupBytes)
            throw new InvalidDataException("Setup accounting differs.");
        long bytes = 0;
        foreach (var (name, hash) in p.SetupFiles)
        {
            ct.ThrowIfCancellationRequested();
            if (!Path.IsPathFullyQualified(name) || !TowerContractJson.Hash(hash) || HarnessJson.FileHash(name) != hash
                || Path.GetFullPath(name).StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Changed, duplicated in-root or invalid prior setup artifact.");
            bytes = checked(bytes + new FileInfo(name).Length);
        }
        if (bytes != p.PriorSetupBytes) throw new InvalidDataException("Lost external setup bytes.");
        foreach (var (name, hash) in source.RequiredHistory)
            if (!p.HistoryFiles.TryGetValue(name, out var bound) || bound != hash) throw new InvalidDataException("Missing historical reservation source.");
        if (liveHistory && !HistoryRegistry(Directory.GetParent(source.Root)!.FullName, root, ct).SetEquals(p.HistoryFiles.Keys))
            throw new InvalidDataException("Registered history changed since binding.");
        var seeds = new HashSet<int>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (name, hash) in p.HistoryFiles)
        {
            ct.ThrowIfCancellationRequested();
            if (!Path.IsPathFullyQualified(name) || !TowerContractJson.Hash(hash) || HarnessJson.FileHash(name) != hash)
                throw new InvalidDataException("Changed history input.");
            if (seen.Add(hash)) seeds.UnionWith(TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(name)));
        }
        Seeds(HarnessJson.Read<TowerCompleteSeeds>(Path.Combine(root, "seeds.json")), seeds);
        foreach (var name in new[] { "reservation-intent.json", "allocation-journal.jsonl", "reservation.json", "history-input.json", "seed-ledger.json" })
            if (!p.FrozenFiles.ContainsKey(name)) throw new InvalidDataException("Missing external reservation proof.");
        TowerCompleteReservation.Verify(root, HarnessJson.Read<TowerCompleteSeeds>(Path.Combine(root, "seeds.json")), TowerCompleteFamilySetup.Allocator, ct);
    }

    public static async Task<object> Inspect(string source, string output, CancellationToken ct = default)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Seed-free inspection cannot fight.")).Activate();
        var s = await Source(source, ct);
        var result = new { status = "ControllerSourceVerifiedNoSeedsNoCombat", version = TowerCompleteFamily.Version,
            capacity = TowerCompleteFamily.Capacity(s.Cells, ct), sourceSeal = TowerCompleteFamily.SourceSeal,
            first = Enumerable.Range(0, 33).Select(w => TowerCompleteFamily.Interval(w, 32, 43319)).ToArray(),
            secondMaximum = Enumerable.Range(0, 257).Select(w => TowerCompleteFamily.Interval(w, 256, 4096)).ToArray(),
            orderedCellsHash = HarnessJson.Hash(s.Cells), originalScenarios = s.Scenarios.Count, preparations = 0, fights = 0, newSeeds = 0 };
        HarnessJson.WriteNew(output, result); return result;
    }
}
