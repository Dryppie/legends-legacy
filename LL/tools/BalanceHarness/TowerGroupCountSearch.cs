using System.Globalization;
using System.Text.Json.Serialization;
using Common.Randomness;

namespace BalanceHarness;

public sealed record BossGroupCountChoice(int FreshIndex, int? GuidedIndex, string Route, int? GroupIndex,
    int RequestedOwners, BossJoinedGroup? Group,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BossGroupVariation? Variation = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BossGroupAllocation? Allocation = null);
public sealed record BossGroupCountTrace(string CatalogueHash, int RetainedGroups, bool CatalogueTruncated,
    BossGroupCountChoice Choice, int PlacedOwners, int? FinalOwners, string Outcome,
    IReadOnlyList<BossJoinedInsertion> Insertions,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BossGroupCompletionTrace? Completion = null);
internal sealed record BossGroupCountReservation(IReadOnlyDictionary<int, IReadOnlyList<string>> Prefix,
    IReadOnlyList<BossJoinedInsertion> Insertions, int PlacedOwners, string? Rejection);

/// <summary>Outcome-independent group/count coverage; only completed legal parties can be evaluated.</summary>
public static class TowerGroupCountSearch
{
    public const string Version = "independent-group-count-v1";
    public const string Method = "group-count-joint";

    internal static BossGroupCountChoice Select(BossJoinedCatalogue catalogue, int freshIndex, int owners, int seed)
    {
        if (freshIndex < 0 || owners < 1) throw new InvalidDataException("Invalid group/count schedule position.");
        if (freshIndex % 8 == 7 || catalogue.Groups.Count == 0)
            return new(freshIndex, null, catalogue.Groups.Count == 0 ? "empty-catalogue-uniform" : "uniform", null, 0, null);
        var guided = freshIndex - freshIndex / 8;
        var groups = catalogue.Groups.OrderBy(g => g.Id, StringComparer.Ordinal).ToArray();
        var offset = (int)((uint)StableRandom.Seed(Version, seed.ToString(CultureInfo.InvariantCulture)) % (uint)groups.Length);
        var visit = guided % groups.Length;
        var index = (visit + offset) % groups.Length;
        // One visit per group per sweep; each later sweep advances its owner count.
        // The diagonal starts the first sweep with varied counts rather than all single-owner teams.
        var count = 1 + (int)(((long)guided / groups.Length + visit) % owners);
        return new(freshIndex, guided, "group-count", index, count, groups[index]);
    }

    internal static BossGroupCountReservation Reserve(BossDiscoveryInputs input, BossJoinedGroup group, int count, Random random)
    {
        if (count < 1 || count > input.RequiredPartySize) throw new InvalidDataException("Invalid requested group owner count.");
        var planned = Enumerable.Range(1, input.RequiredPartySize).ToDictionary(slot => slot, _ => new List<string>());
        var slots = planned.Keys.ToArray(); random.Shuffle(slots);
        var insertions = new List<BossJoinedInsertion>();
        foreach (var slot in slots.Take(count))
        {
            var insertion = TowerJoinedMechanics.Insert(input, group, slot, planned, "group-count", 1);
            insertions.Add(insertion);
            if (insertion.Outcome != "inserted")
            {
                // Earlier entries describe tentative work, rolled back as one reservation.
                return new(planned.Keys.ToDictionary(s => s, _ => (IReadOnlyList<string>)Array.Empty<string>()),
                    insertions, 0, insertion.Outcome);
            }
        }
        return new(planned.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.Order(StringComparer.Ordinal).ToArray()),
            insertions, count, null);
    }

    internal static int Owners(PartyChoice party, BossJoinedGroup group) =>
        party.Builds.Values.Count(ids => group.EssenceIds.All(ids.Contains));
}

public sealed partial class TowerBossPartyGenerator
{
    internal BossGeneratedChoice FreshGroupCount(Random random, int freshIndex, int generationSeed)
    {
        if (input.Generation.PolicyVersion != TowerGroupCountSearch.Version || joinedCatalogue is null)
            throw new InvalidDataException("Group/count construction requires its explicit policy.");
        var schedule = TowerGroupCountSearch.Select(joinedCatalogue, freshIndex, input.RequiredPartySize, generationSeed);
        return CompleteGroupCount(schedule, random, random);
    }

    private BossGeneratedChoice CompleteGroupCount(BossGroupCountChoice schedule, Random placementRandom, Random fillerRandom, int? completionSeed = null)
    {
        BossGroupCompletionTrace? completion = null;
        BossGroupCountTrace Trace(int placed, int? final, string outcome, IReadOnlyList<BossJoinedInsertion> insertions) =>
            new(HarnessJson.Hash(joinedCatalogue!), joinedCatalogue!.Groups.Count, joinedCatalogue.Truncated, schedule, placed, final, outcome, insertions, completion);
        if (schedule.Group is null)
        {
            var uniform = Fresh(fillerRandom, false);
            return uniform with { GroupCount = Trace(0, null, uniform.Rejection ?? "complete", []) };
        }
        var reserved = TowerGroupCountSearch.Reserve(input, schedule.Group, schedule.RequestedOwners, placementRandom);
        if (reserved.Rejection is not null)
            return new(null, "group-count", null, "group-reservation-" + reserved.Rejection,
                GroupCount: Trace(0, null, "reservation-rolled-back", reserved.Insertions));
        var prefix = reserved.Prefix;
        if (completionSeed is { } seed)
        {
            var plan = TowerGroupCompletionSearch.Complete(input, mechanics.Coverage!, schedule.Group, reserved, seed, schedule.Allocation?.PriorityKind);
            prefix = plan.Prefix; completion = plan.Trace;
        }
        var choice = FreshCoverage(fillerRandom, completeCores: false, prefix: prefix);
        var owners = choice.Party is null ? (int?)null : TowerGroupCountSearch.Owners(choice.Party, schedule.Group);
        // Coverage/filler can accidentally complete the group on another owner. Never label that as the requested count.
        var rejection = choice.Rejection ?? (owners == schedule.RequestedOwners ? null : "group-count-drift");
        return choice with { Intent = "group-count:" + schedule.RequestedOwners.ToString(CultureInfo.InvariantCulture),
            Rejection = rejection, GroupCount = Trace(reserved.PlacedOwners, owners, rejection ?? "complete", reserved.Insertions) };
    }
}
