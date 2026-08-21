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
    double AttackSpeedBonus,
    double PenetrationBonus,
    double SoftDefenseBonus,
    double CritChanceBonus,
    double CritDamageBonus,
    float CritChanceCap,
    float CritDamageCap);

public sealed record RegionCombatBalanceCatalog(
    int Version,
    CombatProgressionFoundation Foundation,
    IReadOnlyList<RegionCombatBalanceProfile> Profiles,
    IReadOnlyList<RegionCombatBalanceRegion> Regions,
    string FallbackProfileId = "unified-global-v1");

public sealed record CombatProgressionFoundation(
    int AreasPerRegion,
    double AreaGrowth,
    double RegionJump);

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
    double Exponent,
    int? LinearAfterStep = null,
    double? LinearGrowthPerStep = null);

public sealed record RegionCombatBalanceRegion(
    string RegionKey,
    string ProfileId,
    int StartingGlobalStep,
    int StartingCombatRating,
    int EndingCombatRating,
    IReadOnlyList<string> AreaIds,
    IReadOnlyList<string> DefaultBuildIds);
