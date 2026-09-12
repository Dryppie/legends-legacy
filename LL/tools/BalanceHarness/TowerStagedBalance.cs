using System.Text;
using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerStagedCell(string Id, string CohortId, string Role, TowerScenario Scenario);
public sealed record TowerStagedDefinition(int SchemaVersion, string Id, string Policy,
    IReadOnlyDictionary<string, string> ContentHashes, string SettingsHash, string ExecutionHash,
    IReadOnlyList<TowerBalanceCohort> Cohorts, IReadOnlyList<TowerStagedCell> Cells,
    IReadOnlyList<string> AnchorIds, IReadOnlyList<int> FirstSeeds, IReadOnlyList<int> SecondSeeds,
    int MaximumSecondStageCells, IReadOnlyList<int> ExcludedCombatSeeds, int MaximumBattles);
public sealed record TowerStagedSelection(string Status, IReadOnlyList<string> SecondStageIds,
    IReadOnlyList<string> FirstStageBreaches, string FirstEvidenceHash);
public sealed record TowerStagedCheck(string Id, string CohortId, int? Stage, int? Wins, int? Samples,
    RateEstimate? Adjusted, bool ObservedAboveCeiling, string? ArtifactHash);
public sealed record TowerStagedReport(string Policy, string DefinitionHash, GoalOutcome Assessment, int ExitCode,
    int FamilySize, int FirstStageCells, int SecondStageCells, int LogicalTrials, int MaximumLogicalTrials,
    TowerStagedSelection Selection, IReadOnlyList<TowerStagedCheck> Cells, string Scope);

/// <summary>Two fixed looks with separate samples/alpha budgets. Historical outcomes can allocate anchors only.</summary>
public static class TowerStagedBalance
{
    public const string Policy = "tower-staged-bonferroni-wilson-95-v1";
    public static TowerStagedDefinition Read(string path) => TowerContractJson.Read<TowerStagedDefinition>(path);

    public static int Validate(TowerStagedDefinition d)
    {
        if (d is null || d.SchemaVersion != 1 || d.Policy != Policy || !TowerBenchmark.SafeId(d.Id)
            || d.Cells is not { Count: > 0 and <= 10000 } || d.Cohorts is not { Count: > 0 and <= 100 }
            || d.AnchorIds is not { Count: > 0 } || d.AnchorIds.Distinct().Count() != d.AnchorIds.Count
            || d.MaximumSecondStageCells < d.AnchorIds.Count || d.MaximumSecondStageCells > d.Cells.Count
            || d.FirstSeeds is not { Count: > 0 and <= 1000 } || d.SecondSeeds is not { Count: > 0 and <= 1000 }
            || d.SecondSeeds.Count < d.FirstSeeds.Count || d.FirstSeeds.Concat(d.SecondSeeds).Distinct().Count() != d.FirstSeeds.Count+d.SecondSeeds.Count
            || d.ExcludedCombatSeeds is null || d.ExcludedCombatSeeds.Count > TowerStudyLimits.HistoricalSeeds
            || d.ExcludedCombatSeeds.Distinct().Count() != d.ExcludedCombatSeeds.Count
            || d.FirstSeeds.Concat(d.SecondSeeds).Intersect(d.ExcludedCombatSeeds).Any()
            || d.MaximumBattles is < 1 or > 500000)
            throw new InvalidDataException("Invalid staged confirmation policy, family, seeds or reservation.");
        if (d.Cells.Any(c => c is null || c.Scenario is null || c.Scenario.Seeds is not { Count: 0 })
            || d.Cells.Select(c => c.Id).Distinct().Count() != d.Cells.Count
            || d.Cohorts.Any(c => c is null) || d.Cohorts.Select(c => c.Id).Distinct().Count() != d.Cohorts.Count
            || d.Cohorts.Select(c => (c.Budget,c.Context,c.EquipmentBudgetHash)).Distinct().Count() != d.Cohorts.Count)
            throw new InvalidDataException("Staged recipes require unique identities and empty per-recipe schedules.");
        var cohorts = d.Cohorts.ToDictionary(c => c.Id); var anchors = d.AnchorIds.ToHashSet(StringComparer.Ordinal);
        if (anchors.Except(d.Cells.Select(c => c.Id)).Any() || d.Cells.Any(c => !cohorts.ContainsKey(c.CohortId)))
            throw new InvalidDataException("Undeclared anchor or cohort.");
        foreach (var group in d.Cells.GroupBy(c => c.CohortId))
            if (!group.Any(c => anchors.Contains(c.Id)) || group.Select(c => (c.Scenario.Id,c.Scenario.StartsAt)).Distinct().Count() != 1)
                throw new InvalidDataException("Each cohort needs a forced fresh anchor and one fixed encounter identity/start.");
        if (d.Cohorts.Any(c => !d.Cells.Any(cell => cell.CohortId == c.Id))
            || d.Cells.Select(c => HarnessJson.Hash(new { c.CohortId, Recipe = TowerBossDiscovery.RecipeHash(c.Scenario.Party) })).Distinct().Count() != d.Cells.Count)
            throw new InvalidDataException("Empty cohort or duplicate normalized recipe.");
        // Reuse the unchanged strict ordinary contract checks in bounded partitions, without changing their limits.
        foreach (var chunk in d.Cells.Chunk(64))
        {
            var cells = chunk.Select(c => new TowerBalanceCellDefinition(c.Id,c.CohortId,c.Role,
                c.Scenario with { Seeds = d.FirstSeeds },d.FirstSeeds.Count)).ToArray();
            TowerBalanceEvaluator.Validate(new(1,d.Id,TowerBalanceEvaluator.IntervalPolicy,d.ContentHashes,d.SettingsHash,d.ExecutionHash,
                d.Cohorts.Where(c => chunk.Any(cell => cell.CohortId == c.Id)).ToArray(),cells,d.ExcludedCombatSeeds,100000));
        }
        var maximum = checked((d.Cells.Count-d.AnchorIds.Count)*d.FirstSeeds.Count + d.MaximumSecondStageCells*d.SecondSeeds.Count);
        if (maximum > d.MaximumBattles) throw new InvalidDataException("Staged maximum exceeds frozen combat reservation.");
        return maximum;
    }

    public static TowerScenario Scenario(TowerStagedDefinition d, TowerStagedCell cell, int stage) => cell.Scenario with
    { Seeds = stage == 1 ? d.FirstSeeds : stage == 2 ? d.SecondSeeds : throw new ArgumentOutOfRangeException(nameof(stage)) };

    public static TowerStagedSelection Select(TowerStagedDefinition d, IReadOnlyList<TowerBalanceEvidence> first)
    {
        Validate(d); var anchors = d.AnchorIds.ToHashSet(StringComparer.Ordinal);
        var expected = d.Cells.Where(c => !anchors.Contains(c.Id)).ToArray();
        CheckEvidence(d,expected,1,first);
        var breaches = first.Where(e => Wins(e)*2 > e.Trials.Count).Select(e => e.CellId).Order(StringComparer.Ordinal).ToArray();
        var selected = anchors.Concat(first.Where(e => Interval(Wins(e),e.Trials.Count,expected.Length).Upper > .5).Select(e => e.CellId))
            .Distinct().Order(StringComparer.Ordinal).ToArray();
        var status = breaches.Length > 0 ? "FirstStageCeilingBreach" : selected.Length > d.MaximumSecondStageCells ? "SecondStageCapacityExceeded" : "Proceed";
        return new(status,selected,breaches,HarnessJson.Hash(first.OrderBy(e => e.CellId,StringComparer.Ordinal).ToArray()));
    }

    public static TowerStagedReport Evaluate(TowerStagedDefinition d, IReadOnlyList<TowerBalanceEvidence> first,
        IReadOnlyList<TowerBalanceEvidence> second)
    {
        var maximum = Validate(d); var selection = Select(d,first); var firstById = first.ToDictionary(e => e.CellId);
        var selected = selection.SecondStageIds.ToHashSet(StringComparer.Ordinal);
        var expectedSecond = selection.Status == "Proceed" ? d.Cells.Where(c => selected.Contains(c.Id)).ToArray() : [];
        CheckEvidence(d,expectedSecond,2,second); var secondById = second.ToDictionary(e => e.CellId);
        var rows = d.Cells.Select(c => {
            var stage = secondById.ContainsKey(c.Id) ? 2 : firstById.ContainsKey(c.Id) && !selected.Contains(c.Id) ? 1 : (int?)null;
            // Pending cells keep their first-stage observations visible, but never provide final acceptance bounds.
            var evidence = secondById.GetValueOrDefault(c.Id) ?? firstById.GetValueOrDefault(c.Id);
            var wins = evidence is null ? (int?)null : Wins(evidence);
            var interval = stage is null ? null : Interval(wins!.Value,evidence!.Trials.Count,stage==1 ? first.Count : second.Count);
            return new TowerStagedCheck(c.Id,c.CohortId,stage,wins,evidence?.Trials.Count,interval,
                evidence is not null && wins*2 > evidence.Trials.Count,evidence?.ArtifactHash);
        }).ToArray();
        var outcome = selection.Status == "FirstStageCeilingBreach" || rows.Any(c => c.ObservedAboveCeiling) ? GoalOutcome.Fail
            : selection.Status != "Proceed" ? GoalOutcome.Inconclusive
            : d.Cohorts.Any(g => rows.Where(c => c.CohortId==g.Id).All(c => c.Adjusted!.Upper < .1)) ? GoalOutcome.Fail
            : rows.All(c => c.Adjusted!.Upper <= .5) && d.Cohorts.All(g => rows.Any(c => c.CohortId==g.Id && c.Adjusted!.Lower >= .1)) ? GoalOutcome.Pass
            : GoalOutcome.Inconclusive;
        return new(Policy,HarnessJson.Hash(d),outcome,outcome switch { GoalOutcome.Pass=>0,GoalOutcome.Fail=>1,GoalOutcome.Invalid=>2,_=>3 },
            d.Cells.Count,first.Count,second.Count,first.Sum(e=>e.Trials.Count)+second.Sum(e=>e.Trials.Count),maximum,selection,rows,
            "Entire frozen family; alpha .025 per stage. Anchors skip the short look and always require the larger fresh sample. Unresolved first-stage cells also receive that sample. Stage-two Bonferroni uses its complete selected family, conditional on first-stage data and independent new seeds. No pooling, optional extension or dropped cells. Any completed-stage observed rate above 50% rejects acceptance. Fixed-look Wilson coverage is approximate; no claim about unsearched builds or lifetime repeated studies.");
    }

    private static int Wins(TowerBalanceEvidence e) => e.Trials.Count(t => t.Outcome==BattleOutcome.Victory);
    private static void CheckEvidence(TowerStagedDefinition d, IReadOnlyList<TowerStagedCell> expected, int stage, IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        if (evidence is null || evidence.Count != expected.Count || evidence.Any(e => e is null || e.CellId is null)
            || evidence.Select(e=>e.CellId).Distinct().Count()!=evidence.Count
            || !evidence.Select(e=>e.CellId).Order().SequenceEqual(expected.Select(c=>c.Id).Order()))
            throw new InvalidDataException("Missing, duplicate or undeclared staged evidence.");
        var byId=evidence.ToDictionary(e=>e.CellId); var cohorts=d.Cohorts.ToDictionary(c=>c.Id);
        foreach (var cell in expected)
        {
            var e=byId[cell.Id]; var scenario=Scenario(d,cell,stage);
            if (e.Status!="Complete" || e.Error is not null || e.ScenarioHash!=HarnessJson.Hash(scenario)
                || e.ContentHash!=HarnessJson.Hash(d.ContentHashes) || e.SettingsHash!=d.SettingsHash || e.ExecutionHash!=d.ExecutionHash
                || e.RequiredPartySize!=cohorts[cell.CohortId].RequiredPartySize || !TowerContractJson.Hash(e.ArtifactHash)
                || e.Trials is null || e.Trials.Any(t=>t is null || !Enum.IsDefined(t.Outcome))
                || !e.Trials.Select(t=>t.Seed).SequenceEqual(scenario.Seeds))
                throw new InvalidDataException("Staged evidence does not match its complete frozen recipe/content/settings/execution/schedule.");
        }
    }

    public static RateEstimate Interval(int wins,int samples,int familySize)
    {
        if (samples<1 || wins<0 || wins>samples || familySize is <1 or >10000) throw new ArgumentOutOfRangeException(nameof(samples));
        // Acklam lower-tail approximation; .025 stage alpha split across both tails and every stage cell.
        var q=Math.Sqrt(-2*Math.Log(.0125/familySize));
        var numerator=(((((-7.784894002430293e-3*q-.3223964580411365)*q-2.400758277161838)*q-2.549732539343734)*q+4.374664141464968)*q+2.938163982698783);
        var denominator=((((7.784695709041462e-3*q+.3224671290700398)*q+2.445134137142996)*q+3.754408661907416)*q+1);
        var z=-numerator/denominator; var p=wins/(double)samples; var scale=1+z*z/samples;
        var center=(p+z*z/(2*samples))/scale;
        var width=z*Math.Sqrt(p*(1-p)/samples+z*z/(4d*samples*samples))/scale;
        return new(p,Math.Max(0,center-width),Math.Min(1,center+width),1-.025/familySize);
    }

    public static string Markdown(TowerStagedReport r)
    {
        var text=new StringBuilder($"# Staged Tower confirmation: {r.Assessment}\n\n{r.Scope}\n\nFamily: {r.FamilySize}; first/second cells: {r.FirstStageCells}/{r.SecondStageCells}; logical fights: {r.LogicalTrials}/{r.MaximumLogicalTrials} maximum.\n\nSelection: {r.Selection.Status}.\n\n| Cell | Stage | Wins / samples | Adjusted interval |\n| --- | ---: | ---: | --- |\n");
        foreach(var c in r.Cells) text.AppendLine(FormattableString.Invariant($"| {c.Id} | {c.Stage?.ToString() ?? "pending"} | {c.Wins}/{c.Samples} | {(c.Adjusted is null ? "unresolved" : FormattableString.Invariant($"{c.Adjusted.Lower:P2}–{c.Adjusted.Upper:P2}"))} |"));
        return text.ToString();
    }
}
