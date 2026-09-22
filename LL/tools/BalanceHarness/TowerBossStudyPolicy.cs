using Domain.Models.Combat;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record BossFinalist(PartyChoice Party, bool Primary, string CapabilityPattern, string BehaviorPattern, string Reason);
public sealed record BossConfirmationMember(string CellId, string Context, IReadOnlyList<string> GeneratedIds,
    IReadOnlyList<string> ReferenceIds, bool Primary);
public sealed record BossIncumbentSelection(string ReferenceId, string PartyId, string Reason);
public sealed record BossConfirmationFreeze(string PolicyVersion, string ShortlistHash, string SelectionHash, int AfterTrialCount,
    TowerBalanceDefinition Definition, IReadOnlyList<BossConfirmationMember> Members,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BossIncumbentSelection? IncumbentSelection = null);
public sealed record BossStudyComparison(string GeneratedCell, string ReferenceCell, string ReferenceId, string Context,
    int GainedWins, int LostWins, PairedEstimate Difference);
public sealed record BossEarlierBreach(string Stage, string PartyId, string Context, int Wins, int Samples, bool Confirmed);
public sealed record BossStudyConclusion(GoalOutcome GeneratedViability, GoalOutcome OverallAssessment,
    IReadOnlyList<BossEarlierBreach> EarlierBreaches, IReadOnlyList<string> Notes);

/// <summary>Selection sees frozen shortlist measurements, which may include supplied incumbents.
/// Independent confirmation measurements enter only after finalists freeze.</summary>
public static class TowerBossStudyPolicy
{
    public const string Version = "tower-staged-confirmation-v1";
    public const string ZeroWinVersion = "tower-staged-zero-win-health-v1";
    public const string IncumbentTieVersion = "tower-staged-incumbent-tie-v1";
    public const double AlternativeMargin = .10;

    public static string BehaviorPattern(BossBehavior b)
    {
        // Coarse, predeclared observational bins avoid claiming a new strategy from tiny numeric/order changes.
        int Magnitude(double value) => value <= 0 ? 0 : 1 + (int)Math.Floor(Math.Log2(1 + value));
        return HarnessJson.Hash(new[] { (int)Math.Floor(b.HealthDeficit * 10), Magnitude(b.DamagePrevented),
            Magnitude(b.Healing + (b.Recovery?.FriendlyRegeneration ?? 0)), (int)(b.DeniedTicks / 100),
            (int)(b.SummonActiveTicks / 100), Magnitude((b.Recovery?.GuardianHealing ?? 0) + (b.Recovery?.GuardianRegeneration ?? 0)) });
    }

    public static IReadOnlyList<BossFinalist> Select(BossDiscoveryInputs inputs, BossGenerationMechanics mechanics,
        IReadOnlyList<PartyChoice> shortlist, IReadOnlyList<BossDiscoveryMeasurement> selection,
        IReadOnlyDictionary<string, IReadOnlyList<int>> selectionSeeds, int maximum, string policyVersion = Version)
        => SelectCore(inputs, mechanics, shortlist, selection, selectionSeeds, maximum, policyVersion, null);

    // The new selector receives its designation only through a validated, frozen definition.
    // Generation inputs intentionally contain no reference IDs or historical fitness.
    public static IReadOnlyList<BossFinalist> Select(TowerBossDiscoveryDefinition definition, BossGenerationMechanics mechanics,
        IReadOnlyList<PartyChoice> shortlist, IReadOnlyList<BossDiscoveryMeasurement> selection)
    {
        var inputs = TowerBossImprovement.Inputs(definition); // Validates reference/recipe binding and practical scope.
        var stages = definition.Stages;
        if (stages.SelectionPolicyVersion == IncumbentTieVersion
            && (shortlist.Count != stages.Shortlist || definition.Starts.Any(s => !shortlist.Any(p => p.Id == s.Party.Id))))
            throw new InvalidDataException("Incumbent tie selection requires all four nominees, including both protected supplied teams.");
        var primary = stages.SelectionPrimaryReferenceId is null ? null
            : definition.Starts.Single(s => s.ReferenceId == stages.SelectionPrimaryReferenceId).Party.Id;
        return SelectCore(inputs, mechanics, shortlist, selection, stages.Schedules.ToDictionary(p => p.Key, p => p.Value.Selection),
            stages.GeneratedFinalists, stages.SelectionPolicyVersion, primary);
    }

    private static IReadOnlyList<BossFinalist> SelectCore(BossDiscoveryInputs inputs, BossGenerationMechanics mechanics,
        IReadOnlyList<PartyChoice> shortlist, IReadOnlyList<BossDiscoveryMeasurement> selection,
        IReadOnlyDictionary<string, IReadOnlyList<int>> selectionSeeds, int maximum, string policyVersion, string? primaryPartyId)
    {
        if (policyVersion is not (Version or ZeroWinVersion or IncumbentTieVersion)) throw new InvalidDataException("Unknown staged selection policy.");
        if (policyVersion == IncumbentTieVersion && (primaryPartyId is null || !shortlist.Any(p => p.Id == primaryPartyId)))
            throw new InvalidDataException("Incumbent tie selection requires the designated supplied primary on the frozen shortlist.");
        if (maximum is < 1 or > 5 || shortlist.Count == 0 || shortlist.Count != selection.Count
            || shortlist.Select(p => p.Id).Distinct().Count() != shortlist.Count || selection.Select(p => p.Id).Distinct().Count() != selection.Count
            || !shortlist.Select(p => p.Id).Order().SequenceEqual(selection.Select(p => p.Id).Order()))
            throw new InvalidDataException("Selection requires every frozen generated shortlist member exactly once.");
        var generator = new TowerBossPartyGenerator(inputs, mechanics);
        foreach (var party in shortlist) if (generator.Invalid(party) is not null) throw new InvalidDataException("Illegal shortlisted party.");
        foreach (var row in selection)
            if (row.Fitness != TowerBossGeneration.Fitness(inputs with { DiscoverySeeds = selectionSeeds }, row.Cells, row.Fitness.VictoryDuration)
                || row.Behavior is null || new[] { row.Behavior.HealthDeficit, row.Behavior.Healing, row.Behavior.DamagePrevented,
                    row.Behavior.DeniedTicks, row.Behavior.SummonActiveTicks, row.Behavior.Recovery?.FriendlyRegeneration ?? 0,
                    row.Behavior.Recovery?.GuardianHealing ?? 0, row.Behavior.Recovery?.GuardianRegeneration ?? 0 }.Any(n => !double.IsFinite(n) || n < 0))
                throw new InvalidDataException("Invalid or incomplete selection measurement.");
        var parties = shortlist.ToDictionary(p => p.Id);
        var discoveryRanks = shortlist.Select((p, i) => (p.Id, Rank: i + 1)).ToDictionary(p => p.Id, p => p.Rank);
        var winsPolicy = policyVersion is ZeroWinVersion or IncumbentTieVersion;
        var ranked = winsPolicy ? TowerZeroWinSelection.Rank(selection,
            r => r.Cells.Sum(c => c.Clears.Count(won => won)),
            r => r.Cells.OrderBy(c => c.Context, StringComparer.Ordinal).Sum(c => c.GuardianHealth * c.Clears.Count) / r.Cells.Sum(c => c.Clears.Count),
            r => discoveryRanks[r.Id], r => r.Id) : TowerBossGeneration.Rank(selection);
        var primary = ranked.First();
        var reason = winsPolicy ? "Primary selected by selection wins; only zero-win ties use mean guardian health, then frozen discovery rank and stable ID."
            : "Primary selected by target win rate, boss progress, survival, winning duration and stable ID.";
        if (policyVersion == IncumbentTieVersion)
        {
            var topWins = primary.Cells.Sum(c => c.Clears.Count(won => won));
            var tied = ranked.Where(r => r.Cells.Sum(c => c.Clears.Count(won => won)) == topWins).ToArray();
            var incumbent = tied.SingleOrDefault(r => r.Id == primaryPartyId);
            if (topWins > 0 && tied.Length > 1 && incumbent is not null)
            {
                primary = incumbent;
                reason = "Designated supplied primary retained on a positive maximum selection-win tie.";
            }
            else reason = topWins == 0 ? "All nominees had zero selection wins; mean guardian health, frozen discovery rank and stable ID selected the primary."
                : tied.Length == 1 ? "Unique maximum selection wins selected the primary; incumbent tie preference did not apply."
                : "The designated supplied primary was below the positive maximum selection-win tie; frozen discovery rank and stable ID selected the primary.";
        }
        BossFinalist Finalist(BossDiscoveryMeasurement row, bool first) => new(parties[row.Id], first,
            generator.CapabilityPattern(parties[row.Id]), BehaviorPattern(row.Behavior), first ? reason
                : "Distinct capability and observed behavior patterns within ten percentage points of the selection primary; not a causal strategy claim.");
        var result = new List<BossFinalist> { Finalist(primary, true) };
        var alternatives = winsPolicy ? ranked : selection.OrderByDescending(r => r.Fitness.WorstContextWinRate).ThenBy(r => r.Id, StringComparer.Ordinal);
        foreach (var row in alternatives.Where(r => r.Id != primary.Id && r.Fitness.WorstContextWinRate + AlternativeMargin + 1e-12 >= primary.Fitness.WorstContextWinRate))
        {
            if (result.Count >= maximum) break;
            var candidate = Finalist(row, false);
            if (result.All(r => r.CapabilityPattern != candidate.CapabilityPattern && r.BehaviorPattern != candidate.BehaviorPattern)) result.Add(candidate);
        }
        return result;
    }

    public static BossConfirmationFreeze Freeze(TowerBossDiscoveryDefinition d, IReadOnlyList<PartyChoice> shortlist,
        IReadOnlyList<BossDiscoveryMeasurement> selection, IReadOnlyList<BossFinalist> finalists, int trials)
    {
        TowerBossDiscovery.Validate(d);
        if (finalists.Count is < 1 or > 5 || finalists.Count > d.Stages.GeneratedFinalists || !finalists[0].Primary
            || finalists.Skip(1).Any(f => f.Primary) || finalists.Select(f => f.Party.Id).Distinct().Count() != finalists.Count)
            throw new InvalidDataException("Invalid frozen generated finalists.");
        var cohorts = d.Contexts.OrderBy(c => c.Id, StringComparer.Ordinal).Select(c => new TowerBalanceCohort(
            "cohort-" + HarnessJson.Hash(c.Id)[..20], d.Budget, d.RequiredPartySize, c.Id,
            TowerBossDiscovery.EquipmentBudgetHash(c.CharacterTemplates), d.BudgetPurpose)).ToArray();
        var cells = new List<TowerBalanceCellDefinition>(); var members = new List<BossConfirmationMember>();
        void Add(string context, TowerScenario scenario, string? generated, string? reference, bool primary)
        {
            // The experiment label and new seeds are common; ordered builds, equipment and actor identity vectors remain exact.
            scenario = scenario with { Id = d.Id, Seeds = d.Stages.Schedules[context].Confirmation };
            var id = "cell-" + HarnessJson.Hash(new { Context = context, Recipe = TowerBossDiscovery.RecipeHash(scenario.Party) });
            var existing = members.FindIndex(m => m.CellId == id);
            if (existing >= 0)
            {
                var member = members[existing];
                members[existing] = member with { GeneratedIds = member.GeneratedIds.Concat(generated is null ? [] : new[] { generated }).Distinct().Order(StringComparer.Ordinal).ToArray(),
                    ReferenceIds = member.ReferenceIds.Concat(reference is null ? [] : new[] { reference }).Distinct().Order(StringComparer.Ordinal).ToArray(), Primary = member.Primary || primary };
                return;
            }
            cells.Add(new(id, cohorts.Single(c => c.Context == context).Id, generated is null ? "reference" : "generated", scenario, scenario.Seeds.Count));
            members.Add(new(id, context, generated is null ? [] : [generated], reference is null ? [] : [reference], primary));
        }
        foreach (var finalist in finalists)
        foreach (var context in d.Contexts.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            TowerBossDiscovery.ValidateParty(d, finalist.Party);
            Add(context.Id, TowerBossDiscovery.Scenario(d, context.Id, finalist.Party, d.Stages.Schedules[context.Id].Confirmation), finalist.Party.Id, null, finalist.Primary);
        }
        foreach (var reference in d.References.OrderBy(r => r.Id, StringComparer.Ordinal)) Add(reference.Context, reference.Scenario, null, reference.Id, false);
        var exclusions = d.ExcludedCombatSeeds.Concat(d.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Diagnostics))).Distinct().Order().ToArray();
        var definition = new TowerBalanceDefinition(1, "confirmation-" + HarnessJson.Hash(d.Id)[..20], TowerBalanceEvaluator.IntervalPolicy,
            d.ContentHashes, d.SettingsHash, d.ExecutionHash, cohorts, cells, exclusions, checked(cells.Sum(c => c.Scenario.Seeds.Count)));
        TowerBalanceEvaluator.Validate(definition);
        var designation = d.Stages.SelectionPrimaryReferenceId is { } referenceId
            ? new BossIncumbentSelection(referenceId, d.Starts.Single(s => s.ReferenceId == referenceId).Party.Id, finalists[0].Reason) : null;
        return new(d.Stages.SelectionPolicyVersion, HarnessJson.Hash(shortlist), HarnessJson.Hash(selection), trials, definition, members, designation);
    }

    public static IReadOnlyList<BossStudyComparison> Compare(BossConfirmationFreeze frozen, IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        var rows = evidence.ToDictionary(e => e.CellId); var result = new List<BossStudyComparison>();
        foreach (var generated in frozen.Members.Where(m => m.GeneratedIds.Count > 0))
        foreach (var reference in frozen.Members.Where(m => m.Context == generated.Context && m.ReferenceIds.Count > 0))
        {
            if (!rows.TryGetValue(generated.CellId, out var g) || !rows.TryGetValue(reference.CellId, out var r)
                || g.Status != "Complete" || r.Status != "Complete" || !g.Trials.Select(t => t.Seed).SequenceEqual(r.Trials.Select(t => t.Seed))) continue;
            var pairs = g.Trials.Zip(r.Trials).ToArray();
            var gained = pairs.Count(p => p.First.Outcome == BattleOutcome.Victory && p.Second.Outcome != BattleOutcome.Victory);
            var lost = pairs.Count(p => p.First.Outcome != BattleOutcome.Victory && p.Second.Outcome == BattleOutcome.Victory);
            foreach (var id in reference.ReferenceIds) result.Add(new(generated.CellId, reference.CellId, id, generated.Context,
                gained, lost, PairedStatistics.ClearRate(gained, lost, pairs.Length)));
        }
        return result;
    }

    public static BossStudyConclusion Conclude(TowerBossDiscoveryDefinition d, BossGenerationResult discovery,
        IReadOnlyList<BossDiscoveryMeasurement> selection, BossConfirmationFreeze frozen, TowerBalanceReport assessment)
    {
        var notes = new List<string>(); var breaches = new List<BossEarlierBreach>();
        var recipes = discovery.Arms.SelectMany(a => a.Proposals).Where(p => p.Party is not null).Select(p => p.Party!)
            .Concat(discovery.DiscoveryShortlist).DistinctBy(p => p.Id).ToDictionary(p => p.Id);
        bool Confirmed(string id, string context)
        {
            if (frozen.Members.Any(m => m.Context == context && m.GeneratedIds.Contains(id))) return true;
            if (!recipes.TryGetValue(id, out var party)) return false;
            var hash = TowerBossDiscovery.RecipeHash(TowerBossDiscovery.Scenario(d, context, party, d.Stages.Schedules[context].Confirmation).Party);
            return frozen.Members.Where(m => m.Context == context).Any(m =>
                TowerBossDiscovery.RecipeHash(frozen.Definition.Cells.Single(c => c.Id == m.CellId).Scenario.Party) == hash);
        }
        var discoveryRows = d.Generation.PolicyVersion == TowerEvaluationAllocationSearch.Version
            ? discovery.Arms.SelectMany(TowerEvaluationAllocationSearch.Observations)
            : discovery.Arms.SelectMany(a => a.Evaluations).DistinctBy(r => r.Id);
        foreach (var stage in new[] { (Name: "discovery", Rows: discoveryRows),
            (Name: "selection", Rows: selection.AsEnumerable()) })
        foreach (var row in stage.Rows)
        foreach (var cell in row.Cells.Where(c => c.Clears.Count(x => x) * 2 > c.Clears.Count))
            breaches.Add(new(stage.Name, row.Id, cell.Context, cell.Clears.Count(x => x), cell.Clears.Count,
                Confirmed(row.Id, cell.Context)));
        var viability = new List<GoalOutcome>();
        foreach (var cohort in frozen.Definition.Cohorts)
        {
            var ids = frozen.Members.Where(m => m.Context == cohort.Context && m.GeneratedIds.Count > 0).Select(m => m.CellId).ToHashSet();
            var cells = assessment.Cells.Where(c => ids.Contains(c.Id)).ToArray();
            viability.Add(cells.Length != ids.Count || cells.Any(c => c.Outcome == GoalOutcome.Invalid) ? GoalOutcome.Invalid
                : cells.Any(c => c.LowerSupported) ? GoalOutcome.Pass : cells.All(c => c.AdjustedInterval?.Upper < .10) ? GoalOutcome.Fail : GoalOutcome.Inconclusive);
        }
        var generated = viability.Contains(GoalOutcome.Invalid) ? GoalOutcome.Invalid : viability.Contains(GoalOutcome.Fail) ? GoalOutcome.Fail
            : viability.Contains(GoalOutcome.Inconclusive) ? GoalOutcome.Inconclusive : GoalOutcome.Pass;
        var unresolved = breaches.Any(b => !b.Confirmed);
        var overall = assessment.Assessment == GoalOutcome.Pass && unresolved ? GoalOutcome.Inconclusive : assessment.Assessment;
        if (unresolved) notes.Add("An earlier above-ceiling candidate/context was not confirmed. Coverage remains unresolved; do not add candidates or resample this frozen family to manufacture acceptance.");
        if (generated != GoalOutcome.Pass) notes.Add(d.Mode == TowerBossDiscovery.Independent
            ? "Reference viability alone does not establish that independent generation found a supported viable party in every declared context."
            : "Benchmark viability alone does not establish that retained-build search found a supported viable party in every declared context.");
        if (d.Mode == TowerBossDiscovery.Improve) notes.Add("This is explicitly reference-derived improvement; searched viability is not independent rediscovery or evidence of near-optimality.");
        notes.Add("Generated viability tests the 10% lower threshold only; an above-50% generated party can demonstrate search viability while balance fails.");
        notes.Add("Fresh confirmation assesses the frozen family. Earlier rates remain visible and are not pooled with confirmation. Paired reference differences are descriptive, not acceptance gates.");
        if (d.BudgetPurpose == "diagnostic") notes.Add("This diagnostic budget cannot establish intended progression acceptance.");
        return new(generated, overall, breaches, notes);
    }
}
