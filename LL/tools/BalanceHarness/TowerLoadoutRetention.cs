namespace BalanceHarness;

/// <summary>Half ranked, half structurally diverse ordered modules from this arm's completed parties.</summary>
public static class TowerLoadoutRetention
{
    public const string Version = "independent-loadout-retention-v15";
    public const string Method = "retained-loadout-composition-joint";
    public static readonly string[] Methods = ["loadout-composition-joint", Method];
    public const int RankedSeats = 64;

    public static BossLoadoutModule[] Library(IEnumerable<BossDiscoveryMeasurement> measurements,
        IReadOnlyDictionary<string, BossGeneratedProposal> measured)
    {
        // Resolve duplicates to their highest-ranked complete source party, then lowest slot.
        // Inspect only measured rows: unevaluated/future proposals cannot enter the pool.
        var modules = new Dictionary<string, BossLoadoutModule>(StringComparer.Ordinal);
        string? method = null; int? seed = null;
        foreach (var row in TowerBossGeneration.Rank(measurements))
        {
            if (!measured.TryGetValue(row.Id, out var p) || p.Result != "evaluated" || p.Party?.Id != row.Id
                || p.Provenance.ReferenceIds.Count != 0 || (method is not null && method != p.Provenance.Method)
                || (seed is not null && seed != p.Provenance.GenerationSeed))
                throw new InvalidDataException("Retention requires completed independent source parties from one arm.");
            method = p.Provenance.Method; seed = p.Provenance.GenerationSeed;
            foreach (var (slot, essences) in p.Party.Builds.OrderBy(p => p.Key))
            {
                var id = HarnessJson.Hash(essences);
                modules.TryAdd(id, new(id, p.Provenance.Id, slot, essences.ToArray()));
            }
        }
        var pool = modules.Values.ToArray();
        var selected = pool.Take(RankedSeats).ToList();
        var remaining = pool.Skip(RankedSeats).ToArray();
        var distances = remaining.Select(m => selected.Min(s => Distance(m.Essences, s.Essences))).ToArray();
        while (selected.Count < TowerLoadoutComposition.Capacity && selected.Count < pool.Length)
        {
            // Largest distance to the nearest retained module; ties retain complete-party rank/slot order.
            var index = 0;
            for (var i = 1; i < remaining.Length; i++) if (distances[i] > distances[index]) index = i;
            var next = remaining[index]; selected.Add(next); distances[index] = -1;
            for (var i = 0; i < remaining.Length; i++)
                if (distances[i] >= 0) distances[i] = Math.Min(distances[i], Distance(remaining[i].Essences, next.Essences));
        }
        return selected.ToArray();
    }

    // Membership difference precedes ordering difference: one new essence exceeds all positional mismatches.
    internal static int Distance(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left.Count != right.Count) throw new InvalidDataException("Loadout distances require equal slot counts.");
        var missing = 0; var order = 0;
        for (var i = 0; i < left.Count; i++)
        {
            if (!right.Contains(left[i], StringComparer.Ordinal)) missing++;
            if (left[i] != right[i]) order++;
        }
        return missing * (left.Count + 1) + order;
    }
}
