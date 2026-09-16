namespace BalanceHarness;

public sealed record BossJointCoreConstruction(string CoreId, bool SearchExhausted, string StopReason,
    int VisitedStates, int Recipes);
public sealed record BossJointStructuralTrace(string Version, int ProposalIndex, string PoolHash, int FullRecipes,
    IReadOnlyList<BossJointCoreConstruction> Cores, bool AllocationExhausted, string AllocationStopReason,
    int AllocationStates, int CandidateChecks, int RetainedParties, string? StructuralPartyId);

/// <summary>A finite, opt-in structural search. Authored coverage is not a combat-strength score.</summary>
public static class TowerJointStructuralSearch
{
    public const string Version = "independent-joint-structural-v1";
    public const string Method = "joint-structural";
    public const string Operator = "fresh-joint-structural";
    public const int MaximumCores = 64;
    public const int MaximumCandidates = 16;
    public static bool IsStructural(string policy) => policy is Version or TowerJointStructuralDiversity.Version or TowerTeamCoverageSearch.Version or TowerFillerDiversitySearch.Version or TowerCorePortfolioSearch.Version or TowerDiscoveryRefinementSearch.Version or TowerDiscoveryRefinementSearch.RoleSafeVersion or TowerDiscoveryRefinementSearch.NovelVersion or TowerDiscoveryRefinementSearch.LocalVersion or TowerDiscoveryRefinementSearch.FreshFirstVersion;

    internal static void ValidateBounds(string policy, int characters, int essenceSlots, int providers)
    {
        if (IsStructural(policy) && (characters is < 1 or > 10 || essenceSlots is < 1 or > 5 || providers > 128))
            throw new InvalidDataException("Joint structural search exceeds its fixed construction bounds.");
    }
}

public sealed partial class TowerBossPartyGenerator
{
    private BossJointPartyResult? jointParties;
    private BossJointStructuralTrace? jointTrace;

    internal BossGeneratedChoice FreshJointStructural(int proposalIndex, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!TowerJointStructuralSearch.IsStructural(input.Generation.PolicyVersion)
            || proposalIndex is < 0 or >= TowerJointStructuralSearch.MaximumCandidates)
            throw new InvalidDataException("Joint structural proposals require the explicit bounded policy.");
        var diverse = input.Generation.PolicyVersion == TowerJointStructuralDiversity.Version;
        var corePortfolio = input.Generation.PolicyVersion == TowerCorePortfolioSearch.Version;
        var fillerDiverse = input.Generation.PolicyVersion == TowerFillerDiversitySearch.Version || corePortfolio;
        var collective = input.Generation.PolicyVersion is TowerTeamCoverageSearch.Version or TowerDiscoveryRefinementSearch.Version or TowerDiscoveryRefinementSearch.RoleSafeVersion or TowerDiscoveryRefinementSearch.NovelVersion or TowerDiscoveryRefinementSearch.LocalVersion or TowerDiscoveryRefinementSearch.FreshFirstVersion || fillerDiverse;
        if (jointParties is null)
        {
            using var timing = TowerPerformanceTrace.Measure("joint-structural.construct");
            var cores = new List<BossJointCoreConstruction>();
            var recipes = new List<IReadOnlyList<string>>();
            if (fillerDiverse)
            {
                var result = TowerFillerDiversitySearch.ConstructPool(families, mechanics.Coverage!, mechanics.Cores!,
                    input.Budget.EssenceSlots, input.OwnedCopies, token);
                cores.AddRange(result.Cores); recipes.AddRange(result.Recipes);
            }
            else if (collective)
            {
                var result = TowerTeamCoverageSearch.ConstructPool(families, mechanics.Coverage!, mechanics.Cores!,
                    input.Budget.EssenceSlots, input.OwnedCopies, token);
                cores.AddRange(result.Cores); recipes.AddRange(result.Recipes);
            }
            else foreach (var core in mechanics.Cores!.OrderBy(c => c.Id, StringComparer.Ordinal))
            {
                var result = TowerJointLoadoutConstructor.Construct(families, mechanics.Coverage!, core.EssenceIds,
                    diverse ? TowerJointStructuralDiversity.RequiredKindsPerCharacter : TowerPartyCoverage.Kinds,
                    input.Budget.EssenceSlots, input.OwnedCopies, 256, 16, token);
                cores.Add(new(core.Id, result.SearchExhausted, result.StopReason, result.VisitedStates, result.Recipes.Count));
                recipes.AddRange(result.Recipes.Where(r => r.EssenceIds.Count == input.Budget.EssenceSlots).Select(r => r.EssenceIds));
            }
            var pool = recipes.DistinctBy(r => HarnessJson.Hash(r)).OrderBy(r => HarnessJson.Hash(r), StringComparer.Ordinal).ToArray();
            var slots = Enumerable.Range(1, input.RequiredPartySize).Select(slot => new BossJointPartySlot(slot, pool)).ToArray();
            var parties = corePortfolio
                ? TowerJointPartyAllocator.AllocateCorePortfolio(families, mechanics.Coverage!, TowerTeamCoverageSearch.RequiredKindsPerTeam,
                    mechanics.Cores!, slots, input.Budget.EssenceSlots, input.OwnedCopies, TowerTeamCoverageSearch.MinimumDistinctRecipes,
                    TowerTeamCoverageSearch.MaximumUsesPerRecipe, 256, 16, 250_000, token)
                : collective
                ? TowerJointPartyAllocator.AllocateCovered(families, mechanics.Coverage!, TowerTeamCoverageSearch.RequiredKindsPerTeam,
                    slots, input.Budget.EssenceSlots, input.OwnedCopies, TowerTeamCoverageSearch.MinimumDistinctRecipes,
                    TowerTeamCoverageSearch.MaximumUsesPerRecipe, 256, 16, 250_000, token)
                : diverse
                ? TowerJointPartyAllocator.AllocateDiverse(families, slots, input.Budget.EssenceSlots, input.OwnedCopies,
                    TowerJointStructuralDiversity.MinimumDistinctRecipes(input.RequiredPartySize), TowerJointStructuralDiversity.MaximumUsesPerRecipe,
                    256, 16, 250_000, token)
                : TowerJointPartyAllocator.Allocate(families, slots, input.Budget.EssenceSlots, input.OwnedCopies, input.RequiredPartySize, 1, 256, 16, 250_000, token);
            // Publish the cache only after both bounded searches complete successfully.
            jointTrace = new(input.Generation.PolicyVersion, 0, HarnessJson.Hash(pool), pool.Length, cores.ToArray(),
                parties.SearchExhausted, parties.StopReason, parties.VisitedStates, parties.CandidateChecks, parties.Parties.Count, null);
            jointParties = parties;
        }
        var selected = jointParties.Parties.ElementAtOrDefault(proposalIndex);
        var trace = jointTrace! with { ProposalIndex = proposalIndex, StructuralPartyId = selected?.Id };
        if (selected is null) return new(null, "joint-structural", null, "joint-structural-pool-unavailable", JointStructural: trace);
        var choice = Choice(selected.Placements.ToDictionary(p => p.PartySlot, p => p.EssenceIds), "joint-structural", selected.Id);
        return choice with { JointStructural = trace };
    }
}
