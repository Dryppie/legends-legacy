namespace BalanceHarness;

public sealed record BossLocalRefinementTrace(int MaximumChecks, int Checks, int StartOffset, string Status,
    int? Slot, string? Removed, string? Added, IReadOnlyDictionary<string, int> Skips);
internal sealed record BossLocalRefinementProposal(BossGeneratedChoice Choice, BossLocalRefinementTrace Trace);

public sealed partial class TowerBossPartyGenerator
{
    // One random offset, then at most one pass through slot x position x provider.
    // Canonical ordering is reapplied by Choice; it is never a search dimension.
    internal BossLocalRefinementProposal RefineLocal(Random random, BossGeneratedProposal parent,
        IReadOnlySet<string> observed, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (input.Generation.PolicyVersion is not (TowerDiscoveryRefinementSearch.LocalVersion or TowerDiscoveryRefinementSearch.FreshFirstVersion)
            || parent.Result != "evaluated" || parent.Party is null || Invalid(parent.Party) is not null
            || parent.Provenance.Method != TowerDiscoveryRefinementSearch.Method
            || parent.Provenance.GenerationSeed != input.Generation.Seeds.Single()
            || parent.Provenance.ReferenceIds.Count != 0 || !observed.Contains(parent.Party.Id))
            throw new InvalidDataException("Local refinement requires a legal completed parent from this independent arm.");

        using var timing = TowerPerformanceTrace.Measure("local-refinement.construct");
        var slots = parent.Party.Builds.Keys.Order().ToArray();
        var positions = input.Budget.EssenceSlots;
        var maximum = checked(slots.Length * positions * pool.Length);
        var offset = random.Next(maximum);
        var skips = new SortedDictionary<string, int>(StringComparer.Ordinal);
        void Skip(string reason) => skips[reason] = skips.GetValueOrDefault(reason) + 1;
        for (var check = 0; check < maximum; check++)
        {
            token.ThrowIfCancellationRequested();
            var index = (offset + check) % maximum;
            var slot = slots[index / (positions * pool.Length)];
            var position = index / pool.Length % positions;
            var added = pool[index % pool.Length];
            var removed = parent.Party.Builds[slot][position];
            if (added == removed) { Skip("unchanged"); continue; }
            var recipe = parent.Party.Builds[slot].ToArray(); recipe[position] = added;
            var builds = parent.Party.Builds.ToDictionary(p => p.Key, p => p.Value);
            builds[slot] = recipe;
            var choice = Choice(builds, TowerDiscoveryRefinementSearch.LocalOperator, null);
            if (choice.Rejection is not null) { Skip(choice.Rejection); continue; }
            if (observed.Contains(choice.Party!.Id)) { Skip("already-measured"); continue; }
            return new(choice, new(maximum, check + 1, offset, "novel-local-edit", slot, removed, added, skips));
        }

        // The ordinary controller records and charges this duplicate, without an evaluator
        // call or fresh refill. No extra attempt or inventory/role exception is granted.
        return new(new(parent.Party, TowerDiscoveryRefinementSearch.LocalOperator, null, null),
            new(maximum, maximum, offset, "exhausted", null, null, null, skips));
    }
}
