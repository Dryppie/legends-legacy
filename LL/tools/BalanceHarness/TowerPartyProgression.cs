using Domain.Models.Essences;
using Domain.Models.Items;

namespace BalanceHarness;

public sealed record TowerPartyStarts(int SchemaVersion, string Source, IReadOnlyList<CharacterShortlist> Shortlists,
    IReadOnlyDictionary<int, IReadOnlyList<string>> ReferenceBuilds);

/// <summary>Explicit, separate progression budgets and deterministic extensions of historical seeds.</summary>
public static class TowerPartyProgression
{
    public const string Version = "tower-party-search-v2";
    public static string Algorithm(TowerPartySearchDefinition d) => d.SchemaVersion == 1 ? TowerPartySelection.Version : d.SchemaVersion == 3 ? TowerWholeParty.Version : Version;
    public static int[] CombatSeeds(TowerPartySearchDefinition d) => TowerLoadoutPilot.Schedule(d.CharacterSeed, d.CharacterSamples).Values.SelectMany(s => s)
        .Concat(TowerLoadoutPilot.Schedule(d.PartySeed, d.PartySamples).Values.SelectMany(s => s))
        .Concat(TowerLoadoutPilot.Schedule(d.ConfirmationSeed, d.ConfirmationSamples).Values.SelectMany(s => s)).ToArray();

    public static async Task RunBatchAsync(string root, string catalogs, string output, CancellationToken token, Action<string>? progress = null)
    {
        if (Path.Exists(output)) throw new IOException("Choose a new progression-study directory.");
        var excluded = HistoricalSeeds.ToList(); var definitions = new Dictionary<int, TowerPartySearchDefinition>();
        foreach (var slots in new[] { 6, 5, 7, 8, 9, 10 })
        {
            var d = Definition(root, catalogs, slots, 202609110 + slots, excluded, thorough: slots == 6);
            definitions.Add(slots, d); excluded.AddRange(CombatSeeds(d));
        }
        Directory.CreateDirectory(output);
        HarnessJson.WriteNew(Path.Combine(output, "batch-plan.json"), new { MaximumBattles = definitions.Values.Sum(TowerPartySelection.Validate), Definitions = definitions });
        var completed = new List<object>(); var status = "Incomplete";
        try
        {
            foreach (var pair in definitions)
            {
                progress?.Invoke($"Starting {pair.Key}-slot cohort: {TowerPartySelection.Validate(pair.Value)} maximum combats.");
                var report = await TowerPartySearch.RunAsync(root, catalogs, Path.Combine(output, $"slots-{pair.Key}"), pair.Value, token, progress);
                completed.Add(new { Slots = pair.Key, report.Status, report.ActualBattles, report.Selection });
            }
            status = "Complete";
        }
        catch (OperationCanceledException) { status = "Cancelled"; throw; }
        finally { HarnessJson.WriteNew(Path.Combine(output, "batch-results.json"), new { Status = status, Completed = completed }); }
    }
    public static int[] HistoricalSeeds => TowerPartySelection.Default.ExcludedCombatSeeds
        .Concat(TowerLoadoutPilot.Schedule(202609105, 3).Values.SelectMany(s => s))
        .Concat(TowerLoadoutPilot.Schedule(202609106, 4).Values.SelectMany(s => s))
        .Concat(TowerLoadoutPilot.Schedule(202609107, 40).Values.SelectMany(s => s)).Distinct().Order().ToArray();

    public static TowerSearchBudget Budget(int slots) => slots switch
    {
        4 => new(4, 30, 1, 1, ItemQuality.Standard, 1),
        5 => new(5, 40, 1, 2, ItemQuality.Standard, 1),
        6 => new(6, 50, 2, 2, ItemQuality.Standard, 10),
        7 => new(7, 60, 2, 3, ItemQuality.Fine, 10),
        8 => new(8, 70, 2, 3, ItemQuality.Fine, 10),
        9 => new(9, 80, 2, 4, ItemQuality.Fine, 10),
        10 => new(10, 90, 2, 4, ItemQuality.Fine, 10),
        _ => throw new InvalidDataException("Choose four through ten Essence slots.")
    };

    public static void ValidateBudget(TowerPartySearchDefinition d)
    {
        if (d.SchemaVersion == 1)
        {
            if (d.Budget is not null || d.StartingLoadouts is not null || d.ReferenceBuilds is not null)
                throw new InvalidDataException("Legacy four-slot studies cannot contain schema-2 fields.");
            return;
        }
        var b = d.Budget;
        bool Legal(IReadOnlyList<string>? ids) => ids is not null && ids.Count == b!.EssenceSlots
            && ids.All(id => !string.IsNullOrWhiteSpace(id)) && ids.Distinct(StringComparer.Ordinal).Count() == ids.Count;
        if (b is null || b.EssenceSlots is < 4 or > 10 || b.CharacterLevel is < 1 or > 100
            || EssenceSlotProgression.GetUnlockedSlotCount(b.CharacterLevel) < b.EssenceSlots
            || b.Tier is < 1 or > 2 || b.Rank is < 1 or > 4 || b.Quality is not (ItemQuality.Standard or ItemQuality.Fine)
            || b.PriorityFloor is < 1 or > 15 || d.StartingLoadouts is null || d.ReferenceBuilds is null
            || !d.StartingLoadouts.Keys.Order().SequenceEqual(TowerPartySelection.Targets(d))
            || !d.ReferenceBuilds.Keys.Order().SequenceEqual(TowerPartySelection.Targets(d))
            || d.StartingLoadouts.Values.Any(starts => starts is not { Count: > 0 and <= 4 } || starts.Count + 1 >= d.CandidatesPerArm || starts.Any(ids => !Legal(ids)))
            || d.ReferenceBuilds.Values.Any(ids => !Legal(ids)))
            throw new InvalidDataException("Invalid explicit progression budget, historical starts or reference party; reserve fresh exploration capacity.");
    }

    public static string[] Extend(IReadOnlyList<string> prefix, IReadOnlyList<string> control, int count, IReadOnlyDictionary<string, string> families)
    {
        if (prefix.Count > count || prefix.Any(id => !families.ContainsKey(id))
            || prefix.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != prefix.Count)
            throw new InvalidDataException("Historical seed is not a legal ordered prefix.");
        var result = prefix.ToList(); var used = prefix.Select(id => families[id]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var id in control.Concat(families.Keys.Order(StringComparer.Ordinal)))
        {
            if (result.Count == count) break;
            if (used.Add(families[id])) result.Add(id);
        }
        if (result.Count != count) throw new InvalidDataException("Insufficient source families to extend a historical loadout.");
        return result.ToArray();
    }

    public static TowerPartySearchDefinition Definition(string root, string catalogs, int slots, int master,
        IReadOnlyList<int>? exclusions = null, bool thorough = false)
    {
        var budget = Budget(slots);
        var scenarios = Scenarios(root, catalogs, budget);
        var historical = HarnessJson.Read<TowerPartyStarts>(Path.Combine(catalogs, "tower-party-starts.json"));
        if (historical.SchemaVersion != 1 || string.IsNullOrWhiteSpace(historical.Source)) throw new InvalidDataException("Invalid historical starts fixture.");
        var families = EssenceMechanicsInventory.Create(root, TowerBundle.ReadSettings(root).Threat).Essences.ToDictionary(e => e.Id, e => e.SourceMonsterId);
        var starts = new Dictionary<int, IReadOnlyList<IReadOnlyList<string>>>();
        var reference = new Dictionary<int, IReadOnlyList<string>>();
        foreach (var slot in TowerPartySelection.Slots)
        {
            var control = scenarios[0].Party.Single(p => p.PartySlot == slot).Build.EssenceIds;
            reference.Add(slot, Extend(historical.ReferenceBuilds[slot], control, slots, families));
            starts.Add(slot, historical.Shortlists.Single(s => s.Slot == slot).Candidates.Skip(1)
                .Select(c => (IReadOnlyList<string>)Extend(c.Essences, control, slots, families)).Append(reference[slot]).DistinctBy(ids => HarnessJson.Hash(ids)).ToArray());
        }
        int Seed(string label) => Common.Randomness.StableRandom.Seed("tower-party-progression-v2", master.ToString(System.Globalization.CultureInfo.InvariantCulture), label);
        var d = new TowerPartySearchDefinition(2, thorough ? [Seed("search-a"), Seed("search-b")] : [Seed("search-a")],
            8, thorough ? 2 : 1, thorough ? 16 : 12, 2, thorough ? 6 : 4, thorough ? 40 : 20,
            Seed("character"), Seed("party"), Seed("confirmation"), Seed("combinations"), thorough ? 16000 : 6000,
            (exclusions ?? HistoricalSeeds).Distinct().Order().ToArray(), budget, starts, reference);
        TowerPartySelection.Validate(d); return d;
    }

    public static IReadOnlyList<TowerScenario> Scenarios(string root, string catalogs, TowerSearchBudget b)
    {
        var curve = HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(catalogs, "tower-curve.json"));
        var prefix = $"slots-{b.EssenceSlots}-";
        var profiles = curve.Profiles.Where(p => p.Id.StartsWith(prefix, StringComparison.Ordinal)).Select(p => p with
        { Build = p.Build with { CharacterLevel = b.CharacterLevel, Tier = b.Tier, Rank = b.Rank, Quality = b.Quality },
            Assumptions = $"Separate {b.EssenceSlots}-slot cohort: level {b.CharacterLevel}, Uncommon {b.Quality} tier {b.Tier} rank {b.Rank}, baseline rolls. Provisional progression gear; level-1 unascended/unevolved Essences, no styles; hypothetical ownership." }).ToArray();
        var definition = curve with { SchemaVersion = 1, Id = $"tower-party-slots-{b.EssenceSlots}", Floors = Enumerable.Range(1, 15).ToArray(), Profiles = profiles,
            Parties = [new("balanced", "Fixed cell composition. Only absolute first-cell slots 1–4 are searched; other participants retain authored recipes.",
                [prefix + "guardian", prefix + "restorer", prefix + "striker", prefix + "striker", prefix + "controller"])] };
        return TowerBenchmark.Expand(definition, root, 1, 1).Select(s => s with { Party = s.Party.Select(p => p with
        { Build = p.Build with { IdentityEssenceIds = p.Build.EssenceIds.ToArray() } }).ToArray() }).ToArray();
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>> Contexts(string root, string catalogs, int target, TowerSearchBudget b)
    {
        var original = Scenarios(root, catalogs, b);
        var alternative = original.Select(s =>
        {
            var changed = TowerLoadoutPilot.Apply(s, target, new("previous-05", "Fixed alternative allies", [], true), s.Seeds);
            return changed with { Party = changed.Party.Select(p => (target == 0 ? TowerPartySelection.Slots.Contains(p.PartySlot) : p.PartySlot == target)
                ? s.Party.Single(q => q.PartySlot == p.PartySlot) : p).ToArray(),
                Assumptions = [.. s.Assumptions, "Alternative candidate-05 allies exclude all searched targets; joint contexts differ only in later cells."] };
        }).ToArray();
        return new Dictionary<string, IReadOnlyList<TowerScenario>> { [$"slots-{b.EssenceSlots}--balanced"] = original, [$"slots-{b.EssenceSlots}--previous-05"] = alternative };
    }
}
