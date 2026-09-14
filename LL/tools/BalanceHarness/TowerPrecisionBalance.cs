using System.Text;
using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerPrecisionSource(TowerStagedDefinition Definition,
    IReadOnlyList<TowerBalanceEvidence> First, IReadOnlyList<TowerBalanceEvidence> Second);
public sealed record TowerPrecisionDefinition(int SchemaVersion, string Id, string Policy,
    string SourceManifestHash, string SourceDefinitionHash, string FirstEvidenceHash, string SecondEvidenceHash,
    string SourceLedgerHash, IReadOnlyList<int> ExcludedCombatSeeds, IReadOnlyList<string> FreshCellIds,
    IReadOnlyList<int> FreshSeeds, string ExecutionHash, int MaximumBattles);
public sealed record TowerPrecisionSelection(string Status, IReadOnlyList<string> FreshCellIds,
    int FirstFamily, int SecondFamily, IReadOnlyList<TowerStagedCheck> Retained);
public sealed record TowerPrecisionReport(string Policy, string DefinitionHash, GoalOutcome Assessment, int ExitCode,
    int FamilySize, int FirstFamily, int SecondFamily, int FreshFamily, int HistoricalTrials, int FreshTrials,
    double FirstAlpha, double SecondAlpha, double FreshAlpha, IReadOnlyList<TowerStagedCheck> Cells, string Scope);

/// <summary>A separate, fixed fresh look after tightening the entire completed second-stage family.</summary>
public static class TowerPrecisionBalance
{
    public const string Policy = "tower-composite-precision-wilson-95-v1";
    public const double FirstAlpha = .025, SecondAlpha = .0125, FreshAlpha = .0125;
    public static TowerPrecisionDefinition Read(string path) => TowerContractJson.Read<TowerPrecisionDefinition>(path);
    public static string EvidenceHash(IReadOnlyList<TowerBalanceEvidence> evidence) =>
        HarnessJson.Hash(evidence.OrderBy(e => e.CellId, StringComparer.Ordinal).ToArray());

    public static TowerPrecisionSelection Select(TowerPrecisionSource source)
    {
        var original = TowerStagedBalance.Evaluate(source.Definition, source.First, source.Second);
        if (original.Selection.Status != "Proceed")
            throw new InvalidDataException("Precision requires a complete original two-stage family.");
        var tightened = original.Cells.Select(c => c.Stage == 2
            ? c with { Adjusted = Interval(c.Wins!.Value, c.Samples!.Value, source.Second.Count, SecondAlpha) }
            : c).ToArray();
        var unresolved = tightened.Where(c => c.Adjusted!.Upper > .5).Select(c => c.Id).Order(StringComparer.Ordinal).ToArray();
        // Preserve even first-stage breaches for cells whose later observation fell below the ceiling.
        var breach = source.First.Concat(source.Second).Any(e => Wins(e) * 2 > e.Trials.Count);
        return new(breach ? "SourceCeilingBreach" : "Proceed", unresolved, source.First.Count, source.Second.Count, tightened);
    }

    public static TowerPrecisionSelection Validate(TowerPrecisionDefinition d, TowerPrecisionSource source, JsonElement ledger)
    {
        if (d is null || d.SchemaVersion != 1 || d.Policy != Policy || !TowerBenchmark.SafeId(d.Id)
            || new[] { d.SourceManifestHash, d.SourceDefinitionHash, d.FirstEvidenceHash, d.SecondEvidenceHash,
                d.SourceLedgerHash, d.ExecutionHash }.Any(h => !TowerContractJson.Hash(h))
            || d.SourceDefinitionHash != HarnessJson.Hash(source.Definition)
            || d.FirstEvidenceHash != EvidenceHash(source.First) || d.SecondEvidenceHash != EvidenceHash(source.Second)
            || d.SourceLedgerHash != HarnessJson.Hash(ledger)
            || d.ExcludedCombatSeeds is null || d.ExcludedCombatSeeds.Count > TowerStudyLimits.HistoricalSeeds
            || d.ExcludedCombatSeeds.Distinct().Count() != d.ExcludedCombatSeeds.Count
            || d.FreshCellIds is not { Count: > 0 and <= 20000 }
            || d.FreshCellIds.Any(id => !TowerBenchmark.SafeId(id))
            || !d.FreshCellIds.SequenceEqual(d.FreshCellIds.Distinct().Order(StringComparer.Ordinal))
            || d.FreshSeeds is not { Count: > 0 and <= 1000 } || d.FreshSeeds.Distinct().Count() != d.FreshSeeds.Count
            || d.MaximumBattles is < 1 or > 100000 || (long)d.FreshCellIds.Count * d.FreshSeeds.Count != d.MaximumBattles)
            throw new InvalidDataException("Invalid frozen precision contract or replaced source evidence.");
        var excluded = TowerSearchBenchmark.History(ledger).ToHashSet();
        if (!excluded.SetEquals(d.ExcludedCombatSeeds)
            || source.Definition.ExcludedCombatSeeds.Concat(source.Definition.FirstSeeds).Concat(source.Definition.SecondSeeds).Any(s => !excluded.Contains(s))
            || d.FreshSeeds.Any(excluded.Contains))
            throw new InvalidDataException("Precision must exclude every source ledger reservation, including unused seeds.");
        var selection = Select(source);
        if (selection.Status != "Proceed" || !d.FreshCellIds.SequenceEqual(selection.FreshCellIds))
            throw new InvalidDataException("Precision cannot omit an unresolved recipe or override a source ceiling breach.");
        return selection;
    }

    public static TowerScenario Scenario(TowerPrecisionDefinition d, TowerStagedCell cell) => cell.Scenario with { Seeds = d.FreshSeeds };

    public static TowerPrecisionReport Evaluate(TowerPrecisionDefinition d, TowerPrecisionSource source, JsonElement ledger,
        IReadOnlyList<TowerBalanceEvidence> fresh)
    {
        var selection = Validate(d, source, ledger);
        if (fresh is null || fresh.Any(e => e is null || e.CellId is null)
            || fresh.Count != d.FreshCellIds.Count || fresh.Select(e => e.CellId).Distinct().Count() != fresh.Count
            || !fresh.Select(e => e.CellId).Order(StringComparer.Ordinal).SequenceEqual(d.FreshCellIds))
            throw new InvalidDataException("Missing, duplicate or extra precision evidence.");
        var cells = source.Definition.Cells.ToDictionary(c => c.Id);
        var cohorts = source.Definition.Cohorts.ToDictionary(c => c.Id);
        foreach (var e in fresh)
        {
            var cell = cells[e.CellId];
            if (e.Status != "Complete" || e.Error is not null || e.ScenarioHash != HarnessJson.Hash(Scenario(d, cell))
                || e.ContentHash != HarnessJson.Hash(source.Definition.ContentHashes) || e.SettingsHash != source.Definition.SettingsHash
                || e.ExecutionHash != d.ExecutionHash || e.RequiredPartySize != cohorts[cell.CohortId].RequiredPartySize
                || !TowerContractJson.Hash(e.ArtifactHash) || e.Trials is null
                || e.Trials.Any(t => t is null || !Enum.IsDefined(t.Outcome)) || !e.Trials.Select(t => t.Seed).SequenceEqual(d.FreshSeeds))
                throw new InvalidDataException("Precision evidence differs from the exact frozen recipe/content/settings/execution/schedule.");
        }
        var byId = fresh.ToDictionary(e => e.CellId);
        var rows = selection.Retained.Select(c => byId.TryGetValue(c.Id, out var e)
            ? new TowerStagedCheck(c.Id, c.CohortId, 3, Wins(e), e.Trials.Count,
                Interval(Wins(e), e.Trials.Count, fresh.Count, FreshAlpha), Wins(e) * 2 > e.Trials.Count, e.ArtifactHash)
            : c).ToArray();
        var outcome = rows.Any(c => c.ObservedAboveCeiling) ? GoalOutcome.Fail
            : cohorts.Keys.Any(g => rows.Where(c => c.CohortId == g).All(c => c.Adjusted!.Upper < .1)) ? GoalOutcome.Fail
            : rows.All(c => c.Adjusted!.Upper <= .5) && cohorts.Keys.All(g => rows.Any(c => c.CohortId == g && c.Adjusted!.Lower >= .1))
                ? GoalOutcome.Pass : GoalOutcome.Inconclusive;
        return new(Policy, HarnessJson.Hash(d), outcome, outcome switch { GoalOutcome.Pass => 0, GoalOutcome.Fail => 1, _ => 3 },
            rows.Length, selection.FirstFamily, selection.SecondFamily, fresh.Count,
            source.First.Concat(source.Second).Sum(e => e.Trials.Count), fresh.Sum(e => e.Trials.Count),
            FirstAlpha, SecondAlpha, FreshAlpha, rows,
            "Complete source family, original first-stage selection and bounds retained at alpha .025; every original second-stage bound tightened at alpha .0125 before selecting the entire unresolved family for independent fresh samples at alpha .0125. Final refreshed bounds use fresh samples only. Any source or fresh observed rate above 50% rejects acceptance. No dropped cells, pooling or optional extension. Approximate Wilson coverage; no lifetime repeated-study guarantee, unsearched-build, search-reliability or acquisition claim.");
    }

    private static int Wins(TowerBalanceEvidence e) => e.Trials.Count(t => t.Outcome == BattleOutcome.Victory);

    public static RateEstimate Interval(int wins, int samples, int familySize, double alpha)
    {
        if (samples < 1 || wins < 0 || wins > samples || familySize is < 1 or > 20000 || alpha is not (.025 or .0125))
            throw new ArgumentOutOfRangeException(nameof(samples));
        // Acklam lower-tail approximation, explicitly recording the actual family and stage alpha.
        var q = Math.Sqrt(-2 * Math.Log(alpha / (2 * familySize)));
        var numerator = (((((-7.784894002430293e-3*q-.3223964580411365)*q-2.400758277161838)*q-2.549732539343734)*q+4.374664141464968)*q+2.938163982698783);
        var denominator = ((((7.784695709041462e-3*q+.3224671290700398)*q+2.445134137142996)*q+3.754408661907416)*q+1);
        var z = -numerator / denominator; var p = wins / (double)samples; var scale = 1 + z*z/samples;
        var center = (p + z*z/(2*samples)) / scale;
        var width = z*Math.Sqrt(p*(1-p)/samples + z*z/(4d*samples*samples)) / scale;
        return new(p, Math.Max(0, center-width), Math.Min(1, center+width), 1-alpha/familySize);
    }

    public static string Markdown(TowerPrecisionReport r)
    {
        var text = new StringBuilder($"# Composite Tower precision: {r.Assessment}\n\n{r.Scope}\n\nFamily: {r.FamilySize}; historical fights: {r.HistoricalTrials}; fresh fights: {r.FreshTrials}.\n\n| Cell | Final stage | Wins / samples | Adjusted interval |\n| --- | ---: | ---: | --- |\n");
        foreach (var c in r.Cells) text.AppendLine(FormattableString.Invariant($"| {c.Id} | {c.Stage} | {c.Wins}/{c.Samples} | {c.Adjusted!.Lower:P2}–{c.Adjusted.Upper:P2} |"));
        return text.ToString();
    }
}
