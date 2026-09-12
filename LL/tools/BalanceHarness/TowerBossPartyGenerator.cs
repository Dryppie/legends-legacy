using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record BossGenerationMechanics(int Floor, IReadOnlyList<string> CounterIntents,
    IReadOnlyList<TowerEssenceMechanics> Essences, IReadOnlyList<TowerEnablerConsumerPair> Interactions,
    IReadOnlyDictionary<string, string> SourceHashes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<BossMechanicCore>? Cores = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<BossCoverageFeature>? Coverage = null);
public sealed record BossGeneratedChoice(PartyChoice? Party, string Intent, string? Interaction, string? Rejection);

/// <summary>Reference-free complete-party construction. Capability weights propose hypotheses; combat measures strength.</summary>
public sealed partial class TowerBossPartyGenerator
{
    private readonly BossDiscoveryInputs input;
    private readonly BossGenerationMechanics mechanics;
    private readonly Dictionary<string, string> families;
    private readonly Dictionary<string, TowerEssenceMechanics> features;
    private readonly Dictionary<string, IReadOnlyList<string>> capabilities;
    private readonly Dictionary<string, string> patterns = new(StringComparer.Ordinal);
    private readonly string[] pool;
    private readonly string[][] groups;
    private readonly BigInteger[,] combinations;
    private readonly string[] intents;

    public TowerBossPartyGenerator(BossDiscoveryInputs input, BossGenerationMechanics mechanics)
    {
        // Neither API accepts a discovery definition, historical context builder or benchmark catalog.
        this.input = JsonSerializer.Deserialize<BossDiscoveryInputs>(JsonSerializer.Serialize(input, HarnessJson.Options), HarnessJson.Options)!;
        this.mechanics = JsonSerializer.Deserialize<BossGenerationMechanics>(JsonSerializer.Serialize(mechanics, HarnessJson.Options), HarnessJson.Options)!;
        TowerBossGeneration.ValidateInputs(this.input);
        if (mechanics is null || mechanics.Floor != input.Floor || mechanics.Essences is null
            || mechanics.Essences.Any(e => e is null || e.Signals is null)
            || mechanics.Essences.Select(e => e.Id).Distinct().Count() != mechanics.Essences.Count
            || !mechanics.Essences.Select(e => e.Id).Order(StringComparer.Ordinal).SequenceEqual(input.AllowedEssences.Select(e => e.Id).Order(StringComparer.Ordinal))
            || mechanics.CounterIntents is null || mechanics.CounterIntents.Any(string.IsNullOrWhiteSpace)
            || mechanics.SourceHashes is null || !mechanics.SourceHashes.Keys.Order().SequenceEqual(TowerBossInventory.SourceFiles.Order())
            || mechanics.SourceHashes.Any(p => input.ContentHashes.GetValueOrDefault(p.Key) != p.Value)
            || mechanics.Interactions is null)
            throw new InvalidDataException("Generation mechanics must match the frozen target and eligible content.");
        families = this.input.AllowedEssences.ToDictionary(e => e.Id, e => e.Family, StringComparer.Ordinal);
        if (input.Generation.PolicyVersion is TowerBossGeneration.MechanicsVersion or TowerBossGeneration.CoverageVersion && this.mechanics.Cores is null
            || this.mechanics.Cores is not null && (this.mechanics.Cores.Any(c => c is null || !TowerContractJson.Hash(c.Id)
                || c.Kind is not ("condition" or "basic-attack" or "chain") || c.EssenceIds is not { Count: >= 2 and <= 3 }
                || c.EssenceIds.Count > input.Budget.EssenceSlots || c.EssenceIds.Any(id => !families.ContainsKey(id))
                || c.EssenceIds.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != c.EssenceIds.Count
                || c.EvidenceKeys is not { Count: > 0 } || c.EvidenceKeys.Any(string.IsNullOrWhiteSpace) || string.IsNullOrWhiteSpace(c.Limitation))
                || this.mechanics.Cores.Select(c => c.Id).Distinct().Count() != this.mechanics.Cores.Count))
            throw new InvalidDataException("Invalid same-owner mechanic cores.");
        ValidateCoverage();
        features = this.mechanics.Essences.ToDictionary(e => e.Id, StringComparer.Ordinal);
        capabilities = features.ToDictionary(p => p.Key, p => CapabilitySignals(p.Value.Signals), StringComparer.Ordinal);
        if (features.Any(e => !StringComparer.OrdinalIgnoreCase.Equals(e.Value.SourceMonsterId, families[e.Key]))
            || this.mechanics.Interactions.Any(p => p is null || !families.ContainsKey(p.EnablerEssenceId)
                || !families.ContainsKey(p.ConsumerEssenceId) || p.EnablerEssenceId == p.ConsumerEssenceId
                || p.Compatibility is not ("same-owner-or-explicit-recipient-required" or "recipient-and-trigger-scope-unverified")))
            throw new InvalidDataException("Invalid generation family or interaction scope.");
        pool = families.Keys.Where(id => this.input.OwnedCopies is null || this.input.OwnedCopies.GetValueOrDefault(id) > 0)
            .Order(StringComparer.Ordinal).ToArray();
        groups = pool.GroupBy(id => families[id], StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => g.ToArray()).ToArray();
        combinations = new BigInteger[groups.Length + 1, input.Budget.EssenceSlots + 1];
        combinations[groups.Length, 0] = 1;
        for (var g = groups.Length - 1; g >= 0; g--)
        {
            combinations[g, 0] = 1;
            for (var k = 1; k <= input.Budget.EssenceSlots; k++)
                combinations[g, k] = combinations[g + 1, k] + groups[g].Length * combinations[g + 1, k - 1];
        }
        if (combinations[0, input.Budget.EssenceSlots] == 0) throw new InvalidDataException("No legal character can be constructed from this pool.");
        // Sustain remains reachable even against healing-feedback bosses; its efficacy is tested, never assumed.
        intents = this.mechanics.CounterIntents.Concat(["focused-damage", "protection", "sustain", "denial", "pressure"])
            .Where(i => i != "add-clearing" || this.mechanics.CounterIntents.Contains("add-clearing"))
            .Distinct().Order(StringComparer.Ordinal).ToArray();
    }

    public static BossGenerationMechanics FromInventory(BossDiscoveryInputs input, TowerBossInventoryReport inventory)
    {
        var boss = inventory.Bosses.Single(b => b.FloorNumber == input.Floor);
        if (boss.RequiredSlots != input.RequiredPartySize) throw new InvalidDataException("Generate the entire production party.");
        var allowed = input.AllowedEssences.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        return new(input.Floor, boss.CounterIntents,
            inventory.Essences.Where(e => allowed.Contains(e.Id)).OrderBy(e => e.Id, StringComparer.Ordinal).ToArray(),
            inventory.EnablerConsumerPairs.Where(p => allowed.Contains(p.EnablerEssenceId) && allowed.Contains(p.ConsumerEssenceId))
                .OrderBy(p => p.EnablerEssenceId, StringComparer.Ordinal).ThenBy(p => p.ConsumerEssenceId, StringComparer.Ordinal)
                .ThenBy(p => p.Mechanism, StringComparer.Ordinal).ThenBy(p => p.ProducerNodeKey, StringComparer.Ordinal)
                .ThenBy(p => p.ConsumerNodeKey, StringComparer.Ordinal).ToArray(), inventory.SourceHashes,
            input.Generation.PolicyVersion is TowerBossGeneration.MechanicsVersion or TowerBossGeneration.CoverageVersion ? TowerMechanicCores.Create(input, inventory) : null,
            input.Generation.PolicyVersion == TowerBossGeneration.CoverageVersion ? TowerPartyCoverage.Create(input, inventory) : null);
    }

    public BigInteger LegalOrderedCharacterCount => combinations[0, input.Budget.EssenceSlots]
        * Enumerable.Range(1, input.Budget.EssenceSlots).Aggregate(BigInteger.One, (n, k) => n * k);

    private static BigInteger Below(Random random, BigInteger maximum)
    {
        if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));
        if (maximum == 1) return 0;
        var bytes = (maximum - 1).ToByteArray(isUnsigned: true, isBigEndian: true);
        var mask = 1;
        while (mask < bytes[0]) mask = (mask << 1) | 1;
        for (var attempt = 0; attempt < 128; attempt++)
        {
            random.NextBytes(bytes); bytes[0] &= (byte)mask;
            var value = new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
            if (value < maximum) return value;
        }
        throw new InvalidDataException("Bounded random integer sampling failed.");
    }

    public IReadOnlyList<string> UniformCharacter(Random random)
    {
        var remaining = input.Budget.EssenceSlots;
        var selected = new List<string>();
        for (var g = 0; remaining > 0 && g < groups.Length; g++)
        {
            // Weight a family by its actual number of Essence variants and all legal suffix completions.
            // Uniform families would overrepresent single-variant families; greedy rejection of used families is also biased.
            var include = groups[g].Length * combinations[g + 1, remaining - 1];
            if (Below(random, combinations[g, remaining]) >= include) continue;
            selected.Add(groups[g][random.Next(groups[g].Length)]); remaining--;
        }
        var result = selected.ToArray(); random.Shuffle(result);
        return result;
    }

    public BossGeneratedChoice Fresh(Random random, bool constructive)
    {
        var intent = constructive ? intents[random.Next(intents.Length)] : "uniform";
        var builds = new Dictionary<int, IReadOnlyList<string>>();
        for (var slot = 1; slot <= input.RequiredPartySize; slot++)
        {
            var ids = constructive ? ConstructCharacter(random, builds, intent) : UniformCharacter(random);
            if (ids is null) return new(null, intent, null, "owned-or-family-dead-end");
            builds.Add(slot, ids);
        }
        return Choice(builds, intent, null);
    }

    public BossGeneratedChoice FreshCoordinated(Random random)
    {
        var intent = intents[random.Next(intents.Length)];
        var builds = new Dictionary<int, IReadOnlyList<string>>();
        var prototype = ConstructCharacter(random, builds, intent);
        if (prototype is null) return new(null, intent, null, "owned-or-family-dead-end");
        var shuffled = prototype.ToArray(); random.Shuffle(shuffled);
        var core = shuffled.Take(random.Next(1, input.Budget.EssenceSlots)).ToArray();
        var slots = Enumerable.Range(1, input.RequiredPartySize).ToArray(); random.Shuffle(slots);
        var sharedCount = input.RequiredPartySize == 1 ? 1 : random.Next(2, input.RequiredPartySize + 1);
        if (input.OwnedCopies is not null) sharedCount = Math.Min(sharedCount, core.Min(id => input.OwnedCopies.GetValueOrDefault(id)));
        for (var i = 0; i < slots.Length; i++)
        {
            var ids = ConstructCharacter(random, builds, intent, i < sharedCount ? core : null);
            if (ids is null) return new(null, intent, null, "owned-or-family-dead-end");
            builds.Add(slots[i], ids);
        }
        return Choice(builds, "shared-core:" + intent, null);
    }

    public BossGeneratedChoice BroadcastCore(Random random, PartyChoice parent)
    {
        if (Invalid(parent) is not null) throw new InvalidDataException("Broadcast requires a legal generated parent.");
        if (input.RequiredPartySize < 2) return new(null, "broadcast-core", null, "needs-two-characters");
        var builds = parent.Builds.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.ToArray());
        var slots = builds.Keys.Order().ToArray(); random.Shuffle(slots);
        var donor = builds[slots[0]].ToArray(); random.Shuffle(donor);
        var core = donor.Take(random.Next(1, input.Budget.EssenceSlots)).ToArray();
        var coreFamilies = core.Select(id => families[id]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var count = random.Next(1, input.RequiredPartySize);
        foreach (var slot in slots.Skip(1).Take(count))
            builds[slot] = core.Concat(builds[slot].Where(id => !coreFamilies.Contains(families[id])))
                .Take(input.Budget.EssenceSlots).ToArray();
        return Choice(builds, "broadcast-core", null);
    }

    private BossMechanicCore? PickCore(Random random)
    {
        var groups = (mechanics.Cores ?? []).Where(c => c.EssenceIds.All(id => input.OwnedCopies is null || input.OwnedCopies.GetValueOrDefault(id) > 0))
            .GroupBy(c => c.Kind).OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => g.ToArray()).ToArray();
        if (groups.Length == 0) return null;
        var group = groups[random.Next(groups.Length)]; return group[random.Next(group.Length)];
    }

    public BossGeneratedChoice FreshMechanics(Random random)
    {
        var primary = PickCore(random);
        if (primary is null) return FreshCoordinated(random) with { Intent = "no-compatible-core:fallback-coordinated" };
        var secondary = PickCore(random)!;
        var slots = Enumerable.Range(1, input.RequiredPartySize).ToArray(); random.Shuffle(slots);
        var count = input.RequiredPartySize == 1 ? 1 : random.Next(2, input.RequiredPartySize + 1);
        var builds = new Dictionary<int, IReadOnlyList<string>>();
        foreach (var (slot, index) in slots.Select((slot, index) => (slot, index)))
        {
            var core = index < count ? primary : secondary;
            var available = core.EssenceIds.All(id => input.OwnedCopies is null
                || builds.Values.Sum(ids => ids.Count(e => e == id)) < input.OwnedCopies.GetValueOrDefault(id));
            var ids = ConstructCharacter(random, builds, intents[random.Next(intents.Length)], available ? core.EssenceIds : null);
            if (ids is null) return new(null, "mechanic-cores", core.Id, "owned-or-family-dead-end");
            builds.Add(slot, ids);
        }
        return Choice(builds, "mechanic-cores", primary.Id + ";" + secondary.Id);
    }

    public BossGeneratedChoice ReplaceMechanicCore(Random random, PartyChoice parent)
    {
        if (Invalid(parent) is not null) throw new InvalidDataException("Core replacement requires a legal generated parent.");
        var core = PickCore(random);
        if (core is null) return new(null, "mechanic-core", null, "no-compatible-core");
        var builds = parent.Builds.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.ToArray());
        var slots = builds.Keys.Order().ToArray(); random.Shuffle(slots);
        var coreFamilies = core.EssenceIds.Select(id => families[id]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var slot in slots.Take(random.Next(1, input.RequiredPartySize + 1)))
            builds[slot] = core.EssenceIds.Concat(builds[slot].Where(id => !coreFamilies.Contains(families[id])))
                .Take(input.Budget.EssenceSlots).ToArray();
        return Choice(builds, "mechanic-core", core.Id);
    }

    private IReadOnlyList<string>? ConstructCharacter(Random random, IReadOnlyDictionary<int, IReadOnlyList<string>> other, string intent,
        IReadOnlyList<string>? prefix = null)
    {
        var used = other.Values.SelectMany(ids => ids).GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        var ids = new List<string>(); var selectedFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in prefix ?? [])
        {
            if (!selectedFamilies.Add(families[id]) || input.OwnedCopies is not null && used.GetValueOrDefault(id) >= input.OwnedCopies.GetValueOrDefault(id)) return null;
            ids.Add(id); used[id] = used.GetValueOrDefault(id) + 1;
        }
        while (ids.Count < input.Budget.EssenceSlots)
        {
            var eligible = pool.Where(id => !selectedFamilies.Contains(families[id])
                && (input.OwnedCopies is null || used.GetValueOrDefault(id) < input.OwnedCopies.GetValueOrDefault(id))).ToArray();
            if (eligible.Length == 0) return null;
            // Every character may be a hybrid; there are no fixed tank/healer/DPS cells.
            var goal = random.Next(4) == 0 ? intents[random.Next(intents.Length)] : intent;
            var weights = eligible.Select(id => 1 + (Capabilities(id).Contains(goal) ? 6 : 0)
                + (mechanics.CounterIntents.Any(Capabilities(id).Contains) ? 1 : 0)).ToArray();
            var ticket = random.Next(weights.Sum()); var index = 0;
            while (ticket >= weights[index]) ticket -= weights[index++];
            var chosen = eligible[index]; ids.Add(chosen); selectedFamilies.Add(families[chosen]);
            used[chosen] = used.GetValueOrDefault(chosen) + 1;
        }
        return ids;
    }

    private IReadOnlyList<string> Capabilities(string id) => capabilities[id];

    private static IReadOnlyList<string> CapabilitySignals(IReadOnlyList<string> signals)
    {
        var result = signals.Where(s => s.StartsWith("intent:", StringComparison.Ordinal)).Select(s => s[7..]).ToList();
        if (signals.Any(s => s is "condition:Poison" or "condition:Bleed" or "condition:Burn")) result.Add("pressure");
        return result;
    }

    public string CapabilityPattern(PartyChoice party)
    {
        if (!patterns.TryGetValue(party.Id, out var pattern))
            patterns.Add(party.Id, pattern = HarnessJson.Hash(party.Builds.OrderBy(p => p.Key).Select(p =>
                intents.Select(intent => p.Value.Count(id => Capabilities(id).Contains(intent))).ToArray()).ToArray()));
        return pattern;
    }

    public string? Invalid(PartyChoice party)
    {
        if (party is null || party.Builds is null || party.Id != HarnessJson.Hash(party.Builds)
            || !party.Builds.Keys.Order().SequenceEqual(Enumerable.Range(1, input.RequiredPartySize))) return "invalid-party-identity";
        if (party.Builds.Values.Any(ids => ids is null || ids.Count != input.Budget.EssenceSlots || ids.Any(id => id is null || !families.ContainsKey(id))))
            return "invalid-pool-or-count";
        if (party.Builds.Values.Any(ids => ids.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count)) return "duplicate-family";
        if (input.OwnedCopies is not null && party.Builds.Values.SelectMany(ids => ids).GroupBy(id => id)
            .Any(g => g.Count() > input.OwnedCopies.GetValueOrDefault(g.Key))) return "owned-copies-exceeded";
        return null;
    }

    private BossGeneratedChoice Choice(IReadOnlyDictionary<int, IReadOnlyList<string>> builds, string intent, string? interaction)
    {
        var party = TowerPartySelection.Choice("independent-generated", builds.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value));
        return new(party, intent, interaction, Invalid(party));
    }

    public BossGeneratedChoice PairReplacement(PartyChoice parent, TowerEnablerConsumerPair pair, int first, int second, int left, int right)
    {
        if (Invalid(parent) is not null || !mechanics.Interactions.Any(p => HarnessJson.Hash(p) == HarnessJson.Hash(pair)) || first < 1 || second < 1
            || first > input.RequiredPartySize || second > input.RequiredPartySize || left < 0 || right < 0
            || left >= input.Budget.EssenceSlots || right >= input.Budget.EssenceSlots || first == second && left == right)
            throw new InvalidDataException("Invalid structural interaction placement.");
        var key = $"{pair.EnablerEssenceId} + {pair.ConsumerEssenceId}; {pair.Mechanism}: {pair.ProducerNodeKey} -> {pair.ConsumerNodeKey}; {pair.Compatibility}";
        if (first != second && pair.Compatibility == "same-owner-or-explicit-recipient-required")
            return new(null, "interaction-hypothesis", key, "same-owner-required");
        var builds = parent.Builds.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.ToArray());
        var a = builds[first].ToArray(); a[left] = pair.EnablerEssenceId; builds[first] = a;
        var b = builds[second].ToArray(); b[right] = pair.ConsumerEssenceId; builds[second] = b;
        return Choice(builds, "interaction-hypothesis", key);
    }

    public BossGeneratedChoice Mutate(Random random, string operation, PartyChoice parent, PartyChoice? other = null)
    {
        if (Invalid(parent) is not null || other is not null && Invalid(other) is not null)
            throw new InvalidDataException("Mutation requires legal generated parents.");
        var builds = parent.Builds.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.ToArray());
        var first = random.Next(1, input.RequiredPartySize + 1);
        var left = random.Next(input.Budget.EssenceSlots);
        var right = (left + 1 + random.Next(input.Budget.EssenceSlots - 1)) % input.Budget.EssenceSlots;
        var second = input.RequiredPartySize == 1 ? first : (first - 1 + 1 + random.Next(input.RequiredPartySize - 1)) % input.RequiredPartySize + 1;
        if (operation is "double" or "cross-character" && random.Next(2) == 0)
        {
            var pairs = mechanics.Interactions.Where(p => operation == "double" || p.Compatibility != "same-owner-or-explicit-recipient-required").ToArray();
            if (pairs.Length > 0)
            {
                var pair = pairs[random.Next(pairs.Length)];
                var recipient = operation == "double" ? first : second;
                var existingEnablerFamily = Array.FindIndex(parent.Builds[first].ToArray(), id => StringComparer.OrdinalIgnoreCase.Equals(families[id], families[pair.EnablerEssenceId]));
                var existingConsumerFamily = Array.FindIndex(parent.Builds[recipient].ToArray(), id => StringComparer.OrdinalIgnoreCase.Equals(families[id], families[pair.ConsumerEssenceId]));
                left = existingEnablerFamily >= 0 ? existingEnablerFamily : left;
                right = existingConsumerFamily >= 0 ? existingConsumerFamily : right;
                if (first == recipient && left == right) right = (left + 1) % input.Budget.EssenceSlots;
                return PairReplacement(parent, pair, first, recipient, left, right);
            }
        }
        var ids = builds[first].ToArray();
        switch (operation)
        {
            case "single": ids[left] = pool[random.Next(pool.Length)]; builds[first] = ids; break;
            case "double": ids[left] = pool[random.Next(pool.Length)]; ids[right] = pool[random.Next(pool.Length)]; builds[first] = ids; break;
            case "order": (ids[left], ids[right]) = (ids[right], ids[left]); builds[first] = ids; break;
            case "cross-character":
                if (first == second) return new(null, "joint", null, "needs-two-characters");
                ids[left] = pool[random.Next(pool.Length)]; builds[first] = ids;
                var partner = builds[second].ToArray(); partner[right] = pool[random.Next(pool.Length)]; builds[second] = partner; break;
            case "whole-character":
                builds.Remove(first);
                var replacement = ConstructCharacter(random, builds, intents[random.Next(intents.Length)]);
                if (replacement is null) return new(null, "whole-character", null, "owned-or-family-dead-end");
                builds[first] = replacement; break;
            case "recombine":
                if (other is null || other.Id == parent.Id) throw new InvalidDataException("Recombination requires two distinct generated parents.");
                foreach (var slot in builds.Keys.ToArray()) if (random.Next(2) == 0) builds[slot] = other.Builds[slot].ToArray();
                break;
            default: throw new InvalidDataException("Unknown complete-party mutation.");
        }
        return Choice(builds, operation, null);
    }
}
