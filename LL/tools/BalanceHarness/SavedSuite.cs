using System.Text.Json;
using System.Text.RegularExpressions;

namespace BalanceHarness;

/// <summary>Checks archived evidence without loading current combat content or executing gameplay.</summary>
public sealed record SavedSuite(string Directory, SuiteRunInput Input, RunManifest Manifest,
    SuiteReport Scorecard, IReadOnlyDictionary<string, BattleObservation> Observations,
    IReadOnlyDictionary<string, string> GameplayHashes, IReadOnlyDictionary<string, int> TickRates,
    string ArtifactHash)
{
    public const string MetricsVersion = "idle-scorecard-v1";

    public static SavedSuite Read(string directory, CancellationToken cancellationToken = default)
    {
        directory = Path.GetFullPath(directory);
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(Path.Combine(directory, "failure.json")))
            throw new InvalidDataException("Run has a bundle failure; it cannot supply verified comparison evidence.");
        var input = HarnessJson.Read<SuiteRunInput>(Path.Combine(directory, "suite-input.json"));
        var manifest = HarnessJson.Read<RunManifest>(Path.Combine(directory, "manifest.json"));
        RunBundle.VerifySnapshot(directory, manifest, HarnessJson.Hash(input), cancellationToken);
        if (input.SchemaVersion != 1 || input.Definition.SchemaVersion != 1
            || input.SeedScheduleVersion != IdleSuite.SeedScheduleVersion
            || input.Cells.Count is < 1 or > 1000 || input.Cells.Sum(c => (long)c.Trials.Count) > 100000
            || input.Cells.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != input.Cells.Count)
            throw new InvalidDataException("Unsupported or invalid saved suite.");
        foreach (var cell in input.Cells)
        {
            if (!Regex.IsMatch(cell.Id, "^[a-z0-9][a-z0-9.-]{0,193}$", RegexOptions.CultureInvariant)
                || cell.Input.SchemaVersion != 1 || cell.Input.Scenario.SchemaVersion != 1
                || cell.Input.Scenario.Id != cell.Id || cell.Trials.Count is < 1 or > 10000
                || cell.Input.Area.GetProperty("id").GetString() != cell.Input.Scenario.AreaId
                || cell.Input.Creature.GetProperty("id").GetGuid() != cell.Input.Scenario.CreatureId
                || cell.Trials.Select(t => t.Index).Distinct().Count() != cell.Trials.Count
                || cell.Trials.Select(t => t.Seed).Distinct().Count() != cell.Trials.Count
                || cell.Trials.Any(t => t.Index < 0 || t.Index >= cell.Trials.Count
                    || t.BattleId != FormattableString.Invariant($"{cell.Id}.{t.Index + 1:D4}")))
                throw new InvalidDataException($"Invalid cell identity or trial schedule: {cell.Id}.");
        }
        var scorecard = HarnessJson.Read<SuiteReport>(Path.Combine(directory, "scorecard.json"));
        if (scorecard.SchemaVersion != 1 || scorecard.Policy != "Advisory"
            || scorecard.Status is not ("Complete" or "Invalid" or "Cancelled")
            || !double.IsFinite(scorecard.ElapsedSeconds) || scorecard.ElapsedSeconds < 0)
            throw new InvalidDataException("Unsupported scorecard schema, policy or status.");
        var observations = new List<BattleObservation>();
        foreach (var line in File.ReadLines(Path.Combine(directory, "battles.jsonl")))
        {
            cancellationToken.ThrowIfCancellationRequested();
            observations.Add(JsonSerializer.Deserialize<BattleObservation>(line, HarnessJson.Options)
                ?? throw new InvalidDataException("Empty battle observation."));
            if (observations.Count > 100000) throw new InvalidDataException("Too many battle observations.");
        }
        var recalculated = SuiteScorecard.Create(input, observations, scorecard.Status == "Cancelled", scorecard.ElapsedSeconds);
        if (HarnessJson.Hash(recalculated) != HarnessJson.Hash(scorecard))
            throw new InvalidDataException("Saved scorecard does not match its battle observations.");

        var planned = input.Cells.SelectMany(c => c.Trials.Select(t => (Cell: c, Trial: t)))
            .ToDictionary(x => x.Trial.BattleId, StringComparer.Ordinal);
        var gameplayHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        var tickRates = new Dictionary<string, int>(StringComparer.Ordinal);
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in new[] { "suite-input.json", "manifest.json", "scorecard.json", "scorecard.md", "battles.jsonl" })
            files.Add(name, HarnessJson.FileHash(Path.Combine(directory, name)));
        foreach (var observation in observations.Where(o => o.Status == "Completed"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (cell, trial) = planned[observation.BattleId];
            var name = "battles/" + trial.BattleId + ".json"; // Validated identifiers, never arbitrary paths.
            var battle = HarnessJson.Read<BattleReport>(Path.Combine(directory, name));
            if (HarnessJson.Hash(BattleObservation.FromResult(cell, trial, battle)) != HarnessJson.Hash(observation))
                throw new InvalidDataException($"Battle record does not match the observation: {trial.BattleId}.");
            if (tickRates.TryGetValue(cell.Id, out var rate) && rate != battle.TicksPerSecond)
                throw new InvalidDataException($"Inconsistent tick units within {cell.Id}.");
            tickRates[cell.Id] = battle.TicksPerSecond;
            gameplayHashes.Add(trial.BattleId, HarnessJson.Hash(new
                { battle.PreparedParticipants, battle.Summary, battle.TicksPerSecond }));
            files.Add(name, HarnessJson.FileHash(Path.Combine(directory, name)));
        }
        return new(directory, input, manifest, scorecard,
            observations.ToDictionary(o => o.BattleId, StringComparer.Ordinal), gameplayHashes, tickRates,
            HarnessJson.Hash(files));
    }
}
