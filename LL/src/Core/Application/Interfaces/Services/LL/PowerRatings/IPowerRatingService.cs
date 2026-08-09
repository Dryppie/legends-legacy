using Domain.Models.Dungeons.Definitions;

namespace Application.Interfaces.Services.LL.PowerRatings;

public static class PowerRatingAlgorithm
{
    public const int Version = 23;
    public const int CombatRulesVersion = 11;
    // Retained under its existing name for persistence compatibility. It now
    // versions the deterministic Combat Rating definition, not a benchmark.
    public const int BenchmarkDefinitionVersion = 14;
    public const int RatingSeedSetVersion = 1;
    public const int DungeonSeedSetVersion = 2;
    public const int RecommendationSeedSetVersion = 2;
}

public enum PowerRatingConfidence
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum PowerAnalysisState
{
    Available = 0,
    Unsupported = 1,
    InsufficientCombatData = 2,
    LowConfidence = 3,
    CalculationFailed = 4
}

public sealed record PowerRatingSnapshot(
    int AlgorithmVersion,
    string BuildFingerprint,
    int Overall,
    int SingleTargetOffense,
    int MultiTargetOffense,
    int PhysicalDurability,
    int MagicalDurability,
    int Sustain,
    int ControlUtility,
    DateTimeOffset ComputedAtUtc,
    PowerRatingConfidence Confidence,
    PowerAnalysisState State,
    string? StatusMessage = null);

public sealed record OverallPowerRating(
    int Overall,
    PowerAnalysisState State,
    string? StatusMessage = null);

public sealed record DungeonPartySelection(IReadOnlyList<Guid> CompanionIds)
{
    public static DungeonPartySelection Solo { get; } = new([]);
}

public interface IPowerRatingService
{
    Task<OverallPowerRating> GetCharacterOverallRatingAsync(
        Guid characterId,
        CancellationToken cancellationToken);

    Task<PowerRatingSnapshot> GetCharacterRatingAsync(
        Guid characterId,
        CancellationToken cancellationToken);

    Task<PowerRatingSnapshot> GetPartyRatingAsync(
        Guid characterId,
        DungeonPartySelection partySelection,
        CancellationToken cancellationToken);
}

public sealed record PowerRequirementProfile(
    decimal SingleTarget,
    decimal AreaDamage,
    decimal PhysicalDurability,
    decimal MagicalDurability,
    decimal Sustain,
    decimal Control,
    decimal BossBurst,
    decimal Attrition);

public sealed record DungeonPowerRecommendation(
    int RecommendedPartyPower,
    int LowerRecommendedPower,
    int UpperRecommendedPower,
    PowerRequirementProfile Requirements,
    int AlgorithmVersion,
    string DungeonContentHash,
    PowerRatingConfidence Confidence,
    PowerAnalysisState State,
    int SimulationCount,
    TimeSpan EstimatedRunDuration,
    IReadOnlyDictionary<string, decimal> CanonicalPartyCompletionRates,
    string? StatusMessage = null);

public interface IDungeonPowerAnalyzer
{
    DungeonPowerCalibrationIdentity GetCalibrationIdentity(string dungeonId);

    Task<DungeonPowerRecommendation> AnalyzeDungeonAsync(
        string dungeonId,
        DungeonTier tier,
        CancellationToken cancellationToken);
}

public sealed record DungeonPowerCalibrationIdentity(
    string DungeonId,
    int DungeonTier,
    string DungeonContentHash,
    int AlgorithmVersion,
    int CombatRulesVersion,
    int BenchmarkDefinitionVersion,
    int RecommendationSeedSetVersion,
    int EquipmentBalanceVersion);

public sealed record PersistedDungeonPowerRecommendation(
    DungeonPowerCalibrationIdentity Identity,
    DungeonPowerRecommendation Recommendation,
    DateTimeOffset UpdatedAtUtc);

public interface IDungeonPowerRecommendationRepository
{
    Task<IReadOnlyList<PersistedDungeonPowerRecommendation>> GetAllAsync(
        CancellationToken cancellationToken);

    Task UpsertAsync(
        PersistedDungeonPowerRecommendation recommendation,
        CancellationToken cancellationToken);
}

public interface IDungeonPowerRecommendationStore
{
    bool IsCalibrationComplete { get; }
    bool TryGet(string dungeonId, out DungeonPowerRecommendation recommendation);
    IReadOnlyDictionary<string, DungeonPowerRecommendation> GetAll();
    void MarkCalibrationComplete();
    void Publish(IReadOnlyDictionary<string, DungeonPowerRecommendation> recommendations);
    bool Remove(string dungeonId);
    void Set(string dungeonId, DungeonPowerRecommendation recommendation);
}

public enum DungeonReadinessBand
{
    VeryUnlikely = 0,
    Risky = 1,
    Uncertain = 2,
    Favored = 3,
    Comfortable = 4
}

public sealed record ReadinessInsight(string Code, string Message, decimal Severity);

public sealed record DungeonReadinessResult(
    PowerRatingSnapshot PartyPower,
    DungeonPowerRecommendation Recommendation,
    DungeonReadinessBand Band,
    decimal EstimatedCompletionProbability,
    decimal CompletionProbabilityLowerBound,
    decimal CompletionProbabilityUpperBound,
    decimal? CheckpointReachProbability,
    IReadOnlyList<ReadinessInsight> Strengths,
    IReadOnlyList<ReadinessInsight> Weaknesses,
    int SimulationCount,
    PowerRatingConfidence Confidence,
    PowerAnalysisState State,
    string? StatusMessage = null);

public interface IDungeonReadinessService
{
    Task<DungeonReadinessResult> AnalyzeAsync(
        Guid characterId,
        string dungeonId,
        DungeonTier tier,
        DungeonPartySelection partySelection,
        CancellationToken cancellationToken);
}

public sealed record DungeonPowerDiagnostic(
    string DungeonId,
    string DungeonName,
    int Tier,
    DungeonPowerRecommendation Recommendation,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);

public interface IPowerAnalysisDiagnostics
{
    Task<IReadOnlyList<DungeonPowerDiagnostic>> AnalyzeAllDungeonsAsync(
        CancellationToken cancellationToken);
}

public interface IPowerPredictionTelemetryBuffer
{
    void Record(Guid characterId, string dungeonId, DungeonReadinessResult result);
    bool TryTake(Guid characterId, string dungeonId, out DungeonReadinessResult result);
}
