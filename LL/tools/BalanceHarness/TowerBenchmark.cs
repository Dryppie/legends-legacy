using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Common.Randomness;
using Services.LL.PowerRatings;
using Services.LL.WorldTower;

namespace BalanceHarness;

public sealed record TowerBenchmarkProfile(string Id, string Assumptions, EquipmentReferenceBuildDefinition Build);
public sealed record TowerBenchmarkParty(string Id, string Assumptions, IReadOnlyList<string> CellProfiles,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<int, IReadOnlyList<string>>? FloorCellProfiles = null);
public sealed record TowerBenchmarkDefinition(int SchemaVersion, string Id, DateTimeOffset StartsAt,
    int SamplesPerCell, IReadOnlyList<int> Floors, IReadOnlyList<TowerBenchmarkProfile> Profiles,
    IReadOnlyList<TowerBenchmarkParty> Parties);
public sealed record TowerBenchmarkInput(int SchemaVersion, TowerBenchmarkDefinition Definition, int MasterSeed,
    TowerSettings Settings, IReadOnlyList<TowerScenario> Scenarios);
public sealed record TowerBenchmarkCell(string Id, int Floor, string Party, string Status, int Valid,
    int Wins, int Defeats, int Draws, RateEstimate? ClearRate, NumericDistribution SurvivalPercent,
    NumericDistribution DurationSeconds, NumericDistribution GuardianHealthPercent);
public sealed record TowerBenchmarkReport(int SchemaVersion, string Status, int PlannedBattles, int ValidBattles,
    IReadOnlyList<TowerBenchmarkCell> Cells);
public sealed record SavedTowerBenchmark(string Directory, TowerBenchmarkInput Input, TowerManifest Manifest,
    TowerBenchmarkReport Report, IReadOnlyDictionary<string, SavedTower> Cells);

/// <summary>Finite, recipe-driven parties; no optimization or automatic balance acceptance.</summary>
public static class TowerBenchmark
{
    public static IReadOnlyList<TowerScenario> Expand(TowerBenchmarkDefinition definition, string contentRoot,
        int masterSeed, int? samples = null)
    {
        var count = samples ?? definition.SamplesPerCell;
        if (definition.SchemaVersion is not (1 or 2) || !SafeId(definition.Id) || count is < 1 or > 1000
            || definition.Floors.Count is < 1 or > 20 || definition.Parties.Count is < 1 or > 20
            || definition.Profiles.Count is < 1 or > 100
            || (long)count * definition.Floors.Count * definition.Parties.Count > 10000
            || definition.Floors.Distinct().Count() != definition.Floors.Count
            || definition.Profiles.Any(p => !SafeId(p.Id) || string.IsNullOrWhiteSpace(p.Assumptions))
            || definition.Profiles.Select(p => p.Id).Distinct().Count() != definition.Profiles.Count
            || definition.Parties.Select(p => p.Id).Distinct().Count() != definition.Parties.Count)
            throw new InvalidDataException("Invalid Tower benchmark catalog, identifiers or sampling budget (maximum 10,000 battles).");
        var profiles = definition.Profiles.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var floors = new JsonWorldTowerDefinitionProvider(Path.Combine(contentRoot, "Data", TowerBattleRunner.FloorFile), HarnessJson.Options);
        foreach (var party in definition.Parties)
        {
            bool LegalCell(IReadOnlyList<string>? cell) => cell is { Count: 5 } && cell.All(profiles.ContainsKey);
            if (!SafeId(party.Id) || string.IsNullOrWhiteSpace(party.Assumptions) || !LegalCell(party.CellProfiles)
                || (definition.SchemaVersion == 1 && party.FloorCellProfiles is not null)
                || (definition.SchemaVersion == 2 && (party.FloorCellProfiles is null
                    || party.FloorCellProfiles.Count is < 1 or > 20
                    || definition.Floors.Any(f => !party.FloorCellProfiles.ContainsKey(f))
                    || party.FloorCellProfiles.Any(p => floors.GetFloor(p.Key) is null || !LegalCell(p.Value)))))
                throw new InvalidDataException("Each Tower party requires five known cell profiles; schema 2 requires explicit legal cells for every selected released floor.");
        }
        var scenarios = new List<TowerScenario>();
        foreach (var floorNumber in definition.Floors.Order())
        {
            var floor = floors.GetFloor(floorNumber) ?? throw new InvalidDataException("Benchmark references an unreleased Tower floor.");
            var seeds = Enumerable.Range(0, count).Select(i => StableRandom.Seed("tower-benchmark-seeds-v1",
                masterSeed.ToString(CultureInfo.InvariantCulture), floorNumber.ToString(CultureInfo.InvariantCulture),
                i.ToString(CultureInfo.InvariantCulture))).ToArray();
            foreach (var party in definition.Parties.OrderBy(p => p.Id, StringComparer.Ordinal))
            {
                var cellProfiles = party.FloorCellProfiles?[floorNumber] ?? party.CellProfiles;
                var id = $"floor-{floorNumber.ToString(CultureInfo.InvariantCulture)}.{party.Id}";
                var members = Enumerable.Range(1, floor.RequiredSlots).Select(slot =>
                {
                    var profile = profiles[cellProfiles[(slot - 1) % 5]];
                    // Identity follows party/slot, independent of floor, content coefficients or list order.
                    return new TowerPartyRecipe(slot, profile.Build with
                        { Id = $"{definition.Id}.{party.Id}.slot-{slot.ToString(CultureInfo.InvariantCulture)}.{profile.Id}" });
                }).ToArray();
                scenarios.Add(new(1, id, floorNumber, definition.StartsAt, "uncleared-no-contributions",
                    [party.Assumptions, .. cellProfiles.Distinct().Select(p => profiles[p].Assumptions),
                     "Fixed legal ownership budgets; access and acquisition are assumed. No approved Tower target or starter band.",
                     "Repeat the ordered five-member cell to fill RequiredSlots. Full prepared health, no contributions or styles; production opening cooldowns."],
                    seeds, members));
            }
        }
        return scenarios;
    }

    internal static bool SafeId(string id) => id is { Length: > 0 and <= 100 }
        && Regex.IsMatch(id, "^[a-z0-9][a-z0-9-]*$", RegexOptions.CultureInvariant);

    public static async Task<TowerBenchmarkReport> RunAsync(string apiRoot, string catalog, string output,
        int masterSeed = 1337, int? samples = null, string? reference = null, CancellationToken token = default,
        Action<string>? progress = null)
    {
        if (Path.Exists(output)) throw new IOException("Benchmark output already exists; choose a new directory.");
        // A reference is explicit, immutable and verified before spending time running a candidate.
        if (reference is not null && ReadSaved(reference, token).Report.Status != "Complete")
            throw new InvalidDataException("Tower comparison reference must be complete.");
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        TowerBenchmarkInput? input = null;
        var cells = new Dictionary<string, SavedTower>(StringComparer.Ordinal);
        var status = "Invalid";
        try
        {
            var root = Path.Combine(output, "content");
            var hashes = TowerBundle.CopyContent(apiRoot, root, token);
            var settings = TowerBundle.ReadSettings(apiRoot);
            var definition = HarnessJson.Read<TowerBenchmarkDefinition>(catalog);
            if (samples.HasValue) definition = definition with { SamplesPerCell = samples.Value };
            var scenarios = Expand(definition, root, masterSeed);
            input = new(1, definition, masterSeed, settings, scenarios);
            var runner = new TowerBattleRunner(root, new OfflineContent(root, settings.Threat));
            // Validate every generated profile/floor before any battle; child bundles freeze all seeds.
            foreach (var scenario in scenarios)
            {
                token.ThrowIfCancellationRequested();
                runner.CreateInput(scenario, scenario.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks);
            }
            HarnessJson.WriteNew(Path.Combine(output, "benchmark-input.json"), input);
            HarnessJson.WriteNew(Path.Combine(output, "benchmark-manifest.json"), new TowerManifest(1,
                HarnessJson.Hash(input), hashes, ExecutionIdentity.Current()));
            Directory.CreateDirectory(Path.Combine(output, "scenarios"));
            Directory.CreateDirectory(Path.Combine(output, "cells"));
            foreach (var scenario in scenarios)
                HarnessJson.WriteNew(Path.Combine(output, "scenarios", scenario.Id + ".json"), scenario);
            foreach (var scenario in scenarios)
            {
                token.ThrowIfCancellationRequested();
                var run = Path.Combine(output, "cells", scenario.Id);
                try
                {
                    await TowerBundle.CreateAsync(root, Path.Combine(output, "scenarios", scenario.Id + ".json"), run,
                        token, message => progress?.Invoke($"{scenario.Id}: {message}"), settings);
                }
                finally
                {
                    if (File.Exists(Path.Combine(run, "tower-results.json")) && File.Exists(Path.Combine(run, "tower-input.json")))
                        cells[scenario.Id] = TowerBundle.ReadSaved(run);
                }
            }
            status = "Complete";
        }
        catch (Exception error)
        {
            status = error is OperationCanceledException ? "Cancelled" : "Invalid";
            HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new { Status = status, error.Message });
            throw;
        }
        finally
        {
            if (input is not null)
            {
                var report = Summarize(input, cells, status);
                HarnessJson.WriteNew(Path.Combine(output, "benchmark.json"), report);
                File.WriteAllText(Path.Combine(output, "benchmark.md"), Markdown(report));
            }
        }
        var saved = ReadSaved(output, token);
        if (reference is not null)
        {
            var comparison = TowerBenchmarkComparison.Create(reference, output, Path.Combine(output, "comparison"), token);
            if (comparison.Status != "Compared")
                throw new InvalidDataException($"Benchmark measurement saved; comparison is {comparison.Status}. See comparison/comparison.md.");
        }
        return saved.Report;
    }

    public static SavedTowerBenchmark ReadSaved(string directory, CancellationToken token = default)
    {
        var input = HarnessJson.Read<TowerBenchmarkInput>(Path.Combine(directory, "benchmark-input.json"));
        var manifest = HarnessJson.Read<TowerManifest>(Path.Combine(directory, "benchmark-manifest.json"));
        TowerBundle.VerifySnapshot(directory, manifest, HarnessJson.Hash(input), token);
        if (input.SchemaVersion != 1 || HarnessJson.Hash(Expand(input.Definition, Path.Combine(directory, "content"), input.MasterSeed))
            != HarnessJson.Hash(input.Scenarios))
            throw new InvalidDataException("Saved benchmark scenarios do not match the catalog/schedule.");
        var report = HarnessJson.Read<TowerBenchmarkReport>(Path.Combine(directory, "benchmark.json"));
        var cells = new Dictionary<string, SavedTower>(StringComparer.Ordinal);
        foreach (var scenario in input.Scenarios)
        {
            token.ThrowIfCancellationRequested();
            var run = Path.Combine(directory, "cells", scenario.Id);
            if (!Directory.Exists(run)) continue;
            var cell = TowerBundle.ReadSaved(run, token);
            if (HarnessJson.Hash(cell.Inputs[0].Scenario) != HarnessJson.Hash(scenario)
                || HarnessJson.Hash(cell.Manifest.ContentHashes) != HarnessJson.Hash(manifest.ContentHashes)
                || HarnessJson.Hash(cell.Manifest.Execution) != HarnessJson.Hash(manifest.Execution)
                || cell.Inputs.Any(i => HarnessJson.Hash(i.ThreatAndTanking) != HarnessJson.Hash(input.Settings.Threat)
                    || i.CheckpointIntervalTicks != input.Settings.CheckpointIntervalTicks))
                throw new InvalidDataException("Benchmark cell inputs, content, settings or execution identity are inconsistent.");
            cells.Add(scenario.Id, cell);
        }
        if (report.Status is not ("Complete" or "Cancelled" or "Invalid")
            || (report.Status == "Complete" && (cells.Count != input.Scenarios.Count || cells.Values.Any(c => c.Scorecard.Status != "Complete")))
            || HarnessJson.Hash(report) != HarnessJson.Hash(Summarize(input, cells, report.Status)))
            throw new InvalidDataException("Benchmark report is inconsistent with verified cell evidence.");
        return new(Path.GetFullPath(directory), input, manifest, report, cells);
    }

    public static async Task<TowerBattleReport> ReplayAsync(string directory, string battle, bool detailed, CancellationToken token)
    {
        var saved = ReadSaved(directory, token);
        var match = saved.Input.Scenarios.SelectMany(s => Enumerable.Range(1, s.Seeds.Count)
            .Select(i => (Cell: s.Id, Trial: $"tower.{i.ToString("D4", CultureInfo.InvariantCulture)}")))
            .SingleOrDefault(x => $"{x.Cell}/{x.Trial}" == battle);
        if (match.Cell is null) throw new InvalidDataException("Unknown benchmark battle; use cell-id/tower.NNNN.");
        return await TowerBundle.ReplayAsync(Path.Combine(directory, "cells", match.Cell), match.Trial, detailed, token);
    }

    public static double Survival(TowerBattleReport report) => 100d * report.Battle.Summary.Friendly.Count(p => p.Health > 0)
        / report.Battle.Summary.Friendly.Count;

    private static TowerBenchmarkReport Summarize(TowerBenchmarkInput input, IReadOnlyDictionary<string, SavedTower> cells, string status)
    {
        var summaries = input.Scenarios.Select(s =>
        {
            cells.TryGetValue(s.Id, out var cell);
            var trials = cell?.Scorecard.Trials ?? [];
            return new TowerBenchmarkCell(s.Id, s.FloorNumber, s.Id[(s.Id.IndexOf('.') + 1)..], cell?.Scorecard.Status ?? "NotRun",
                trials.Count, trials.Count(t => t.Report.Succeeded), cell?.Scorecard.Defeats ?? 0, cell?.Scorecard.Draws ?? 0,
                cell?.Scorecard.ClearRate, SuiteScorecard.Distribution(trials.Select(t => Survival(t.Report))),
                SuiteScorecard.Distribution(trials.Select(t => t.Report.Battle.Summary.DurationSeconds)),
                SuiteScorecard.Distribution(trials.Select(t => (double)t.Report.GuardianHealthRemainingPercent)));
        }).ToArray();
        return new(1, status, input.Scenarios.Sum(s => s.Seeds.Count), summaries.Sum(c => c.Valid), summaries);
    }

    private static string Markdown(TowerBenchmarkReport report)
    {
        var text = new StringBuilder($"# Tower benchmarks\n\n{report.Status}: {report.ValidBattles}/{report.PlannedBattles} valid battles.\n\n");
        text.AppendLine("Descriptive measurements only; no approved Tower target, starter 50–90% band, or automatic baseline promotion. Profiles are regenerated from the captured current content. Survival is the percentage of original party members alive at the end.\n");
        text.AppendLine("| Cell | Status | Wins / valid | Clear % [95% Wilson] | Survival % | Seconds | Guardian health % |\n| --- | --- | --- | --- | --- | --- | --- |");
        foreach (var cell in report.Cells)
        {
            var rate = cell.ClearRate is { } r ? FormattableString.Invariant($"{100*r.Rate:F2} [{100*r.Lower:F2}–{100*r.Upper:F2}]") : "unavailable";
            text.AppendLine(FormattableString.Invariant($"| {cell.Id} | {cell.Status} | {cell.Wins}/{cell.Valid} | {rate} | {cell.SurvivalPercent.Mean:F2} | {cell.DurationSeconds.Mean:F2} | {cell.GuardianHealthPercent.Mean:F2} |"));
        }
        text.AppendLine("\nEach cell has its own Markdown/JSON scorecard and saved trials under `cells/`. Replay IDs use `cell-id/tower.0001`. No pooled win rate: parties on a floor share seeds. Generated recipes and the full schedule are in `benchmark-input.json` and `scenarios/`.");
        return text.ToString();
    }
}
