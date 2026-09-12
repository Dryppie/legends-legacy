using System.Text.Json;
using Domain.Models.Combat.Abilities;

namespace BalanceHarness;

public sealed record BossMechanicCore(string Id, string Kind, IReadOnlyList<string> EssenceIds,
    IReadOnlyList<string> EvidenceKeys, string Limitation);

/// <summary>Conservative same-owner proposals from authored mechanics, not a ranking or proof of synergy.</summary>
public static class TowerMechanicCores
{
    public static IReadOnlyList<BossMechanicCore> Create(BossDiscoveryInputs input, TowerBossInventoryReport inventory)
    {
        var allowed = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Family, StringComparer.Ordinal);
        var essences = inventory.Essences.Where(e => allowed.ContainsKey(e.Id)).ToArray();
        var nodes = inventory.Nodes.ToDictionary(n => n.Key, StringComparer.Ordinal);
        var edges = inventory.References.Where(r => r.Activates).ToLookup(r => r.SourceKey, StringComparer.Ordinal);
        HashSet<string> Active(TowerEssenceMechanics essence)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<string>(essence.AbilityIds.Select(id => "Ability:" + id));
            while (pending.TryPop(out var key))
                if (seen.Add(key)) foreach (var edge in edges[key]) pending.Push(edge.TargetKey);
            return seen;
        }
        var active = essences.ToDictionary(e => e.Id, Active, StringComparer.Ordinal);
        var effects = nodes.Values.Where(n => n.Kind == TowerMechanicNodeKind.Effect)
            .ToDictionary(n => n.Key, n => n.Definition.Deserialize<AbilityEffectSpec>(HarnessJson.Options)!, StringComparer.Ordinal);
        var triggers = nodes.Values.Where(n => n.Kind == TowerMechanicNodeKind.Trigger)
            .ToDictionary(n => n.Key, n => n.Definition.Deserialize<AbilityTriggerSpec>(HarnessJson.Options)!, StringComparer.Ordinal);
        var cores = new List<BossMechanicCore>();
        void Add(string kind, IEnumerable<string> ids, IEnumerable<string> evidence)
        {
            var recipe = ids.Distinct(StringComparer.Ordinal).ToArray();
            if (recipe.Length is < 2 or > 3 || recipe.Length > input.Budget.EssenceSlots
                || recipe.Any(id => !allowed.ContainsKey(id))
                || recipe.Select(id => allowed[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != recipe.Length) return;
            var keys = evidence.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            cores.Add(new(HarnessJson.Hash(new { kind, recipe, keys }), kind, recipe, keys,
                "Same-owner structural hypothesis. Actual target, event timing, chance, conditions, cooldowns, caps and action blocking still require combat validation."));
        }

        foreach (var pair in inventory.EnablerConsumerPairs)
        {
            if (!allowed.ContainsKey(pair.EnablerEssenceId) || !allowed.ContainsKey(pair.ConsumerEssenceId)
                || !pair.Mechanism.StartsWith("StandardCondition:", StringComparison.Ordinal)
                || !effects.TryGetValue(pair.ProducerNodeKey, out var producer) || !effects.TryGetValue(pair.ConsumerNodeKey, out var consumer)
                || !active[pair.EnablerEssenceId].Contains(pair.ProducerNodeKey) || !active[pair.ConsumerEssenceId].Contains(pair.ConsumerNodeKey)
                || producer.Operation != AbilityEffectOperation.ApplyCondition || producer.Conditions.Count != 0
                || producer.Target is not (AbilityTargetSelector.CurrentTarget or AbilityTargetSelector.RandomEnemy or AbilityTargetSelector.AllEnemies)
                || producer.Condition is null) continue;
            // Negative predicates and self-only condition reads are not enabled by applying a condition to an enemy.
            var positive = consumer.Conditions.Any(c => c.Condition == producer.Condition
                && (c.Type == AbilityConditionType.AnyEnemyHasCondition || c.Type == AbilityConditionType.HasCondition
                    && (c.Subject == AbilityConditionSubject.EventTarget || c.Subject == AbilityConditionSubject.Target
                        && consumer.Target is AbilityTargetSelector.CurrentTarget or AbilityTargetSelector.RandomEnemy or AbilityTargetSelector.AllEnemies)));
            if (positive) Add("condition", [pair.EnablerEssenceId, pair.ConsumerEssenceId], [pair.ProducerNodeKey, pair.ConsumerNodeKey]);
        }

        foreach (var producer in effects.Where(p => p.Value.Operation == AbilityEffectOperation.PerformBasicAttack
                     && p.Value.Target is AbilityTargetSelector.Self or AbilityTargetSelector.NonSummonedAllies or AbilityTargetSelector.AllAllies))
        foreach (var listener in triggers.Where(p => p.Value.Event == AbilityTriggerEvent.OnBasicAttack
                     && p.Value.Conditions.All(c => c.Type == AbilityConditionType.EventSourceIsSelf)))
        foreach (var enabler in essences.Where(e => active[e.Id].Contains(producer.Key)))
        foreach (var consumer in essences.Where(e => e.Id != enabler.Id && active[e.Id].Contains(listener.Key)))
        {
            // A status listener belongs to its recipient. Only include a consumer that applies this status to itself.
            var owner = inventory.References.Single(r => r.TargetKey == listener.Key && r.Relation == TowerMechanicRelation.ContainsTrigger).SourceKey;
            if (nodes[owner].Kind == TowerMechanicNodeKind.Summon) continue;
            if (nodes[owner].Kind == TowerMechanicNodeKind.Status && !effects.Any(e => active[consumer.Id].Contains(e.Key)
                    && e.Value.Operation == AbilityEffectOperation.ApplyStatus && "Status:" + e.Value.StatusId == owner
                    && e.Value.Target == AbilityTargetSelector.Self)) continue;
            if (nodes[owner].Kind is not (TowerMechanicNodeKind.Ability or TowerMechanicNodeKind.Status)) continue;
            // Runtime PerformBasicAttack(target) emits OnBasicAttack with the recipient as source; these are co-located.
            Add("basic-attack", [enabler.Id, consumer.Id], [producer.Key, listener.Key]);
        }
        var pairs = cores.ToArray();
        foreach (var condition in pairs.Where(c => c.Kind == "condition"))
        foreach (var attack in pairs.Where(c => c.Kind == "basic-attack" && c.EssenceIds.Intersect(condition.EssenceIds).Any()))
            Add("chain", condition.EssenceIds.Concat(attack.EssenceIds), condition.EvidenceKeys.Concat(attack.EvidenceKeys));
        // Uniform sampling later is over recipes per kind, not inflated by duplicate dependency paths.
        return cores.OrderBy(c => c.Id, StringComparer.Ordinal)
            .DistinctBy(c => c.Kind + ":" + string.Join("|", c.EssenceIds.Order(StringComparer.Ordinal)))
            .OrderBy(c => c.Kind, StringComparer.Ordinal).ThenBy(c => c.Id, StringComparer.Ordinal).ToArray();
    }
}
