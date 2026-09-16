using System.Globalization;
using Common.Randomness;

namespace BalanceHarness;

public sealed record BossGroupAllocation(string Version, int VariantIndex, int VariantCount,
    IReadOnlyList<string> EligibleKinds, string? PriorityKind);
internal sealed record BossGroupAllocationEntry(BossGroupDiversityEntry Entry, IReadOnlyList<string> EligibleKinds, int VariantCount);

/// <summary>Give competing missing categories a bounded turn at scarce group-owner slots.</summary>
public static class TowerGroupAllocationSearch
{
    public const string Version = "independent-group-allocation-v1";
    public const string Method = "group-allocation-joint";

    internal static IReadOnlyList<BossGroupAllocationEntry> CreateOrder(BossDiscoveryInputs input,
        IReadOnlyList<BossCoverageFeature> coverage, IReadOnlyList<BossGroupDiversityEntry> order)
    {
        if (order.Count > TowerJoinedMechanics.MaximumGroups) throw new InvalidDataException("Unbounded allocation catalogue.");
        var families = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Family, StringComparer.Ordinal);
        return order.Select(entry => {
            var group = entry.Group.EssenceIds;
            var covered = coverage.Where(f => group.Contains(f.EssenceId)).Select(f => f.Kind).ToHashSet(StringComparer.Ordinal);
            string[] kinds = group.Count >= input.Budget.EssenceSlots ? [] : coverage.Where(f => !covered.Contains(f.Kind)
                && !group.Any(id => StringComparer.OrdinalIgnoreCase.Equals(families[id], families[f.EssenceId]))
                && (input.OwnedCopies is null || input.OwnedCopies.GetValueOrDefault(f.EssenceId) > 0))
                .Select(f => f.Kind).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            if (kinds.Length > TowerPartyCoverage.Kinds.Length || kinds.Any(k => !TowerPartyCoverage.Kinds.Contains(k)))
                throw new InvalidDataException("Invalid allocation categories.");
            return new BossGroupAllocationEntry(entry, kinds, kinds.Length > input.Budget.EssenceSlots - group.Count ? Math.Max(1, kinds.Length) : 1);
        }).ToArray();
    }

    internal static BossGroupCountChoice Select(IReadOnlyList<BossGroupAllocationEntry> order, int freshIndex, int owners, int seed)
    {
        if (freshIndex < 0 || owners < 1) throw new InvalidDataException("Invalid allocation schedule position.");
        if (freshIndex % 8 == 7 || order.Count == 0)
            return new(freshIndex, null, order.Count == 0 ? "empty-catalogue-uniform" : "uniform", null, 0, null);
        var guided = freshIndex - freshIndex / 8;
        var width = order.Sum(e => e.VariantCount);
        var sweep = guided / width;
        var offset = guided % width;
        var position = 0;
        while (offset >= order[position].VariantCount) offset -= order[position++].VariantCount;
        // A bundle shares the original diversity count, placement and filler seeds.
        var logicalGuided = sweep * order.Count + position;
        var logicalFresh = logicalGuided + logicalGuided / 7;
        var choice = TowerGroupDiversitySearch.Select(order.Select(e => e.Entry).ToArray(), logicalFresh, owners, seed);
        var entry = order[position];
        var label = choice.Variation!.FillerSeed.ToString(CultureInfo.InvariantCulture);
        var kinds = entry.EligibleKinds.OrderBy(k => (uint)StableRandom.Seed(TowerGroupCompletionSearch.Version, "kind", label, entry.Entry.Group.Id, k))
            .ThenBy(k => k, StringComparer.Ordinal).ToArray();
        return choice with { FreshIndex = freshIndex, GuidedIndex = guided, Route = "group-allocation",
            Allocation = new(Version, offset, entry.VariantCount, kinds, kinds.Length == 0 ? null : kinds[offset]) };
    }
}

public sealed partial class TowerBossPartyGenerator
{
    private int? allocationSeed;
    private IReadOnlyList<BossGroupAllocationEntry>? allocationOrder;

    internal BossGeneratedChoice FreshGroupAllocation(Random random, int freshIndex, int generationSeed)
    {
        if (input.Generation.PolicyVersion != TowerGroupAllocationSearch.Version || joinedCatalogue is null)
            throw new InvalidDataException("Group allocation requires its explicit policy.");
        if (allocationOrder is null || allocationSeed != generationSeed)
        {
            allocationOrder = TowerGroupAllocationSearch.CreateOrder(input, mechanics.Coverage!, TowerGroupDiversitySearch.Order(joinedCatalogue, generationSeed));
            allocationSeed = generationSeed;
        }
        var schedule = TowerGroupAllocationSearch.Select(allocationOrder, freshIndex, input.RequiredPartySize, generationSeed);
        return schedule.Variation is { } v
            ? CompleteGroupCount(schedule, new Random(v.PlacementSeed), new Random(v.FillerSeed), v.FillerSeed)
            : CompleteGroupCount(schedule, random, random);
    }
}
