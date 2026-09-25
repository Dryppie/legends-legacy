using System.Globalization;
using Common.Randomness;
using Domain.Models.WorldTower;

namespace BalanceHarness;

public sealed record TowerLoadoutAssignment(IReadOnlyDictionary<int, int> SourceByDestination);
public sealed record TowerLoadoutPlacementRecipe(PartyChoice Party, int Subgroup,
    IReadOnlyList<int> ChangedOwners, int ReplacementDistance, string SeedFreeScenarioHash,
    IReadOnlyList<TowerLoadoutAssignment> Assignments);
public sealed record TowerLoadoutPlacementCatalogue(string Version, string ScopeHash, string ParentId,
    int AssignmentsExamined, int IdentityAssignments, int ReferenceAssignments, int DuplicateAssignments,
    IReadOnlyList<TowerLoadoutPlacementRecipe> Recipes, string Interpretation);
public sealed record TowerLoadoutPlacementStep(string CatalogueHash, int DrawOrdinal, int Subgroup,
    TowerLoadoutAssignment Assignment);

/// <summary>Whole Essence lists move within one production subgroup. Actor identity,
/// gear, and subgroup composition stay fixed. No outcomes or external entropy enter construction.</summary>
internal sealed class TowerLoadoutPlacement
{
    internal const string Version = "tower-subgroup-loadout-placement-v1";
    internal const string Operator = "subgroup-loadout-placement";
    private readonly TowerLoadoutPlacementCatalogue catalogue;
    private readonly TowerLoadoutPlacementRecipe[] order;
    private readonly string[] references;
    private readonly string catalogueHash;
    internal TowerLoadoutPlacementCatalogue Catalogue => TowerBatchRacing.Copy(catalogue);

    internal TowerLoadoutPlacement(TowerProposalContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        catalogue = Create(context.Scope, context.BenchmarkReferenceId, token);
        if (catalogue.Recipes.Count < 17)
            throw new InvalidDataException("Loadout placement requires at least 17 distinct nonreference recipes before any panel.");
        references = context.Scope.Starts.Select(s => s.Party.Id).Order(StringComparer.Ordinal).ToArray();
        catalogueHash = HarnessJson.Hash(catalogue);
        order = catalogue.Recipes.ToArray(); // Create already sorts distinct identities before shuffling.
        new Random(StableRandom.Seed(TowerProposalPolicies.LoadoutPlacementPolicyVersion,
            context.RootSeed.ToString(CultureInfo.InvariantCulture), "loadout-order", catalogue.ParentId, catalogueHash)).Shuffle(order);
    }

    internal static TowerLoadoutPlacementCatalogue Create(TowerBossDiscoveryDefinition scope,
        string benchmarkReferenceId, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        scope = TowerBatchRacing.Copy(scope);
        TowerBatchRacing.ValidateScope(scope);
        if (scope.RequiredPartySize != 10 || scope.Budget.EssenceSlots != 5
            || WorldTowerPartyRules.MaximumPartySize != 5
            || scope.Starts.Count(s => s.ReferenceId == benchmarkReferenceId) != 1)
            throw new InvalidDataException("Loadout placement requires ten owners, five Essence slots and one bound benchmark.");
        var parent = scope.Starts.Single(s => s.ReferenceId == benchmarkReferenceId).Party;
        var excluded = scope.Starts.Select(s => s.Party.Id).ToHashSet(StringComparer.Ordinal);
        var groups = parent.Builds.Keys.Order().GroupBy(WorldTowerPartyRules.GetPartyNumber).ToArray();
        if (groups.Length != 2 || groups.Any(g => g.Count() != 5))
            throw new InvalidDataException("Loadout placement requires two complete production subgroups.");
        var recipes = new Dictionary<string, TowerLoadoutPlacementRecipe>(StringComparer.Ordinal);
        var examined = 0; var identities = 0; var referenceAssignments = 0; var duplicates = 0;
        foreach (var group in groups)
        {
            var owners = group.ToArray();
            foreach (var sources in Permutations(owners, token))
            {
                token.ThrowIfCancellationRequested(); examined++;
                var assignment = new TowerLoadoutAssignment(owners.Select((owner, i) => (owner, source: sources[i]))
                    .ToDictionary(p => p.owner, p => p.source));
                var party = Apply(scope, parent, assignment, token);
                if (party.Id == parent.Id) { identities++; continue; }
                if (excluded.Contains(party.Id)) { referenceAssignments++; continue; }
                if (recipes.TryGetValue(party.Id, out var existing))
                {
                    if (HarnessJson.Hash(existing.Party.Builds) != HarnessJson.Hash(party.Builds))
                        throw new InvalidDataException("Loadout placement identity collision.");
                    recipes[party.Id] = existing with { Assignments = [.. existing.Assignments, assignment] };
                    duplicates++;
                }
                else recipes.Add(party.Id, new(party, group.Key,
                    owners.Where(o => !parent.Builds[o].SequenceEqual(party.Builds[o])).ToArray(),
                    TowerSuppliedCompositionSearch.Distance(parent, party) / 2,
                    HarnessJson.Hash(Scenario(scope, benchmarkReferenceId, party)), [assignment]));
            }
        }
        if (examined != 240) throw new InvalidDataException("Incomplete loadout placement catalogue.");
        return new(Version, HarnessJson.Hash(scope), parent.Id, examined, identities, referenceAssignments, duplicates,
            recipes.Values.OrderBy(r => r.Party.Id, StringComparer.Ordinal).ToArray(),
            "StructuralPlacementOnlyNoCombatValueOrUptimeClaim");
    }

    // Validation is kept at this boundary as assignment records are exported evidence.
    internal static PartyChoice Apply(TowerBossDiscoveryDefinition scope, PartyChoice parent,
        TowerLoadoutAssignment assignment, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        TowerBatchRacing.ValidateParties(scope, [parent]);
        var map = assignment?.SourceByDestination;
        if (map is null || map.Count != 5 || !map.Keys.Order().SequenceEqual(map.Values.Order())
            || map.Keys.Any(o => !parent.Builds.ContainsKey(o))
            || map.Keys.Select(WorldTowerPartyRules.GetPartyNumber).Distinct().Count() != 1)
            throw new InvalidDataException("Loadout assignment must be a bijection within one complete production subgroup.");
        var builds = parent.Builds.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.ToArray());
        // Read exclusively from the parent, never from a partially applied permutation.
        foreach (var (destination, source) in map) builds[destination] = parent.Builds[source].ToArray();
        var party = TowerPartySelection.Choice(TowerProposalPolicies.LoadoutPlacementPolicyVersion,
            TowerCompositionSearch.CanonicalBuilds(builds));
        TowerBatchRacing.ValidateParties(scope, [party]);
        return party;
    }

    internal static TowerScenario Scenario(TowerBossDiscoveryDefinition scope, string benchmarkReferenceId, PartyChoice party)
    {
        TowerBossDiscovery.ValidateParty(scope, party);
        var anchor = scope.References.Single(r => r.Id == benchmarkReferenceId).Scenario;
        return TowerBatchRacing.Copy(anchor with { Seeds = [], Party = anchor.Party.Select(actor => actor with {
            Build = actor.Build with { EssenceIds = party.Builds[actor.PartySlot] }
        }).ToArray() });
    }

    private static IEnumerable<int[]> Permutations(int[] owners, CancellationToken token)
    {
        var path = new int[owners.Length]; var used = new bool[owners.Length];
        return Visit(0);
        IEnumerable<int[]> Visit(int depth)
        {
            token.ThrowIfCancellationRequested();
            if (depth == owners.Length) { yield return path.ToArray(); yield break; }
            for (var i = 0; i < owners.Length; i++)
            {
                if (used[i]) continue;
                used[i] = true; path[depth] = owners[i];
                foreach (var value in Visit(depth + 1)) yield return value;
                used[i] = false;
            }
        }
    }

    internal TowerAdaptiveBatch Generate(int wave, IReadOnlyList<PartyChoice> beam, IReadOnlySet<string> seenBefore,
        IReadOnlyList<TowerPanelEvaluation> feedback, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (wave is not (1 or 2)) throw new InvalidDataException("Unknown placement wave.");
        var offset = wave == 1 ? 0 : 9;
        var expectedSeen = references.Concat(order.Take(offset).Select(r => r.Party.Id)).ToHashSet(StringComparer.Ordinal);
        if (!expectedSeen.SetEquals(seenBefore))
            throw new InvalidDataException("Placement wave requires exactly its references and preceding fixed draw prefix.");
        var rows = order.Skip(offset).Take(wave == 1 ? 9 : 8).ToArray();
        var proposals = rows.Select((r, i) => new TowerAdaptiveProposal(i + 1, Operator, Operator, "benchmark",
            [catalogue.ParentId], r.Assignments[0].SourceByDestination.Keys.Order().ToArray(), r.ChangedOwners,
            1, r.ReplacementDistance, null, null, r.Party, null,
            LoadoutPlacement: new(catalogueHash, offset + i + 1, r.Subgroup, r.Assignments[0]))).ToArray();
        token.ThrowIfCancellationRequested();
        return TowerBatchRacing.Copy(new TowerAdaptiveBatch(wave, feedback.Sum(p => p.Observations.Count),
            feedback.Select(p => HarnessJson.Hash(p.Freeze)).ToArray(), beam.Select(p => p.Id).ToArray(),
            seenBefore.Order(StringComparer.Ordinal).ToArray(), proposals, rows.Select(r => r.Party).ToArray()));
    }
}
