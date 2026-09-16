namespace BalanceHarness;

public sealed record BossJoinedGroup(string Id, IReadOnlyList<string> EssenceIds,
    IReadOnlyList<string> SourceCoreIds, IReadOnlyList<string> EvidenceKeys);
public sealed record BossJoinedCatalogue(int DistinctBaseRecipes, int ConsideredBaseRecipes, int ExaminedPairs,
    int DistinctLegalUnions, bool Truncated, IReadOnlyList<BossJoinedGroup> Groups);
public sealed record BossJoinedInsertion(int Slot, string Route, int CompatibleGroups, BossJoinedGroup? Selected,
    string Outcome, IReadOnlyList<string> Before, IReadOnlyList<string> After);
public sealed record BossJoinedTrace(string CatalogueHash, int RetainedGroups, bool CatalogueTruncated,
    string Route, IReadOnlyList<BossJoinedInsertion> Insertions);

/// <summary>Bounded structural hypotheses from overlapping authored groups, without combat or recipe inputs.</summary>
public static class TowerJoinedMechanics
{
    public const string Version = "independent-joined-mechanics-v1";
    public const string Method = "joined-mechanics-joint";
    public const int MaximumBaseRecipes = 128;
    public const int MaximumGroups = 256;
    public const int MaximumMembers = 5;

    public static BossJoinedCatalogue Create(BossDiscoveryInputs input, IReadOnlyList<BossMechanicCore> cores)
    {
        var families = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Family, StringComparer.Ordinal);
        bool Legal(IReadOnlyList<string> ids) => ids.Count <= Math.Min(input.Budget.EssenceSlots, MaximumMembers)
            && ids.All(id => families.ContainsKey(id) && (input.OwnedCopies is null || input.OwnedCopies.GetValueOrDefault(id) > 0))
            && ids.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() == ids.Count;
        // Same recipe with several evidence paths gets one sampling seat. Choose its stable first path.
        var bases = cores.Select(c => c with { EssenceIds = c.EssenceIds.Order(StringComparer.Ordinal).ToArray() })
            .Where(c => c.EssenceIds.Count is >= 2 and <= 3 && Legal(c.EssenceIds))
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .DistinctBy(c => HarnessJson.Hash(c.EssenceIds)).ToArray();
        var considered = bases.Take(MaximumBaseRecipes).ToArray();
        var unions = new Dictionary<string, BossJoinedGroup>(StringComparer.Ordinal);
        var examined = 0;
        for (var i = 0; i < considered.Length; i++)
        for (var j = i + 1; j < considered.Length; j++)
        {
            examined++;
            var a = considered[i]; var b = considered[j];
            if (!a.EssenceIds.Intersect(b.EssenceIds, StringComparer.Ordinal).Any()) continue;
            var ids = a.EssenceIds.Concat(b.EssenceIds).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            if (ids.Length <= Math.Max(a.EssenceIds.Count, b.EssenceIds.Count) || !Legal(ids)) continue;
            var id = HarnessJson.Hash(new { policy = Version, essences = ids });
            unions.TryAdd(id, new(id, ids, new[] { a.Id, b.Id }.Order(StringComparer.Ordinal).ToArray(),
                a.EvidenceKeys.Concat(b.EvidenceKeys).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()));
        }
        return new(bases.Length, considered.Length, examined, unions.Count,
            bases.Length > MaximumBaseRecipes || unions.Count > MaximumGroups,
            unions.Values.OrderBy(g => g.Id, StringComparer.Ordinal).Take(MaximumGroups).ToArray());
    }

    internal static string? Rejection(BossDiscoveryInputs input, BossJoinedGroup group, int slot,
        IReadOnlyDictionary<int, List<string>> planned)
    {
        var families = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Family, StringComparer.Ordinal);
        var union = planned[slot].Concat(group.EssenceIds).Distinct(StringComparer.Ordinal).ToArray();
        if (union.Any(id => !families.ContainsKey(id))) return "unavailable-member";
        if (union.Length > input.Budget.EssenceSlots) return "slot-limit";
        if (union.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != union.Length) return "family-conflict";
        if (input.OwnedCopies is not null && union.Any(id =>
            planned.Where(p => p.Key != slot).Count(p => p.Value.Contains(id)) + 1 > input.OwnedCopies.GetValueOrDefault(id)))
            return "owned-copies-exhausted";
        return null;
    }

    internal static BossJoinedInsertion Insert(BossDiscoveryInputs input, BossJoinedGroup group, int slot,
        IReadOnlyDictionary<int, List<string>> planned, string route, int compatible)
    {
        var before = planned[slot].Order(StringComparer.Ordinal).ToArray();
        var rejection = Rejection(input, group, slot, planned);
        if (rejection is null)
            foreach (var id in group.EssenceIds.Where(id => !planned[slot].Contains(id))) planned[slot].Add(id);
        var after = planned[slot].Order(StringComparer.Ordinal).ToArray();
        return new(slot, route, compatible, group, rejection ?? (before.SequenceEqual(after) ? "already-present" : "inserted"), before, after);
    }
}

public sealed partial class TowerBossPartyGenerator
{
    private readonly BossJoinedCatalogue? joinedCatalogue;
    private BossJoinedTrace? JoinedTrace(string route, IReadOnlyList<BossJoinedInsertion>? insertions = null) =>
        joinedCatalogue is null ? null : new(HarnessJson.Hash(joinedCatalogue), joinedCatalogue.Groups.Count,
            joinedCatalogue.Truncated, route, insertions ?? []);

    private BossJoinedInsertion InsertJoined(Random random, int slot, IReadOnlyDictionary<int, List<string>> planned)
    {
        var before = planned[slot].Order(StringComparer.Ordinal).ToArray();
        if (random.Next(2) != 0) return new(slot, "skipped", 0, null, "not-sampled", before, before);
        var compatible = joinedCatalogue!.Groups.Where(g => TowerJoinedMechanics.Rejection(input, g, slot, planned) is null).ToArray();
        if (compatible.Length > 0)
            return TowerJoinedMechanics.Insert(input, compatible[random.Next(compatible.Length)], slot, planned, "joined", compatible.Length);
        var fallback = (mechanics.Cores ?? []).Select(c => new BossJoinedGroup(c.Id, c.EssenceIds, [c.Id], c.EvidenceKeys))
            .Where(g => TowerJoinedMechanics.Rejection(input, g, slot, planned) is null).ToArray();
        if (fallback.Length > 0)
            return TowerJoinedMechanics.Insert(input, fallback[random.Next(fallback.Length)], slot, planned, "base-core", fallback.Length);
        // Retain one representative failed insertion with its exact reason; do not partially consume ownership.
        if (joinedCatalogue.Groups.Count > 0)
            return TowerJoinedMechanics.Insert(input, joinedCatalogue.Groups[random.Next(joinedCatalogue.Groups.Count)], slot, planned, "joined-rejected", 0);
        return new(slot, "no-groups", 0, null, "no-compatible-group", before, before);
    }
}
