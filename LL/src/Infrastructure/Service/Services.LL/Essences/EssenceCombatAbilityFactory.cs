using Application.Interfaces.Services.LL.Essences;
using Domain.Interfaces.Combat.Abilities;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.Effects.Actions;
using Domain.Models.Combat.Abilities.Effects.Conditions;
using Domain.Models.Combat.Abilities.Effects.Duration;
using Domain.Models.Combat.Abilities.Effects.Intervals;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;
using Domain.Models.Combat.Abilities.Effects.Trigger;
using Domain.Models.Combat.Abilities.Effects.Usages;
using Domain.Models.Combat.Abilities.ResourceCosts;
using Domain.Models.Combat.Abilities.Triggers;
using Domain.Models.Combat.Abilities.Triggers.TriggerFilters;
using Domain.Models.Damages;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;

namespace Services.LL.Essences;

public sealed class EssenceCombatAbilityFactory : IEssenceCombatAbilityFactory
{
    public IReadOnlyList<ResolvedCombatAbility> CreateAbilities(EssenceDefinition definition, PlayerEssence essence)
    {
        var abilities = new List<ResolvedCombatAbility>();

        if (!string.IsNullOrWhiteSpace(definition.ActiveAbility.Id))
        {
            abilities.Add(CreateResolvedCombatAbility(
                definition,
                ApplyEvolutionModifiers(definition.ActiveAbility, definition.Evolution.ActiveAbilityModifiers, essence),
                essence,
                CombatAbilityType.Active));
        }

        if (!string.IsNullOrWhiteSpace(definition.PassiveAbility.Id))
        {
            abilities.Add(CreateResolvedCombatAbility(
                definition,
                ApplyEvolutionModifiers(definition.PassiveAbility, definition.Evolution.PassiveAbilityModifiers, essence),
                essence,
                CombatAbilityType.Passive));
        }

        return abilities;
    }

    private static CombatAbilityDefinition MapCombatAbility(AbilityDefinition ability, PlayerEssence essence, CombatAbilityType type)
    {
        var combatAbility = new CombatAbilityDefinition
        {
            Id = ability.Id,
            Name = ability.Name,
            Description = ability.Description,
            Type = type,
            Cooldown = SecondsToCombatTicks(EssenceProgressionConstants.ScaleActiveCooldownSeconds(ability.CooldownSeconds, essence.AscensionTier)),
            Usage = new UnlimitedUsage(),
            Condition = BuildCondition(ability.Conditions)
        };

        var triggers = type == CombatAbilityType.Active
            ? [new AbilityTriggerDefinition { Type = "OnAbilityUsed" }]
            : ability.Triggers.Count == 0
                ? [new AbilityTriggerDefinition { Type = "OnCombatStart" }]
                : ability.Triggers;

        foreach (var trigger in triggers)
        {
            var combatTrigger = new Trigger
            {
                Event = MapTrigger(trigger.Type),
                Actions = [.. ability.Effects.Select(effect => MapCombatEffect(ability, effect, essence))]
            };

            if (type == CombatAbilityType.Active && combatTrigger.Event == TriggerEvent.OnAbilityUsed)
                combatTrigger.Filters.Add(new AbilityIdFilter { AllowedIds = [ability.Id] });

            combatAbility.Triggers.Add(combatTrigger);
        }

        return combatAbility;
    }

    private static CombatAbilityInstance CreateCombatAbilityInstance(CombatAbilityDefinition definition)
    {
        var instance = new CombatAbilityInstance(definition);
        if (definition.Type == CombatAbilityType.Passive) instance.RemainingTimeUntilUse = 0;
        return instance;
    }

    private static ResolvedCombatAbility CreateResolvedCombatAbility(
        EssenceDefinition definition,
        AbilityDefinition ability,
        PlayerEssence essence,
        CombatAbilityType type)
    {
        var combatDefinition = MapCombatAbility(ability, essence, type);
        var instance = CreateCombatAbilityInstance(combatDefinition);
        var tags = new HashSet<string>(GetEssenceTags(definition, essence), StringComparer.OrdinalIgnoreCase);

        foreach (var tag in ability.Tags)
            tags.Add(tag);

        return new ResolvedCombatAbility(
            ability.Id,
            essence.Id,
            essence.EssenceDefinitionId,
            type.ToString(),
            essence.Level,
            tags,
            combatDefinition.Cooldown,
            instance);
    }

    private static AbilityDefinition ApplyEvolutionModifiers(
        AbilityDefinition ability,
        IReadOnlyCollection<AbilityModifierDefinition> modifiers,
        PlayerEssence essence)
    {
        if (!essence.IsEvolved || modifiers.Count == 0) return ability;

        var copy = new AbilityDefinition
        {
            Id = ability.Id,
            Name = ability.Name,
            Description = ability.Description,
            CooldownSeconds = ability.CooldownSeconds,
            Kind = ability.Kind,
            Targeting = ability.Targeting,
            Tags = [.. ability.Tags],
            Triggers = [.. ability.Triggers.Select(x => new AbilityTriggerDefinition { Type = x.Type, InternalCooldownSeconds = x.InternalCooldownSeconds })],
            Conditions = [.. ability.Conditions.Select(CloneCondition)],
            Effects = [.. ability.Effects.Select(CloneEffect)]
        };

        foreach (var modifier in modifiers)
        {
            if (modifier.Operation.Equals("AddEffect", StringComparison.OrdinalIgnoreCase) && modifier.Effect is not null)
            {
                copy.Effects.Add(CloneEffect(modifier.Effect));
                continue;
            }

            var effect = copy.Effects.FirstOrDefault(x => x.Id.Equals(modifier.Target, StringComparison.OrdinalIgnoreCase));
            if (effect is null) continue;

            if (modifier.Operation.Equals("AddMultiplier", StringComparison.OrdinalIgnoreCase))
            {
                var multiplier = 1 + modifier.Value;
                effect.Scaling.BaseValue *= multiplier;
            }
            else if (modifier.Operation.Equals("AddFlat", StringComparison.OrdinalIgnoreCase))
            {
                effect.Scaling.BaseValue += modifier.Value;
            }
        }

        return copy;
    }

    private static AbilityEffectDefinition CloneEffect(AbilityEffectDefinition effect) =>
        new()
        {
            Id = effect.Id,
            Type = effect.Type,
            Target = effect.Target,
            Attribute = effect.Attribute,
            Status = effect.Status,
            Resource = effect.Resource,
            DurationSeconds = effect.DurationSeconds,
            IntervalSeconds = effect.IntervalSeconds,
            Uses = effect.Uses,
            AttackType = effect.AttackType,
            DamageType = effect.DamageType,
            EffectTags = [.. effect.EffectTags],
            Log = effect.Log,
            LifeStealPercentage = effect.LifeStealPercentage,
            Conditions = [.. effect.Conditions.Select(CloneCondition)],
            Scaling = new AbilityScalingFormula
            {
                BaseValue = effect.Scaling.BaseValue,
                AttributeScaling = [.. effect.Scaling.AttributeScaling.Select(x => new AbilityAttributeScalingDefinition { Attribute = x.Attribute, Coefficient = x.Coefficient })]
            }
        };

    private static AbilityConditionDefinition CloneCondition(AbilityConditionDefinition condition) =>
        new()
        {
            Type = condition.Type,
            Tag = condition.Tag,
            Status = condition.Status,
            Value = condition.Value
        };

    private static EffectDefinition MapCombatEffect(AbilityDefinition ability, AbilityEffectDefinition effect, PlayerEssence essence)
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
        var combatEffect = new EffectDefinition(
            action,
            duration,
            BuildCondition(effect.Conditions.Count == 0 ? ability.Conditions : effect.Conditions),
            interval,
            usage,
            effectTags: ParseEffectTags(effect.EffectTags),
            effectModifications: [],
            targeting: MapTargeting(string.IsNullOrWhiteSpace(effect.Target) ? ability.Targeting : effect.Target),
            attackType: ParseAttackType(effect.AttackType, isDamage ? AttackType.Melee : AttackType.None),
            damageType: ParseDamageType(effect.DamageType, isDamage ? DamageType.Magical : DamageType.None),
            chance: BuildChance(effect.Conditions.Count == 0 ? ability.Conditions : effect.Conditions))
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

    private static ICondition BuildCondition(IReadOnlyCollection<AbilityConditionDefinition> conditions)
    {
        var mapped = conditions
            .Select(MapCondition)
            .Where(x => x is not null)
            .Cast<ICondition>()
            .ToList();

        return mapped.Count switch
        {
            0 => new NoCondition(),
            1 => mapped[0],
            _ => new AllConditions(mapped)
        };
    }

    private static ICondition? MapCondition(AbilityConditionDefinition condition) =>
        condition.Type switch
        {
            AbilityConditionType.TargetHealthBelowPercent when condition.Value is > 0 =>
                new CombatantHealthCondition(useSource: false, (int)Math.Round(condition.Value.Value), ComparisonType.LessThan),
            "HealthBelowPercent" when condition.Value is > 0 =>
                new CombatantHealthCondition(useSource: false, (int)Math.Round(condition.Value.Value), ComparisonType.LessThan),
            AbilityConditionType.SourceHealthBelowPercent when condition.Value is > 0 =>
                new CombatantHealthCondition(useSource: true, (int)Math.Round(condition.Value.Value), ComparisonType.LessThan),
            AbilityConditionType.SourceHealthAbovePercent when condition.Value is > 0 =>
                new CombatantHealthCondition(useSource: true, (int)Math.Round(condition.Value.Value), ComparisonType.GreaterThan),
            AbilityConditionType.TargetHasStatus when !string.IsNullOrWhiteSpace(condition.Status) =>
                new CombatantStatusCondition(useSource: false, condition.Status),
            AbilityConditionType.TargetHasStatusStacksAtLeast when !string.IsNullOrWhiteSpace(condition.Status)
                && condition.Value is > 0
                && Enum.TryParse<StatusEffectType>(condition.Status, ignoreCase: true, out var statusEffect) =>
                new CombatantStatusStacksCondition(useSource: false, statusEffect, (int)Math.Round(condition.Value.Value)),
            AbilityConditionType.SourceHasStatus when !string.IsNullOrWhiteSpace(condition.Status) =>
                new CombatantStatusCondition(useSource: true, condition.Status),
            AbilityConditionType.RandomChance => null,
            AbilityConditionType.ChanceRoll => null,
            AbilityConditionType.CooldownReady => null,
            AbilityConditionType.Always => null,
            AbilityConditionType.SourceHasTag when !string.IsNullOrWhiteSpace(condition.Tag) =>
                new CombatantTagCondition(useSource: true, condition.Tag),
            AbilityConditionType.TargetHasTag when !string.IsNullOrWhiteSpace(condition.Tag) =>
                new CombatantTagCondition(useSource: false, condition.Tag),
            AbilityConditionType.IsSpecies when !string.IsNullOrWhiteSpace(condition.Tag) =>
                new CombatantTagCondition(useSource: false, NormalizeSpeciesTag(condition.Tag)),
            AbilityConditionType.SourceIsSummon =>
                new CombatantSummonedCondition(useSource: true),
            _ => null
        };

    private static string NormalizeSpeciesTag(string tag) =>
        tag.StartsWith("Species.", StringComparison.OrdinalIgnoreCase) ? tag : $"Species.{tag}";

    private static int BuildChance(IReadOnlyCollection<AbilityConditionDefinition> conditions)
    {
        var chance = conditions.FirstOrDefault(x =>
            x.Type.Equals(AbilityConditionType.RandomChance, StringComparison.OrdinalIgnoreCase)
            || x.Type.Equals(AbilityConditionType.ChanceRoll, StringComparison.OrdinalIgnoreCase));
        return chance?.Value is > 0 ? Math.Clamp((int)Math.Round(chance.Value.Value), 1, 100) : 100;
    }

    private static TriggerEvent MapTrigger(string trigger)
    {
        var normalized = trigger.StartsWith("Trigger.", StringComparison.OrdinalIgnoreCase)
            ? trigger["Trigger.".Length..]
            : trigger;

        return normalized switch
        {
            "OnCombatStart" => TriggerEvent.OnCombatStart,
            "OnAbilityUsed" => TriggerEvent.OnAbilityUsed,
            AbilityTriggerType.OnAbilityUse => TriggerEvent.OnAbilityUsed,
            AbilityTriggerType.OnBasicAttack => TriggerEvent.BasicAttack,
            "OnHit" => TriggerEvent.OnAttack,
            AbilityTriggerType.OnMeleeAttack => TriggerEvent.OnMeleeAttack,
            AbilityTriggerType.OnRangedAttack => TriggerEvent.OnRangedAttack,
            AbilityTriggerType.OnAttacked => TriggerEvent.OnAttacked,
            AbilityTriggerType.OnDamaged => TriggerEvent.OnDamaged,
            AbilityTriggerType.OnMeleeAttacked => TriggerEvent.OnMeleeAttacked,
            AbilityTriggerType.OnRangedAttacked => TriggerEvent.OnRangedAttacked,
            AbilityTriggerType.OnHealthChanged => TriggerEvent.OnHealthChanged,
            "OnCrit" => TriggerEvent.OnCriticalHit,
            "OnTakeDamage" => TriggerEvent.OnDamaged,
            "OnKill" => TriggerEvent.OnKill,
            "OnDodge" => TriggerEvent.OnDodge,
            AbilityTriggerType.OnStatusApplied => TriggerEvent.OnStatusApplied,
            AbilityTriggerType.OnStatusExpired => TriggerEvent.OnEffectExpired,
            AbilityTriggerType.OnInterval => TriggerEvent.OnTickInterval,
            "OnDeath" => TriggerEvent.OnDeath,
            "OnHeal" => TriggerEvent.OnHeal,
            AbilityTriggerType.OnHealed => TriggerEvent.OnHealed,
            AbilityTriggerType.OnLifestealHeal => TriggerEvent.OnLifestealHeal,
            _ => throw new NotSupportedException($"Essence trigger '{trigger}' is not supported by combat mapping.")
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

    private static IEnumerable<string> GetEssenceTags(EssenceDefinition definition, PlayerEssence essence) =>
        definition.Tags.Concat(essence.IsEvolved ? definition.Evolution.AddsTags : []);

    private static int SecondsToCombatTicks(double seconds) => Math.Max(0, (int)Math.Round(seconds * 10));
}
