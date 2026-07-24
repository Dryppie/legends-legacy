namespace Domain.Models.Attributes;

public enum AttributeUnit
{
    FlatPoints,
    PercentagePoints,
    Rating,
    HealthPerFiveSeconds,
    MultiplierInput
}

public enum AttributeStackingRule
{
    Additive,
    Multiplicative,
    Maximum,
    DerivedOnly
}

public enum AttributeCapKind
{
    None,
    Fixed,
    ContextDependent
}

public enum AttributeBenchmarkScenario
{
    PhysicalOffense,
    MagicalOffense,
    PeriodicOffense,
    PhysicalPressure,
    MagicalPressure,
    HealingSustain,
    StatusResilience,
    CrowdControlResilience,
    SummonOffense,
    MixedPressure,
    UnmitigatedPressure,
    BurstPressure,
    LongSustain
}

public sealed record AttributeDefinition(
    AttributeType AttributeType,
    string DisplayName,
    string Description,
    AttributeUnit Unit,
    AttributeStackingRule StackingRule,
    float MinimumValue,
    float? MaximumValue,
    AttributeCapKind CapKind,
    bool IsEquipmentEligible,
    bool IsContentFacing,
    int DisplayPrecision,
    string DisplaySuffix,
    AttributeType? ApprovedPrimarySource,
    IReadOnlyList<AttributeBenchmarkScenario> RelevantBenchmarkScenarios);
