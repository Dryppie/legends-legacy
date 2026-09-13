using System.Text.Json;
using Domain.Models.Damages;
using Domain.Models.Combat.Abilities;

namespace BalanceHarness;

public sealed record BossIncomingThreat(bool Complete, IReadOnlyList<DamageType> DamageTypes,
    IReadOnlyList<string> EvidenceKeys, IReadOnlyList<string> Limitations);
public sealed record BossProtectionCompatibility(BossIncomingThreat Threat,
    IReadOnlyList<BossCoverageFeature> Coverage, IReadOnlyList<BossCoverageFeature> Excluded);

/// <summary>Conservative authored-type applicability; never estimates efficacy or learns from outcomes.</summary>
public static class TowerProtectionCompatibility
{
    public static BossProtectionCompatibility Create(BossDiscoveryInputs input, TowerBossInventoryReport inventory)
    {
        var threat = IncomingThreat(inventory, input.Floor);
        var baseline = TowerAttributeDefense.Create(input, inventory);
        var nodes = inventory.Nodes.ToDictionary(n => n.Key, StringComparer.OrdinalIgnoreCase);
        var excluded = baseline.Where(f => f.Kind == "protection" && Exclude(f, threat, nodes)).ToArray();
        return new(threat, baseline.Except(excluded).ToArray(), excluded);
    }

    public static bool Exclude(BossCoverageFeature feature, BossIncomingThreat threat,
        IReadOnlyDictionary<string, TowerMechanicNode> nodes)
    {
        if (feature.Kind != "protection" || !threat.Complete || threat.DamageTypes.Count == 0) return false;
        var keys = feature.EvidenceKeys.Where(k => k.StartsWith("Effect:", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (keys.Length == 0) return false;
        foreach (var key in keys)
        {
            if (!nodes.TryGetValue(key, out var node) || node.Kind != TowerMechanicNodeKind.Effect || node.Unknowns.Count > 0) return false;
            var effect = node.Definition.Deserialize<AbilityEffectSpec>(HarnessJson.Options)!;
            // A mixed, generic or uncertain route keeps the complete provider eligible.
            if (effect.Operation != AbilityEffectOperation.ModifyDamageTaken || effect.BaseValue >= 0
                || effect.ScalingAttribute is not null || effect.ScalingCoefficient != 0 || effect.MaximumScalingCoefficient != 0
                || effect.EventMagnitudeCoefficient != 0 || effect.ScalingCondition is not null || effect.ConditionScalingCoefficient != 0
                || effect.ScalingStatusId is not null || effect.StatusScalingCoefficient != 0 || effect.ScalingOwnedSummonId is not null
                || effect.OwnedSummonScalingCoefficient != 0 || effect.LivingNonSummonedAllyDamagePercent != 0
                || effect.InheritEventDamageType || effect.DamageType == DamageType.None || !Enum.IsDefined(effect.DamageType)
                || threat.DamageTypes.Contains(effect.DamageType)) return false;
        }
        return true;
    }

    public static BossIncomingThreat IncomingThreat(TowerBossInventoryReport inventory, int floor)
    {
        var boss = inventory.Bosses.Single(b => b.FloorNumber == floor);
        var nodes = inventory.Nodes.ToDictionary(n => n.Key, StringComparer.OrdinalIgnoreCase);
        var outgoing = inventory.References.ToLookup(r => r.SourceKey, StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<string>(boss.AbilityIds.Select(id => "Ability:" + id));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Tower guardians are prepared without equipment. Executor fallback and attacking summons use Physical.
        var types = new HashSet<DamageType> { DamageType.Physical };
        var evidence = new HashSet<string>(StringComparer.Ordinal) { "runtime:tower-unarmed-basic-attack" };
        var limitations = new HashSet<string>(StringComparer.Ordinal);
        if (boss.AbilityIds.Count == 0) limitations.Add("Missing native ability roots.");
        void Unresolved(string key) => limitations.Add("Unresolved damage semantics: " + key);
        while (pending.TryPop(out var key))
        {
            if (!seen.Add(key)) continue;
            if (!nodes.TryGetValue(key, out var node)) { Unresolved(key); continue; }
            // Event/condition identity nodes carry generic timing caveats, not executable effects.
            // Applied conditions are resolved below; all owner effects are visited regardless of triggers.
            if (!Enum.IsDefined(node.Kind) || node.Unknowns.Count > 0
                && node.Kind is not (TowerMechanicNodeKind.EventIdentity or TowerMechanicNodeKind.StandardCondition)) Unresolved(key);
            // Include reads as well as activation edges: an overapproximation cannot assert an absent type.
            foreach (var reference in outgoing[key]) pending.Push(reference.TargetKey);
            if (node.Kind == TowerMechanicNodeKind.Summon)
            {
                var summon = node.Definition.Deserialize<SummonSpec>(HarnessJson.Options)!;
                if (summon.CanBasicAttack) { types.Add(DamageType.Physical); evidence.Add(key); }
                foreach (var id in summon.AbilityIds) pending.Push("Ability:" + id);
            }
            if (node.Kind is TowerMechanicNodeKind.Ability or TowerMechanicNodeKind.Status)
            {
                var effects = node.Kind == TowerMechanicNodeKind.Ability
                    ? node.Definition.Deserialize<AbilitySpec>(HarnessJson.Options)!.Effects
                    : node.Definition.Deserialize<StatusSpec>(HarnessJson.Options)!.Effects;
                // Require complete typed nodes even if a caller supplies a truncated reference graph.
                foreach (var effect in effects) pending.Push("Effect:" + key + "/" + effect.Id);
            }
            if (node.Kind != TowerMechanicNodeKind.Effect) continue;
            var e = node.Definition.Deserialize<AbilityEffectSpec>(HarnessJson.Options)!;
            if (e.InheritEventDamageType) { Unresolved(key); continue; }
            void Reference(string kind, string? id) { if (id is not null) pending.Push(kind + ":" + id); }
            Reference("Status", e.StatusId); Reference("Status", e.AlternativeStatusId);
            Reference("Summon", e.SummonId); Reference("Ability", e.AbilityId);
            switch (e.Operation)
            {
                case AbilityEffectOperation.Damage:
                    if (e.DamageType == DamageType.None || !Enum.IsDefined(e.DamageType)) Unresolved(key);
                    else { types.Add(e.DamageType); evidence.Add(key); }
                    break;
                case AbilityEffectOperation.ApplyCondition:
                    var type = e.Condition switch {
                        StandardConditionType.Poison => DamageType.Poison,
                        StandardConditionType.Burn => DamageType.Burn,
                        StandardConditionType.Bleed => DamageType.Bleed,
                        _ => DamageType.None };
                    if (type != DamageType.None) { types.Add(type); evidence.Add(key); }
                    else if (e.Condition is not (StandardConditionType.Vulnerable or StandardConditionType.Weaken
                        or StandardConditionType.Slow or StandardConditionType.Wound or StandardConditionType.Corrosion
                        or StandardConditionType.Haste or StandardConditionType.Empower or StandardConditionType.Recovery
                        or StandardConditionType.Renewal or StandardConditionType.Guard or StandardConditionType.Ward
                        or StandardConditionType.Unstoppable or StandardConditionType.Stun or StandardConditionType.Taunt
                        or StandardConditionType.Stealth or StandardConditionType.Chill or StandardConditionType.Freeze
                        or StandardConditionType.Mark or StandardConditionType.Cover or StandardConditionType.Silence
                        or StandardConditionType.Soaked)) Unresolved(key);
                    if (e.AlternativeCondition is not null) Unresolved(key);
                    break;
                // These operations do not create a new damage channel; their referenced owners are traversed.
                case AbilityEffectOperation.ApplyStatus:
                case AbilityEffectOperation.ModifyStatusStacks:
                case AbilityEffectOperation.Summon:
                case AbilityEffectOperation.SynchronizeAttributePerStatusStack:
                case AbilityEffectOperation.GrantBarrier:
                    break;
                default:
                    // Includes delegated attacks, random conditions, conversion/retaliation and unsupported routes.
                    Unresolved(key);
                    break;
            }
        }
        return new(limitations.Count == 0, types.Order().ToArray(), evidence.Order(StringComparer.Ordinal).ToArray(),
            limitations.Order(StringComparer.Ordinal).ToArray());
    }
}
