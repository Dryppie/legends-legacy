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
    IReadOnlyList<WorldTowerFloorBalanceResult> Floors,
    bool Passed,
    IReadOnlyList<string> Blockers);

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
    IReadOnlyList<WorldTowerRosterBalanceResult> Rosters,
    bool Passed,
    IReadOnlyList<string> Failures);

public enum WorldTowerBalanceRosterKind
{
    Cooperative,
    NoGuardian,
    NoRestorer,
    DamageLight
}

public sealed record WorldTowerRosterBalanceResult(
    string Roster,
    WorldTowerBalanceRosterKind Kind,
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
    double AverageVictorySurvivors,
    double AverageGuardianHealthRemainingPercent,
    IReadOnlyList<string> Profiles,
    WorldTowerCooperationTelemetry Cooperation);

public sealed record WorldTowerCooperationTelemetry(
    double GuardianAttentionSharePercent,
    double RestorerAttentionSharePercent,
    double GuardianThreatGenerated,
    double RestorerThreatGenerated,
    double GuardianIncomingRawDamage,
    double RestorerHealingDone,
    double DamageRedirectedToGuardians,
    double AverageSurvivors,
    IReadOnlyList<WorldTowerPartyCooperationTelemetry> Parties);

public sealed record WorldTowerPartyCooperationTelemetry(
    int PartyNumber,
    int PartySize,
    double GuardianAttentionSharePercent,
    double RestorerAttentionSharePercent,
    double GuardianThreatGenerated,
    double RestorerThreatGenerated,
    double GuardianIncomingRawDamage,
    double RestorerHealingDone,
    double AverageSurvivors);
