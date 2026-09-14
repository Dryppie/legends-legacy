namespace BalanceHarness;

public sealed record BossLoadoutModule(string Id, string ProposalId, int SourceSlot, IReadOnlyList<string> Essences);
public sealed record BossLoadoutUse(BossLoadoutModule Module, IReadOnlyList<int> TargetSlots);
public sealed record BossLoadoutTrace(int LibraryCount, string LibraryHash, IReadOnlyList<BossLoadoutUse> Uses);
internal sealed record BossLoadoutProposal(BossGeneratedChoice Choice, string[] Parents, BossLoadoutTrace Trace);

/// <summary>Reusable ordered loadouts from this arm's measured parties. A loadout has no standalone fitness.</summary>
public static class TowerLoadoutComposition
{
    public const int Capacity = 128;

    public static BossLoadoutModule[] Library(IEnumerable<BossDiscoveryMeasurement> measurements,
        IReadOnlyDictionary<string, BossGeneratedProposal> measured)
    {
        var modules = new Dictionary<string, BossLoadoutModule>(StringComparer.Ordinal);
        foreach (var row in TowerBossGeneration.Rank(measurements))
        {
            var proposal = measured[row.Id];
            if (proposal.Result != "evaluated" || proposal.Party?.Id != row.Id || proposal.Provenance.ReferenceIds.Count != 0)
                throw new InvalidDataException("Loadouts require completed independently generated source parties.");
            foreach (var (slot, essences) in proposal.Party.Builds.OrderBy(p => p.Key))
            {
                var id = HarnessJson.Hash(essences);
                modules.TryAdd(id, new(id, proposal.Provenance.Id, slot, essences.ToArray()));
                if (modules.Count == Capacity) return modules.Values.ToArray();
            }
        }
        return modules.Values.ToArray();
    }
}

public sealed partial class TowerBossPartyGenerator
{
    internal BossLoadoutProposal CoordinateLoadouts(Random random, string operation, BossGeneratedProposal parent,
        IReadOnlyList<BossLoadoutModule> library)
    {
        if (input.Generation.PolicyVersion is not (TowerBossGeneration.LoadoutCompositionVersion or TowerGenerationFeedback.Version or TowerLoadoutRetention.Version or TowerPartyLineages.Version or TowerSearchAllocation.Version or TowerLateAllocation.Version or TowerSearchPortfolio.Version)
            || parent.Result != "evaluated" || parent.Party is null || Invalid(parent.Party) is not null
            || parent.Provenance.ReferenceIds.Count != 0 || library.Count is < 1 or > TowerLoadoutComposition.Capacity
            || library.Select(m => m.Id).Distinct().Count() != library.Count
            || library.Any(m => m.Id != HarnessJson.Hash(m.Essences) || m.Essences.Count != input.Budget.EssenceSlots
                || m.Essences.Any(id => !families.ContainsKey(id))
                || m.Essences.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != m.Essences.Count))
            throw new InvalidDataException("Coordinated loadouts require legal generated parents and ordered modules.");
        var builds = parent.Party.Builds.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.ToArray());
        var slots = builds.Keys.Order().ToArray(); random.Shuffle(slots);
        var uses = new List<BossLoadoutUse>();
        var parents = new List<string>();
        void Apply(BossLoadoutModule module, int[] targets)
        {
            foreach (var slot in targets) builds[slot] = module.Essences.ToArray();
            uses.Add(new(module, targets)); parents.Add(module.ProposalId);
        }
        BossLoadoutModule FromParent(int slot) => new(HarnessJson.Hash(parent.Party.Builds[slot]),
            parent.Provenance.Id, slot, parent.Party.Builds[slot].ToArray());
        switch (operation)
        {
            case "loadout-distribute":
                parents.Add(parent.Provenance.Id);
                Apply(library[random.Next(library.Count)], slots.Take(random.Next(1, slots.Length + 1)).ToArray());
                break;
            case "loadout-compose":
                // Every module count from one to party size is reachable. Random positive
                // partitions and positions are search choices, never historical recipe counts.
                var count = random.Next(1, Math.Min(slots.Length, library.Count) + 1);
                var donors = library.ToArray(); random.Shuffle(donors);
                var cuts = Enumerable.Range(1, slots.Length - 1).ToArray(); random.Shuffle(cuts);
                var ends = cuts.Take(count - 1).Append(slots.Length).Order().ToArray();
                var start = 0;
                for (var i = 0; i < count; i++)
                { Apply(donors[i], slots[start..ends[i]]); start = ends[i]; }
                break;
            case "loadout-refine":
                var source = FromParent(slots[0]);
                var targets = slots.Where(slot => parent.Party.Builds[slot].SequenceEqual(source.Essences)).ToArray();
                var ids = source.Essences.ToArray();
                var left = random.Next(ids.Length); var right = (left + 1 + random.Next(ids.Length - 1)) % ids.Length;
                switch (random.Next(3))
                {
                    case 0: ids[left] = pool[random.Next(pool.Length)]; break;
                    case 1: ids[left] = pool[random.Next(pool.Length)]; ids[right] = pool[random.Next(pool.Length)]; break;
                    case 2: (ids[left], ids[right]) = (ids[right], ids[left]); break;
                }
                Apply(source, targets);
                foreach (var slot in targets) builds[slot] = ids.ToArray();
                break;
            case "loadout-placement":
                parents.Add(parent.Provenance.Id);
                if (slots.Length >= 2)
                { Apply(FromParent(slots[0]), [slots[1]]); Apply(FromParent(slots[1]), [slots[0]]); }
                break;
            default: throw new InvalidDataException("Unknown coordinated loadout operator.");
        }
        // Enforce owned copies and all family/slot constraints after the whole coordinated
        // change. Never silently repair a rejected recipe or spend combat on it.
        return new(Choice(builds, operation, null), parents.Distinct(StringComparer.Ordinal).ToArray(),
            new(library.Count, HarnessJson.Hash(library), uses));
    }
}
