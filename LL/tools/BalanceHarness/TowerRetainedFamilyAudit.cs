using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record TowerRetainedAuditRequest(string Version, string Inventory, string MidpointRoot,
    IReadOnlyDictionary<string, string> InputHashes, IReadOnlyDictionary<string, string> GameplayHashes,
    int Entries, int MaximumSeconds, long MaximumBytes);
public sealed record TowerRetainedAuditIdentity(string RecipeHash, string ContextHash, string CellHash, string EquipmentHash);
public sealed record TowerRetainedAuditRow(int Ordinal, string InventoryKey, string Status, string? Error,
    string? ScenarioHash, TowerRetainedAuditIdentity? Identity, string? InputHash, string? ParticipantsHash,
    int Sources, JsonElement? Participants);
public sealed record TowerRetainedAuditResult(string Status, int Entries, int Materialized, int Invalid,
    int DistinctCells, int AliasEntries, int Contexts, int RequiredMidpointRecipes, int MatchedMidpointRecipes,
    IReadOnlyDictionary<string, int> ContextCounts, IReadOnlyDictionary<string, int> Errors,
    IReadOnlyDictionary<string, string> MidpointMatches, double Seconds, int Fights, int NewSeeds);

/// <summary>Read-only all-entry materialization. No allocator, combat, confirmation or cap extension.</summary>
public static class TowerRetainedFamilyAudit
{
    public const string Version = "tower-retained-family-audit-v1";
    public const string InventoryHash = "8173a6535d12dacdfd6e71eda939520dfeabb7e4399eae7db7d98837feb21fcf";
    public const string MidpointManifest = "8a23325bea8fe71f62be5f313b6e49cf29814c276572122e1970c2412d0a2300";
    public const int Entries = 43879, MaximumSeconds = 1500;
    public const long MaximumBytes = 1073741824;
    internal static JsonSerializerOptions Strict { get; } = new(HarnessJson.Options) {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, RespectRequiredConstructorParameters = true,
        WriteIndented = false };

    internal static TowerScenario Scenario(JsonElement raw, TowerBalanceCohort target, TowerScenario context)
    {
        var s = raw.Deserialize<TowerScenario>(Strict) ?? throw new InvalidDataException("Empty scenario.");
        if (s.SchemaVersion != 1 || !TowerBenchmark.SafeId(s.Id) || s.Seeds is not { Count: 0 }
            || s.Assumptions is null || s.Assumptions.Any(a => a is null)
            || s.FloorNumber != target.Budget.PriorityFloor || s.PreparationState != context.PreparationState
            || s.StartsAt != context.StartsAt) throw new InvalidDataException("Incompatible retained scenario context or nonempty schedule.");
        // Sorting declared slots is equivalent to the production materializer; preserve character and Essence identities.
        if (s.Party is null || s.Party.Any(p => p is null)) throw new InvalidDataException("Missing party.");
        s = s with { Party = s.Party.OrderBy(p => p.PartySlot).ToArray() };
        TowerBossDiscovery.ValidateEquipment(s.Party, target.Budget, target.RequiredPartySize);
        return s;
    }

    internal static TowerRetainedAuditIdentity Identity(TowerScenario s)
    {
        var recipe = TowerBossDiscovery.RecipeHash(s.Party);
        var equipment = TowerBossDiscovery.EquipmentBudgetHash(s.Party);
        var context = HarnessJson.Hash(new { s.SchemaVersion, s.Id, s.FloorNumber, s.StartsAt, s.PreparationState, Equipment = equipment });
        return new(recipe, context, HarnessJson.Hash(new { Context = context, Recipe = recipe }), equipment);
    }

    internal static void ParticipantShape(JsonElement participants, int partySize)
    {
        var rows = participants.EnumerateArray().ToArray();
        if (rows.Length != partySize + 1 || rows.Count(p => p.GetProperty("slot").GetProperty("side").GetString() == "Friendly") != partySize
            || rows.Count(p => p.GetProperty("slot").GetProperty("side").GetString() == "Hostile") != 1)
            throw new InvalidDataException("Prepared roster does not contain the complete party and guardian.");
    }

    // The public audit and synthetic tests share this exact bounded, streaming loop.
    internal static async Task<TowerRetainedAuditResult> Scan(Stream inventory, Stream rowsOutput, int expected,
        TowerBalanceCohort cohort, TowerScenario context,
        IReadOnlyDictionary<string, (string Id, string ParticipantsHash)> required,
        Func<TowerScenario, CancellationToken, Task<(string InputHash, JsonElement Participants)>> prepare,
        Action checkpoint, CancellationToken ct = default, Action<string>? progress = null, TowerPerformanceTrace? trace = null)
    {
        using var guard = trace is null ? new TowerPerformanceTrace(_ => throw new InvalidOperationException("Inventory audit cannot fight.")).Activate() : null;
        var clock = Stopwatch.StartNew(); var keys = new HashSet<string>(StringComparer.Ordinal);
        var identities = new Dictionary<string, string>(StringComparer.Ordinal);
        var contexts = new Dictionary<string, int>(StringComparer.Ordinal); var errors = new Dictionary<string, int>(StringComparer.Ordinal);
        var matched = new Dictionary<string, string>(StringComparer.Ordinal); var count = 0; var valid = 0;
        await foreach (var raw in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(inventory, Strict, ct))
        {
            ct.ThrowIfCancellationRequested(); checkpoint();
            if (++count > expected) throw new InvalidDataException("Inventory exceeds frozen entry count.");
            if (!raw.EnumerateObject().Select(p => p.Name).Order().SequenceEqual(new[] { "key", "scenario", "sources" }))
                throw new InvalidDataException("Unexpected retained entry envelope.");
            var key = raw.GetProperty("key").GetString(); var sources = raw.GetProperty("sources").GetArrayLength();
            if (!TowerContractJson.Hash(key) || !keys.Add(key!) || sources < 1) throw new InvalidDataException("Invalid, duplicate or origin-free inventory key.");
            string? scenarioHash = null; TowerRetainedAuditIdentity? identity = null; TowerRetainedAuditRow row;
            try
            {
                var scenario = Scenario(raw.GetProperty("scenario"), cohort, context); scenarioHash = HarnessJson.Hash(scenario);
                identity = Identity(scenario);
                var prepared = await prepare(scenario, ct); ParticipantShape(prepared.Participants, cohort.RequiredPartySize);
                var digest = HarnessJson.Hash(prepared.Participants);
                if (identities.TryGetValue(identity.CellHash, out var prior) && prior != digest)
                    throw new InvalidDataException("A canonical recipe/context alias materializes different participants.");
                if (required.TryGetValue(identity.CellHash, out var match))
                {
                    if (digest != match.ParticipantsHash) throw new InvalidDataException("Retained midpoint recipe has different prepared participants.");
                    matched[match.Id] = key!;
                }
                identities[identity.CellHash] = digest; contexts[identity.ContextHash] = contexts.GetValueOrDefault(identity.ContextHash) + 1; valid++;
                row = new(count - 1, key!, "Materialized", null, scenarioHash, identity, prepared.InputHash, digest, sources, prepared.Participants);
            }
            catch (Exception e) when (e is InvalidDataException or JsonException or ArgumentException or KeyNotFoundException)
            {
                var reason = e.GetType().Name + ": " + e.Message; errors[reason] = errors.GetValueOrDefault(reason) + 1;
                row = new(count - 1, key!, "InvalidRetainedEntry", reason, scenarioHash, identity, null, null, sources, null);
            }
            var bytes = JsonSerializer.SerializeToUtf8Bytes(row, Strict); await rowsOutput.WriteAsync(bytes, ct); await rowsOutput.WriteAsync(new byte[] { 10 }, ct);
            if (count % 1024 == 0) { await rowsOutput.FlushAsync(ct); checkpoint(); progress?.Invoke($"Retained audit: {count}/{expected}; materialized {valid}; {clock.Elapsed.TotalSeconds:F1}s."); }
        }
        if (count != expected) throw new InvalidDataException("Incomplete retained inventory.");
        checkpoint();
        var status = valid == expected && matched.Count == required.Count ? "MaterializedCompleteFamily" : "RetainedFamilyIssues";
        return new(status, count, valid, count - valid, identities.Count, valid - identities.Count, contexts.Count,
            required.Count, matched.Count, contexts, errors, matched, clock.Elapsed.TotalSeconds, 0, 0);
    }

    public static async Task<TowerRetainedAuditResult> Run(string requestPath, string output, CancellationToken token = default, Action<string>? progress = null)
    {
        token.ThrowIfCancellationRequested(); var request = TowerContractJson.Read<TowerRetainedAuditRequest>(requestPath);
        if (request.Version != Version || request.Entries != Entries || request.MaximumSeconds is < 1 or > MaximumSeconds
            || request.MaximumBytes is < 1048576 or > MaximumBytes || request.InputHashes.Count < 3 || request.GameplayHashes.Count != 4)
            throw new InvalidDataException("Invalid frozen zero-combat audit scope.");
        using var lease = TowerCompactBundle.AcquireWriter(output);
        if (Path.Exists(output)) throw new IOException("Use a new audit output. No retry/resume.");
        Directory.CreateDirectory(output); File.Copy(requestPath, Path.Combine(output, "request.json"));
        HarnessJson.WriteNew(Path.Combine(output, "started.json"), new { utc = DateTimeOffset.UtcNow, request.MaximumSeconds, request.MaximumBytes, fights = 0, newSeeds = 0 });
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(request.MaximumSeconds));
        var ct = deadline.Token; var clock = Stopwatch.StartNew();
        var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Retained audit cannot fight.")); using var active = trace.Activate();
        using var process = Process.GetCurrentProcess(); var cpu = process.TotalProcessorTime; var allocated = GC.GetTotalAllocatedBytes();
        void Check() { ct.ThrowIfCancellationRequested(); if (clock.Elapsed.TotalSeconds > request.MaximumSeconds) throw new OperationCanceledException("Audit deadline.", ct); }
        try
        {
            using (TowerPerformanceTrace.Measure("audit.inputs"))
            {
                foreach (var pair in request.InputHashes) { Check(); if (HarnessJson.FileHash(pair.Key) != pair.Value) throw new InvalidDataException("Changed audit input: " + pair.Key); }
                if (!request.InputHashes.ContainsKey(Path.GetFullPath(request.Inventory)) || HarnessJson.FileHash(request.Inventory) != InventoryHash
                    || HarnessJson.FileHash(Path.Combine(request.MidpointRoot, "midpoint-files.json")) != MidpointManifest)
                    throw new InvalidDataException("Use the frozen retained inventory and completed midpoint.");
                TowerBulkCampaign.VerifyFiles(request.MidpointRoot, "midpoint-files.json", true, ct);
                var execution = ExecutionIdentity.Current();
                foreach (var pair in request.GameplayHashes)
                    if (!execution.AssemblyHashes.TryGetValue(pair.Key, out var actual) || actual != pair.Value) throw new InvalidDataException("Captured gameplay mismatch.");
            }
            var d = TowerBalanceEvaluator.Read(Path.Combine(request.MidpointRoot, "definition.json")); var cohort = d.Cohorts.Single(); var context = d.Cells[0].Scenario;
            var materialized = HarnessJson.Read<TowerMidpointMaterialization>(Path.Combine(request.MidpointRoot, "materialized/materialization.json"));
            var reference = materialized.Variant.Cells.ToDictionary(c => c.Id);
            var required = d.Cells.ToDictionary(c => Identity(c.Scenario).CellHash, c => (c.Id, reference[c.Id].ParticipantsHash));
            var contentRoot = Path.Combine(request.MidpointRoot, "materialized/content"); var settings = TowerBundle.ReadSettings(contentRoot);
            var content = new OfflineContent(contentRoot, settings.Threat); var runner = new TowerBattleRunner(contentRoot, content);
            var probe = d.Cells[0].Scenario.Seeds[0]; // Existing reserved value, preparation only. Never allocate a schedule.
            TowerRetainedAuditResult result;
            using (TowerPerformanceTrace.Measure("audit.materialize-all"))
            await using (var source = File.OpenRead(request.Inventory))
            await using (var zip = new GZipStream(source, CompressionMode.Decompress))
            await using (var target = new FileStream(Path.Combine(output, "rows.jsonl.gz"), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                await using (var compressed = new GZipStream(target, CompressionLevel.Fastest, true))
                    result = await Scan(zip, compressed, Entries, cohort, context, required, async (scenario, cancellation) => {
                        var input = runner.CreateInput(scenario with { Seeds = [probe] }, probe, settings.Threat, settings.CheckpointIntervalTicks);
                        var prepared = IdleBattleRunner.DescribeParticipants(await runner.PrepareAsync(input, cancellation));
                        return (HarnessJson.Hash(input), prepared);
                    }, () => { Check(); if (target.Length > request.MaximumBytes - 1048576) throw new IOException("Audit output cap."); }, ct, progress, trace);
                target.Flush(true);
            }
            HarnessJson.WriteNew(Path.Combine(output, "result.json"), result);
            HarnessJson.WriteNew(Path.Combine(output, "performance.json"), new { status = result.Status, seconds = clock.Elapsed.TotalSeconds,
                cpuSeconds = (process.TotalProcessorTime - cpu).TotalSeconds, allocatedBytes = GC.GetTotalAllocatedBytes() - allocated,
                peakWorkingSetBytes = process.PeakWorkingSet64, timings = trace.Snapshot(), fights = 0, newSeeds = 0 });
            var inventory = TowerBulkCampaign.Paths(output).ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash);
            HarnessJson.WriteNew(Path.Combine(output, "files.json"), inventory);
            TowerBulkCampaign.VerifyFiles(output, "files.json", true, ct);
            if (TowerBulkCampaign.StorageBytes(output, ct) > request.MaximumBytes) throw new IOException("Final audit output cap.");
            Check(); return result;
        }
        catch (Exception e)
        {
            HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new { error = e.ToString(), seconds = clock.Elapsed.TotalSeconds, fights = 0, newSeeds = 0, noRetry = true });
            throw;
        }
    }
}
