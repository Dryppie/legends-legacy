using System.Globalization;
using System.Text;
using System.Text.Json;
using Domain.Models.Combat;
using Services.LL.WorldTower;
using Services.LL.Combat.Engine;

namespace BalanceHarness;

public sealed record TowerManifest(int SchemaVersion, string InputHash,
    IReadOnlyDictionary<string, string> ContentHashes, ExecutionIdentity Execution);
public sealed record TowerTrial(string Id, int Seed, TowerBattleReport Report);
public sealed record TowerScorecard(string Status, int Planned, int Valid, int Invalid, int Cancelled, int NotRun,
    int Wins, int Defeats, int Draws, int TickLimits, RateEstimate? ClearRate,
    NumericDistribution WinDurationSeconds, NumericDistribution NonWinDurationSeconds, IReadOnlyList<TowerTrial> Trials);
public sealed record TowerSettings(ThreatAndTankingOptions Threat, int CheckpointIntervalTicks);
public sealed record SavedTower(TowerManifest Manifest, IReadOnlyList<TowerBattleInput> Inputs, TowerScorecard Scorecard);

/// <summary>Separate versioned Tower envelope; historical idle contracts are unchanged.</summary>
public static class TowerBundle
{
    internal static IReadOnlyList<string> Files => [.. OfflineContent.Files, TowerBattleRunner.FloorFile];

    public static async Task<TowerScorecard> CreateAsync(string apiRoot, string scenarioPath, string output,
        CancellationToken token = default, Action<string>? progress = null, TowerSettings? settingsOverride = null)
    {
        if (Path.Exists(output)) throw new IOException("Output already exists; choose a new Tower run directory.");
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        var trials = new List<TowerTrial>();
        var planned = 0;
        var invalid = 0;
        var cancelled = 0;
        try
        {
            var snapshotRoot = Path.Combine(output, "content");
            var hashes = CopyContent(apiRoot, snapshotRoot, token);
            var settings = settingsOverride ?? ReadSettings(apiRoot);
            var threat = settings.Threat;
            var interval = settings.CheckpointIntervalTicks;
            var scenario = HarnessJson.Read<TowerScenario>(scenarioPath);
            planned = scenario.Seeds.Count;
            var runner = new TowerBattleRunner(snapshotRoot, new OfflineContent(snapshotRoot, threat));
            var inputs = scenario.Seeds.Select(seed => runner.CreateInput(scenario, seed, threat, interval)).ToArray();
            // Complete validation and freeze the whole schedule before the first battle.
            HarnessJson.WriteNew(Path.Combine(output, "tower-input.json"), inputs);
            HarnessJson.WriteNew(Path.Combine(output, "tower-manifest.json"), new TowerManifest(1,
                HarnessJson.Hash(inputs), hashes, ExecutionIdentity.Current()));
            Directory.CreateDirectory(Path.Combine(output, "battles"));
            foreach (var input in inputs)
            {
                token.ThrowIfCancellationRequested();
                var id = BattleId(trials.Count);
                var report = await runner.RunAsync(input, token: token);
                HarnessJson.WriteNew(Path.Combine(output, "battles", id + ".json"), report);
                trials.Add(new(id, input.Rules.RandomSeed, report));
                progress?.Invoke($"Tower: {trials.Count}/{inputs.Length} battles processed.");
            }
        }
        catch (Exception error)
        {
            if (error is OperationCanceledException) cancelled = 1; else invalid = 1;
            HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new
                { Status = cancelled > 0 ? "Cancelled" : "Invalid", ErrorType = error.GetType().Name, error.Message });
            throw;
        }
        finally
        {
            var scorecard = Summarize(planned, trials, invalid, cancelled);
            HarnessJson.WriteNew(Path.Combine(output, "scorecard.json"), scorecard);
            File.WriteAllText(Path.Combine(output, "scorecard.md"), Markdown(scorecard));
            HarnessJson.WriteNew(Path.Combine(output, "tower-results.json"), trials.ToDictionary(t => t.Id,
                t => HarnessJson.FileHash(Path.Combine(output, "battles", t.Id + ".json"))));
        }
        return Summarize(planned, trials, invalid, cancelled);
    }

    internal static TowerSettings ReadSettings(string apiRoot)
    {
        var (threat, _) = RunBundle.ReadCombatSettings(apiRoot);
        using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(apiRoot, "appsettings.json")),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var interval = settings.RootElement.TryGetProperty("WorldTower", out var tower)
            && tower.TryGetProperty("CombatTicksPerFrame", out var value) ? value.GetInt32() : new WorldTowerOptions().CombatTicksPerFrame;
        return new(threat, interval);
    }

    internal static IReadOnlyDictionary<string, string> CopyContent(string apiRoot, string snapshotRoot, CancellationToken token)
    {
        var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Files)
        {
            token.ThrowIfCancellationRequested();
            var destination = Path.Combine(snapshotRoot, "Data", file);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(Path.Combine(apiRoot, "Data", file), destination, false);
            hashes.Add(file, HarnessJson.FileHash(destination));
        }
        return hashes;
    }

    internal static void VerifySnapshot(string run, TowerManifest manifest, string inputHash, CancellationToken token)
    {
        if (manifest.SchemaVersion != 1 || inputHash != manifest.InputHash
            || !manifest.ContentHashes.Keys.Order(StringComparer.Ordinal).SequenceEqual(Files.Order(StringComparer.Ordinal)))
            throw new InvalidDataException("Unsupported or modified Tower inputs/content manifest.");
        foreach (var file in Files)
        {
            token.ThrowIfCancellationRequested();
            if (HarnessJson.FileHash(Path.Combine(run, "content", "Data", file)) != manifest.ContentHashes[file])
                throw new InvalidDataException($"Modified Tower content snapshot: {file}");
        }
    }

    // Reading comparisons across builds must not execute the historical build or regenerate its stats.
    public static SavedTower ReadSaved(string run, CancellationToken token = default)
    {
        var inputs = HarnessJson.Read<TowerBattleInput[]>(Path.Combine(run, "tower-input.json"));
        var manifest = HarnessJson.Read<TowerManifest>(Path.Combine(run, "tower-manifest.json"));
        VerifySnapshot(run, manifest, HarnessJson.Hash(inputs), token);
        if (inputs.Length is < 1 or > 1000 || !inputs.Select(i => i.Rules.RandomSeed).SequenceEqual(inputs[0].Scenario.Seeds)
            || inputs.Any(i => i.SchemaVersion != 1 || HarnessJson.Hash(i.Scenario) != HarnessJson.Hash(inputs[0].Scenario)))
            throw new InvalidDataException("Invalid saved Tower schedule.");
        var score = HarnessJson.Read<TowerScorecard>(Path.Combine(run, "scorecard.json"));
        var hashes = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(run, "tower-results.json"));
        if (hashes.Count > inputs.Length || !hashes.Keys.Order().SequenceEqual(Enumerable.Range(0, hashes.Count).Select(BattleId)))
            throw new InvalidDataException("Invalid saved Tower result inventory.");
        var trials = new List<TowerTrial>();
        for (var index = 0; index < hashes.Count; index++)
        {
            token.ThrowIfCancellationRequested();
            var id = BattleId(index);
            var path = Path.Combine(run, "battles", id + ".json");
            if (!File.Exists(path) || HarnessJson.FileHash(path) != hashes[id])
                throw new InvalidDataException("Missing or modified saved Tower result.");
            var report = HarnessJson.Read<TowerBattleReport>(path);
            if (report.Battle.ScenarioId != inputs[index].Scenario.Id || report.Battle.Seed != inputs[index].Rules.RandomSeed
                || report.Succeeded != (report.Battle.Summary.ContentOutcome == BattleOutcome.Victory))
                throw new InvalidDataException("Saved Tower result has inconsistent identity/outcome.");
            trials.Add(new(id, inputs[index].Rules.RandomSeed, report));
        }
        if (score.Invalid is < 0 or > 1 || score.Cancelled is < 0 or > 1 || score.Invalid + score.Cancelled > 1
            || HarnessJson.Hash(score) != HarnessJson.Hash(Summarize(inputs.Length, trials, score.Invalid, score.Cancelled)))
            throw new InvalidDataException("Saved Tower scorecard does not match verified results.");
        return new(manifest, inputs, score);
    }

    public static async Task<TowerBattleReport> ReplayAsync(string run, string battleId, bool detailed,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var inputs = HarnessJson.Read<TowerBattleInput[]>(Path.Combine(run, "tower-input.json"));
        var manifest = HarnessJson.Read<TowerManifest>(Path.Combine(run, "tower-manifest.json"));
        if (manifest.SchemaVersion != 1 || HarnessJson.Hash(inputs) != manifest.InputHash
            || inputs.Length == 0 || !manifest.ContentHashes.Keys.Order(StringComparer.Ordinal).SequenceEqual(Files.Order(StringComparer.Ordinal)))
            throw new InvalidDataException("Unsupported or modified Tower inputs/content manifest.");
        foreach (var file in Files)
        {
            token.ThrowIfCancellationRequested();
            if (HarnessJson.FileHash(Path.Combine(run, "content", "Data", file)) != manifest.ContentHashes[file])
                throw new InvalidDataException($"Modified Tower content snapshot: {file}");
        }
        if (HarnessJson.Hash(ExecutionIdentity.Current()) != HarnessJson.Hash(manifest.Execution))
            throw new InvalidDataException("Tower replay requires the original assemblies, runtime and platform.");
        // Derive paths only from our schedule, never from user or manifest path fragments.
        var index = Enumerable.Range(0, inputs.Length).SingleOrDefault(i => BattleId(i) == battleId, -1);
        if (index < 0) throw new InvalidDataException("Unknown Tower battle ID.");
        var resultPath = Path.Combine(run, "battles", BattleId(index) + ".json");
        var results = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(run, "tower-results.json"));
        if (!results.TryGetValue(battleId, out var hash) || !File.Exists(resultPath) || HarnessJson.FileHash(resultPath) != hash)
            throw new InvalidDataException("Missing or modified saved Tower result.");
        var input = inputs[index];
        var content = new OfflineContent(Path.Combine(run, "content"), input.ThreatAndTanking);
        var report = await new TowerBattleRunner(Path.Combine(run, "content"), content).RunAsync(input, detailed, token);
        var original = HarnessJson.Read<TowerBattleReport>(resultPath);
        RunBundle.VerifyResult(original.Battle, report.Battle);
        if (original.Succeeded != report.Succeeded || original.GuardianHealthRemainingPercent != report.GuardianHealthRemainingPercent
            || original.DisplayDurationSeconds != report.DisplayDurationSeconds)
            throw new InvalidDataException("Replay diverged from the saved Tower outcome.");
        return report;
    }

    private static string BattleId(int index) => $"tower.{(index + 1).ToString("D4", CultureInfo.InvariantCulture)}";
    private static TowerScorecard Summarize(int planned, IReadOnlyList<TowerTrial> trials, int invalid, int cancelled)
    {
        var wins = trials.Count(t => t.Report.Succeeded);
        return new(invalid > 0 ? "Invalid" : cancelled > 0 ? "Cancelled" : trials.Count == planned && planned > 0 ? "Complete" : "Incomplete",
            planned, trials.Count, invalid, cancelled, Math.Max(0, planned - trials.Count - invalid - cancelled), wins,
            trials.Count(t => t.Report.Battle.Summary.ContentOutcome == BattleOutcome.Defeat),
            trials.Count(t => t.Report.Battle.Summary.ContentOutcome == BattleOutcome.Draw),
            trials.Count(t => t.Report.Battle.Summary.TerminationReason == "TickLimit"),
            SuiteScorecard.Wilson(wins, trials.Count),
            SuiteScorecard.Distribution(trials.Where(t => t.Report.Succeeded).Select(t => t.Report.Battle.Summary.DurationSeconds)),
            SuiteScorecard.Distribution(trials.Where(t => !t.Report.Succeeded).Select(t => t.Report.Battle.Summary.DurationSeconds)), trials);
    }

    private static string Markdown(TowerScorecard score)
    {
        var text = new StringBuilder("# Tower floor report\n\n");
        text.AppendLine($"Status: **{score.Status}**. Planned {score.Planned}; valid {score.Valid}; invalid {score.Invalid}; cancelled {score.Cancelled}; not run {score.NotRun}.\n");
        text.AppendLine($"Wins: **{score.Wins}**; defeats: **{score.Defeats}**; draws: **{score.Draws}**; tick limits: **{score.TickLimits}**.\n");
        if (score.ClearRate is { } rate)
            text.AppendLine(FormattableString.Invariant($"Clear rate: **{rate.Rate * 100:F2}%**; pointwise 95% Wilson interval **[{rate.Lower * 100:F2}%, {rate.Upper * 100:F2}%]**.\n"));
        text.AppendLine("Descriptive only. No Tower win-rate or duration target is approved; the starter 50–90% band does not apply. JSON includes a pointwise 95% Wilson clear-rate interval. One fixed floor and party do not measure the Tower difficulty curve or progression journey.\n");
        text.AppendLine("The frozen floor, materialized party, ownership assumptions, no-contribution starting state, rules and declared seeds are in `tower-input.json`. Content and execution hashes are in `tower-manifest.json`.\n");
        text.AppendLine("| Battle | Seed | Engine / Tower outcome | Seconds | Guardian health % |\n| --- | ---: | --- | ---: | ---: |");
        foreach (var trial in score.Trials)
            text.AppendLine(FormattableString.Invariant($"| {trial.Id} | {trial.Seed} | {trial.Report.Battle.Summary.EngineOutcome} / {trial.Report.Battle.Summary.ContentOutcome} | {trial.Report.Battle.Summary.DurationSeconds:F1} | {trial.Report.GuardianHealthRemainingPercent:F2} |"));
        return text.ToString();
    }
}
