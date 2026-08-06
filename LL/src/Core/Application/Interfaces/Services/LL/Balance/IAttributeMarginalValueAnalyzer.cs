using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;

namespace Application.Interfaces.Services.LL.Balance;

public interface IAttributeMarginalValueAnalyzer
{
    AttributeBalanceAnalysisReport Analyze(CancellationToken cancellationToken);
}

public enum AttributeBalanceScenario
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

public enum AttributeBalanceFindingKind
{
    Inert,
    Overvalued,
    Undervalued,
    CapLimited,
    EqualBudgetMismatch,
    LoadoutMismatch,
    LoadoutCapPressure,
    AggregateCapWaste,
    SummonCalibrationMismatch,
    HandCalibrationMismatch,
    CraftingCombatPeerMismatch,
    MaximumProgressionMismatch,
    BalanceVersionBlocked
}

public enum EquipmentLoadoutComparisonPurpose
{
    PeerBalance,
    OutputDecomposition
}

public sealed record AttributeBalanceAnalysisReport(
    int BalanceVersion,
    int CombatRulesVersion,
    IReadOnlyCollection<AttributeDefinition> AttributeDefinitions,
    IReadOnlyList<int> Tiers,
    IReadOnlyList<int> Seeds,
    double MarginalBudgetFraction,
    IReadOnlyList<AttributeMarginalValueMeasurement> Measurements,
    IReadOnlyList<EqualBudgetAttributeComparison> EqualBudgetComparisons,
    IReadOnlyList<EquipmentLoadoutMeasurement> Loadouts,
    IReadOnlyList<EquipmentLoadoutComparison> LoadoutComparisons,
    IReadOnlyList<SummonCalibrationComparison> SummonCalibrations,
    IReadOnlyList<HandCalibrationComparison> HandCalibrations,
    IReadOnlyList<CraftingCombatPeerComparison> CraftingCombatPeers,
    CraftingCatalogConstraintReport CraftingCatalogConstraints,
    MaximumEquipmentProgressionReport MaximumEquipmentProgression,
    EquipmentBalanceCalibrationGate CalibrationGate,
    IReadOnlyList<AttributeBalanceFinding> Findings);

public sealed record AttributeMarginalValueMeasurement(
    int Tier,
    AttributeType Attribute,
    double BaselineValue,
    double PointDelta,
    double BudgetSpent,
    double CurrentCostPerPoint,
    double? SuggestedCostPerPoint,
    double MedianRelativeGainPercent,
    double RelativeGainPerBudget,
    bool CapLimited,
    IReadOnlyList<AttributeScenarioMeasurement> Scenarios);

public sealed record AttributeScenarioMeasurement(
    AttributeBalanceScenario Scenario,
    double BaselineScore,
    double ModifiedScore,
    double RelativeGainPercent,
    double RelativeGainConfidenceLowPercent,
    double RelativeGainConfidenceHighPercent);

public enum AttributePeerComparisonGroup
{
    Offense,
    Crit,
    Defense,
    Sustain,
    Penetration
}

public enum AttributePeerComparisonIntent
{
    StrictPeer,
    GeneralistVersusSpecialist
}

public sealed record EqualBudgetAttributeComparison(
    string Id,
    AttributePeerComparisonGroup Group,
    AttributePeerComparisonIntent Intent,
    int Tier,
    AttributeBalanceScenario Scenario,
    string Context,
    bool IsReleaseGate,
    string FirstLabel,
    string SecondLabel,
    AttributeType? FirstAttribute,
    AttributeType? SecondAttribute,
    double Budget,
    double TolerancePercentagePoints,
    double FirstRelativeGainPercent,
    double SecondRelativeGainPercent,
    double DifferencePercentagePoints,
    bool Passed,
    EquipmentLoadoutOutput BaselineOutput,
    EquipmentLoadoutOutput FirstOutput,
    EquipmentLoadoutOutput SecondOutput);

public sealed record EquipmentLoadoutMeasurement(
    string Id,
    string Name,
    int Tier,
    double TargetBudget,
    double SpentBudget,
    double UnspentBudget,
    double AggregateRedistributedBudget,
    double RelevantScenarioUtilityIndex,
    IReadOnlyDictionary<AttributeType, double> AttributePoints,
    IReadOnlyDictionary<AttributeType, double> PreRedistributionAttributePoints,
    IReadOnlyList<AttributeType> AttributesOverSingleStatCap,
    IReadOnlyList<EquipmentAggregateCapMeasurement> AggregateCapsBeforeRedistribution,
    IReadOnlyList<EquipmentAggregateCapMeasurement> AggregateCaps,
    IReadOnlyList<EquipmentLoadoutAllocationRecommendation> AllocationRecommendations,
    IReadOnlyList<EquipmentLoadoutScenarioMeasurement> Scenarios);

public sealed record EquipmentLoadoutAllocationRecommendation(
    AttributeType Attribute,
    double CurrentBudgetSharePercent,
    double CandidateBudgetSharePercent,
    double PointChange,
    double BudgetChange);

public sealed record EquipmentAggregateCapMeasurement(
    AttributeType Attribute,
    double EffectiveCharacterCap,
    double BaselineValue,
    double DirectEquipmentPoints,
    double TotalValue,
    double EffectiveValue,
    double ExcessPoints,
    double DirectEquipmentExcessPoints,
    double EquivalentWastedBudget,
    double WastedTargetBudgetPercent);

public sealed record EquipmentLoadoutScenarioMeasurement(
    AttributeBalanceScenario Scenario,
    bool IsRoleRelevant,
    double MeanScore,
    double RelativeToScenarioMedianPercent,
    double ScoreConfidenceLow,
    double ScoreConfidenceHigh,
    EquipmentLoadoutOutput Output,
    EquipmentLoadoutUtilityBreakdown Utility);

public sealed record EquipmentLoadoutOutput(
    double DirectDamage,
    double SummonDamage,
    double Healing,
    double HealthRegeneration,
    double BarrierGenerated,
    double BarrierAbsorbed,
    double IncomingRawDamage,
    double AvoidedDamage,
    double TypedMitigationPrevented,
    double PhysicalMitigationPrevented,
    double MagicalMitigationPrevented,
    double BlockPrevented,
    double DamageReductionPrevented,
    double DamageAmplified,
    double FinalHealthDamage,
    double DamageTaken,
    double RemainingHealth,
    double DurationTicks,
    double AvoidedAttacks,
    double SummonsCreated,
    double AverageActiveSummons,
    double SummonUptimePercent);

public sealed record EquipmentLoadoutUtilityBreakdown(
    double Damage,
    double Sustain,
    double Prevention,
    double Survival,
    double Total);

public sealed record EquipmentLoadoutComparison(
    int Tier,
    AttributeBalanceScenario Scenario,
    EquipmentLoadoutComparisonPurpose Purpose,
    string FirstLoadoutId,
    string SecondLoadoutId,
    double FirstScore,
    double SecondScore,
    double DifferencePercent,
    EquipmentLoadoutOutput FirstOutput,
    EquipmentLoadoutOutput SecondOutput);

public enum HandCalibrationMode
{
    RepresentativeFundingAndBehavior,
    EqualBudget,
    EqualBudgetAndBehavior
}

public sealed record SummonCalibrationComparison(
    int Tier,
    int DurationTicks,
    double SummonerSpentBudget,
    double DirectCasterSpentBudget,
    double SummonerDamagePerHundredBudget,
    double DirectCasterDamagePerHundredBudget,
    double EqualBudgetDifferencePercent,
    double SummonAbilityReferenceDamage,
    double DirectAbilityReferenceDamage,
    double AbilityBudgetDifferencePercent,
    double SummonDamageSharePercent,
    double ExplicitSummonStatContributionPercent,
    EquipmentLoadoutOutput SummonerOutput,
    EquipmentLoadoutOutput WithoutSummonAbilityOutput,
    EquipmentLoadoutOutput WithoutExplicitSummonStatsOutput,
    EquipmentLoadoutOutput DirectCasterOutput);

public sealed record HandCalibrationComparison(
    int Tier,
    int DurationTicks,
    HandCalibrationMode Mode,
    double DualWieldTargetBudget,
    double TwoHandedTargetBudget,
    double DualWieldSpentBudget,
    double TwoHandedSpentBudget,
    double DualWieldDamagePerHundredBudget,
    double TwoHandedDamagePerHundredBudget,
    double DifferencePercent,
    EquipmentLoadoutOutput DualWieldOutput,
    EquipmentLoadoutOutput TwoHandedOutput);

public enum CraftingCombatPeerGroup
{
    HandConfiguration,
    Shield,
    ArmorFamily,
    Blueprint
}

public sealed record CraftingCombatPeerComparison(
    string Id,
    CraftingCombatPeerGroup Group,
    int Tier,
    AttributeBalanceScenario Scenario,
    string FirstDesignId,
    string SecondDesignId,
    double FirstSpentBudget,
    double SecondSpentBudget,
    IReadOnlyDictionary<AttributeType, double> FirstAttributePoints,
    IReadOnlyDictionary<AttributeType, double> SecondAttributePoints,
    double FirstUtilityPerHundredBudget,
    double SecondUtilityPerHundredBudget,
    double DifferencePercent,
    double TolerancePercent,
    bool IsReleaseGate,
    bool Passed,
    EquipmentLoadoutOutput FirstOutput,
    EquipmentLoadoutOutput SecondOutput);

public sealed record CraftingCatalogConstraintReport(
    int CandidateBalanceVersion,
    bool ProductionActive,
    int RecipesAnalyzed,
    int BlueprintsAnalyzed,
    int ComposedDesignsAnalyzed,
    int LoadoutsAnalyzed,
    int ProductionLoadoutsOverCap,
    int ReferenceLoadoutsOverCap,
    int ProductionLoadoutsWithUnspentBudget,
    int ReferenceLoadoutsWithUnspentBudget,
    IReadOnlyList<CraftingCatalogStatConstraintSummary> StatSummaries,
    IReadOnlyList<CraftingCatalogLoadoutConstraintMeasurement> WorstProductionLoadouts);

public sealed record CraftingCatalogStatConstraintSummary(
    AttributeType Attribute,
    int ProductionViolationCount,
    int ReferenceViolationCount,
    double MaximumProductionExcessPoints,
    double MaximumReferenceExcessPoints);

public sealed record CraftingCatalogLoadoutConstraintMeasurement(
    string Id,
    int Tier,
    string ArmorFamily,
    string HandConfiguration,
    string? BlueprintId,
    double TargetBudget,
    double ProductionSpentBudget,
    double ReferenceSpentBudget,
    double ProductionMaximumWastedBudgetPercent,
    double ReferenceMaximumWastedBudgetPercent,
    IReadOnlyList<AttributeType> ProductionAttributesOverCap,
    IReadOnlyList<AttributeType> ReferenceAttributesOverCap);

public sealed record MaximumEquipmentProgressionReport(
    int Tier,
    ItemQuality Quality,
    Rarity Rarity,
    double QualityMultiplier,
    double CraftingVarianceMultiplier,
    int RarityUpgradesPerItem,
    int LoadoutsAnalyzed,
    int LoadoutsOverCap,
    int LoadoutsWithUnspentBudget,
    IReadOnlyList<CraftingCombatPeerComparison> CombatPeers,
    IReadOnlyList<MaximumEquipmentCapSaturationGroup> CapSaturationByAttribute,
    IReadOnlyList<MaximumEquipmentUnspentBudgetGroup> UnspentBudgetByRecipe,
    IReadOnlyList<MaximumEquipmentLoadoutMeasurement> WorstLoadouts);

public sealed record MaximumEquipmentCapSaturationGroup(
    AttributeType Attribute,
    int LoadoutCount,
    double AverageWastedBudgetPercent,
    double MaximumWastedBudgetPercent);

public sealed record MaximumEquipmentUnspentBudgetGroup(
    string RecipeId,
    EquipmentType EquipmentType,
    string? BlueprintId,
    int LoadoutOccurrences,
    double GeneratedUnspentBudget,
    double RarityUnspentBudget,
    double TotalUnspentBudget,
    IReadOnlyList<AttributeType> CappedAttributes,
    IReadOnlyList<AttributeType> BindingCombatCaps);

public sealed record MaximumEquipmentLoadoutMeasurement(
    string Id,
    string ArmorFamily,
    string HandConfiguration,
    string? BlueprintId,
    double TargetBudget,
    double SpentBudget,
    double StaticBaseModifierBudget,
    double GeneratedStatBudget,
    double RarityImprovementBudget,
    double UnspentBudget,
    double MaximumWastedBudgetPercent,
    IReadOnlyList<AttributeType> AttributesOverCap);

public sealed record EquipmentBalanceCalibrationGate(
    double SummonTolerancePercent,
    double HandTolerancePercent,
    double AggregateCapWasteTolerancePercent,
    bool OverflowRedistributionActive,
    bool AggregateCapUtilizationPassed,
    bool CandidateAggregateCapUtilizationPassed,
    bool EqualBudgetPeerMatrixPassed,
    bool SummonCalibrationPassed,
    bool HandCalibrationPassed,
    bool CraftingCombatPeerMatrixPassed,
    bool MaximumEquipmentProgressionAnalyzed,
    bool MaximumEquipmentProgressionPassed,
    bool ActiveProfilePassed,
    IReadOnlyList<string> Blockers);

public sealed record AttributeBalanceFinding(
    AttributeBalanceFindingKind Kind,
    int Tier,
    AttributeType? Attribute,
    string Message);
