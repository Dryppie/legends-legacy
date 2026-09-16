namespace BalanceHarness;

/// <summary>Refine independently constructed teams using this arm's completed discovery measurements.</summary>
public static class TowerDiscoveryRefinementSearch
{
    public const string Version = "independent-discovery-refinement-v1";
    public const string RoleSafeVersion = "independent-discovery-refinement-roles-v2";
    public const string NovelVersion = "independent-discovery-refinement-novel-v3";
    public const string LocalVersion = "independent-discovery-refinement-local-v4";
    public const string FreshFirstVersion = "independent-discovery-refinement-fresh-first-v5";
    public const string LocalOperator = "single-slot-refine";
    public const string Method = "discovery-refinement";
    public const string FreshOperator = "fresh-refinement-coverage";

    internal static bool Fresh(int attempt, int completed) => completed == 0 || attempt < 4 || attempt % 4 == 3;

    // Reserve three quarters of the fixed proposal budget for fresh construction.
    // No completed parent uses the existing charged fresh fallback; caps never expand.
    internal static bool FreshFirst(int attempt, int completed, int budget) =>
        completed == 0 || attempt < budget - budget / 4;

    internal static string RefinementOperator(int attempt) => (attempt % 4) switch
    {
        0 => "loadout-distribute",
        1 => "loadout-refine",
        2 => "whole-character",
        _ => throw new InvalidDataException("Fresh exploration cannot use a refinement operator.")
    };

    internal static int ParentCount(BossDiscoveryProvenance p) => p.Method != Method ? -1 : p.Operator switch
    {
        FreshOperator => 0,
        "whole-character" or "loadout-refine" => 1,
        "loadout-distribute" when p.ParentIds.Count is >= 1 and <= 2 => p.ParentIds.Count,
        _ => -1
    };

    internal static int LocalParentCount(BossDiscoveryProvenance p) => p.Method != Method ? -1 : p.Operator switch
    {
        FreshOperator => 0,
        LocalOperator => 1,
        _ => -1
    };
}

public sealed partial class TowerBossPartyGenerator
{
    // Enumerate a finite distribution neighbourhood before emitting one proposal. No
    // evaluator or extra random draw is used; exhausted construction still costs an attempt.
    internal BossLoadoutProposal NovelDistribution(BossGeneratedProposal parent,
        IReadOnlyList<BossLoadoutModule> library, int[] slots, int donorOffset, int requestedCount,
        IReadOnlySet<string> observed, CancellationToken token)
    {
        var maximum = checked(library.Count * slots.Length); var checks = 0;
        for (var donorIndex = 0; donorIndex < library.Count; donorIndex++)
        {
            var donor = library[(donorOffset + donorIndex) % library.Count];
            for (var lengthIndex = 0; lengthIndex < slots.Length; lengthIndex++)
            {
                token.ThrowIfCancellationRequested(); checks++;
                var count = (requestedCount - 1 + lengthIndex) % slots.Length + 1;
                var targets = RoleSafeTargets(parent.Party!.Builds, donor.Essences, slots.Take(count).ToArray())
                    .Where(slot => !parent.Party.Builds[slot].SequenceEqual(donor.Essences)).ToArray();
                var builds = parent.Party.Builds.ToDictionary(p => p.Key, p => p.Value);
                foreach (var slot in targets) builds[slot] = donor.Essences;
                var choice = Choice(builds, "loadout-distribute", null);
                if (targets.Length == 0 || choice.Rejection is not null || observed.Contains(choice.Party!.Id)) continue;
                return new(choice, new[] { parent.Provenance.Id, donor.ProposalId }.Distinct(StringComparer.Ordinal).ToArray(),
                    new(library.Count, HarnessJson.Hash(library), [new(donor, targets)],
                        new(maximum, checks, donorOffset, requestedCount, "novel-distribution")));
            }
        }
        // Keep the unchanged parent's identity so the controller's ordinary duplicate
        // accounting applies. There is no refill, candidate evaluation or cap extension.
        return new(new(parent.Party, "loadout-distribute", null, null), [parent.Provenance.Id],
            new(library.Count, HarnessJson.Hash(library), [], new(maximum, checks, donorOffset, requestedCount, "exhausted")));
    }

    private bool HasTeamRoles(IEnumerable<IReadOnlyList<string>> builds)
    {
        var ids = builds.SelectMany(ids => ids).ToHashSet(StringComparer.Ordinal);
        return TowerTeamCoverageSearch.RequiredKindsPerTeam.All(kind => mechanics.Coverage!.Any(f =>
            f.Kind == kind && ids.Contains(f.EssenceId)));
    }

    // Construct a subset of the requested placements, retaining each role's last provider.
    // The caller records the actual targets; an unchanged party still costs a duplicate proposal.
    internal int[] RoleSafeTargets(IReadOnlyDictionary<int, IReadOnlyList<string>> original,
        IReadOnlyList<string> module, IReadOnlyList<int> requested)
    {
        var builds = original.ToDictionary(p => p.Key, p => p.Value);
        var accepted = new List<int>();
        foreach (var slot in requested)
        {
            var previous = builds[slot]; builds[slot] = module;
            if (HasTeamRoles(builds.Values)) accepted.Add(slot);
            else builds[slot] = previous;
        }
        return accepted.ToArray();
    }

    // One bounded pass through the canonical pool, starting at the sampled offset. This
    // changes provider choice, never ability order, and uses no additional random draws.
    internal string RoleSafeReplacement(IReadOnlyDictionary<int, IReadOnlyList<string>> builds,
        IReadOnlyList<int> targets, IReadOnlyList<string> recipe, int position, int offset)
    {
        var targetSet = targets.ToHashSet();
        for (var i = 0; i < pool.Length; i++)
        {
            var id = pool[(offset + i) % pool.Length];
            if (id == recipe[position]) continue;
            var edited = recipe.ToArray(); edited[position] = id;
            if (HasTeamRoles(builds.Select(p => targetSet.Contains(p.Key) ? edited : p.Value))) return id;
        }
        return recipe[position];
    }

    private IReadOnlyList<string> RoleSafePrefix(IReadOnlyDictionary<int, IReadOnlyList<string>> remaining,
        IReadOnlyList<string> removed)
    {
        var present = remaining.Values.SelectMany(ids => ids).ToHashSet(StringComparer.Ordinal);
        var prefix = new List<string>();
        foreach (var kind in TowerTeamCoverageSearch.RequiredKindsPerTeam.Order(StringComparer.Ordinal))
        {
            var providers = mechanics.Coverage!.Where(f => f.Kind == kind).Select(f => f.EssenceId).ToHashSet(StringComparer.Ordinal);
            if (providers.Overlaps(present)) continue;
            var id = removed.Where(providers.Contains).Order(StringComparer.Ordinal).First();
            prefix.Add(id); present.Add(id);
        }
        return prefix;
    }
}
