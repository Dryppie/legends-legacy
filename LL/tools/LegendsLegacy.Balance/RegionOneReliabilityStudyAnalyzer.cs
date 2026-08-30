using Services.LL.Combat.Engine;
using Domain.Models.Combat.Abilities;

namespace LegendsLegacy.Balance;

public enum RegionOneReliabilityVerdict
{
    Disabled = 0,
    Pass = 1,
    Fail = 2,
    Inconclusive = 3,
    Unavailable = 4
}

public enum RegionOneReliabilityFaultKind
{
    Health = 0,
    Offense = 1,
    Regeneration = 2,
    AddPressure = 3,
    DistributedAttrition = 4
}

public enum RegionOneReliabilityRecoveryMethod
{
    None = 0,
    DominantFailureMode = 1,
    PairedPhysicalTelemetry = 2,
    ObservedFailureMode = 3
}

public enum RegionOneReliabilityFamilyContractVerdict
{
    NotApplicable = 0,
    Pass = 1,
    Inconclusive = 2,
    InsufficientEvidence = 3
}

public sealed record RegionOneReliabilityStudyOptions
{
    public bool Enabled { get; init; }
    public int RostersPerFamily { get; init; } = 3;
    public int SimulationsPerRoster { get; init; } = 10;
    public int HealthOffenseFloor { get; init; } = 1;
    public int RegenerationFloor { get; init; } = 7;
    public int AddPressureFloor { get; init; } = 3;
    public double MinimumReferenceClearRate { get; init; } = 0.40;
    public double MaximumReferenceClearRate { get; init; } = 0.80;
    public double TargetReferenceClearRate { get; init; } = 0.60;
    public double FaultMultiplier { get; init; } = 1.40;
    public double MinimumClearRateDrop { get; init; } = 0.10;
    public double MinimumDominantFailureShare { get; init; } = 0.60;
    public double MinimumOffenseDamagePerSecondRatio { get; init; } = 1.10;
    public double MaximumHealthDamagePerSecondDeviation { get; init; } = 0.10;
    public double MinimumHealthRemainingRatioIncrease { get; init; } = 0.10;
    public double MinimumGuardianSelfSustainTelemetryRatio { get; init; } = 1.10;
    public double MinimumAddPressurePeakHostileRatio { get; init; } = 1.50;
    public double MinimumDistributedDamagePerSecondRatio { get; init; } = 1.10;
    public double MinimumAddPressureWindowResetAdvantage { get; init; } = 0.10;
    public double MaximumAddPressureDoseResponseReversal { get; init; } = 0.05;
    public int MaximumReferenceRefinementIterations { get; init; } = 8;
    public bool ProgressionFidelityEnabled { get; init; } = true;
    public IReadOnlyList<int> ProgressionFidelityFloors { get; init; } = [3, 4, 5, 6, 7, 8];
    public double ProgressionFidelityMaterialClearRateDifference { get; init; } = 0.15;
    public double ProgressionFidelityMaterialDurationRatioDifference { get; init; } = 0.10;
    public IReadOnlyList<double> AddPressurePayloadDoseMultipliers { get; init; } = [0.25, 0.50, 0.75, 1.00];
    public IReadOnlyList<double> MechanicDoseFractions { get; init; } = [0.25, 0.50, 0.75, 1.00];
    public IReadOnlyList<double> ReferenceDifficultyFactors { get; init; } =
        [1.00, 0.85, 0.70, 0.60, 0.55, 0.50, 0.45, 0.40, 0.35, 0.30, 0.25];

    public RegionOneReliabilityStudyOptions Validate()
    {
        if (RostersPerFamily is < 1 or > 15)
            throw new ArgumentOutOfRangeException(nameof(RostersPerFamily));
        if (SimulationsPerRoster is < 5 or > 100)
            throw new ArgumentOutOfRangeException(nameof(SimulationsPerRoster));
        if (HealthOffenseFloor < 1 || RegenerationFloor < 1 || AddPressureFloor < 1
            || new[] { HealthOffenseFloor, RegenerationFloor, AddPressureFloor }.Distinct().Count() != 3)
            throw new ArgumentOutOfRangeException(nameof(HealthOffenseFloor));
        if (!double.IsFinite(MinimumReferenceClearRate)
            || !double.IsFinite(MaximumReferenceClearRate)
            || !double.IsFinite(TargetReferenceClearRate)
            || MinimumReferenceClearRate is < 0 or > 1
            || MaximumReferenceClearRate is < 0 or > 1
            || MinimumReferenceClearRate >= MaximumReferenceClearRate
            || TargetReferenceClearRate < MinimumReferenceClearRate
            || TargetReferenceClearRate > MaximumReferenceClearRate)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumReferenceClearRate));
        }
        if (!double.IsFinite(FaultMultiplier) || FaultMultiplier is <= 1 or > 2)
            throw new ArgumentOutOfRangeException(nameof(FaultMultiplier));
        if (!double.IsFinite(MinimumClearRateDrop) || MinimumClearRateDrop is <= 0 or > 0.50)
            throw new ArgumentOutOfRangeException(nameof(MinimumClearRateDrop));
        if (!double.IsFinite(MinimumDominantFailureShare) || MinimumDominantFailureShare is < 0.50 or > 1)
            throw new ArgumentOutOfRangeException(nameof(MinimumDominantFailureShare));
        if (!double.IsFinite(MinimumOffenseDamagePerSecondRatio)
            || MinimumOffenseDamagePerSecondRatio is <= 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumOffenseDamagePerSecondRatio));
        }
        if (!double.IsFinite(MaximumHealthDamagePerSecondDeviation)
            || MaximumHealthDamagePerSecondDeviation is <= 0 or > 0.25)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumHealthDamagePerSecondDeviation));
        }
        if (!double.IsFinite(MinimumHealthRemainingRatioIncrease)
            || MinimumHealthRemainingRatioIncrease is <= 0 or > 0.50)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumHealthRemainingRatioIncrease));
        }
        if (!double.IsFinite(MinimumGuardianSelfSustainTelemetryRatio)
            || MinimumGuardianSelfSustainTelemetryRatio is <= 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumGuardianSelfSustainTelemetryRatio));
        }
        if (!double.IsFinite(MinimumAddPressurePeakHostileRatio)
            || MinimumAddPressurePeakHostileRatio is <= 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumAddPressurePeakHostileRatio));
        }
        if (!double.IsFinite(MinimumDistributedDamagePerSecondRatio)
            || MinimumDistributedDamagePerSecondRatio is <= 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumDistributedDamagePerSecondRatio));
        }
        if (!double.IsFinite(MinimumAddPressureWindowResetAdvantage)
            || MinimumAddPressureWindowResetAdvantage is < 0 or > 0.50)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumAddPressureWindowResetAdvantage));
        }
        if (!double.IsFinite(MaximumAddPressureDoseResponseReversal)
            || MaximumAddPressureDoseResponseReversal is < 0 or > 0.25)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumAddPressureDoseResponseReversal));
        }
        if (MaximumReferenceRefinementIterations is < 0 or > 12)
            throw new ArgumentOutOfRangeException(nameof(MaximumReferenceRefinementIterations));
        if (ProgressionFidelityFloors.Count == 0
            || ProgressionFidelityFloors.Any(floor => floor is < 1 or > 10)
            || ProgressionFidelityFloors.Distinct().Count() != ProgressionFidelityFloors.Count)
        {
            throw new ArgumentException(
                "Progression-fidelity floors must be a unique non-empty Region 1 floor set.",
                nameof(ProgressionFidelityFloors));
        }
        if (!double.IsFinite(ProgressionFidelityMaterialClearRateDifference)
            || ProgressionFidelityMaterialClearRateDifference is <= 0 or > 0.50)
        {
            throw new ArgumentOutOfRangeException(nameof(ProgressionFidelityMaterialClearRateDifference));
        }
        if (!double.IsFinite(ProgressionFidelityMaterialDurationRatioDifference)
            || ProgressionFidelityMaterialDurationRatioDifference is <= 0 or > 0.50)
        {
            throw new ArgumentOutOfRangeException(nameof(ProgressionFidelityMaterialDurationRatioDifference));
        }
        if (AddPressurePayloadDoseMultipliers.Count == 0
            || AddPressurePayloadDoseMultipliers.Any(value => !double.IsFinite(value) || value is < 0.25 or > 1)
            || AddPressurePayloadDoseMultipliers.Distinct().Count() != AddPressurePayloadDoseMultipliers.Count
            || !AddPressurePayloadDoseMultipliers.SequenceEqual(AddPressurePayloadDoseMultipliers.OrderBy(value => value))
            || AddPressurePayloadDoseMultipliers[^1] != 1)
        {
            throw new ArgumentException(
                "Add-pressure payload doses must be unique, ascending values between 0.25 and 1 and include 1 as the final dose.",
                nameof(AddPressurePayloadDoseMultipliers));
        }
        if (MechanicDoseFractions.Count == 0
            || MechanicDoseFractions.Any(value => !double.IsFinite(value) || value is < 0.25 or > 1)
            || MechanicDoseFractions.Distinct().Count() != MechanicDoseFractions.Count
            || !MechanicDoseFractions.SequenceEqual(MechanicDoseFractions.OrderBy(value => value))
            || MechanicDoseFractions[^1] != 1)
        {
            throw new ArgumentException(
                "Mechanic dose fractions must be unique, ascending values between 0.25 and 1 and include 1 as the final dose.",
                nameof(MechanicDoseFractions));
        }
        if (ReferenceDifficultyFactors.Count == 0
            || ReferenceDifficultyFactors.Any(value => !double.IsFinite(value) || value is < 0.25 or > 1)
            || ReferenceDifficultyFactors.Distinct().Count() != ReferenceDifficultyFactors.Count)
        {
            throw new ArgumentException(
                "Reference difficulty factors must be a unique non-empty set between 0.25 and 1.",
                nameof(ReferenceDifficultyFactors));
        }
        return this;
    }
}

public sealed record RegionOneReliabilityRosterEvidenceSnapshot(
    string Signature,
    int TrialCount,
    int ClearCount,
    double ClearRate,
    IReadOnlyList<WorldTowerTrialSnapshot> Trials);

public sealed record RegionOneReliabilityFamilyEvidenceSnapshot(
    PartyFamilyKind Family,
    int PartyCount,
    int TrialCount,
    int ClearCount,
    double ClearRate,
    int AdditionalHostileSpawnTrialCount,
    int AdditionalHostileClearTrialCount,
    double? AdditionalHostileClearRate,
    double? AverageAdditionalHostileClearDurationTicks,
    double AverageHostileSummonsCreated,
    double AverageHostileSummonWaveCount,
    double? AverageHostileSummonsPerWave,
    double? AverageHostileSummonWaveIntervalTicks,
    double AverageAdditionalHostileWindowCount,
    double AverageClearedAdditionalHostileWindowCount,
    double AverageHostileSummonActiveTicks,
    double AverageHostileSummonUptimeRatio,
    double AveragePeakHostileSummons,
    PartyFamilyUncertaintySnapshot Uncertainty,
    IReadOnlyDictionary<WorldTowerObservedFailureMode, int> PrimaryObservedFailureModeCounts,
    IReadOnlyList<RegionOneReliabilityRosterEvidenceSnapshot> Rosters);

public sealed record RegionOneReliabilityReferenceCandidateSnapshot(
    double DifficultyFactor,
    int TrialCount,
    double IntendedBalancedClearRate,
    double RosterConfidenceLowerBound,
    double RosterConfidenceUpperBound,
    bool InsideReferenceWindow);

public sealed record RegionOneReliabilityReferenceSnapshot(
    int Floor,
    string EncounterName,
    double? SelectedDifficultyFactor,
    RegionOneReliabilityVerdict Verdict,
    IReadOnlyList<RegionOneReliabilityReferenceCandidateSnapshot> Candidates,
    IReadOnlyList<RegionOneReliabilityFamilyEvidenceSnapshot> Families,
    IReadOnlyList<string> Warnings);

public sealed record RegionOneReliabilityPhysicalComparisonSnapshot(
    double ReferenceAverageHostileDamagePerSecond,
    double FaultAverageHostileDamagePerSecond,
    double HostileDamagePerSecondRatio,
    double ReferenceAverageDurationTicks,
    double FaultAverageDurationTicks,
    double DurationRatio,
    double ReferenceAverageGuardianHealthRemainingRatio,
    double FaultAverageGuardianHealthRemainingRatio,
    double GuardianHealthRemainingRatioIncrease,
    double ReferenceAverageGuardianPassiveRegeneration,
    double FaultAverageGuardianPassiveRegeneration,
    double? GuardianPassiveRegenerationRatio,
    double ReferenceAverageGuardianAbilityHealing,
    double FaultAverageGuardianAbilityHealing,
    double? GuardianAbilityHealingRatio,
    double ReferenceAverageGuardianSelfSustainPerSecond,
    double FaultAverageGuardianSelfSustainPerSecond,
    double? GuardianSelfSustainPerSecondRatio,
    double ReferenceAveragePeakAdditionalHostiles,
    double FaultAveragePeakAdditionalHostiles,
    double? PeakAdditionalHostilesRatio,
    double ReferenceAverageFinalAdditionalHostiles,
    double FaultAverageFinalAdditionalHostiles,
    double ReferenceAverageNonPrimaryFriendlyDamageTakenPerSecond,
    double FaultAverageNonPrimaryFriendlyDamageTakenPerSecond,
    double? NonPrimaryFriendlyDamageTakenPerSecondRatio,
    double ReferenceAverageFriendlyDamageTakenConcentration,
    double FaultAverageFriendlyDamageTakenConcentration,
    double FriendlyDamageTakenConcentrationChange)
{
    public double FaultAverageInjectedDistributedDamagePerSecond { get; init; }
    public double FaultAverageInjectedDistributedDamageHits { get; init; }
    public double FaultAverageInjectedDistributedDamageWaves { get; init; }
    public double FaultAverageInjectedDistributedDamagePeakTargetsPerWave { get; init; }
}

public sealed record RegionOneReliabilityFamilyResponseSnapshot(
    bool Applicable,
    PartyFamilyKind? ExpectedAdvantagedFamily,
    double? ReferenceAdvantageOverIntended,
    double? FaultAdvantageOverIntended,
    double? AdvantageDelta,
    double? ReferenceDefensiveAdvantageOverIntended,
    double? FaultDefensiveAdvantageOverIntended,
    double? DefensiveAdvantageDelta,
    bool? Matched,
    string Assessment)
{
    public static RegionOneReliabilityFamilyResponseSnapshot NotApplicable { get; } = new(
        false, null, null, null, null, null, null, null, null,
        "No reviewed directional family-response assertion is configured for this fault.");
}

public sealed record RegionOneReliabilityAddPressurePayloadDoseSnapshot(
    double DuplicateSummonPotencyMultiplier,
    int TrialCount,
    IReadOnlyList<RegionOneReliabilityFamilyEvidenceSnapshot> Families,
    RegionOneReliabilityFamilyResponseSnapshot FamilyResponse);

public sealed record RegionOneReliabilityMechanicDoseFamilySnapshot(
    PartyFamilyKind Family,
    int TrialCount,
    double ClearRate,
    double AverageDurationTicks,
    double AverageFriendlyDeaths,
    double AverageRemainingHealthRatio,
    double AverageGuardianHealthRemainingRatio,
    double AverageGuardianSelfSustainPerSecond,
    double AverageGuardianDamageTakenPerSecond,
    double AverageGuardianNetDamagePerSecond,
    double AverageNonPrimaryFriendlyDamageTakenPerSecond,
    double AverageFriendlyDamageTakenConcentration,
    double AveragePartySustainPerSecond,
    double AverageInjectedDistributedDamagePerSecond,
    double AverageInjectedDistributedDamagePeakTargetsPerWave,
    double FriendlyDeathEventRate,
    double? AverageObservedFirstFriendlyDeathTick,
    double RestrictedMeanFirstFriendlyDeathTicks);

public sealed record RegionOneReliabilityMechanicDoseSnapshot(
    double DoseFraction,
    double AppliedMultiplier,
    int TrialCount,
    IReadOnlyList<RegionOneReliabilityMechanicDoseFamilySnapshot> Families);

public sealed record RegionOneReliabilityFaultSnapshot(
    RegionOneReliabilityFaultKind Fault,
    int Floor,
    EncounterCalibrationParameterGroup? ExpectedParameterGroup,
    WorldTowerObservedFailureMode ExpectedObservedFailureMode,
    EncounterCalibrationParameterGroup? RecoveredParameterGroup,
    string InjectedControl,
    double FaultMultiplier,
    double ReferenceClearRate,
    double FaultClearRate,
    double ClearRateDrop,
    WorldTowerObservedFailureMode DominantObservedFailureMode,
    double DominantObservedFailureShare,
    double ExpectedObservedFailureShare,
    RegionOneReliabilityRecoveryMethod RecoveryMethod,
    RegionOneReliabilityPhysicalComparisonSnapshot PhysicalComparison,
    RegionOneReliabilityFamilyResponseSnapshot FamilyResponse,
    bool InjectionReachedPhysicalTelemetry,
    bool FaultObservable,
    bool DiagnosticRecoveryMatched,
    string CalibrationResponse,
    double? SuggestedCorrectionFactor,
    bool CorrectionVerifiedByFrozenReference,
    RegionOneReliabilityVerdict Verdict,
    IReadOnlyList<RegionOneReliabilityFamilyEvidenceSnapshot> Families,
    IReadOnlyList<string> Warnings)
{
    public IReadOnlyList<RegionOneReliabilityAddPressurePayloadDoseSnapshot> AddPressurePayloadDoseResponse { get; init; } = [];
    public IReadOnlyList<RegionOneReliabilityMechanicDoseSnapshot> MechanicDoseResponse { get; init; } = [];
    public RegionOneReliabilityVerdict DiagnosticVerdict { get; init; } = RegionOneReliabilityVerdict.Disabled;
    public RegionOneReliabilityFamilyContractVerdict FamilyContractVerdict { get; init; } =
        RegionOneReliabilityFamilyContractVerdict.NotApplicable;
}

public sealed record RegionOneReliabilityUnavailableFaultSnapshot(
    string Fault,
    RegionOneReliabilityVerdict Verdict,
    string Reason);

public sealed record RegionOneReliabilityCleanseDemandPreconditionSnapshot(
    bool EvidenceAvailable,
    int CatalogAbilityCount,
    int CatalogCleanseEffectCount,
    int CatalogDispelEffectCount,
    int ProfiledBuildCount,
    int CleanseCapableBuildCount,
    int MaximumCleansesObserved,
    double MaximumCleansesPer15Seconds,
    int Floor,
    bool FloorRequiresCleanse,
    int RequestedMechanicRosters,
    int RetainedMechanicRosters,
    PartyFamilyMaterialStatus? MaterialStatus,
    bool PrerequisitesSatisfied,
    bool InjectionImplemented,
    string Assessment)
{
    public static RegionOneReliabilityCleanseDemandPreconditionSnapshot NotEvaluated { get; } = new(
        false, 0, 0, 0, 0, 0, 0, 0, 8, false, 0, 0, null, false, false,
        "Cleanse-demand prerequisites were not evaluated.");
}

public sealed record RegionOneProgressionCapabilityDistributionSnapshot(
    BuildCapabilityDimension Dimension,
    string Unit,
    double P10,
    double P50,
    double P90);

public sealed record RegionOneProgressionPopulationSnapshot(
    string ProfileId,
    int SlotCount,
    int CharacterLevel,
    int UnlockedEssenceSlots,
    string GearPackageId,
    int BuildCount,
    double MeanBenchmarkPower,
    IReadOnlyList<RegionOneProgressionCapabilityDistributionSnapshot> CapabilityDistributions);

public sealed record RegionOneProgressionProfileEvidenceSnapshot(
    string ProfileId,
    bool CurrentNearestProfile,
    double AbsoluteTargetPowerDistance,
    double RelativeTargetPowerDistance,
    int RequestedRosterCount,
    int RetainedRosterCount,
    PartyFamilyMaterialStatus MaterialStatus,
    int TrialCount,
    double? ClearRate,
    double? RosterConfidenceLowerBound,
    double? RosterConfidenceUpperBound,
    double? P10DurationTicks,
    double? MedianDurationTicks,
    double? P90DurationTicks,
    double? AverageFriendlyDeaths,
    double? AverageRemainingHealthRatio,
    WorldTowerObservedFailureMode? PrimaryObservedFailureMode,
    bool? MateriallyDifferentFromCurrent,
    IReadOnlyList<string> Warnings);

public sealed record RegionOneProgressionFloorFidelitySnapshot(
    int Floor,
    string EncounterName,
    int RequiredSlots,
    double TargetBenchmarkPower,
    double RecommendedDisplayCr,
    string CurrentNearestProfileId,
    double? NeutralDifficultyFactor,
    RegionOneReliabilityVerdict Verdict,
    bool? IntermediateProfileMateriallyChangesConclusion,
    IReadOnlyList<RegionOneProgressionProfileEvidenceSnapshot> Profiles,
    string Assessment)
{
    public IReadOnlyList<RegionOneReliabilityReferenceCandidateSnapshot> NeutralReferenceCandidates { get; init; } = [];
}

public sealed record RegionOneProgressionFidelitySnapshot(
    bool Enabled,
    RegionOneReliabilityVerdict Verdict,
    bool ProductionContentModified,
    int TotalCombatTrials,
    bool? ProfilePowerOrderingMonotonic,
    IReadOnlyList<RegionOneProgressionPopulationSnapshot> Populations,
    IReadOnlyList<RegionOneProgressionFloorFidelitySnapshot> Floors,
    string Recommendation,
    IReadOnlyList<string> Warnings)
{
    public static RegionOneProgressionFidelitySnapshot NotEvaluated { get; } = new(
        false,
        RegionOneReliabilityVerdict.Disabled,
        false,
        0,
        null,
        [],
        [],
        "Progression fidelity was not evaluated.",
        []);

    public RegionOneMatchedGenomeProgressionSnapshot MatchedGenomePowerProbe { get; init; } =
        RegionOneMatchedGenomeProgressionSnapshot.NotEvaluated;
}

public sealed record RegionOneReliabilityPopulationProtocolSnapshot(
    int BalanceSchemaVersion,
    int EssenceBuildsPerProfile,
    int PveBenchmarkScoringVersion,
    int OptimizerAlgorithmVersion,
    EssenceOptimizerOptions OptimizerOptions,
    int RepresentativeBuildAlgorithmVersion,
    RepresentativeBuildOptions RepresentativeBuildOptions,
    int CapabilityProfilerAlgorithmVersion,
    string CapabilityNormalizationVersion,
    string CapabilityContentFingerprint,
    int CapabilityProbeSeedCount,
    int PartyFamilyBuilderAlgorithmVersion,
    PartyFamilyBuilderOptions PartyFamilyBuilderOptions,
    int WorldTowerAnalyzerAlgorithmVersion,
    WorldTowerAnalysisOptions WorldTowerAnalysisOptions);

public sealed record RegionOneReliabilityStudySnapshot(
    int AlgorithmVersion,
    int Seed,
    RegionOneReliabilityStudyOptions Options,
    bool ProductionContentModified,
    bool ReleaseEligible,
    int TotalCombatTrials,
    RegionOneReliabilityVerdict Verdict,
    IReadOnlyList<RegionOneReliabilityReferenceSnapshot> References,
    IReadOnlyList<RegionOneReliabilityFaultSnapshot> Faults,
    IReadOnlyList<RegionOneReliabilityUnavailableFaultSnapshot> UnsupportedFaults,
    IReadOnlyList<string> Warnings)
{
    public RegionOneReliabilityCleanseDemandPreconditionSnapshot CleanseDemandPrecondition { get; init; } =
        RegionOneReliabilityCleanseDemandPreconditionSnapshot.NotEvaluated;
    public RegionOneProgressionFidelitySnapshot ProgressionFidelity { get; init; } =
        RegionOneProgressionFidelitySnapshot.NotEvaluated;
    public RegionOneReliabilityPopulationProtocolSnapshot? PopulationProtocol { get; init; }
}

public sealed class RegionOneReliabilityStudyAnalyzer(
    IEncounterScaleProbeCombatEvaluator combatEvaluator,
    PartyFamilyBuilder? partyFamilyBuilder = null)
{
    public const int AlgorithmVersion = 18;
    private readonly PartyFamilyBuilder _partyFamilyBuilder = partyFamilyBuilder ?? new PartyFamilyBuilder();
    private static readonly PartyFamilyKind[] RequiredFamilies =
        [PartyFamilyKind.IntendedBalanced, PartyFamilyKind.Defensive, PartyFamilyKind.SingleTargetSpecialist];

    public RegionOneReliabilityStudySnapshot Analyze(
        WorldTowerAnalysisSnapshot worldTower,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        PartyFamilySuiteSnapshot partyFamilies,
        int runSeed,
        RegionOneReliabilityStudyOptions? requestedOptions = null,
        BuildCapabilitySuiteSnapshot? buildCapabilities = null,
        IReadOnlyList<AbilitySpec>? abilityCatalog = null,
        RegionOneMatchedGenomeProgressionSnapshot? matchedGenomeProgression = null,
        RegionOneReliabilityPopulationProtocolSnapshot? populationProtocol = null)
    {
        ArgumentNullException.ThrowIfNull(worldTower);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        ArgumentNullException.ThrowIfNull(partyFamilies);
        var options = (requestedOptions ?? new RegionOneReliabilityStudyOptions()).Validate();
        var cleanseDemandPrecondition = CreateCleanseDemandPrecondition(
            partyFamilies,
            buildCapabilities,
            abilityCatalog,
            options.RostersPerFamily);
        var unsupported = new[]
        {
            new RegionOneReliabilityUnavailableFaultSnapshot(
                "CleanseDemand",
                RegionOneReliabilityVerdict.Unavailable,
                cleanseDemandPrecondition.Assessment)
        };
        if (!options.Enabled)
        {
            return new RegionOneReliabilityStudySnapshot(
                AlgorithmVersion,
                runSeed,
                options,
                ProductionContentModified: false,
                ReleaseEligible: false,
                TotalCombatTrials: 0,
                RegionOneReliabilityVerdict.Disabled,
                [],
                [],
                unsupported,
                ["Region 1 reliability fault injection is disabled for this run."])
            {
                CleanseDemandPrecondition = cleanseDemandPrecondition,
                PopulationProtocol = populationProtocol
            };
        }

        var representativeById = representativeBuilds.Profiles.SelectMany(profile => profile.Builds
                .Select(build => new RepresentativeLookup(profile.Id, build)))
            .GroupBy(value => value.Build.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var worldTowerByFloor = worldTower.Floors.ToDictionary(floor => floor.Floor);
        var familiesByFloor = partyFamilies.Floors.ToDictionary(floor => floor.Floor);
        var counter = new EvaluationCounter();
        var references = new[]
        {
            CreateReference(
                options.HealthOffenseFloor,
                requireAddFree: true,
                worldTowerByFloor,
                familiesByFloor,
                representativeById,
                runSeed,
                worldTower.Options.MaxTicks,
                options,
                counter),
            CreateReference(
                options.RegenerationFloor,
                requireAddFree: false,
                worldTowerByFloor,
                familiesByFloor,
                representativeById,
                runSeed,
                worldTower.Options.MaxTicks,
                options,
                counter),
            CreateReference(
                options.AddPressureFloor,
                requireAddFree: false,
                worldTowerByFloor,
                familiesByFloor,
                representativeById,
                runSeed,
                worldTower.Options.MaxTicks,
                options,
                counter)
        };
        var healthReference = references[0];
        var regenerationReference = references[1];
        var addPressureReference = references[2];
        var faults = new[]
        {
            EvaluateFault(
                RegionOneReliabilityFaultKind.Health,
                EncounterCalibrationParameterGroup.Health,
                healthReference,
                familiesByFloor,
                representativeById,
                runSeed,
                worldTower.Options.MaxTicks,
                options,
                counter),
            EvaluateFault(
                RegionOneReliabilityFaultKind.Offense,
                EncounterCalibrationParameterGroup.Offense,
                healthReference,
                familiesByFloor,
                representativeById,
                runSeed,
                worldTower.Options.MaxTicks,
                options,
                counter),
            EvaluateFault(
                RegionOneReliabilityFaultKind.Regeneration,
                EncounterCalibrationParameterGroup.Regeneration,
                regenerationReference,
                familiesByFloor,
                representativeById,
                runSeed,
                worldTower.Options.MaxTicks,
                options,
                counter),
            EvaluateFault(
                RegionOneReliabilityFaultKind.AddPressure,
                null,
                addPressureReference,
                familiesByFloor,
                representativeById,
                runSeed,
                worldTower.Options.MaxTicks,
                options,
                counter),
            EvaluateFault(
                RegionOneReliabilityFaultKind.DistributedAttrition,
                null,
                healthReference,
                familiesByFloor,
                representativeById,
                runSeed,
                worldTower.Options.MaxTicks,
                options,
                counter)
        };
        var progressionFidelity = CreateProgressionFidelityStudy(
            worldTower,
            representativeBuilds,
            partyFamilies,
            buildCapabilities,
            representativeById,
            references,
            runSeed,
            options,
            counter,
            matchedGenomeProgression);
        var verdict = faults.Any(fault => fault.Verdict == RegionOneReliabilityVerdict.Fail)
            ? RegionOneReliabilityVerdict.Fail
            : faults.All(fault => fault.Verdict == RegionOneReliabilityVerdict.Pass)
                ? RegionOneReliabilityVerdict.Pass
                : faults.Any(fault => fault.Verdict == RegionOneReliabilityVerdict.Unavailable)
                    ? RegionOneReliabilityVerdict.Unavailable
                    : RegionOneReliabilityVerdict.Inconclusive;
        var warnings = references.SelectMany(reference => reference.Warnings.Select(warning =>
                $"Floor {reference.Floor}: {warning}"))
            .Concat(faults.SelectMany(fault => fault.Warnings.Select(warning => $"{fault.Fault}: {warning}")))
            .Concat(progressionFidelity.Warnings.Select(warning => $"Progression fidelity: {warning}"))
            .ToArray();
        return new RegionOneReliabilityStudySnapshot(
            AlgorithmVersion,
            runSeed,
            options,
            ProductionContentModified: false,
            ReleaseEligible: false,
            counter.TotalTrials,
            verdict,
            references,
            faults,
            unsupported,
            warnings)
        {
            CleanseDemandPrecondition = cleanseDemandPrecondition,
            ProgressionFidelity = progressionFidelity,
            PopulationProtocol = populationProtocol
        };
    }

    private RegionOneProgressionFidelitySnapshot CreateProgressionFidelityStudy(
        WorldTowerAnalysisSnapshot worldTower,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        PartyFamilySuiteSnapshot partyFamilies,
        BuildCapabilitySuiteSnapshot? buildCapabilities,
        IReadOnlyDictionary<string, RepresentativeLookup> representativeById,
        IReadOnlyList<RegionOneReliabilityReferenceSnapshot> reliabilityReferences,
        int runSeed,
        RegionOneReliabilityStudyOptions options,
        EvaluationCounter counter,
        RegionOneMatchedGenomeProgressionSnapshot? matchedGenomeProgression)
    {
        if (!options.ProgressionFidelityEnabled)
        {
            return new RegionOneProgressionFidelitySnapshot(
                false,
                RegionOneReliabilityVerdict.Disabled,
                false,
                0,
                null,
                [],
                [],
                "Progression-fidelity comparison is disabled for this run.",
                [])
            {
                MatchedGenomePowerProbe = matchedGenomeProgression ?? RegionOneMatchedGenomeProgressionSnapshot.NotEvaluated
            };
        }
        if (buildCapabilities is null)
        {
            return new RegionOneProgressionFidelitySnapshot(
                true,
                RegionOneReliabilityVerdict.Unavailable,
                false,
                0,
                null,
                [],
                [],
                "Measured build capabilities are required for progression-fidelity rosters.",
                ["Build-capability evidence was not supplied."])
            {
                MatchedGenomePowerProbe = matchedGenomeProgression ?? RegionOneMatchedGenomeProgressionSnapshot.NotEvaluated
            };
        }

        var profiles = representativeBuilds.Profiles
            .Where(profile => profile.TargetPercentile == 75 && profile.SlotCount is >= 4 and <= 6)
            .OrderBy(profile => profile.SlotCount)
            .ToArray();
        var populations = profiles.Select(profile => CreateProgressionPopulation(profile, buildCapabilities)).ToArray();
        var expectedSlots = new[] { 4, 5, 6 };
        if (!profiles.Select(profile => profile.SlotCount).SequenceEqual(expectedSlots))
        {
            return new RegionOneProgressionFidelitySnapshot(
                true,
                RegionOneReliabilityVerdict.Unavailable,
                false,
                0,
                null,
                populations,
                [],
                "The E4/E5/E6 P75 population matrix is incomplete; retain the current mapping pending complete evidence.",
                ["Progression fidelity requires exactly one P75 profile for each of E4, E5, and E6."])
            {
                MatchedGenomePowerProbe = matchedGenomeProgression ?? RegionOneMatchedGenomeProgressionSnapshot.NotEvaluated
            };
        }

        var powerOrderingMonotonic = profiles.Zip(profiles.Skip(1))
            .All(pair => pair.First.MeanSelectedScore <= pair.Second.MeanSelectedScore);
        var worldTowerByFloor = worldTower.Floors.ToDictionary(floor => floor.Floor);
        var partyFamiliesByFloor = partyFamilies.Floors.ToDictionary(floor => floor.Floor);
        var referenceByFloor = reliabilityReferences
            .Where(reference => reference.Verdict == RegionOneReliabilityVerdict.Pass
                                && reference.SelectedDifficultyFactor.HasValue)
            .ToDictionary(reference => reference.Floor);
        var warnings = new List<string>();
        var trialsBefore = counter.TotalTrials;
        var floors = new List<RegionOneProgressionFloorFidelitySnapshot>();

        foreach (var floorNumber in options.ProgressionFidelityFloors.Order())
        {
            if (!worldTowerByFloor.TryGetValue(floorNumber, out var worldTowerFloor)
                || !partyFamiliesByFloor.TryGetValue(floorNumber, out var partyFloor))
            {
                var warning = $"Floor {floorNumber} is absent from the frozen World Tower or party-family evidence.";
                warnings.Add(warning);
                floors.Add(UnavailableProgressionFloor(floorNumber, warning));
                continue;
            }

            var currentFamily = partyFloor.Families.SingleOrDefault(family =>
                family.Family == PartyFamilyKind.IntendedBalanced);
            if (currentFamily is null || currentFamily.Parties.Count < options.RostersPerFamily)
            {
                var warning = $"Floor {floorNumber} lacks {options.RostersPerFamily} valid current-profile IntendedBalanced rosters.";
                warnings.Add(warning);
                floors.Add(UnavailableProgressionFloor(worldTowerFloor, warning));
                continue;
            }

            double? neutralFactor;
            RegionOneReliabilityFamilyEvidenceSnapshot? currentEvidence;
            IReadOnlyList<RegionOneReliabilityReferenceCandidateSnapshot> neutralReferenceCandidates;
            if (referenceByFloor.TryGetValue(floorNumber, out var existingReference))
            {
                neutralFactor = existingReference.SelectedDifficultyFactor;
                currentEvidence = existingReference.Families.Single(family =>
                    family.Family == PartyFamilyKind.IntendedBalanced);
                neutralReferenceCandidates = existingReference.Candidates;
            }
            else
            {
                var referenceSearch = FindProgressionReference(
                    partyFloor,
                    currentFamily,
                    representativeById,
                    runSeed,
                    worldTower.Options.MaxTicks,
                    options,
                    counter);
                neutralFactor = referenceSearch.Selected?.DifficultyFactor;
                currentEvidence = referenceSearch.Selected?.Evidence;
                neutralReferenceCandidates = referenceSearch.Candidates;
            }

            if (!neutralFactor.HasValue || currentEvidence is null)
            {
                var warning = $"Floor {floorNumber} produced no 40-80% current-profile neutral reference.";
                warnings.Add(warning);
                floors.Add(UnavailableProgressionFloor(worldTowerFloor, warning) with
                {
                    NeutralReferenceCandidates = neutralReferenceCandidates
                });
                continue;
            }

            var profileEvidence = new List<RegionOneProgressionProfileEvidenceSnapshot>();
            foreach (var profile in profiles)
            {
                var isCurrent = profile.Id.Equals(worldTowerFloor.RepresentativeProfileId, StringComparison.Ordinal);
                IReadOnlyList<PartyFamilyPartySnapshot> rosters;
                try
                {
                    rosters = isCurrent
                        ? currentFamily.Parties.Take(options.RostersPerFamily).ToArray()
                        : _partyFamilyBuilder.BuildBalancedProgressionProbeParties(
                            worldTowerFloor,
                            profile.Id,
                            representativeBuilds,
                            buildCapabilities,
                            runSeed,
                            options.RostersPerFamily);
                }
                catch (InvalidOperationException exception)
                {
                    warnings.Add($"Floor {floorNumber} {profile.Id}: {exception.Message}");
                    profileEvidence.Add(UnavailableProgressionProfile(
                        profile,
                        worldTowerFloor,
                        isCurrent,
                        options.RostersPerFamily,
                        0,
                        exception.Message));
                    continue;
                }

                if (rosters.Count < options.RostersPerFamily)
                {
                    warnings.Add(
                        $"Floor {floorNumber} {profile.Id} retained only {rosters.Count}/{options.RostersPerFamily} " +
                        "constraint-passing progression rosters.");
                    profileEvidence.Add(UnavailableProgressionProfile(
                        profile,
                        worldTowerFloor,
                        isCurrent,
                        options.RostersPerFamily,
                        rosters.Count,
                        $"Only {rosters.Count}/{options.RostersPerFamily} constraint-passing rosters were retained."));
                    continue;
                }

                var evidence = isCurrent
                    ? currentEvidence
                    : EvaluateFamily(
                        partyFloor,
                        new PartyFamilySnapshot(
                            PartyFamilyKind.IntendedBalanced,
                            PartyFamilyDisposition.ShouldSucceed,
                            options.RostersPerFamily,
                            rosters,
                            "progression-fidelity"),
                        representativeById,
                        neutralFactor.Value,
                        neutralFactor.Value,
                        1,
                        1,
                        0,
                        runSeed,
                        worldTower.Options.MaxTicks,
                        options,
                        counter);
                profileEvidence.Add(SummarizeProgressionProfile(
                    profile,
                    worldTowerFloor,
                    isCurrent,
                    options.RostersPerFamily,
                    rosters.Count,
                    evidence));
            }

            var current = profileEvidence.SingleOrDefault(profile => profile.CurrentNearestProfile);
            var compared = profileEvidence.Select(profile => profile with
            {
                MateriallyDifferentFromCurrent = profile.CurrentNearestProfile
                    ? false
                    : IsMateriallyDifferent(profile, current, options)
            }).ToArray();
            var intermediate = compared.SingleOrDefault(profile => profile.ProfileId == "E5_P75");
            var intermediateChanged = intermediate?.MateriallyDifferentFromCurrent;
            var floorVerdict = current?.ClearRate.HasValue == true
                               && compared.All(profile => profile.ClearRate.HasValue)
                ? intermediateChanged == true
                    ? RegionOneReliabilityVerdict.Inconclusive
                    : RegionOneReliabilityVerdict.Pass
                : RegionOneReliabilityVerdict.Unavailable;
            var assessment = floorVerdict == RegionOneReliabilityVerdict.Unavailable
                ? "The floor lacks a complete E4/E5/E6 neutral-reference comparison."
                : intermediateChanged == true
                    ? "The E5 population materially changes at least one combat conclusion relative to the current nearest profile."
                    : "The E5 population does not materially change clear rate, duration, or dominant failure mode relative to the current nearest profile.";
            floors.Add(new RegionOneProgressionFloorFidelitySnapshot(
                floorNumber,
                worldTowerFloor.EncounterName,
                worldTowerFloor.RequiredSlots,
                worldTowerFloor.TargetBenchmarkPower,
                worldTowerFloor.RecommendedDisplayCr,
                worldTowerFloor.RepresentativeProfileId,
                neutralFactor,
                floorVerdict,
                intermediateChanged,
                compared,
                assessment)
            {
                NeutralReferenceCandidates = neutralReferenceCandidates
            });
        }

        var completeFloors = floors.Where(floor => floor.Verdict != RegionOneReliabilityVerdict.Unavailable).ToArray();
        var incompleteEvidence = floors.Any(floor => floor.Verdict == RegionOneReliabilityVerdict.Unavailable);
        var materiallyChanged = completeFloors.Any(floor => floor.IntermediateProfileMateriallyChangesConclusion == true);
        var verdict = completeFloors.Length == 0
            ? RegionOneReliabilityVerdict.Unavailable
            : !powerOrderingMonotonic || materiallyChanged
                ? RegionOneReliabilityVerdict.Inconclusive
                : RegionOneReliabilityVerdict.Pass;
        var recommendation = incompleteEvidence
            ? "No cohort-model decision is supported: at least one selected floor lacks a complete neutral-reference E4/E5/E6 comparison. Increase frozen population coverage only enough to test whether valid intermediate rosters exist; do not infer that missing evidence means no material change."
            : !powerOrderingMonotonic
            ? "Do not assign E5 as a mid-region progression cohort yet: measured P75 benchmark power is not monotonic across E4/E5/E6. Test an explicit mid gear/level package before changing the current mapping."
            : materiallyChanged
                ? "A mid-region cohort materially changes encounter conclusions; evaluate the smallest explicit three-stage E4/E5/E6 progression model before release."
                : "The intermediate cohort did not materially change the tested conclusions; retain the simpler current mapping and avoid adding floor-specific optimizers.";
        return new RegionOneProgressionFidelitySnapshot(
            true,
            verdict,
            false,
            counter.TotalTrials - trialsBefore,
            powerOrderingMonotonic,
            populations,
            floors,
            recommendation,
            warnings)
        {
            MatchedGenomePowerProbe = matchedGenomeProgression ?? RegionOneMatchedGenomeProgressionSnapshot.NotEvaluated
        };
    }

    private ProgressionReferenceSearchEvidence FindProgressionReference(
        PartyFamilyFloorSnapshot floor,
        PartyFamilySnapshot currentFamily,
        IReadOnlyDictionary<string, RepresentativeLookup> representativeById,
        int runSeed,
        int maxTicks,
        RegionOneReliabilityStudyOptions options,
        EvaluationCounter counter)
    {
        var candidates = options.ReferenceDifficultyFactors.Select(factor =>
            new ProgressionReferenceEvidence(
                factor,
                EvaluateFamily(
                    floor,
                    currentFamily,
                    representativeById,
                    factor,
                    factor,
                    1,
                    1,
                    0,
                    runSeed,
                    maxTicks,
                    options,
                    counter)))
            .ToList();
        var selected = candidates
            .Where(candidate => candidate.Evidence.ClearRate >= options.MinimumReferenceClearRate
                                && candidate.Evidence.ClearRate <= options.MaximumReferenceClearRate)
            .OrderBy(candidate => Math.Abs(candidate.Evidence.ClearRate - options.TargetReferenceClearRate))
            .ThenBy(candidate => Math.Abs(1 - candidate.DifficultyFactor))
            .FirstOrDefault();
        if (selected is null && options.MaximumReferenceRefinementIterations > 0)
        {
            var ordered = candidates.OrderByDescending(candidate => candidate.DifficultyFactor).ToArray();
            var bracket = ordered.Zip(ordered.Skip(1)).FirstOrDefault(pair =>
                pair.First.Evidence.ClearRate < options.MinimumReferenceClearRate
                && pair.Second.Evidence.ClearRate > options.MaximumReferenceClearRate);
            if (bracket.First is not null && bracket.Second is not null)
            {
                var hard = bracket.First;
                var easy = bracket.Second;
                for (var iteration = 0;
                     iteration < options.MaximumReferenceRefinementIterations && selected is null;
                     iteration++)
                {
                    var midpoint = Round((hard.DifficultyFactor + easy.DifficultyFactor) / 2);
                    if (candidates.Any(candidate => Math.Abs(candidate.DifficultyFactor - midpoint) < 0.0001))
                        break;
                    var refined = new ProgressionReferenceEvidence(
                        midpoint,
                        EvaluateFamily(
                            floor,
                            currentFamily,
                            representativeById,
                            midpoint,
                            midpoint,
                            1,
                            1,
                            0,
                            runSeed,
                            maxTicks,
                            options,
                            counter));
                    candidates.Add(refined);
                    if (refined.Evidence.ClearRate >= options.MinimumReferenceClearRate
                        && refined.Evidence.ClearRate <= options.MaximumReferenceClearRate)
                    {
                        selected = refined;
                    }
                    else if (refined.Evidence.ClearRate < options.MinimumReferenceClearRate)
                    {
                        hard = refined;
                    }
                    else if (refined.Evidence.ClearRate > options.MaximumReferenceClearRate)
                    {
                        easy = refined;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        return new ProgressionReferenceSearchEvidence(
            selected,
            candidates
                .OrderByDescending(candidate => candidate.DifficultyFactor)
                .Select(candidate => new RegionOneReliabilityReferenceCandidateSnapshot(
                    candidate.DifficultyFactor,
                    candidate.Evidence.TrialCount,
                    candidate.Evidence.ClearRate,
                    candidate.Evidence.Uncertainty.RosterClusterLowerBound,
                    candidate.Evidence.Uncertainty.RosterClusterUpperBound,
                    candidate.Evidence.ClearRate >= options.MinimumReferenceClearRate
                    && candidate.Evidence.ClearRate <= options.MaximumReferenceClearRate))
                .ToArray());
    }

    private static RegionOneProgressionPopulationSnapshot CreateProgressionPopulation(
        RepresentativeEssenceProfileSnapshot profile,
        BuildCapabilitySuiteSnapshot capabilities)
    {
        var capabilityByBuild = capabilities.Profiles.ToDictionary(value => value.BuildId, StringComparer.Ordinal);
        var measured = profile.Builds
            .Select(build => capabilityByBuild.GetValueOrDefault(build.SourceBuildId))
            .OfType<BuildCapabilityProfileSnapshot>()
            .ToArray();
        var distributions = Enum.GetValues<BuildCapabilityDimension>().Select(dimension =>
        {
            var values = measured.Select(profile => profile.Dimensions.Single(value => value.Dimension == dimension))
                .ToArray();
            if (values.Length == 0)
                return null;
            var ordered = values.Select(value => value.RawValue).Order().ToArray();
            return new RegionOneProgressionCapabilityDistributionSnapshot(
                dimension,
                values[0].Unit,
                Round(Percentile(ordered, 0.10)),
                Round(Percentile(ordered, 0.50)),
                Round(Percentile(ordered, 0.90)));
        }).OfType<RegionOneProgressionCapabilityDistributionSnapshot>().ToArray();
        var character = profile.Builds[0].Character;
        return new RegionOneProgressionPopulationSnapshot(
            profile.Id,
            profile.SlotCount,
            character.CharacterLevel,
            character.UnlockedEssenceSlots,
            character.GearPackageId,
            profile.Builds.Count,
            Round(profile.MeanSelectedScore),
            distributions);
    }

    private static RegionOneProgressionProfileEvidenceSnapshot SummarizeProgressionProfile(
        RepresentativeEssenceProfileSnapshot profile,
        WorldTowerFloorAnalysisSnapshot floor,
        bool isCurrent,
        int requestedRosters,
        int retainedRosters,
        RegionOneReliabilityFamilyEvidenceSnapshot evidence)
    {
        var trials = evidence.Rosters.SelectMany(roster => roster.Trials).ToArray();
        var durations = trials.Select(trial => (double)trial.DurationTicks).Order().ToArray();
        var dominantMode = evidence.PrimaryObservedFailureModeCounts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Select(pair => (WorldTowerObservedFailureMode?)pair.Key)
            .FirstOrDefault();
        var absoluteDistance = Math.Abs(profile.MeanSelectedScore - floor.TargetBenchmarkPower);
        return new RegionOneProgressionProfileEvidenceSnapshot(
            profile.Id,
            isCurrent,
            Round(absoluteDistance),
            floor.TargetBenchmarkPower == 0 ? 0 : Round(absoluteDistance / floor.TargetBenchmarkPower),
            requestedRosters,
            retainedRosters,
            PartyFamilyMaterialStatus.Available,
            trials.Length,
            evidence.ClearRate,
            evidence.Uncertainty.RosterClusterLowerBound,
            evidence.Uncertainty.RosterClusterUpperBound,
            Round(Percentile(durations, 0.10)),
            Round(Percentile(durations, 0.50)),
            Round(Percentile(durations, 0.90)),
            Round(trials.Average(trial => trial.FriendlyDeaths)),
            Round(trials.Average(trial => trial.RemainingHealthRatio)),
            dominantMode,
            null,
            []);
    }

    private static bool? IsMateriallyDifferent(
        RegionOneProgressionProfileEvidenceSnapshot candidate,
        RegionOneProgressionProfileEvidenceSnapshot? current,
        RegionOneReliabilityStudyOptions options)
    {
        if (!candidate.ClearRate.HasValue || !candidate.MedianDurationTicks.HasValue
            || current?.ClearRate is null || current.MedianDurationTicks is null)
        {
            return null;
        }
        var clearDifference = Math.Abs(candidate.ClearRate.Value - current.ClearRate.Value);
        var durationDifference = current.MedianDurationTicks.Value <= 0
            ? 0
            : Math.Abs(candidate.MedianDurationTicks.Value - current.MedianDurationTicks.Value)
              / current.MedianDurationTicks.Value;
        var failureModeChanged = candidate.PrimaryObservedFailureMode.HasValue
                                 && current.PrimaryObservedFailureMode.HasValue
                                 && candidate.PrimaryObservedFailureMode != current.PrimaryObservedFailureMode;
        return clearDifference >= options.ProgressionFidelityMaterialClearRateDifference
               || durationDifference >= options.ProgressionFidelityMaterialDurationRatioDifference
               || failureModeChanged;
    }

    private static RegionOneProgressionProfileEvidenceSnapshot UnavailableProgressionProfile(
        RepresentativeEssenceProfileSnapshot profile,
        WorldTowerFloorAnalysisSnapshot floor,
        bool isCurrent,
        int requestedRosters,
        int retainedRosters,
        string warning)
    {
        var absoluteDistance = Math.Abs(profile.MeanSelectedScore - floor.TargetBenchmarkPower);
        return new RegionOneProgressionProfileEvidenceSnapshot(
            profile.Id,
            isCurrent,
            Round(absoluteDistance),
            floor.TargetBenchmarkPower == 0 ? 0 : Round(absoluteDistance / floor.TargetBenchmarkPower),
            requestedRosters,
            retainedRosters,
            PartyFamilyMaterialStatus.InsufficientFamilyMaterial,
            0,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            [warning]);
    }

    private static RegionOneProgressionFloorFidelitySnapshot UnavailableProgressionFloor(
        WorldTowerFloorAnalysisSnapshot floor,
        string assessment) => new(
        floor.Floor,
        floor.EncounterName,
        floor.RequiredSlots,
        floor.TargetBenchmarkPower,
        floor.RecommendedDisplayCr,
        floor.RepresentativeProfileId,
        null,
        RegionOneReliabilityVerdict.Unavailable,
        null,
        [],
        assessment);

    private static RegionOneProgressionFloorFidelitySnapshot UnavailableProgressionFloor(
        int floor,
        string assessment) => new(
        floor,
        "Unknown",
        0,
        0,
        0,
        "Unknown",
        null,
        RegionOneReliabilityVerdict.Unavailable,
        null,
        [],
        assessment);

    private static RegionOneReliabilityCleanseDemandPreconditionSnapshot CreateCleanseDemandPrecondition(
        PartyFamilySuiteSnapshot partyFamilies,
        BuildCapabilitySuiteSnapshot? buildCapabilities,
        IReadOnlyList<AbilitySpec>? abilityCatalog,
        int requestedRosters)
    {
        const int cleanseFloor = 8;
        var floor = partyFamilies.Floors.SingleOrDefault(value => value.Floor == cleanseFloor);
        var response = floor?.ResponseProfile.Responses.SingleOrDefault(value =>
            value.Family == PartyFamilyKind.MechanicSpecialist);
        var mechanicFamily = floor?.Families.SingleOrDefault(value =>
            value.Family == PartyFamilyKind.MechanicSpecialist);
        var evidenceAvailable = buildCapabilities is not null && abilityCatalog is not null;
        var profiles = buildCapabilities?.Profiles ?? [];
        var cleanseEffectCount = abilityCatalog?.Sum(ability => ability.Effects.Count(effect =>
            effect.Operation == AbilityEffectOperation.Cleanse)) ?? 0;
        var dispelEffectCount = abilityCatalog?.Sum(ability => ability.Effects.Count(effect =>
            effect.Operation == AbilityEffectOperation.Dispel)) ?? 0;
        var cleanseCapable = profiles.Where(profile => profile.Mechanics.StatusEffectsCleansed > 0).ToArray();
        var maximumCleanses = profiles.Count == 0
            ? 0
            : profiles.Max(profile => profile.Mechanics.StatusEffectsCleansed);
        var maximumCleansesPer15Seconds = profiles.Count == 0
            ? 0
            : profiles.Max(profile => profile.Mechanics.CleansesPer15Seconds);
        var retainedRosters = mechanicFamily?.Parties.Count ?? 0;
        var floorRequiresCleanse = response?.RequiredMechanic == PartyMechanicCapabilityKind.Cleanse;
        var prerequisitesSatisfied = evidenceAvailable
                                     && cleanseEffectCount > 0
                                     && cleanseCapable.Length > 0
                                     && floorRequiresCleanse
                                     && retainedRosters >= requestedRosters;
        var assessment = !evidenceAvailable
            ? "Cleanse-demand prerequisite evidence was not supplied; controlled injection remains unavailable."
            : cleanseEffectCount == 0
                ? $"The loaded production ability catalog contains zero Cleanse effects; {profiles.Count} profiled builds produced zero physical cleanses and Floor 8 retained {retainedRosters}/{requestedRosters} required MechanicSpecialist rosters. Controlled cleanse injection remains unavailable."
                : cleanseCapable.Length == 0
                    ? $"The catalog exposes {cleanseEffectCount} Cleanse effects, but none of {profiles.Count} profiled builds produced a physical cleanse. Controlled cleanse injection remains unavailable."
                    : retainedRosters < requestedRosters
                        ? $"Cleanse-capable builds exist, but Floor 8 retained only {retainedRosters}/{requestedRosters} required MechanicSpecialist rosters. Controlled cleanse injection remains unavailable."
                        : "Cleanse prerequisites are satisfied, but a controlled harmful-status-pressure injection has not been implemented.";
        return new RegionOneReliabilityCleanseDemandPreconditionSnapshot(
            evidenceAvailable,
            abilityCatalog?.Count ?? 0,
            cleanseEffectCount,
            dispelEffectCount,
            profiles.Count,
            cleanseCapable.Length,
            maximumCleanses,
            Round(maximumCleansesPer15Seconds),
            cleanseFloor,
            floorRequiresCleanse,
            requestedRosters,
            retainedRosters,
            mechanicFamily?.MaterialStatus,
            prerequisitesSatisfied,
            InjectionImplemented: false,
            assessment);
    }

    private RegionOneReliabilityReferenceSnapshot CreateReference(
        int floorNumber,
        bool requireAddFree,
        IReadOnlyDictionary<int, WorldTowerFloorAnalysisSnapshot> worldTowerByFloor,
        IReadOnlyDictionary<int, PartyFamilyFloorSnapshot> familiesByFloor,
        IReadOnlyDictionary<string, RepresentativeLookup> representativeById,
        int runSeed,
        int maxTicks,
        RegionOneReliabilityStudyOptions options,
        EvaluationCounter counter)
    {
        if (!worldTowerByFloor.TryGetValue(floorNumber, out var worldTowerFloor)
            || !familiesByFloor.TryGetValue(floorNumber, out var familyFloor))
        {
            return UnavailableReference(floorNumber, "Unknown", "The configured reliability floor is absent from the frozen run.");
        }
        if (requireAddFree && worldTowerFloor.Trials.Any(trial =>
                trial.PeakActiveHostileCombatants > 1 || trial.PeakActiveHostileSummons > 0))
        {
            return UnavailableReference(
                floorNumber,
                worldTowerFloor.EncounterName,
                "The configured health/offense reference emitted additional hostiles and is not add-free.");
        }
        var populationError = ValidatePopulation(familyFloor, options.RostersPerFamily);
        if (populationError is not null)
            return UnavailableReference(floorNumber, worldTowerFloor.EncounterName, populationError);

        var intended = familyFloor.Families.Single(family => family.Family == PartyFamilyKind.IntendedBalanced);
        var candidates = options.ReferenceDifficultyFactors.Select(factor =>
        {
            var evidence = EvaluateFamily(
                familyFloor,
                intended,
                representativeById,
                factor,
                factor,
                1,
                1,
                0,
                runSeed,
                maxTicks,
                options,
                counter);
            return new ReferenceCandidateEvidence(
                new RegionOneReliabilityReferenceCandidateSnapshot(
                    factor,
                    evidence.TrialCount,
                    evidence.ClearRate,
                    evidence.Uncertainty.RosterClusterLowerBound,
                    evidence.Uncertainty.RosterClusterUpperBound,
                    evidence.ClearRate >= options.MinimumReferenceClearRate
                    && evidence.ClearRate <= options.MaximumReferenceClearRate),
                evidence);
        }).ToList();
        var selected = candidates
            .Where(candidate => candidate.Snapshot.InsideReferenceWindow)
            .OrderBy(candidate => Math.Abs(candidate.Snapshot.IntendedBalancedClearRate - options.TargetReferenceClearRate))
            .ThenBy(candidate => Math.Abs(1 - candidate.Snapshot.DifficultyFactor))
            .FirstOrDefault();
        if (selected is null && options.MaximumReferenceRefinementIterations > 0)
        {
            var ordered = candidates.OrderByDescending(candidate => candidate.Snapshot.DifficultyFactor).ToArray();
            var bracket = ordered.Zip(ordered.Skip(1))
                .FirstOrDefault(pair =>
                    pair.First.Snapshot.IntendedBalancedClearRate < options.MinimumReferenceClearRate
                    && pair.Second.Snapshot.IntendedBalancedClearRate > options.MaximumReferenceClearRate);
            if (bracket.First is not null && bracket.Second is not null)
            {
                var hard = bracket.First;
                var easy = bracket.Second;
                for (var iteration = 0;
                     iteration < options.MaximumReferenceRefinementIterations && selected is null;
                     iteration++)
                {
                    var midpoint = Round((hard.Snapshot.DifficultyFactor + easy.Snapshot.DifficultyFactor) / 2);
                    if (candidates.Any(candidate => Math.Abs(candidate.Snapshot.DifficultyFactor - midpoint) < 0.0001))
                        break;
                    var evidence = EvaluateFamily(
                        familyFloor,
                        intended,
                        representativeById,
                        midpoint,
                        midpoint,
                        1,
                        1,
                        0,
                        runSeed,
                        maxTicks,
                        options,
                        counter);
                    var refined = new ReferenceCandidateEvidence(
                        new RegionOneReliabilityReferenceCandidateSnapshot(
                            midpoint,
                            evidence.TrialCount,
                            evidence.ClearRate,
                            evidence.Uncertainty.RosterClusterLowerBound,
                            evidence.Uncertainty.RosterClusterUpperBound,
                            evidence.ClearRate >= options.MinimumReferenceClearRate
                            && evidence.ClearRate <= options.MaximumReferenceClearRate),
                        evidence);
                    candidates.Add(refined);
                    if (refined.Snapshot.InsideReferenceWindow)
                        selected = refined;
                    else if (refined.Snapshot.IntendedBalancedClearRate < options.MinimumReferenceClearRate)
                        hard = refined;
                    else if (refined.Snapshot.IntendedBalancedClearRate > options.MaximumReferenceClearRate)
                        easy = refined;
                    else
                        break;
                }
            }
        }
        if (selected is null)
        {
            return new RegionOneReliabilityReferenceSnapshot(
                floorNumber,
                worldTowerFloor.EncounterName,
                null,
                RegionOneReliabilityVerdict.Unavailable,
                candidates.OrderByDescending(candidate => candidate.Snapshot.DifficultyFactor)
                    .Select(candidate => candidate.Snapshot).ToArray(),
                [],
                [$"No frozen neutral reference produced a {options.MinimumReferenceClearRate:P0}-{options.MaximumReferenceClearRate:P0} IntendedBalanced clear rate."]);
        }

        var families = new List<RegionOneReliabilityFamilyEvidenceSnapshot> { selected.Evidence };
        var evaluationFamilies = RequiredFamilies.AsEnumerable();
        var multiTarget = familyFloor.Families.SingleOrDefault(family =>
            family.Family == PartyFamilyKind.MultiTargetSpecialist);
        if (multiTarget is not null && multiTarget.Parties.Count >= options.RostersPerFamily)
            evaluationFamilies = evaluationFamilies.Append(PartyFamilyKind.MultiTargetSpecialist);
        families.AddRange(evaluationFamilies.Where(family => family != PartyFamilyKind.IntendedBalanced).Select(kind =>
            EvaluateFamily(
                familyFloor,
                familyFloor.Families.Single(family => family.Family == kind),
                representativeById,
                selected.Snapshot.DifficultyFactor,
                selected.Snapshot.DifficultyFactor,
                1,
                1,
                0,
                runSeed,
                maxTicks,
                options,
                counter)));
        return new RegionOneReliabilityReferenceSnapshot(
            floorNumber,
            worldTowerFloor.EncounterName,
            selected.Snapshot.DifficultyFactor,
            RegionOneReliabilityVerdict.Pass,
            candidates.OrderByDescending(candidate => candidate.Snapshot.DifficultyFactor)
                .Select(candidate => candidate.Snapshot).ToArray(),
            families,
            []);
    }

    private RegionOneReliabilityFaultSnapshot EvaluateFault(
        RegionOneReliabilityFaultKind fault,
        EncounterCalibrationParameterGroup? expected,
        RegionOneReliabilityReferenceSnapshot reference,
        IReadOnlyDictionary<int, PartyFamilyFloorSnapshot> familiesByFloor,
        IReadOnlyDictionary<string, RepresentativeLookup> representativeById,
        int runSeed,
        int maxTicks,
        RegionOneReliabilityStudyOptions options,
        EvaluationCounter counter)
    {
        if (reference.Verdict != RegionOneReliabilityVerdict.Pass
            || !reference.SelectedDifficultyFactor.HasValue
            || !familiesByFloor.TryGetValue(reference.Floor, out var familyFloor))
        {
            return UnavailableFault(fault, expected, reference.Floor, options.FaultMultiplier,
                "A valid neutral reference and complete frozen family population are required.");
        }
        var factor = reference.SelectedDifficultyFactor.Value;
        var health = fault == RegionOneReliabilityFaultKind.Health ? factor * options.FaultMultiplier : factor;
        var offense = fault == RegionOneReliabilityFaultKind.Offense ? factor * options.FaultMultiplier : factor;
        const double regeneration = 1;
        var guardianAbilityHealing = fault == RegionOneReliabilityFaultKind.Regeneration
            ? options.FaultMultiplier
            : 1;
        var additionalSummonCopies = fault == RegionOneReliabilityFaultKind.AddPressure ? 1 : 0;
        var distributedDamage = fault == RegionOneReliabilityFaultKind.DistributedAttrition
            ? options.FaultMultiplier
            : 1;
        var evidence = reference.Families.Select(referenceFamily => EvaluateFamily(
                familyFloor,
                familyFloor.Families.Single(family => family.Family == referenceFamily.Family),
                representativeById,
                health,
                offense,
                regeneration,
                guardianAbilityHealing,
                additionalSummonCopies,
                runSeed,
                maxTicks,
                options,
                counter,
                guardianDistributedDamageMultiplier: distributedDamage))
            .ToArray();
        var referenceClear = reference.Families.Single(family => family.Family == PartyFamilyKind.IntendedBalanced).ClearRate;
        var faultClear = evidence.Single(family => family.Family == PartyFamilyKind.IntendedBalanced).ClearRate;
        var clearDrop = Round(referenceClear - faultClear);
        var observedFailures = evidence.SelectMany(family => family.Rosters)
            .SelectMany(roster => roster.Trials)
            .Select(trial => trial.FailureDiagnostic.PrimaryObservedFailureMode)
            .Where(mode => mode != WorldTowerObservedFailureMode.None)
            .GroupBy(mode => mode)
            .Select(group => (Mode: group.Key, Count: group.Count()))
            .OrderByDescending(value => value.Count)
            .ThenBy(value => value.Mode)
            .ToArray();
        var totalFailures = observedFailures.Sum(value => value.Count);
        var dominant = observedFailures.FirstOrDefault();
        var dominantMode = totalFailures == 0 ? WorldTowerObservedFailureMode.None : dominant.Mode;
        var dominantShare = totalFailures == 0 ? 0 : Round(dominant.Count / (double)totalFailures);
        var failedTrials = evidence.SelectMany(family => family.Rosters)
            .SelectMany(roster => roster.Trials)
            .Where(trial => !IsVictory(trial))
            .ToArray();
        var expectedObservedMode = fault switch
        {
            RegionOneReliabilityFaultKind.AddPressure => WorldTowerObservedFailureMode.AddPressure,
            RegionOneReliabilityFaultKind.DistributedAttrition => WorldTowerObservedFailureMode.PartyAttrition,
            _ => WorldTowerObservedFailureMode.None
        };
        var usesObservedFailureRecovery = fault is RegionOneReliabilityFaultKind.AddPressure
            or RegionOneReliabilityFaultKind.DistributedAttrition;
        var expectedObservedShare = usesObservedFailureRecovery && failedTrials.Length > 0
            ? Round(failedTrials.Count(trial =>
                    trial.FailureDiagnostic.PrimaryObservedFailureMode == expectedObservedMode
                    || trial.FailureDiagnostic.ContributingConditions.Contains(expectedObservedMode))
                / (double)failedTrials.Length)
            : dominantShare;
        var physicalComparison = CreatePhysicalComparison(reference.Families, evidence);
        var familyResponse = CreateFamilyResponse(fault, reference.Families, evidence, options);
        var payloadDoseResponse = fault == RegionOneReliabilityFaultKind.AddPressure
            ? options.AddPressurePayloadDoseMultipliers.Select(dose =>
            {
                var doseEvidence = dose == 1
                    ? evidence
                    : reference.Families.Select(referenceFamily => EvaluateFamily(
                            familyFloor,
                            familyFloor.Families.Single(family => family.Family == referenceFamily.Family),
                            representativeById,
                            health,
                            offense,
                            regeneration,
                            guardianAbilityHealing,
                            additionalSummonCopies,
                            runSeed,
                            maxTicks,
                            options,
                            counter,
                            guardianAdditionalSummonPotencyMultiplier: dose))
                        .ToArray();
                return new RegionOneReliabilityAddPressurePayloadDoseSnapshot(
                    dose,
                    doseEvidence.Sum(family => family.TrialCount),
                    doseEvidence,
                    dose == 1
                        ? familyResponse
                        : CreateFamilyResponse(fault, reference.Families, doseEvidence, options));
            }).ToArray()
            : [];
        if (fault == RegionOneReliabilityFaultKind.AddPressure)
        {
            familyResponse = ApplyAddPressureDoseCoherence(
                familyResponse,
                payloadDoseResponse,
                options.MaximumAddPressureDoseResponseReversal);
            payloadDoseResponse = payloadDoseResponse.Select(dose =>
                    dose.DuplicateSummonPotencyMultiplier == 1
                        ? dose with { FamilyResponse = familyResponse }
                        : dose)
                .ToArray();
        }
        var mechanicDoseResponse = fault is RegionOneReliabilityFaultKind.Regeneration
            or RegionOneReliabilityFaultKind.DistributedAttrition
            ? options.MechanicDoseFractions.Select(doseFraction =>
            {
                var appliedMultiplier = 1 + (options.FaultMultiplier - 1) * doseFraction;
                var doseEvidence = doseFraction == 1
                    ? evidence
                    : reference.Families.Select(referenceFamily => EvaluateFamily(
                            familyFloor,
                            familyFloor.Families.Single(family => family.Family == referenceFamily.Family),
                            representativeById,
                            factor,
                            factor,
                            1,
                            fault == RegionOneReliabilityFaultKind.Regeneration ? appliedMultiplier : 1,
                            0,
                            runSeed,
                            maxTicks,
                            options,
                            counter,
                            guardianDistributedDamageMultiplier:
                                fault == RegionOneReliabilityFaultKind.DistributedAttrition ? appliedMultiplier : 1))
                        .ToArray();
                return new RegionOneReliabilityMechanicDoseSnapshot(
                    Round(doseFraction),
                    Round(appliedMultiplier),
                    doseEvidence.Sum(family => family.TrialCount),
                    doseEvidence.Select(family => CreateMechanicDoseFamilySnapshot(family, maxTicks)).ToArray());
            }).ToArray()
            : [];
        var (actual, recoveryMethod) = usesObservedFailureRecovery
            ? (null, expectedObservedShare >= options.MinimumDominantFailureShare
                ? RegionOneReliabilityRecoveryMethod.ObservedFailureMode
                : RegionOneReliabilityRecoveryMethod.None)
            : RecoverParameterGroup(dominantMode, dominantShare, physicalComparison, options);
        var injectionReachedPhysicalTelemetry = InjectionReachedPhysicalTelemetry(
            fault,
            expected,
            physicalComparison,
            options);
        var observable = clearDrop >= options.MinimumClearRateDrop;
        var expectedEvidenceReached = usesObservedFailureRecovery
            ? expectedObservedShare >= options.MinimumDominantFailureShare
            : dominantShare >= options.MinimumDominantFailureShare;
        var matched = usesObservedFailureRecovery
            ? expectedEvidenceReached
            : actual == expected;
        var diagnosticVerdict = !injectionReachedPhysicalTelemetry
            ? RegionOneReliabilityVerdict.Unavailable
            : !observable || !expectedEvidenceReached
            ? RegionOneReliabilityVerdict.Inconclusive
            : !matched
                ? RegionOneReliabilityVerdict.Fail
                : RegionOneReliabilityVerdict.Pass;
        var familyContractVerdict = !familyResponse.Applicable
            ? RegionOneReliabilityFamilyContractVerdict.NotApplicable
            : familyResponse.Matched switch
            {
                true => RegionOneReliabilityFamilyContractVerdict.Pass,
                false => RegionOneReliabilityFamilyContractVerdict.Inconclusive,
                null => RegionOneReliabilityFamilyContractVerdict.InsufficientEvidence
            };
        var verdict = diagnosticVerdict != RegionOneReliabilityVerdict.Pass
            ? diagnosticVerdict
            : familyContractVerdict is RegionOneReliabilityFamilyContractVerdict.NotApplicable
                or RegionOneReliabilityFamilyContractVerdict.Pass
                ? RegionOneReliabilityVerdict.Pass
                : RegionOneReliabilityVerdict.Inconclusive;
        var warnings = new List<string>();
        if (!injectionReachedPhysicalTelemetry)
        {
            warnings.Add(fault switch
            {
                RegionOneReliabilityFaultKind.Regeneration => "The Guardian ability-healing override produced no measurable self-sustain-per-second increase; this encounter's sustain fault is not physically observable.",
                RegionOneReliabilityFaultKind.AddPressure => "The duplicate brood-summon override did not produce the required peak-add increase in compact telemetry.",
                RegionOneReliabilityFaultKind.DistributedAttrition => "The Slam the Gates override did not produce both the required non-primary friendly damage increase and directly attributed multi-target injected damage.",
                _ => $"The {expected} override did not reach its required paired physical-telemetry signature."
            });
        }
        if (!observable)
            warnings.Add($"The injected fault reduced IntendedBalanced clear rate by only {clearDrop:P0}; at least {options.MinimumClearRateDrop:P0} is required.");
        if (!expectedEvidenceReached)
            warnings.Add(usesObservedFailureRecovery
                ? $"{expectedObservedMode} appeared in only {expectedObservedShare:P0} of failed trials; at least {options.MinimumDominantFailureShare:P0} is required."
                : "No observed failure mode reached the required dominance threshold.");
        else if (!matched)
            warnings.Add($"Expected {expected} recovery, but dominant mode {dominantMode} resolved to {actual?.ToString() ?? "no safe parameter group"}.");
        if (familyResponse.Applicable && familyResponse.Matched is not true)
            warnings.Add(familyResponse.Assessment);
        return new RegionOneReliabilityFaultSnapshot(
            fault,
            reference.Floor,
            expected,
            expectedObservedMode,
            actual,
            GetInjectedControl(fault),
            fault == RegionOneReliabilityFaultKind.AddPressure ? 2 : options.FaultMultiplier,
            referenceClear,
            faultClear,
            clearDrop,
            dominantMode,
            dominantShare,
            expectedObservedShare,
            recoveryMethod,
            physicalComparison,
            familyResponse,
            injectionReachedPhysicalTelemetry,
            observable,
            matched,
            fault switch
            {
                RegionOneReliabilityFaultKind.AddPressure => "Review: the assisted calibrator has no add-count parameter group.",
                RegionOneReliabilityFaultKind.DistributedAttrition => "Review: the assisted calibrator has no ability-specific distributed-damage parameter group.",
                _ => matched ? "Supported" : "Review"
            },
            usesObservedFailureRecovery || !matched
                ? null
                : Round(1 / options.FaultMultiplier),
            !usesObservedFailureRecovery && matched,
            verdict,
            evidence,
            warnings)
        {
            AddPressurePayloadDoseResponse = payloadDoseResponse,
            MechanicDoseResponse = mechanicDoseResponse,
            DiagnosticVerdict = diagnosticVerdict,
            FamilyContractVerdict = familyContractVerdict
        };
    }

    private static RegionOneReliabilityMechanicDoseFamilySnapshot CreateMechanicDoseFamilySnapshot(
        RegionOneReliabilityFamilyEvidenceSnapshot family,
        int maxTicks)
    {
        var trials = family.Rosters.SelectMany(roster => roster.Trials).ToArray();
        var firstDeathTicks = trials
            .Where(trial => trial.FirstFriendlyDeathTick.HasValue)
            .Select(trial => trial.FirstFriendlyDeathTick!.Value)
            .ToArray();
        return new RegionOneReliabilityMechanicDoseFamilySnapshot(
            family.Family,
            trials.Length,
            family.ClearRate,
            Round(trials.Average(trial => trial.DurationTicks)),
            Round(trials.Average(trial => trial.FriendlyDeaths)),
            Round(trials.Average(trial => trial.RemainingHealthRatio)),
            Round(trials.Average(trial => trial.GuardianHealthRemainingRatio)),
            Round(trials.Average(GetGuardianSelfSustainPerSecond)),
            Round(trials.Average(trial => trial.GuardianDamageTakenPerSecond)),
            Round(trials.Average(trial =>
                trial.GuardianDamageTakenPerSecond - GetGuardianSelfSustainPerSecond(trial))),
            Round(trials.Average(trial => trial.NonPrimaryFriendlyDamageTakenPerSecond)),
            Round(trials.Average(trial => trial.FriendlyDamageTakenConcentration)),
            Round(trials.Average(trial => trial.PartySustainPerSecond)),
            Round(trials.Average(trial => trial.GuardianInjectedDistributedDamagePerSecond)),
            Round(trials.Average(trial => trial.GuardianInjectedDistributedDamagePeakTargetsPerWave)),
            Round(firstDeathTicks.Length / (double)trials.Length),
            firstDeathTicks.Length == 0 ? null : Round(firstDeathTicks.Average()),
            Round(RestrictedMeanFirstFriendlyDeathTicks(trials, maxTicks)));
    }

    private static double RestrictedMeanFirstFriendlyDeathTicks(
        IReadOnlyList<WorldTowerTrialSnapshot> trials,
        int maxTicks)
    {
        var observations = trials.Select(trial => new
            {
                Time = Math.Clamp(
                    trial.FirstFriendlyDeathTick
                    ?? (IsVictory(trial) ? maxTicks : trial.DurationTicks),
                    0,
                    maxTicks),
                Event = trial.FirstFriendlyDeathTick.HasValue
            })
            .OrderBy(observation => observation.Time)
            .ToArray();
        var survival = 1d;
        var restrictedMean = 0d;
        var previousTime = 0;
        foreach (var time in observations.Select(observation => observation.Time).Distinct())
        {
            restrictedMean += survival * (time - previousTime);
            var atRisk = observations.Count(observation => observation.Time >= time);
            var deaths = observations.Count(observation => observation.Time == time && observation.Event);
            if (atRisk > 0 && deaths > 0)
                survival *= 1 - deaths / (double)atRisk;
            previousTime = time;
        }
        restrictedMean += survival * (maxTicks - previousTime);
        return restrictedMean;
    }

    private RegionOneReliabilityFamilyEvidenceSnapshot EvaluateFamily(
        PartyFamilyFloorSnapshot floor,
        PartyFamilySnapshot family,
        IReadOnlyDictionary<string, RepresentativeLookup> representativeById,
        double health,
        double offense,
        double regeneration,
        double guardianAbilityHealing,
        int guardianAdditionalSummonCopies,
        int runSeed,
        int maxTicks,
        RegionOneReliabilityStudyOptions options,
        EvaluationCounter counter,
        double guardianAdditionalSummonPotencyMultiplier = 1,
        double guardianDistributedDamageMultiplier = 1)
    {
        var rosters = family.Parties.Take(options.RostersPerFamily).Select(party =>
        {
            var builds = party.Members.Select(member =>
            {
                if (!representativeById.TryGetValue(member.BuildId, out var representative))
                    throw new InvalidOperationException($"Reliability-study build '{member.BuildId}' is absent from representative evidence.");
                return ToEssenceBuild(representative.Build, representative.ProfileId);
            }).ToArray();
            var trials = combatEvaluator.EvaluateScaleProbe(new EncounterScaleProbeCombatRequest(
                floor.Floor,
                floor.RequiredSlots,
                builds,
                runSeed,
                options.SimulationsPerRoster,
                maxTicks,
                new EncounterScaleProbeOverride(
                    floor.Floor,
                    floor.RequiredSlots,
                    health,
                    offense,
                    RegenerationMultiplier: regeneration,
                    GuardianAbilityHealingMultiplier: guardianAbilityHealing,
                    GuardianAdditionalSummonCopies: guardianAdditionalSummonCopies,
                    GuardianAdditionalSummonPotencyMultiplier: guardianAdditionalSummonPotencyMultiplier,
                    GuardianDistributedDamageMultiplier: guardianDistributedDamageMultiplier))).ToArray();
            counter.TotalTrials += trials.Length;
            var clears = trials.Count(IsVictory);
            return new RegionOneReliabilityRosterEvidenceSnapshot(
                party.Signature,
                trials.Length,
                clears,
                Round(clears / (double)trials.Length),
                trials);
        }).ToArray();
        var uncertainty = CreateUncertainty(rosters);
        var allTrials = rosters.SelectMany(roster => roster.Trials).ToArray();
        var addSpawnTrials = allTrials
            .Where(trial => trial.FirstAdditionalHostileTick.HasValue)
            .ToArray();
        var addClearDurations = addSpawnTrials
            .Select(trial => trial.FirstAdditionalHostileClearDurationTicks)
            .Where(duration => duration.HasValue)
            .Select(duration => duration!.Value)
            .ToArray();
        var waveIntervalCount = allTrials.Sum(trial => trial.HostileSummonWaveIntervalCount);
        var waveIntervalTotalTicks = allTrials.Sum(trial => trial.HostileSummonWaveIntervalTotalTicks);
        var hostileSummonWaveCount = allTrials.Sum(trial => trial.HostileSummonWaveCount);
        var hostileSummonsCreated = allTrials.Sum(trial => trial.TotalHostileSummons);
        return new RegionOneReliabilityFamilyEvidenceSnapshot(
            family.Family,
            rosters.Length,
            allTrials.Length,
            allTrials.Count(IsVictory),
            Round(allTrials.Count(IsVictory) / (double)allTrials.Length),
            addSpawnTrials.Length,
            addClearDurations.Length,
            addSpawnTrials.Length == 0
                ? null
                : Round(addClearDurations.Length / (double)addSpawnTrials.Length),
            addClearDurations.Length == 0
                ? null
                : Round(addClearDurations.Average()),
            Round(allTrials.Average(trial => trial.TotalHostileSummons)),
            Round(allTrials.Average(trial => trial.HostileSummonWaveCount)),
            hostileSummonWaveCount == 0
                ? null
                : Round(hostileSummonsCreated / (double)hostileSummonWaveCount),
            waveIntervalCount == 0
                ? null
                : Round(waveIntervalTotalTicks / (double)waveIntervalCount),
            Round(allTrials.Average(trial => trial.AdditionalHostileWindowCount)),
            Round(allTrials.Average(trial => trial.ClearedAdditionalHostileWindowCount)),
            Round(allTrials.Average(trial => trial.HostileSummonActiveTicks)),
            Round(allTrials.Average(trial => trial.HostileSummonUptimeRatio)),
            Round(allTrials.Average(trial => trial.PeakActiveHostileSummons)),
            uncertainty,
            allTrials.Select(trial => trial.FailureDiagnostic.PrimaryObservedFailureMode)
                .GroupBy(mode => mode)
                .ToDictionary(group => group.Key, group => group.Count()),
            rosters);
    }

    private static string? ValidatePopulation(PartyFamilyFloorSnapshot floor, int requestedRosters)
    {
        foreach (var kind in RequiredFamilies)
        {
            var family = floor.Families.SingleOrDefault(value => value.Family == kind);
            if (family is null || family.Parties.Count < requestedRosters)
            {
                return $"{kind} retains {family?.Parties.Count ?? 0}/{requestedRosters} required unique constraint-passing rosters.";
            }
            if (family.Parties.Take(requestedRosters).Any(party => !party.ConstraintsSatisfied))
                return $"{kind} contains invalid roster material and cannot enter fault injection.";
        }
        return null;
    }

    private static RegionOneReliabilityReferenceSnapshot UnavailableReference(
        int floor,
        string encounterName,
        string warning) =>
        new(floor, encounterName, null, RegionOneReliabilityVerdict.Unavailable, [], [], [warning]);

    private static RegionOneReliabilityFaultSnapshot UnavailableFault(
        RegionOneReliabilityFaultKind fault,
        EncounterCalibrationParameterGroup? expected,
        int floor,
        double multiplier,
        string warning) =>
        new(
            fault,
            floor,
            expected,
            fault switch
            {
                RegionOneReliabilityFaultKind.AddPressure => WorldTowerObservedFailureMode.AddPressure,
                RegionOneReliabilityFaultKind.DistributedAttrition => WorldTowerObservedFailureMode.PartyAttrition,
                _ => WorldTowerObservedFailureMode.None
            },
            null,
            GetInjectedControl(fault),
            multiplier,
            0,
            0,
            0,
            WorldTowerObservedFailureMode.None,
            0,
            0,
            RegionOneReliabilityRecoveryMethod.None,
            new RegionOneReliabilityPhysicalComparisonSnapshot(
                0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, null,
                0, 0, null,
                0, 0, null,
                0, 0, null, 0, 0,
                0, 0, null, 0, 0, 0),
            RegionOneReliabilityFamilyResponseSnapshot.NotApplicable,
            false,
            false,
            false,
            "Unavailable",
            null,
            false,
            RegionOneReliabilityVerdict.Unavailable,
            [],
            [warning])
        {
            DiagnosticVerdict = RegionOneReliabilityVerdict.Unavailable,
            FamilyContractVerdict = fault is RegionOneReliabilityFaultKind.Regeneration
                or RegionOneReliabilityFaultKind.AddPressure
                or RegionOneReliabilityFaultKind.DistributedAttrition
                ? RegionOneReliabilityFamilyContractVerdict.InsufficientEvidence
                : RegionOneReliabilityFamilyContractVerdict.NotApplicable
        };

    private static (EncounterCalibrationParameterGroup? ParameterGroup, RegionOneReliabilityRecoveryMethod Method)
        RecoverParameterGroup(
            WorldTowerObservedFailureMode dominantMode,
            double dominantShare,
            RegionOneReliabilityPhysicalComparisonSnapshot physical,
            RegionOneReliabilityStudyOptions options)
    {
        if (dominantShare < options.MinimumDominantFailureShare)
            return (null, RegionOneReliabilityRecoveryMethod.None);

        if (physical.GuardianSelfSustainPerSecondRatio
            >= options.MinimumGuardianSelfSustainTelemetryRatio)
        {
            return (
                EncounterCalibrationParameterGroup.Regeneration,
                RegionOneReliabilityRecoveryMethod.PairedPhysicalTelemetry);
        }

        var raw = EncounterCalibrator.ResolveParameterGroup(dominantMode);
        if (raw != EncounterCalibrationParameterGroup.Offense)
        {
            return (raw, raw.HasValue
                ? RegionOneReliabilityRecoveryMethod.DominantFailureMode
                : RegionOneReliabilityRecoveryMethod.None);
        }

        if (physical.HostileDamagePerSecondRatio >= options.MinimumOffenseDamagePerSecondRatio)
        {
            return (
                EncounterCalibrationParameterGroup.Offense,
                RegionOneReliabilityRecoveryMethod.PairedPhysicalTelemetry);
        }

        var damagePerSecondStable = Math.Abs(physical.HostileDamagePerSecondRatio - 1)
                                   <= options.MaximumHealthDamagePerSecondDeviation;
        if (damagePerSecondStable
            && physical.GuardianHealthRemainingRatioIncrease >= options.MinimumHealthRemainingRatioIncrease)
        {
            return (
                EncounterCalibrationParameterGroup.Health,
                RegionOneReliabilityRecoveryMethod.PairedPhysicalTelemetry);
        }

        return (raw, RegionOneReliabilityRecoveryMethod.DominantFailureMode);
    }

    private static bool InjectionReachedPhysicalTelemetry(
        RegionOneReliabilityFaultKind fault,
        EncounterCalibrationParameterGroup? expected,
        RegionOneReliabilityPhysicalComparisonSnapshot physical,
        RegionOneReliabilityStudyOptions options)
    {
        if (fault == RegionOneReliabilityFaultKind.AddPressure)
        {
            return physical.ReferenceAveragePeakAdditionalHostiles <= 0
                ? physical.FaultAveragePeakAdditionalHostiles > 0
                : physical.PeakAdditionalHostilesRatio >= options.MinimumAddPressurePeakHostileRatio;
        }
        if (fault == RegionOneReliabilityFaultKind.DistributedAttrition)
        {
            var distributedDamageReached = physical.ReferenceAverageNonPrimaryFriendlyDamageTakenPerSecond <= 0
                ? physical.FaultAverageNonPrimaryFriendlyDamageTakenPerSecond > 0
                : physical.NonPrimaryFriendlyDamageTakenPerSecondRatio
                  >= options.MinimumDistributedDamagePerSecondRatio;
            var injectedDamageReached = physical.FaultAverageInjectedDistributedDamagePerSecond > 0
                                        && physical.FaultAverageInjectedDistributedDamagePeakTargetsPerWave >= 2;
            return distributedDamageReached && injectedDamageReached;
        }

        return expected switch
        {
            EncounterCalibrationParameterGroup.Health =>
                Math.Abs(physical.HostileDamagePerSecondRatio - 1)
                <= options.MaximumHealthDamagePerSecondDeviation
                && physical.GuardianHealthRemainingRatioIncrease
                >= options.MinimumHealthRemainingRatioIncrease,
            EncounterCalibrationParameterGroup.Offense =>
                physical.HostileDamagePerSecondRatio >= options.MinimumOffenseDamagePerSecondRatio,
            EncounterCalibrationParameterGroup.Regeneration =>
                physical.ReferenceAverageGuardianSelfSustainPerSecond <= 0
                    ? physical.FaultAverageGuardianSelfSustainPerSecond > 0
                    : physical.GuardianSelfSustainPerSecondRatio
                      >= options.MinimumGuardianSelfSustainTelemetryRatio,
            _ => false
        };
    }

    private static RegionOneReliabilityPhysicalComparisonSnapshot CreatePhysicalComparison(
        IReadOnlyList<RegionOneReliabilityFamilyEvidenceSnapshot> reference,
        IReadOnlyList<RegionOneReliabilityFamilyEvidenceSnapshot> fault)
    {
        var referenceTrials = reference.SelectMany(family => family.Rosters)
            .SelectMany(roster => roster.Trials).ToArray();
        var faultTrials = fault.SelectMany(family => family.Rosters)
            .SelectMany(roster => roster.Trials).ToArray();
        var referenceDamagePerSecond = referenceTrials.Average(trial => trial.HostileDamagePerSecond);
        var faultDamagePerSecond = faultTrials.Average(trial => trial.HostileDamagePerSecond);
        var referenceDuration = referenceTrials.Average(trial => trial.DurationTicks);
        var faultDuration = faultTrials.Average(trial => trial.DurationTicks);
        var referenceHealth = referenceTrials.Average(trial => trial.GuardianHealthRemainingRatio);
        var faultHealth = faultTrials.Average(trial => trial.GuardianHealthRemainingRatio);
        var referenceRegeneration = referenceTrials.Average(GetGuardianPassiveRegeneration);
        var faultRegeneration = faultTrials.Average(GetGuardianPassiveRegeneration);
        var referenceAbilityHealing = referenceTrials.Average(trial => trial.GuardianAbilityHealing);
        var faultAbilityHealing = faultTrials.Average(trial => trial.GuardianAbilityHealing);
        var referenceSelfSustainPerSecond = referenceTrials.Average(GetGuardianSelfSustainPerSecond);
        var faultSelfSustainPerSecond = faultTrials.Average(GetGuardianSelfSustainPerSecond);
        var referencePeakAdditionalHostiles = referenceTrials.Average(trial =>
            Math.Max(0, trial.PeakActiveHostileCombatants - 1));
        var faultPeakAdditionalHostiles = faultTrials.Average(trial =>
            Math.Max(0, trial.PeakActiveHostileCombatants - 1));
        var referenceFinalAdditionalHostiles = referenceTrials.Average(trial =>
            Math.Max(0, trial.FinalActiveHostileCombatants - 1));
        var faultFinalAdditionalHostiles = faultTrials.Average(trial =>
            Math.Max(0, trial.FinalActiveHostileCombatants - 1));
        var referenceNonPrimaryDamagePerSecond = referenceTrials.Average(trial =>
            trial.NonPrimaryFriendlyDamageTakenPerSecond);
        var faultNonPrimaryDamagePerSecond = faultTrials.Average(trial =>
            trial.NonPrimaryFriendlyDamageTakenPerSecond);
        var referenceDamageConcentration = referenceTrials.Average(trial =>
            trial.FriendlyDamageTakenConcentration);
        var faultDamageConcentration = faultTrials.Average(trial =>
            trial.FriendlyDamageTakenConcentration);
        return new RegionOneReliabilityPhysicalComparisonSnapshot(
            Round(referenceDamagePerSecond),
            Round(faultDamagePerSecond),
            Ratio(faultDamagePerSecond, referenceDamagePerSecond),
            Round(referenceDuration),
            Round(faultDuration),
            Ratio(faultDuration, referenceDuration),
            Round(referenceHealth),
            Round(faultHealth),
            Round(faultHealth - referenceHealth),
            Round(referenceRegeneration),
            Round(faultRegeneration),
            referenceRegeneration > 0 ? Ratio(faultRegeneration, referenceRegeneration) : null,
            Round(referenceAbilityHealing),
            Round(faultAbilityHealing),
            referenceAbilityHealing > 0 ? Ratio(faultAbilityHealing, referenceAbilityHealing) : null,
            Round(referenceSelfSustainPerSecond),
            Round(faultSelfSustainPerSecond),
            referenceSelfSustainPerSecond > 0
                ? Ratio(faultSelfSustainPerSecond, referenceSelfSustainPerSecond)
                : null,
            Round(referencePeakAdditionalHostiles),
            Round(faultPeakAdditionalHostiles),
            referencePeakAdditionalHostiles > 0
                ? Ratio(faultPeakAdditionalHostiles, referencePeakAdditionalHostiles)
                : null,
            Round(referenceFinalAdditionalHostiles),
            Round(faultFinalAdditionalHostiles),
            Round(referenceNonPrimaryDamagePerSecond),
            Round(faultNonPrimaryDamagePerSecond),
            referenceNonPrimaryDamagePerSecond > 0
                ? Ratio(faultNonPrimaryDamagePerSecond, referenceNonPrimaryDamagePerSecond)
                : null,
            Round(referenceDamageConcentration),
            Round(faultDamageConcentration),
            Round(faultDamageConcentration - referenceDamageConcentration))
        {
            FaultAverageInjectedDistributedDamagePerSecond = Round(faultTrials.Average(trial =>
                trial.GuardianInjectedDistributedDamagePerSecond)),
            FaultAverageInjectedDistributedDamageHits = Round(faultTrials.Average(trial =>
                trial.GuardianInjectedDistributedDamageHitCount)),
            FaultAverageInjectedDistributedDamageWaves = Round(faultTrials.Average(trial =>
                trial.GuardianInjectedDistributedDamageWaveCount)),
            FaultAverageInjectedDistributedDamagePeakTargetsPerWave = Round(faultTrials.Average(trial =>
                trial.GuardianInjectedDistributedDamagePeakTargetsPerWave))
        };
    }

    private static RegionOneReliabilityFamilyResponseSnapshot CreateFamilyResponse(
        RegionOneReliabilityFaultKind fault,
        IReadOnlyList<RegionOneReliabilityFamilyEvidenceSnapshot> reference,
        IReadOnlyList<RegionOneReliabilityFamilyEvidenceSnapshot> injected,
        RegionOneReliabilityStudyOptions options)
    {
        if (fault is not (RegionOneReliabilityFaultKind.Regeneration
            or RegionOneReliabilityFaultKind.AddPressure
            or RegionOneReliabilityFaultKind.DistributedAttrition))
            return RegionOneReliabilityFamilyResponseSnapshot.NotApplicable;

        if (fault == RegionOneReliabilityFaultKind.Regeneration)
        {
            return new RegionOneReliabilityFamilyResponseSnapshot(
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "No author-approved Regeneration family contract is configured. Physical Guardian self-sustain recovery remains authoritative; specialist identification requires an absolute sustained-damage-versus-self-sustain contract and independent replication.");
        }

        if (fault == RegionOneReliabilityFaultKind.DistributedAttrition)
        {
            return new RegionOneReliabilityFamilyResponseSnapshot(
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "No author-approved DistributedAttrition family contract is configured. Direct injected damage and PartyAttrition recovery remain authoritative; affected-family identification requires an independently replicated attrition-resilient cohort.");
        }

        if (fault == RegionOneReliabilityFaultKind.AddPressure
            && (!reference.Any(family => family.Family == PartyFamilyKind.MultiTargetSpecialist)
                || !injected.Any(family => family.Family == PartyFamilyKind.MultiTargetSpecialist)))
        {
            return new RegionOneReliabilityFamilyResponseSnapshot(
                true,
                PartyFamilyKind.MultiTargetSpecialist,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "MultiTargetSpecialist retains fewer than the required valid rosters on this floor, so the authored add-pressure response shape cannot be validated.");
        }

        var referenceIntended = ClearRate(reference, PartyFamilyKind.IntendedBalanced);
        var injectedIntended = ClearRate(injected, PartyFamilyKind.IntendedBalanced);
        var expectedFamily = PartyFamilyKind.MultiTargetSpecialist;
        var comparisonFamily = PartyFamilyKind.SingleTargetSpecialist;
        var referenceSpecialist = ClearRate(reference, expectedFamily);
        var injectedSpecialist = ClearRate(injected, expectedFamily);
        var referenceDefensive = ClearRate(reference, comparisonFamily);
        var injectedDefensive = ClearRate(injected, comparisonFamily);
        var referenceAdvantage = Round(referenceSpecialist - referenceIntended);
        var injectedAdvantage = Round(injectedSpecialist - injectedIntended);
        var advantageDelta = Round(injectedAdvantage - referenceAdvantage);
        var referenceDefensiveAdvantage = Round(referenceDefensive - referenceIntended);
        var injectedDefensiveAdvantage = Round(injectedDefensive - injectedIntended);
        var defensiveDelta = Round(injectedDefensiveAdvantage - referenceDefensiveAdvantage);
        var referenceMultiTarget = reference.Single(family =>
            family.Family == PartyFamilyKind.MultiTargetSpecialist);
        var injectedMultiTarget = injected.Single(family =>
            family.Family == PartyFamilyKind.MultiTargetSpecialist);
        var injectedIntendedEvidence = injected.Single(family =>
            family.Family == PartyFamilyKind.IntendedBalanced);
        var specialistWindowResetRate = AddWindowResetRate(injectedMultiTarget);
        var intendedWindowResetRate = AddWindowResetRate(injectedIntendedEvidence);
        var strongestComparisonResetRate = injected
            .Where(family => family.Family != PartyFamilyKind.MultiTargetSpecialist)
            .Max(AddWindowResetRate);
        var specialistStrongestController = specialistWindowResetRate >= strongestComparisonResetRate;
        var windowResetAdvantage = specialistWindowResetRate - intendedWindowResetRate;
        var normalizedBurdenReached = injectedMultiTarget.AverageHostileSummonUptimeRatio
                                      > referenceMultiTarget.AverageHostileSummonUptimeRatio;
        var matched = specialistStrongestController
                      && windowResetAdvantage >= options.MinimumAddPressureWindowResetAdvantage
                      && normalizedBurdenReached;
        var assessment = FormattableString.Invariant(
            $"MultiTargetSpecialist strongest add-window reset rate {specialistWindowResetRate:P0} versus {strongestComparisonResetRate:P0}; reset advantage {windowResetAdvantage:+0.0%;-0.0%;0.0%} versus IntendedBalanced (minimum {options.MinimumAddPressureWindowResetAdvantage:P0}); normalized summon uptime {referenceMultiTarget.AverageHostileSummonUptimeRatio:P0}→{injectedMultiTarget.AverageHostileSummonUptimeRatio:P0}. Legacy relative clear-rate advantage changed by {advantageDelta:+0.0%;-0.0%;0.0%} and is diagnostic only.");
        return new RegionOneReliabilityFamilyResponseSnapshot(
            true,
            expectedFamily,
            referenceAdvantage,
            injectedAdvantage,
            advantageDelta,
            referenceDefensiveAdvantage,
            injectedDefensiveAdvantage,
            defensiveDelta,
            matched,
            assessment);
    }

    private static RegionOneReliabilityFamilyResponseSnapshot ApplyAddPressureDoseCoherence(
        RegionOneReliabilityFamilyResponseSnapshot response,
        IReadOnlyList<RegionOneReliabilityAddPressurePayloadDoseSnapshot> doses,
        double maximumReversal)
    {
        var multiTarget = doses
            .OrderBy(dose => dose.DuplicateSummonPotencyMultiplier)
            .Select(dose => dose.Families.SingleOrDefault(family =>
                family.Family == PartyFamilyKind.MultiTargetSpecialist))
            .ToArray();
        if (multiTarget.Any(family => family is null))
            return response;

        var coherent = true;
        for (var index = 1; index < multiTarget.Length; index++)
        {
            var previous = multiTarget[index - 1]!;
            var current = multiTarget[index]!;
            coherent &= AddWindowResetRate(current) <= AddWindowResetRate(previous) + maximumReversal;
            coherent &= current.AverageHostileSummonUptimeRatio
                        >= previous.AverageHostileSummonUptimeRatio - maximumReversal;
        }

        return response with
        {
            Matched = response.Matched is true && coherent,
            Assessment = response.Assessment + FormattableString.Invariant(
                $" Graded MultiTarget payload response was {(coherent ? "coherent" : "not coherent")} within a {maximumReversal:P0} reversal tolerance.")
        };
    }

    private static double AddWindowResetRate(RegionOneReliabilityFamilyEvidenceSnapshot family) =>
        family.AverageAdditionalHostileWindowCount <= 0
            ? 0
            : family.AverageClearedAdditionalHostileWindowCount / family.AverageAdditionalHostileWindowCount;

    private static double ClearRate(
        IReadOnlyList<RegionOneReliabilityFamilyEvidenceSnapshot> families,
        PartyFamilyKind family) =>
        families.Single(value => value.Family == family).ClearRate;

    private static double GetGuardianPassiveRegeneration(WorldTowerTrialSnapshot trial) =>
        trial.GuardianPassiveRegeneration;

    private static double GetGuardianSelfSustainPerSecond(WorldTowerTrialSnapshot trial) =>
        trial.GuardianTotalSelfSustain
        / Math.Max(1d / FastCombatEngine.TicksPerSecond, trial.DurationTicks / (double)FastCombatEngine.TicksPerSecond);

    private static string GetInjectedControl(RegionOneReliabilityFaultKind fault) =>
        fault switch
        {
            RegionOneReliabilityFaultKind.Health => "GuardianHealth",
            RegionOneReliabilityFaultKind.Offense => "GuardianOffense",
            RegionOneReliabilityFaultKind.Regeneration => "GuardianAbilityHealing",
            RegionOneReliabilityFaultKind.AddPressure => "GuardianBroodSummonCount",
            RegionOneReliabilityFaultKind.DistributedAttrition => "GuardianSlamTheGatesDamage",
            _ => throw new ArgumentOutOfRangeException(nameof(fault))
        };

    private static double Ratio(double numerator, double denominator) =>
        denominator <= 0 ? 0 : Round(numerator / denominator);

    private static PartyFamilyUncertaintySnapshot CreateUncertainty(
        IReadOnlyList<RegionOneReliabilityRosterEvidenceSnapshot> rosters)
    {
        var totalTrials = rosters.Sum(roster => roster.TrialCount);
        var totalClears = rosters.Sum(roster => roster.ClearCount);
        var pooled = Wilson(totalClears, totalTrials);
        var rosterRates = rosters.Select(roster => roster.ClearCount / (double)roster.TrialCount).ToArray();
        var clustered = Wilson(rosterRates.Sum(), rosterRates.Length);
        var mean = rosterRates.Average();
        var between = rosterRates.Length < 2
            ? 0
            : rosterRates.Sum(rate => (rate - mean) * (rate - mean)) / (rosterRates.Length - 1);
        return new PartyFamilyUncertaintySnapshot(
            "roster",
            "roster-effective-wilson-v1",
            Round(pooled.Lower),
            Round(pooled.Upper),
            Round(clustered.Lower),
            Round(clustered.Upper),
            Round(between),
            Round(rosterRates.Average(rate => rate * (1 - rate))));
    }

    private static (double Lower, double Upper) Wilson(double successes, int trials)
    {
        if (trials <= 0)
            return (0, 1);
        const double z = 1.959963984540054;
        var proportion = successes / trials;
        var denominator = 1 + z * z / trials;
        var center = (proportion + z * z / (2 * trials)) / denominator;
        var margin = z * Math.Sqrt(
            proportion * (1 - proportion) / trials + z * z / (4d * trials * trials)) / denominator;
        return (Math.Max(0, center - margin), Math.Min(1, center + margin));
    }

    private static bool IsVictory(WorldTowerTrialSnapshot trial) =>
        trial.Outcome.Equals("Victory", StringComparison.Ordinal);

    private static EssenceBuildSnapshot ToEssenceBuild(
        RepresentativeEssenceBuildSnapshot build,
        string profileId) =>
        new(build.Id, profileId, build.Essences.Count, 0, build.Essences, build.Character);

    private static double Percentile(IReadOnlyList<double> ordered, double percentile)
    {
        if (ordered.Count == 0)
            return 0;
        if (ordered.Count == 1)
            return ordered[0];
        var index = percentile * (ordered.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper)
            return ordered[lower];
        var weight = index - lower;
        return ordered[lower] + (ordered[upper] - ordered[lower]) * weight;
    }

    private static double Round(double value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private sealed record RepresentativeLookup(
        string ProfileId,
        RepresentativeEssenceBuildSnapshot Build);

    private sealed record ReferenceCandidateEvidence(
        RegionOneReliabilityReferenceCandidateSnapshot Snapshot,
        RegionOneReliabilityFamilyEvidenceSnapshot Evidence);

    private sealed record ProgressionReferenceEvidence(
        double DifficultyFactor,
        RegionOneReliabilityFamilyEvidenceSnapshot Evidence);

    private sealed record ProgressionReferenceSearchEvidence(
        ProgressionReferenceEvidence? Selected,
        IReadOnlyList<RegionOneReliabilityReferenceCandidateSnapshot> Candidates);

    private sealed class EvaluationCounter
    {
        public int TotalTrials { get; set; }
    }
}
