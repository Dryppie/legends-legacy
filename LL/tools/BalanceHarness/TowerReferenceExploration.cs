using System.Globalization;
using Common.Randomness;

namespace BalanceHarness;

public sealed record BossReferenceExplorationTrace(string ReferenceId, int Visit, int Radius,
    IReadOnlyList<int> ScheduledSlots, int ConstructionChecks);

/// <summary>Opt-in periodic exploration of each supplied reference; no outcome-dependent scheduling.</summary>
public static class TowerReferenceExploration
{
    public const string Version = "retained-composition-three-reference-exploration-v1";
    public const string OffsetVersion = "retained-composition-three-reference-exploration-offset-v1";
    public const string Operator = "reference-exploration";
    internal static bool IsSupported(string? version) => version is Version or OffsetVersion;

    internal sealed class Schedule
    {
        private readonly BossDiscoveryStart[] references;
        private readonly int[] visits, cursors;
        private int opportunity;

        internal Schedule(IReadOnlyList<BossDiscoveryStart> starts, int? ownerOffsetSeed = null)
        {
            references = starts.OrderBy(s => s.ReferenceId, StringComparer.Ordinal).ToArray();
            if (references.Length != 3 || references.Select(s => s.ReferenceId).Distinct(StringComparer.Ordinal).Count() != 3
                || references.Any(s => s.Party.Builds.Count < 3))
                throw new InvalidDataException("Reference exploration requires exactly three references and at least three character slots.");
            visits = new int[references.Length]; cursors = new int[references.Length];
            if (ownerOffsetSeed is { } seed)
                for (var index = 0; index < references.Length; index++)
                    // One independent draw per reference; never consume construction or mutation randomness.
                    cursors[index] = new Random(StableRandom.Seed(OffsetVersion, seed.ToString(CultureInfo.InvariantCulture),
                        references[index].ReferenceId, "owner-offset")).Next(references[index].Party.Builds.Count);
        }

        internal BossReferenceExplorationTrace Next()
        {
            var index = opportunity++ % references.Length;
            var reference = references[index]; var visit = visits[index]++;
            // Alternate within each reference, independently of the three-reference cycle.
            var radius = 2 + visit % 2;
            var owners = reference.Party.Builds.Keys.Order().ToArray();
            var slots = Enumerable.Range(0, radius).Select(i => owners[(cursors[index] + i) % owners.Length]).Order().ToArray();
            cursors[index] = (cursors[index] + radius) % owners.Length;
            // Consumption happens before construction, so rejection/duplication cannot refill a visit.
            return new(reference.ReferenceId, visit, radius, slots, 0);
        }
    }

    internal static (BossGeneratedChoice Choice, BossReferenceExplorationTrace Trace) Propose(
        BossDiscoveryInputs input, PartyChoice reference, BossReferenceExplorationTrace step, Random random)
    {
        var allowed = input.AllowedEssences.OrderBy(e => e.Id, StringComparer.Ordinal).ToArray();
        var families = allowed.ToDictionary(e => e.Id, e => e.Family, StringComparer.Ordinal);
        for (var check = 1; check <= TowerSuppliedCompositionSearch.ConstructionChecks; check++)
        {
            var builds = reference.Builds.ToDictionary(p => p.Key, p => p.Value.ToArray());
            var removed = new Dictionary<int, string>();
            // Free all selected copies first, allowing legal transfers between changed owners.
            foreach (var slot in step.ScheduledSlots)
            {
                var id = builds[slot][random.Next(builds[slot].Length)]; removed.Add(slot, id);
                builds[slot] = builds[slot].Where(value => value != id).ToArray();
            }
            var used = builds.Values.SelectMany(ids => ids).GroupBy(id => id, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            var complete = true;
            foreach (var slot in step.ScheduledSlots)
            {
                var candidates = allowed.Where(e => e.Id != removed[slot]
                    && !builds[slot].Any(id => StringComparer.OrdinalIgnoreCase.Equals(families[id], e.Family))
                    && (input.OwnedCopies is null || used.GetValueOrDefault(e.Id) < input.OwnedCopies.GetValueOrDefault(e.Id))).ToArray();
                if (candidates.Length == 0) { complete = false; break; }
                var selected = candidates[random.Next(candidates.Length)].Id;
                builds[slot] = builds[slot].Append(selected).ToArray();
                used[selected] = used.GetValueOrDefault(selected) + 1;
            }
            if (!complete) continue;
            var choice = TowerSuppliedCompositionSearch.Choice(input, builds, Operator);
            if (choice.Rejection is null && TowerSuppliedCompositionSearch.Distance(reference, choice.Party!) == 2 * step.Radius)
                return (choice, step with { ConstructionChecks = check });
        }
        return (new(null, Operator, null, "construction-exhausted"),
            step with { ConstructionChecks = TowerSuppliedCompositionSearch.ConstructionChecks });
    }
}
