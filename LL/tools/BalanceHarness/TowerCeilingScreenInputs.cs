using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Common.Randomness;

namespace BalanceHarness;

public sealed record TowerCeilingBindingRequest(string Version, string PreparationRoot, string PreparationManifestHash,
    string LedgerPath, string LedgerHash, IReadOnlyDictionary<string, string> HistoryFiles,
    double PriorSetupSeconds, IReadOnlyDictionary<string, string> SetupFiles);
public sealed record TowerCeilingScreenSeeds(string Version, IReadOnlyList<int> Historical, IReadOnlyList<int> Shared);
public sealed record TowerCeilingScreenProtocol(string Version, string HarnessHash, string PreparationManifestHash,
    int MaximumFights, int MaximumSeconds, long MaximumBytes, int Retries, string StorageAccounting,
    double SetupSeconds, long PriorSetupBytes, IReadOnlyDictionary<string, string> FrozenFiles);

/// <summary>Bind an externally reserved schedule. No seed allocation is exposed by this controller.</summary>
public static class TowerCeilingScreenInputs
{
    public const int MaximumSeconds = 14400;
    public const long MaximumBytes = 8589934592;
    internal const string FinalFiles = "screen-files.json";
    internal static string HarnessHash => HarnessJson.FileHash(typeof(TowerCeilingScreenInputs).Assembly.Location);

    internal static Dictionary<string, string> Inventory(string root) => TowerBulkCampaign.Paths(root)
        .Where(p => Path.GetRelativePath(root, p) != FinalFiles && Path.GetRelativePath(root, p) != FinalFiles + ".pending")
        .ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'), HarnessJson.FileHash, StringComparer.Ordinal);

    internal static void ValidateLimits(TowerCeilingScreenProtocol p)
    {
        if (p.Version != TowerCeilingScreenContract.Version || p.HarnessHash != HarnessHash
            || p.MaximumFights != TowerCeilingScreenContract.MaximumFights || p.MaximumSeconds != MaximumSeconds
            || p.MaximumBytes != MaximumBytes || p.Retries != 0 || p.StorageAccounting != TowerStorageAccountant.Mode
            || !double.IsFinite(p.SetupSeconds) || p.SetupSeconds < 0 || p.SetupSeconds >= MaximumSeconds
            || p.PriorSetupBytes < 0 || p.PriorSetupBytes >= MaximumBytes - 1048576
            || !TowerContractJson.Hash(p.PreparationManifestHash))
            throw new InvalidDataException("Changed controller identity, grid limits, setup charge or retry policy.");
    }

    internal static long SetupBytes(TowerCeilingBindingRequest request, string requestPath)
    {
        if (!double.IsFinite(request.PriorSetupSeconds) || request.PriorSetupSeconds < 0 || request.PriorSetupSeconds >= MaximumSeconds
            || request.SetupFiles.Count == 0 || !request.SetupFiles.TryGetValue(request.LedgerPath, out var ledgerHash) || ledgerHash != request.LedgerHash)
            throw new InvalidDataException("Record all pre-binding setup time/files, including the externally reserved ledger.");
        long bytes = new FileInfo(requestPath).Length;
        foreach (var (path, hash) in request.SetupFiles)
        {
            if (!Path.IsPathFullyQualified(path) || !TowerContractJson.Hash(hash) || HarnessJson.FileHash(path) != hash)
                throw new InvalidDataException("Changed or unbound setup artifact.");
            bytes = checked(bytes + new FileInfo(path).Length);
        }
        if (bytes >= MaximumBytes - 1048576) throw new InvalidDataException("Prior setup exhausted the global storage budget.");
        return bytes;
    }

    internal static void ValidateSeeds(TowerCeilingScreenSeeds seeds, IReadOnlyList<int> required)
    {
        if (seeds.Version != TowerCeilingScreenContract.Version || seeds.Shared.Count != 128
            || seeds.Shared.Distinct().Count() != 128 || seeds.Historical.Count < 481219
            || seeds.Historical.Count > TowerStudyLimits.HistoricalSeeds - 128
            || !seeds.Historical.SequenceEqual(seeds.Historical.Distinct().Order())
            || !seeds.Historical.SequenceEqual(required.Distinct().Order())
            || seeds.Shared.Intersect(seeds.Historical).Any())
            throw new InvalidDataException("Bind exactly 128 fresh shared reservations and the complete refreshed history.");
        // Verify the separately allocated list; never return or publish new values here.
        var used = seeds.Historical.ToHashSet(); var index = 0;
        for (var attempt = 0; index < 128 && attempt < 100000; attempt++)
        {
            var value = StableRandom.Seed(TowerCeilingScreenContract.Version, "2026091421", "screen", attempt.ToString(CultureInfo.InvariantCulture));
            if (used.Add(value) && seeds.Shared[index++] != value) throw new InvalidDataException("Shared schedule differs from frozen allocator order.");
        }
        if (index != 128) throw new InvalidDataException("Frozen seed proposal limit exhausted.");
    }

    internal static TowerBalanceDefinition BindDefinition(TowerBalanceDefinition template, TowerCeilingScreenSeeds seeds) => template with {
        ExecutionHash = HarnessJson.Hash(ExecutionIdentity.Current()), ExcludedCombatSeeds = seeds.Historical,
        Cells = template.Cells.Select(c => c with { Scenario = c.Scenario with { Seeds = seeds.Shared } }).ToArray()
    };

    internal static void ValidateDefinitions(IReadOnlyList<TowerBalanceDefinition> definitions,
        IReadOnlyList<TowerBalanceDefinition> templates, TowerCeilingScreenSeeds seeds)
    {
        if (definitions.Count != 4 || templates.Count != 4) throw new InvalidDataException("All four factors are required.");
        for (var i = 0; i < 4; i++)
        {
            var d = definitions[i]; TowerBalanceEvaluator.Validate(d);
            if (d.Id != TowerCeilingScreenContract.VariantId(i) || d.SchemaVersion != 1 || d.Cells.Count != 253
                || d.MaximumBattles != 32384 || d.Cells.Any(c => c.MinimumSamples != 128)
                || !d.Cells.Select(c => c.Id).SequenceEqual(d.Cells.Select(c => c.Id).Order(StringComparer.Ordinal)))
                throw new InvalidDataException("Changed factor order, recipe family or per-factor envelope.");
            TowerPortfolioConfirmation.Equal(BindDefinition(templates[i], seeds), d, "complete bound factor");
        }
    }

    internal static TowerCeilingPreparationReceipt ReadPreparation(string root, string hash, CancellationToken token)
    {
        if (HarnessJson.FileHash(Path.Combine(root, "prepared-files.json")) != hash) throw new InvalidDataException("Preparation manifest changed.");
        var receipt = TowerCeilingScreenPreparation.Verify(root, token);
        TowerPortfolioConfirmationArchive.RequireGameplay(receipt.Execution, ExecutionIdentity.Current());
        if (receipt.Reservations != 481219 || receipt.Variants.Count != 4
            || !receipt.Variants.Select(v => v.Id).SequenceEqual(Enumerable.Range(0, 4).Select(TowerCeilingScreenContract.VariantId))
            || !receipt.Variants.Select(v => v.Factor).SequenceEqual(TowerCeilingScreenContract.Factors)
            || receipt.Variants.Any(v => v.Cells.Count != 253)) throw new InvalidDataException("Incomplete prepared grid.");
        return receipt;
    }

    // This reuses only the already charged historical probe, and stops before the engine.
    public static async Task<object> AuditMaterializationAsync(string root, string manifestHash, CancellationToken token = default)
    {
        var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Materialization audit cannot fight."));
        using var active = trace.Activate(); var clock = Stopwatch.StartNew();
        var receipt = ReadPreparation(root, manifestHash, token);
        var request = HarnessJson.Read<TowerCeilingPreparationRequest>(Path.Combine(root, "request.json"));
        var count = 0;
        foreach (var variant in request.Variants)
        {
            var d = TowerBalanceEvaluator.Read(Path.Combine(root, variant.Id, "definition-template.json"));
            var settings = TowerBundle.ReadSettings(variant.ContentRoot);
            TowerPortfolioConfirmation.Equal(d.ContentHashes, TowerCompactBundle.ContentHashes(variant.ContentRoot, token), "prepared content");
            if (HarnessJson.Hash(settings) != d.SettingsHash) throw new InvalidDataException("Changed prepared settings.");
            var runner = new TowerBattleRunner(variant.ContentRoot, new OfflineContent(variant.ContentRoot, settings.Threat));
            foreach (var cell in d.Cells)
            {
                token.ThrowIfCancellationRequested();
                var saved = HarnessJson.Read<JsonElement>(Path.Combine(root, variant.Id, cell.Id + ".json"));
                var input = runner.CreateInput(cell.Scenario with { Seeds = [receipt.ProbeSeed] }, receipt.ProbeSeed, settings.Threat, settings.CheckpointIntervalTicks);
                TowerPortfolioConfirmation.Equal(input, saved.GetProperty("input"), "original probe input");
                TowerPortfolioConfirmation.Equal(IdleBattleRunner.DescribeParticipants(await runner.PrepareAsync(input, token)), saved.GetProperty("participants"), "original prepared participants");
                count++;
            }
        }
        if (count != 1012) throw new InvalidDataException("Missing materialized cells.");
        return new { status = "Verified", cells = count, fights = 0, newSeeds = 0, seconds = clock.Elapsed.TotalSeconds, timings = trace.Snapshot() };
    }

    public static async Task<TowerCeilingScreenProtocol> BindAsync(string requestPath, string output, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested(); output = Path.GetFullPath(output);
        using var lease = TowerCompactBundle.AcquireWriter(output);
        if (Path.Exists(output)) throw new IOException("Use a new study output; no binding retry or resume.");
        var clock = Stopwatch.StartNew(); using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(MaximumSeconds)); var ct = deadline.Token;
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Binding cannot fight.")).Activate();
        var request = HarnessJson.Read<TowerCeilingBindingRequest>(requestPath);
        if (request.Version != TowerCeilingScreenContract.Version || request.HistoryFiles.Count == 0
            || HarnessJson.FileHash(request.LedgerPath) != request.LedgerHash) throw new InvalidDataException("Missing bound reservation ledger/history.");
        var priorBytes = SetupBytes(request, requestPath);
        if (request.PriorSetupSeconds + clock.Elapsed.TotalSeconds >= MaximumSeconds) throw new InvalidDataException("Prior setup exhausted the global deadline.");
        deadline.CancelAfter(TimeSpan.FromSeconds(MaximumSeconds - request.PriorSetupSeconds - clock.Elapsed.TotalSeconds));
        var receipt = ReadPreparation(request.PreparationRoot, request.PreparationManifestHash, ct);
        var sourceRequest = HarnessJson.Read<TowerCeilingPreparationRequest>(Path.Combine(request.PreparationRoot, "request.json"));
        var source = sourceRequest.CapturedRoot;
        if (HarnessJson.FileHash(Path.Combine(source, "seed-ledger.json")) != sourceRequest.LedgerHash
            || HarnessJson.FileHash(Path.Combine(source, "final-files.json")) != sourceRequest.SourceManifestHash) throw new InvalidDataException("Captured source binding changed.");
        TowerBulkCampaign.VerifyFiles(source, "final-files.json", true, ct);
        var oldSeeds = HarnessJson.Read<TowerPortfolioConfirmationSeeds>(Path.Combine(source, "seed-ledger.json"));
        var historical = oldSeeds.Historical.Concat(oldSeeds.Confirmation).ToHashSet();
        foreach (var (path, hash) in request.HistoryFiles)
        {
            ct.ThrowIfCancellationRequested();
            if (!Path.IsPathFullyQualified(path) || !TowerContractJson.Hash(hash) || HarnessJson.FileHash(path) != hash)
                throw new InvalidDataException("Changed registered seed-history input.");
            historical.UnionWith(TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(path)));
        }
        var seeds = HarnessJson.Read<TowerCeilingScreenSeeds>(request.LedgerPath); ValidateSeeds(seeds, historical.Order().ToArray());
        Directory.CreateDirectory(output); string P(string n) => Path.Combine(output, n);
        try
        {
            // Explicit WriteThrough + Flush(true): reservations survive any following partial setup.
            using (var ledger = new FileStream(P("seed-ledger.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read, 1, FileOptions.WriteThrough))
            { JsonSerializer.Serialize(ledger, seeds, HarnessJson.Options); ledger.Flush(true); }
            File.Copy(requestPath, P("binding-request.json"));
            HarnessJson.WriteNew(P("preparation-receipt.json"), receipt);
            Directory.CreateDirectory(P("setup-evidence")); var setupIndex = 0;
            foreach (var (path, hash) in request.SetupFiles.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                ct.ThrowIfCancellationRequested();
                var copy = P("setup-evidence/" + (setupIndex++).ToString("D4", CultureInfo.InvariantCulture) + ".artifact");
                File.Copy(path, copy);
                if (HarnessJson.FileHash(copy) != hash) throw new InvalidDataException("Setup artifact changed during copy.");
            }
            Directory.CreateDirectory(P("history")); var historyIndex = 0;
            foreach (var (path, hash) in request.HistoryFiles.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                ct.ThrowIfCancellationRequested();
                var copy = P("history/" + (historyIndex++).ToString("D4", CultureInfo.InvariantCulture) + ".json");
                File.Copy(path, copy);
                if (HarnessJson.FileHash(copy) != hash) throw new InvalidDataException("History changed during durable copy.");
            }
            var templates = new List<TowerBalanceDefinition>(); var definitions = new List<TowerBalanceDefinition>();
            for (var i = 0; i < 4; i++)
            {
                ct.ThrowIfCancellationRequested(); var id = TowerCeilingScreenContract.VariantId(i); var variant = sourceRequest.Variants[i];
                var template = TowerBalanceEvaluator.Read(Path.Combine(request.PreparationRoot, id, "definition-template.json"));
                var content = P(id + "-input");
                TowerPortfolioConfirmation.Equal(template.ContentHashes, TowerBundle.CopyContent(variant.ContentRoot, content, ct), "copied factor content");
                File.Copy(Path.Combine(variant.ContentRoot, "appsettings.json"), Path.Combine(content, "appsettings.json"));
                var d = BindDefinition(template, seeds); templates.Add(template); definitions.Add(d);
                HarnessJson.WriteNew(P(id + "-definition.json"), d);
                // Verify seed binding cannot change the production prepared roster.
                var settings = TowerBundle.ReadSettings(content); var runner = new TowerBattleRunner(content, new OfflineContent(content, settings.Threat));
                foreach (var cell in d.Cells)
                {
                    ct.ThrowIfCancellationRequested();
                    var input = runner.CreateInput(cell.Scenario, seeds.Shared[0], settings.Threat, settings.CheckpointIntervalTicks);
                    var participants = IdleBattleRunner.DescribeParticipants(await runner.PrepareAsync(input, ct));
                    if (HarnessJson.Hash(participants) != receipt.Variants[i].Cells.Single(c => c.Id == cell.Id).ParticipantsHash)
                        throw new InvalidDataException("Fresh binding changed prepared participants.");
                }
                if (priorBytes + TowerBulkCampaign.StorageBytes(output, ct) > MaximumBytes) throw new InvalidDataException("Setup storage cap exceeded.");
            }
            ValidateDefinitions(definitions, templates, seeds);
            HarnessJson.WriteNew(P("executable-files.json"), TowerBossStudy.RetainExecutable(output, ExecutionIdentity.Current()));
            var files = Inventory(output);
            var protocol = new TowerCeilingScreenProtocol(TowerCeilingScreenContract.Version, HarnessHash, request.PreparationManifestHash,
                129536, MaximumSeconds, MaximumBytes, 0, TowerStorageAccountant.Mode, request.PriorSetupSeconds + clock.Elapsed.TotalSeconds, priorBytes, files);
            ValidateLimits(protocol); HarnessJson.WriteNew(P("protocol.json"), protocol);
            // All setup through publication is charged. The small final receipt is included in the exact pre-run inventory.
            ct.ThrowIfCancellationRequested();
            if (priorBytes + TowerBulkCampaign.StorageBytes(output, ct) > MaximumBytes) throw new InvalidDataException("Final setup storage cap exceeded.");
            HarnessJson.WriteNew(P("setup-charge.json"), new { seconds = request.PriorSetupSeconds + clock.Elapsed.TotalSeconds });
            return protocol; // The following run charges its complete preflight and reconstruction to the same remaining budget.
        }
        catch (Exception e)
        {
            HarnessJson.WriteNew(P("binding-failure.json"), new { error = e.ToString(), seconds = clock.Elapsed.TotalSeconds, newFights = 0, noResume = true }); throw;
        }
    }

    internal static (TowerCeilingScreenProtocol Protocol, TowerBalanceDefinition[] Definitions, TowerCeilingPreparationReceipt Receipt) Inputs(string root, CancellationToken token, bool checkLiveHistory = false)
    {
        var p = HarnessJson.Read<TowerCeilingScreenProtocol>(Path.Combine(root, "protocol.json")); ValidateLimits(p);
        TowerPortfolioConfirmationRun.VerifyFrozen(root, p.FrozenFiles, token);
        var request = HarnessJson.Read<TowerCeilingBindingRequest>(Path.Combine(root, "binding-request.json"));
        if (request.PreparationManifestHash != p.PreparationManifestHash) throw new InvalidDataException("Preparation binding differs.");
        var receipt = ReadPreparation(request.PreparationRoot, p.PreparationManifestHash, token);
        TowerPortfolioConfirmation.Equal(receipt, HarnessJson.Read<TowerCeilingPreparationReceipt>(Path.Combine(root, "preparation-receipt.json")), "prepared receipt");
        var setupIndex = 0; long priorBytes = new FileInfo(Path.Combine(root, "binding-request.json")).Length;
        foreach (var (path, hash) in request.SetupFiles.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            var copy = Path.Combine(root, "setup-evidence", (setupIndex++).ToString("D4", CultureInfo.InvariantCulture) + ".artifact");
            if (HarnessJson.FileHash(copy) != hash || checkLiveHistory && HarnessJson.FileHash(path) != hash)
                throw new InvalidDataException("Prior setup evidence changed.");
            priorBytes = checked(priorBytes + new FileInfo(copy).Length);
        }
        if (priorBytes != p.PriorSetupBytes || request.PriorSetupSeconds > p.SetupSeconds) throw new InvalidDataException("Lost prior setup charge.");
        var seeds = HarnessJson.Read<TowerCeilingScreenSeeds>(Path.Combine(root, "seed-ledger.json"));
        var required = TowerBalanceEvaluator.Read(Path.Combine(request.PreparationRoot, "scale-100", "definition-template.json")).ExcludedCombatSeeds.ToHashSet();
        var historyIndex = 0;
        foreach (var (path, hash) in request.HistoryFiles.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            var copy = Path.Combine(root, "history", (historyIndex++).ToString("D4", CultureInfo.InvariantCulture) + ".json");
            if (HarnessJson.FileHash(copy) != hash || checkLiveHistory && HarnessJson.FileHash(path) != hash)
                throw new InvalidDataException("Registered history changed since binding; stop and reconcile explicitly.");
            required.UnionWith(TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(copy)));
        }
        ValidateSeeds(seeds, required.Order().ToArray());
        var templates = Enumerable.Range(0, 4).Select(i => TowerBalanceEvaluator.Read(Path.Combine(request.PreparationRoot, TowerCeilingScreenContract.VariantId(i), "definition-template.json"))).ToArray();
        var definitions = Enumerable.Range(0, 4).Select(i => TowerBalanceEvaluator.Read(Path.Combine(root, TowerCeilingScreenContract.VariantId(i) + "-definition.json"))).ToArray();
        ValidateDefinitions(definitions, templates, seeds);
        return (p, definitions, receipt);
    }

    public static TowerCeilingScreenProtocol VerifyPrepared(string root, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Prepared verification cannot fight.")).Activate();
        if (File.Exists(Path.Combine(root, "started.json")) || File.Exists(Path.Combine(root, FinalFiles)))
            throw new InvalidDataException("Study already started; no resume, retry or extension.");
        var p = Inputs(root, token, checkLiveHistory: true).Protocol;
        var expected = p.FrozenFiles.Keys.Append("protocol.json").Append("setup-charge.json").ToHashSet(StringComparer.Ordinal);
        if (!expected.SetEquals(TowerBulkCampaign.Paths(root).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))))
            throw new InvalidDataException("Unbound or extra study files.");
        var setup = SetupSeconds(root, p);
        if (setup >= MaximumSeconds || p.PriorSetupBytes + TowerBulkCampaign.StorageBytes(root, token) > MaximumBytes) throw new InvalidDataException("Setup exhausted study limits.");
        return p;
    }

    internal static double SetupSeconds(string root, TowerCeilingScreenProtocol p)
    {
        var value = HarnessJson.Read<JsonElement>(Path.Combine(root, "setup-charge.json")).GetProperty("seconds").GetDouble();
        if (!double.IsFinite(value) || value < p.SetupSeconds || value >= MaximumSeconds) throw new InvalidDataException("Invalid total setup time charge.");
        return value;
    }
}
