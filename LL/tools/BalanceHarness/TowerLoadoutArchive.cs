using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO.Compression;

namespace BalanceHarness;

public sealed record LoadoutTrial(string Id, string Stage, string Recipe, int Seed, string InputHash, string CacheKey);
public sealed record LoadoutScope(string Algorithm, TowerSettings Settings, ExecutionIdentity Execution,
    IReadOnlyDictionary<string, string> ContentHashes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ReportStorage = null);

internal sealed record VerifiedLoadoutInventory(string Hash, IReadOnlyList<LoadoutTrial> Trials);

/// <summary>Shared frozen content and exact trial cache. Arm namespaces deliberately charge both methods equally.</summary>
public sealed class TowerLoadoutArchive(string output, LoadoutScope scope, int maximumBattles)
{
    private readonly Dictionary<string, LoadoutTrial> cache = new(StringComparer.Ordinal);
    private readonly List<LoadoutTrial> trials = [];
    private readonly JsonSerializerOptions compact = new(HarnessJson.Options) { WriteIndented = false };
    private readonly TowerBattleRunner runner = new(Path.Combine(output, "content"),
        new OfflineContent(Path.Combine(output, "content"), scope.Settings.Threat));
    public IReadOnlyList<LoadoutTrial> Trials => trials;
    public int CacheHits { get; private set; }

    public static string Key(LoadoutScope scope, string arm, TowerBattleInput input) => HarnessJson.Hash(new { scope, arm, input });

    public async Task<(LoadoutTrial Trial, TowerBattleReport Report)> EvaluateAsync(string arm, string stage,
        TowerScenario scenario, int seed, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var input = runner.CreateInput(scenario, seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
        var key = Key(scope, arm, input);
        if (cache.TryGetValue(key, out var previous))
        {
            CacheHits++;
            return (previous, ReadBattle(output, previous.Id, scope.ReportStorage));
        }
        if (trials.Count >= maximumBattles) throw new InvalidDataException("Hard combat budget exhausted.");
        var recipe = HarnessJson.Hash(scenario);
        var recipePath = Path.Combine(output, "recipes", recipe + ".json");
        if (!File.Exists(recipePath)) HarnessJson.WriteNew(recipePath, scenario);
        var trial = new LoadoutTrial($"trial-{trials.Count + 1:D6}", stage, recipe, seed, HarnessJson.Hash(input), key);
        var report = await runner.RunAsync(input, token: token);
        WriteBattle(output, trial.Id, report, scope.ReportStorage);
        File.AppendAllText(Path.Combine(output, "trials.jsonl"), JsonSerializer.Serialize(trial, compact) + "\n");
        trials.Add(trial); cache.Add(key, trial);
        return (trial, report);
    }

    private static string BattlePath(string output, string id, string? storage)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(id, "^trial-[0-9]{6}$")) throw new InvalidDataException("Invalid trial ID.");
        if (storage is not (null or "gzip-json-v1")) throw new InvalidDataException("Unknown battle storage version.");
        return Path.Combine(output, "battles", id + (storage is null ? ".json" : ".json.gz"));
    }

    public static void WriteBattle(string output, string id, TowerBattleReport report, string? storage)
    {
        var path = BattlePath(output, id, storage);
        if (storage is null) { HarnessJson.WriteNew(path, report); return; }
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        using var gzip = new GZipStream(file, CompressionLevel.Fastest);
        JsonSerializer.Serialize(gzip, report, new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false });
    }

    public static TowerBattleReport ReadBattle(string output, string id, string? storage)
    {
        var path = BattlePath(output, id, storage);
        if (storage is null) return HarnessJson.Read<TowerBattleReport>(path);
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        return JsonSerializer.Deserialize<TowerBattleReport>(gzip, HarnessJson.Options) ?? throw new InvalidDataException("Empty compressed battle.");
    }

    public static IReadOnlyList<LoadoutTrial> Verify(string output, CancellationToken token = default)
        => VerifyInventory(output, token).Trials;

    internal static VerifiedLoadoutInventory VerifyInventory(string output, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(output, "files.json"));
        var actual = Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(output, p).Replace('\\', '/')).Where(p => p != "files.json").Order(StringComparer.Ordinal);
        if (!actual.SequenceEqual(files.Keys.Order(StringComparer.Ordinal))) throw new InvalidDataException("Modified archive inventory.");
        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();
            if (HarnessJson.FileHash(Path.Combine(output, file.Key)) != file.Value) throw new InvalidDataException($"Modified archive file: {file.Key}");
        }
        var trials = File.Exists(Path.Combine(output, "trials.jsonl"))
            ? File.ReadLines(Path.Combine(output, "trials.jsonl")).Select(s => JsonSerializer.Deserialize<LoadoutTrial>(s, HarnessJson.Options)!).ToArray() : [];
        if (!trials.Select(t => t.Id).SequenceEqual(Enumerable.Range(1, trials.Length).Select(i => $"trial-{i:D6}")))
            throw new InvalidDataException("Invalid trial ledger.");
        token.ThrowIfCancellationRequested();
        return new(HarnessJson.Hash(files), trials);
    }

    public static async Task<TowerBattleReport> ReplayAsync(string output, string trialId, bool detailed, CancellationToken token = default)
    {
        var trial = Verify(output, token).SingleOrDefault(t => t.Id == trialId) ?? throw new InvalidDataException("Unknown trial.");
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(output, "scope.json"));
        if (HarnessJson.Hash(scope.Execution) != HarnessJson.Hash(ExecutionIdentity.Current()))
            throw new InvalidDataException("Replay requires the original assemblies, runtime and platform.");
        var recipe = HarnessJson.Read<TowerScenario>(Path.Combine(output, "recipes", trial.Recipe + ".json"));
        var root = Path.Combine(output, "content");
        var runner = new TowerBattleRunner(root, new OfflineContent(root, scope.Settings.Threat));
        var input = runner.CreateInput(recipe, trial.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
        if (HarnessJson.Hash(recipe) != trial.Recipe || HarnessJson.Hash(input) != trial.InputHash)
            throw new InvalidDataException("Reconstructed trial differs from its saved input.");
        var result = await runner.RunAsync(input, detailed, token);
        var saved = ReadBattle(output, trial.Id, scope.ReportStorage);
        RunBundle.VerifyResult(saved.Battle, result.Battle);
        if (saved.Succeeded != result.Succeeded || saved.GuardianHealthRemainingPercent != result.GuardianHealthRemainingPercent
            || saved.DisplayDurationSeconds != result.DisplayDurationSeconds) throw new InvalidDataException("Replay outcome changed.");
        return result;
    }
}
