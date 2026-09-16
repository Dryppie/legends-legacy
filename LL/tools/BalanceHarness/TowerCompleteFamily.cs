using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerCompleteObservation(string CellHash, int Wins, int Defeats, int Draws, string ArtifactHash);
public sealed record TowerCompleteSelection(string Status, IReadOnlyList<string> SecondCells,
    IReadOnlyList<string> Breaches, string FirstEvidenceHash);
public sealed record TowerCompleteCheck(string CellHash, string ContextHash, int? Stage,
    TowerCompleteObservation? Observation, RateEstimate? Interval);
public sealed record TowerCompleteAssessment(GoalOutcome Outcome, TowerCompleteSelection Selection,
    int LogicalTrials, IReadOnlyList<TowerCompleteCheck> Cells);
public sealed record TowerCompleteCapacity(int Cells, int Anchors, int FirstCells, int FirstFights,
    int MaximumSecondFights, int MaximumFights, int RemainingSecondCells);

/// <summary>Dedicated fixed-family contract. Legacy staged and ordinary study limits stay unchanged.</summary>
public static class TowerCompleteFamily
{
    public const string Version = "tower-captured-v19-complete-family-v1";
    public const int Cells = 43879, Anchors = 560, FirstSamples = 32, SecondSamples = 256,
        MaximumSecondCells = 4096, MaximumFights = 2434784, MaximumSeconds = 86400, BatchCells = 256;
    public const long MaximumBytes = 68719476736;
    public const string SourceSeal = "78b20e50a9af9a8bbd24e37fe09b0050d6a3a4687bc7a8b3dc8c7bf4e3ddfa79";
    public const string SourceCells = "ab889005721127743f9c5bc9444fa727b5b32b54b42b4c31203f99e501c9dbce";

    public static TowerCompleteCapacity Capacity(IReadOnlyList<TowerConfirmationCell> cells, CancellationToken ct = default)
    {
        var reasons = cells.SelectMany(c => c.AnchorReasons).ToArray();
        var summary = TowerConfirmationContext.Validate(cells, Cells, reasons, ct);
        if (reasons.Length != 580 || summary.Anchors != Anchors || summary.Contexts != 1
            || reasons.Count(r => r.StartsWith("historical:", StringComparison.Ordinal)) != 327
            || reasons.Count(r => r.StartsWith("midpoint:", StringComparison.Ordinal)) != 253)
            throw new InvalidDataException("Requires the entire fixed family and historical/midpoint anchor union.");
        return new(Cells, Anchors, Cells - Anchors, checked((Cells - Anchors) * FirstSamples),
            MaximumSecondCells * SecondSamples, MaximumFights, MaximumSecondCells - Anchors);
    }

    // Same approximate quantile as the existing staged policy, with only this new contract's family bound.
    public static RateEstimate Interval(int wins, int samples, int family)
    {
        if (samples is not (FirstSamples or SecondSamples) || wins < 0 || wins > samples || family is < 1 or > Cells)
            throw new ArgumentOutOfRangeException(nameof(wins));
        var q = Math.Sqrt(-2 * Math.Log(.0125 / family));
        var numerator = (((((-7.784894002430293e-3*q-.3223964580411365)*q-2.400758277161838)*q-2.549732539343734)*q+4.374664141464968)*q+2.938163982698783);
        var denominator = ((((7.784695709041462e-3*q+.3224671290700398)*q+2.445134137142996)*q+3.754408661907416)*q+1);
        var z = -numerator / denominator; var p = wins / (double)samples; var scale = 1 + z*z/samples;
        var center = (p + z*z/(2*samples)) / scale;
        var width = z * Math.Sqrt(p*(1-p)/samples + z*z/(4d*samples*samples)) / scale;
        return new(p, Math.Max(0, center-width), Math.Min(1, center+width), 1-.025/family);
    }

    internal static void CheckObservations(IReadOnlyList<TowerConfirmationCell> expected,
        IReadOnlyList<TowerCompleteObservation> rows, int samples)
    {
        if (rows is null || rows.Count != expected.Count || rows.Any(r => r is null || !TowerContractJson.Hash(r.CellHash)
                || !TowerContractJson.Hash(r.ArtifactHash) || r.Wins < 0 || r.Defeats < 0 || r.Draws < 0
                || (long)r.Wins+r.Defeats+r.Draws != samples)
            || !rows.Select(r => r.CellHash).SequenceEqual(expected.Select(c => c.CellHash)))
            throw new InvalidDataException("Missing, reordered, duplicated or invalid complete stage evidence.");
    }

    // Internal small fixtures exercise the same decisions without creating combat reports or reserving seeds.
    internal static TowerCompleteSelection Select(IReadOnlyList<TowerConfirmationCell> cells,
        IReadOnlyList<TowerCompleteObservation> first, int capacity = MaximumSecondCells)
    {
        var expected = cells.Where(c => c.AnchorReasons.Count == 0).ToArray();
        CheckObservations(expected, first, FirstSamples);
        var breaches = first.Where(r => r.Wins * 2 > FirstSamples).Select(r => r.CellHash).ToArray();
        var selected = cells.Where(c => c.AnchorReasons.Count > 0).Select(c => c.CellHash)
            .Concat(first.Where(r => Interval(r.Wins, FirstSamples, first.Count).Upper > .5).Select(r => r.CellHash))
            .Order(StringComparer.Ordinal).ToArray();
        return new(breaches.Length > 0 ? "FirstStageCeilingBreach" : selected.Length > capacity ? "SecondStageCapacityExceeded" : "Proceed",
            selected, breaches, HarnessJson.Hash(first));
    }

    internal static TowerCompleteAssessment Assess(IReadOnlyList<TowerConfirmationCell> cells,
        IReadOnlyList<TowerCompleteObservation> first, IReadOnlyList<TowerCompleteObservation> second, int capacity = MaximumSecondCells)
    {
        var selection = Select(cells, first, capacity); var selected = selection.SecondCells.ToHashSet(StringComparer.Ordinal);
        var expected = selection.Status == "Proceed" ? cells.Where(c => selected.Contains(c.CellHash)).ToArray() : [];
        CheckObservations(expected, second, SecondSamples);
        var a = first.ToDictionary(r => r.CellHash); var b = second.ToDictionary(r => r.CellHash);
        var checks = cells.Select(c => {
            int? stage = b.ContainsKey(c.CellHash) ? 2 : a.ContainsKey(c.CellHash) && !selected.Contains(c.CellHash) ? 1 : null;
            var row = b.GetValueOrDefault(c.CellHash) ?? a.GetValueOrDefault(c.CellHash);
            return new TowerCompleteCheck(c.CellHash, c.ContextHash, stage, row,
                stage is null ? null : Interval(row!.Wins, stage == 1 ? FirstSamples : SecondSamples, stage == 1 ? first.Count : second.Count));
        }).ToArray();
        var outcome = selection.Status == "FirstStageCeilingBreach" || second.Any(r => r.Wins * 2 > SecondSamples) ? GoalOutcome.Fail
            : selection.Status != "Proceed" ? GoalOutcome.Inconclusive
            : checks.GroupBy(c => c.ContextHash).Any(g => g.All(c => c.Interval!.Upper < .1)) ? GoalOutcome.Fail
            : checks.All(c => c.Interval!.Upper <= .5) && checks.GroupBy(c => c.ContextHash).All(g => g.Any(c => c.Interval!.Lower >= .1)) ? GoalOutcome.Pass
            : GoalOutcome.Inconclusive;
        return new(outcome, selection, checked(first.Count * FirstSamples + second.Count * SecondSamples), checks);
    }

    internal static TowerCompleteObservation Observation(TowerConfirmationCell cell, TowerScenario scenario,
        TowerBalanceCohort cohort, IReadOnlyDictionary<string, string> content, string settings, string execution, TowerBalanceEvidence evidence)
    {
        if (evidence.CellId != cell.CellHash || evidence.Status != "Complete" || evidence.Error is not null
            || evidence.ScenarioHash != HarnessJson.Hash(scenario) || evidence.ContentHash != HarnessJson.Hash(content)
            || evidence.SettingsHash != settings || evidence.ExecutionHash != execution || evidence.RequiredPartySize != cohort.RequiredPartySize
            || !TowerContractJson.Hash(evidence.ArtifactHash) || evidence.Trials is null
            || evidence.Trials.Any(t => t is null || !Enum.IsDefined(t.Outcome))
            || !evidence.Trials.Select(t => t.Seed).SequenceEqual(scenario.Seeds))
            throw new InvalidDataException("Archive evidence differs from frozen recipe, settings, execution or ordered schedule.");
        return new(cell.CellHash, evidence.Trials.Count(t => t.Outcome == BattleOutcome.Victory),
            evidence.Trials.Count(t => t.Outcome == BattleOutcome.Defeat), evidence.Trials.Count(t => t.Outcome == BattleOutcome.Draw), evidence.ArtifactHash);
    }
}
