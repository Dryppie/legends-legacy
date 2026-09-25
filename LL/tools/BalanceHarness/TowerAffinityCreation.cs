using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record TowerAffinityCreationPair(string Id, IReadOnlyList<string> EssenceIds, IReadOnlyList<string> AffinityIds);
public sealed record TowerAffinityCreationEdit(IReadOnlyList<string> Removed, IReadOnlyList<string> Added);
public sealed record TowerAffinityOwnerOpportunity(string ParentId, int Owner, string PairId, bool AlreadyActive,
    IReadOnlyList<string> MissingEssences, IReadOnlyList<string> ProtectedEssences,
    IReadOnlyList<TowerAffinityCreationEdit> LegalEdits,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TowerAlliedActionProvider>? AlliedActionProtections = null);
public sealed record TowerAffinityCreationStep(string PairId, IReadOnlyList<string> TargetAffinityIds,
    IReadOnlyList<string> Removed, IReadOnlyList<string> Added, IReadOnlyList<string> NewlyActivatedAffinityIds,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerAffinityRemovalSelection? RemovalSelection = null);
public sealed record TowerAffinityRemovalSelection(string Rule, IReadOnlyList<string> ProtectedEssences,
    int EligiblePairs, int EligibleEdits,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TowerAlliedActionProvider>? AlliedActionProtections = null);
public sealed record TowerAffinityCreationCoverage(IReadOnlyList<TowerAffinityCreationPair> Pairs,
    IReadOnlyList<TowerAffinityOwnerOpportunity> Opportunities, int ActivePairOwners, int EligiblePairOwners,
    int AttemptedCreations, int AcceptedCreations, int AcceptedParentOwners, int UnchangedRecipes,
    IReadOnlyDictionary<string, int> Rejections, string Interpretation,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerAlliedActionProtectionReport? AlliedActionProtection = null);

/// <summary>Bounded structural placement of explicitly selected authored affinities.
/// Multiple routes for the same essence pair do not weight its sampling probability.
/// No combat outcomes, external randomness or historical performance enter this code.</summary>
internal sealed class TowerAffinityCreation
{
    private readonly BossDiscoveryInputs input;
    private readonly TowerDamageSourceAffinity[] affinities;
    private readonly Dictionary<string, string> families;
    private readonly Func<PartyChoice, int, HashSet<string>> protectedIds;
    private readonly bool preserveCompletableAffinities;
    private readonly TowerAlliedActionProtectionReport? alliedActionProtection;
    private readonly Dictionary<(string Parent, int Owner), TowerAffinityOwnerOpportunity[]> opportunities = [];
    internal TowerAffinityCreationPair[] Pairs { get; }

    internal TowerAffinityCreation(BossDiscoveryInputs input, TowerDamageSourceAffinity[] affinities,
        Func<PartyChoice, int, HashSet<string>> protectedIds, bool preserveCompletableAffinities = false,
        TowerAlliedActionProtectionReport? alliedActionProtection = null)
    {
        if (input.Budget.EssenceSlots is < 2 or > 10 || affinities.Length is < 1 or > 32)
            throw new InvalidDataException("Affinity creation requires two to ten slots and one to 32 selected routes.");
        this.input = input; this.affinities = affinities; this.protectedIds = protectedIds;
        this.preserveCompletableAffinities = preserveCompletableAffinities;
        if (alliedActionProtection is not null && !preserveCompletableAffinities)
            throw new InvalidDataException("Allied-action protection requires selected-endpoint protection.");
        this.alliedActionProtection = alliedActionProtection;
        families = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Family, StringComparer.Ordinal);
        Pairs = affinities.GroupBy(a => HarnessJson.Hash(new[] { a.ProducerEssenceId, a.ModifierEssenceId }.Order(StringComparer.Ordinal).ToArray()))
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new TowerAffinityCreationPair(g.Key,
                new[] { g.First().ProducerEssenceId, g.First().ModifierEssenceId }.Order(StringComparer.Ordinal).ToArray(),
                g.Select(a => a.Id).Order(StringComparer.Ordinal).ToArray())).ToArray();
    }

    internal TowerAffinityOwnerOpportunity[] Opportunities(PartyChoice parent, int owner, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (opportunities.TryGetValue((parent.Id, owner), out var cached)) return cached;
        var old = parent.Builds[owner];
        var copies = parent.Builds.Values.SelectMany(ids => ids).GroupBy(id => id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var parentProtection = protectedIds(parent, owner);
        var alliedProtections = alliedActionProtection?.Providers.Where(p => old.Contains(p.EssenceId)).ToArray();
        var rows = new List<TowerAffinityOwnerOpportunity>();
        foreach (var pair in Pairs)
        {
            token.ThrowIfCancellationRequested();
            var missing = pair.EssenceIds.Except(old, StringComparer.Ordinal).ToArray();
            var protect = parentProtection.ToHashSet(StringComparer.Ordinal);
            if (alliedProtections is not null) protect.UnionWith(alliedProtections.Select(p => p.EssenceId));
            if (preserveCompletableAffinities)
            {
                // Protect existing endpoints of every selected route the addition
                // could complete, including routes already active in this owner.
                var possible = old.Concat(missing).ToHashSet(StringComparer.Ordinal);
                protect.UnionWith(affinities
                    .Where(a => possible.Contains(a.ProducerEssenceId) && possible.Contains(a.ModifierEssenceId))
                    .SelectMany(a => new[] { a.ProducerEssenceId, a.ModifierEssenceId })
                    .Where(old.Contains));
            }
            var removable = old.Except(pair.EssenceIds.Concat(protect), StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            var edits = new List<TowerAffinityCreationEdit>();
            // At most 32 pairs * C(10,2) placements per owner; no random retry loop.
            // Only the missing endpoints are added, keeping the smallest edit distance.
            if (missing.Length != 0)
            foreach (var removed in Removals(removable, missing.Length))
            {
                token.ThrowIfCancellationRequested();
                var edit = new TowerAffinityCreationEdit(removed, missing);
                var replaced = old.Except(removed, StringComparer.Ordinal).Concat(missing).ToArray();
                if (replaced.Length == input.Budget.EssenceSlots
                    && replaced.All(families.ContainsKey)
                    && replaced.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() == replaced.Length
                    && (input.OwnedCopies is null || missing.All(id => copies.GetValueOrDefault(id) + 1 <= input.OwnedCopies.GetValueOrDefault(id))))
                    edits.Add(edit);
            }
            rows.Add(new(parent.Id, owner, pair.Id, missing.Length == 0, missing,
                protect.Order(StringComparer.Ordinal).ToArray(), edits, alliedProtections));
        }
        cached = rows.ToArray(); opportunities.Add((parent.Id, owner), cached);
        return cached;
    }

    private static IEnumerable<string[]> Removals(string[] removable, int count)
    {
        for (var i = 0; i < removable.Length; i++)
            if (count == 1) yield return [removable[i]];
            else if (count == 2)
                for (var j = i + 1; j < removable.Length; j++) yield return [removable[i], removable[j]];
    }

    private BossGeneratedChoice Build(PartyChoice parent, int owner, TowerAffinityCreationEdit edit)
    {
        var builds = parent.Builds.ToDictionary(p => p.Key, p => p.Value.ToArray());
        builds[owner] = builds[owner].Except(edit.Removed, StringComparer.Ordinal).Concat(edit.Added).ToArray();
        return TowerSuppliedCompositionSearch.Choice(input, builds, "affinity-create");
    }

    internal (PartyChoice? Party, TowerAffinityCreationStep? Step, string? Rejection) Create(
        PartyChoice parent, int owner, Random random, CancellationToken token)
    {
        var rows = Opportunities(parent, owner, token);
        var eligible = rows.Where(r => r.LegalEdits.Count > 0).ToArray();
        if (eligible.Length == 0)
            return (null, null, rows.All(r => r.AlreadyActive) ? "affinities-already-active"
                : alliedActionProtection is not null ? "no-legal-allied-action-preserving-affinity-creation"
                : preserveCompletableAffinities ? "no-legal-preserving-affinity-creation" : "no-legal-affinity-creation");
        // Equal weight per eligible pair, then equal weight per legal minimal edit.
        var row = eligible[random.Next(eligible.Length)];
        var edit = row.LegalEdits[random.Next(row.LegalEdits.Count)];
        var built = Build(parent, owner, edit);
        if (built.Rejection is not null || built.Party!.Id == parent.Id)
            throw new InvalidDataException("A previously legal affinity placement changed during generation.");
        var pair = Pairs.Single(p => p.Id == row.PairId);
        var active = affinities.Where(a => !(parent.Builds[owner].Contains(a.ProducerEssenceId)
                && parent.Builds[owner].Contains(a.ModifierEssenceId))
            && built.Party.Builds[owner].Contains(a.ProducerEssenceId) && built.Party.Builds[owner].Contains(a.ModifierEssenceId))
            .Select(a => a.Id).Order(StringComparer.Ordinal).ToArray();
        return (built.Party, new(pair.Id, pair.AffinityIds, edit.Removed, edit.Added, active,
            preserveCompletableAffinities ? new(alliedActionProtection is null ? TowerProposalPolicies.CompletableAffinityRemovalRule
                : TowerProposalPolicies.AlliedActionRemovalRule,
                row.ProtectedEssences, eligible.Length, row.LegalEdits.Count, row.AlliedActionProtections) : null), null);
    }

    internal TowerAffinityCreationCoverage Coverage(IEnumerable<PartyChoice> parents, TowerAdaptiveBatch batch, CancellationToken token)
    {
        var all = parents.DistinctBy(p => p.Id).OrderBy(p => p.Id, StringComparer.Ordinal)
            .SelectMany(p => p.Builds.Keys.Order().SelectMany(owner => Opportunities(p, owner, token))).ToArray();
        var attempts = batch.Proposals.Where(p => p.RequestedOperator == "affinity-create").ToArray();
        var accepted = attempts.Where(p => p.Rejection is null).ToArray();
        return new(Pairs, all, all.Count(r => r.AlreadyActive), all.Count(r => r.LegalEdits.Count > 0),
            attempts.Length, accepted.Length, accepted.Select(p => (p.Parents[0], p.ChangedOwners.Single())).Distinct().Count(),
            attempts.Count(p => p.Party is not null && p.Parents.Contains(p.Party.Id)),
            attempts.Where(p => p.Rejection is not null).GroupBy(p => p.Rejection!).OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal),
            "AuthoredCompatibilityAndLegalCoverageOnlyNotMeasuredCombatSynergy", alliedActionProtection);
    }
}
