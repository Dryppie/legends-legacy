using System.Text.Json;
using Domain.Models.Combat.Abilities;

namespace BalanceHarness;

public sealed record BossCoverageFeature(string EssenceId, string Kind, IReadOnlyList<string> EvidenceKeys);

/// <summary>Direct authored effects supply proposal categories, not estimated uptime or combat strength.</summary>
public static class TowerPartyCoverage
{
    public static readonly string[] Kinds = ["recurring-control", "enemy-pressure", "attack-enabler", "protection", "recovery"];

    public static IReadOnlyList<BossCoverageFeature> Create(BossDiscoveryInputs input, TowerBossInventoryReport inventory)
    {
        var allowed = input.AllowedEssences.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        var nodes = inventory.Nodes.ToDictionary(n => n.Key, StringComparer.Ordinal);
        var features = new List<BossCoverageFeature>();
        foreach (var essence in inventory.Essences.Where(e => allowed.Contains(e.Id)))
        {
            var evidence = Kinds.ToDictionary(k => k, _ => new List<string>());
            foreach (var id in essence.AbilityIds)
            {
                var key = "Ability:" + id;
                var ability = nodes[key].Definition.Deserialize<AbilitySpec>(HarnessJson.Options)!;
                foreach (var effect in ability.Effects)
                {
                    if (effect.ChancePercent <= 0) continue;
                    var enemy = effect.Target is AbilityTargetSelector.CurrentTarget or AbilityTargetSelector.RandomEnemy
                        or AbilityTargetSelector.AllEnemies or AbilityTargetSelector.TwoEnemies or AbilityTargetSelector.TwoRandomEnemies
                        or AbilityTargetSelector.ThreeEnemies or AbilityTargetSelector.ThreeRandomEnemies or AbilityTargetSelector.LowestHealthEnemy
                        or AbilityTargetSelector.HighestHealthEnemy or AbilityTargetSelector.LowestCurrentHealthEnemy
                        or AbilityTargetSelector.HighestMaxHealthEnemy or AbilityTargetSelector.HighestConditionStacksEnemy;
                    var ally = effect.Target is AbilityTargetSelector.Self or AbilityTargetSelector.AllAllies or AbilityTargetSelector.NonSummonedAllies
                        or AbilityTargetSelector.LowestHealthAlly or AbilityTargetSelector.TwoAllies or AbilityTargetSelector.HighestMaxHealthAlly
                        or AbilityTargetSelector.RandomAlly;
                    void Add(string kind) => evidence[kind].Add("Effect:" + key + "/" + effect.Id);
                    var triggers = ability.Triggers.Select((t, i) => (Trigger: t, Index: i)).Where(t => t.Trigger.EffectIds.Count == 0 || t.Trigger.EffectIds.Contains(effect.Id))
                        .Where(t => t.Trigger.Event is not (AbilityTriggerEvent.OnCombatStart or AbilityTriggerEvent.OnDeath or AbilityTriggerEvent.OnEnemyDeath or AbilityTriggerEvent.OnKill)).ToArray();
                    if (enemy && effect.Operation == AbilityEffectOperation.ApplyCondition
                        && effect.Condition is StandardConditionType.Stun or StandardConditionType.Freeze
                        && (ability.Kind == AbilitySpecKind.Active || triggers.Length > 0))
                    {
                        Add("recurring-control"); evidence["recurring-control"].Add(key);
                        evidence["recurring-control"].AddRange(triggers.Select(t => "Trigger:" + key + "/" + t.Index));
                    }
                    if (enemy && effect.Operation == AbilityEffectOperation.ApplyCondition
                        && effect.Condition is StandardConditionType.Poison or StandardConditionType.Bleed or StandardConditionType.Burn
                            or StandardConditionType.Corrosion or StandardConditionType.Weaken or StandardConditionType.Slow
                            or StandardConditionType.Vulnerable or StandardConditionType.Wound or StandardConditionType.Decay)
                        Add("enemy-pressure");
                    if (ally && (effect.Operation == AbilityEffectOperation.PerformBasicAttack
                        || effect.Operation == AbilityEffectOperation.ApplyCondition && effect.Condition is StandardConditionType.Haste or StandardConditionType.Empower))
                        Add("attack-enabler");
                    if (ally && (effect.Operation is AbilityEffectOperation.GrantBarrier or AbilityEffectOperation.GrantCover
                        || effect.Operation == AbilityEffectOperation.ModifyDamageTaken && effect.BaseValue < 0
                        || effect.Operation == AbilityEffectOperation.ApplyCondition && effect.Condition is StandardConditionType.Guard or StandardConditionType.Ward or StandardConditionType.Cover))
                        Add("protection");
                    if (ally && (effect.Operation == AbilityEffectOperation.Heal
                        || effect.Operation == AbilityEffectOperation.ModifyRegenerationRate && effect.BaseValue > 0
                        || effect.Operation == AbilityEffectOperation.ApplyCondition && effect.Condition is StandardConditionType.Recovery or StandardConditionType.Renewal))
                        Add("recovery");
                }
            }
            foreach (var (kind, keys) in evidence.Where(p => p.Value.Count > 0))
                features.Add(new(essence.Id, kind, keys.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()));
        }
        return features.OrderBy(f => f.Kind, StringComparer.Ordinal).ThenBy(f => f.EssenceId, StringComparer.Ordinal).ToArray();
    }
}

public sealed partial class TowerBossPartyGenerator
{
    private void ValidateCoverage()
    {
        if (input.Generation.PolicyVersion == TowerBossGeneration.CoverageVersion && mechanics.Coverage is null
            || mechanics.Coverage is not null && (mechanics.Coverage.Any(f => f is null || !families.ContainsKey(f.EssenceId)
                || !TowerPartyCoverage.Kinds.Contains(f.Kind) || f.EvidenceKeys is not { Count: > 0 } || f.EvidenceKeys.Any(string.IsNullOrWhiteSpace))
                || mechanics.Coverage.Select(f => (f.EssenceId, f.Kind)).Distinct().Count() != mechanics.Coverage.Count))
            throw new InvalidDataException("Invalid content-derived party coverage features.");
    }

    private IGrouping<string, BossCoverageFeature>[] CoverageGroups() => (mechanics.Coverage ?? [])
        .Where(f => input.OwnedCopies is null || input.OwnedCopies.GetValueOrDefault(f.EssenceId) > 0)
        .GroupBy(f => f.Kind).OrderBy(g => g.Key, StringComparer.Ordinal).ToArray();

    public BossGeneratedChoice FreshCoverage(Random random)
    {
        // This arm explicitly retains a uniform route to every legal ordered team, including uncategorized effects.
        if (random.Next(8) == 0) return Fresh(random, false) with { Intent = "coverage:uniform" };
        var groups = CoverageGroups();
        if (groups.Length == 0) return Fresh(random, false) with { Intent = "coverage:no-features-uniform" };
        random.Shuffle(groups);
        var planned = Enumerable.Range(1, input.RequiredPartySize).ToDictionary(s => s, _ => new List<string>());
        var used = new Dictionary<string, int>(); var trace = new List<string>();
        bool Add(int slot, string id)
        {
            var ids = planned[slot];
            if (ids.Contains(id)) return true;
            if (ids.Count >= input.Budget.EssenceSlots || ids.Any(e => StringComparer.OrdinalIgnoreCase.Equals(families[e], families[id]))
                || input.OwnedCopies is not null && used.GetValueOrDefault(id) >= input.OwnedCopies.GetValueOrDefault(id)) return false;
            ids.Add(id); used[id] = used.GetValueOrDefault(id) + 1; return true;
        }
        foreach (var group in groups)
        {
            var choices = group.OrderBy(f => f.EssenceId, StringComparer.Ordinal).ToArray();
            var provider = choices[random.Next(choices.Length)].EssenceId;
            var count = random.Next(input.RequiredPartySize + 1);
            var slots = planned.Keys.ToArray(); random.Shuffle(slots);
            var placed = 0;
            foreach (var slot in slots) { if (placed == count) break; if (Add(slot, provider)) placed++; }
            trace.Add(group.Key + ":" + count + "/" + placed);
        }
        // Reserve coverage before trying compatible cores. Existing assignments and ownership constrain completion.
        var order = planned.Keys.ToArray(); random.Shuffle(order);
        foreach (var slot in order.Where(_ => random.Next(2) == 0))
        {
            var eligible = (mechanics.Cores ?? []).Where(c => {
                var extra = c.EssenceIds.Where(id => !planned[slot].Contains(id)).ToArray();
                return planned[slot].Count + extra.Length <= input.Budget.EssenceSlots
                    && extra.All(id => !planned[slot].Any(e => StringComparer.OrdinalIgnoreCase.Equals(families[e], families[id]))
                        && (input.OwnedCopies is null || used.GetValueOrDefault(id) < input.OwnedCopies.GetValueOrDefault(id)));
            }).ToArray();
            if (eligible.Length > 0) foreach (var id in eligible[random.Next(eligible.Length)].EssenceIds) Add(slot, id);
        }
        var builds = planned.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.ToArray());
        foreach (var slot in order)
        {
            var prefix = builds[slot]; builds.Remove(slot);
            var ids = ConstructCharacter(random, builds, intents[random.Next(intents.Length)], prefix);
            if (ids is null) return new(null, "coverage:" + string.Join(";", trace), null, "owned-or-family-dead-end");
            var shuffled = ids.ToArray(); random.Shuffle(shuffled); builds.Add(slot, shuffled);
        }
        return Choice(builds, "coverage:" + string.Join(";", trace), null);
    }

    public BossGeneratedChoice ChangeCoverage(Random random, PartyChoice parent)
    {
        if (Invalid(parent) is not null) throw new InvalidDataException("Coverage changes require a legal generated parent.");
        var groups = CoverageGroups();
        if (groups.Length == 0) return new(null, "coverage-count", null, "no-coverage-features");
        var group = groups[random.Next(groups.Length)].ToArray(); var selected = group[random.Next(group.Length)];
        var builds = parent.Builds.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.ToArray());
        var slots = builds.Keys.Order().ToArray(); random.Shuffle(slots);
        var count = random.Next(1, input.RequiredPartySize + 1);
        foreach (var slot in slots.Take(count))
        {
            var ids = builds[slot].ToArray();
            var index = Array.FindIndex(ids, id => StringComparer.OrdinalIgnoreCase.Equals(families[id], families[selected.EssenceId]));
            ids[index >= 0 ? index : random.Next(ids.Length)] = selected.EssenceId; builds[slot] = ids;
        }
        return Choice(builds, "coverage-count:" + selected.Kind + ":" + count, null);
    }

    public BossGeneratedChoice ChangePlacement(Random random, PartyChoice parent)
    {
        if (Invalid(parent) is not null) throw new InvalidDataException("Placement requires a legal generated parent.");
        if (input.RequiredPartySize < 2) return new(null, "placement", null, "needs-two-characters");
        var guided = random.Next(2) == 0; BossGeneratedChoice? best = null; var bestGain = int.MinValue;
        int Cores(IReadOnlyList<string> ids) => (mechanics.Cores ?? []).Count(c => c.EssenceIds.All(ids.Contains));
        for (var attempt = 0; attempt < 32; attempt++)
        {
            var first = random.Next(1, input.RequiredPartySize + 1);
            var second = (first + random.Next(input.RequiredPartySize - 1)) % input.RequiredPartySize + 1;
            var left = parent.Builds[first].ToArray(); var right = parent.Builds[second].ToArray();
            var x = random.Next(left.Length); var y = random.Next(right.Length);
            if (left[x] == right[y]) continue;
            (left[x], right[y]) = (right[y], left[x]);
            if (left.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != left.Length
                || right.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != right.Length) continue;
            var builds = parent.Builds.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.ToArray());
            builds[first] = left; builds[second] = right;
            var result = Choice(builds, guided ? "placement:core-completion-hypothesis" : "placement:uniform-legal-swap", null);
            if (!guided) return result;
            var gain = Cores(left) + Cores(right) - Cores(parent.Builds[first]) - Cores(parent.Builds[second]);
            if (gain > bestGain) { best = result; bestGain = gain; }
        }
        return best ?? new(null, "placement", null, "no-legal-swap-in-bounded-sample");
    }
}
