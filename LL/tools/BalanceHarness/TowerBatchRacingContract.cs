using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Combat;

namespace BalanceHarness;

// This contract accepts already constructed teams. It neither generates proposals nor
// authorizes combat. The scope supplies legality and combat identity, not old outcomes.
public sealed record TowerBatchRacingPlan(string Version, TowerBossDiscoveryDefinition Scope,
    IReadOnlyList<PartyChoice> FirstWave, IReadOnlyList<PartyChoice> SecondWave,
    IReadOnlyList<TowerRacingPanel> Panels, int MaximumEvaluations);
public sealed record TowerRacingPanel(string Role, IReadOnlyList<int> Seeds);
public sealed record TowerPanelFreeze(string Version, string PlanHash, string ScopeHash, int Index,
    string Role, string Context, IReadOnlyList<int> Seeds, IReadOnlyList<PartyChoice> Parties,
    int EvaluationsBefore, int PlannedEvaluations);
public sealed record TowerPanelTrial(string ScopeHash, string PanelHash, string Role, string PartyId,
    int Ordinal, int Seed, TowerScenario Scenario);
public sealed record TowerPanelOutcome(string RequestHash, string TrialId, int Seed,
    BattleOutcome Outcome, double GuardianHealth, double Survival, double DurationSeconds);
public sealed record TowerPanelObservation(TowerPanelTrial Request, TowerPanelOutcome Outcome);
public sealed record TowerPanelScore(string Id, int Samples, int Wins, int Draws, BossDiscoveryFitness Fitness);
public sealed record TowerPanelContrast(string PartyId, string ReferenceId, int Samples, int GainedWins, int LostWins);
public sealed record TowerPanelEvaluation(TowerPanelFreeze Freeze, bool Complete,
    IReadOnlyList<TowerPanelObservation> Observations, IReadOnlyList<TowerPanelScore> Scores,
    IReadOnlyList<TowerPanelContrast> Contrasts);
public sealed record TowerRacingDecision(int Wave, IReadOnlyList<string> EliteIds, string DiversityId,
    int DiversityDistance, int CompetitiveCutoffWins, bool DiversityFallback,
    IReadOnlyList<string> SurvivorIds, IReadOnlyList<string> PrunedIds,
    IReadOnlyList<TowerPanelScore> CommonScores, IReadOnlyList<string> BeamIds);
public sealed record TowerBatchRacingReport(string Version, string PlanHash, string Status,
    int PlannedEvaluations, int ChargedEvaluations, IReadOnlyList<TowerPanelEvaluation> Panels,
    IReadOnlyList<TowerRacingDecision> Decisions, IReadOnlyList<string> Nominees,
    string? RawSelectedId, string? Error,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerBenchmarkValidationFreeze? ValidationFreeze = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerBenchmarkValidationDecision? ValidationDecision = null);

public static partial class TowerBatchRacing
{
    public const string Version = "tower-frozen-batch-racing-v1";
    public const int PlannedEvaluations = 528;
    public const int RungSamples = 8;
    public const int SelectionSamples = 40;
    private static readonly string[] Roles = ["wave-1-screen", "wave-1-continuation",
        "wave-2-screen", "wave-2-continuation", "selection"];

    internal static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(
        JsonSerializer.Serialize(value, HarnessJson.Options), HarnessJson.Options)!;

    public static void Validate(TowerBatchRacingPlan plan)
    {
        if (plan is null || plan.Version != Version || plan.Scope is null
            || plan.FirstWave is not { Count: 9 } || plan.SecondWave is not { Count: 8 }
            || plan.Panels is not { Count: 5 }
            || plan.MaximumEvaluations is < PlannedEvaluations or > 100000)
            throw new InvalidDataException("Racing requires version 1, nine then eight frozen challengers and a budget of at least 528.");
        ValidateScopeAndPanels(plan.Scope, plan.Panels, plan.MaximumEvaluations);
        ValidateParties(plan.Scope, plan.Scope.Starts.Select(s => s.Party).Concat(plan.FirstWave).Concat(plan.SecondWave).ToArray());
    }

    internal static void ValidateScopeAndPanels(TowerBossDiscoveryDefinition d, IReadOnlyList<TowerRacingPanel> panels, int maximum,
        bool benchmarkValidation = false)
    {
        var roles = benchmarkValidation ? TowerBenchmarkValidation.PanelRoles : Roles;
        if (d is null || panels is null || panels.Count != roles.Count || maximum is < PlannedEvaluations or > 100000)
            throw new InvalidDataException("Racing requires a scope, the versioned panel schedule and a budget of at least 528.");
        ValidateScope(d);
        for (var i = 0; i < roles.Count; i++)
            if (panels[i] is not { } panel || panel.Role != roles[i] || panel.Seeds is null
                || panel.Seeds.Count != (i < 4 ? RungSamples : benchmarkValidation
                    ? i == 4 ? TowerBenchmarkValidation.NominationSamples : TowerBenchmarkValidation.ValidationSamples
                    : SelectionSamples))
                throw new InvalidDataException("Racing panels must have the declared role, order and sample count.");
        var seeds = panels.SelectMany(p => p.Seeds).ToArray();
        var reserved = d.ExcludedCombatSeeds.Concat(d.Generation.Seeds)
            .Concat(d.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection)
                .Concat(s.Confirmation).Concat(s.Diagnostics).Concat(s.Feedback ?? [])))
            .Concat(d.References.SelectMany(r => r.Scenario.Seeds)).ToHashSet();
        if (seeds.Distinct().Count() != seeds.Length || seeds.Any(reserved.Contains))
            throw new InvalidDataException("Racing panels must be mutually fresh and disjoint from historical, legacy and held-out seeds.");
    }

    internal static void ValidateScope(TowerBossDiscoveryDefinition d)
    {
        TowerBossDiscovery.Validate(d);
        if (d.Contexts.Count != 1 || d.Starts.Count != 3 || d.References.Count != 3
            || d.Stages.SelectionPolicyVersion != TowerBossStudyPolicy.IncumbentTieVersion
            || d.Stages.SelectionPrimaryReferenceId is null
            || d.Starts.Count(s => s.ReferenceId == d.Stages.SelectionPrimaryReferenceId) != 1)
            throw new InvalidDataException("Racing requires one context, three bound references and the existing incumbent-tie selector.");
        ValidateParties(d, d.Starts.Select(s => s.Party).ToArray());
    }

    internal static void ValidateParties(TowerBossDiscoveryDefinition d, IReadOnlyList<PartyChoice> parties)
    {
        foreach (var party in parties)
        {
            TowerBossDiscovery.ValidateParty(d, party);
            if (party.Builds.Values.Any(ids => !TowerCompositionSearch.IsCanonical(ids)))
                throw new InvalidDataException("Racing requires canonical owner-preserving compositions.");
        }
        if (parties.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != parties.Count)
            throw new InvalidDataException("Frozen batches must contain distinct new recipes, without reference or earlier-wave clones.");
    }

    private static void ValidateOutcome(TowerPanelTrial request, TowerPanelOutcome outcome, ISet<string> trials)
    {
        if (outcome is null || outcome.RequestHash != HarnessJson.Hash(request) || outcome.Seed != request.Seed
            || string.IsNullOrWhiteSpace(outcome.TrialId) || !Enum.IsDefined(outcome.Outcome)
            || !double.IsFinite(outcome.GuardianHealth) || outcome.GuardianHealth is < 0 or > 100
            || !double.IsFinite(outcome.Survival) || outcome.Survival is < 0 or > 100
            || !double.IsFinite(outcome.DurationSeconds) || outcome.DurationSeconds < 0
            || !trials.Add(outcome.TrialId))
            throw new InvalidDataException("Panel outcome has mismatched identity, duplicate trial evidence or invalid telemetry.");
    }
}
