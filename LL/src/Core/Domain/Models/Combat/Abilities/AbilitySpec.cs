using Domain.Models.Attributes;
using Domain.Models.Damages;

namespace Domain.Models.Combat.Abilities;

public enum AbilitySpecKind
{
    Active = 0,
    Passive = 1
}

public enum AbilityTriggerEvent
{
    OnCombatStart = 0,
    OnAbilityUsed = 1,
    OnBasicAttack = 2,
    OnHit = 3,
    OnDamaged = 4,
    OnAttacked = 5,
    OnStatusApplied = 6,
    OnStatusExpired = 7,
    OnDeath = 8,
    OnKill = 9,
    OnInterval = 10,
    OnMeleeAttack = 11,
    OnRangedAttack = 12,
    OnMeleeAttacked = 13,
    OnRangedAttacked = 14,
    OnHealthChanged = 15,
    OnHeal = 16,
    OnHealed = 17,
    OnLifestealHeal = 18,
    OnDodge = 19,
    OnBarrierApplied = 20,
    OnBarrierAbsorbed = 21,
    OnBarrierBroken = 22,
    OnStatusRemoved = 23,
    OnStatusCleansed = 24,
    OnStatusDispelled = 25,
    OnSummonChanged = 26,
    OnBarrierContributionBroken = 27,
    OnBarrierExpired = 28,
    OnSummonGroupResolved = 29,
    OnStatusChanged = 30,
    OnEnemyDeath = 31,
    OnDamageDealt = 32
}

public enum AbilityEffectOperation
{
    Damage = 0,
    Heal = 1,
    GrantBarrier = 2,
    ApplyStatus = 3,
    ModifyStatusStacks = 4,
    RemoveStatus = 5,
    Cleanse = 6,
    ModifyAttribute = 7,
    Summon = 8,
    SelfDestruct = 9,
    RestoreResource = 10,
    ApplyCondition = 11,
    Dispel = 12,
    ModifyThreat = 13,
    ModifyRegenerationRate = 14,
    ModifyRegenerationInterval = 15,
    ModifyHealingReceived = 16,
    ModifyDamageDealt = 17,
    ModifyDamageTaken = 18,
    ModifyDamageTakenFromCondition = 19,
    ApplyRandomCondition = 20,
    ModifyNextBasicAttackDamage = 21,
    ModifyNextBasicAttackArmorPenetration = 22,
    ModifyAttributePercentOfInitial = 23,
    TransferAttributePercent = 24,
    ConsumeConditionStacks = 25,
    RemoveCondition = 26,
    SynchronizeAttributePerOwnedSummon = 27,
    ConsumeOwnedSummon = 28,
    SynchronizeAttributePerStatusStack = 29,
    SwapHealth = 30,
    SynchronizeAttributePerMissingHealthStep = 31,
    GrantCover = 32,
    ModifyDamageDealtToLowHealth = 33
}

public enum AbilityTargetSelector
{
    Self = 0,
    CurrentTarget = 1,
    Source = 2,
    EventSource = 3,
    EventTarget = 4,
    RandomEnemy = 5,
    LowestHealthAlly = 6,
    AllEnemies = 7,
    AllAllies = 8,
    EveryoneButSelf = 9,
    TwoEnemies = 10,
    TwoAllies = 11,
    HighestMaxHealthAlly = 12,
    SummonedAllies = 13,
    NonSummonedAllies = 14,
    SummonedEnemies = 15,
    LowestHealthEnemy = 16,
    HighestHealthEnemy = 17,
    LowestCurrentHealthEnemy = 18,
    HighestMaxHealthEnemy = 19,
    HighestCurrentHealthOwnedSummon = 20,
    OwnedSummons = 21,
    RandomAlly = 22,
    TwoRandomEnemies = 23,
    ThreeRandomEnemies = 24,
    ThreeEnemies = 25
}

public enum AbilityConditionType
{
    Always = 0,
    HealthBelowPercent = 1,
    HealthAbovePercent = 2,
    HasStatus = 3,
    StatusStacksAtLeast = 4,
    HasTag = 5,
    ChancePercent = 6,
    HasCondition = 7,
    ConditionStacksAtLeast = 8,
    EventDamageTypeIs = 9,
    EventAttackTypeIs = 10,
    EventWasCritical = 11,
    EventWasDirectHit = 12,
    EventIdIs = 13,
    EventSourceIsSelf = 14,
    HealthAtOrBelowPercent = 15,
    AnyEnemyHealthBelowPercent = 16,
    NoEnemyHealthBelowPercent = 17,
    EventSourceIsEnemy = 18,
    EventMagnitudeAtLeast = 19,
    EventMagnitudeAtMost = 20,
    EventSourceIsAlly = 21,
    EventIdIsNot = 22,
    AnyEnemyHasCondition = 23,
    NoEnemyHasCondition = 24,
    HasBarrier = 25,
    EventTargetIsAlly = 26
}

public enum StandardConditionType
{
    Haste = 0,
    Slow = 1,
    Empower = 2,
    Weaken = 3,
    Vulnerable = 4,
    Wound = 5,
    Recovery = 6,
    Decay = 7,
    Renewal = 8,
    Guard = 9,
    Ward = 10,
    Unstoppable = 11,
    Poison = 12,
    Burn = 13,
    Bleed = 14,
    Stun = 15,
    Taunt = 16,
    Stealth = 17,
    Chill = 18,
    Freeze = 19,
    Corrosion = 20,
    Doom = 21,
    Thorns = 22,
    Mark = 23,
    Cover = 24
}

public enum AbilityConditionSubject
{
    Source = 0,
    Target = 1,
    EventSource = 2,
    EventTarget = 3
}

public enum AbilityStatusStackingPolicy
{
    Refresh = 0,
    Stack = 1,
    Replace = 2
}

public enum AbilityResourceType
{
    Health = 0,
    Barrier = 1,
    Cooldown = 2,
    Mana = 3
}

public enum CritEligibility
{
    Default = 0,
    Allowed = 1,
    Disallowed = 2
}

public sealed class AbilitySpec
{
    public string Id { get; set; } = string.Empty;
    public AbilitySpecKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OwningEssenceId { get; set; }
    public int CooldownTicks { get; set; }
    public int? ThreatValue { get; set; }
    public float ThreatMultiplier { get; set; } = 1f;
    public List<string> Tags { get; set; } = [];
    public List<string> DeliveryTags { get; set; } = [];
    public List<string> EffectTags { get; set; } = [];
    public AbilityTargetSelector? TargetingType { get; set; }
    public Dictionary<AttributeType, float> Scaling { get; set; } = [];
    public AbilityConversionFlags ConversionFlags { get; set; } = new();
    public bool IsHardCrowdControl { get; set; }
    public bool CanEcho { get; set; } = true;
    public bool CanRepeat { get; set; } = true;
    public bool CanTriggerWeaponEffects { get; set; } = true;
    public List<AbilityCostSpec> Costs { get; set; } = [];
    public List<AbilityTriggerSpec> Triggers { get; set; } = [];
    public List<AbilityEffectSpec> Effects { get; set; } = [];
}

public sealed class AbilityConversionFlags
{
    public bool AllowDamageTypeConversion { get; set; } = true;
    public bool AllowScalingConversion { get; set; } = true;
    public bool AllowDeliveryConversion { get; set; } = true;
    public bool AllowTargetingConversion { get; set; } = true;
    public bool AllowSummonProxy { get; set; }
    public bool AllowEquipmentOverride { get; set; } = true;
    public bool AllowTrueDamageConversion { get; set; }
}

public sealed class AbilityCostSpec
{
    public AbilityResourceType Resource { get; set; } = AbilityResourceType.Health;
    public int BaseValue { get; set; }
    public AttributeType? ScalingAttribute { get; set; }
    public float ScalingCoefficient { get; set; }
}

public sealed class AbilityTriggerSpec
{
    public AbilityTriggerEvent Event { get; set; }
    public int InternalCooldownTicks { get; set; }
    public int InitialDelayTicks { get; set; }
    public int EveryNthOccurrence { get; set; } = 1;
    public List<AbilityConditionSpec> Conditions { get; set; } = [];
    public List<string> EffectIds { get; set; } = [];
}

public sealed class AbilityEffectSpec
{
    public string Id { get; set; } = string.Empty;
    public AbilityEffectOperation Operation { get; set; }
    public AbilityTargetSelector Target { get; set; } = AbilityTargetSelector.CurrentTarget;
    // Operation-specific parameter for percentages, stacks, charges, or counts.
    // Damage, Heal, and GrantBarrier must derive magnitude from scaling inputs instead.
    public int BaseValue { get; set; }
    public AttributeType? ScalingAttribute { get; set; }
    public AbilityConditionSubject ScalingAttributeSubject { get; set; } = AbilityConditionSubject.Source;
    public float ScalingCoefficient { get; set; }
    public float MaximumScalingCoefficient { get; set; }
    public float EventMagnitudeCoefficient { get; set; }
    public StandardConditionType? ScalingCondition { get; set; }
    public AbilityConditionSubject ScalingConditionSubject { get; set; } = AbilityConditionSubject.Source;
    public float ConditionScalingCoefficient { get; set; }
    public string? ScalingStatusId { get; set; }
    public AbilityConditionSubject ScalingStatusSubject { get; set; } = AbilityConditionSubject.Source;
    public AttributeType StatusScalingAttribute { get; set; } = AttributeType.Power;
    public float StatusScalingCoefficient { get; set; }
    public AttributeType? HealingScalingAttribute { get; set; }
    public float HealingScalingCoefficient { get; set; }
    public float MaximumHealingScalingCoefficient { get; set; }
    public AttributeType? Attribute { get; set; }
    public string? StatusId { get; set; }
    public StandardConditionType? Condition { get; set; }
    public StandardConditionType? AlternativeCondition { get; set; }
    public string? SummonId { get; set; }
    public bool CountAllOwnedSummons { get; set; }
    public int RepeatCount { get; set; } = 1;
    public int HealthStepPercent { get; set; }
    public string? RepeatPerOwnedSummonId { get; set; }
    public string? ScalingOwnedSummonId { get; set; }
    public float OwnedSummonScalingCoefficient { get; set; }
    public string? SummonGroupId { get; set; }
    public string? LinkedEffectId { get; set; }
    public double SummonPowerMultiplier { get; set; } = 1d;
    public double SummonHealthMultiplier { get; set; } = 1d;
    public AbilityResourceType Resource { get; set; } = AbilityResourceType.Health;
    public int DurationTicks { get; set; }
    public bool RefreshDuration { get; set; }
    public int IntervalTicks { get; set; }
    public int Uses { get; set; }
    public bool OncePerTarget { get; set; }
    public bool GuaranteedConditionApplication { get; set; }
    public int StaggerPower { get; set; }
    public bool MaintainWhileConditionsMet { get; set; }
    public int LivingNonSummonedAllyDamagePercent { get; set; }
    public int SubsequentTargetDamagePercent { get; set; } = 100;
    public int ChancePercent { get; set; } = 100;
    public AttackType AttackType { get; set; } = AttackType.None;
    public DamageType DamageType { get; set; } = DamageType.None;
    public bool InheritEventDamageType { get; set; }
    public CritEligibility CritEligibility { get; set; } = CritEligibility.Default;
    public float CritChanceBonus { get; set; }
    public float ArmorPenetrationBonus { get; set; }
    public float LifeStealPercentage { get; set; }
    public StandardConditionType? LifeStealTargetCondition { get; set; }
    public decimal ProcCoefficient { get; set; } = 1m;
    public List<string> Tags { get; set; } = [];
    public List<AbilityConditionSpec> Conditions { get; set; } = [];
}

public sealed class AbilityConditionSpec
{
    public AbilityConditionType Type { get; set; }
    public AbilityConditionSubject Subject { get; set; } = AbilityConditionSubject.Target;
    public string? StatusId { get; set; }
    public StandardConditionType? Condition { get; set; }
    public DamageType DamageType { get; set; } = DamageType.None;
    public AttackType AttackType { get; set; } = AttackType.None;
    public string? Tag { get; set; }
    public int Value { get; set; }
}

public sealed class StatusSpec
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AbilityStatusStackingPolicy StackingPolicy { get; set; }
    public int MaxStacks { get; set; } = 1;
    public int DurationTicks { get; set; }
    public bool LockAtMaxStacks { get; set; }
    public float SourceDamageTakenPercentPerStack { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<AbilityTriggerSpec> Triggers { get; set; } = [];
    public List<AbilityEffectSpec> Effects { get; set; } = [];
}

public sealed class SummonSpec
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int DurationTicks { get; set; }
    public int MaxActive { get; set; }
    public bool CanBasicAttack { get; set; } = true;
    public float? ThreatMultiplier { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<string> AbilityIds { get; set; } = [];
    public List<SummonAttributeSpec> Attributes { get; set; } = [];
}

public enum AbilityThreatFunctionBand
{
    ProtectiveSelf,
    ProtectiveAlly,
    Retaliation,
    SupportAlly,
    HardControl,
    SoftControl,
    Damage,
    SelfSustain,
    Utility
}

public static class AbilityThreatRules
{
    private const int TicksPerSecond = 10;
    private const int NominalBasicAttackIntervalTicks = 30;
    private const int NominalOneShotPeriodTicks = 200;
    private const int NominalReactivePeriodTicks = 40;

    public const int BasicAttackThreatValue = 8;
    public const float ProtectiveSelfThreatPerSecond = 5f;
    public const float ProtectiveAllyThreatPerSecond = 5f;
    public const float RetaliationThreatPerSecond = 3.5f;
    public const float SupportAllyThreatPerSecond = 3.5f;
    public const float HardControlThreatPerSecond = 2.5f;
    public const float SoftControlThreatPerSecond = 2f;
    public const float DamageThreatPerSecond = 1.5f;
    public const float SelfSustainThreatPerSecond = 1.5f;
    public const float UtilityThreatPerSecond = 0.5f;

    public static int GetThreatValue(AbilitySpec ability, AbilityThreatTuning? tuning = null)
    {
        if (ability.ThreatValue is { } authored)
            return authored;

        tuning ??= AbilityThreatTuning.Default;

        if (ability.Tags.Contains("NonCombat", StringComparer.OrdinalIgnoreCase))
            return 0;

        if (ability.Kind == AbilitySpecKind.Active)
            return DeriveThreatValue(ability.Effects, ability.CooldownTicks, null, tuning);

        var effectsById = ability.Effects.ToDictionary(effect => effect.Id, StringComparer.OrdinalIgnoreCase);
        var triggers = ability.Triggers.Count == 0
            ? [new AbilityTriggerSpec { Event = AbilityTriggerEvent.OnCombatStart }]
            : ability.Triggers;

        return triggers.Sum(trigger => GetThreatValue(ability, trigger, effectsById, tuning));
    }

    public static int GetThreatValue(
        AbilitySpec ability,
        AbilityTriggerSpec trigger,
        AbilityThreatTuning? tuning = null)
    {
        if (ability.ThreatValue is { } authored)
            return authored;

        tuning ??= AbilityThreatTuning.Default;
        if (ability.Tags.Contains("NonCombat", StringComparer.OrdinalIgnoreCase))
            return 0;

        var effectsById = ability.Effects.ToDictionary(effect => effect.Id, StringComparer.OrdinalIgnoreCase);
        return GetThreatValue(ability, trigger, effectsById, tuning);
    }

    public static double GetEstimatedThreatPerSecond(
        AbilitySpec ability,
        AbilityThreatTuning? tuning = null)
    {
        tuning ??= AbilityThreatTuning.Default;
        var multiplier = Math.Max(0, ability.ThreatMultiplier);
        if (multiplier == 0)
            return 0;

        if (ability.Kind == AbilitySpecKind.Active)
        {
            return GetThreatValue(ability, tuning)
                * multiplier
                * TicksPerSecond
                / Math.Max(1, ability.CooldownTicks);
        }

        var triggers = ability.Triggers.Count == 0
            ? [new AbilityTriggerSpec { Event = AbilityTriggerEvent.OnCombatStart }]
            : ability.Triggers;

        var triggeredThreatPerSecond = triggers.Sum(trigger =>
            GetThreatValue(ability, trigger, tuning)
            * multiplier
            * TicksPerSecond
            / Math.Max(1, GetTriggerPeriodTicks(ability, trigger)));
        return triggeredThreatPerSecond + GetMaintainedThreatPerSecond(ability, tuning) * multiplier;
    }

    public static bool HasMaintainedThreat(AbilitySpec ability, AbilityThreatTuning? tuning = null) =>
        ability.ThreatMultiplier > 0 && GetMaintainedThreatPerSecond(ability, tuning) > 0;

    public static double GetMaintainedThreatPerSecond(
        AbilitySpec ability,
        AbilityThreatTuning? tuning = null)
    {
        if (ability.ThreatValue is not null
            || ability.Tags.Contains("NonCombat", StringComparer.OrdinalIgnoreCase))
        {
            return 0;
        }

        tuning ??= AbilityThreatTuning.Default;
        return ability.Effects
            .Where(effect => effect.MaintainWhileConditionsMet)
            .Select(effect => GetFunctionBand(effect, triggerEvent: null))
            .Where(band => band is not null)
            .Select(band => band!.Value)
            .Distinct()
            .Sum(band => GetThreatPerSecond(band, tuning));
    }

    public static int GetTriggerPeriodTicks(AbilitySpec ability, AbilityTriggerSpec trigger)
    {
        var effectsById = ability.Effects.ToDictionary(effect => effect.Id, StringComparer.OrdinalIgnoreCase);
        var effects = trigger.EffectIds.Count == 0
            ? ability.Effects
            : trigger.EffectIds.Select(effectId => effectsById[effectId]).ToList();
        return GetEffectivePeriodTicks(trigger, effects);
    }

    private static int GetThreatValue(
        AbilitySpec ability,
        AbilityTriggerSpec trigger,
        IReadOnlyDictionary<string, AbilityEffectSpec> effectsById,
        AbilityThreatTuning tuning)
    {
        var effects = trigger.EffectIds.Count == 0
            ? ability.Effects
            : trigger.EffectIds.Select(effectId => effectsById[effectId]).ToList();
        effects = effects.Where(effect => !effect.MaintainWhileConditionsMet).ToList();
        var periodTicks = GetEffectivePeriodTicks(trigger, effects);
        return DeriveThreatValue(effects, periodTicks, trigger.Event, tuning);
    }

    private static int DeriveThreatValue(
        IEnumerable<AbilityEffectSpec> effects,
        int periodTicks,
        AbilityTriggerEvent? triggerEvent,
        AbilityThreatTuning tuning)
    {
        var bands = effects
            .Select(effect => GetFunctionBand(effect, triggerEvent))
            .Where(band => band is not null)
            .Select(band => band!.Value)
            .Distinct()
            .ToList();
        if (bands.Count == 0)
            return 0;

        var threatPerSecond = bands.Sum(band => GetThreatPerSecond(band, tuning));
        return (int)Math.Round(
            threatPerSecond * Math.Max(1, periodTicks) / TicksPerSecond,
            MidpointRounding.AwayFromZero);
    }

    private static int GetEffectivePeriodTicks(
        AbilityTriggerSpec trigger,
        IReadOnlyCollection<AbilityEffectSpec> effects)
    {
        // One-shot effects may retain a very large runtime lockout to avoid rechecking the
        // trigger. That sentinel is not their threat cadence; use the actual activation delay.
        if (effects.Any(effect => effect.Uses == 1))
        {
            return trigger.InitialDelayTicks > 0
                ? trigger.InitialDelayTicks
                : NominalOneShotPeriodTicks;
        }

        if (trigger.InternalCooldownTicks > 0)
            return trigger.InternalCooldownTicks;

        if (trigger.Event == AbilityTriggerEvent.OnCombatStart)
            return NominalOneShotPeriodTicks;

        if (trigger.Event == AbilityTriggerEvent.OnInterval)
            return Math.Max(1, trigger.EveryNthOccurrence);

        if (trigger.Event is AbilityTriggerEvent.OnBasicAttack
                or AbilityTriggerEvent.OnMeleeAttack
                or AbilityTriggerEvent.OnRangedAttack)
        {
            return NominalBasicAttackIntervalTicks * Math.Max(1, trigger.EveryNthOccurrence);
        }

        return NominalReactivePeriodTicks * Math.Max(1, trigger.EveryNthOccurrence);
    }

    public static AbilityThreatFunctionBand? GetFunctionBand(
        AbilityEffectSpec effect,
        AbilityTriggerEvent? triggerEvent)
    {
        var targetsSelf = IsSelfTarget(effect.Target);
        var targetsAllies = IsAllyTarget(effect.Target);
        var reactive = triggerEvent is AbilityTriggerEvent.OnDamaged
            or AbilityTriggerEvent.OnAttacked
            or AbilityTriggerEvent.OnMeleeAttacked
            or AbilityTriggerEvent.OnRangedAttacked
            or AbilityTriggerEvent.OnBarrierAbsorbed
            or AbilityTriggerEvent.OnBarrierBroken
            or AbilityTriggerEvent.OnBarrierContributionBroken;

        if (effect.Operation == AbilityEffectOperation.Damage)
            return targetsSelf ? null : reactive ? AbilityThreatFunctionBand.Retaliation : AbilityThreatFunctionBand.Damage;

        if (effect.Operation == AbilityEffectOperation.Heal)
            return targetsSelf ? AbilityThreatFunctionBand.SelfSustain : AbilityThreatFunctionBand.SupportAlly;

        if (effect.Operation == AbilityEffectOperation.GrantBarrier)
            return targetsSelf ? AbilityThreatFunctionBand.SelfSustain : AbilityThreatFunctionBand.ProtectiveAlly;

        if (effect.Operation == AbilityEffectOperation.GrantCover)
            return AbilityThreatFunctionBand.ProtectiveAlly;

        if (effect.Operation == AbilityEffectOperation.Summon
            || effect.Operation == AbilityEffectOperation.ModifyThreat)
        {
            return null;
        }

        if (effect.Operation == AbilityEffectOperation.ApplyStatus && !targetsSelf && !targetsAllies)
            return AbilityThreatFunctionBand.SoftControl;

        if (effect.Operation == AbilityEffectOperation.ApplyCondition)
            return GetConditionBand(effect, targetsSelf, targetsAllies);

        if (effect.Operation is (AbilityEffectOperation.ModifyAttribute
                or AbilityEffectOperation.ModifyAttributePercentOfInitial)
            && IsDefensiveAttribute(effect.Attribute))
        {
            if (effect.BaseValue <= 0 && effect.ScalingCoefficient <= 0)
                return null;
            return targetsSelf ? AbilityThreatFunctionBand.ProtectiveSelf : AbilityThreatFunctionBand.ProtectiveAlly;
        }

        if (effect.Operation is (AbilityEffectOperation.ModifyDamageTaken
                or AbilityEffectOperation.ModifyDamageTakenFromCondition)
            && (effect.BaseValue < 0 || effect.ScalingCoefficient < 0))
        {
            return targetsSelf ? AbilityThreatFunctionBand.ProtectiveSelf : AbilityThreatFunctionBand.ProtectiveAlly;
        }

        if (effect.Operation == AbilityEffectOperation.ModifyHealingReceived)
        {
            if (effect.BaseValue > 0 || effect.ScalingCoefficient > 0)
                return targetsSelf ? AbilityThreatFunctionBand.SelfSustain : AbilityThreatFunctionBand.SupportAlly;
            return targetsSelf || targetsAllies ? null : AbilityThreatFunctionBand.SoftControl;
        }

        if (effect.Operation == AbilityEffectOperation.ModifyRegenerationRate)
        {
            if (effect.BaseValue > 0 || effect.ScalingCoefficient > 0)
                return targetsSelf ? AbilityThreatFunctionBand.SelfSustain : AbilityThreatFunctionBand.SupportAlly;
            return targetsSelf || targetsAllies ? null : AbilityThreatFunctionBand.SoftControl;
        }

        if (effect.Operation == AbilityEffectOperation.ModifyDamageDealt
            && (effect.BaseValue < 0 || effect.ScalingCoefficient < 0)
            && !targetsSelf
            && !targetsAllies)
        {
            return AbilityThreatFunctionBand.SoftControl;
        }

        return AbilityThreatFunctionBand.Utility;
    }

    private static AbilityThreatFunctionBand? GetConditionBand(
        AbilityEffectSpec effect,
        bool targetsSelf,
        bool targetsAllies) => effect.Condition switch
        {
            StandardConditionType.Mark or StandardConditionType.Taunt or StandardConditionType.Stealth => null,
            StandardConditionType.Thorns => AbilityThreatFunctionBand.Retaliation,
            StandardConditionType.Guard or StandardConditionType.Ward or StandardConditionType.Renewal
                or StandardConditionType.Recovery or StandardConditionType.Unstoppable when targetsSelf
                => AbilityThreatFunctionBand.ProtectiveSelf,
            StandardConditionType.Guard or StandardConditionType.Ward or StandardConditionType.Renewal
                or StandardConditionType.Recovery or StandardConditionType.Unstoppable when targetsAllies
                => AbilityThreatFunctionBand.ProtectiveAlly,
            StandardConditionType.Empower or StandardConditionType.Haste when !targetsSelf
                => AbilityThreatFunctionBand.SupportAlly,
            StandardConditionType.Stun or StandardConditionType.Freeze when !targetsSelf && !targetsAllies
                => AbilityThreatFunctionBand.HardControl,
            StandardConditionType.Slow or StandardConditionType.Weaken or StandardConditionType.Vulnerable
                or StandardConditionType.Chill or StandardConditionType.Corrosion or StandardConditionType.Wound
                or StandardConditionType.Decay or StandardConditionType.Doom when !targetsSelf && !targetsAllies
                => AbilityThreatFunctionBand.SoftControl,
            StandardConditionType.Poison or StandardConditionType.Burn or StandardConditionType.Bleed
                when !targetsSelf && !targetsAllies => AbilityThreatFunctionBand.Damage,
            _ => AbilityThreatFunctionBand.Utility
        };

    private static bool IsSelfTarget(AbilityTargetSelector target) =>
        target is AbilityTargetSelector.Self or AbilityTargetSelector.Source;

    private static bool IsAllyTarget(AbilityTargetSelector target) => target is
        AbilityTargetSelector.LowestHealthAlly
        or AbilityTargetSelector.AllAllies
        or AbilityTargetSelector.TwoAllies
        or AbilityTargetSelector.HighestMaxHealthAlly
        or AbilityTargetSelector.SummonedAllies
        or AbilityTargetSelector.NonSummonedAllies
        or AbilityTargetSelector.HighestCurrentHealthOwnedSummon
        or AbilityTargetSelector.OwnedSummons
        or AbilityTargetSelector.RandomAlly;

    private static bool IsDefensiveAttribute(AttributeType? attribute) => attribute is
        AttributeType.Armor
        or AttributeType.Resistance
        or AttributeType.MaxHealth
        or AttributeType.DamageReduction
        or AttributeType.BlockChance
        or AttributeType.DodgeChance;

    public static float GetThreatPerSecond(AbilityThreatFunctionBand band, AbilityThreatTuning tuning) => band switch
    {
        AbilityThreatFunctionBand.ProtectiveSelf => tuning.ProtectiveSelfThreatPerSecond,
        AbilityThreatFunctionBand.ProtectiveAlly => tuning.ProtectiveAllyThreatPerSecond,
        AbilityThreatFunctionBand.Retaliation => tuning.RetaliationThreatPerSecond,
        AbilityThreatFunctionBand.SupportAlly => tuning.SupportAllyThreatPerSecond,
        AbilityThreatFunctionBand.HardControl => tuning.HardControlThreatPerSecond,
        AbilityThreatFunctionBand.SoftControl => tuning.SoftControlThreatPerSecond,
        AbilityThreatFunctionBand.Damage => tuning.DamageThreatPerSecond,
        AbilityThreatFunctionBand.SelfSustain => tuning.SelfSustainThreatPerSecond,
        _ => tuning.UtilityThreatPerSecond
    };
}

public sealed record AbilityThreatTuning(
    int BasicAttackThreatValue,
    float ProtectiveSelfThreatPerSecond,
    float ProtectiveAllyThreatPerSecond,
    float RetaliationThreatPerSecond,
    float SupportAllyThreatPerSecond,
    float HardControlThreatPerSecond,
    float SoftControlThreatPerSecond,
    float DamageThreatPerSecond,
    float SelfSustainThreatPerSecond,
    float UtilityThreatPerSecond,
    float DefaultSummonThreatMultiplier)
{
    public static AbilityThreatTuning Default { get; } = new(
        AbilityThreatRules.BasicAttackThreatValue,
        AbilityThreatRules.ProtectiveSelfThreatPerSecond,
        AbilityThreatRules.ProtectiveAllyThreatPerSecond,
        AbilityThreatRules.RetaliationThreatPerSecond,
        AbilityThreatRules.SupportAllyThreatPerSecond,
        AbilityThreatRules.HardControlThreatPerSecond,
        AbilityThreatRules.SoftControlThreatPerSecond,
        AbilityThreatRules.DamageThreatPerSecond,
        AbilityThreatRules.SelfSustainThreatPerSecond,
        AbilityThreatRules.UtilityThreatPerSecond,
        0.25f);
}

public sealed class SummonAttributeSpec
{
    public AttributeType Attribute { get; set; }
    public int BaseValue { get; set; }
    public AttributeType? ScalingAttribute { get; set; }
    public float ScalingCoefficient { get; set; }
    public int MinimumValue { get; set; }
}
