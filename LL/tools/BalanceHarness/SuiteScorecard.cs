using System.Globalization;
using System.Text;
using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record BattleObservation(string BattleId, string CellId, int Index, int Seed, string Status,
    BattleOutcome? Outcome = null, string? TerminationReason = null, double? DurationSeconds = null,
    double? RemainingHealthFraction = null, string? Error = null)
{
    public static BattleObservation FromResult(SuiteCell cell, SuiteTrial trial, BattleReport result)
    {
        if (result.SchemaVersion != 1 || result.ScenarioId != cell.Id || result.Seed != trial.Seed
            || result.TicksPerSecond <= 0 || result.Summary.DurationTicks < 0
            || result.Summary.DurationSeconds != result.Summary.DurationTicks / (double)result.TicksPerSecond)
            throw new InvalidDataException($"Invalid battle identity or duration units: {trial.BattleId}.");
        var player = result.Summary.Friendly.Single(x => x.Id == "friendly-1");
        if (player.MaxHealth <= 0) throw new InvalidDataException("Missing player maximum health.");
        return new(trial.BattleId, cell.Id, trial.Index, trial.Seed, "Completed",
            result.Summary.ContentOutcome, result.Summary.TerminationReason,
            result.Summary.DurationSeconds, player.Health / (double)player.MaxHealth);
    }
}
public sealed record RateEstimate(double Rate, double Lower, double Upper, double Confidence = 0.95);
public sealed record NumericDistribution(int Count, double? Mean, double? Median, double? P90);
public sealed record CellScorecard(string CellId, string Stage, string Build, string Encounter,
    int Planned, int Valid, int Invalid, int Cancelled, int NotRun, int Wins, int Losses, int Draws,
    int TickLimitDraws, RateEstimate? ClearRate, NumericDistribution DurationSeconds,
    NumericDistribution WinDurationSeconds, NumericDistribution NonWinDurationSeconds,
    NumericDistribution RemainingHealthFraction, NumericDistribution WinRemainingHealthFraction);
public sealed record SuiteReport(int SchemaVersion, string SuiteId, string Status, string Policy,
    int MasterSeed, int Planned, int Valid, int Invalid, int Cancelled, int NotRun,
    double ElapsedSeconds, IReadOnlyList<CellScorecard> Cells);

public static class SuiteScorecard
{
    public static SuiteReport Create(SuiteRunInput input, IReadOnlyList<BattleObservation> observations,
        bool cancelled, double elapsedSeconds)
    {
        var expected = input.Cells.SelectMany(c => c.Trials.Select(t => (t.BattleId, Cell: c, Trial: t)))
            .ToDictionary(x => x.BattleId, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var observation in observations)
        {
            if (!seen.Add(observation.BattleId) || !expected.TryGetValue(observation.BattleId, out var entry)
                || observation.CellId != entry.Cell.Id || observation.Index != entry.Trial.Index
                || observation.Seed != entry.Trial.Seed)
                throw new InvalidDataException("Unexpected or duplicate battle observation.");
            if (observation.Status is not ("Completed" or "Invalid" or "Cancelled")
                || (observation.Status == "Completed" && (observation.Outcome is null
                    || !Enum.IsDefined(observation.Outcome.Value)
                    || observation.DurationSeconds is not { } duration || !double.IsFinite(duration) || duration < 0
                    || observation.RemainingHealthFraction is not { } health || !double.IsFinite(health) || health is < 0 or > 1)))
                throw new InvalidDataException("Invalid observation status or missing combat measurements.");
        }
        var grouped = observations.ToLookup(x => x.CellId, StringComparer.Ordinal);
        var cells = input.Cells.Select(cell =>
        {
            var rows = grouped[cell.Id].ToArray();
            var valid = rows.Where(x => x.Status == "Completed").ToArray();
            var wins = valid.Count(x => x.Outcome == BattleOutcome.Victory);
            return new CellScorecard(cell.Id, cell.Stage, cell.Build, cell.Encounter, cell.Trials.Count,
                valid.Length, rows.Count(x => x.Status == "Invalid"), rows.Count(x => x.Status == "Cancelled"),
                cell.Trials.Count - rows.Length, wins,
                valid.Count(x => x.Outcome == BattleOutcome.Defeat), valid.Count(x => x.Outcome == BattleOutcome.Draw),
                valid.Count(x => x.Outcome == BattleOutcome.Draw && x.TerminationReason == "TickLimit"),
                Wilson(wins, valid.Length), Distribution(valid.Select(x => x.DurationSeconds!.Value)),
                Distribution(valid.Where(x => x.Outcome == BattleOutcome.Victory).Select(x => x.DurationSeconds!.Value)),
                Distribution(valid.Where(x => x.Outcome != BattleOutcome.Victory).Select(x => x.DurationSeconds!.Value)),
                Distribution(valid.Select(x => x.RemainingHealthFraction!.Value)),
                Distribution(valid.Where(x => x.Outcome == BattleOutcome.Victory).Select(x => x.RemainingHealthFraction!.Value)));
        }).ToArray();
        var invalidCount = cells.Sum(x => x.Invalid);
        var notRun = cells.Sum(x => x.NotRun);
        var cancelledCount = cells.Sum(x => x.Cancelled);
        var status = cancelled || cancelledCount > 0 ? "Cancelled"
            : invalidCount > 0 || notRun > 0 ? "Invalid" : "Complete";
        return new(1, input.Definition.Id, status, "Advisory", input.MasterSeed,
            expected.Count, cells.Sum(x => x.Valid), invalidCount, cancelledCount, notRun, elapsedSeconds, cells);
    }

    public static RateEstimate? Wilson(int wins, int samples)
    {
        if (samples < 0 || wins < 0 || wins > samples) throw new ArgumentOutOfRangeException(nameof(wins));
        if (samples == 0) return null;
        const double z = 1.959963984540054;
        var rate = wins / (double)samples;
        var divisor = 1 + z * z / samples;
        var center = (rate + z * z / (2 * samples)) / divisor;
        var margin = z * Math.Sqrt(rate * (1 - rate) / samples + z * z / (4 * samples * samples)) / divisor;
        return new(rate, Math.Max(0, center - margin), Math.Min(1, center + margin));
    }

    public static NumericDistribution Distribution(IEnumerable<double> source)
    {
        var values = source.Order().ToArray();
        if (values.Any(x => !double.IsFinite(x))) throw new InvalidDataException("Non-finite measurement.");
        return values.Length == 0 ? new(0, null, null, null)
            : new(values.Length, values.Average(), Quantile(0.5), values.Length >= 10 ? Quantile(0.9) : null);
        double Quantile(double fraction)
        {
            var position = (values.Length - 1) * fraction;
            var lower = (int)Math.Floor(position);
            return values[lower] + (values[(int)Math.Ceiling(position)] - values[lower]) * (position - lower);
        }
    }

    public static string Markdown(SuiteRunInput input, SuiteReport report, IReadOnlyList<BattleObservation> observations)
    {
        var text = new StringBuilder();
        text.AppendLine($"# Idle balance scorecard: {Escape(report.SuiteId)}\n");
        text.AppendLine($"**{report.Status} — advisory only.** No balance targets or baseline acceptance have been applied.\n");
        text.AppendLine($"Planned {report.Planned}; valid {report.Valid}; invalid {report.Invalid}; cancelled {report.Cancelled}; not run {report.NotRun}. Master seed: {report.MasterSeed}. Runtime: {Number(report.ElapsedSeconds)} s.\n");
        text.AppendLine(Escape(input.Definition.Description) + "\n");
        text.AppendLine("Clear rate is wins / valid battles, with a 95% Wilson interval for each cell. Invalid or cancelled runs remain incomplete evidence. Builds share encounter seeds; their results are paired and must not be pooled as independent trials.\n");
        text.AppendLine("| Stage | Build | Encounter | W/L/D (valid n) | Clear rate [95% interval] | Win median s | Non-win median s | Mean health left | Tick-limit draws | Invalid/cancelled/not run |");
        text.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var cell in report.Cells)
        {
            var rate = cell.ClearRate is { } r ? $"{Percent(r.Rate)} [{Percent(r.Lower)}, {Percent(r.Upper)}]" : "unavailable";
            text.AppendLine($"| {Escape(cell.Stage)} | {Escape(cell.Build)} | {Escape(cell.Encounter)} | {cell.Wins}/{cell.Losses}/{cell.Draws} ({cell.Valid}) | {rate} | {Number(cell.WinDurationSeconds.Median)} | {Number(cell.NonWinDurationSeconds.Median)} | {Percent(cell.RemainingHealthFraction.Mean)} | {cell.TickLimitDraws} | {cell.Invalid}/{cell.Cancelled}/{cell.NotRun} |");
        }
        text.AppendLine("\nHealth left is the original player's final health / maximum health, averaged over all valid attempts, including defeats. Tick-limit draws are a subset of draws. Non-win durations include defeats and draws; capped durations are not kill times. Missing values are unavailable, never zero. JSON also includes means, medians, and interpolated p90 values (only when n ≥ 10).\n");
        text.AppendLine("## Replay examples\n");
        text.AppendLine("One saved example per observed outcome in each cell. All individual outcomes are indexed in `battles.jsonl`; these examples are not a representative sample.\n");
        text.AppendLine("| Battle ID | Outcome/status | Seed |");
        text.AppendLine("| --- | --- | --- |");
        foreach (var row in observations.GroupBy(x => (x.CellId, x.Status, x.Outcome)).Select(x => x.First()))
            text.AppendLine($"| {Escape(row.BattleId)} | {row.Outcome?.ToString() ?? row.Status} | {row.Seed} |");
        text.AppendLine("\n```powershell\ndotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- replay --run <suite-directory> --battle <battle-id> --detailed\n```\n");
        text.AppendLine("## Profile assumptions\n");
        foreach (var stage in input.Definition.Stages)
        {
            text.AppendLine($"### {Escape(stage.Name)}\n");
            foreach (var assumption in stage.Assumptions) text.AppendLine($"- {Escape(assumption)}");
            text.AppendLine();
        }
        return text.ToString();
    }

    private static string Number(double? value) => value?.ToString("0.##", CultureInfo.InvariantCulture) ?? "—";
    private static string Percent(double? value) => value.HasValue ? Number(value * 100) + "%" : "—";
    private static string Escape(string value) => value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
}
