using LegendsLegacy.Balance;

namespace EssenceSystem.Tests;

public sealed class RegionOneReliabilityPopulationPolicyAnalyzerTests
{
    [Fact]
    public void Policy_confirms_diagnostics_but_preserves_missing_family_contracts()
    {
        var studies = new[] { CreateStudy(1337), CreateStudy(2029), CreateStudy(8471) };

        var result = new RegionOneReliabilityPopulationPolicyAnalyzer().Analyze(studies);

        Assert.Equal(RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence, result.Verdict);
        Assert.False(result.ExpansionEligible);
        Assert.True(result.ProtocolCompatible);
        Assert.True(result.ProductionContentUnmodified);
        Assert.Equal([1337, 2029, 8471], result.Seeds);
        Assert.All(result.Faults, fault =>
        {
            Assert.Equal(3, fault.PassCount);
            Assert.Equal(0, fault.InconclusiveCount);
            Assert.True(fault.PhysicalReachReplicated);
            Assert.True(fault.DiagnosticRecoveryReplicated);
            Assert.Equal(RegionOneReliabilityPopulationPolicyVerdict.Confirmed, fault.DiagnosticVerdict);
        });
        Assert.All(
            result.Faults.Where(fault => fault.Fault is RegionOneReliabilityFaultKind.Regeneration
                or RegionOneReliabilityFaultKind.DistributedAttrition),
            fault =>
            {
                Assert.Equal(
                    RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence,
                    fault.FamilyContractVerdict);
                Assert.Equal(RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence, fault.Verdict);
            });
        var addPressure = result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.AddPressure);
        Assert.Equal(RegionOneReliabilityPopulationPolicyVerdict.Confirmed, addPressure.FamilyContractVerdict);
        Assert.Equal(RegionOneReliabilityPopulationPolicyVerdict.Confirmed, addPressure.Verdict);
        Assert.Null(result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.Health).FamilyContractVerdict);
        var cleanse = Assert.Single(result.UnsupportedFaults);
        Assert.Equal("CleanseDemand", cleanse.Fault);
        Assert.Equal(3, cleanse.UnavailableCount);
        Assert.Contains(result.Warnings, warning => warning.Contains("affected-family", StringComparison.Ordinal));
    }

    [Fact]
    public void Policy_requires_three_distinct_complete_protocol_compatible_populations()
    {
        var analyzer = new RegionOneReliabilityPopulationPolicyAnalyzer();
        var insufficient = analyzer.Analyze([CreateStudy(1337), CreateStudy(2029)]);

        Assert.Equal(RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence, insufficient.Verdict);
        Assert.All(insufficient.Faults, fault =>
            Assert.Equal(RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence, fault.Verdict));
        Assert.Throws<ArgumentException>(() => analyzer.Analyze([CreateStudy(1337), CreateStudy(1337)]));

        var unavailable = analyzer.Analyze([
            CreateStudy(1337),
            CreateStudy(2029),
            CreateStudy(8471, unavailable: [RegionOneReliabilityFaultKind.Regeneration])
        ]);
        var regeneration = unavailable.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.Regeneration);
        Assert.Equal(RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence, regeneration.DiagnosticVerdict);
        Assert.Equal(1, regeneration.UnavailableCount);

        var incompatible = analyzer.Analyze([
            CreateStudy(1337),
            CreateStudy(2029),
            CreateStudy(8471) with { AlgorithmVersion = RegionOneReliabilityStudyAnalyzer.AlgorithmVersion - 1 }
        ]);
        Assert.False(incompatible.ProtocolCompatible);
        Assert.Equal(RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence, incompatible.Verdict);

        var upstreamIncompatible = analyzer.Analyze([
            CreateStudy(1337),
            CreateStudy(2029),
            CreateStudy(8471, essenceBuildsPerProfile: 50)
        ]);
        Assert.False(upstreamIncompatible.ProtocolCompatible);
        Assert.Equal(RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence, upstreamIncompatible.Verdict);
        Assert.Contains(
            upstreamIncompatible.Warnings,
            warning => warning.Contains("upstream optimizer/cohort protocols", StringComparison.Ordinal));

        var missingProvenance = analyzer.Analyze([
            CreateStudy(1337),
            CreateStudy(2029),
            CreateStudy(8471, includePopulationProtocol: false)
        ]);
        Assert.False(missingProvenance.ProtocolCompatible);
        Assert.Equal(RegionOneReliabilityPopulationPolicyVerdict.InsufficientEvidence, missingProvenance.Verdict);
        Assert.Contains(
            missingProvenance.Warnings,
            warning => warning.Contains("lacks upstream", StringComparison.Ordinal));
    }

    [Fact]
    public void Unsupported_faults_block_expansion_after_diagnostics_and_family_contracts_are_confirmed()
    {
        var analyzer = new RegionOneReliabilityPopulationPolicyAnalyzer();
        var withUnsupported = analyzer.Analyze([
            CreateStudy(1337, allFamilyContractsConfirmed: true),
            CreateStudy(2029, allFamilyContractsConfirmed: true),
            CreateStudy(8471, allFamilyContractsConfirmed: true)
        ]);
        var withoutUnsupported = analyzer.Analyze([
            CreateStudy(1337, includeUnsupported: false, allFamilyContractsConfirmed: true),
            CreateStudy(2029, includeUnsupported: false, allFamilyContractsConfirmed: true),
            CreateStudy(8471, includeUnsupported: false, allFamilyContractsConfirmed: true)
        ]);

        Assert.Equal(RegionOneReliabilityPopulationPolicyVerdict.Confirmed, withUnsupported.Verdict);
        Assert.False(withUnsupported.ExpansionEligible);
        Assert.Equal(RegionOneReliabilityPopulationPolicyVerdict.Confirmed, withoutUnsupported.Verdict);
        Assert.True(withoutUnsupported.ExpansionEligible);
    }

    private static RegionOneReliabilityStudySnapshot CreateStudy(
        int seed,
        IReadOnlyList<RegionOneReliabilityFaultKind>? unavailable = null,
        bool includeUnsupported = true,
        bool allFamilyContractsConfirmed = false,
        bool includePopulationProtocol = true,
        int essenceBuildsPerProfile = 10)
    {
        unavailable ??= [];
        var faults = Enum.GetValues<RegionOneReliabilityFaultKind>()
            .Select(fault => CreateFault(fault, unavailable.Contains(fault), allFamilyContractsConfirmed))
            .ToArray();
        var verdict = faults.Any(fault => fault.DiagnosticVerdict == RegionOneReliabilityVerdict.Unavailable)
            ? RegionOneReliabilityVerdict.Unavailable
            : faults.All(fault => fault.Verdict == RegionOneReliabilityVerdict.Pass)
                ? RegionOneReliabilityVerdict.Pass
                : RegionOneReliabilityVerdict.Inconclusive;
        RegionOneReliabilityUnavailableFaultSnapshot[] unsupported = includeUnsupported
            ? [new("CleanseDemand", RegionOneReliabilityVerdict.Unavailable, "No player cleanse capability.")]
            : [];

        return new RegionOneReliabilityStudySnapshot(
            RegionOneReliabilityStudyAnalyzer.AlgorithmVersion,
            seed,
            new RegionOneReliabilityStudyOptions { Enabled = true },
            ProductionContentModified: false,
            ReleaseEligible: false,
            TotalCombatTrials: 1,
            verdict,
            References: [],
            faults,
            unsupported,
            Warnings: [])
        {
            PopulationProtocol = includePopulationProtocol
                ? CreatePopulationProtocol(essenceBuildsPerProfile)
                : null
        };
    }

    private static RegionOneReliabilityPopulationProtocolSnapshot CreatePopulationProtocol(
        int essenceBuildsPerProfile) => new(
        ProductionBalanceRunner.BalanceSchemaVersion,
        essenceBuildsPerProfile,
        PveBenchmarkRunner.ScoringVersion,
        EssenceBuildOptimizer.AlgorithmVersion,
        new EssenceOptimizerOptions(),
        RepresentativeBuildLibrary.AlgorithmVersion,
        new RepresentativeBuildOptions(),
        BuildCapabilityProfiler.AlgorithmVersion,
        BuildCapabilityProfiler.NormalizationVersion,
        "test-content-fingerprint",
        1,
        PartyFamilyBuilder.AlgorithmVersion,
        new PartyFamilyBuilderOptions(),
        WorldTowerContentAnalyzer.AlgorithmVersion,
        new WorldTowerAnalysisOptions());

    private static RegionOneReliabilityFaultSnapshot CreateFault(
        RegionOneReliabilityFaultKind fault,
        bool unavailable,
        bool allFamilyContractsConfirmed)
    {
        var diagnosticVerdict = unavailable
            ? RegionOneReliabilityVerdict.Unavailable
            : RegionOneReliabilityVerdict.Pass;
        var familyContractVerdict = fault switch
        {
            RegionOneReliabilityFaultKind.AddPressure => unavailable
                ? RegionOneReliabilityFamilyContractVerdict.InsufficientEvidence
                : RegionOneReliabilityFamilyContractVerdict.Pass,
            RegionOneReliabilityFaultKind.Regeneration or RegionOneReliabilityFaultKind.DistributedAttrition =>
                unavailable || !allFamilyContractsConfirmed
                    ? RegionOneReliabilityFamilyContractVerdict.InsufficientEvidence
                    : RegionOneReliabilityFamilyContractVerdict.Pass,
            _ => RegionOneReliabilityFamilyContractVerdict.NotApplicable
        };
        var overallVerdict = diagnosticVerdict != RegionOneReliabilityVerdict.Pass
            ? diagnosticVerdict
            : familyContractVerdict is RegionOneReliabilityFamilyContractVerdict.NotApplicable
                or RegionOneReliabilityFamilyContractVerdict.Pass
                ? RegionOneReliabilityVerdict.Pass
                : RegionOneReliabilityVerdict.Inconclusive;
        var familyResponseApplicable = familyContractVerdict !=
                                       RegionOneReliabilityFamilyContractVerdict.NotApplicable;
        var familyResponse = familyResponseApplicable
            ? new RegionOneReliabilityFamilyResponseSnapshot(
                true,
                familyContractVerdict == RegionOneReliabilityFamilyContractVerdict.Pass
                    ? PartyFamilyKind.IntendedBalanced
                    : null,
                null,
                null,
                null,
                null,
                null,
                null,
                familyContractVerdict == RegionOneReliabilityFamilyContractVerdict.Pass ? true : null,
                "Test response.")
            : RegionOneReliabilityFamilyResponseSnapshot.NotApplicable;

        return new RegionOneReliabilityFaultSnapshot(
            fault,
            Floor: 1,
            ExpectedParameterGroup: null,
            ExpectedObservedFailureMode: WorldTowerObservedFailureMode.Other,
            RecoveredParameterGroup: null,
            InjectedControl: "TestControl",
            FaultMultiplier: 1.4,
            ReferenceClearRate: 0.6,
            FaultClearRate: 0.4,
            ClearRateDrop: 0.2,
            DominantObservedFailureMode: WorldTowerObservedFailureMode.Other,
            DominantObservedFailureShare: 1,
            ExpectedObservedFailureShare: 1,
            RecoveryMethod: RegionOneReliabilityRecoveryMethod.PairedPhysicalTelemetry,
            PhysicalComparison: null!,
            familyResponse,
            InjectionReachedPhysicalTelemetry: !unavailable,
            FaultObservable: !unavailable,
            DiagnosticRecoveryMatched: !unavailable,
            CalibrationResponse: "Test response.",
            SuggestedCorrectionFactor: null,
            CorrectionVerifiedByFrozenReference: false,
            overallVerdict,
            Families: [],
            Warnings: [])
        {
            DiagnosticVerdict = diagnosticVerdict,
            FamilyContractVerdict = familyContractVerdict
        };
    }
}
