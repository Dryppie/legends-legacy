using System.Globalization;
using Common.Randomness;

namespace BalanceHarness;

public sealed record BossGroupCompletionInsertion(int Slot, IReadOnlyList<string> Before, IReadOnlyList<string> After);
public sealed record BossGroupCompletionStep(string Kind, string EssenceId, int CompatibleProviders,
    IReadOnlyList<string> EvidenceKeys, IReadOnlyList<BossGroupCompletionInsertion> Insertions);
public sealed record BossGroupCompletionTrace(string Version, int Seed, IReadOnlyList<string> KindOrder,
    IReadOnlyList<BossGroupCompletionStep> Steps);
internal sealed record BossGroupCompletionPlan(IReadOnlyDictionary<int, IReadOnlyList<string>> Prefix, BossGroupCompletionTrace Trace);

/// <summary>Complete missing authored coverage on group owners; these categories do not estimate combat strength.</summary>
public static class TowerGroupCompletionSearch
{
    public const string Version = "independent-group-completion-v1";
    public const string Method = "group-completion-joint";

    internal static BossGroupCompletionPlan Complete(BossDiscoveryInputs input, IReadOnlyList<BossCoverageFeature> coverage,
        BossJoinedGroup group, BossGroupCountReservation reserved, int seed, string? firstKind = null)
    {
        if (reserved.Rejection is not null) throw new InvalidDataException("Completion requires a successful group reservation.");
        using var timing = TowerPerformanceTrace.Measure("generation.group-completion.reserve");
        var planned = reserved.Prefix.ToDictionary(p => p.Key, p => p.Value.ToList());
        var families = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Family, StringComparer.Ordinal);
        var features = coverage.GroupBy(f => f.EssenceId).ToDictionary(g => g.Key, g => g.ToArray());
        var used = planned.Values.SelectMany(x => x).GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        var label = seed.ToString(CultureInfo.InvariantCulture);
        uint Tie(string purpose, string id) => (uint)StableRandom.Seed(Version, purpose, label, group.Id, id);
        var owners = reserved.Insertions.Select(i => i.Slot).Distinct()
            .OrderBy(s => Tie("slot", s.ToString(CultureInfo.InvariantCulture))).ThenBy(s => s).ToArray();
        var kinds = coverage.Select(f => f.Kind).Distinct(StringComparer.Ordinal)
            .OrderBy(k => Tie("kind", k)).ThenBy(k => k, StringComparer.Ordinal).ToArray();
        if (firstKind is not null)
        {
            if (input.Generation.PolicyVersion != TowerGroupAllocationSearch.Version || !kinds.Contains(firstKind))
                throw new InvalidDataException("Category priority requires the explicit allocation policy and authored coverage.");
            kinds = kinds.OrderBy(k => k == firstKind ? 0 : 1).ToArray();
        }
        var steps = new List<BossGroupCompletionStep>();
        bool Missing(int slot, string kind) => !planned[slot].Any(id => features.GetValueOrDefault(id, []).Any(f => f.Kind == kind));
        bool Fits(int slot, string id) => planned[slot].Count < input.Budget.EssenceSlots
            && !planned[slot].Any(e => StringComparer.OrdinalIgnoreCase.Equals(families[e], families[id]));
        foreach (var kind in kinds)
        {
            // Every iteration inserts at least one Essence. Existing slot/copy limits bound the work.
            while (true)
            {
                var candidates = coverage.Where(f => f.Kind == kind).Select(f => new {
                    Feature = f,
                    Slots = owners.Where(s => Missing(s, kind) && Fits(s, f.EssenceId))
                        .Take(input.OwnedCopies is null ? owners.Length : Math.Max(0, input.OwnedCopies.GetValueOrDefault(f.EssenceId) - used.GetValueOrDefault(f.EssenceId))).ToArray()
                }).Where(c => c.Slots.Length > 0).OrderByDescending(c => c.Slots.Length)
                    .ThenBy(c => Tie("provider:" + kind, c.Feature.EssenceId)).ThenBy(c => c.Feature.EssenceId, StringComparer.Ordinal).ToArray();
                if (candidates.Length == 0) break;
                var selected = candidates[0]; var id = selected.Feature.EssenceId;
                var insertions = new List<BossGroupCompletionInsertion>();
                foreach (var slot in selected.Slots)
                {
                    var before = planned[slot].Order(StringComparer.Ordinal).ToArray();
                    planned[slot].Add(id); used[id] = used.GetValueOrDefault(id) + 1;
                    insertions.Add(new(slot, before, planned[slot].Order(StringComparer.Ordinal).ToArray()));
                }
                steps.Add(new(kind, id, candidates.Length, selected.Feature.EvidenceKeys.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(), insertions));
            }
        }
        return new(planned.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.Order(StringComparer.Ordinal).ToArray()),
            new(input.Generation.PolicyVersion == TowerGroupAllocationSearch.Version ? TowerGroupAllocationSearch.Version : Version, seed, kinds, steps));
    }
}

public sealed partial class TowerBossPartyGenerator
{
    internal BossGeneratedChoice FreshGroupCompletion(Random random, int freshIndex, int generationSeed)
    {
        if (input.Generation.PolicyVersion != TowerGroupCompletionSearch.Version || joinedCatalogue is null)
            throw new InvalidDataException("Group completion requires its explicit policy.");
        if (diversityOrder is null || diversitySeed != generationSeed)
        {
            diversityOrder = TowerGroupDiversitySearch.Order(joinedCatalogue, generationSeed);
            diversitySeed = generationSeed;
        }
        var schedule = TowerGroupDiversitySearch.Select(diversityOrder, freshIndex, input.RequiredPartySize, generationSeed);
        return schedule.Variation is { } v
            ? CompleteGroupCount(schedule, new Random(v.PlacementSeed), new Random(v.FillerSeed), v.FillerSeed)
            : CompleteGroupCount(schedule, random, random);
    }
}
