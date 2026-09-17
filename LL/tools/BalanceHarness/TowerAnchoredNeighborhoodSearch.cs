using System.Globalization;
using System.Text.Json;
using Common.Randomness;

namespace BalanceHarness;

public sealed record BossAnchoredEdit(int PartySlot, string Removed, string Added);
public sealed record BossAnchoredBatch(string PrimaryReferenceId, IReadOnlyList<int> CharacterOrder,
    IReadOnlyDictionary<int, int> LegalOptionsPerSlot);

/// <summary>A fixed, balanced sample of one supplied team's legal single-edit neighborhood.</summary>
public static class TowerAnchoredNeighborhoodSearch
{
    public const string Version = "anchored-neighborhood-v1";
    public const int Neighbors = 44, Candidates = 46;
    internal const string BatchArtifact = "anchored-candidate-batch.json";
    internal static bool IsFrozenBatch(BossGenerationResult result) => result.Version == Version
        && result.Arms.Count == 1 && result.Arms[0].StopReason == "BatchFrozen";

    internal static void Validate(TowerBossDiscoveryDefinition d)
    {
        if (d.Mode != TowerBossDiscovery.Improve || d.RequiredPartySize != 10 || d.Budget.EssenceSlots != 5
            || d.Generation.CandidatesPerArm != Candidates || d.Contexts.Count != 1
            || d.Stages.Schedules.Values.Any(s => s.Discovery.Count != 8 || s.Selection.Count != 32)
            || d.PrimaryReferenceId is null || !d.Starts.Any(s => s.ReferenceId == d.PrimaryReferenceId))
            throw new InvalidDataException("Anchored neighborhood requires an explicit supplied primary reference, ten characters with five Essences, 46 candidates, eight discovery and 32 selection values in one context.");
    }

    private static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, HarnessJson.Options), HarnessJson.Options)!;

    public static async Task<BossGenerationResult> RunAsync(TowerBossDiscoveryDefinition definition,
        Func<PartyChoice, string, CancellationToken, Task<BossDiscoveryMeasurement>> evaluate,
        CancellationToken token = default, Action<BossGenerationResult>? checkpoint = null)
    {
        ArgumentNullException.ThrowIfNull(evaluate);
        var d = Copy(definition);
        TowerBossDiscovery.Validate(d);
        if (d.Generation.PolicyVersion != Version) throw new InvalidDataException("Use the explicit anchored-neighborhood contract.");
        var input = TowerBossDiscovery.CopyGenerationInputs(d);
        var seed = d.Generation.Seeds.Single();
        var method = d.Generation.Methods.Single();
        var armId = method + "-" + seed.ToString(CultureInfo.InvariantCulture);
        var proposals = new List<BossGeneratedProposal>();
        var rows = new List<BossDiscoveryMeasurement>();
        BossAnchoredBatch? batch = null;
        var status = "Incomplete"; var stop = "Constructing"; string? error = null;
        PartyChoice[] shortlist = [];
        BossGenerationArm Arm() => new(method, seed, stop, proposals.ToArray(), rows.ToArray(), AnchoredBatch: batch);
        BossGenerationResult Report() => new(Version, status, [Arm()], shortlist, error);
        void Snapshot(string reason) { stop = reason; checkpoint?.Invoke(Copy(Report())); }
        try
        {
            token.ThrowIfCancellationRequested();
            // Construction has no access to measurements. All quotas must be possible before any combat.
            var frozen = Freeze(d, armId, token);
            batch = frozen.Batch;
            proposals.AddRange(frozen.Proposals);
            Snapshot("BatchFrozen");
            for (var i = 0; i < proposals.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var proposal = proposals[i];
                proposals[i] = proposal with { Result = "evaluating" };
                Snapshot("Running");
                var row = await evaluate(Copy(proposal.Party!), armId, token);
                if (row is null || row.Id != proposal.Party!.Id || row.Fitness is null
                    || row.Fitness != TowerBossGeneration.Fitness(input, row.Cells, row.Fitness.VictoryDuration)
                    || row.Behavior is null || new[] { row.Behavior.HealthDeficit, row.Behavior.Healing,
                        row.Behavior.DamagePrevented, row.Behavior.DeniedTicks, row.Behavior.SummonActiveTicks }.Any(v => !double.IsFinite(v) || v < 0))
                    throw new InvalidDataException("Anchored search requires a complete finite discovery measurement.");
                rows.Add(Copy(row));
                proposals[i] = proposal with { Result = "evaluated" };
                Snapshot("Running");
            }
            token.ThrowIfCancellationRequested();
            shortlist = TowerSuppliedCompositionSearch.IncumbentShortlist(d, [Arm()]);
            if (shortlist.Length != 4) throw new InvalidDataException("Anchored nomination requires two supplied teams and two challengers.");
            status = "Complete";
            stop = "CandidateBudgetReached";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { status = stop = "Cancelled"; }
        catch (Exception exception) { status = stop = "Invalid"; error = exception.GetType().Name + ": " + exception.Message; }
        var result = Report();
        checkpoint?.Invoke(Copy(result));
        return result;
    }

    private static (BossAnchoredBatch Batch, BossGeneratedProposal[] Proposals) Freeze(
        TowerBossDiscoveryDefinition d, string armId, CancellationToken token)
    {
        var seed = d.Generation.Seeds.Single();
        var root = seed.ToString(CultureInfo.InvariantCulture);
        var order = Enumerable.Range(1, 10).ToArray();
        Shuffle(order, new Random(StableRandom.Seed(Version, root, "character-order")));
        var primary = d.Starts.Single(s => s.ReferenceId == d.PrimaryReferenceId);
        var families = d.AllowedEssences.ToDictionary(e => e.Id, e => e.Family, StringComparer.Ordinal);
        var pool = families.Keys.Order(StringComparer.Ordinal).ToArray();
        var used = primary.Party.Builds.Values.SelectMany(v => v).GroupBy(id => id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var suppliedIds = d.Starts.Select(s => s.Party.Id).ToHashSet(StringComparer.Ordinal);
        var options = new Dictionary<int, BossAnchoredEdit[]>();
        var counts = new Dictionary<int, int>();
        foreach (var slot in order)
        {
            token.ThrowIfCancellationRequested();
            var ids = primary.Party.Builds[slot];
            var legal = new List<BossAnchoredEdit>();
            foreach (var removed in ids)
            foreach (var added in pool)
            {
                token.ThrowIfCancellationRequested();
                if (ids.Contains(added, StringComparer.Ordinal)
                    || ids.Where(id => id != removed).Any(id => StringComparer.OrdinalIgnoreCase.Equals(families[id], families[added]))
                    || d.OwnedCopies is not null && used.GetValueOrDefault(added) >= d.OwnedCopies.GetValueOrDefault(added)) continue;
                var edit = new BossAnchoredEdit(slot, removed, added);
                if (!suppliedIds.Contains(Apply(primary.Party, edit).Id)) legal.Add(edit);
            }
            var quota = Array.IndexOf(order, slot) < 4 ? 5 : 4;
            if (legal.Count < quota)
                throw new InvalidDataException($"Character {slot} has {legal.Count} legal non-supplied single edits; its frozen quota requires {quota}.");
            var shuffled = legal.ToArray();
            Shuffle(shuffled, new Random(StableRandom.Seed(Version, root, "character-options", slot.ToString(CultureInfo.InvariantCulture))));
            options.Add(slot, shuffled.Take(quota).ToArray());
            counts.Add(slot, legal.Count);
        }
        var proposals = new List<BossGeneratedProposal>();
        void Add(PartyChoice party, string operation, string parent, string reference, BossAnchoredEdit? edit) => proposals.Add(new(
            new($"{armId}-proposal-{proposals.Count:D5}", seed, d.Generation.Methods.Single(), operation, [parent], [reference]),
            party, operation, null, "proposed", AnchoredEdit: edit));
        foreach (var start in d.Starts.OrderBy(s => s.Id, StringComparer.Ordinal))
            Add(start.Party, "supplied", start.Id, start.ReferenceId, null);
        var parentId = proposals.Single(p => p.Party!.Id == primary.Party.Id).Provenance.Id;
        // Four full passes, then four extra slots from the same root-shuffled order.
        for (var i = 0; i < Neighbors; i++)
        {
            var edit = options[order[i % 10]][i / 10];
            var party = Apply(primary.Party, edit);
            TowerBossDiscovery.ValidateParty(d, party);
            Add(party, "single", parentId, primary.ReferenceId, edit);
        }
        if (proposals.Select(p => p.Party!.Id).Distinct(StringComparer.Ordinal).Count() != Candidates)
            throw new InvalidDataException("Frozen anchored batch contains duplicate recipes.");
        TowerBossDiscovery.ValidateProvenance(d, proposals.Select(p => p.Provenance).ToArray());
        return (new(primary.ReferenceId, order, counts), proposals.ToArray());
    }

    private static PartyChoice Apply(PartyChoice primary, BossAnchoredEdit edit) => TowerPartySelection.Choice(Version,
        primary.Builds.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => (IReadOnlyList<string>)(p.Key == edit.PartySlot
            ? p.Value.Where(id => id != edit.Removed).Append(edit.Added).Order(StringComparer.Ordinal).ToArray() : p.Value.ToArray())));

    private static void Shuffle<T>(T[] values, Random random)
    {
        for (var i = values.Length - 1; i > 0; i--)
        { var j = random.Next(i + 1); (values[i], values[j]) = (values[j], values[i]); }
    }
}
