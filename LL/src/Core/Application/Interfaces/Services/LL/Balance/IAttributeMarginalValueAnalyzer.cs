using Domain.Models.Attributes;

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
    SummonOffense
}

public enum AttributeBalanceFindingKind
{
    Inert,
    Overvalued,
    Undervalued,
    CapLimited,
    EqualBudgetMismatch
}

public sealed record AttributeBalanceAnalysisReport(
    int BalanceVersion,
    int CombatRulesVersion,
    IReadOnlyList<int> Tiers,
    IReadOnlyList<int> Seeds,
    double MarginalBudgetFraction,
    IReadOnlyList<AttributeMarginalValueMeasurement> Measurements,
    IReadOnlyList<EqualBudgetAttributeComparison> EqualBudgetComparisons,
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

public sealed record EqualBudgetAttributeComparison(
    int Tier,
    AttributeBalanceScenario Scenario,
    AttributeType FirstAttribute,
    AttributeType SecondAttribute,
    double Budget,
    double FirstRelativeGainPercent,
    double SecondRelativeGainPercent,
    double DifferencePercentagePoints);

public sealed record AttributeBalanceFinding(
    AttributeBalanceFindingKind Kind,
    int Tier,
    AttributeType? Attribute,
    string Message);
