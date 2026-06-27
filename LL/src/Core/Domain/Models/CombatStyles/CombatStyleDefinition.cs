using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;

namespace Domain.Models.CombatStyles;

public sealed record CombatStyleDefinition(
    string Id,
    string Name,
    string Description,
    string ResourceId,
    int MaxLevel,
    IReadOnlyList<string> RecommendedTags,
    IReadOnlyList<AttributeType> RecommendedStats,
    IReadOnlyList<CombatStyleFocusDefinition> Focuses,
    IReadOnlyList<CombatStyleRuleDefinition> Rules,
    string CoreMechanic);

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
    public bool? SourceMustBePlayer { get; init; }
    public bool? SourceMustBeOwnedSummon { get; init; }
    public bool? TargetMustBePlayer { get; init; }
    public bool? TargetMustBeOwnedSummon { get; init; }
    public bool ActiveAbilityOnly { get; init; }
    public bool PassiveAbilityOnly { get; init; }
}

public abstract record StyleRuleOperation;

public sealed record NoOpStyleRuleOperation : StyleRuleOperation;

public sealed record ModifyEffectAmountOperation(
    decimal AdditivePercent,
    bool UsesProcCoefficient = false) : StyleRuleOperation;

public sealed record AddDamageReductionOperation(
    decimal Percent,
    bool UsesProcCoefficient = false) : StyleRuleOperation;

public sealed record GainStyleResourceOperation(
    string ResourceId,
    decimal Amount,
    bool UsesProcCoefficient = true) : StyleRuleOperation;

public sealed record AddBonusDamageFromStatOperation(
    AttributeType Stat,
    decimal Coefficient,
    DamageType DamageType,
    bool UsesProcCoefficient = true) : StyleRuleOperation;

public sealed record ModifySummonStatsOperation(
    decimal? MaxHealthPercent = null,
    decimal? DamagePercent = null,
    decimal? DamageReductionInheritancePercent = null,
    decimal? MagicPowerInheritancePercent = null,
    decimal? MaxInheritedDamageReductionPercent = null) : StyleRuleOperation;

public sealed record SetPendingEmpowermentOperation(
    string EmpowermentId,
    EffectPredicate AppliesTo,
    decimal AdditivePercent,
    bool ConsumeOnUse = true) : StyleRuleOperation;
