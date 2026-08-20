namespace Application.Interfaces.Services.LL.Regions;

public interface IAreaCombatSimulator
{
    Task<AreaSimulationOptions> GetOptionsAsync(CancellationToken cancellationToken);
    Task<AreaSimulationReport> RunAsync(
        AreaSimulationRequest request,
        CancellationToken cancellationToken);
    Task<AreaEncounterSimulationReport> RunEncounterAsync(
        AreaEncounterSimulationRequest request,
        CancellationToken cancellationToken);
}

public sealed record AreaSimulationOptions(
    IReadOnlyList<AreaSimulationAreaOption> Areas,
    IReadOnlyList<string> Profiles,
    IReadOnlyList<AreaSimulationBuildOption> Builds,
    IReadOnlyList<AreaSimulationRegionProjection> RegionProjections,
    int MaximumEncounters);

public sealed record AreaSimulationAreaOption(
    string Id,
    string Name,
    string RegionKey,
    int LevelRequirement,
    int GlobalStep,
    int RegionStep,
    int RecommendedCombatRating,
    string ProfileId,
    int TargetWinRateBasisPoints,
    string DefaultBuildId);

public sealed record AreaSimulationBuildOption(
    string Id,
    int Tier,
    string Quality,
    string Rarity);

public sealed record AreaSimulationRegionProjection(
    int RegionNumber,
    int EquipmentTier,
    int EndingCharacterLevel,
    int EssenceCount,
    int RecommendedEndpointCombatRating,
    int MaximumEndpointCombatRating,
    IReadOnlyList<AreaSimulationProfileProjection> Profiles);

public sealed record AreaSimulationProfileProjection(
    string Profile,
    int CombatRating);

public sealed record AreaSimulationRequest(
    string AreaId,
    int EncounterCount,
    int RandomSeed,
    string CharacterProfile,
    string BuildId);

public sealed record AreaSimulationReport(
    string AreaId,
    string AreaName,
    int LevelRequirement,
    string CharacterProfile,
    string BuildId,
    int PlayerMaxHealth,
    int RequestedEncounters,
    int Victories,
    int Defeats,
    int Draws,
    double WinRate,
    double AverageCombatTicks,
    double MedianCombatTicks,
    double P95CombatTicks,
    double AverageDamageTaken,
    double P95DamageTaken,
    decimal TargetExperiencePerHour,
    decimal TargetCindersPerHour,
    decimal EffectiveExperiencePerHour,
    decimal EffectiveCindersPerHour,
    int RandomSeed,
    CreatureScalingProfile Scaling,
    IReadOnlyList<AreaSimulationCompositionResult> Compositions,
    IReadOnlyList<AreaSimulationEncounterResult> Encounters);

public sealed record AreaSimulationCompositionResult(
    string Composition,
    int Attempts,
    int Victories,
    double WinRate,
    double AverageCombatTicks,
    double AverageDamageTaken);

public sealed record AreaSimulationEncounterResult(
    int EncounterNumber,
    int Seed,
    string Outcome,
    int CombatTicks,
    int DamageTaken,
    int RemainingHealth,
    IReadOnlyList<string> Enemies);

public sealed record AreaEncounterSimulationRequest(
    string AreaId,
    Guid CreatureId,
    int EncounterCount,
    int RandomSeed,
    string CharacterProfile,
    string BuildId);

public sealed record AreaEncounterSimulationReport(
    string AreaId,
    Guid CreatureId,
    string CreatureName,
    string CharacterProfile,
    string BuildId,
    int PlayerMaxHealth,
    IReadOnlyList<AreaEncounterSimulationAttempt> Attempts);

public sealed record AreaEncounterSimulationAttempt(
    int EncounterNumber,
    int Seed,
    string Outcome,
    int CombatTicks,
    int DamageTaken,
    int HealingDone,
    int HealthRegenerated,
    int RemainingHealth,
    int EnemyRemainingHealth);
