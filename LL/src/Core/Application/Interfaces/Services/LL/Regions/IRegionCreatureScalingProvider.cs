using Domain.Models.Regions.Areas;

namespace Application.Interfaces.Services.LL.Regions;

public interface IRegionCreatureScalingProvider
{
    CreatureScalingProfile GetScaling(Area area);
    RegionCombatBalanceCatalog GetCatalog();
}

public sealed record CreatureScalingProfile(
    string ProfileId,
    string? RegionKey,
    int GlobalStep,
    int? RegionStep,
    int ProgressionStep,
    int? RecommendedCombatRating,
    double HealthMultiplier,
    double OffenseMultiplier,
    double DefenseMultiplier,
    double ResistanceMultiplier,
    double AttackSpeedMultiplier,
    double PenetrationMultiplier,
    double SoftDefenseMultiplier,
    double CritChanceBonus,
    double CritDamageBonus,
    float CritChanceCap,
    float CritDamageCap);

public sealed record RegionCombatBalanceCatalog(
    int Version,
    IReadOnlyList<RegionCombatBalanceProfile> Profiles,
    IReadOnlyList<RegionCombatBalanceRegion> Regions);

public sealed record RegionCombatBalanceProfile(
    string Id,
    int TargetWinRateBasisPoints,
    RegionCombatGrowthCurve HealthCurve,
    RegionCombatGrowthCurve OffenseCurve,
    RegionCombatGrowthCurve DefenseCurve,
    RegionCombatGrowthCurve ResistanceCurve,
    double AttackSpeedGrowthPerStep,
    double PenetrationGrowthPerStep,
    double SoftDefenseGrowthPerStep,
    double CritChancePerStep,
    double CritDamagePerStep,
    float CritChanceCap,
    float CritDamageCap,
    double MaximumStepIncrease);

public sealed record RegionCombatGrowthCurve(
    string Model,
    double BaseMultiplier,
    double GrowthPerStep,
    double Exponent);

public sealed record RegionCombatBalanceRegion(
    string RegionKey,
    string ProfileId,
    int StartingGlobalStep,
    int StartingCombatRating,
    int EndingCombatRating,
    IReadOnlyList<string> AreaIds,
    IReadOnlyList<string> DefaultBuildIds);
