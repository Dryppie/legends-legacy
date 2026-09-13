using System.Text.Json;
using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;

namespace BalanceHarness;

/// <summary>Opt-in direct attribute mitigation hypotheses; no learned weights or reference recipes.</summary>
public static class TowerAttributeDefense
{
    public const string Limitation = "Direct positive attribute-mitigation hypothesis. Trigger/effect conditions, chance, recipient attributes, rounding, caps, living allies and duration may prevent benefit; no guaranteed uptime or strength. Nested and unsupported value routes remain unclassified and ordinarily reachable.";

    public static IReadOnlyList<BossCoverageFeature> Create(BossDiscoveryInputs input, TowerBossInventoryReport inventory)
    {
        var baseline = TowerPartyCoverage.Create(input, inventory).ToDictionary(f => (f.EssenceId, f.Kind));
        var allowed = input.AllowedEssences.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        var nodes = inventory.Nodes.ToDictionary(n => n.Key, StringComparer.Ordinal);
        foreach (var essence in inventory.Essences.Where(e => allowed.Contains(e.Id)))
        foreach (var id in essence.AbilityIds)
        {
            var key = "Ability:" + id;
            var ability = nodes[key].Definition.Deserialize<AbilitySpec>(HarnessJson.Options)!;
            foreach (var effect in ability.Effects.Where(e => Eligible(ability, e)))
            {
                var evidence = new[] { "Effect:" + key + "/" + effect.Id, key }
                    .Concat(ability.Triggers.Select((t, i) => (t, i))
                        .Where(p => p.t.EffectIds.Count == 0 || p.t.EffectIds.Contains(effect.Id, StringComparer.OrdinalIgnoreCase))
                        .Select(p => "Trigger:" + key + "/" + p.i));
                var featureKey = (essence.Id, "protection");
                baseline.TryGetValue(featureKey, out var old);
                baseline[featureKey] = new(essence.Id, "protection", (old?.EvidenceKeys ?? []).Concat(evidence)
                    .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(), Limitation);
            }
        }
        return baseline.Values.OrderBy(f => f.Kind, StringComparer.Ordinal).ThenBy(f => f.EssenceId, StringComparer.Ordinal).ToArray();
    }

    public static bool Eligible(AbilitySpec ability, AbilityEffectSpec effect)
    {
        if (effect.Attribute is not (AttributeType.Armor or AttributeType.Resistance or AttributeType.DamageReduction)
            || effect.Target is not (AbilityTargetSelector.Self or AbilityTargetSelector.AllAllies or AbilityTargetSelector.NonSummonedAllies
                or AbilityTargetSelector.LowestHealthAlly or AbilityTargetSelector.TwoAllies or AbilityTargetSelector.HighestMaxHealthAlly or AbilityTargetSelector.RandomAlly)
            || effect.ChancePercent <= 0 || !float.IsFinite(effect.ScalingCoefficient)
            || ability.Triggers.Count > 0 && !ability.Triggers.Any(t => t.EffectIds.Count == 0 || t.EffectIds.Contains(effect.Id, StringComparer.OrdinalIgnoreCase))) return false;
        // No triggers compiles to OnAbilityUsed for actives and OnCombatStart for passives. Conditional
        // selected triggers remain hypotheses, with their definitions/evidence retained rather than assumed uptime.
        return effect.Operation switch
        {
            // These two operations bypass the generic magnitude calculation in the engine.
            AbilityEffectOperation.ModifyAttributePercentOfInitial => effect.ScalingCoefficient > 0,
            AbilityEffectOperation.SynchronizeAttributePerLivingNonSummonedAlly => effect.MaximumCount > 0
                && (Math.Abs(effect.ScalingCoefficient) > float.Epsilon ? effect.ScalingCoefficient > 0 : effect.BaseValue > 0),
            AbilityEffectOperation.ModifyAttribute => effect.BaseValue >= 0 && effect.ScalingCoefficient >= 0
                && float.IsFinite(effect.MaximumScalingCoefficient) && effect.MaximumScalingCoefficient >= 0
                && effect.EventMagnitudeCoefficient == 0 && effect.ScalingCondition is null && effect.ConditionScalingCoefficient == 0
                && string.IsNullOrEmpty(effect.ScalingStatusId) && effect.StatusScalingCoefficient == 0
                && string.IsNullOrEmpty(effect.ScalingOwnedSummonId) && effect.OwnedSummonScalingCoefficient == 0
                && effect.LivingNonSummonedAllyDamagePercent == 0 && effect.SubsequentTargetDamagePercent == 100
                && (effect.ScalingAttribute is null || effect.ScalingAttribute is AttributeType.Power or AttributeType.MaxHealth or AttributeType.Armor or AttributeType.Resistance or AttributeType.DamageReduction)
                && (effect.BaseValue > 0 || effect.ScalingAttribute is not null && Math.Max(effect.ScalingCoefficient, effect.MaximumScalingCoefficient) > 0),
            _ => false
        };
    }
}
