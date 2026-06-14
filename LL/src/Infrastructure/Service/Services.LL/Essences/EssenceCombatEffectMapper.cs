using Domain.Interfaces.Combat.Abilities;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.Effects.Actions;
using Domain.Models.Combat.Abilities.Effects.Duration;
using Domain.Models.Combat.Abilities.Effects.Intervals;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;
using Domain.Models.Combat.Abilities.Effects.Usages;
using Domain.Models.Combat.Abilities.ResourceCosts;
using Domain.Models.Damages;
using Domain.Models.Essences;

namespace Services.LL.Essences;

internal static class EssenceCombatEffectMapper
{
    public static EffectDefinition Map(AbilityDefinition ability, AbilityEffectDefinition effect, PlayerEssence essence)
    {
        var magnitude = Scale(effect, essence);
        var scaledDurationSeconds = effect.DurationSeconds is > 0
            ? EssenceProgressionConstants.ScaleEffectDurationSeconds(effect.DurationSeconds.Value, essence.AscensionTier, effect.Type, effect.Status)
            : effect.DurationSeconds;
        var action = BuildAction(effect, magnitude, essence.AscensionTier, scaledDurationSeconds);
        var isDamage = action is CombatEffectAction { Operation: CombatEffectOperation.Damage };
        IEffectDuration duration = scaledDurationSeconds is > 0
            ? new TimedDuration(SecondsToCombatTicks(scaledDurationSeconds.Value))
            : new NoDuration();
        IEffectInterval interval = effect.IntervalSeconds is > 0
            ? new Interval(SecondsToCombatTicks(effect.IntervalSeconds.Value))
            : new NoInterval();
        IUsage usage = effect.Uses is > 0
            ? new LimitedUsage(effect.Uses.Value)
            : new UnlimitedUsage();
        var effectConditions = effect.Conditions.Count == 0 ? ability.Conditions : effect.Conditions;
        var combatEffect = new EffectDefinition(
            action,
            duration,
            EssenceCombatConditionMapper.Build(effectConditions),
            interval,
            usage,
            effectTags: ParseEffectTags(effect.EffectTags),
            effectModifications: [],
            targeting: MapTargeting(string.IsNullOrWhiteSpace(effect.Target) ? ability.Targeting : effect.Target),
            attackType: ParseAttackType(effect.AttackType, isDamage ? AttackType.Melee : AttackType.None),
            damageType: ParseDamageType(effect.DamageType, isDamage ? DamageType.Magical : DamageType.None),
            chance: EssenceCombatConditionMapper.BuildChance(effectConditions))
        {
            Log = string.IsNullOrWhiteSpace(effect.Log) ? BuildEffectLog(effect.Type) : effect.Log,
            SourceName = ability.Name
        };

        return combatEffect;
    }

    private static IEffectAction BuildAction(AbilityEffectDefinition effect, int magnitude, int ascensionTier, double? scaledDurationSeconds)
    {
        var scalingAttribute = FirstScalingAttribute(effect);
        var scalingMultiplier = FirstScalingCoefficient(effect);

        return effect.Type switch
        {
            AbilityEffectType.Damage => new CombatEffectAction { Operation = CombatEffectOperation.Damage, Magnitude = magnitude, ScalingAttribute = scalingAttribute, ScalingMultiplier = scalingMultiplier, LifeStealPercentage = effect.LifeStealPercentage },
            AbilityEffectType.Heal => new CombatEffectAction { Operation = CombatEffectOperation.RestoreResource, Resource = ResourceType.Health, Magnitude = magnitude, ScalingAttribute = scalingAttribute, ScalingMultiplier = scalingMultiplier },
            AbilityEffectType.GrantBarrier => new CombatEffectAction { Operation = CombatEffectOperation.RestoreResource, Resource = ResourceType.Barrier, Magnitude = magnitude, ScalingAttribute = scalingAttribute, ScalingMultiplier = scalingMultiplier },
            AbilityEffectType.RemoveStatus => new CombatEffectAction { Operation = CombatEffectOperation.RemoveStatus, StatusId = effect.Status ?? string.Empty, Magnitude = Math.Max(1, magnitude) },
            AbilityEffectType.ModifyStatusEffect => new CombatEffectAction { Operation = CombatEffectOperation.ModifyStatusEffect, StatusId = effect.Status ?? string.Empty, Magnitude = Math.Max(1, magnitude) },
            AbilityEffectType.Cleanse => new CombatEffectAction { Operation = CombatEffectOperation.Cleanse },
            AbilityEffectType.Summon => new CombatEffectAction
            {
                Operation = CombatEffectOperation.Summon,
                SummonId = effect.Status ?? effect.Attribute ?? effect.Id,
                SummonDuration = scaledDurationSeconds is > 0 ? SecondsToCombatTicks(scaledDurationSeconds.Value) : 0,
                SummonPowerMultiplier = (float)EssenceProgressionConstants.GetSummonPowerMultiplier(ascensionTier),
                SummonHealthMultiplier = (float)EssenceProgressionConstants.GetSummonHealthMultiplier(ascensionTier)
            },
            AbilityEffectType.Taunt => new CombatEffectAction { Operation = CombatEffectOperation.ModifyAttribute, Attribute = AttributeType.Fortitude, Magnitude = magnitude, ModifierType = ModifierType.Flat },
            AbilityEffectType.ReflectDamage => new CombatEffectAction { Operation = CombatEffectOperation.Damage, Magnitude = magnitude, ScalingAttribute = scalingAttribute, ScalingMultiplier = scalingMultiplier },
            AbilityEffectType.AbsorbDamage => new CombatEffectAction { Operation = CombatEffectOperation.RestoreResource, Resource = ResourceType.Barrier, Magnitude = magnitude, ScalingAttribute = scalingAttribute, ScalingMultiplier = scalingMultiplier },
            AbilityEffectType.TriggerSecondaryEffect => new CombatEffectAction { Operation = CombatEffectOperation.TriggerSecondaryEffect, SecondaryEffectId = effect.Status ?? effect.Id, Magnitude = magnitude },
            AbilityEffectType.RestoreResource when effect.Resource?.Equals(nameof(ResourceType.Health), StringComparison.OrdinalIgnoreCase) == true =>
                new CombatEffectAction { Operation = CombatEffectOperation.RestoreResource, Resource = ResourceType.Health, Magnitude = magnitude, ScalingAttribute = scalingAttribute, ScalingMultiplier = scalingMultiplier },
            AbilityEffectType.RestoreResource when effect.Resource?.Equals(nameof(ResourceType.Barrier), StringComparison.OrdinalIgnoreCase) == true =>
                new CombatEffectAction { Operation = CombatEffectOperation.RestoreResource, Resource = ResourceType.Barrier, Magnitude = magnitude, ScalingAttribute = scalingAttribute, ScalingMultiplier = scalingMultiplier },
            AbilityEffectType.RestoreResource when effect.Attribute?.Equals(nameof(AttributeType.MaxHealth), StringComparison.OrdinalIgnoreCase) == true =>
                new CombatEffectAction { Operation = CombatEffectOperation.RestoreResource, Resource = ResourceType.Health, Magnitude = magnitude, ScalingAttribute = scalingAttribute, ScalingMultiplier = scalingMultiplier },
            AbilityEffectType.RestoreResource =>
                new CombatEffectAction { Operation = CombatEffectOperation.ModifyAttribute, Attribute = AttributeType.Cooldown, Magnitude = magnitude, ModifierType = ModifierType.Flat },
            AbilityEffectType.ModifyAttribute => new CombatEffectAction { Operation = CombatEffectOperation.ModifyAttribute, Attribute = ParseAttribute(effect.Attribute), Magnitude = magnitude, ModifierType = ModifierType.Flat },
            AbilityEffectType.ApplyStatus when !string.IsNullOrWhiteSpace(effect.Status) => new CombatEffectAction
            {
                Operation = CombatEffectOperation.ApplyStatus,
                StatusId = effect.Status,
                StatusDuration = scaledDurationSeconds is > 0 ? SecondsToCombatTicks(scaledDurationSeconds.Value) : 0
            },
            _ => throw new NotSupportedException($"Essence effect type '{effect.Type}' is not supported by combat mapping.")
        };
    }

    private static CombatTargeting MapTargeting(string target) =>
        target switch
        {
            AbilityTargetSelector.Self => CombatTargeting.Self,
            AbilityTargetSelector.CurrentTarget => CombatTargeting.SingleEnemy,
            AbilityTargetSelector.RandomEnemy => CombatTargeting.SingleRandomEnemy,
            AbilityTargetSelector.LowestHealthEnemy => CombatTargeting.SingleEnemyLowestHealth,
            AbilityTargetSelector.HighestHealthEnemy => CombatTargeting.SingleEnemy,
            AbilityTargetSelector.LowestHealthAlly => CombatTargeting.SingleAllyLowestHealth,
            AbilityTargetSelector.RandomAlly => CombatTargeting.SingleRandomAlly,
            AbilityTargetSelector.AllEnemies => CombatTargeting.AllEnemies,
            AbilityTargetSelector.AllAllies => CombatTargeting.AllAllies,
            AbilityTargetSelector.EveryoneButYou => CombatTargeting.EveryoneButYou,
            AbilityTargetSelector.TwoEnemies => CombatTargeting.TwoEnemies,
            AbilityTargetSelector.TwoAllies => CombatTargeting.TwoAllies,
            AbilityTargetSelector.HighestMaxHealthAlly => CombatTargeting.AllyHighestMaxHealth,
            AbilityTargetSelector.AllyHighestMaxHealth => CombatTargeting.AllyHighestMaxHealth,
            AbilityTargetSelector.Attacker => CombatTargeting.CauseOfTrigger,
            AbilityTargetSelector.DamageSource => CombatTargeting.CauseOfTrigger,
            AbilityTargetSelector.AbilityUser => CombatTargeting.Self,
            AbilityTargetSelector.SummonedAllies => CombatTargeting.SummonedAllies,
            AbilityTargetSelector.NonSummonedAllies => CombatTargeting.NonSummonedAllies,
            _ => CombatTargeting.SingleEnemy
        };

    private static string BuildEffectLog(string effectType) =>
        effectType switch
        {
            AbilityEffectType.Damage => "{Actor}'s Essence hit {Target} for {Amount}.",
            AbilityEffectType.Heal => "{Actor}'s Essence restored {Amount} health to {Target}.",
            AbilityEffectType.GrantBarrier => "{Actor}'s Essence granted {Amount} barrier to {Target}.",
            AbilityEffectType.RestoreResource => "{Actor}'s Essence restored {Amount} resource to {Target}.",
            AbilityEffectType.ModifyAttribute => "{Actor}'s Essence modified {Target} by {Amount}.",
            AbilityEffectType.ApplyStatus => "{Actor}'s Essence applied {Status} to {Target}.",
            AbilityEffectType.ModifyStatusEffect => "{Actor}'s Essence applied {Amount} {Status} to {Target}.",
            AbilityEffectType.RemoveStatus => "{Actor}'s Essence removed {Status} from {Target}.",
            AbilityEffectType.Cleanse => "{Actor}'s Essence cleansed {Amount} effects from {Target}.",
            AbilityEffectType.Summon => "{Actor}'s Essence summoned {Target}.",
            AbilityEffectType.Taunt => "{Actor}'s Essence drew {Amount} threat from {Target}.",
            AbilityEffectType.ReflectDamage => "{Actor}'s Essence reflected {Amount} damage to {Target}.",
            AbilityEffectType.AbsorbDamage => "{Actor}'s Essence absorbed {Amount} damage for {Target}.",
            AbilityEffectType.TriggerSecondaryEffect => "{Actor}'s Essence triggered {Status} on {Target}.",
            _ => "{Actor}'s Essence affected {Target} for {Amount}."
        };

    private static int Scale(AbilityEffectDefinition effect, PlayerEssence essence) =>
        Math.Max(0, (int)Math.Round(EssenceProgressionConstants.ScaleAbilityValue(
            effect.Scaling.BaseValue,
            essence.Level,
            essence.AscensionTier,
            effect.Type)));

    private static AttributeType? FirstScalingAttribute(AbilityEffectDefinition effect) =>
        effect.Scaling.AttributeScaling.FirstOrDefault()?.Attribute;

    private static float FirstScalingCoefficient(AbilityEffectDefinition effect) =>
        (float)(effect.Scaling.AttributeScaling.FirstOrDefault()?.Coefficient ?? 0);

    private static AttributeType ParseAttribute(string? attribute) =>
        Enum.TryParse<AttributeType>(attribute, ignoreCase: true, out var parsed)
            ? parsed
            : throw new NotSupportedException($"Essence attribute '{attribute}' is not supported by combat mapping.");

    private static AttackType ParseAttackType(string? attackType, AttackType fallback) =>
        Enum.TryParse<AttackType>(attackType, ignoreCase: true, out var parsed) ? parsed : fallback;

    private static DamageType ParseDamageType(string? damageType, DamageType fallback) =>
        Enum.TryParse<DamageType>(damageType, ignoreCase: true, out var parsed) ? parsed : fallback;

    private static List<EffectTag> ParseEffectTags(IEnumerable<string> tags) =>
        tags.Select(tag => Enum.TryParse<EffectTag>(tag, ignoreCase: true, out var parsed) ? parsed : EffectTag.None)
            .Where(tag => tag != EffectTag.None)
            .Distinct()
            .ToList();

    private static int SecondsToCombatTicks(double seconds) => Math.Max(0, (int)Math.Round(seconds * 10));
}
