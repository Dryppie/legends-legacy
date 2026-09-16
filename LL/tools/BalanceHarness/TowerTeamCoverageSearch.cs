namespace BalanceHarness;

public sealed record BossTeamRoleFocus(string CoreId, string? RequiredKind, int MaximumRecipes, BossJointLoadoutResult Result);
public sealed record BossTeamLoadoutPool(IReadOnlyList<IReadOnlyList<string>> Recipes,
    IReadOnlyList<BossJointCoreConstruction> Cores, IReadOnlyList<BossTeamRoleFocus> Focuses);

/// <summary>Team-wide authored coverage with legal recipe reuse; it makes no combat-strength claim.</summary>
public static class TowerTeamCoverageSearch
{
    public const string Version = "independent-team-coverage-v1";
    public const string Method = "team-coverage";
    public const string Operator = "fresh-team-coverage";
    public const int MinimumDistinctRecipes = 1;
    public const int MaximumUsesPerRecipe = 10;
    public static IReadOnlyList<string> RequiredKindsPerTeam => TowerPartyCoverage.Kinds;

    public static BossTeamLoadoutPool ConstructPool(IReadOnlyDictionary<string, string> families,
        IReadOnlyList<BossCoverageFeature> coverage, IReadOnlyList<BossMechanicCore> cores, int essenceSlots,
        IReadOnlyDictionary<string, int>? availableCopies = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(families); ArgumentNullException.ThrowIfNull(coverage);
        ArgumentNullException.ThrowIfNull(cores); cancellationToken.ThrowIfCancellationRequested();
        if (cores.Count > TowerJointStructuralSearch.MaximumCores || cores.Any(c => c is null)
            || cores.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != cores.Count)
            throw new InvalidDataException("Team coverage requires a bounded, unique authored core catalogue.");
        var recipes = new List<IReadOnlyList<string>>(); var summaries = new List<BossJointCoreConstruction>(); var focuses = new List<BossTeamRoleFocus>();
        foreach (var core in cores.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            var states = 0; var count = 0; var exhausted = true; var completedFocuses = 0;
            foreach (var kind in new string?[] { null }.Concat(RequiredKindsPerTeam.Order(StringComparer.Ordinal)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (states == 256) { exhausted = false; break; }
                var maximum = kind is null ? 1 : 3;
                var result = TowerJointLoadoutConstructor.ConstructComplete(families, coverage, core.EssenceIds,
                    kind is null ? [] : [kind], essenceSlots, availableCopies, 256 - states, maximum, cancellationToken);
                states += result.VisitedStates; count += result.Recipes.Count; exhausted &= result.SearchExhausted; completedFocuses++;
                focuses.Add(new(core.Id, kind, maximum, result)); recipes.AddRange(result.Recipes.Select(r => r.EssenceIds));
            }
            var allExhausted = exhausted && completedFocuses == 6;
            summaries.Add(new(core.Id, allExhausted, allExhausted ? "exhausted" : states == 256 ? "state-limit" : "focus-recipe-limit", states, count));
        }
        return new(recipes.DistinctBy(r => HarnessJson.Hash(r)).OrderBy(r => HarnessJson.Hash(r), StringComparer.Ordinal).ToArray(), summaries, focuses);
    }
}
