using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegendsLegacy.Balance;

public enum EliteCertificationProfile
{
    Developer,
    Release
}

public enum EliteCertificationVerdict
{
    CertifiedElite,
    DeveloperProfileOnly,
    SearchUnstable,
    LocalImprovementFound,
    ScenarioCoverageFailure,
    PartyOptimizationRequired,
    HumanBuildOutperformed,
    InsufficientPlayerEvidence
}

public sealed record EliteCertificationPolicy(
    string PolicyId,
    int PolicyVersion,
    double RestartBestScoreSpreadTolerance,
    double CrossStrategyScoreTolerance,
    double PlateauImprovementTolerance,
    int DeveloperPlateauGenerations,
    int ReleasePlateauGenerations,
    double GenericLocalImprovementTolerance,
    double EncounterClearRateImprovementTolerance,
    double EncounterKillTimeImprovementTolerance,
    double HoldoutMaximumIntervalWidth,
    double HumanBenchmarkAdvantageTolerance,
    double HumanClearRateAdvantageTolerance,
    double HumanKillTimeAdvantageTolerance,
    double P95MinimumConfidenceLowerBound,
    double P99MinimumConfidenceLowerBound,
    double P95KillTimeRatioWarning,
    double P99KillTimeRatioWarning,
    double SpecializedKillTimeRatioWarning,
    double MechanicBypassKillTimeRatio,
    int MinimumCuratedBuildsPerSlotProfile,
    int MinimumCuratedPartiesPerEncounter)
{
    public static EliteCertificationPolicy V1 { get; } = new(
        "WorldTowerEliteCertificationV1",
        1,
        0.50,
        1.00,
        0.25,
        4,
        10,
        1.00,
        0.03,
        0.05,
        0.05,
        2.00,
        0.03,
        0.05,
        0.80,
        0.90,
        0.70,
        0.55,
        0.45,
        0.35,
        3,
        1);

    public EliteCertificationPolicy Validate()
    {
        if (string.IsNullOrWhiteSpace(PolicyId))
            throw new InvalidOperationException("Elite certification policy ID is required.");
        if (PolicyVersion < 1)
            throw new InvalidOperationException("Elite certification policy version must be positive.");
        ValidateNonNegative(RestartBestScoreSpreadTolerance, nameof(RestartBestScoreSpreadTolerance));
        ValidateNonNegative(CrossStrategyScoreTolerance, nameof(CrossStrategyScoreTolerance));
        ValidateNonNegative(PlateauImprovementTolerance, nameof(PlateauImprovementTolerance));
        ValidatePositive(GenericLocalImprovementTolerance, nameof(GenericLocalImprovementTolerance));
        ValidateRate(EncounterClearRateImprovementTolerance, nameof(EncounterClearRateImprovementTolerance));
        ValidateRate(EncounterKillTimeImprovementTolerance, nameof(EncounterKillTimeImprovementTolerance));
        ValidateRate(HoldoutMaximumIntervalWidth, nameof(HoldoutMaximumIntervalWidth));
        ValidateNonNegative(HumanBenchmarkAdvantageTolerance, nameof(HumanBenchmarkAdvantageTolerance));
        ValidateRate(HumanClearRateAdvantageTolerance, nameof(HumanClearRateAdvantageTolerance));
        ValidateRate(HumanKillTimeAdvantageTolerance, nameof(HumanKillTimeAdvantageTolerance));
        ValidateRate(P95MinimumConfidenceLowerBound, nameof(P95MinimumConfidenceLowerBound));
        ValidateRate(P99MinimumConfidenceLowerBound, nameof(P99MinimumConfidenceLowerBound));
        ValidateRate(P95KillTimeRatioWarning, nameof(P95KillTimeRatioWarning));
        ValidateRate(P99KillTimeRatioWarning, nameof(P99KillTimeRatioWarning));
        ValidateRate(SpecializedKillTimeRatioWarning, nameof(SpecializedKillTimeRatioWarning));
        ValidateRate(MechanicBypassKillTimeRatio, nameof(MechanicBypassKillTimeRatio));
        if (DeveloperPlateauGenerations < 1 || ReleasePlateauGenerations < DeveloperPlateauGenerations)
            throw new InvalidOperationException("Elite certification plateau generation requirements are invalid.");
        if (MinimumCuratedBuildsPerSlotProfile < 1 || MinimumCuratedPartiesPerEncounter < 1)
            throw new InvalidOperationException("Elite certification curated evidence minimums must be positive.");
        return this;
    }

    public string CreateFingerprint()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    public static EliteCertificationPolicy Load(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Elite certification policy file was not found.", fullPath);
        return (JsonSerializer.Deserialize<EliteCertificationPolicy>(File.ReadAllText(fullPath), JsonOptions)
                ?? throw new InvalidOperationException("Elite certification policy JSON was empty."))
            .Validate();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private static void ValidateRate(double value, string name)
    {
        if (!double.IsFinite(value) || value is <= 0 or > 1)
            throw new InvalidOperationException($"Elite certification policy '{name}' must be in (0, 1].");
    }

    private static void ValidatePositive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new InvalidOperationException($"Elite certification policy '{name}' must be positive.");
    }

    private static void ValidateNonNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0)
            throw new InvalidOperationException($"Elite certification policy '{name}' must not be negative.");
    }
}

public sealed record EliteCertificationOptions(
    EliteCertificationProfile Profile = EliteCertificationProfile.Developer,
    int RestartCount = 3,
    int PopulationSize = 64,
    int Generations = 12,
    int EliteCount = 8,
    int FinalistsPerSlotProfile = 6,
    int LocalSwapDepth = 2,
    int TwoSwapChallengerLimitPerFinalist = 250,
    int HoldoutSeeds = 4,
    int SimulationsPerSeed = 25,
    int PartyGenomeBudgetPerFloor = 2_000,
    double MutationRate = 0.25,
    double RandomInjectionRate = 0.10,
    double DiversityPenalty = 8,
    string? TopPlayerBuildsPath = null,
    int MaximumGenerations = 24,
    int RestartLocalRefinementPassLimit = 6,
    int FinalistRefinementRoundLimit = 3)
{
    public static EliteCertificationOptions ForProfile(EliteCertificationProfile profile) =>
        profile == EliteCertificationProfile.Release
            ? new EliteCertificationOptions(
                Profile: profile,
                RestartCount: 8,
                PopulationSize: 256,
                Generations: 60,
                EliteCount: 32,
                FinalistsPerSlotProfile: 12,
                LocalSwapDepth: 2,
                TwoSwapChallengerLimitPerFinalist: 0,
                HoldoutSeeds: 8,
                SimulationsPerSeed: 200,
                PartyGenomeBudgetPerFloor: 25_000,
                MaximumGenerations: 100,
                RestartLocalRefinementPassLimit: 12,
                FinalistRefinementRoundLimit: 5)
            : new EliteCertificationOptions();

    public EliteCertificationOptions Validate()
    {
        if (RestartCount is < 2 or > 32)
            throw new ArgumentOutOfRangeException(nameof(RestartCount), "Elite restart count must be between 2 and 32.");
        if (PopulationSize is < 4 or > 500)
            throw new ArgumentOutOfRangeException(nameof(PopulationSize), "Elite population must be between 4 and 500.");
        if (Generations is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(Generations), "Elite generation count must be between 1 and 100.");
        if (MaximumGenerations < Generations || MaximumGenerations > 100)
            throw new ArgumentOutOfRangeException(nameof(MaximumGenerations), "Elite maximum generations must be between the minimum generation count and 100.");
        if (RestartLocalRefinementPassLimit is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(RestartLocalRefinementPassLimit));
        if (FinalistRefinementRoundLimit is < 0 or > 20)
            throw new ArgumentOutOfRangeException(nameof(FinalistRefinementRoundLimit));
        if (EliteCount < 1 || EliteCount >= PopulationSize)
            throw new ArgumentOutOfRangeException(nameof(EliteCount), "Elite count must be positive and below population size.");
        if (FinalistsPerSlotProfile is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(FinalistsPerSlotProfile), "Elite finalist count must be between 1 and 50.");
        if (LocalSwapDepth is < 1 or > 2)
            throw new ArgumentOutOfRangeException(nameof(LocalSwapDepth), "Elite local swap depth must be 1 or 2.");
        if (TwoSwapChallengerLimitPerFinalist is < 0 or > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(TwoSwapChallengerLimitPerFinalist));
        if (HoldoutSeeds is < 2 or > 50)
            throw new ArgumentOutOfRangeException(nameof(HoldoutSeeds), "Elite holdout seeds must be between 2 and 50.");
        if (SimulationsPerSeed is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(SimulationsPerSeed), "Elite simulations per seed must be between 1 and 1,000.");
        if (PartyGenomeBudgetPerFloor is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(PartyGenomeBudgetPerFloor));
        if (MutationRate is < 0.01 or > 1 || RandomInjectionRate is < 0 or > 0.5)
            throw new ArgumentOutOfRangeException(nameof(MutationRate), "Elite mutation and injection rates are invalid.");
        if (DiversityPenalty is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(DiversityPenalty));
        return this;
    }
}

public sealed record TopPlayerBuildFixture(
    string Id,
    string SourceCategory,
    DateOnly ReviewDate,
    int SlotCount,
    IReadOnlyList<string> EssenceIds,
    string GearPackageId,
    int CharacterLevel,
    string ProgressionState,
    string IntendedRole,
    int? EncounterFloor,
    string? ObservedResult,
    string ReviewerNote);

public sealed record TopPlayerPartyFixture(
    string Id,
    string SourceCategory,
    DateOnly ReviewDate,
    int EncounterFloor,
    IReadOnlyList<string> BuildIds,
    string? ObservedResult,
    string ReviewerNote);

public sealed record TopPlayerFixtureDocument(
    int SchemaVersion,
    string? ContentFingerprint,
    IReadOnlyList<TopPlayerBuildFixture> Builds,
    IReadOnlyList<TopPlayerPartyFixture> Parties)
{
    public static TopPlayerFixtureDocument Empty { get; } = new(1, null, [], []);

    public static TopPlayerFixtureDocument Load(string? path, string expectedContentFingerprint)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Empty;
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Top-player build fixture file was not found.", fullPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var document = JsonSerializer.Deserialize<TopPlayerFixtureDocument>(File.ReadAllText(fullPath), options)
                       ?? throw new InvalidOperationException("Top-player fixture JSON was empty.");
        if (document.SchemaVersion != 1)
            throw new InvalidOperationException($"Unsupported top-player fixture schema {document.SchemaVersion}.");
        if ((document.Builds.Count > 0 || document.Parties.Count > 0)
            && !string.Equals(document.ContentFingerprint, expectedContentFingerprint, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Top-player fixture fingerprint '{document.ContentFingerprint ?? "missing"}' does not match current content '{expectedContentFingerprint}'.");
        }
        return document;
    }
}
