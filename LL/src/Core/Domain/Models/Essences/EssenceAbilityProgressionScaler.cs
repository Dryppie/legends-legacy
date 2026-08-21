using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;

namespace Domain.Models.Essences;

public static class EssenceAbilityProgressionScaler
{
    public static AbilitySpec Apply(AbilitySpec ability, int ascensionTier)
    {
        if (ascensionTier <= 0)
            return ability;

        var scaled = CloneAbility(ability);
        if (scaled.Kind == AbilitySpecKind.Active)
            scaled.CooldownTicks = ScaleCooldownTicks(scaled.CooldownTicks, ascensionTier);

        foreach (var trigger in scaled.Triggers)
            trigger.InternalCooldownTicks = ScaleCooldownTicks(trigger.InternalCooldownTicks, ascensionTier);

        foreach (var effect in scaled.Effects)
        {
            var valueMultiplier = EssenceProgressionConstants.ScaleAbilityValue(
                1d,
                ascensionTier,
                effect.Operation.ToString());

            effect.BaseValue = ScaleValue(effect.BaseValue, valueMultiplier);
            effect.ScalingCoefficient *= (float)valueMultiplier;
            effect.MaximumScalingCoefficient *= (float)valueMultiplier;
            effect.EventMagnitudeCoefficient *= (float)valueMultiplier;
            effect.ConditionScalingCoefficient *= (float)valueMultiplier;
            effect.StatusScalingCoefficient *= (float)valueMultiplier;

            if (effect.DurationTicks > 0)
            {
                effect.DurationTicks = SecondsToTicks(EssenceProgressionConstants.ScaleEffectDurationSeconds(
                    effect.DurationTicks / 10d,
                    ascensionTier,
                    effect.Operation.ToString(),
                    effect.StatusId));
            }

            if (effect.Operation == AbilityEffectOperation.Summon)
            {
                effect.SummonPowerMultiplier *= EssenceProgressionConstants.GetSummonPowerMultiplier(ascensionTier);
                effect.SummonHealthMultiplier *= EssenceProgressionConstants.GetSummonHealthMultiplier(ascensionTier);
            }
        }

        scaled.Description = ScaleAuthoredModifierValues(
            ability.Description,
            ability.Effects,
            scaled.Effects);

        return scaled;
    }

    private static string ScaleAuthoredModifierValues(
        string description,
        IReadOnlyList<AbilityEffectSpec> originalEffects,
        IReadOnlyList<AbilityEffectSpec> scaledEffects)
    {
        var scaledDescription = description;
        for (var index = 0; index < Math.Min(originalEffects.Count, scaledEffects.Count); index++)
        {
            var original = originalEffects[index];
            var scaled = scaledEffects[index];
            if (!UsesAuthoredBaseValue(original.Operation)
                || original.BaseValue == 0
                || original.BaseValue == scaled.BaseValue)
            {
                continue;
            }

            scaledDescription = ReplaceFirstNumericToken(
                scaledDescription,
                Math.Abs(original.BaseValue).ToString(),
                Math.Abs(scaled.BaseValue).ToString());
        }

        return scaledDescription;
    }

    private static bool UsesAuthoredBaseValue(AbilityEffectOperation operation) =>
        operation is AbilityEffectOperation.ModifyAttribute
            or AbilityEffectOperation.ModifyStatusStacks
            or AbilityEffectOperation.ModifyThreat
            or AbilityEffectOperation.ModifyRegenerationRate
            or AbilityEffectOperation.ModifyRegenerationInterval
            or AbilityEffectOperation.ModifyHealingReceived
            or AbilityEffectOperation.ModifyDamageDealt
            or AbilityEffectOperation.ModifyDamageTaken
            or AbilityEffectOperation.ModifyDamageTakenFromCondition
            or AbilityEffectOperation.ModifyNextBasicAttackDamage
            or AbilityEffectOperation.ModifyNextBasicAttackArmorPenetration;

    private static string ReplaceFirstNumericToken(
        string value,
        string original,
        string replacement)
    {
        var searchFrom = 0;
        while (searchFrom < value.Length)
        {
            var index = value.IndexOf(original, searchFrom, StringComparison.Ordinal);
            if (index < 0)
                return value;

            var hasNumericCharacterBefore = index > 0
                && (char.IsDigit(value[index - 1]) || value[index - 1] == '.');
            var end = index + original.Length;
            var hasNumericCharacterAfter = end < value.Length
                && (char.IsDigit(value[end]) || value[end] == '.');
            if (!hasNumericCharacterBefore && !hasNumericCharacterAfter)
                return value[..index] + replacement + value[end..];

            searchFrom = end;
        }

        return value;
    }

    private static int ScaleCooldownTicks(int ticks, int ascensionTier) =>
        ticks <= 0
            ? ticks
            : SecondsToTicks(EssenceProgressionConstants.ScaleActiveCooldownSeconds(ticks / 10d, ascensionTier));

    private static int ScaleValue(int value, double multiplier) =>
        (int)Math.Round(value * multiplier, MidpointRounding.AwayFromZero);

    private static int SecondsToTicks(double seconds) =>
        Math.Max(0, (int)Math.Round(seconds * 10d, MidpointRounding.AwayFromZero));

    private static AbilitySpec CloneAbility(AbilitySpec ability) =>
        new()
        {
            Id = ability.Id,
            Kind = ability.Kind,
            Name = ability.Name,
            Description = ability.Description,
            OwningEssenceId = ability.OwningEssenceId,
            CooldownTicks = ability.CooldownTicks,
            ThreatValue = ability.ThreatValue,
            ThreatMultiplier = ability.ThreatMultiplier,
            Tags = [.. ability.Tags],
            DeliveryTags = [.. ability.DeliveryTags],
            EffectTags = [.. ability.EffectTags],
            TargetingType = ability.TargetingType,
            Scaling = new Dictionary<AttributeType, float>(ability.Scaling),
            ConversionFlags = CloneConversionFlags(ability.ConversionFlags),
            IsHardCrowdControl = ability.IsHardCrowdControl,
            CanEcho = ability.CanEcho,
            CanRepeat = ability.CanRepeat,
            CanTriggerWeaponEffects = ability.CanTriggerWeaponEffects,
            Costs = [.. ability.Costs.Select(CloneCost)],
            Triggers = [.. ability.Triggers.Select(CloneTrigger)],
            Effects = [.. ability.Effects.Select(CloneEffect)]
        };

    private static AbilityConversionFlags CloneConversionFlags(AbilityConversionFlags flags) =>
        new()
        {
            AllowDamageTypeConversion = flags.AllowDamageTypeConversion,
            AllowScalingConversion = flags.AllowScalingConversion,
            AllowDeliveryConversion = flags.AllowDeliveryConversion,
            AllowTargetingConversion = flags.AllowTargetingConversion,
            AllowSummonProxy = flags.AllowSummonProxy,
            AllowEquipmentOverride = flags.AllowEquipmentOverride,
            AllowTrueDamageConversion = flags.AllowTrueDamageConversion
        };

    private static AbilityCostSpec CloneCost(AbilityCostSpec cost) =>
        new()
        {
            Resource = cost.Resource,
            BaseValue = cost.BaseValue,
            ScalingAttribute = cost.ScalingAttribute,
            ScalingCoefficient = cost.ScalingCoefficient
        };

    private static AbilityTriggerSpec CloneTrigger(AbilityTriggerSpec trigger) =>
        new()
        {
            Event = trigger.Event,
            InternalCooldownTicks = trigger.InternalCooldownTicks,
            InitialDelayTicks = trigger.InitialDelayTicks,
            EveryNthOccurrence = trigger.EveryNthOccurrence,
            Conditions = [.. trigger.Conditions.Select(CloneCondition)],
            EffectIds = [.. trigger.EffectIds]
        };

    private static AbilityEffectSpec CloneEffect(AbilityEffectSpec effect) =>
        new()
        {
            Id = effect.Id,
            Operation = effect.Operation,
            Target = effect.Target,
            BaseValue = effect.BaseValue,
            ScalingAttribute = effect.ScalingAttribute,
            ScalingAttributeSubject = effect.ScalingAttributeSubject,
            ScalingCoefficient = effect.ScalingCoefficient,
            MaximumScalingCoefficient = effect.MaximumScalingCoefficient,
            EventMagnitudeCoefficient = effect.EventMagnitudeCoefficient,
            ScalingCondition = effect.ScalingCondition,
            ConditionScalingCoefficient = effect.ConditionScalingCoefficient,
            ScalingStatusId = effect.ScalingStatusId,
            ScalingStatusSubject = effect.ScalingStatusSubject,
            StatusScalingAttribute = effect.StatusScalingAttribute,
            StatusScalingCoefficient = effect.StatusScalingCoefficient,
            HealingScalingAttribute = effect.HealingScalingAttribute,
            HealingScalingCoefficient = effect.HealingScalingCoefficient,
            MaximumHealingScalingCoefficient = effect.MaximumHealingScalingCoefficient,
            Attribute = effect.Attribute,
            StatusId = effect.StatusId,
            Condition = effect.Condition,
            AlternativeCondition = effect.AlternativeCondition,
            SummonId = effect.SummonId,
            RepeatCount = effect.RepeatCount,
            HealthStepPercent = effect.HealthStepPercent,
            RepeatPerOwnedSummonId = effect.RepeatPerOwnedSummonId,
            ScalingOwnedSummonId = effect.ScalingOwnedSummonId,
            OwnedSummonScalingCoefficient = effect.OwnedSummonScalingCoefficient,
            SummonGroupId = effect.SummonGroupId,
            LinkedEffectId = effect.LinkedEffectId,
            SummonPowerMultiplier = effect.SummonPowerMultiplier,
            SummonHealthMultiplier = effect.SummonHealthMultiplier,
            Resource = effect.Resource,
            DurationTicks = effect.DurationTicks,
            IntervalTicks = effect.IntervalTicks,
            Uses = effect.Uses,
            OncePerTarget = effect.OncePerTarget,
            GuaranteedConditionApplication = effect.GuaranteedConditionApplication,
            StaggerPower = effect.StaggerPower,
            MaintainWhileConditionsMet = effect.MaintainWhileConditionsMet,
            LivingNonSummonedAllyDamagePercent = effect.LivingNonSummonedAllyDamagePercent,
            SubsequentTargetDamagePercent = effect.SubsequentTargetDamagePercent,
            ChancePercent = effect.ChancePercent,
            AttackType = effect.AttackType,
            DamageType = effect.DamageType,
            CritEligibility = effect.CritEligibility,
            CritChanceBonus = effect.CritChanceBonus,
            ArmorPenetrationBonus = effect.ArmorPenetrationBonus,
            LifeStealPercentage = effect.LifeStealPercentage,
            LifeStealTargetCondition = effect.LifeStealTargetCondition,
            ProcCoefficient = effect.ProcCoefficient,
            Tags = [.. effect.Tags],
            Conditions = [.. effect.Conditions.Select(CloneCondition)]
        };

    private static AbilityConditionSpec CloneCondition(AbilityConditionSpec condition) =>
        new()
        {
            Type = condition.Type,
            Subject = condition.Subject,
            StatusId = condition.StatusId,
            Condition = condition.Condition,
            DamageType = condition.DamageType,
            AttackType = condition.AttackType,
            Tag = condition.Tag,
            Value = condition.Value
        };
}
