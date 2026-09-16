using System.Globalization;

namespace BalanceHarness;

public sealed record BossFillerDiversityPass(string CoreId, string? RequiredKind, int Ordinal,
    string ProviderOrderHash, string ExposureHash, BossJointLoadoutResult Result);
public sealed record BossFillerDiversityPool(IReadOnlyList<IReadOnlyList<string>> Recipes,
    IReadOnlyList<BossJointCoreConstruction> Cores, IReadOnlyList<BossFillerDiversityPass> Passes);

/// <summary>Diversify construction fillers without combat feedback or changing equipped Essence order.</summary>
public static class TowerFillerDiversitySearch
{
    public const string Version = "independent-team-filler-diverse-v1";
    public const string Method = "team-filler-diverse";
    public const string Operator = "fresh-team-filler-diverse";

    public static BossFillerDiversityPool ConstructPool(IReadOnlyDictionary<string, string> families,
        IReadOnlyList<BossCoverageFeature> coverage, IReadOnlyList<BossMechanicCore> cores, int essenceSlots,
        IReadOnlyDictionary<string, int>? availableCopies = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(families); ArgumentNullException.ThrowIfNull(coverage); ArgumentNullException.ThrowIfNull(cores);
        cancellationToken.ThrowIfCancellationRequested();
        if (families.Count is < 1 or > TowerJointLoadoutConstructor.MaximumProviders
            || families.Any(p => string.IsNullOrWhiteSpace(p.Key) || string.IsNullOrWhiteSpace(p.Value))
            || cores.Count > TowerJointStructuralSearch.MaximumCores || cores.Any(c => c is null)
            || cores.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != cores.Count)
            throw new InvalidDataException("Filler diversity requires bounded providers and unique authored cores.");

        var exposure = families.Keys.Order(StringComparer.Ordinal).ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
        var recipes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var summaries = new List<BossJointCoreConstruction>(); var passes = new List<BossFillerDiversityPass>();
        foreach (var core in cores.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            var states = 0; var recorded = 0; var allExhausted = true; var focuses = 0;
            foreach (var kind in new string?[] { null }.Concat(TowerTeamCoverageSearch.RequiredKindsPerTeam.Order(StringComparer.Ordinal)))
            {
                if (states == 256) { allExhausted = false; break; }
                var exhausted = false;
                for (var ordinal = 0; ordinal < (kind is null ? 1 : 3) && states < 256; ordinal++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var order = families.Keys.OrderBy(id => exposure[id]).ThenBy(id => HarnessJson.Hash(new[] {
                        Version, core.Id, kind ?? "", ordinal.ToString(CultureInfo.InvariantCulture), id }), StringComparer.Ordinal)
                        .ThenBy(id => id, StringComparer.Ordinal).ToArray();
                    var exposureHash = HarnessJson.Hash(exposure);
                    var result = TowerJointLoadoutConstructor.ConstructCompleteInProviderOrder(families, coverage, core.EssenceIds,
                        kind is null ? [] : [kind], essenceSlots, order, availableCopies, 256 - states, 1, cancellationToken);
                    states += result.VisitedStates; recorded += result.Recipes.Count;
                    passes.Add(new(core.Id, kind, ordinal, HarnessJson.Hash(order), exposureHash, result));
                    foreach (var recipe in result.Recipes)
                        if (recipes.TryAdd(HarnessJson.Hash(recipe.EssenceIds), recipe.EssenceIds))
                            foreach (var id in recipe.EssenceIds.Where(id => !core.EssenceIds.Contains(id))) exposure[id]++;
                    exhausted = result.SearchExhausted;
                    if (exhausted) break;
                }
                allExhausted &= exhausted; focuses++;
            }
            allExhausted &= focuses == 6;
            summaries.Add(new(core.Id, allExhausted, allExhausted ? "exhausted" : states == 256 ? "state-limit" : "focus-recipe-limit", states, recorded));
        }
        return new(recipes.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Value).ToArray(), summaries, passes);
    }
}
