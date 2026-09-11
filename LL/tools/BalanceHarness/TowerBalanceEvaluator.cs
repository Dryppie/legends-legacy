using System.Globalization;
using System.Text;
using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerBalanceCohort(string Id, TowerSearchBudget Budget, int RequiredPartySize,
    string Context, string EquipmentBudgetHash, string Purpose = "intended-progression");
public sealed record TowerBalanceCellDefinition(string Id, string CohortId, string Role,
    TowerScenario Scenario, int MinimumSamples);
public sealed record TowerBalanceDefinition(int SchemaVersion, string Id, string IntervalPolicy,
    IReadOnlyDictionary<string, string> ContentHashes, string SettingsHash, string ExecutionHash,
    IReadOnlyList<TowerBalanceCohort> Cohorts, IReadOnlyList<TowerBalanceCellDefinition> Cells,
    IReadOnlyList<int> ExcludedCombatSeeds, int MaximumBattles);
public sealed record TowerBalanceTrial(int Seed, BattleOutcome Outcome);
public sealed record TowerBalanceEvidence(string CellId, string Status, string ScenarioHash,
    string ContentHash, string SettingsHash, string ExecutionHash, int RequiredPartySize,
    IReadOnlyList<TowerBalanceTrial> Trials, string ArtifactHash, string? Error = null);
public sealed record TowerBalanceCellCheck(string Id, string CohortId, string Role, GoalOutcome Outcome,
    int Planned, int MinimumSamples, int Valid, int Wins, int Defeats, int Draws,
    bool ObservedAboveCeiling, bool ObservedViable, bool UpperSupported, bool LowerSupported,
    RateEstimate? PointwiseInterval, RateEstimate? AdjustedInterval, IReadOnlyList<string> Issues, string? ArtifactHash);
public sealed record TowerBalanceCohortCheck(string Id, int Floor, string Context, string Purpose, GoalOutcome Outcome,
    bool ObservedAboveCeiling, bool ObservedViable, bool SupportedViable, IReadOnlyList<string> Reasons);
public sealed record TowerBalanceReport(int SchemaVersion, string EvaluatorVersion, string DefinitionId,
    string DefinitionHash, string IntervalPolicy, int FamilySize, double FamilyConfidence, GoalOutcome Assessment,
    int ExitCode, IReadOnlyList<string> Issues, IReadOnlyList<TowerBalanceCohortCheck> Cohorts,
    IReadOnlyList<TowerBalanceCellCheck> Cells, string Scope);

/// <summary>Universal upper ceiling and existential lower viability, evaluated separately for each frozen cohort.</summary>
public static class TowerBalanceEvaluator
{
    public const string Version = "tower-balance-v1";
    public const string IntervalPolicy = "bonferroni-wilson-95-v1";
    public const double Minimum = .10;
    public const double Maximum = .50;
    public static TowerBalanceDefinition Read(string path) => TowerContractJson.Read<TowerBalanceDefinition>(path);

    public static void Validate(TowerBalanceDefinition d)
    {
        if (d is null || d.SchemaVersion != 1 || !TowerBenchmark.SafeId(d.Id) || d.IntervalPolicy != IntervalPolicy
            || d.ContentHashes is null || !d.ContentHashes.Keys.Order().SequenceEqual(TowerBundle.Files.Order())
            || d.ContentHashes.Values.Any(h => !TowerContractJson.Hash(h))
            || !TowerContractJson.Hash(d.SettingsHash) || !TowerContractJson.Hash(d.ExecutionHash)
            || d.Cohorts is not { Count: > 0 and <= 100 } || d.Cells is not { Count: > 0 and <= 1000 }
            || d.Cohorts.Any(c => c is null || !TowerBenchmark.SafeId(c.Id) || !TowerBenchmark.SafeId(c.Context)
                || !TowerBossDiscovery.LegalBudget(c.Budget) || !TowerBossDiscovery.LegalPurpose(c.Budget, c.Purpose)
                || c.RequiredPartySize is < 1 or > 50 || !TowerContractJson.Hash(c.EquipmentBudgetHash))
            || d.Cohorts.Select(c => c.Id).Distinct().Count() != d.Cohorts.Count
            || d.Cohorts.Select(c => (c.Budget, c.Context, c.EquipmentBudgetHash)).Distinct().Count() != d.Cohorts.Count
            || d.ExcludedCombatSeeds is null || d.ExcludedCombatSeeds.Count > 100000
            || d.ExcludedCombatSeeds.Distinct().Count() != d.ExcludedCombatSeeds.Count || d.MaximumBattles is < 1 or > 100000)
            throw new InvalidDataException("Invalid frozen Tower balance family, cohort, hashes or interval policy.");
        var cohorts = d.Cohorts.ToDictionary(c => c.Id);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var recipes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cell in d.Cells)
        {
            if (cell is null || !TowerBenchmark.SafeId(cell.Id) || !ids.Add(cell.Id)
                || cell.CohortId is null || !cohorts.TryGetValue(cell.CohortId, out var cohort)
                || cell.Role is not ("generated" or "reference") || cell.Scenario is null || cell.Scenario.SchemaVersion != 1
                || cell.Scenario.FloorNumber != cohort.Budget.PriorityFloor || !TowerBenchmark.SafeId(cell.Scenario.Id)
                || cell.Scenario.PreparationState != "uncleared-no-contributions" || cell.Scenario.Assumptions is null
                || cell.Scenario.Seeds is not { Count: > 0 and <= 1000 }
                || cell.Scenario.Seeds.Distinct().Count() != cell.Scenario.Seeds.Count
                || cell.Scenario.Seeds.Intersect(d.ExcludedCombatSeeds).Any()
                || cell.MinimumSamples is < 1 or > 1000 || cell.MinimumSamples > cell.Scenario.Seeds.Count)
                throw new InvalidDataException("Every confirmation cell requires a unique ID, complete cohort and fresh bounded schedule.");
            TowerBossDiscovery.ValidateEquipment(cell.Scenario.Party, cohort.Budget, cohort.RequiredPartySize);
            if (TowerBossDiscovery.EquipmentBudgetHash(cell.Scenario.Party) != cohort.EquipmentBudgetHash)
                throw new InvalidDataException("Cell equipment or character budget differs from its declared cohort.");
            if (!recipes.Add(HarnessJson.Hash(new { cell.CohortId, Recipe = TowerBossDiscovery.RecipeHash(cell.Scenario.Party) })))
                throw new InvalidDataException("An exact confirmation recipe must be registered once, even if it has multiple sources.");
        }
        foreach (var cohort in d.Cohorts)
        {
            var cells = d.Cells.Where(c => c.CohortId == cohort.Id).ToArray();
            if (cells.Length == 0 || cells.Any(c => !c.Scenario.Seeds.SequenceEqual(cells[0].Scenario.Seeds)
                || c.Scenario.StartsAt != cells[0].Scenario.StartsAt))
                throw new InvalidDataException("Every required cohort needs cells with equal paired seeds and starting context.");
        }
        if (d.Cells.Sum(c => (long)c.Scenario.Seeds.Count) > d.MaximumBattles)
            throw new InvalidDataException("Frozen confirmation family exceeds the actual-combat cap.");
    }

    public static TowerBalanceReport Evaluate(TowerBalanceDefinition d, IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        Validate(d);
        ArgumentNullException.ThrowIfNull(evidence);
        var issues = new List<string>();
        var expected = d.Cells.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        if (evidence.Any(e => e is null || e.CellId is null)) throw new InvalidDataException("Malformed Tower evidence entry.");
        foreach (var group in evidence.GroupBy(e => e.CellId).Where(g => g.Count() > 1)) issues.Add($"Duplicate evidence cell: {group.Key}.");
        foreach (var cell in evidence.Where(e => !expected.Contains(e.CellId))) issues.Add($"Undeclared evidence cell: {cell.CellId}.");
        var checks = new List<TowerBalanceCellCheck>();
        foreach (var cell in d.Cells)
        {
            var errors = new List<string>();
            var matches = evidence.Where(e => e.CellId == cell.Id).ToArray();
            var row = matches.Length == 1 ? matches[0] : null;
            var cohort = d.Cohorts.Single(c => c.Id == cell.CohortId);
            if (row is null) errors.Add(matches.Length == 0 ? "Required confirmation evidence is missing." : "Duplicate evidence cannot be pooled.");
            else
            {
                if (row.Error is not null) errors.Add(row.Error);
                if (row.Status != "Complete") errors.Add($"Evidence status is {row.Status}; incomplete or invalid trials cannot pass.");
                if (row.ScenarioHash != HarnessJson.Hash(cell.Scenario) || row.ContentHash != HarnessJson.Hash(d.ContentHashes)
                    || row.SettingsHash != d.SettingsHash || row.ExecutionHash != d.ExecutionHash
                    || row.RequiredPartySize != cohort.RequiredPartySize || !TowerContractJson.Hash(row.ArtifactHash))
                    errors.Add("Recipe, content, equipment/context, execution identity or required party size does not match the frozen cell.");
                if (row.Trials is null || row.Trials.Any(t => t is null || !Enum.IsDefined(t.Outcome))
                    || !row.Trials.Select(t => t.Seed).SequenceEqual(cell.Scenario.Seeds))
                    errors.Add("Invalid, missing, duplicate, replaced or extra trial identities.");
            }
            // Preserve raw observed breaches even if another integrity/completeness issue blocks acceptance.
            var trials = row?.Trials?.Where(t => t is not null && Enum.IsDefined(t.Outcome)).ToArray() ?? [];
            var wins = trials.Count(t => t.Outcome == BattleOutcome.Victory);
            var above = wins * 2 > trials.Length;
            var viable = trials.Length > 0 && wins / (double)trials.Length >= Minimum;
            RateEstimate? pointwise = null, adjusted = null;
            if (errors.Count == 0)
            {
                pointwise = Wilson(wins, trials.Length, 1);
                adjusted = Wilson(wins, trials.Length, d.Cells.Count);
            }
            var enough = trials.Length >= cell.MinimumSamples;
            var upper = enough && adjusted?.Upper <= Maximum;
            var lower = enough && adjusted?.Lower >= Minimum;
            var outcome = errors.Count > 0 ? GoalOutcome.Invalid : above ? GoalOutcome.Fail
                : upper ? GoalOutcome.Pass : GoalOutcome.Inconclusive;
            checks.Add(new(cell.Id, cell.CohortId, cell.Role, outcome, cell.Scenario.Seeds.Count, cell.MinimumSamples,
                trials.Length, wins, trials.Count(t => t.Outcome == BattleOutcome.Defeat), trials.Count(t => t.Outcome == BattleOutcome.Draw),
                above, viable, upper, lower, pointwise, adjusted, errors, row?.ArtifactHash));
        }
        var cohorts = d.Cohorts.Select(c =>
        {
            var cells = checks.Where(x => x.CohortId == c.Id).ToArray();
            var reasons = new List<string>();
            var invalid = cells.Any(x => x.Outcome == GoalOutcome.Invalid);
            var above = cells.Any(x => x.ObservedAboveCeiling);
            var observedViable = cells.Any(x => x.ObservedViable);
            var supportedViable = cells.Any(x => x.LowerSupported);
            if (invalid) reasons.Add("Required cohort evidence is invalid or incomplete.");
            if (above) reasons.Add("At least one observed party exceeds the inclusive 50% ceiling; other teams cannot hide it.");
            if (!observedViable) reasons.Add("No tested party has demonstrated an observed 10% win rate.");
            if (!supportedViable) reasons.Add("No party's adjusted lower bound establishes at least 10% viability.");
            if (cells.Any(x => !x.UpperSupported)) reasons.Add("Not every party's adjusted upper bound establishes at most 50% wins.");
            var impossible = cells.All(x => x.AdjustedInterval?.Upper < Minimum);
            var assessment = invalid ? GoalOutcome.Invalid : above || impossible ? GoalOutcome.Fail
                : supportedViable && cells.All(x => x.UpperSupported) ? GoalOutcome.Pass : GoalOutcome.Inconclusive;
            if (assessment == GoalOutcome.Pass) reasons.Add("Every included party supports the ceiling and at least one supports viability.");
            return new TowerBalanceCohortCheck(c.Id, c.Budget.PriorityFloor, c.Context, c.Purpose, assessment, above, observedViable, supportedViable, reasons);
        }).ToArray();
        var assessment = issues.Count > 0 ? GoalOutcome.Invalid : Aggregate(cohorts.Select(c => c.Outcome));
        return new(1, Version, d.Id, HarnessJson.Hash(d), IntervalPolicy, d.Cells.Count, .95, assessment,
            assessment switch { GoalOutcome.Pass => 0, GoalOutcome.Fail => 1, GoalOutcome.Invalid => 2, _ => 3 }, issues, cohorts, checks,
            "Scoped assessment of a predeclared confirmation family. Approximate Bonferroni-adjusted Wilson intervals; no guarantee about unsearched teams. Diagnostic-budget findings do not accept intended progression. Search strength, execution completion and archive integrity are separate conclusions.");
    }

    public static RateEstimate? Wilson(int wins, int samples, int familySize)
    {
        if (samples < 0 || wins < 0 || wins > samples || familySize is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(samples));
        if (samples == 0) return null;
        var z = CriticalValue(familySize);
        var p = wins / (double)samples;
        var denominator = 1 + z * z / samples;
        var center = (p + z * z / (2 * samples)) / denominator;
        var width = z * Math.Sqrt(p * (1 - p) / samples + z * z / (4d * samples * samples)) / denominator;
        return new(p, Math.Max(0, center - width), Math.Min(1, center + width), 1 - .05 / familySize);
    }

    private static double CriticalValue(int familySize)
    {
        if (familySize == 1) return 1.959963984540054;
        // Acklam's lower-tail rational approximation, reflected to the upper tail.
        // Here p <= .0125, so only the tail polynomial is needed. Coefficients are
        // independently checked against Python statistics.NormalDist numerical fixtures.
        // Published coefficients: https://alchemy.cs.washington.edu/api/html/convergencetest_8h-source.html
        var q = Math.Sqrt(-2 * Math.Log(.025 / familySize));
        var numerator = (((((-7.784894002430293e-3 * q - .3223964580411365) * q - 2.400758277161838) * q
            - 2.549732539343734) * q + 4.374664141464968) * q + 2.938163982698783);
        var denominator = ((((7.784695709041462e-3 * q + .3224671290700398) * q + 2.445134137142996) * q
            + 3.754408661907416) * q + 1);
        return -numerator / denominator;
    }

    private static GoalOutcome Aggregate(IEnumerable<GoalOutcome> outcomes)
    {
        var values = outcomes.ToArray();
        return values.Contains(GoalOutcome.Invalid) ? GoalOutcome.Invalid : values.Contains(GoalOutcome.Fail) ? GoalOutcome.Fail
            : values.Contains(GoalOutcome.Inconclusive) ? GoalOutcome.Inconclusive : GoalOutcome.Pass;
    }

    public static string Markdown(TowerBalanceReport report)
    {
        string Interval(RateEstimate? rate) => rate is null ? "Unavailable" : string.Create(CultureInfo.InvariantCulture,
            $"{rate.Rate:P2} [{rate.Lower:P2}, {rate.Upper:P2}]");
        var text = new StringBuilder($"# Tower balance assessment: {report.Assessment}\n\n{report.Scope}\n\n");
        text.AppendLine($"Frozen family: {report.FamilySize} recipe/context cells; 95% approximate simultaneous coverage. Draws count as non-wins.\n");
        foreach (var issue in report.Issues) text.AppendLine("- " + issue);
        foreach (var cohort in report.Cohorts)
        {
            text.AppendLine($"\n## {cohort.Id}: floor {cohort.Floor}, {cohort.Context}, {cohort.Purpose} — {cohort.Outcome}\n");
            foreach (var reason in cohort.Reasons) text.AppendLine("- " + reason);
        }
        text.AppendLine("\n## Per-party confirmation\n\n| Cell | Role | Wins / valid | Draws | Pointwise 95% | Adjusted interval | Upper-bound check |\n| --- | --- | ---: | ---: | --- | --- | --- |");
        foreach (var cell in report.Cells)
        {
            text.AppendLine($"| {cell.Id} | {cell.Role} | {cell.Wins}/{cell.Valid} | {cell.Draws} | {Interval(cell.PointwiseInterval)} | {Interval(cell.AdjustedInterval)} | {cell.Outcome} |");
        }
        text.AppendLine("\nA passing per-party upper-bound check does not require that party to reach 10%. Viability is assessed per cohort above.\n");
        foreach (var cell in report.Cells.Where(c => c.Issues.Count > 0))
            foreach (var issue in cell.Issues) text.AppendLine($"- {cell.Id}: {issue}");
        return text.ToString();
    }
}
