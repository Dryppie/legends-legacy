using System.Text.Json;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;

namespace BalanceHarness;

public sealed record TowerDamageSourceAffinity(string Id, string ProducerEssenceId, string ModifierEssenceId,
    string DamageType, string SourceScope, IReadOnlyList<string> ProducerRoute, IReadOnlyList<string> ModifierRoute);
public sealed record TowerExcludedDamageRoute(string EssenceId, IReadOnlyList<string> Route, string Reason);
public sealed record TowerDamageSourceAffinityReport(string Version, string InventoryHash,
    IReadOnlyDictionary<string, string> SourceHashes, IReadOnlyList<TowerDamageSourceAffinity> Affinities,
    IReadOnlyList<TowerExcludedDamageRoute> ExcludedRoutes, IReadOnlyList<string> Limitations);

/// <summary>Separate, conservative authored hypotheses. Never rewrites the v1 interaction graph.
/// Version 1 covers only the source-owned Poison routes reviewed against the captured engine.</summary>
public static class TowerDamageSourceAffinities
{
    public const string Version = "tower-damage-source-affinity-v1";
    private sealed record Route(TowerEssenceMechanics Essence, string[] Keys);

    public static TowerDamageSourceAffinityReport Create(TowerBossInventoryReport inventory)
    {
        if (inventory is null || inventory.SchemaVersion != 1 || inventory.Nodes is null || inventory.Essences is null
            || inventory.SourceHashes is null || inventory.SourceHashes.Values.Any(h => !TowerContractJson.Hash(h))
            || !inventory.SourceHashes.Keys.Order(StringComparer.Ordinal).SequenceEqual(TowerBossInventory.SourceFiles.Order(StringComparer.Ordinal))
            || inventory.Nodes.Select(n => n.Key).Distinct(StringComparer.Ordinal).Count() != inventory.Nodes.Count
            || inventory.Essences.Select(e => e.Id).Distinct(StringComparer.Ordinal).Count() != inventory.Essences.Count)
            throw new InvalidDataException("Invalid source-bound affinity inventory.");
        var nodes = inventory.Nodes.ToDictionary(n => n.Key, StringComparer.Ordinal);
        var producers = new List<Route>(); var modifiers = new List<Route>();
        var excluded = new List<TowerExcludedDamageRoute>();
        TowerMechanicNode Node(string key, TowerMechanicNodeKind kind)
        {
            if (!nodes.TryGetValue(key, out var node) || node.Kind != kind || node.Unknowns.Count != 0)
                throw new InvalidDataException("Missing, ambiguous or unresolved affinity node: " + key);
            return node;
        }
        T Definition<T>(string key, TowerMechanicNodeKind kind) => Node(key, kind).Definition.Deserialize<T>(HarnessJson.Options)
            ?? throw new InvalidDataException("Missing affinity definition: " + key);
        void Exclude(TowerEssenceMechanics essence, string[] keys, string reason) => excluded.Add(new(essence.Id, keys, reason));
        void CheckEffectNode(string ownerKey, AbilityEffectSpec effect)
        {
            var node = Node("Effect:" + ownerKey + "/" + effect.Id, TowerMechanicNodeKind.Effect);
            if (HarnessJson.Hash(node.Definition) != HarnessJson.Hash(effect))
                throw new InvalidDataException("Affinity effect differs from its owner definition.");
        }
        // Only explicit OnBasicAttack paths are covered. Every selected trigger is retained;
        // guards remain conditional evidence, never interpreted as guaranteed activation.
        IEnumerable<string[]> TriggerRoutes(TowerEssenceMechanics essence, string ownerKey, string[] prefix,
            AbilityEffectSpec effect, IReadOnlyList<AbilityTriggerSpec> triggers)
        {
            for (var i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                if (trigger.EffectIds.Count != 0 && !trigger.EffectIds.Contains(effect.Id)) continue;
                var key = "Trigger:" + ownerKey + "/" + i;
                if (HarnessJson.Hash(Node(key, TowerMechanicNodeKind.Trigger).Definition) != HarnessJson.Hash(trigger))
                    throw new InvalidDataException("Affinity trigger differs from its owner definition.");
                var route = prefix.Append(key).Append("Effect:" + ownerKey + "/" + effect.Id).ToArray();
                if (trigger.Event != AbilityTriggerEvent.OnBasicAttack)
                    Exclude(essence, route, "trigger-source-credit-unreviewed");
                else yield return route;
            }
        }
        void Poison(TowerEssenceMechanics essence, AbilityEffectSpec effect, string[] route, bool triggered)
        {
            if (effect.Operation != AbilityEffectOperation.ApplyCondition || effect.Condition != StandardConditionType.Poison) return;
            if (effect.Target != (triggered ? AbilityTargetSelector.EventTarget : AbilityTargetSelector.CurrentTarget))
                Exclude(essence, route, "poison-recipient-unreviewed");
            else producers.Add(new(essence, route));
        }
        foreach (var essence in inventory.Essences.OrderBy(e => e.Id, StringComparer.Ordinal))
        foreach (var abilityId in essence.AbilityIds.Order(StringComparer.Ordinal))
        {
            var abilityKey = "Ability:" + abilityId;
            var ability = Definition<AbilitySpec>(abilityKey, TowerMechanicNodeKind.Ability);
            // Variant essences can share a base family's passive. AbilityIds is the
            // equipped-root declaration; OwningEssenceId is not the combatant source.
            if (ability.Id != abilityId)
                throw new InvalidDataException("Affinity root identity changed.");
            if (ability.ConversionFlags.AllowSummonProxy)
            { Exclude(essence, [abilityKey], "summon-proxy-source-credit-unreviewed"); continue; }
            foreach (var effect in ability.Effects)
            {
                CheckEffectNode(abilityKey, effect);
                var route = new[] { abilityKey, "Effect:" + abilityKey + "/" + effect.Id };
                if (effect.Operation == AbilityEffectOperation.Summon)
                { Exclude(essence, route, "summon-source-credit-unreviewed"); continue; }
                if (effect.Operation == AbilityEffectOperation.ModifyDamageDealt && effect.DamageType == DamageType.Poison)
                {
                    if (ability.Kind == AbilitySpecKind.Passive && ability.Triggers.Count == 0
                        && effect.Target == AbilityTargetSelector.Self && effect.Conditions.Count == 0
                        && effect.BaseValue > 0 && effect.ChancePercent == 100 && effect.DurationTicks == 0
                        && effect.ScalingAttribute is null && effect.ScalingCondition is null && effect.ScalingStatusId is null
                        && effect.ScalingOwnedSummonId is null && effect.EventMagnitudeCoefficient == 0)
                        modifiers.Add(new(essence, route));
                    else Exclude(essence, route, "modifier-recipient-or-activation-unreviewed");
                }
                if (ability.Kind == AbilitySpecKind.Active && ability.Triggers.Count == 0)
                    Poison(essence, effect, route, false);
                else if (ability.Kind == AbilitySpecKind.Passive && effect.Operation == AbilityEffectOperation.ApplyCondition
                    && effect.Condition == StandardConditionType.Poison)
                    foreach (var triggered in TriggerRoutes(essence, abilityKey, [abilityKey], effect, ability.Triggers))
                        Poison(essence, effect, triggered, true);
                // One self-applied status layer keeps status.Source and status.Owner on
                // the essence holder. Other recipients and summon paths are not followed.
                if (effect.Operation != AbilityEffectOperation.ApplyStatus || effect.StatusId is null) continue;
                if (ability.Kind != AbilitySpecKind.Active || ability.Triggers.Count != 0 || effect.Target != AbilityTargetSelector.Self)
                { Exclude(essence, route, "status-recipient-or-activation-unreviewed"); continue; }
                var statusKey = "Status:" + effect.StatusId;
                var status = Definition<StatusSpec>(statusKey, TowerMechanicNodeKind.Status);
                if (status.Id != effect.StatusId) throw new InvalidDataException("Changed status identity.");
                foreach (var statusEffect in status.Effects)
                {
                    CheckEffectNode(statusKey, statusEffect);
                    if (statusEffect.Operation is AbilityEffectOperation.Summon or AbilityEffectOperation.ApplyStatus)
                        Exclude(essence, [.. route, statusKey, "Effect:" + statusKey + "/" + statusEffect.Id], "nested-status-or-summon-source-credit-unreviewed");
                    if (statusEffect.Operation != AbilityEffectOperation.ApplyCondition || statusEffect.Condition != StandardConditionType.Poison) continue;
                    foreach (var triggered in TriggerRoutes(essence, statusKey, [.. route, statusKey], statusEffect, status.Triggers))
                        Poison(essence, statusEffect, triggered, true);
                }
            }
        }
        var affinities = (from producer in producers from modifier in modifiers
            where producer.Essence.Id != modifier.Essence.Id
                && !producer.Essence.SourceMonsterId.Equals(modifier.Essence.SourceMonsterId, StringComparison.OrdinalIgnoreCase)
            select new TowerDamageSourceAffinity(HarnessJson.Hash(new { Version, Producer = producer.Essence.Id,
                    Modifier = modifier.Essence.Id, ProducerRoute = producer.Keys, ModifierRoute = modifier.Keys,
                    DamageType = "Poison", SourceScope = "same-poison-source-owner" }),
                producer.Essence.Id, modifier.Essence.Id, "Poison", "same-poison-source-owner", producer.Keys, modifier.Keys))
            .DistinctBy(a => a.Id).OrderBy(a => a.Id, StringComparer.Ordinal).ToArray();
        return new(Version, HarnessJson.Hash(inventory), inventory.SourceHashes, affinities,
            excluded.DistinctBy(HarnessJson.Hash).OrderBy(HarnessJson.Hash, StringComparer.Ordinal).ToArray(),
            ["Authored source compatibility only; no efficacy, uptime, successful application or prepared damage-type guarantee.",
             "Version 1 covers Poison applied by direct active effects, owner Basic Attack passives, or one self-applied status layer, and unconditional self Poison modifiers.",
             "Summon/proxy, other trigger events, other status recipients, nested statuses and other damage types are outside this version.",
             "Source hashes bind declared content; native admission must independently authenticate content and executable before combat."]);
    }
}
