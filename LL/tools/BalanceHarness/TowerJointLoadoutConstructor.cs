namespace BalanceHarness;

public sealed record BossJointLoadoutWitness(string Kind, string EssenceId, IReadOnlyList<string> EvidenceKeys);
public sealed record BossJointLoadout(IReadOnlyList<string> EssenceIds, IReadOnlyList<BossJointLoadoutWitness> Coverage);
public sealed record BossJointLoadoutResult(bool Feasible, bool SearchExhausted, string StopReason,
    int VisitedStates, IReadOnlyList<BossJointLoadout> Recipes);

/// <summary>
/// Bounded structural role coverage around an authored core. No combat scoring, RNG or party allocation.
/// Coverage witnesses describe authored effects, not targeting compatibility, uptime or strength.
/// </summary>
public static class TowerJointLoadoutConstructor
{
    public const int MaximumProviders = 128;
    public const int MaximumStates = 4096;
    public const int MaximumRecipes = 256;

    public static BossJointLoadoutResult Construct(IReadOnlyDictionary<string, string> families,
        IReadOnlyList<BossCoverageFeature> coverage, IReadOnlyList<string> anchor,
        IReadOnlyList<string> requiredKinds, int essenceSlots,
        IReadOnlyDictionary<string, int>? availableCopies = null, int maximumStates = 256,
        int maximumRecipes = 16, CancellationToken cancellationToken = default)
        => ConstructCore(families, coverage, anchor, requiredKinds, essenceSlots, availableCopies, maximumStates, maximumRecipes, false, cancellationToken);

    /// <summary>Fill every slot, optionally requiring selected roles on this character.</summary>
    public static BossJointLoadoutResult ConstructComplete(IReadOnlyDictionary<string, string> families,
        IReadOnlyList<BossCoverageFeature> coverage, IReadOnlyList<string> anchor,
        IReadOnlyList<string> requiredKinds, int essenceSlots,
        IReadOnlyDictionary<string, int>? availableCopies = null, int maximumStates = 256,
        int maximumRecipes = 16, CancellationToken cancellationToken = default)
        => ConstructCore(families, coverage, anchor, requiredKinds, essenceSlots, availableCopies, maximumStates, maximumRecipes, true, cancellationToken);

    /// <summary>Use an explicit construction traversal; emitted Essence order remains canonical.</summary>
    public static BossJointLoadoutResult ConstructCompleteInProviderOrder(IReadOnlyDictionary<string, string> families,
        IReadOnlyList<BossCoverageFeature> coverage, IReadOnlyList<string> anchor, IReadOnlyList<string> requiredKinds,
        int essenceSlots, IReadOnlyList<string> providerOrder, IReadOnlyDictionary<string, int>? availableCopies = null,
        int maximumStates = 256, int maximumRecipes = 16, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(providerOrder);
        return ConstructCore(families, coverage, anchor, requiredKinds, essenceSlots, availableCopies,
            maximumStates, maximumRecipes, true, cancellationToken, providerOrder);
    }

    private static BossJointLoadoutResult ConstructCore(IReadOnlyDictionary<string, string> families,
        IReadOnlyList<BossCoverageFeature> coverage, IReadOnlyList<string> anchor, IReadOnlyList<string> requiredKinds,
        int essenceSlots, IReadOnlyDictionary<string, int>? availableCopies, int maximumStates, int maximumRecipes,
        bool complete, CancellationToken cancellationToken, IReadOnlyList<string>? providerOrder = null)
    {
        ArgumentNullException.ThrowIfNull(families);
        ArgumentNullException.ThrowIfNull(coverage);
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(requiredKinds);
        cancellationToken.ThrowIfCancellationRequested();
        if (families.Count is < 1 or > MaximumProviders || essenceSlots is < 1 or > 5
            || maximumStates is < 1 or > MaximumStates || maximumRecipes is < 1 or > MaximumRecipes
            || families.Any(p => string.IsNullOrWhiteSpace(p.Key) || string.IsNullOrWhiteSpace(p.Value))
            || requiredKinds.Count < (complete ? 0 : 1) || requiredKinds.Count > 5 || requiredKinds.Distinct(StringComparer.Ordinal).Count() != requiredKinds.Count
            || requiredKinds.Any(k => !TowerPartyCoverage.Kinds.Contains(k))
            || anchor.Count > essenceSlots || anchor.Distinct(StringComparer.Ordinal).Count() != anchor.Count
            || anchor.Any(id => !families.ContainsKey(id))
            || coverage.Count > MaximumProviders * TowerPartyCoverage.Kinds.Length
            || coverage.Any(f => f is null || !families.ContainsKey(f.EssenceId) || !TowerPartyCoverage.Kinds.Contains(f.Kind)
                || f.EvidenceKeys is not { Count: > 0 } || f.EvidenceKeys.Any(string.IsNullOrWhiteSpace))
            || coverage.Select(f => (f.EssenceId, f.Kind)).Distinct().Count() != coverage.Count
            || availableCopies is not null && availableCopies.Any(p => !families.ContainsKey(p.Key) || p.Value < 0)
            || providerOrder is not null && (providerOrder.Count != families.Count
                || providerOrder.Distinct(StringComparer.Ordinal).Count() != families.Count
                || providerOrder.Any(id => id is null || !families.ContainsKey(id))))
            throw new InvalidDataException("Invalid joint loadout bounds or authored inputs.");

        var ids = providerOrder?.ToArray() ?? families.Keys.Order(StringComparer.Ordinal).ToArray();
        var kinds = requiredKinds.Order(StringComparer.Ordinal).ToArray();
        var features = coverage.ToDictionary(f => (f.EssenceId, f.Kind));
        var masks = ids.ToDictionary(id => id, id => kinds.Select((kind, index) =>
            features.ContainsKey((id, kind)) ? 1 << index : 0).Aggregate(0, (a, b) => a | b), StringComparer.Ordinal);
        var target = (1 << kinds.Length) - 1;
        var recipes = new List<BossJointLoadout>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        string? stop = null;
        bool Available(string id) => availableCopies is null || availableCopies.GetValueOrDefault(id) > 0;
        if (anchor.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != anchor.Count
            || anchor.Any(id => !Available(id)))
            return new(false, true, "incompatible-anchor", 0, recipes);

        void Visit(string[] chosen)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stop is not null) return;
            // Length-prefixed IDs avoid ambiguous keys without constraining authored identifiers.
            var key = string.Concat(chosen.Select(id => id.Length + ":" + id));
            if (visited.Contains(key)) return;
            if (visited.Count == maximumStates) { stop = "state-limit"; return; }
            visited.Add(key);
            var mask = chosen.Aggregate(0, (value, id) => value | masks[id]);
            if (mask == target && (!complete || chosen.Length == essenceSlots))
            {
                // Stop only when another distinct solution exceeds the result budget.
                if (recipes.Count == maximumRecipes) { stop = "recipe-limit"; return; }
                recipes.Add(new(chosen, kinds.Select(kind => {
                    var id = chosen.First(candidate => features.ContainsKey((candidate, kind)));
                    return new BossJointLoadoutWitness(kind, id, features[(id, kind)].EvidenceKeys
                        .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
                }).ToArray()));
                return;
            }
            if (chosen.Length == essenceSlots) return;
            var compatible = ids.Where(id => Available(id) && !chosen.Contains(id)
                && !chosen.Any(other => StringComparer.OrdinalIgnoreCase.Equals(families[other], families[id]))).ToArray();
            // Every feasible extension must supply every missing role. Branch on the most constrained one.
            var providers = mask == target ? compatible : kinds.Select((kind, index) => (kind, index)).Where(k => (mask & (1 << k.index)) == 0)
                .Select(k => new { k.kind, Ids = compatible.Where(id => (masks[id] & (1 << k.index)) != 0).ToArray() })
                .OrderBy(k => k.Ids.Length).ThenBy(k => k.kind, StringComparer.Ordinal).First().Ids;
            foreach (var id in providers)
            {
                Visit(chosen.Append(id).Order(StringComparer.Ordinal).ToArray());
                if (stop is not null) break;
            }
        }

        Visit(anchor.Order(StringComparer.Ordinal).ToArray());
        return new(recipes.Count > 0, stop is null, stop ?? "exhausted", visited.Count, recipes);
    }
}
