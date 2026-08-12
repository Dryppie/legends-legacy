using Domain.Models.WorldTower;

namespace Application.Interfaces.Services.LL.WorldTower;

public interface IWorldTowerBalanceAnalyzer
{
    Task<WorldTowerBalanceReport> AnalyzeAsync(
        WorldTowerBalanceRequest request,
        CancellationToken cancellationToken);
}

public sealed record WorldTowerBalanceRequest(
    int? FloorNumber,
    int AttemptsPerRoster,
    int RandomSeed,
    WorldTowerBalanceLoadout? Loadout = null);

public sealed record WorldTowerBalanceLoadout(
    int CharacterLevel,
    string EquipmentRarity,
    int EssenceCount);

public sealed record WorldTowerBalanceReport(
    int AttemptsPerRoster,
    int RandomSeed,
    bool UsesTierOneOnly,
    IReadOnlyList<WorldTowerFloorBalanceResult> Floors);

public sealed record WorldTowerFloorBalanceResult(
    int FloorNumber,
    string FloorName,
    int RequiredSlots,
    int CharacterLevel,
    int EquipmentTier,
    string EquipmentRarity,
    int EssenceCount,
    int RecommendedPowerRating,
    int CanonicalAveragePowerRating,
    TowerGuardianScalingDefinition GuardianScaling,
    IReadOnlyList<WorldTowerRosterBalanceResult> Rosters);

public sealed record WorldTowerRosterBalanceResult(
    string Roster,
    int Attempts,
    int Victories,
    int Defeats,
    int Draws,
    double WinRate,
    double WinRateLower95,
    double WinRateUpper95,
    double MedianVictoryTicks,
    double P95VictoryTicks,
    double AverageSurvivors,
    double AverageGuardianHealthRemainingPercent,
    IReadOnlyList<string> Profiles);
