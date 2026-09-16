using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Common.Randomness;

namespace BalanceHarness;

public sealed record TowerMidpointSeeds(string Version, IReadOnlyList<int> Historical, IReadOnlyList<int> Shared);
public sealed record TowerMidpointMaterialization(string Status, string SourceRoot, string SourceManifestHash,
    string HarnessHash, TowerCeilingPreparedVariant Variant, int ProbeSeed);
public sealed record TowerMidpointProtocol(string Version, string HarnessHash, int MaximumFights,
    double MaximumSeconds, long MaximumBytes, int Retries, double SetupSeconds, IReadOnlyDictionary<string, string> FrozenFiles);
public sealed record TowerMidpointResult(string Status, decimal Factor, int MaximumWins,
    IReadOnlyList<TowerCeilingCellResult> Cells);

/// <summary>A new fixed midpoint study. Earlier grid contracts and evaluator caps are unchanged.</summary>
public static class TowerMidpointStudy
{
    public const string Version = "tower-captured-v19-midpoint-v1", FinalFiles = "midpoint-files.json";
    public const int Recipes = 253, Samples = 256, MaximumFights = Recipes * Samples, MaximumSeconds = 14400;
    public const long MaximumBytes = 8589934592;
    public const decimal Factor = 1.10m;
    public const string SourceManifest = "81a3217fff5d9035efd5540a8e8c42013424dd504e970e19e27bad4a2f5f8da8";
    public const string SourceLedger = "ed43c2bd3a0ccd6f9b488f6817f04a07e7c35e72eec4a59283707d1db41b3c34";
    internal static string HarnessHash => HarnessJson.FileHash(typeof(TowerMidpointStudy).Assembly.Location);
    // Retain the preceding 1,012-cell per-cell threshold, even though this fresh fixed setting has 253 cells.
    public static RateEstimate Interval(int wins) => TowerStagedBalance.Interval(wins, Samples, 506);

    internal static JsonNode Content(JsonNode baseline)
    {
        var result = baseline.DeepClone();
        var scaling = result["floors"]!.AsArray().Single(n => n!["floorNumber"]!.GetValue<int>() == 5)!["guardianScaling"]!;
        if (scaling["health"]!.GetValue<decimal>() != TowerCeilingScreenContract.Health
            || scaling["offense"]!.GetValue<decimal>() != TowerCeilingScreenContract.Offense)
            throw new InvalidDataException("Midpoint must start directly from captured baseline.");
        scaling["health"] = TowerCeilingScreenContract.Health * Factor;
        scaling["offense"] = TowerCeilingScreenContract.Offense * Factor;
        return result;
    }

    internal static void VerifyParticipants(JsonElement baseline, JsonElement candidate)
    {
        var a = JsonNode.Parse(baseline.GetRawText())!.AsArray(); var b = JsonNode.Parse(candidate.GetRawText())!.AsArray();
        if (a.Count != 11 || b.Count != 11) throw new InvalidDataException("Expected two five-player parties and Kharad.");
        var original = a.Single(n => n!["slot"]!["side"]!.GetValue<string>() == "Hostile")!;
        var guardian = b.Single(n => n!["slot"]!["side"]!.GetValue<string>() == "Hostile")!;
        foreach (var key in new[] { "Power", "MaxHealth" })
        {
            var expected = original["combatAttributes"]![key]!.GetValue<double>() * (double)Factor;
            var actual = guardian["combatAttributes"]![key]!.GetValue<double>();
            if (!double.IsFinite(actual) || Math.Abs(actual - expected) > Math.Max(.001, expected * .00001))
                throw new InvalidDataException("Midpoint participant scalar differs.");
        }
        if (guardian["health"]!.GetValue<int>() != (int)guardian["combatAttributes"]!["MaxHealth"]!.GetValue<double>())
            throw new InvalidDataException("Guardian starting health differs.");
        guardian["health"] = original["health"]!.DeepClone();
        foreach (var key in new[] { "Power", "MaxHealth" }) guardian["combatAttributes"]![key] = original["combatAttributes"]![key]!.DeepClone();
        if (!JsonNode.DeepEquals(a, b)) throw new InvalidDataException("Changed participant outside guardian Health/Power.");
    }

    private static void Gameplay()
    {
        var expected = new Dictionary<string, string> {
            ["Application"] = "c5de6e53ce658cb9a04c9e370a7f71b1229d512eebc49e69a7ee4f4d48603718",
            ["Common"] = "1a2e12d1bed3be3327d248e39a4c485f56c21c30c141c01558973bf794690cde",
            ["Domain"] = "0fb5a36c175ba838982c458a864ec5474b0f83a65c6e97ad873d7b0e67a0c4e8",
            ["Services.LL"] = "e5b70eee30a753556d6d9c048ec3502e2ee1fc86e27fdf7c9326a3d9d31d049e" };
        var current = ExecutionIdentity.Current();
        foreach (var pair in expected)
            if (!current.AssemblyHashes.TryGetValue(pair.Key, out var hash) || hash != pair.Value)
                throw new InvalidDataException("Use captured-v19 gameplay assemblies.");
    }

    internal static void Scope(string root)
    {
        var s = HarnessJson.Read<JsonElement>(Path.Combine(root, "scope.json"));
        if (s.GetProperty("version").GetString() != Version || s.GetProperty("factor").GetString() != "1.10"
            || s.GetProperty("samples").GetInt32() != Samples || s.GetProperty("recipes").GetInt32() != Recipes
            || s.GetProperty("maximumFights").GetInt32() != MaximumFights || s.GetProperty("maximumSeconds").GetInt32() != MaximumSeconds
            || s.GetProperty("maximumBytes").GetInt64() != MaximumBytes || s.GetProperty("retries").GetInt32() != 0
            || s.GetProperty("maximumFreshSeeds").GetInt32() != Samples || s.GetProperty("masterSeed").GetInt32() != 2026091422
            || s.GetProperty("intervalReferenceFamily").GetInt32() != 1012 || s.GetProperty("intervalAlpha").GetDouble() != .05)
            throw new InvalidDataException("Changed fixed midpoint scope.");
    }

    internal static double SetupElapsed(string root)
    {
        var s = HarnessJson.Read<JsonElement>(Path.Combine(root, "scope.json"));
        var seconds = (DateTimeOffset.UtcNow - s.GetProperty("createdUtc").GetDateTimeOffset()).TotalSeconds
            + s.GetProperty("initialReviewConservativeSeconds").GetDouble();
        if (!double.IsFinite(seconds) || seconds < 0 || seconds >= MaximumSeconds) throw new InvalidDataException("Setup time exhausted.");
        return seconds;
    }

    public static async Task<TowerMidpointMaterialization> Materialize(string root, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested(); using var lease = TowerCompactBundle.AcquireWriter(root);
        Scope(root); Gameplay();
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Materialization cannot fight.")).Activate();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(Math.Min(1800, MaximumSeconds - SetupElapsed(root)))); var ct = deadline.Token;
        if (Directory.Exists(Path.Combine(root, "materialized")) || File.Exists(Path.Combine(root, "protocol.json")))
            throw new InvalidDataException("No materialization retry or overwrite.");
        var source = HarnessJson.Read<JsonElement>(Path.Combine(root, "scope.json")).GetProperty("sourceRoot").GetString()!;
        if (HarnessJson.FileHash(Path.Combine(source, "screen-files.json")) != SourceManifest
            || HarnessJson.FileHash(Path.Combine(source, "seed-ledger.json")) != SourceLedger)
            throw new InvalidDataException("Changed completed source study.");
        TowerBulkCampaign.VerifyFiles(source, "screen-files.json", true, ct);
        var captured = TowerBalanceEvaluator.Read(Path.Combine(source, "scale-100-definition.json")); TowerBalanceEvaluator.Validate(captured);
        var reviewed = HarnessJson.Read<TowerCeilingPreparationReceipt>(Path.Combine(source, "preparation-receipt.json")).Variants[0];
        if (captured.Cells.Count != Recipes || captured.Cells.Count(c => c.Role == "reference") != 112
            || captured.Cells.Count(c => c.Role == "generated") != 141) throw new InvalidDataException("Complete source family required.");
        var target = Path.Combine(root, "materialized"); Directory.CreateDirectory(target);
        File.Copy(Path.Combine(source, "scale-100-definition.json"), Path.Combine(target, "source-definition.json"));
        File.Copy(Path.Combine(source, "seed-ledger.json"), Path.Combine(target, "source-seed-ledger.json"));
        File.Copy(Path.Combine(source, "screen-files.json"), Path.Combine(target, "source-files.json"));
        var baselineContent = Path.Combine(source, "scale-100-input"); var content = Path.Combine(target, "content");
        TowerBundle.CopyContent(baselineContent, content, ct);
        File.Copy(Path.Combine(baselineContent, "appsettings.json"), Path.Combine(content, "appsettings.json"));
        var floor = Path.Combine(content, "Data", TowerBattleRunner.FloorFile);
        File.WriteAllText(floor, Content(JsonNode.Parse(File.ReadAllText(floor))!).ToJsonString());
        var hashes = TowerCompactBundle.ContentHashes(content, ct); var settings = TowerBundle.ReadSettings(content);
        var template = captured with { Id = Version, ContentHashes = hashes, ExecutionHash = HarnessJson.Hash(ExecutionIdentity.Current()),
            MaximumBattles = MaximumFights, Cells = captured.Cells.Select(c => c with { MinimumSamples = Samples, Scenario = c.Scenario with { Seeds = [] } }).ToArray() };
        HarnessJson.WriteNew(Path.Combine(target, "template.json"), template);
        var baselineRunner = new TowerBattleRunner(baselineContent, new OfflineContent(baselineContent, settings.Threat));
        var runner = new TowerBattleRunner(content, new OfflineContent(content, settings.Threat));
        var cells = new List<TowerCeilingPreparedCell>(); var probe = captured.Cells[0].Scenario.Seeds[0];
        foreach (var cell in captured.Cells)
        {
            ct.ThrowIfCancellationRequested();
            var input = runner.CreateInput(cell.Scenario, probe, settings.Threat, settings.CheckpointIntervalTicks);
            var baselineInput = baselineRunner.CreateInput(cell.Scenario, probe, settings.Threat, settings.CheckpointIntervalTicks);
            var baseline = IdleBattleRunner.DescribeParticipants(await baselineRunner.PrepareAsync(baselineInput, ct));
            if (HarnessJson.Hash(baseline) != reviewed.Cells.Single(c => c.Id == cell.Id).ParticipantsHash)
                throw new InvalidDataException("Baseline roster differs from reviewed captured source.");
            var participants = IdleBattleRunner.DescribeParticipants(await runner.PrepareAsync(input, ct)); VerifyParticipants(baseline, participants);
            HarnessJson.WriteNew(Path.Combine(target, cell.Id + ".json"), new { cell.Id, input, participants });
            cells.Add(new(cell.Id, TowerBossDiscovery.RecipeHash(cell.Scenario.Party), HarnessJson.Hash(input), HarnessJson.Hash(participants)));
        }
        var result = new TowerMidpointMaterialization("MaterializedSeedBindingPending", source, SourceManifest, HarnessHash,
            new("scale-110", Factor, HarnessJson.Hash(hashes), cells), probe);
        HarnessJson.WriteNew(Path.Combine(target, "materialization.json"), result);
        HarnessJson.WriteNew(Path.Combine(target, "materialized-files.json"), Inventory(target));
        if (TowerBulkCampaign.StorageBytes(root, ct) > MaximumBytes) throw new InvalidDataException("Setup storage exhausted.");
        ct.ThrowIfCancellationRequested(); return result;
    }

    internal static Dictionary<string, string> Inventory(string root) => TowerBulkCampaign.Paths(root)
        .Where(p => Path.GetRelativePath(root, p) != FinalFiles && Path.GetRelativePath(root, p) != FinalFiles + ".pending")
        .ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'), HarnessJson.FileHash, StringComparer.Ordinal);

    internal static TowerBalanceDefinition Definition(TowerBalanceDefinition template, TowerMidpointSeeds seeds) => template with {
        ExcludedCombatSeeds = seeds.Historical, Cells = template.Cells.Select(c => c with { Scenario = c.Scenario with { Seeds = seeds.Shared } }).ToArray() };

    internal static void ValidateSeeds(TowerMidpointSeeds seeds, IReadOnlyList<int> required)
    {
        if (seeds.Version != Version || !seeds.Historical.SequenceEqual(required.Distinct().Order())
            || seeds.Historical.Count > TowerStudyLimits.HistoricalSeeds - Samples || seeds.Shared.Count != Samples
            || seeds.Shared.Distinct().Count() != Samples || seeds.Shared.Intersect(seeds.Historical).Any())
            throw new InvalidDataException("Complete history and exactly 256 fresh shared values required.");
        var used = seeds.Historical.ToHashSet(); var expected = new List<int>();
        for (var i = 0; expected.Count < Samples && i < 100000; i++)
        {
            var value = StableRandom.Seed(Version, "2026091422", "midpoint", i.ToString(CultureInfo.InvariantCulture));
            if (used.Add(value)) expected.Add(value);
        }
        if (!expected.SequenceEqual(seeds.Shared)) throw new InvalidDataException("Fresh accepted sequence differs.");
    }

    private static TowerBalanceDefinition BoundInputs(string root, bool live, CancellationToken ct)
    {
        Scope(root); Gameplay(); var m = Path.Combine(root, "materialized");
        TowerBulkCampaign.VerifyFiles(m, "materialized-files.json", true, ct);
        var materialized = HarnessJson.Read<TowerMidpointMaterialization>(Path.Combine(m, "materialization.json"));
        if (materialized.HarnessHash != HarnessHash || materialized.SourceManifestHash != SourceManifest
            || materialized.Variant.Factor != Factor || materialized.Variant.Cells.Count != Recipes)
            throw new InvalidDataException("Materialization scope differs.");
        if (live && HarnessJson.FileHash(Path.Combine(materialized.SourceRoot, "screen-files.json")) != SourceManifest)
            throw new InvalidDataException("Source seal differs.");
        var template = TowerBalanceEvaluator.Read(Path.Combine(m, "template.json"));
        var source = TowerBalanceEvaluator.Read(Path.Combine(m, "source-definition.json"));
        if (HarnessJson.FileHash(Path.Combine(m, "source-files.json")) != SourceManifest
            || HarnessJson.FileHash(Path.Combine(m, "source-seed-ledger.json")) != SourceLedger
            || HarnessJson.Read<Dictionary<string, string>>(Path.Combine(m, "source-files.json"))["scale-100-definition.json"] != HarnessJson.FileHash(Path.Combine(m, "source-definition.json"))
            || !source.Cells.Select(c => c.Id).SequenceEqual(materialized.Variant.Cells.Select(c => c.Id)))
            throw new InvalidDataException("Retained source definition or roster family differs.");
        var expected = source with { Id = Version, ContentHashes = TowerCompactBundle.ContentHashes(Path.Combine(m, "content"), ct),
            ExecutionHash = HarnessJson.Hash(ExecutionIdentity.Current()), MaximumBattles = MaximumFights,
            Cells = source.Cells.Select(c => c with { MinimumSamples = Samples, Scenario = c.Scenario with { Seeds = [] } }).ToArray() };
        TowerPortfolioConfirmation.Equal(expected, template, "unchanged complete midpoint template");
        var baseFloor = Path.Combine(materialized.SourceRoot, "scale-100-input", "Data", TowerBattleRunner.FloorFile);
        var newFloor = Path.Combine(m, "content", "Data", TowerBattleRunner.FloorFile);
        if (HarnessJson.FileHash(baseFloor) != source.ContentHashes[TowerBattleRunner.FloorFile]
            || materialized.Variant.ContentHash != HarnessJson.Hash(template.ContentHashes)
            || !JsonNode.DeepEquals(Content(JsonNode.Parse(File.ReadAllText(baseFloor))!), JsonNode.Parse(File.ReadAllText(newFloor))))
            throw new InvalidDataException("Content differs outside the two fixed midpoint scalars.");
        foreach (var item in source.ContentHashes.Where(p => p.Key != TowerBattleRunner.FloorFile))
            if (template.ContentHashes[item.Key] != item.Value) throw new InvalidDataException("Changed non-guardian content.");
        if (HarnessJson.Hash(TowerBundle.ReadSettings(Path.Combine(m, "content"))) != source.SettingsHash)
            throw new InvalidDataException("Changed combat settings.");
        var required = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(m, "source-seed-ledger.json"))).ToHashSet();
        var history = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(root, "history-files.json")); var ordinal = 0;
        if (history.Count == 0) throw new InvalidDataException("Registered history inventory is required.");
        foreach (var (path, digest) in history.OrderBy(v => v.Key, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested(); var copy = Path.Combine(root, "history", (ordinal++).ToString("D4", CultureInfo.InvariantCulture) + ".json");
            if (HarnessJson.FileHash(copy) != digest || live && HarnessJson.FileHash(path) != digest)
                throw new InvalidDataException("Registered history changed.");
            required.UnionWith(TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(copy)));
        }
        var seeds = HarnessJson.Read<TowerMidpointSeeds>(Path.Combine(root, "seed-ledger.json")); ValidateSeeds(seeds, required.Order().ToArray());
        var definition = Definition(template, seeds); TowerBalanceEvaluator.Validate(definition); return definition;
    }

    public static async Task<TowerMidpointProtocol> Bind(string root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); using var lease = TowerCompactBundle.AcquireWriter(root);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Binding cannot fight.")).Activate();
        if (File.Exists(Path.Combine(root, "protocol.json")) || File.Exists(Path.Combine(root, "started.json"))) throw new InvalidDataException("No bind retry.");
        HarnessJson.WriteNew(Path.Combine(root, "binding-started.json"), new { utc = DateTimeOffset.UtcNow });
        var d = BoundInputs(root, true, ct); var settings = TowerBundle.ReadSettings(Path.Combine(root, "materialized/content"));
        var runner = new TowerBattleRunner(Path.Combine(root, "materialized/content"), new OfflineContent(Path.Combine(root, "materialized/content"), settings.Threat));
        var materialized = HarnessJson.Read<TowerMidpointMaterialization>(Path.Combine(root, "materialized/materialization.json"));
        foreach (var cell in d.Cells)
        {
            ct.ThrowIfCancellationRequested(); var input = runner.CreateInput(cell.Scenario, cell.Scenario.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks);
            if (HarnessJson.Hash(IdleBattleRunner.DescribeParticipants(await runner.PrepareAsync(input, ct))) != materialized.Variant.Cells.Single(c => c.Id == cell.Id).ParticipantsHash)
                throw new InvalidDataException("Seed binding changed prepared participants.");
        }
        HarnessJson.WriteNew(Path.Combine(root, "definition.json"), d);
        var p = new TowerMidpointProtocol(Version, HarnessHash, MaximumFights, MaximumSeconds, MaximumBytes, 0, SetupElapsed(root), Inventory(root));
        HarnessJson.WriteNew(Path.Combine(root, "protocol.json"), p);
        HarnessJson.WriteNew(Path.Combine(root, "setup-charge.json"), new { seconds = SetupElapsed(root) });
        return Check(root, ct);
    }

    internal static TowerMidpointProtocol Inputs(string root, bool live, CancellationToken ct)
    {
        var p = HarnessJson.Read<TowerMidpointProtocol>(Path.Combine(root, "protocol.json"));
        if (p.Version != Version || p.HarnessHash != HarnessHash || p.MaximumFights != MaximumFights || p.MaximumSeconds != MaximumSeconds
            || p.MaximumBytes != MaximumBytes || p.Retries != 0 || !double.IsFinite(p.SetupSeconds) || p.SetupSeconds < 0 || p.SetupSeconds >= MaximumSeconds)
            throw new InvalidDataException("Changed fixed midpoint execution envelope.");
        TowerPortfolioConfirmationRun.VerifyFrozen(root, p.FrozenFiles, ct);
        TowerPortfolioConfirmation.Equal(BoundInputs(root, live, ct), TowerBalanceEvaluator.Read(Path.Combine(root, "definition.json")), "bound midpoint definition");
        return p;
    }

    internal static double Setup(string root)
    {
        var value = HarnessJson.Read<JsonElement>(Path.Combine(root, "setup-charge.json")).GetProperty("seconds").GetDouble();
        var minimum = HarnessJson.Read<TowerMidpointProtocol>(Path.Combine(root, "protocol.json")).SetupSeconds;
        if (!double.IsFinite(value) || value < minimum || value >= MaximumSeconds) throw new InvalidDataException("Invalid setup charge.");
        return value;
    }

    public static TowerMidpointProtocol Check(string root, CancellationToken ct = default)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Readiness cannot fight.")).Activate();
        if (File.Exists(Path.Combine(root, "started.json")) || File.Exists(Path.Combine(root, FinalFiles))) throw new InvalidDataException("No resume or retry.");
        var p = Inputs(root, true, ct); _ = Setup(root);
        if (!p.FrozenFiles.Keys.Concat(new[] { "protocol.json", "setup-charge.json" }).ToHashSet(StringComparer.Ordinal)
                .SetEquals(TowerBulkCampaign.Paths(root).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')))
            || TowerBulkCampaign.StorageBytes(root, ct) > p.MaximumBytes) throw new InvalidDataException("Prepared inventory or storage differs.");
        return p;
    }

    internal static TowerMidpointResult Reconstruct(TowerBalanceDefinition d, IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        if (d.Id != Version || d.SchemaVersion != 1 || d.MaximumBattles != MaximumFights || d.Cells.Count != Recipes
            || d.Cohorts.Count != 1 || d.Cells.Count(c => c.Role == "reference") != 112 || d.Cells.Count(c => c.Role == "generated") != 141
            || !d.Cells.Select(c => c.Id).SequenceEqual(d.Cells.Select(c => c.Id).Order(StringComparer.Ordinal))
            || d.Cells.Any(c => c.MinimumSamples != Samples || c.Scenario.Seeds.Count != Samples)) throw new InvalidDataException("Incomplete midpoint family.");
        var report = TowerBalanceEvaluator.Evaluate(d, evidence);
        if (report.Issues.Count != 0 || report.Cells.Any(c => c.Issues.Count != 0 || c.Valid != Samples)) throw new InvalidDataException("Invalid midpoint evidence.");
        var cells = report.Cells.Select(c => new TowerCeilingCellResult("scale-110", c.Id, c.Wins, c.Defeats, c.Draws, Interval(c.Wins))).ToArray();
        var maximum = cells.Max(c => c.Wins);
        var qualifies = maximum / (double)Samples is >= .15 and <= .40 && cells.All(c => c.Interval.Upper <= .5) && cells.Any(c => c.Interval.Lower >= .1);
        return new(qualifies ? "CandidateForFullFamilyConfirmation" : "Unresolved", Factor, maximum, cells);
    }
}
