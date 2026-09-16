namespace BalanceHarness;

public sealed record BossJointPartySlot(int PartySlot, IReadOnlyList<IReadOnlyList<string>> Recipes);
public sealed record BossJointPartyPlacement(int PartySlot, string RecipeId, IReadOnlyList<string> EssenceIds);
public sealed record BossJointParty(string Id, IReadOnlyList<BossJointPartyPlacement> Placements,
    IReadOnlyDictionary<string, int> UsedCopies, int DistinctRecipes);
public sealed record BossJointPartyResult(bool Feasible, bool SearchExhausted, string StopReason,
    int VisitedStates, int CandidateChecks, IReadOnlyList<BossJointParty> Parties);

/// <summary>Assign complete structural loadouts to character slots under one shared inventory.</summary>
public static class TowerJointPartyAllocator
{
    public const int MaximumRecipesPerSlot = 1024;
    public const int MaximumStates = 4096;
    public const int MaximumCandidateChecks = 1_000_000;
    public const int MaximumParties = 64;
    private sealed record Option(string Id, string[] Essences, int Coverage);

    public static BossJointPartyResult Allocate(IReadOnlyDictionary<string, string> families,
        IReadOnlyList<BossJointPartySlot> slots, int essenceSlots,
        IReadOnlyDictionary<string, int>? availableCopies = null,
        int minimumDistinctRecipes = 1, int maximumUsesPerRecipe = 10,
        int maximumStates = 256, int maximumParties = 16, int maximumCandidateChecks = 250_000,
        CancellationToken cancellationToken = default)
        => AllocateCore(families, slots, essenceSlots, availableCopies, minimumDistinctRecipes, maximumUsesPerRecipe,
            maximumStates, maximumParties, maximumCandidateChecks, false, null, null, cancellationToken);

    /// <summary>Favor recipes not exposed in earlier complete parties; all restarts share the same limits.</summary>
    public static BossJointPartyResult AllocateDiverse(IReadOnlyDictionary<string, string> families,
        IReadOnlyList<BossJointPartySlot> slots, int essenceSlots,
        IReadOnlyDictionary<string, int>? availableCopies = null,
        int minimumDistinctRecipes = 1, int maximumUsesPerRecipe = 10,
        int maximumStates = 256, int maximumParties = 16, int maximumCandidateChecks = 250_000,
        CancellationToken cancellationToken = default)
        => AllocateCore(families, slots, essenceSlots, availableCopies, minimumDistinctRecipes, maximumUsesPerRecipe,
            maximumStates, maximumParties, maximumCandidateChecks, true, null, null, cancellationToken);

    /// <summary>Cover roles across the team, alternating variety and reuse preferences under shared inventory.</summary>
    public static BossJointPartyResult AllocateCovered(IReadOnlyDictionary<string, string> families,
        IReadOnlyList<BossCoverageFeature> coverage, IReadOnlyList<string> requiredKinds,
        IReadOnlyList<BossJointPartySlot> slots, int essenceSlots,
        IReadOnlyDictionary<string, int>? availableCopies = null,
        int minimumDistinctRecipes = 1, int maximumUsesPerRecipe = 10,
        int maximumStates = 256, int maximumParties = 16, int maximumCandidateChecks = 250_000,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coverage); ArgumentNullException.ThrowIfNull(requiredKinds);
        return AllocateCore(families, slots, essenceSlots, availableCopies, minimumDistinctRecipes, maximumUsesPerRecipe,
            maximumStates, maximumParties, maximumCandidateChecks, true, coverage, requiredKinds, cancellationToken);
    }

    /// <summary>Balance authored core combinations across complete teams, preserving role/copy constraints.</summary>
    public static BossJointPartyResult AllocateCorePortfolio(IReadOnlyDictionary<string, string> families,
        IReadOnlyList<BossCoverageFeature> coverage, IReadOnlyList<string> requiredKinds,
        IReadOnlyList<BossMechanicCore> cores, IReadOnlyList<BossJointPartySlot> slots, int essenceSlots,
        IReadOnlyDictionary<string, int>? availableCopies = null,
        int minimumDistinctRecipes = 1, int maximumUsesPerRecipe = 10,
        int maximumStates = 256, int maximumParties = 16, int maximumCandidateChecks = 250_000,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coverage); ArgumentNullException.ThrowIfNull(requiredKinds);
        ArgumentNullException.ThrowIfNull(cores);
        return AllocateCore(families, slots, essenceSlots, availableCopies, minimumDistinctRecipes, maximumUsesPerRecipe,
            maximumStates, maximumParties, maximumCandidateChecks, true, coverage, requiredKinds, cancellationToken, cores);
    }

    private static BossJointPartyResult AllocateCore(IReadOnlyDictionary<string, string> families,
        IReadOnlyList<BossJointPartySlot> slots, int essenceSlots, IReadOnlyDictionary<string, int>? availableCopies,
        int minimumDistinctRecipes, int maximumUsesPerRecipe, int maximumStates, int maximumParties,
        int maximumCandidateChecks, bool diversify, IReadOnlyList<BossCoverageFeature>? coverage,
        IReadOnlyList<string>? requiredKinds, CancellationToken cancellationToken, IReadOnlyList<BossMechanicCore>? cores = null)
    {
        ArgumentNullException.ThrowIfNull(families); ArgumentNullException.ThrowIfNull(slots);
        cancellationToken.ThrowIfCancellationRequested();
        if (families.Count is < 1 or > 128 || families.Any(p => string.IsNullOrWhiteSpace(p.Key) || string.IsNullOrWhiteSpace(p.Value))
            || slots.Count is < 1 or > 10 || essenceSlots is < 1 or > 5
            || minimumDistinctRecipes < 1 || minimumDistinctRecipes > slots.Count || maximumUsesPerRecipe is < 1 or > 10
            || maximumStates is < 1 or > MaximumStates || maximumParties is < 1 or > MaximumParties
            || maximumCandidateChecks is < 1 or > MaximumCandidateChecks
            || slots.Any(s => s is null || s.Recipes is null || s.Recipes.Count > MaximumRecipesPerSlot)
            || !slots.Select(s => s.PartySlot).Order().SequenceEqual(Enumerable.Range(1, slots.Count))
            || availableCopies is not null && availableCopies.Any(p => !families.ContainsKey(p.Key) || p.Value < 0))
            throw new InvalidDataException("Invalid party allocation inputs or bounds.");

        var kinds = requiredKinds?.Order(StringComparer.Ordinal).ToArray() ?? [];
        if (coverage is not null && (kinds.Length is < 1 or > 5 || kinds.Distinct().Count() != kinds.Length
            || kinds.Any(k => !TowerPartyCoverage.Kinds.Contains(k)) || coverage.Count > 128 * 5
            || coverage.Any(f => f is null || !families.ContainsKey(f.EssenceId) || !TowerPartyCoverage.Kinds.Contains(f.Kind)
                || f.EvidenceKeys is not { Count: > 0 } || f.EvidenceKeys.Any(string.IsNullOrWhiteSpace))
            || coverage.Select(f => (f.EssenceId, f.Kind)).Distinct().Count() != coverage.Count))
            throw new InvalidDataException("Invalid team role coverage.");
        var target = (1 << kinds.Length) - 1;
        var features = coverage?.Select(f => (f.EssenceId, f.Kind)).ToHashSet() ?? [];
        var masks = families.Keys.ToDictionary(id => id, id => kinds.Select((kind, index) =>
            features.Contains((id, kind)) ? 1 << index : 0).Aggregate(0, (a, b) => a | b));

        var coreSnapshot = cores is null ? null : TowerCorePortfolioSearch.SnapshotCores(families, cores, essenceSlots);
        var profiles = cores is null ? null : new Dictionary<string, string>(StringComparer.Ordinal);
        var profileExposure = new Dictionary<string, int>(StringComparer.Ordinal);
        var profileUses = new Dictionary<string, int>(StringComparer.Ordinal);
        var options = new SortedDictionary<int, Option[]>();
        foreach (var slot in slots)
        {
            var normalized = new List<Option>();
            foreach (var recipe in slot.Recipes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (recipe is null || recipe.Count != essenceSlots || recipe.Any(id => id is null || !families.ContainsKey(id))
                    || recipe.Distinct(StringComparer.Ordinal).Count() != recipe.Count
                    || recipe.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != recipe.Count)
                    throw new InvalidDataException("Party allocation requires complete, family-compatible loadouts.");
                var ids = recipe.Order(StringComparer.Ordinal).ToArray();
                var recipeId = HarnessJson.Hash(ids);
                normalized.Add(new(recipeId, ids, ids.Aggregate(0, (mask, id) => mask | masks[id])));
                if (profiles is not null && !profiles.ContainsKey(recipeId))
                    profiles.Add(recipeId, HarnessJson.Hash(coreSnapshot!.Where(c => c.Essences.All(ids.Contains)).Select(c => c.Id).ToArray()));
            }
            options.Add(slot.PartySlot, normalized.DistinctBy(r => r.Id).OrderBy(r => r.Id, StringComparer.Ordinal).ToArray());
        }

        var assigned = new Dictionary<int, Option>();
        var used = new Dictionary<string, int>(StringComparer.Ordinal);
        var recipeUses = new Dictionary<string, int>(StringComparer.Ordinal);
        var parties = new List<BossJointParty>();
        var exposure = new Dictionary<string, int>(StringComparer.Ordinal);
        var retainedIds = new HashSet<string>(StringComparer.Ordinal);
        // Static constrained-slot ordering keeps each restart within the global check budget.
        var traversal = diversify ? options.OrderBy(p => p.Value.Length).ThenBy(p => p.Key).ToArray() : options.ToArray();
        var states = 0; var checks = 0; string? stop = null;
        bool Fits(Option recipe)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (checks == maximumCandidateChecks) { stop = "candidate-check-limit"; return false; }
            checks++;
            return recipeUses.GetValueOrDefault(recipe.Id) < maximumUsesPerRecipe
                && (availableCopies is null || recipe.Essences.All(id => used.GetValueOrDefault(id) < availableCopies.GetValueOrDefault(id)));
        }
        void Visit()
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stop is not null) return;
            if (states == maximumStates) { stop = "state-limit"; return; }
            states++;
            var covered = assigned.Values.Aggregate(0, (mask, option) => mask | option.Coverage);
            if (recipeUses.Count + slots.Count - assigned.Count < minimumDistinctRecipes) return;
            if (assigned.Count == slots.Count)
            {
                if (recipeUses.Count < minimumDistinctRecipes || covered != target) return;
                if (parties.Count == maximumParties) { stop = "party-limit"; return; }
                var placements = assigned.OrderBy(p => p.Key).Select(p => new BossJointPartyPlacement(p.Key, p.Value.Id, p.Value.Essences.ToArray())).ToArray();
                var id = HarnessJson.Hash(placements);
                if (diversify && !retainedIds.Add(id)) return;
                parties.Add(new(id, placements,
                    used.OrderBy(p => p.Key, StringComparer.Ordinal).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal), recipeUses.Count));
                if (diversify)
                    foreach (var placement in placements)
                    {
                        exposure[placement.RecipeId] = exposure.GetValueOrDefault(placement.RecipeId) + 1;
                        if (profiles is not null)
                        {
                            var profile = profiles[placement.RecipeId];
                            profileExposure[profile] = profileExposure.GetValueOrDefault(profile) + 1;
                        }
                    }
                return;
            }

            var priorCount = parties.Count;
            var chosenSlot = 0; List<Option>? chosenOptions = null;
            foreach (var (slot, pool) in traversal)
            {
                if (assigned.ContainsKey(slot)) continue;
                var compatible = new List<Option>();
                foreach (var recipe in pool)
                {
                    if (Fits(recipe)) compatible.Add(recipe);
                    if (stop is not null) return;
                }
                if (compatible.Count == 0) return;
                if (chosenOptions is null || compatible.Count < chosenOptions.Count)
                { chosenSlot = slot; chosenOptions = compatible; }
                if (diversify) break;
            }
            // Diversity guides traversal only; the explicit distinct/repetition constraints are authoritative.
            var urgent = slots.Count - assigned.Count <= System.Numerics.BitOperations.PopCount((uint)(target & ~covered));
            var preference = coverage is not null && parties.Count % 2 == 1 ? -1 : 1;
            var ordered = profiles is null
                ? chosenOptions!.OrderByDescending(r => System.Numerics.BitOperations.PopCount((uint)(r.Coverage & ~covered)))
                .ThenBy(r => diversify ? exposure.GetValueOrDefault(r.Id) : 0)
                .ThenBy(r => (coverage is not null && parties.Count % 2 == 1 ? -1 : 1) * recipeUses.GetValueOrDefault(r.Id))
                .ThenByDescending(r => r.Essences.Count(id => !used.ContainsKey(id))).ThenBy(r => r.Id, StringComparer.Ordinal)
                : chosenOptions!.OrderByDescending(r => urgent ? System.Numerics.BitOperations.PopCount((uint)(r.Coverage & ~covered)) : 0)
                .ThenBy(r => profileExposure.GetValueOrDefault(profiles[r.Id]))
                .ThenBy(r => preference * profileUses.GetValueOrDefault(profiles[r.Id]))
                .ThenBy(r => profiles[r.Id], StringComparer.Ordinal)
                .ThenBy(r => exposure.GetValueOrDefault(r.Id))
                .ThenBy(r => preference * recipeUses.GetValueOrDefault(r.Id))
                .ThenByDescending(r => System.Numerics.BitOperations.PopCount((uint)(r.Coverage & ~covered)))
                .ThenBy(r => r.Id, StringComparer.Ordinal);
            foreach (var recipe in ordered)
            {
                assigned.Add(chosenSlot, recipe); recipeUses[recipe.Id] = recipeUses.GetValueOrDefault(recipe.Id) + 1;
                if (profiles is not null)
                    profileUses[profiles[recipe.Id]] = profileUses.GetValueOrDefault(profiles[recipe.Id]) + 1;
                foreach (var id in recipe.Essences) used[id] = used.GetValueOrDefault(id) + 1;
                Visit();
                foreach (var id in recipe.Essences) { if (--used[id] == 0) used.Remove(id); }
                if (--recipeUses[recipe.Id] == 0) recipeUses.Remove(recipe.Id);
                if (profiles is not null && --profileUses[profiles[recipe.Id]] == 0) profileUses.Remove(profiles[recipe.Id]);
                assigned.Remove(chosenSlot);
                if (stop is not null || diversify && parties.Count > priorCount) break;
            }
        }

        do
        {
            var priorCount = parties.Count;
            Visit();
            if (!diversify || stop is not null || parties.Count == priorCount) break;
            if (parties.Count == maximumParties) { stop = "party-limit"; break; }
        } while (true);
        return new(parties.Count > 0, stop is null, stop ?? "exhausted", states, checks, parties);
    }
}
