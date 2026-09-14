namespace BalanceHarness;

public sealed record BossPartyLineage(string FounderId, string? ParentId, int MatchingPositions, int Depth);

/// <summary>Whole-party founder labels and a two-elite/two-alternative main parent beam.</summary>
public static class TowerPartyLineages
{
    public const string Version = "independent-party-lineages-v16";
    public const string Method = "lineage-loadout-composition-joint";
    public static readonly string[] Methods = ["loadout-composition-joint", Method];
    public const int EliteSeats = 2;

    public static BossPartyLineage Describe(BossGeneratedProposal proposal,
        IReadOnlyDictionary<string, BossGeneratedProposal> priorByProposalId)
    {
        if (proposal.Result != "evaluated" || proposal.Party is null || proposal.Provenance.Method != Method
            || proposal.Provenance.ReferenceIds.Count != 0
            || proposal.Provenance.ParentIds.Distinct().Count() != proposal.Provenance.ParentIds.Count)
            throw new InvalidDataException("Lineages require complete independent parties and distinct contributing parents.");
        if (proposal.Provenance.ParentIds.Count == 0)
        {
            if (!proposal.Provenance.Operator.StartsWith("fresh-", StringComparison.Ordinal))
                throw new InvalidDataException("Only fresh construction can establish a founder.");
            return new(proposal.Provenance.Id, null, 0, 0);
        }
        var parents = proposal.Provenance.ParentIds.Select(id => {
            if (!priorByProposalId.TryGetValue(id, out var p) || p.Result != "evaluated" || p.Party is null
                || p.Lineage is null || p.Provenance.Id != id || p.Provenance.Method != proposal.Provenance.Method
                || p.Provenance.GenerationSeed != proposal.Provenance.GenerationSeed || p.Provenance.ReferenceIds.Count != 0)
                throw new InvalidDataException("A lineage parent must be completed earlier in this independent arm.");
            return (Proposal: p, Matches: Matches(proposal.Party, p.Party));
        }).OrderByDescending(p => p.Matches).ThenBy(p => p.Proposal.Party!.Id, StringComparer.Ordinal)
            .ThenBy(p => p.Proposal.Provenance.Id, StringComparer.Ordinal).ToArray();
        // Follow the closest complete contributing recipe, not the union of all donor ancestry.
        // This label describes inherited structure; it is not a claim of genetic independence.
        var parent = parents[0]; var lineage = parent.Proposal.Lineage!;
        return new(lineage.FounderId, parent.Proposal.Provenance.Id, parent.Matches, checked(lineage.Depth + 1));
    }

    internal static int Matches(PartyChoice child, PartyChoice parent)
    {
        if (!child.Builds.Keys.Order().SequenceEqual(parent.Builds.Keys.Order())
            || child.Builds.Any(p => p.Value.Count != parent.Builds[p.Key].Count))
            throw new InvalidDataException("Whole-party lineage matching requires the same character and Essence slots.");
        return child.Builds.Sum(p => p.Value.Where((id, i) => id == parent.Builds[p.Key][i]).Count());
    }

    public static string[] Select(IEnumerable<BossDiscoveryMeasurement> measurements,
        IReadOnlyDictionary<string, BossGeneratedProposal> measured)
    {
        var ranked = TowerBossGeneration.Rank(measurements).ToArray();
        if (ranked.Select(e => e.Id).Distinct().Count() != ranked.Length)
            throw new InvalidDataException("A lineage beam requires unique completed measurements.");
        int? seed = null;
        foreach (var row in ranked)
        {
            if (!measured.TryGetValue(row.Id, out var p) || p.Party?.Id != row.Id || p.Result != "evaluated"
                || p.Lineage is null || p.Provenance.Method != Method || p.Provenance.ReferenceIds.Count != 0
                || (seed is not null && seed != p.Provenance.GenerationSeed))
                throw new InvalidDataException("A lineage beam requires completed labeled parties from one independent arm.");
            seed = p.Provenance.GenerationSeed;
        }
        var selected = ranked.Take(EliteSeats).Select(e => e.Id).ToList();
        var founders = selected.Select(id => measured[id].Lineage!.FounderId).ToHashSet(StringComparer.Ordinal);
        foreach (var row in ranked.Skip(EliteSeats))
        {
            if (selected.Count == TowerBossGeneration.BeamSize) break;
            if (founders.Add(measured[row.Id].Lineage!.FounderId)) selected.Add(row.Id);
        }
        // Small or single-founder populations retain the original rank order for unused seats.
        foreach (var row in ranked)
        {
            if (selected.Count == TowerBossGeneration.BeamSize) break;
            if (!selected.Contains(row.Id)) selected.Add(row.Id);
        }
        return selected.ToArray();
    }

    public static void ValidateArm(BossGenerationArm arm)
    {
        var prior = new Dictionary<string, BossGeneratedProposal>(StringComparer.Ordinal);
        foreach (var p in arm.Proposals)
        {
            if (arm.Method != Method || p.Result != "evaluated")
            {
                if (p.Lineage is not null) throw new InvalidDataException("Only completed lineage-arm proposals carry founder labels.");
                continue;
            }
            if (p.Provenance.Method != arm.Method || p.Provenance.GenerationSeed != arm.Seed || p.Lineage != Describe(p, prior))
                throw new InvalidDataException("Saved lineage differs from complete same-arm contributing ancestry.");
            prior.Add(p.Provenance.Id, p);
        }
    }
}
