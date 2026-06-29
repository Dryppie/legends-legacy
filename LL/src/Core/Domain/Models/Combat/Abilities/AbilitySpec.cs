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
    OnDodge = 19
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
    RestoreResource = 10
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
    NonSummonedAllies = 14
}

public enum AbilityConditionType
{
    Always = 0,
    HealthBelowPercent = 1,
    HealthAbovePercent = 2,
    HasStatus = 3,
    StatusStacksAtLeast = 4,
    HasTag = 5,
    ChancePercent = 6
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

public sealed class AbilitySpec
{
    public string Id { get; set; } = string.Empty;
    public AbilitySpecKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OwningEssenceId { get; set; }
    public int CooldownTicks { get; set; }
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
    public List<AbilityConditionSpec> Conditions { get; set; } = [];
    public List<string> EffectIds { get; set; } = [];
}

public sealed class AbilityEffectSpec
{
    public string Id { get; set; } = string.Empty;
    public AbilityEffectOperation Operation { get; set; }
    public AbilityTargetSelector Target { get; set; } = AbilityTargetSelector.CurrentTarget;
    public int BaseValue { get; set; }
    public AttributeType? ScalingAttribute { get; set; }
    public float ScalingCoefficient { get; set; }
    public AttributeType? Attribute { get; set; }
    public string? StatusId { get; set; }
    public string? SummonId { get; set; }
    public AbilityResourceType Resource { get; set; } = AbilityResourceType.Health;
    public int DurationTicks { get; set; }
    public int IntervalTicks { get; set; }
    public int Uses { get; set; }
    public int ChancePercent { get; set; } = 100;
    public AttackType AttackType { get; set; } = AttackType.None;
    public DamageType DamageType { get; set; } = DamageType.None;
    public float LifeStealPercentage { get; set; }
    public decimal ProcCoefficient { get; set; } = 1m;
    public List<string> Tags { get; set; } = [];
    public List<AbilityConditionSpec> Conditions { get; set; } = [];
}

public sealed class AbilityConditionSpec
{
    public AbilityConditionType Type { get; set; }
    public AbilityConditionSubject Subject { get; set; } = AbilityConditionSubject.Target;
    public string? StatusId { get; set; }
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
    public List<string> Tags { get; set; } = [];
    public List<string> AbilityIds { get; set; } = [];
    public List<SummonAttributeSpec> Attributes { get; set; } = [];
}

public sealed class SummonAttributeSpec
{
    public AttributeType Attribute { get; set; }
    public int BaseValue { get; set; }
    public AttributeType? ScalingAttribute { get; set; }
    public float ScalingCoefficient { get; set; }
    public int MinimumValue { get; set; }
}
