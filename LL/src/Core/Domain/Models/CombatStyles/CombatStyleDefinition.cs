using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;

namespace Domain.Models.CombatStyles;

public sealed record CombatStyleDefinition(
    string Id,
    string Name,
    string Description,
    string ResourceId,
    decimal ResourceMaxAmount,
    int MaxLevel,
    IReadOnlyList<string> RecommendedTags,
    IReadOnlyList<AttributeType> RecommendedStats,
    IReadOnlyList<CombatStyleFocusDefinition> Focuses,
    IReadOnlyList<CombatStyleTreeNodeDefinition> SkillTreeNodes,
    IReadOnlyList<CombatStyleRuleDefinition> Rules,
    IReadOnlyList<StyleRuleOperation> ResourceOverflowOperations,
    string CoreMechanic);

public sealed record CombatStyleTreeNodeDefinition(
    string Id,
    string BranchId,
    string Name,
    string Description,
    int MaxRank,
    int RequiredLevel,
    string? RequiredNodeId,
    int X,
    int Y,
    IReadOnlyList<string> Tags,
    bool CountsTowardFocus)
{
    public IReadOnlyList<CombatStyleRuleDefinition> Rules { get; init; } = [];
    public int Row { get; init; }
    public string Lane { get; init; } = CombatStyleNodeLanes.Middle;
    public string NodeType { get; init; } = CombatStyleNodeTypes.Minor;
    public string? MutatorKind { get; init; }
    public IReadOnlyList<string> MutatorGroups { get; init; } = [];
    public CombatStyleAbilityMutatorDefinition? Mutator { get; init; }
    public CombatStyleNodeTooltipDefinition Tooltip { get; init; } = new();
}

public static class CombatStyleNodeTypes
{
    public const string Major = "Major";
    public const string Minor = "Minor";
}

public static class CombatStyleNodeLanes
{
    public const string Left = "Left";
    public const string Middle = "Middle";
    public const string Right = "Right";
}

public static class CombatStyleMutatorKinds
{
    public const string Amplifier = "Amplifier";
    public const string Converter = "Converter";
    public const string Enabler = "Enabler";
}

public static class CombatStyleMutatorGroups
{
    public const string DamageTypeConversion = "DamageTypeConversion";
    public const string ScalingConversion = "ScalingConversion";
    public const string DeliveryConversion = "DeliveryConversion";
    public const string EquipmentOverride = "EquipmentOverride";
    public const string TargetingConversion = "TargetingConversion";
    public const string ResourceConversion = "ResourceConversion";
    public const string SummonConversion = "SummonConversion";
    public const string DefensiveConversion = "DefensiveConversion";
    public const string SupportConversion = "SupportConversion";
    public const string ControlConversion = "ControlConversion";
    public const string TimingConversion = "TimingConversion";
}

public sealed record CombatStyleNodeTooltipDefinition
{
    public IReadOnlyList<string> Affects { get; init; } = [];
    public IReadOnlyList<string> Tradeoffs { get; init; } = [];
    public IReadOnlyList<string> DoesNotAffect { get; init; } = [];
}

public sealed record CombatStyleAbilityMutatorDefinition
{
    public string Kind { get; init; } = string.Empty;
    public IReadOnlyList<string> Groups { get; init; } = [];
    public CombatStyleMutatorConditionDefinition Conditions { get; init; } = new();
    public CombatStyleMutatorTransformDefinition Transform { get; init; } = new();
    public CombatStyleMutatorTradeoffDefinition Tradeoff { get; init; } = new();
    public decimal? PvpCoefficient { get; init; }
}

public sealed record CombatStyleMutatorConditionDefinition
{
    public IReadOnlyList<string> RequiredAbilityTags { get; init; } = [];
    public IReadOnlyList<string> AnyAbilityTags { get; init; } = [];
    public IReadOnlyList<string> RequiredDeliveryTags { get; init; } = [];
    public IReadOnlyList<string> AnyDeliveryTags { get; init; } = [];
    public IReadOnlyList<string> RequiredEffectTags { get; init; } = [];
    public IReadOnlyList<string> AnyEffectTags { get; init; } = [];
    public IReadOnlyList<AbilityEffectOperation> EffectOperations { get; init; } = [];
    public IReadOnlyList<DamageType> AllowedDamageTypes { get; init; } = [];
    public IReadOnlyList<AbilityTargetSelector> TargetSelectors { get; init; } = [];
    public bool ActiveAbilityOnly { get; init; } = true;
    public bool PassiveAbilityOnly { get; init; }
    public bool? AllowDamageTypeConversionRequired { get; init; }
    public bool? AllowScalingConversionRequired { get; init; }
    public bool? AllowDeliveryConversionRequired { get; init; }
    public bool? AllowTargetingConversionRequired { get; init; }
    public bool? AllowSummonProxyRequired { get; init; }
    public bool? AllowEquipmentOverrideRequired { get; init; }
    public bool ExcludeTrueDamage { get; init; } = true;
    public bool ExcludeHardCrowdControl { get; init; }
}

public sealed record CombatStyleMutatorTransformDefinition
{
    public DamageType? DamageType { get; init; }
    public AbilityTargetSelector? TargetingType { get; init; }
    public AttributeType? ScalingAttribute { get; init; }
    public float? ScalingCoefficientOverride { get; init; }
    public decimal? ScalingCoefficientMultiplier { get; init; }
    public decimal? EffectPotencyMultiplier { get; init; }
    public decimal? CooldownMultiplier { get; init; }
    public decimal? ResourceCostMultiplier { get; init; }
    public IReadOnlyList<string> AddAbilityTags { get; init; } = [];
    public IReadOnlyList<string> AddDeliveryTags { get; init; } = [];
    public IReadOnlyList<string> AddEffectTags { get; init; } = [];
    public IReadOnlyList<string> AddEffectTagsToMatchingEffects { get; init; } = [];
}

public sealed record CombatStyleMutatorTradeoffDefinition
{
    public decimal? EffectPotencyMultiplier { get; init; }
    public decimal? CooldownMultiplier { get; init; }
    public decimal? ResourceCostMultiplier { get; init; }
    public decimal? ProcCoefficientMultiplier { get; init; }
}

public sealed record CombatStyleFocusDefinition(
    string Id,
    string StyleId,
    string Name,
    string Description,
    int UnlockLevel,
    IReadOnlyList<string> RecommendedTags,
    IReadOnlyList<AttributeType> RecommendedStats,
    IReadOnlyList<CombatStyleRuleDefinition> Rules);

public sealed record CombatStyleRuleDefinition
{
    public string Id { get; init; } = string.Empty;
    public int MinStyleLevel { get; init; } = 1;
    public int? MaxStyleLevel { get; init; }
    public CombatStyleEventType EventType { get; init; }
    public EffectPredicate Predicate { get; init; } = new();
    public StyleRuleOperation Operation { get; init; } = new NoOpStyleRuleOperation();
    public int? MaxTriggersPerEncounter { get; init; }
    public int? MaxTriggersPerSource { get; init; }
    public int? MaxTriggersPerTarget { get; init; }
}

public enum CombatStyleEventType
{
    EncounterStarted = 0,
    EffectCalculation = 1,
    AbilityResolved = 2,
    DamageDealt = 3,
    DamageTaken = 4,
    BarrierApplied = 5,
    SummonCreated = 6,
    SummonAction = 7
}

public sealed record EffectPredicate
{
    public IReadOnlyList<string> RequiredTags { get; init; } = [];
    public IReadOnlyList<string> AnyTags { get; init; } = [];
    public IReadOnlyList<AbilityEffectOperation> EffectOperations { get; init; } = [];
    public IReadOnlyList<DamageType> DamageTypes { get; init; } = [];
    public IReadOnlyList<Domain.Models.Damages.AttackType> AttackTypes { get; init; } = [];
    public IReadOnlyList<AbilityTargetSelector> TargetSelectors { get; init; } = [];
    public bool? SourceMustBePlayer { get; init; }
    public bool? SourceMustBeOwnedSummon { get; init; }
    public bool? TargetMustBePlayer { get; init; }
    public bool? TargetMustBeOwnedSummon { get; init; }
    public bool ActiveAbilityOnly { get; init; }
    public bool PassiveAbilityOnly { get; init; }
    public decimal? SourceHealthPercentAtOrBelow { get; init; }
    public IReadOnlyList<StyleValueModifier> SourceHealthPercentAtOrBelowModifiers { get; init; } = [];
    public decimal? TargetHealthPercentAtOrBelow { get; init; }
    public IReadOnlyList<StyleValueModifier> TargetHealthPercentAtOrBelowModifiers { get; init; } = [];
    public bool MultiTargetOnly { get; init; }
    public bool AmplifiableEffectOnly { get; init; }
    public bool HealOrBarrierOnly { get; init; }
    public bool StatusOrDebuffOnly { get; init; }
    public bool RangedOnly { get; init; }
}

public abstract record StyleRuleOperation;

public sealed record NoOpStyleRuleOperation : StyleRuleOperation;

public sealed record StyleValueModifier(
    string Type,
    decimal Value,
    string? NodeId = null,
    string? FocusId = null,
    int MinStyleLevel = 1,
    int? MaxStyleLevel = null);

public sealed record ModifyEffectAmountOperation(
    decimal AdditivePercent,
    bool UsesProcCoefficient = false,
    IReadOnlyList<StyleValueModifier>? AdditivePercentModifiers = null) : StyleRuleOperation;

public sealed record AddDamageReductionOperation(
    decimal Percent,
    bool UsesProcCoefficient = false,
    IReadOnlyList<StyleValueModifier>? PercentModifiers = null) : StyleRuleOperation;

public sealed record GainStyleResourceOperation(
    string ResourceId,
    decimal Amount,
    bool UsesProcCoefficient = true,
    IReadOnlyList<StyleValueModifier>? AmountModifiers = null) : StyleRuleOperation;

public sealed record AddBonusDamageFromStatOperation(
    AttributeType Stat,
    decimal Coefficient,
    DamageType DamageType,
    bool UsesProcCoefficient = true,
    IReadOnlyList<StyleValueModifier>? CoefficientModifiers = null) : StyleRuleOperation;

public sealed record ModifySummonStatsOperation(
    decimal? MaxHealthPercent = null,
    decimal? DamagePercent = null,
    decimal? DamageReductionInheritancePercent = null,
    decimal? MagicPowerInheritancePercent = null,
    decimal? MaxInheritedDamageReductionPercent = null,
    IReadOnlyList<StyleValueModifier>? MaxHealthPercentModifiers = null,
    IReadOnlyList<StyleValueModifier>? DamagePercentModifiers = null) : StyleRuleOperation;

public sealed record SetPendingEmpowermentOperation(
    string EmpowermentId,
    EffectPredicate AppliesTo,
    decimal AdditivePercent,
    bool ConsumeOnUse = true,
    IReadOnlyList<StyleValueModifier>? AdditivePercentModifiers = null) : StyleRuleOperation;

public sealed record TriggerProtectiveShellOperation : StyleRuleOperation;

public sealed record GrantBarrierFromMaxHealthOperation(
    string TriggerKey,
    decimal Percent,
    int? MaxTriggersPerEncounter = null,
    IReadOnlyList<StyleValueModifier>? PercentModifiers = null,
    IReadOnlyList<StyleValueModifier>? MaxTriggerModifiers = null) : StyleRuleOperation;
