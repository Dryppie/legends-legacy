using Application.Interfaces.Services.LL.PowerRatings;

namespace Application.Interfaces.Services.LL.Raids;

public sealed record RaidWingPowerRecommendation(
    int RecommendedPower,
    int LowerRecommendedPower,
    int UpperRecommendedPower);

public sealed record RaidPowerRecommendation(
    string RaidBossId,
    int Tier,
    RaidWingPowerRecommendation Vanguard,
    RaidWingPowerRecommendation Flank,
    RaidWingPowerRecommendation Ward,
    decimal ClearProbability,
    decimal ClearProbabilityLowerBound,
    decimal ClearProbabilityUpperBound,
    PowerRatingConfidence Confidence,
    int SimulationCount,
    string CanonicalRungId,
    DateTimeOffset GeneratedAtUtc);

public sealed record RaidPowerCalibrationIdentity(
    string RaidBossId,
    int Tier,
    string DefinitionHash,
    int RaidRulesVersion,
    int PowerRatingAlgorithmVersion,
    int CombatRulesVersion,
    int EquipmentBalanceVersion,
    int SeedSetVersion);

public sealed record PersistedRaidPowerRecommendation(
    RaidPowerCalibrationIdentity Identity,
    RaidPowerRecommendation Recommendation,
    DateTimeOffset UpdatedAtUtc);

public interface IRaidPowerAnalyzer
{
    RaidPowerCalibrationIdentity GetIdentity(string raidBossId, int tier);
    Task<RaidPowerRecommendation> AnalyzeAsync(string raidBossId, int tier, CancellationToken cancellationToken);
}

public interface IRaidPowerRecommendationRepository
{
    Task<IReadOnlyList<PersistedRaidPowerRecommendation>> GetAllAsync(CancellationToken cancellationToken);
    Task UpsertAsync(PersistedRaidPowerRecommendation recommendation, CancellationToken cancellationToken);
}

public interface IRaidPowerRecommendationStore
{
    bool TryGet(string raidBossId, int tier, out RaidPowerRecommendation recommendation);
    void Publish(IReadOnlyDictionary<string, RaidPowerRecommendation> recommendations);
    void MarkCalibrationComplete();
    bool IsCalibrationComplete { get; }
}
