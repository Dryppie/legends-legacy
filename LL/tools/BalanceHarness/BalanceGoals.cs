using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BalanceHarness;

public enum GoalMetric { Unknown, ClearRate, WinDurationMean, RemainingHealthMean, ClearRateChange, SharedWinDurationChange, RemainingHealthChange }
public enum GoalRole { Unknown, Primary, Guardrail, Diagnostic }
public enum GoalEnforcement { Unknown, Draft, Enforced }
public enum GoalOutcome { Pass, Fail, Inconclusive, Invalid }

public sealed record BalanceGoal(string Id, GoalMetric Metric, string Unit, GoalRole Role,
    GoalEnforcement Enforcement, IReadOnlyList<string> Cells, int MinimumSamples, string Rationale,
    double? Minimum = null, double? Maximum = null, string? ReviewReason = null);

public sealed record BalanceGoals(int SchemaVersion, string Id, string SuiteId, string FixtureHash,
    string MetricsVersion, string Description, IReadOnlyList<string> RequiredCells, IReadOnlyList<BalanceGoal> Goals,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? DiagnosticCells = null)
{
    [JsonIgnore]
    public bool RequiresBaseline => Goals.Any(g => IsChange(g.Metric));
    public static bool IsChange(GoalMetric metric) => metric is GoalMetric.ClearRateChange
        or GoalMetric.SharedWinDurationChange or GoalMetric.RemainingHealthChange;
    public static string UnitFor(GoalMetric metric) => metric switch
    {
        GoalMetric.ClearRate or GoalMetric.RemainingHealthMean => "percent",
        GoalMetric.WinDurationMean or GoalMetric.SharedWinDurationChange => "seconds",
        GoalMetric.ClearRateChange or GoalMetric.RemainingHealthChange => "percentage points",
        _ => throw new InvalidDataException("Unknown goal metric.")
    };

    public static BalanceGoals Read(string file)
    {
        var options = new JsonSerializerOptions(HarnessJson.Options) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        var goals = JsonSerializer.Deserialize<BalanceGoals>(File.ReadAllText(file), options)
            ?? throw new InvalidDataException("Empty goals file.");
        goals.Validate();
        return goals;
    }

    public void Validate()
    {
        if (SchemaVersion != 1 || MetricsVersion != SavedSuite.MetricsVersion || !Named(Id)
            || string.IsNullOrWhiteSpace(SuiteId) || string.IsNullOrWhiteSpace(Description)
            || FixtureHash is null || !Regex.IsMatch(FixtureHash, "^[a-f0-9]{64}$", RegexOptions.CultureInvariant)
            || RequiredCells is null || RequiredCells.Count is < 1 or > 1000
            || RequiredCells.Any(string.IsNullOrWhiteSpace)
            || RequiredCells.Distinct(StringComparer.Ordinal).Count() != RequiredCells.Count
            || Goals is null || Goals.Count is < 1 or > 1000)
            throw new InvalidDataException("Goals require schema 1, named suite/description, fixture hash, supported metrics and distinct required cells.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var required = RequiredCells.ToHashSet(StringComparer.Ordinal);
        if (DiagnosticCells is { } diagnostics && (diagnostics.Count > 1000
            || diagnostics.Any(string.IsNullOrWhiteSpace)
            || diagnostics.Distinct(StringComparer.Ordinal).Count() != diagnostics.Count
            || diagnostics.Any(required.Contains)))
            throw new InvalidDataException("Diagnostic cells must be distinct, named and separate from required goal cells.");
        long checks = 0;
        foreach (var goal in Goals)
        {
            if (goal is null || !Named(goal.Id) || !ids.Add(goal.Id)
                || goal.Role is not (GoalRole.Primary or GoalRole.Guardrail or GoalRole.Diagnostic)
                || goal.Enforcement is not (GoalEnforcement.Draft or GoalEnforcement.Enforced)
                || goal.Unit != UnitFor(goal.Metric) || goal.MinimumSamples is < 1 or > 10000
                || string.IsNullOrWhiteSpace(goal.Rationale) || goal.Cells is null || goal.Cells.Count == 0
                || goal.Cells.Distinct(StringComparer.Ordinal).Count() != goal.Cells.Count
                || goal.Cells.Any(c => !required.Contains(c)))
                throw new InvalidDataException($"Invalid or duplicate goal, units or cell selector: {goal?.Id}.");
            if ((!goal.Minimum.HasValue && !goal.Maximum.HasValue)
                || (goal.Minimum is { } min && !double.IsFinite(min))
                || (goal.Maximum is { } max && !double.IsFinite(max))
                || (goal.Minimum.HasValue && goal.Maximum.HasValue && goal.Minimum > goal.Maximum))
                throw new InvalidDataException($"Goal '{goal.Id}' needs finite, ordered, inclusive bounds.");
            foreach (var bound in new[] { goal.Minimum, goal.Maximum }.OfType<double>())
                if ((goal.Metric is GoalMetric.ClearRate or GoalMetric.RemainingHealthMean && bound is < 0 or > 100)
                    || (goal.Metric == GoalMetric.WinDurationMean && bound < 0)
                    || (goal.Metric is GoalMetric.ClearRateChange or GoalMetric.RemainingHealthChange && bound is < -100 or > 100))
                    throw new InvalidDataException($"Goal '{goal.Id}' has a bound outside the metric's range.");
            if (goal.Enforcement == GoalEnforcement.Enforced
                && (goal.Role == GoalRole.Diagnostic || string.IsNullOrWhiteSpace(goal.ReviewReason)))
                throw new InvalidDataException($"Enforced goal '{goal.Id}' requires a review reason and must be primary or a guardrail.");
            checks += goal.Cells.Count;
        }
        if (checks > 100000 || RequiredCells.Any(c => !Goals.Any(g => g.Role == GoalRole.Primary && g.Cells.Contains(c, StringComparer.Ordinal))))
            throw new InvalidDataException("Every required cell needs a primary goal; at most 100,000 checks are supported.");
    }

    // Sample budgets and enumeration order are not cohort identity. Recipes, assumptions,
    // encounter selections and start conditions are, independent of derived combat coefficients.
    public static string FixtureContractHash(IdleSuiteDefinition suite) => HarnessJson.Hash(suite with
    {
        SamplesPerCell = 0,
        Stages = suite.Stages.OrderBy(s => s.Id, StringComparer.Ordinal).Select(s => s with
        {
            Builds = s.Builds.OrderBy(b => b.Id, StringComparer.Ordinal).Select(b => b with
                { Equipment = b.Equipment.OrderBy(e => e.Slot).ToArray() }).ToArray(),
            Encounters = s.Encounters.OrderBy(e => e.Id, StringComparer.Ordinal).ToArray()
        }).ToArray()
    });

    private static bool Named(string? value) => value is not null
        && Regex.IsMatch(value, "^[a-z0-9][a-z0-9-]{0,63}$", RegexOptions.CultureInvariant);
}
