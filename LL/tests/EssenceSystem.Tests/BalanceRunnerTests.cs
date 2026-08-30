using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences.Definitions;
using Domain.Models.Items;
using LegendsLegacy.Balance;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class BalanceRunnerTests
{
    [Fact]
    public void Benchmark_confidence_audit_is_common_seeded_and_isolated_from_certification()
    {
        var runner = ProductionBalanceComposition.Create(FindApiContentRoot());
        var baselineOptions = CreateTestEliteCertificationOptions(searchOnly: true);
        var auditOptions = baselineOptions with
        {
            BenchmarkConfidenceAuditEnabled = true,
            BenchmarkConfidenceAuditCohortSize = 12,
            BenchmarkConfidenceAuditSeedCount = 2,
            BenchmarkConfidenceTargetScoreMargin = 5
        };
        BalanceRunReport Run(EliteCertificationOptions options) => runner.Run(new BalanceRunRequest(
            8471,
            "test-commit",
            EssenceBuildsPerProfile: 3,
            OptimizerOptions: CreateTestOptimizerOptions(),
            WorldTowerAnalysisOptions: new WorldTowerAnalysisOptions(1),
            EncounterCalibrationOptions: new EncounterCalibrationOptions(SearchIterations: 1),
            EncounterSpecificOptimizationOptions: CreateTestEncounterOptimizerOptions(),
            EliteCertificationOptions: options,
            ScalingValidationOptions: CreateTestScalingValidationOptions()));

        var baseline = Run(baselineOptions);
        var audited = Run(auditOptions);
        var certificationWithoutAudit = audited.EliteBuildCertification with
        {
            Options = audited.EliteBuildCertification.Options with
            {
                BenchmarkConfidenceAuditEnabled = false,
                BenchmarkConfidenceAuditCohortSize = baselineOptions.BenchmarkConfidenceAuditCohortSize,
                BenchmarkConfidenceAuditSeedCount = baselineOptions.BenchmarkConfidenceAuditSeedCount,
                BenchmarkConfidenceTargetScoreMargin = baselineOptions.BenchmarkConfidenceTargetScoreMargin
            },
            TotalBenchmarkConfidenceCombatExecutions = 0,
            BenchmarkConfidenceAudit = null
        };

        Assert.Equivalent(baseline.EliteBuildCertification, certificationWithoutAudit, strict: true);
        var audit = Assert.IsType<EliteBenchmarkConfidenceAuditSnapshot>(
            audited.EliteBuildCertification.BenchmarkConfidenceAudit);
        Assert.True(audit.CommonRandomNumbers);
        Assert.False(audit.CertificationEvidenceAffected);
        Assert.Equal(2, audit.SeedCount);
        Assert.Equal(5, audit.ScenarioCount);
        var referenceProfiles = Assert.IsAssignableFrom<IReadOnlyList<EliteBenchmarkReferenceProfileSnapshot>>(
            audit.ReferenceProfiles);
        var panelSizes = Assert.IsAssignableFrom<IReadOnlyList<EliteBenchmarkPanelSizeSnapshot>>(audit.PanelSizes);
        Assert.Equal(
            referenceProfiles.Sum(profile => profile.CohortSize) * audit.SeedCount * audit.ScenarioCount,
            audit.TotalCombatExecutions);
        Assert.Equal(audit.TotalCombatExecutions, audited.EliteBuildCertification.TotalBenchmarkConfidenceCombatExecutions);
        Assert.Equal(3, referenceProfiles.Count);
        Assert.Equal([1, 2], panelSizes.Select(panel => panel.SeedCount));
        var seedPanel = Assert.IsAssignableFrom<IReadOnlyList<int>>(audit.CombatSeedPanel);
        Assert.Equal(2, seedPanel.Count);
        Assert.Equal(2, seedPanel.Distinct().Count());
        Assert.Contains("panelSizes", JsonSerializer.Serialize(audit), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, audit.AnchorComparisons.Count);
        Assert.All(audit.Builds, build =>
        {
            Assert.True(double.IsFinite(build.ScoreStandardDeviation));
            Assert.True(build.RecommendedSeedCountForTargetMargin >= 2);
        });
        Assert.InRange(audit.BaselineToMeanSpearmanCorrelation, -1, 1);
        Assert.InRange(audit.MinimumBaselineTopKOverlap, 0, 1);
    }

    [Fact]
    public void Descriptor_separability_audit_is_deterministic_authoritative_and_isolated_from_certification()
    {
        var runner = ProductionBalanceComposition.Create(FindApiContentRoot());
        var baselineOptions = CreateTestEliteCertificationOptions(searchOnly: true);
        var auditOptions = baselineOptions with { DescriptorSeparabilityAuditEnabled = true };
        BalanceRunReport Run(EliteCertificationOptions options) => runner.Run(new BalanceRunRequest(
            8471,
            "test-commit",
            EssenceBuildsPerProfile: 3,
            OptimizerOptions: CreateTestOptimizerOptions(),
            WorldTowerAnalysisOptions: new WorldTowerAnalysisOptions(1),
            EncounterCalibrationOptions: new EncounterCalibrationOptions(SearchIterations: 1),
            EncounterSpecificOptimizationOptions: CreateTestEncounterOptimizerOptions(),
            EliteCertificationOptions: options,
            ScalingValidationOptions: CreateTestScalingValidationOptions()));

        var baseline = Run(baselineOptions);
        var audited = Run(auditOptions);
        var replay = Run(auditOptions);

        Assert.Equivalent(audited.EliteBuildCertification, replay.EliteBuildCertification, strict: true);
        var certificationWithoutAudit = audited.EliteBuildCertification with
        {
            Options = audited.EliteBuildCertification.Options with { DescriptorSeparabilityAuditEnabled = false },
            TotalDescriptorAuditCandidatesEvaluated = 0,
            DescriptorSeparabilityAudit = null
        };
        Assert.Equivalent(baseline.EliteBuildCertification, certificationWithoutAudit, strict: true);
        var audit = Assert.IsType<EliteDescriptorSeparabilityAuditSnapshot>(
            audited.EliteBuildCertification.DescriptorSeparabilityAudit);
        Assert.True(audit.AuthoritativeProductionBenchmark);
        Assert.False(audit.CertificationEvidenceAffected);
        Assert.True(audit.UniqueCandidatesEvaluated > 0);
        Assert.Equal(audit.UniqueCandidatesEvaluated, audited.EliteBuildCertification.TotalDescriptorAuditCandidatesEvaluated);
        Assert.True(audit.HighBasinCandidates > 0);
        Assert.True(audit.LowBasinCandidates > audit.HighBasinCandidates);
        Assert.Equal(3, audit.Anchors.Count);
        Assert.Equal(5, audit.DescriptorFamilies.Count);
        Assert.Contains(audit.DescriptorFamilies, descriptor => descriptor.DescriptorId == "scenario-shape");
        var coarseMechanic = Assert.Single(
            audit.DescriptorFamilies,
            descriptor => descriptor.DescriptorId == "mechanic-archetype");
        Assert.Equal(256, coarseMechanic.TheoreticalNicheCeiling);
        Assert.True(coarseMechanic.HardNicheCeilingPassed);
        Assert.True(coarseMechanic.DistinctNeighborhoodSignatures <= coarseMechanic.TheoreticalNicheCeiling);
        Assert.True(coarseMechanic.SeparabilityPassed);
        Assert.True(coarseMechanic.MapCandidatePassed);
        Assert.Contains("mechanic-archetype", audit.MapCandidateDescriptorIds);
        var collision = Assert.IsType<EliteDescriptorCollisionAuditSnapshot>(audit.CollisionAudit);
        Assert.Equal("mechanic-archetype", collision.ParentDescriptorId);
        Assert.Equal("mechanic-intensity-residual", collision.ResidualDescriptorId);
        Assert.Equal(4, collision.FeatureCount);
        Assert.Equal(81, collision.TheoreticalResidualNicheCeiling);
        Assert.True(collision.HardNicheCeilingPassed);
        Assert.True(collision.ParentNicheCandidates >= 0);
        Assert.True(collision.CandidateCount >= 0);
        Assert.Equal(
            collision.CandidateCount,
            collision.HighBasinCandidates + collision.LowBasinCandidates);
        Assert.Equal(
            collision.ParentNicheCandidates,
            collision.CandidateCount + collision.AmbiguousQualityCandidatesExcluded);
        Assert.True(collision.HighScoreFloor > collision.LowScoreCeiling);
        Assert.InRange(collision.DistinctResidualSignatures, 0, 81);
        Assert.InRange(collision.ExactSignaturePurity, 0, 1);
        Assert.InRange(collision.SingletonCandidateRate, 0, 1);
        Assert.InRange(collision.LeaveOneOutHighAccuracy, 0, 1);
        Assert.InRange(collision.LeaveOneOutLowAccuracy, 0, 1);
        Assert.InRange(collision.LeaveOneOutBalancedAccuracy, 0, 1);
        Assert.All(audit.DescriptorFamilies, descriptor =>
        {
            Assert.InRange(descriptor.ExactSignaturePurity, 0, 1);
            Assert.InRange(descriptor.SingletonCandidateRate, 0, 1);
            Assert.InRange(descriptor.NearestAnchorBalancedAccuracy, 0, 1);
        });
    }

    [Fact]
    public void Quality_diversity_island_is_deterministic_and_preserves_the_refined_baseline_ceiling()
    {
        var runner = ProductionBalanceComposition.Create(FindApiContentRoot());
        var islandOptions = CreateTestEliteCertificationOptions(searchOnly: true) with
        {
            RestartValleyBeamWidth = 0,
            RestartValleyBeamDepth = 0,
            RestartValleyCandidateBudget = 0,
            RestartValleyPrefilterLimitPerDepth = 0,
            CoordinatedMutationRate = 0,
            ExplorerArchiveSize = 0,
            QualityDiversityIslandCandidateBudgetPerProfile = 33
        };
        BalanceRunReport Run() => runner.Run(new BalanceRunRequest(
            8471,
            "test-commit",
            EssenceBuildsPerProfile: 3,
            OptimizerOptions: CreateTestOptimizerOptions(),
            WorldTowerAnalysisOptions: new WorldTowerAnalysisOptions(1),
            EncounterCalibrationOptions: new EncounterCalibrationOptions(SearchIterations: 1),
            EncounterSpecificOptimizationOptions: CreateTestEncounterOptimizerOptions(),
            EliteCertificationOptions: islandOptions,
            ScalingValidationOptions: CreateTestScalingValidationOptions()));

        var first = Run();
        var replay = Run();

        Assert.Equivalent(first.EliteBuildCertification, replay.EliteBuildCertification, strict: true);
        Assert.All(first.EliteBuildCertification.Profiles, profile =>
        {
            Assert.All(profile.Restarts, restart =>
            {
                Assert.Equal(33, restart.QualityDiversityIslandCandidatesEvaluated);
                Assert.Equal(32, restart.QualityDiversityIslandInitialCandidatesEvaluated);
                Assert.Equal(1, restart.QualityDiversityIslandDescendantsEvaluated);
                Assert.True(restart.QualityDiversityIslandNichesOccupied > 0);
                Assert.True(restart.QualityDiversityIslandNichesOccupied <= 25);
                Assert.InRange(restart.QualityDiversityIslandNicheReplacements, 0, 33);
                Assert.NotNull(restart.QualityDiversityIslandBestBuildId);
                Assert.Equal(profile.SlotCount, restart.QualityDiversityIslandBestEssenceIds!.Count);
                Assert.True(restart.BestScore >= restart.BaselineBestScore);
            });
        });
    }

    [Fact]
    public void Mechanic_archetype_island_is_deterministic_restart_local_and_preserves_the_refined_baseline_ceiling()
    {
        var runner = ProductionBalanceComposition.Create(FindApiContentRoot());
        var islandOptions = CreateTestEliteCertificationOptions(searchOnly: true) with
        {
            RestartValleyBeamWidth = 0,
            RestartValleyBeamDepth = 0,
            RestartValleyCandidateBudget = 0,
            RestartValleyPrefilterLimitPerDepth = 0,
            CoordinatedMutationRate = 0,
            ExplorerArchiveSize = 0,
            QualityDiversityIslandCandidateBudgetPerProfile = 0,
            MechanicArchetypeIslandCandidateBudgetPerProfile = 33
        };
        BalanceRunReport Run() => runner.Run(new BalanceRunRequest(
            8471,
            "test-commit",
            EssenceBuildsPerProfile: 3,
            OptimizerOptions: CreateTestOptimizerOptions(),
            WorldTowerAnalysisOptions: new WorldTowerAnalysisOptions(1),
            EncounterCalibrationOptions: new EncounterCalibrationOptions(SearchIterations: 1),
            EncounterSpecificOptimizationOptions: CreateTestEncounterOptimizerOptions(),
            EliteCertificationOptions: islandOptions,
            ScalingValidationOptions: CreateTestScalingValidationOptions()));

        var first = Run();
        var replay = Run();

        Assert.Equivalent(first.EliteBuildCertification, replay.EliteBuildCertification, strict: true);
        Assert.All(first.EliteBuildCertification.Profiles, profile =>
        {
            Assert.All(profile.Restarts, restart =>
            {
                Assert.Equal(33, restart.MechanicArchetypeIslandCandidatesEvaluated);
                Assert.Equal(32, restart.MechanicArchetypeIslandInitialCandidatesEvaluated);
                Assert.Equal(1, restart.MechanicArchetypeIslandDescendantsEvaluated);
                Assert.InRange(restart.MechanicArchetypeIslandNichesOccupied, 1, 256);
                Assert.InRange(restart.MechanicArchetypeIslandNicheReplacements, 0, 33);
                Assert.NotNull(restart.MechanicArchetypeIslandBestBuildId);
                Assert.Equal(profile.SlotCount, restart.MechanicArchetypeIslandBestEssenceIds!.Count);
                Assert.True(restart.BestScore >= restart.BaselineBestScore);
                Assert.Equal(0, restart.QualityDiversityIslandCandidatesEvaluated);
                Assert.InRange(restart.MechanicArchetypeHighNicheIslandCandidatesEvaluated, 0, 33);
                if (profile.SlotCount == 5)
                {
                    if (restart.MechanicArchetypeHighNichePresentInBaseline)
                        Assert.True(restart.MechanicArchetypeHighNicheBaselineBestScore > 0);
                    if (restart.MechanicArchetypeHighNicheIslandCandidatesEvaluated > 0)
                    {
                        Assert.True(restart.MechanicArchetypeHighNicheIslandBestScore > 0);
                        Assert.True(
                            restart.MechanicArchetypeHighNicheIslandBestScore
                            <= restart.MechanicArchetypeIslandBestScore);
                    }
                }
                else
                {
                    Assert.False(restart.MechanicArchetypeHighNichePresentInBaseline);
                    Assert.Equal(0, restart.MechanicArchetypeHighNicheBaselineBestScore);
                    Assert.Equal(0, restart.MechanicArchetypeHighNicheIslandCandidatesEvaluated);
                    Assert.Equal(0, restart.MechanicArchetypeHighNicheIslandBestScore);
                }
            });
        });
    }

    [Fact]
    public void Production_smoke_and_bridge_audit_are_deterministic_and_audit_isolated()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero));
        var runner = ProductionBalanceComposition.Create(FindApiContentRoot(), timeProvider);

        var first = runner.Run(new BalanceRunRequest(
            8471,
            "test-commit",
            EssenceBuildsPerProfile: 3,
            OptimizerOptions: CreateTestOptimizerOptions(),
            WorldTowerAnalysisOptions: new WorldTowerAnalysisOptions(1),
            EncounterCalibrationOptions: new EncounterCalibrationOptions(SearchIterations: 1),
            EncounterSpecificOptimizationOptions: CreateTestEncounterOptimizerOptions(),
            EliteCertificationOptions: CreateTestEliteCertificationOptions(searchOnly: true),
            ScalingValidationOptions: CreateTestScalingValidationOptions()));
        var bridgeOptions = CreateTestEliteCertificationOptions(searchOnly: true) with { BridgeAuditEnabled = true };
        var replay = runner.Run(new BalanceRunRequest(
            8471,
            "test-commit",
            EssenceBuildsPerProfile: 3,
            OptimizerOptions: CreateTestOptimizerOptions(),
            WorldTowerAnalysisOptions: new WorldTowerAnalysisOptions(1),
            EncounterCalibrationOptions: new EncounterCalibrationOptions(SearchIterations: 1),
            EncounterSpecificOptimizationOptions: CreateTestEncounterOptimizerOptions(),
            EliteCertificationOptions: bridgeOptions,
            ScalingValidationOptions: CreateTestScalingValidationOptions()));
        var bridgeReplay = runner.Run(new BalanceRunRequest(
            8471,
            "test-commit",
            EssenceBuildsPerProfile: 3,
            OptimizerOptions: CreateTestOptimizerOptions(),
            WorldTowerAnalysisOptions: new WorldTowerAnalysisOptions(1),
            EncounterCalibrationOptions: new EncounterCalibrationOptions(SearchIterations: 1),
            EncounterSpecificOptimizationOptions: CreateTestEncounterOptimizerOptions(),
            EliteCertificationOptions: bridgeOptions,
            ScalingValidationOptions: CreateTestScalingValidationOptions()));

        Assert.Equal(first.Metadata.Seed, replay.Metadata.Seed);
        Assert.Equal(first.Content, replay.Content);
        Assert.Equal(first.Simulation, replay.Simulation);
        Assert.Equivalent(first.GearPackages, replay.GearPackages, strict: true);
        Assert.Equivalent(first.EssenceBuilds, replay.EssenceBuilds, strict: true);
        Assert.Equivalent(first.Benchmarks, replay.Benchmarks, strict: true);
        Assert.Equivalent(first.BuildCapabilities, replay.BuildCapabilities, strict: true);
        Assert.Equivalent(first.PartyFamilies, replay.PartyFamilies, strict: true);
        Assert.Equivalent(first.PartyFamilyEvaluation, replay.PartyFamilyEvaluation, strict: true);
        Assert.Equivalent(first.CombatRatingHealth, replay.CombatRatingHealth, strict: true);
        Assert.Equivalent(first.Optimizer, replay.Optimizer, strict: true);
        Assert.Equivalent(first.RepresentativeBuilds, replay.RepresentativeBuilds, strict: true);
        Assert.Equivalent(first.EssenceMetaAnalysis, replay.EssenceMetaAnalysis, strict: true);
        Assert.Equivalent(first.PowerAnchors, replay.PowerAnchors, strict: true);
        Assert.Equivalent(first.ProgressionBands, replay.ProgressionBands, strict: true);
        Assert.Equivalent(first.WorldTowerAnalysis, replay.WorldTowerAnalysis, strict: true);
        Assert.Equivalent(first.EncounterCalibration, replay.EncounterCalibration, strict: true);
        Assert.Equivalent(first.EncounterSpecificOptimization, replay.EncounterSpecificOptimization, strict: true);
        var certificationWithoutAudit = replay.EliteBuildCertification with
        {
            Options = replay.EliteBuildCertification.Options with { BridgeAuditEnabled = false },
            TotalBridgeNodesEvaluated = 0,
            BridgeAudits = []
        };
        Assert.Equivalent(first.EliteBuildCertification, certificationWithoutAudit, strict: true);
        Assert.Equivalent(replay.EliteBuildCertification, bridgeReplay.EliteBuildCertification, strict: true);
        Assert.True(replay.EliteBuildCertification.Options.BridgeAuditEnabled);
        Assert.NotEqual(EliteCertificationVerdict.CertifiedElite, replay.EliteBuildCertification.Verdict);
        Assert.All(replay.EliteBuildCertification.Profiles, profile =>
            Assert.False(profile.CuratedComparison.RequirementSatisfied));
        Assert.Equal(
            replay.EliteBuildCertification.BridgeAudits!.Sum(audit => audit.LegalBridgeNodesEvaluated),
            replay.EliteBuildCertification.TotalBridgeNodesEvaluated);
        Assert.All(replay.EliteBuildCertification.BridgeAudits!, audit =>
        {
            Assert.Equal(audit.SubstitutionDistance + 1, audit.BestMaximinPath.Count);
            Assert.Equal(audit.SourceBuildId, audit.BestMaximinPath[0].BuildId);
            Assert.Equal(audit.TargetBuildId, audit.BestMaximinPath[^1].BuildId);
            Assert.Equal(audit.PathMinimumScore, audit.BestMaximinPath.Min(node => node.Score));
            Assert.True(audit.LegalBridgeNodesEvaluated >= audit.BestMaximinPath.Count);
        });
        Assert.True(first.EliteBuildCertification.Options.SearchOnly);
        Assert.Empty(first.EliteBuildCertification.Floors);
        Assert.Equal(0, first.EliteBuildCertification.TotalPartyGenomesEvaluated);
        Assert.NotEqual(EliteCertificationVerdict.CertifiedElite, first.EliteBuildCertification.Verdict);
        Assert.Contains(first.EliteBuildCertification.Warnings, warning =>
            warning.Contains("Search-only mode", StringComparison.Ordinal));
        Assert.All(first.EliteBuildCertification.Profiles.SelectMany(profile => profile.Restarts), restart =>
        {
            Assert.Equal(1, restart.ValleyBeamDepthReached);
            Assert.Equal(1, restart.ValleyBeamCandidatesEvaluated);
            Assert.False(restart.ValleyBeamBudgetExhausted);
            Assert.True(restart.ValleyBeamCandidatesGenerated > restart.ValleyBeamCandidatesEvaluated);
            Assert.True(restart.ValleyBeamCandidatesRejectedByPrefilter > 0);
            Assert.True(restart.CoordinatedMutationCandidatesEvaluated > 0);
            Assert.NotNull(restart.RawBestBuildId);
            Assert.NotNull(restart.BestBuildId);
            Assert.NotEmpty(restart.RawBestEssenceIds!);
            Assert.NotEmpty(restart.BestEssenceIds!);
        });
        Assert.True(first.EliteBuildCertification.Profiles
            .SelectMany(profile => profile.Restarts)
            .Sum(restart => restart.ExplorerContinuationCandidatesEvaluated) > 0);
        Assert.Equivalent(first.ScalingValidation, replay.ScalingValidation, strict: true);
        Assert.NotEqual(first.Metadata.RunId, replay.Metadata.RunId);
        Assert.True(first.Content.AbilityCount > 0);
        Assert.True(first.Content.EssenceCount > 1);
    }

    [Fact]
    public void Region_one_gear_packages_use_the_configured_floor_anchors()
    {
        var runner = ProductionBalanceComposition.Create(FindApiContentRoot());

        var report = runner.Run(new BalanceRunRequest(
            8471,
            "test-commit",
            EssenceBuildsPerProfile: 1,
            OptimizerOptions: CreateTestOptimizerOptions(),
            WorldTowerAnalysisOptions: new WorldTowerAnalysisOptions(1),
            EncounterCalibrationOptions: new EncounterCalibrationOptions(SearchIterations: 1),
            EncounterSpecificOptimizationOptions: CreateTestEncounterOptimizerOptions(),
            EliteCertificationOptions: CreateTestEliteCertificationOptions(),
            ScalingValidationOptions: CreateTestScalingValidationOptions()));

        Assert.Collection(
            report.GearPackages,
            floorOne => AssertGearPackage(
                floorOne,
                "T1_Rare_Exceptional_Balanced",
                "WorldTower.Region1.Floor1",
                Rarity.Rare),
            floorTen => AssertGearPackage(
                floorTen,
                "T1_Epic_Exceptional_Balanced",
                "WorldTower.Region1.Floor10",
                Rarity.Epic));
        Assert.True(
            report.GearPackages[1].CombatRating.RawOverall
            > report.GearPackages[0].CombatRating.RawOverall);
    }

    [Fact]
    public void Full_pipeline_generates_every_completed_balance_stage_from_one_request()
    {
        var runner = ProductionBalanceComposition.Create(FindApiContentRoot());

        var report = runner.Run(new BalanceRunRequest(
            8471,
            "test-commit",
            EssenceBuildsPerProfile: 5,
            OptimizerOptions: CreateTestOptimizerOptions(),
            EncounterCalibrationOptions: new EncounterCalibrationOptions(SearchIterations: 1),
            EncounterSpecificOptimizationOptions: CreateTestEncounterOptimizerOptions(),
            EliteCertificationOptions: CreateTestEliteCertificationOptions(),
            ScalingValidationOptions: CreateTestScalingValidationOptions(),
            BuildCapabilityOptions: new BuildCapabilityOptions(ProbeSeedCount: 3),
            PartyFamilyBuilderOptions: new PartyFamilyBuilderOptions(1),
            PartyFamilyEvaluationOptions: new PartyFamilyEvaluationOptions(Enabled: true, SimulationsPerParty: 1),
            EncounterScaleProbeOptions: new EncounterScaleProbeOptions
            {
                Enabled = true,
                Overrides =
                [
                    new EncounterScaleProbeOverride(
                        1,
                        10,
                        HealthMultiplier: 1.1,
                        OffenseMultiplier: 0.9,
                        DefenseMultiplier: 1.05,
                        ResistanceMultiplier: 0.95,
                        RegenerationMultiplier: 1.2,
                        GuardianAbilityHealingMultiplier: 1.3)
                ],
                PerformanceBudget = new EncounterScaleProbePerformanceBudget(
                    MaximumAllocatedBytesPerTrial: 1)
            }));

        Assert.Equal(ProductionBalanceRunner.BalanceSchemaVersion, report.Metadata.BalanceSchemaVersion);
        Assert.Equal(ProductionBalanceRunner.SmokeScenarioId, report.Simulation.ScenarioId);
        Assert.Equal(15, report.EssenceBuilds.Count);
        Assert.Collection(
            report.EssenceBuilds.GroupBy(build => build.ProfileId).OrderBy(group => group.Key),
            group => AssertProfile(group, "E4_RANDOM", 4, 30, "T1_Rare_Exceptional_Balanced"),
            group => AssertProfile(group, "E5_RANDOM", 5, 40, "T1_Rare_Exceptional_Balanced"),
            group => AssertProfile(group, "E6_RANDOM", 6, 50, "T1_Epic_Exceptional_Balanced"));
        Assert.All(report.EssenceBuilds, build =>
        {
            Assert.Equal(build.SlotCount, build.Essences.Count);
            Assert.Equal(
                build.Essences.Count,
                build.Essences.Select(essence => essence.EssenceId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count());
            Assert.Equal(
                build.Essences.Count,
                build.Essences.Select(essence => essence.SourceMonsterId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count());
            Assert.True(build.Character.UnlockedEssenceSlots >= build.SlotCount);
        });
        Assert.Equal(5, report.Benchmarks.Scenarios.Count);
        Assert.Equal(
            [
                "pve.short-single-target",
                "pve.sustained-single-target",
                "pve.high-incoming-damage",
                "pve.three-targets",
                "pve.attrition"
            ],
            report.Benchmarks.Scenarios.Select(scenario => scenario.Id));
        Assert.Equal(report.EssenceBuilds.Count, report.Benchmarks.Builds.Count);
        Assert.All(report.Benchmarks.Builds, build =>
        {
            Assert.Equal(5, build.Components.Count);
            Assert.InRange(build.AggregateScore, 0, 100);
            Assert.Equal(
                Math.Round(build.Components.Average(component => component.Score), 2),
                build.AggregateScore);
            Assert.All(build.Components, component =>
            {
                Assert.InRange(component.Score, 0, 100);
                Assert.InRange(component.Metrics.RemainingHealthRatio, 0, 1);
                Assert.InRange(component.Metrics.AverageFriendlyHealthDeficitRatio, 0, 1);
            });
        });
        Assert.All(report.Benchmarks.Builds.GroupBy(build => build.ProfileId), profile =>
        {
            var ranked = profile.OrderBy(build => build.ProfileRank).ToArray();
            Assert.Equal(Enumerable.Range(1, 5), ranked.Select(build => build.ProfileRank));
            Assert.Equal(profile.Max(build => build.AggregateScore), ranked[0].AggregateScore);
        });
        Assert.Contains(
            report.Benchmarks.Builds.GroupBy(build => build.ProfileId),
            profile => profile.Select(build => build.AggregateScore).Distinct().Count() > 1);
        Assert.Equal(BuildCapabilityProfiler.AlgorithmVersion, report.BuildCapabilities.AlgorithmVersion);
        Assert.Equal(BuildCapabilityProfiler.WaveResponseScenarioId, report.BuildCapabilities.WaveResponseScenarioId);
        Assert.Equal(3, report.BuildCapabilities.ProbeSeedCount);
        Assert.False(report.BuildCapabilities.PersistentCacheEnabled);
        Assert.True(report.BuildCapabilities.Profiles.Count >= report.EssenceBuilds.Count);
        Assert.All(report.EssenceBuilds, build =>
            Assert.Contains(report.BuildCapabilities.Profiles, profile => profile.BuildId == build.Id));
        Assert.All(report.BuildCapabilities.Profiles, profile =>
        {
            Assert.Equal(64, profile.CacheKey.Length);
            Assert.Equal(6, profile.Dimensions.Count);
            Assert.Equal(Enum.GetValues<BuildCapabilityDimension>(), profile.Dimensions.Select(value => value.Dimension));
            Assert.All(profile.Dimensions, dimension =>
            {
                Assert.True(dimension.RawValue >= 0);
                Assert.InRange(dimension.NormalizedScore, 0, 100);
                Assert.False(string.IsNullOrWhiteSpace(dimension.Unit));
            });
            Assert.True(profile.Mechanics.ObservationTicks > 0);
            Assert.True(profile.Mechanics.CleansesPer15Seconds >= 0);
            Assert.True(profile.Mechanics.DispelsPer15Seconds >= 0);
            foreach (var dimension in profile.Dimensions.Where(dimension =>
                         dimension.Dimension is BuildCapabilityDimension.MultiTarget
                             or BuildCapabilityDimension.PartySustain))
            {
                Assert.NotNull(dimension.SeedStandardDeviation);
                Assert.NotNull(dimension.SeedMinimum);
                Assert.NotNull(dimension.SeedMaximum);
                Assert.True(dimension.SeedMinimum <= dimension.SeedMaximum);
            }
            Assert.Contains(
                profile.Dimensions.Single(dimension => dimension.Dimension == BuildCapabilityDimension.MultiTarget)
                    .SupportingMetrics,
                metric => metric.Key == "wave_damage_per_second");
            Assert.Contains(
                profile.Dimensions.Single(dimension => dimension.Dimension == BuildCapabilityDimension.AttritionResilience)
                    .SupportingMetrics,
                metric => metric.Key == "average_health_deficit_ratio" && metric.Value is >= 0 and <= 1);
        });
        Assert.Contains(report.BuildCapabilities.Profiles, profile =>
            profile.Dimensions.Single(dimension =>
                dimension.Dimension == BuildCapabilityDimension.PartySustain).RawValue > 0);
        Assert.Contains(report.BuildCapabilities.Profiles, profile =>
            profile.Mechanics.StatusEffectsCleansed > 0
            || profile.Mechanics.StatusEffectsDispelled > 0
            || profile.Mechanics.StunApplications > 0
            || profile.Mechanics.FreezeApplications > 0
            || profile.Mechanics.SilenceApplications > 0
            || profile.Mechanics.SlowApplications > 0
            || profile.Mechanics.StaggerContributed > 0);
        Assert.Equal(report.WorldTowerAnalysis.Floors.Count, report.PartyFamilies.Floors.Count);
        Assert.Equal(1, report.PartyFamilies.Options.PartiesPerFamily);
        Assert.All(report.PartyFamilies.Floors, floor =>
        {
            Assert.Equal(Enum.GetValues<PartyFamilyKind>(), floor.ResponseProfile.Responses.Select(response => response.Family));
            Assert.Equal(
                report.WorldTowerAnalysis.Floors.Single(worldTowerFloor => worldTowerFloor.Floor == floor.Floor).RequiredSlots,
                floor.RequiredSlots);
            Assert.All(floor.Families, family => Assert.True(family.Parties.Count <= family.RequestedPartyCount));
            Assert.All(floor.Families.SelectMany(family => family.Parties), party =>
            {
                Assert.Equal(floor.RequiredSlots, party.Members.Count);
                Assert.Equal(64, party.Signature.Length);
            });
            Assert.All(floor.Families, family => Assert.Equal(
                family.Parties.Count,
                family.Parties.Select(party => party.Signature).Distinct(StringComparer.Ordinal).Count()));
            Assert.Equal(Enum.GetValues<PartyProgressionCohortKind>(), floor.ProgressionCohorts.Select(value => value.Cohort));
            Assert.Equal(
                floor.Families.Single(family => family.Family == PartyFamilyKind.IntendedBalanced).Parties,
                floor.ProgressionCohorts.Single(cohort => cohort.Cohort == PartyProgressionCohortKind.IntendedP75).Parties);
            Assert.All(floor.ProgressionCohorts, cohort =>
                Assert.All(cohort.Parties, party => Assert.Equal(floor.RequiredSlots, party.Members.Count)));
        });
        Assert.Equal(
            PartyFamilyDisposition.Advantaged,
            report.PartyFamilies.Floors.Single(floor => floor.Floor == 3).ResponseProfile.Responses
                .Single(response => response.Family == PartyFamilyKind.MultiTargetSpecialist).Disposition);
        Assert.Equal(
            PartyFamilyDisposition.Advantaged,
            report.PartyFamilies.Floors.Single(floor => floor.Floor == 7).ResponseProfile.Responses
                .Single(response => response.Family == PartyFamilyKind.SingleTargetSpecialist).Disposition);
        Assert.Equal(
            PartyFamilyDisposition.Advantaged,
            report.PartyFamilies.Floors.Single(floor => floor.Floor == 8).ResponseProfile.Responses
                .Single(response => response.Family == PartyFamilyKind.MechanicSpecialist).Disposition);
        Assert.True(report.PartyFamilyEvaluation.Options.Enabled);
        Assert.Equal(EliteCertificationProfile.Developer, report.PartyFamilyEvaluation.Options.Profile);
        Assert.Equal(
            PartyFamilyCertificationVerdict.DeveloperProfileOnly,
            report.PartyFamilyEvaluation.CertificationVerdict);
        Assert.Equal(report.PartyFamilies.Floors.Count, report.PartyFamilyEvaluation.Floors.Count);
        Assert.False(report.PartyFamilyEvaluation.ProductionContentModified);
        Assert.All(report.PartyFamilyEvaluation.Floors, floor =>
        {
            Assert.Equal(
                report.PartyFamilies.Floors.Single(partyFloor => partyFloor.Floor == floor.Floor).RequiredSlots,
                floor.RequiredSlots);
            Assert.Equal(PartyFamilyCertificationVerdict.DeveloperProfileOnly, floor.CertificationVerdict);
            Assert.NotEmpty(floor.CertificationBlockers);
            Assert.Equal(3, floor.ProgressionCohorts.Count);
            Assert.Equal(
                "reused-intended-balanced-evaluation",
                floor.ProgressionCohorts.Single(cohort => cohort.Cohort == PartyProgressionCohortKind.IntendedP75).EvidenceSource);
            Assert.All(
                floor.ProgressionCohorts.Where(cohort => cohort.Cohort != PartyProgressionCohortKind.IntendedP75),
                cohort => Assert.Equal("capability-profile-constrained-progression-sampler", cohort.EvidenceSource));
            Assert.Contains(
                floor.ProgressionOrdering.Verdict,
                new[]
                {
                    PartyFamilyEvaluationVerdict.Pass,
                    PartyFamilyEvaluationVerdict.Review,
                    PartyFamilyEvaluationVerdict.Fail
                });
            Assert.Contains(floor.Families, family =>
                family.Family == PartyFamilyKind.IntendedBalanced
                && family.EvidenceSource == "production-world-tower-combat"
                && family.TrialCount > 0);
            Assert.All(floor.Families.Where(family => family.TrialCount > 0), family =>
            {
                Assert.InRange(family.ObservedClearRate, 0, 1);
                Assert.True(family.ConfidenceLowerBound <= family.ConfidenceUpperBound);
            });
        });
        Assert.True(report.EncounterScaleProbes.Options.Enabled);
        Assert.False(report.EncounterScaleProbes.ProductionContentModified);
        Assert.False(report.EncounterScaleProbes.ReleaseEligible);
        Assert.True(report.EncounterScaleProbes.TotalCombatTrials > 0);
        Assert.True(report.EncounterScaleProbes.TotalSimulatedTicks > 0);
        Assert.True(report.EncounterScaleProbes.TotalMeasuredWallTimeMilliseconds > 0);
        Assert.True(report.EncounterScaleProbes.TotalAllocatedBytes > 0);
        Assert.True(report.EncounterScaleProbes.ProcessPeakWorkingSetBytes > 0);
        Assert.True(report.EncounterScaleProbes.ManagedHeapHighWaterEstimateBytes > 0);
        Assert.True(report.EncounterScaleProbes.SimulatedTicksPerSecond > 0);
        Assert.False(string.IsNullOrWhiteSpace(report.EncounterScaleProbes.PerformanceEnvironment.FrameworkDescription));
        Assert.False(string.IsNullOrWhiteSpace(report.EncounterScaleProbes.PerformanceEnvironment.OperatingSystemDescription));
        Assert.True(report.EncounterScaleProbes.PerformanceEnvironment.LogicalProcessorCount > 0);
        Assert.True(report.EncounterScaleProbes.PerformanceEnvironment.StopwatchFrequency > 0);
        Assert.Equal(
            EncounterScaleProbePerformanceAssessment.OutsideBudget,
            report.EncounterScaleProbes.PerformanceBudgetAssessment);
        Assert.Equal(report.WorldTowerAnalysis.Floors.Count, report.EncounterScaleProbes.Floors.Count);
        Assert.All(report.EncounterScaleProbes.Floors, floor =>
        {
            Assert.Equal([5, 10, 15], floor.Variants.Select(variant => variant.PlayerCount));
            Assert.Equal(
                report.WorldTowerAnalysis.Floors.Single(value => value.Floor == floor.Floor).RequiredSlots,
                floor.AuthoredPlayerCount);
            var authored = Assert.Single(floor.Variants, variant => variant.IsAuthoredPlayerCount);
            Assert.Equal(EncounterScaleProbeAssessment.AuthoredBaseline, authored.Assessment);
            Assert.Equal("reused-authored-party-family", authored.EvidenceSource);
            Assert.Equal(EncounterScaleProbePerformanceAssessment.NotMeasured, authored.Performance.BudgetAssessment);
            Assert.All(floor.Variants, variant =>
            {
                Assert.Equal(1, variant.PartyCount);
                Assert.Equal(1, variant.TrialCount);
                Assert.True(variant.ConfidenceLowerBound <= variant.ConfidenceUpperBound);
                Assert.Equal(
                    Math.Round(Math.Pow(variant.PlayerCount / (double)floor.AuthoredPlayerCount, 0.85), 4),
                    variant.HealthFormulaRatio);
                Assert.Equal(
                    Math.Round(
                        (1 + 0.05 * (variant.PlayerCount - 1))
                        / (1 + 0.05 * (floor.AuthoredPlayerCount - 1)),
                        4),
                    variant.OffenseFormulaRatio);
                Assert.Equal(
                    Math.Round(Math.Pow(variant.PlayerCount / (double)floor.AuthoredPlayerCount, 0.25), 4),
                    variant.DurabilityFormulaRatio);
            });
            Assert.All(floor.Variants.Where(variant => !variant.IsAuthoredPlayerCount), variant =>
            {
                Assert.True(variant.Performance.Measured);
                Assert.True(variant.Performance.WallTimeMilliseconds > 0);
                Assert.True(variant.Performance.AllocatedBytes > 0);
                Assert.True(variant.Performance.SimulatedTicksPerSecond > 0);
                Assert.Equal(EncounterScaleProbePerformanceAssessment.OutsideBudget, variant.Performance.BudgetAssessment);
                Assert.NotEmpty(variant.Performance.BudgetViolations);
            });
        });
        Assert.Equal(
            new EncounterScaleProbeOverride(
                1,
                10,
                HealthMultiplier: 1.1,
                OffenseMultiplier: 0.9,
                DefenseMultiplier: 1.05,
                ResistanceMultiplier: 0.95,
                RegenerationMultiplier: 1.2,
                GuardianAbilityHealingMultiplier: 1.3),
            report.EncounterScaleProbes.Floors.Single(floor => floor.Floor == 1).Variants
                .Single(variant => variant.PlayerCount == 10).AppliedOverride);
        Assert.False(report.RegionOneReliabilityStudy.Options.Enabled);
        Assert.Equal(RegionOneReliabilityVerdict.Disabled, report.RegionOneReliabilityStudy.Verdict);
        Assert.Equal(0, report.RegionOneReliabilityStudy.TotalCombatTrials);
        Assert.True(report.RegionOneReliabilityStudy.CleanseDemandPrecondition.EvidenceAvailable);
        Assert.Equal(
            report.BuildCapabilities.Profiles.Count,
            report.RegionOneReliabilityStudy.CleanseDemandPrecondition.ProfiledBuildCount);
        Assert.True(report.RegionOneReliabilityStudy.CleanseDemandPrecondition.FloorRequiresCleanse);
        Assert.False(report.RegionOneReliabilityStudy.CleanseDemandPrecondition.PrerequisitesSatisfied);
        Assert.False(report.RegionOneReliabilityStudy.CleanseDemandPrecondition.InjectionImplemented);
        var populationProtocol = Assert.IsType<RegionOneReliabilityPopulationProtocolSnapshot>(
            report.RegionOneReliabilityStudy.PopulationProtocol);
        Assert.Equal(ProductionBalanceRunner.BalanceSchemaVersion, populationProtocol.BalanceSchemaVersion);
        Assert.Equal(5, populationProtocol.EssenceBuildsPerProfile);
        Assert.Equal(report.Benchmarks.ScoringVersion, populationProtocol.PveBenchmarkScoringVersion);
        Assert.Equal(report.Optimizer.AlgorithmVersion, populationProtocol.OptimizerAlgorithmVersion);
        Assert.Equal(report.Optimizer.Options, populationProtocol.OptimizerOptions);
        Assert.Equal(report.RepresentativeBuilds.Options, populationProtocol.RepresentativeBuildOptions);
        Assert.Equal(report.BuildCapabilities.ContentFingerprint, populationProtocol.CapabilityContentFingerprint);
        Assert.Equal(report.BuildCapabilities.ProbeSeedCount, populationProtocol.CapabilityProbeSeedCount);
        Assert.Equal(report.PartyFamilies.Options, populationProtocol.PartyFamilyBuilderOptions);
        Assert.Equal(report.WorldTowerAnalysis.Options, populationProtocol.WorldTowerAnalysisOptions);
        Assert.Equal(report.EssenceBuilds.Count, report.CombatRatingHealth.ObservationCount);
        Assert.Equal(report.EssenceBuilds.Count, report.CombatRatingHealth.Predictions.Count);
        Assert.Equal(3, report.CombatRatingHealth.DistinctDisplayCrCount);
        Assert.All(report.CombatRatingHealth.Bands, band =>
        {
            Assert.True(band.P10Performance <= band.MedianPerformance);
            Assert.True(band.MedianPerformance <= band.P90Performance);
            Assert.Equal(
                Math.Round(band.P90Performance - band.P10Performance, 2),
                band.PerformanceSpread);
        });
        Assert.Collection(
            report.Optimizer.Profiles,
            profile => AssertOptimizerProfile(profile, "E4_OPTIMIZER", 4),
            profile => AssertOptimizerProfile(profile, "E5_OPTIMIZER", 5),
            profile => AssertOptimizerProfile(profile, "E6_OPTIMIZER", 6));
        Assert.Equal(6, report.Optimizer.AlgorithmVersion);
        Assert.All(report.Optimizer.Profiles, profile =>
            Assert.True(profile.Generations.Sum(generation => generation.CoordinatedMutationBirths) > 0));
        Assert.Equal(9, report.RepresentativeBuilds.Profiles.Count);
        Assert.Collection(
            report.RepresentativeBuilds.Profiles,
            profile => AssertRepresentativeProfile(profile, "E4_P50", 4, 50),
            profile => AssertRepresentativeProfile(profile, "E4_P75", 4, 75),
            profile => AssertRepresentativeProfile(profile, "E4_P90", 4, 90),
            profile => AssertRepresentativeProfile(profile, "E5_P50", 5, 50),
            profile => AssertRepresentativeProfile(profile, "E5_P75", 5, 75),
            profile => AssertRepresentativeProfile(profile, "E5_P90", 5, 90),
            profile => AssertRepresentativeProfile(profile, "E6_P50", 6, 50),
            profile => AssertRepresentativeProfile(profile, "E6_P75", 6, 75),
            profile => AssertRepresentativeProfile(profile, "E6_P90", 6, 90));
        Assert.Collection(
            report.PowerAnchors.Anchors,
            anchor => AssertPowerAnchor(
                anchor,
                "WorldTower.Region1.Start",
                1,
                "T1_Rare_Exceptional_Balanced",
                "E4_P75",
                187),
            anchor => AssertPowerAnchor(
                anchor,
                "WorldTower.Region1.End",
                10,
                "T1_Epic_Exceptional_Balanced",
                "E6_P75",
                213));
        var progressionBand = Assert.Single(report.ProgressionBands.Bands);
        Assert.Equal("WorldTower.Region1", progressionBand.Definition.Id);
        Assert.Equal(ProgressionCurveKind.SmoothStep, progressionBand.Curve);
        Assert.Equal(Enumerable.Range(1, 10), progressionBand.Floors.Select(floor => floor.Floor));
        Assert.Equal(progressionBand.StartBenchmarkPower, progressionBand.Floors[0].TargetBenchmarkPower);
        Assert.Equal(progressionBand.EndBenchmarkPower, progressionBand.Floors[^1].TargetBenchmarkPower);
        Assert.Equal("WorldTower.Region1.Start", progressionBand.Floors[0].AnchorId);
        Assert.Equal("WorldTower.Region1.End", progressionBand.Floors[^1].AnchorId);
        Assert.All(progressionBand.Floors.Skip(1), (floor, index) =>
            Assert.True(floor.TargetBenchmarkPower >= progressionBand.Floors[index].TargetBenchmarkPower));
        Assert.Equal(Enumerable.Range(1, 10), report.WorldTowerAnalysis.Floors.Select(floor => floor.Floor));
        Assert.Equal("E4_P75", report.WorldTowerAnalysis.Floors[0].RepresentativeProfileId);
        Assert.Equal("E6_P75", report.WorldTowerAnalysis.Floors[^1].RepresentativeProfileId);
        Assert.Equal(
            report.PowerAnchors.Anchors[0].CombatRating.MedianDisplayCr,
            report.WorldTowerAnalysis.Floors[0].RecommendedDisplayCr);
        Assert.Equal(
            report.PowerAnchors.Anchors[^1].CombatRating.MedianDisplayCr,
            report.WorldTowerAnalysis.Floors[^1].RecommendedDisplayCr);
        Assert.All(report.WorldTowerAnalysis.Floors.Skip(1), (floor, index) =>
            Assert.True(floor.RecommendedDisplayCr >= report.WorldTowerAnalysis.Floors[index].RecommendedDisplayCr));
        Assert.All(report.WorldTowerAnalysis.Floors, floor =>
        {
            Assert.Equal(10, floor.Trials.Count);
            Assert.Equal(floor.Trials.Count, floor.TerminalFailureCounts.Values.Sum());
            Assert.Equal(floor.Trials.Count, floor.PrimaryObservedFailureModeCounts.Values.Sum());
            Assert.True(floor.P10DurationTicks <= floor.MedianDurationTicks);
            Assert.True(floor.MedianDurationTicks <= floor.P90DurationTicks);
            Assert.All(floor.Trials, trial =>
            {
                Assert.Equal(floor.RequiredSlots, trial.BuildIds.Count);
                Assert.Equal(
                    Enumerable.Range(0, floor.RequiredSlots).Select(index => index / 5 + 1),
                    trial.PartyNumbers);
                Assert.Equal(WorldTowerContentAnalyzer.FailureRuleVersion, trial.FailureDiagnostic.RuleVersion);
                Assert.InRange(trial.FailureDiagnostic.Confidence, 0, 1);
                Assert.InRange(trial.GuardianHealthRemainingRatio, 0, 1);
                Assert.True(trial.HostileDamagePerSecond >= 0);
                Assert.True(trial.GuardianPassiveRegeneration >= 0);
                Assert.True(trial.GuardianAbilityHealing >= 0);
                Assert.Equal(
                    trial.GuardianPassiveRegeneration + trial.GuardianAbilityHealing,
                    trial.GuardianTotalSelfSustain);
                Assert.True(trial.GuardianDamageTakenPerSecond >= 0);
                Assert.True(trial.PrimaryTargetDamageTaken >= 0);
                Assert.True(trial.PartySustainPerSecond >= 0);
                Assert.True(trial.PeakActiveHostileCombatants >= 1);
                Assert.True(trial.PeakActiveHostileSummons >= 0);
                Assert.True(trial.CleansedEffects >= 0);
                Assert.True(trial.DispelledEffects >= 0);
                Assert.True(trial.FriendlyActionDeniedTicks >= 0);
                Assert.True(trial.HostileActionDeniedTicks >= 0);
                Assert.NotEmpty(trial.GuardianRegenerationTimeline);
                Assert.Equal(0, trial.GuardianRegenerationTimeline[0].Tick);
                Assert.Equal(
                    trial.GuardianRegenerationTimeline.OrderBy(point => point.Tick).Select(point => point.Tick),
                    trial.GuardianRegenerationTimeline.Select(point => point.Tick));
                if (trial.Outcome == "Victory")
                {
                    Assert.Equal(WorldTowerTerminalFailure.None, trial.FailureDiagnostic.TerminalFailure);
                    Assert.Equal(WorldTowerObservedFailureMode.None, trial.FailureDiagnostic.PrimaryObservedFailureMode);
                }
                else
                {
                    Assert.NotEqual(WorldTowerTerminalFailure.None, trial.FailureDiagnostic.TerminalFailure);
                    Assert.NotEmpty(trial.FailureDiagnostic.Evidence);
                }
            });
            Assert.InRange(floor.ObservedClearRate, 0, 1);
            Assert.InRange(floor.AverageRemainingHealthRatio, 0, 1);
            Assert.True(floor.RecommendedDisplayCr > 0);
            Assert.False(string.IsNullOrWhiteSpace(floor.GuardianAbilityProfileId));
        });
        Assert.Contains(
            report.WorldTowerAnalysis.Floors.SelectMany(floor => floor.Trials),
            trial => trial.GuardianDamageTakenPerSecond > 0);
        Assert.Equal(80, report.EssenceMetaAnalysis.Essences.Count);
        Assert.Equal(30, report.EssenceMetaAnalysis.EvaluatedBuildCount);
        Assert.Equal(2_000, report.EssenceMetaAnalysis.SimulatorEvidence.BattlesRun);
        Assert.Equal(3, report.EssenceMetaAnalysis.PercentileCohortSizes["P95"]);
        Assert.All(report.EssenceMetaAnalysis.Essences, essence =>
        {
            Assert.InRange(essence.OverallUsage, 0, 1);
            Assert.InRange(essence.P95Usage, 0, 1);
        });
        Assert.All(report.EssenceMetaAnalysis.PairSynergies, pair =>
            Assert.True(pair.Appearances >= report.EssenceMetaAnalysis.Options.MinimumPairAppearances));
        Assert.Equal(Enumerable.Range(1, 10), report.EncounterCalibration.Floors.Select(floor => floor.Floor));
        Assert.False(report.EncounterCalibration.ProductionContentModified);
        Assert.All(report.EncounterCalibration.Floors, floor =>
        {
            Assert.InRange(floor.RecommendedDifficultyMultiplier, 0.25, 2);
            Assert.InRange(floor.SuggestedClearRate, 0, 1);
            Assert.NotEmpty(floor.Evaluations);
        });
        Assert.Equal(Enumerable.Range(1, 10), report.EncounterSpecificOptimization.Floors.Select(floor => floor.Floor));
        Assert.Equal(
            report.EncounterSpecificOptimization.Floors.Sum(floor => floor.CandidateCount),
            report.EncounterSpecificOptimization.TotalCandidateEvaluations);
        Assert.All(report.EncounterSpecificOptimization.Floors, floor =>
        {
            Assert.Equal(2, floor.RetainedBuilds.Count);
            Assert.InRange(floor.SpecializedClearRate, 0, 1);
            Assert.InRange(floor.SpecializedMeanPairwiseSimilarity, 0, 1);
            Assert.Equal(
                report.EncounterCalibration.Floors.Single(calibration => calibration.Floor == floor.Floor).SuggestedClearRate,
                floor.GenericClearRate);
        });
        Assert.False(report.EliteBuildCertification.ProductionContentModified);
        Assert.Equal(21, report.EliteBuildCertification.AlgorithmVersion);
        Assert.NotEqual(EliteCertificationVerdict.CertifiedElite, report.EliteBuildCertification.Verdict);
        Assert.Equal(3, report.EliteBuildCertification.Profiles.Count);
        Assert.Equal(Enumerable.Range(1, 10), report.EliteBuildCertification.Floors.Select(floor => floor.Floor));
        Assert.All(report.EliteBuildCertification.Profiles, profile =>
        {
            Assert.True(profile.LegalSearchSpaceSize > profile.UniqueCandidatesEvaluated);
            Assert.Equal(2, profile.Restarts.Count);
            Assert.All(profile.Restarts, restart =>
            {
                Assert.InRange(restart.GenerationsExecuted, 1, 1);
                Assert.True(restart.RawBestScore <= restart.BestScore);
                Assert.InRange(restart.LocalRefinementPasses, 0, 2);
                Assert.Equal(2, restart.RefinementSeedsEvaluated);
                Assert.Equal(
                    restart.LocalCandidatesEvaluated,
                    restart.OneSwapCandidatesEvaluated + restart.TwoSwapCandidatesEvaluated);
                Assert.InRange(restart.TwoSwapCandidatesEvaluated, 0, 1);
                Assert.NotNull(restart.RawBestBuildId);
                Assert.NotNull(restart.BestBuildId);
                Assert.Equal(profile.SlotCount, restart.RawBestEssenceIds!.Count);
                Assert.Equal(profile.SlotCount, restart.BestEssenceIds!.Count);
                Assert.InRange(restart.DistanceFromStrongestRestart, 0, profile.SlotCount);
                Assert.Equal(0, restart.ValleyBeamDepthReached);
                Assert.Equal(0, restart.ValleyBeamCandidatesEvaluated);
                Assert.False(restart.ValleyBeamBudgetExhausted);
                Assert.Equal(0, restart.ValleyBeamCandidatesGenerated);
                Assert.Equal(0, restart.ValleyBeamCandidatesRejectedByPrefilter);
                Assert.Equal(1, restart.StratifiedPortfolioCandidatesEvaluated);
                Assert.NotNull(restart.BaselineBestBuildId);
                Assert.Equal(profile.SlotCount, restart.BaselineBestEssenceIds!.Count);
                Assert.True(restart.BestScore >= restart.BaselineBestScore);
            });
            Assert.True(profile.P95TargetScore <= profile.P99TargetScore);
            Assert.True(profile.P99TargetScore <= profile.BestScore);
            Assert.True(profile.LocalChallenge.CompleteForConfiguredDepth);
            Assert.NotEmpty(profile.Finalists);
        });
        Assert.All(report.EliteBuildCertification.Floors, floor =>
        {
            Assert.Equal(1, floor.PartyGenomesEvaluated);
            Assert.Equal(1, floor.PartyGenomeSearchSpaceSize);
            Assert.True(floor.PartyOptimizationComplete);
            Assert.NotEmpty(floor.P95CohortBuildIds);
            Assert.NotEmpty(floor.P99CohortBuildIds);
            Assert.False(floor.P95CohortBuildIds.SequenceEqual(floor.P99CohortBuildIds));
            Assert.InRange(floor.CertifiedP95.ClearRate, 0, 1);
            Assert.InRange(floor.CertifiedP99.ClearRate, 0, 1);
            Assert.InRange(floor.SpecializedParty.ConfidenceLowerBound, 0, floor.SpecializedParty.ConfidenceUpperBound);
        });
        Assert.Equal(Enumerable.Range(1, 10), report.ScalingValidation.Floors.Select(floor => floor.Floor));
        Assert.False(report.ScalingValidation.ProductionContentModified);
        Assert.Equal(
            report.ScalingValidation.Floors.Sum(floor =>
                floor.HoldoutEvaluation.TrialCount
                + 7 * report.ScalingValidation.Options.HoldoutSeeds
                    * report.ScalingValidation.Options.ProbeSimulationsPerSeed),
            report.ScalingValidation.TotalCombatTrials);
        Assert.All(report.ScalingValidation.Floors, floor =>
        {
            Assert.Equal(2, floor.HoldoutSeedCount);
            Assert.InRange(floor.ConfidenceLowerBound, 0, floor.ConfidenceUpperBound);
            Assert.InRange(floor.ConfidenceUpperBound, floor.ConfidenceLowerBound, 1);
            Assert.InRange(floor.SeedClearRateStandardDeviation, 0, 1);
            Assert.InRange(floor.SeedClearRateRange, 0, 1);
        });
    }

    [Fact]
    public void Representative_library_interpolates_generated_population_percentiles()
    {
        var candidates = EssenceBuildGenerator.InitialSlotCounts
            .SelectMany(slotCount => new[] { 10d, 20d, 30d, 40d, 50d }
                .Select((score, index) => CreateEvaluatedCandidate(slotCount, index + 1, score)))
            .ToArray();

        var result = new RepresentativeBuildLibrary().Create(
            candidates,
            8471,
            diversityPenalty: 0,
            new RepresentativeBuildOptions(2));

        Assert.Equal(9, result.Profiles.Count);
        foreach (var slotCount in EssenceBuildGenerator.InitialSlotCounts)
        {
            Assert.Equal(30, Assert.Single(result.Profiles, profile =>
                profile.Id == $"E{slotCount}_P50").TargetScore);
            Assert.Equal(40, Assert.Single(result.Profiles, profile =>
                profile.Id == $"E{slotCount}_P75").TargetScore);
            Assert.Equal(46, Assert.Single(result.Profiles, profile =>
                profile.Id == $"E{slotCount}_P90").TargetScore);
        }
    }

    [Fact]
    public void Essence_meta_analysis_calculates_percentile_usage_and_additive_pair_delta()
    {
        var definitions = new[]
        {
            CreateMetaEssence("essence.a"),
            CreateMetaEssence("essence.b"),
            CreateMetaEssence("essence.c")
        };
        var candidates = new[]
        {
            CreateMetaCandidate("build-1", 10, "essence.a", "essence.b"),
            CreateMetaCandidate("build-2", 20, "essence.a", "essence.c"),
            CreateMetaCandidate("build-3", 30, "essence.b", "essence.c"),
            CreateMetaCandidate("build-4", 80, "essence.a", "essence.b")
        };
        var simulator = CreateMetaSimulatorEvidence(
            new AbilityBalanceEssenceResult(
                "essence.a",
                "Essence A",
                1,
                100,
                50,
                50,
                0,
                0.5,
                0,
                0.03,
                0.45,
                0.55,
                100,
                500,
                500,
                "InsufficientData"));

        var result = new EssenceMetaAnalyzer(new FakeEssenceDefinitionRepository(definitions)).Analyze(
            candidates,
            simulator,
            new EssenceMetaAnalysisOptions(
                SimulatorBattleCount: 100,
                MinimumPairAppearances: 2,
                SynergyDeltaThreshold: 3,
                MandatoryP95UsageThreshold: 0.8,
                UnderusedOverallUsageThreshold: 0));

        Assert.Equal(2, result.PercentileCohortSizes["P50"]);
        Assert.Equal(1, result.PercentileCohortSizes["P95"]);
        var essenceA = Assert.Single(result.Essences, essence => essence.EssenceId == "essence.a");
        Assert.Equal(0.5, essenceA.P50Usage);
        Assert.Equal(1, essenceA.P95Usage);
        Assert.Equal(100, essenceA.AdminSimulatorBattles);
        Assert.Equal(0.03, essenceA.AdminAdjustedScoreDelta);
        var pair = Assert.Single(result.PairSynergies);
        Assert.Equal("essence.a", pair.FirstEssenceId);
        Assert.Equal("essence.b", pair.SecondEssenceId);
        Assert.Equal(45, pair.ObservedMeanPerformance);
        Assert.Equal(41.67, pair.ExpectedMeanPerformance);
        Assert.Equal(3.33, pair.SynergyDelta);
        Assert.Equal(EssencePairSynergyClassification.Strong, pair.Classification);
        Assert.Contains(result.Warnings, warning => warning.Kind == EssenceMetaWarningKind.MandatoryEssence);
        Assert.Contains(result.Warnings, warning => warning.Kind == EssenceMetaWarningKind.SuspiciousSynergy);
    }

    [Fact]
    public void Essence_meta_analysis_rejects_high_coverage_simulator_evidence_with_no_discrimination()
    {
        var definitions = new[]
        {
            CreateMetaEssence("essence.a"),
            CreateMetaEssence("essence.b")
        };
        var candidates = new[]
        {
            CreateMetaCandidate("build-1", 10, "essence.a"),
            CreateMetaCandidate("build-2", 20, "essence.b")
        };
        AbilityBalanceEssenceResult Evidence(string id) => new(
            id,
            id,
            1,
            1_264,
            632,
            632,
            0,
            0.5,
            0,
            0,
            0.47,
            0.53,
            100,
            500,
            500,
            "Healthy");

        var result = new EssenceMetaAnalyzer(new FakeEssenceDefinitionRepository(definitions)).Analyze(
            candidates,
            CreateMetaSimulatorEvidence(Evidence("essence.a"), Evidence("essence.b")),
            new EssenceMetaAnalysisOptions(MinimumPairAppearances: 2));

        Assert.False(result.SimulatorEvidence.DiscriminationPassed);
        Assert.Equal(1, result.SimulatorEvidence.DistinctEssenceScoreCount);
        Assert.Equal(0, result.SimulatorEvidence.EssenceScoreRange);
        Assert.All(result.Essences, essence => Assert.Equal("NoDiscrimination", essence.AdminClassification));
        Assert.Contains(result.Warnings, warning => warning.Kind == EssenceMetaWarningKind.SimulatorNoDiscrimination);
    }

    [Fact]
    public void Encounter_calibration_converges_with_a_bounded_binary_search()
    {
        var evaluator = new FakeEncounterCalibrationEvaluator(multiplier =>
            Math.Clamp(1.4 - 0.5 * multiplier, 0, 1));
        var result = new EncounterCalibrator(evaluator).Calibrate(
            CreateWorldTowerAnalysis(),
            CreateRepresentativeBuilds(),
            8471,
            new EncounterCalibrationOptions(0.5, 2, 6));

        var floor = Assert.Single(result.Floors);
        Assert.Equal(EncounterCalibrationSearchStatus.Converged, floor.Status);
        Assert.InRange(floor.SuggestedClearRate, 0.55, 0.75);
        Assert.True(floor.RecommendedDifficultyMultiplier > 1);
        Assert.Equal(floor.RecommendedDifficultyMultiplier, floor.HealthAdjustmentFactor);
        Assert.Equal(floor.RecommendedDifficultyMultiplier, floor.DamageAdjustmentFactor);
        Assert.Equal(
            Math.Round(floor.AuthoredHealthMultiplier * floor.HealthAdjustmentFactor, 3),
            floor.SuggestedHealthMultiplier);
        Assert.True(floor.RequiresContentChange);
        Assert.False(result.ProductionContentModified);
        Assert.All(evaluator.Requests, request =>
            Assert.Equal(request.HealthAdjustmentFactor, request.DamageAdjustmentFactor));
    }

    [Fact]
    public void Encounter_calibration_reports_an_exhausted_lower_bound_without_recommending_a_write()
    {
        var source = CreateWorldTowerAnalysis();
        var hardFloor = source.Floors.Single() with
        {
            ObservedClearRate = 0,
            Classification = WorldTowerDifficultyClassification.TooHard
        };
        var evaluator = new FakeEncounterCalibrationEvaluator(_ => 0);

        var result = new EncounterCalibrator(evaluator).Calibrate(
            source with { Floors = [hardFloor] },
            CreateRepresentativeBuilds(),
            8471,
            new EncounterCalibrationOptions(0.5, 2, 6));

        var floor = Assert.Single(result.Floors);
        Assert.Equal(EncounterCalibrationSearchStatus.LowerBoundExhausted, floor.Status);
        Assert.Equal(0.5, floor.RecommendedDifficultyMultiplier);
        Assert.False(floor.RequiresContentChange);
        Assert.Contains("review mechanics", floor.Recommendation);
    }

    [Fact]
    public void Assisted_encounter_calibration_proposes_only_offense_for_dominant_party_attrition()
    {
        var source = CreateWorldTowerAnalysis();
        var hardFloor = source.Floors.Single() with
        {
            ObservedClearRate = 0.2,
            Classification = WorldTowerDifficultyClassification.TooHard,
            PrimaryObservedFailureModeCounts = new Dictionary<WorldTowerObservedFailureMode, int>
            {
                [WorldTowerObservedFailureMode.PartyAttrition] = 8,
                [WorldTowerObservedFailureMode.PrimaryTargetCollapse] = 2
            }
        };
        var evaluator = new FakeScalingValidationEvaluator(request =>
        {
            if (request.HealthAdjustmentFactor == 1 && request.DamageAdjustmentFactor < 1)
                return request.DamageAdjustmentFactor <= 0.7 ? 0.65 : 0.6;
            if (request.HealthAdjustmentFactor == 1 && request.DamageAdjustmentFactor == 1)
                return 0.2;
            return 0.65;
        });

        var result = new EncounterCalibrator(evaluator).Calibrate(
            source with { Floors = [hardFloor] },
            CreateRepresentativeBuilds(),
            8471,
            new EncounterCalibrationOptions(0.5, 2, 2)
            {
                AssistedCalibrationEnabled = true,
                AssistedProbeSimulations = 10
            });

        var floor = Assert.Single(result.Floors);
        Assert.Equal(EncounterAssistedCalibrationVerdict.Proposal, floor.AssistedVerdict);
        Assert.Equal(EncounterCalibrationEvidenceDisposition.Supported, floor.AssistedEvidenceDisposition);
        Assert.Equal(WorldTowerObservedFailureMode.PartyAttrition, floor.DominantObservedFailureMode);
        Assert.Equal(0.8, floor.DominantObservedFailureShare);
        var proposal = Assert.Single(floor.ParameterProposals);
        Assert.Equal(EncounterCalibrationParameterGroup.Offense, proposal.ParameterGroup);
        Assert.True(proposal.HumanApprovalRequired);
        Assert.True(floor.IdentityConstraintsSatisfied);
        Assert.All(
            evaluator.Requests.Where(request => request.HealthAdjustmentFactor == 1 && request.DamageAdjustmentFactor != 1),
            request =>
            {
                Assert.Equal(1, request.DefenseAdjustmentFactor);
                Assert.Equal(1, request.ResistanceAdjustmentFactor);
                Assert.Equal(1, request.RegenerationAdjustmentFactor);
            });
        var sensitivitySeeds = floor.SensitivityProbes
            .Where(probe => probe.Phase == "Sensitivity")
            .Select(probe => probe.RunSeed)
            .Distinct()
            .ToArray();
        Assert.Equal([8471], sensitivitySeeds);
        Assert.Single(floor.SensitivityProbes, probe => probe.Phase == "HoldoutBaseline");
        Assert.Single(floor.SensitivityProbes, probe => probe.Phase == "HoldoutCandidate");
        Assert.DoesNotContain(8471, floor.SensitivityProbes
            .Where(probe => probe.Phase.StartsWith("Holdout", StringComparison.Ordinal))
            .Select(probe => probe.RunSeed));
        Assert.False(result.ProductionContentModified);
    }

    [Fact]
    public void Assisted_encounter_calibration_uses_regeneration_for_dominant_boss_sustain()
    {
        var source = CreateWorldTowerAnalysis();
        var hardFloor = source.Floors.Single() with
        {
            ObservedClearRate = 0.2,
            Classification = WorldTowerDifficultyClassification.TooHard,
            PrimaryObservedFailureModeCounts = new Dictionary<WorldTowerObservedFailureMode, int>
            {
                [WorldTowerObservedFailureMode.BossSustainDominance] = 10
            }
        };
        var evaluator = new FakeScalingValidationEvaluator(request =>
        {
            if (request.RegenerationAdjustmentFactor < 1)
                return request.RegenerationAdjustmentFactor <= 0.7 ? 0.65 : 0.6;
            if (request.HealthAdjustmentFactor == 1 && request.DamageAdjustmentFactor == 1)
                return 0.2;
            return 0.65;
        });

        var result = new EncounterCalibrator(evaluator).Calibrate(
            source with { Floors = [hardFloor] },
            CreateRepresentativeBuilds(),
            8471,
            new EncounterCalibrationOptions(0.5, 2, 2) { AssistedCalibrationEnabled = true });

        var floor = Assert.Single(result.Floors);
        var proposal = Assert.Single(floor.ParameterProposals);
        Assert.Equal(EncounterCalibrationParameterGroup.Regeneration, proposal.ParameterGroup);
        Assert.All(
            evaluator.Requests.Where(request => request.RegenerationAdjustmentFactor != 1),
            request =>
            {
                Assert.Equal(1, request.HealthAdjustmentFactor);
                Assert.Equal(1, request.DamageAdjustmentFactor);
                Assert.Equal(1, request.DefenseAdjustmentFactor);
                Assert.Equal(1, request.ResistanceAdjustmentFactor);
            });
    }

    [Fact]
    public void Assisted_encounter_calibration_returns_review_for_ambiguous_mechanic_evidence()
    {
        var source = CreateWorldTowerAnalysis();
        var hardFloor = source.Floors.Single() with
        {
            ObservedClearRate = 0.2,
            Classification = WorldTowerDifficultyClassification.TooHard,
            PrimaryObservedFailureModeCounts = new Dictionary<WorldTowerObservedFailureMode, int>
            {
                [WorldTowerObservedFailureMode.AddPressure] = 10
            }
        };
        var evaluator = new FakeScalingValidationEvaluator(_ => 0.2);

        var result = new EncounterCalibrator(evaluator).Calibrate(
            source with { Floors = [hardFloor] },
            CreateRepresentativeBuilds(),
            8471,
            new EncounterCalibrationOptions(0.5, 2, 2) { AssistedCalibrationEnabled = true });

        var floor = Assert.Single(result.Floors);
        Assert.Equal(EncounterAssistedCalibrationVerdict.Review, floor.AssistedVerdict);
        Assert.Equal(EncounterCalibrationEvidenceDisposition.Ambiguous, floor.AssistedEvidenceDisposition);
        Assert.Empty(floor.SensitivityProbes);
        Assert.Empty(floor.ParameterProposals);
        Assert.Contains("mechanic review", floor.AssistedRecommendation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Encounter_specific_optimizer_flags_a_narrow_dominant_cheese_strategy()
    {
        var candidates = Enumerable.Range(1, 3)
            .Select(index => CreateEvaluatedCandidate(4, index, 60 - index))
            .Select(candidate => candidate with
            {
                Build = candidate.Build with
                {
                    Essences = candidate.Build.Essences
                        .Select((essence, index) => index == 0
                            ? essence with { EssenceId = "essence.special", DisplayName = "Special Counter" }
                            : essence)
                        .ToArray()
                }
            })
            .ToArray();
        var evaluator = new FakeEncounterBuildEvaluator(request =>
            request.Builds.All(build => build.Essences.Any(essence => essence.EssenceId == "essence.special"))
                ? 1
                : 0.7);

        var result = new EncounterSpecificOptimizer(evaluator).Optimize(
            candidates,
            CreateAnchorRepresentativeLibrary(),
            CreateWorldTowerAnalysis(),
            CreateEncounterCalibration(),
            8471,
            new EncounterSpecificOptimizationOptions(CandidateSimulations: 1, RetainedBuilds: 2));

        var floor = Assert.Single(result.Floors);
        Assert.Equal(EncounterSpecificFindingKind.CheeseRisk, floor.Finding);
        Assert.Equal(0.3, floor.ClearRateAdvantage);
        Assert.Equal(2, floor.RetainedBuilds.Count);
        Assert.Contains(floor.DominantEssences, essence =>
            essence.EssenceId == "essence.special" && essence.UsageRate == 1);
        Assert.Contains(floor.Warnings, warning => warning.Contains("cheese", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, evaluator.Requests.Count);
    }

    [Fact]
    public void Scaling_validation_accepts_stable_holdout_scaling_with_ordered_percentiles()
    {
        var evaluator = new FakeScalingValidationEvaluator(request =>
        {
            if (request.RepresentativeProfileId.EndsWith("_P50", StringComparison.Ordinal))
                return 0.5;
            if (request.RepresentativeProfileId.EndsWith("_P90", StringComparison.Ordinal))
                return 0.8;
            if (request.HealthAdjustmentFactor < 1.5 && request.DamageAdjustmentFactor < 1.5)
                return 0.8;
            if (request.HealthAdjustmentFactor > 1.5 && request.DamageAdjustmentFactor > 1.5)
                return 0.5;
            if (request.HealthAdjustmentFactor > 1.5)
                return 0.6;
            if (request.DamageAdjustmentFactor > 1.5)
                return 0.55;
            return 0.66;
        });

        var result = new ScalingValidationAnalyzer(evaluator).Validate(
            CreateWorldTowerAnalysis(),
            CreateScalingValidationRepresentativeLibrary(),
            CreateEncounterCalibration(),
            8471,
            new ScalingValidationOptions(HoldoutSeeds: 8, SimulationsPerSeed: 50, ProbeSimulationsPerSeed: 25));

        var floor = Assert.Single(result.Floors);
        Assert.Equal(ScalingValidationVerdict.Validated, floor.Verdict);
        Assert.Equal(0.66, floor.HoldoutEvaluation.ClearRate);
        Assert.InRange(floor.ConfidenceLowerBound, 0.55, 0.66);
        Assert.InRange(floor.ConfidenceUpperBound, 0.66, 0.75);
        Assert.True(floor.DifficultyMonotonic);
        Assert.True(floor.PercentileOrderingValid);
        Assert.Equal(1_800, result.TotalCombatTrials);
        Assert.False(result.ProductionContentModified);
        Assert.DoesNotContain(evaluator.Requests, request => request.RunSeed == 8471);
        Assert.Equal(8, evaluator.Requests.Select(request => request.RunSeed).Distinct().Count());
    }

    [Fact]
    public void Scaling_validation_requires_mechanic_review_when_generic_percentiles_invert()
    {
        var evaluator = new FakeScalingValidationEvaluator(request =>
        {
            if (request.RepresentativeProfileId.EndsWith("_P50", StringComparison.Ordinal))
                return 0.9;
            if (request.RepresentativeProfileId.EndsWith("_P90", StringComparison.Ordinal))
                return 0.4;
            if (request.HealthAdjustmentFactor < 1.5)
                return 0.8;
            if (request.HealthAdjustmentFactor > 1.5 || request.DamageAdjustmentFactor > 1.5)
                return 0.5;
            return 0.6;
        });

        var result = new ScalingValidationAnalyzer(evaluator).Validate(
            CreateWorldTowerAnalysis(),
            CreateScalingValidationRepresentativeLibrary(),
            CreateEncounterCalibration(),
            8471,
            new ScalingValidationOptions(HoldoutSeeds: 2, SimulationsPerSeed: 10, ProbeSimulationsPerSeed: 10));

        var floor = Assert.Single(result.Floors);
        Assert.Equal(ScalingValidationVerdict.MechanicReviewRequired, floor.Verdict);
        Assert.False(floor.PercentileOrderingValid);
        Assert.Contains(floor.Warnings, warning => warning.Contains("P50", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ProgressionCurveKind.Linear)]
    [InlineData(ProgressionCurveKind.EaseIn)]
    [InlineData(ProgressionCurveKind.EaseOut)]
    [InlineData(ProgressionCurveKind.SmoothStep)]
    public void Progression_band_applies_the_documented_curve(ProgressionCurveKind curve)
    {
        var result = new ProgressionBandBuilder().Create(
            CreatePowerAnchors(),
            new ProgressionBandOptions(curve));

        var band = Assert.Single(result.Bands);
        Assert.Equal(75, band.Floors[0].TargetBenchmarkPower);
        Assert.Equal(85, band.Floors[^1].TargetBenchmarkPower);
        Assert.Equal(10, band.Floors.Count);
        Assert.All(band.Floors, floor =>
        {
            var position = (floor.Floor - 1) / 9d;
            var expectedWeight = curve switch
            {
                ProgressionCurveKind.Linear => position,
                ProgressionCurveKind.EaseIn => position * position,
                ProgressionCurveKind.EaseOut => 1 - Math.Pow(1 - position, 2),
                ProgressionCurveKind.SmoothStep => position * position * (3 - 2 * position),
                _ => throw new InvalidOperationException()
            };
            Assert.Equal(Math.Round(expectedWeight, 6, MidpointRounding.AwayFromZero), floor.CurveWeight);
            Assert.Equal(
                Math.Round(75 + 10 * expectedWeight, 2, MidpointRounding.AwayFromZero),
                floor.TargetBenchmarkPower);
        });
    }

    [Fact]
    public void Power_anchor_rejects_a_representative_with_mismatched_gear()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new PowerAnchorAnalyzer().Analyze(
                CreateRegionOneGearPackages(),
                CreateAnchorRepresentativeLibrary(mismatchStartGear: true)));

        Assert.Contains("instead of 'T1_Rare_Exceptional_Balanced'", exception.Message);
    }

    [Fact]
    public void Power_anchor_measures_population_statistics_and_component_means()
    {
        var library = new RepresentativeBuildLibrarySnapshot(
            1,
            1337,
            new RepresentativeBuildOptions(2),
            [
                CreateAnchorRepresentativeProfile(
                    "E4_P75",
                    4,
                    "T1_Rare_Exceptional_Balanced",
                    70,
                    80),
                CreateAnchorRepresentativeProfile(
                    "E6_P75",
                    6,
                    "T1_Epic_Exceptional_Balanced",
                    80,
                    100)
            ]);

        var result = new PowerAnchorAnalyzer().Analyze(CreateRegionOneGearPackages(), library);

        var start = result.Anchors[0];
        Assert.Equal(75, start.Performance.MeanBenchmarkPower);
        Assert.Equal(70, start.Performance.MinimumBenchmarkPower);
        Assert.Equal(80, start.Performance.MaximumBenchmarkPower);
        Assert.Equal(25, start.Performance.PopulationVariance);
        Assert.Equal(5, start.Performance.PopulationStandardDeviation);
        Assert.Equal(75, start.Performance.MeanComponentScores["pve.test"]);
    }

    [Fact]
    public void Progression_band_rejects_a_missing_endpoint_anchor()
    {
        var anchors = CreatePowerAnchors();
        var missingEnd = anchors with { Anchors = [anchors.Anchors[0]] };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ProgressionBandBuilder().Create(missingEnd));

        Assert.Contains("WorldTower.Region1.End", exception.Message);
    }

    [Fact]
    public void Combat_rating_analysis_handles_tied_ranks_and_fixed_bands()
    {
        var builds = new[]
        {
            CreateAnalysisBuild("build-1", "E4_RANDOM", 100, 1_000),
            CreateAnalysisBuild("build-2", "E4_RANDOM", 100, 1_000),
            CreateAnalysisBuild("build-3", "E6_RANDOM", 200, 2_000),
            CreateAnalysisBuild("build-4", "E6_RANDOM", 200, 2_000)
        };
        var scores = new[] { 10d, 20d, 30d, 40d };
        var benchmarks = new PveBenchmarkSuiteSnapshot(
            1,
            [],
            builds.Select((build, index) => new PveBenchmarkBuildSnapshot(
                build.Id,
                build.ProfileId,
                index % 2 + 1,
                scores[index],
                [])).ToArray());

        var result = new CombatRatingAnalyzer().Analyze(builds, benchmarks);

        Assert.Equal(0.8944, result.Model.SpearmanCorrelation);
        Assert.Equal(0.8, result.Model.RSquared);
        Assert.Equal(5, result.Model.MeanAbsoluteError);
        Assert.Equal(5, result.Model.RootMeanSquareError);
        Assert.Equal(8, result.Model.MeanWithinBandSpread);
        Assert.Equal(CombatRatingHealthClassification.Good, result.Classification);
        Assert.Collection(
            result.Bands,
            band => AssertBand(band, 100, 109, 15, 11, 19),
            band => AssertBand(band, 200, 209, 35, 31, 39));
        Assert.Empty(result.Outliers);
    }

    [Fact]
    public void Combat_rating_analysis_flags_large_negative_residuals_as_low_outliers()
    {
        var builds = Enumerable.Range(0, 10)
            .Select(index => CreateAnalysisBuild(
                $"regular-{index}",
                "TEST",
                100 + index * 10,
                1_000 + index * 100))
            .Append(CreateAnalysisBuild("low-outlier", "TEST", 150, 1_500))
            .ToArray();
        var benchmarkBuilds = builds.Take(10)
            .Select((build, index) => new PveBenchmarkBuildSnapshot(
                build.Id,
                build.ProfileId,
                index + 1,
                20 + index * 5,
                []))
            .Append(new PveBenchmarkBuildSnapshot("low-outlier", "TEST", 11, 0, []))
            .ToArray();

        var result = new CombatRatingAnalyzer().Analyze(
            builds,
            new PveBenchmarkSuiteSnapshot(1, [], benchmarkBuilds));

        var outlier = Assert.Single(result.Outliers, candidate => candidate.BuildId == "low-outlier");
        Assert.Equal("Low", outlier.Direction);
        Assert.True(outlier.Residual <= -CombatRatingAnalyzer.MinimumOutlierResidual);
        Assert.True(outlier.PercentageError < 0);
    }

    [Fact]
    public void Report_writer_persists_latest_and_immutable_history_outputs()
    {
        var outputRoot = Path.Combine(
            Path.GetTempPath(),
            "legends-legacy-balance-tests",
            Guid.NewGuid().ToString("N"));
        try
        {
            var report = CreateReport();

            var paths = new BalanceReportWriter().Write(report, outputRoot);

            Assert.True(File.Exists(paths.LatestJsonPath));
            Assert.True(File.Exists(paths.LatestMarkdownPath));
            Assert.True(File.Exists(paths.LatestGearPackagesJsonPath));
            Assert.True(File.Exists(paths.LatestEssenceBuildsJsonPath));
            Assert.True(File.Exists(paths.LatestBenchmarksJsonPath));
            Assert.True(File.Exists(paths.LatestBuildCapabilitiesJsonPath));
            Assert.True(File.Exists(paths.LatestPartyFamiliesJsonPath));
            Assert.True(File.Exists(paths.LatestPartyFamilyEvaluationJsonPath));
            Assert.True(File.Exists(paths.LatestEncounterScaleProbesJsonPath));
            Assert.True(File.Exists(paths.LatestRegionOneReliabilityStudyJsonPath));
            Assert.True(File.Exists(paths.LatestCombatRatingJsonPath));
            Assert.True(File.Exists(paths.LatestOptimizerJsonPath));
            Assert.True(File.Exists(paths.LatestRepresentativeBuildsJsonPath));
            Assert.True(File.Exists(paths.LatestEssenceMetaAnalysisJsonPath));
            Assert.True(File.Exists(paths.LatestPowerAnchorsJsonPath));
            Assert.True(File.Exists(paths.LatestProgressionBandsJsonPath));
            Assert.True(File.Exists(paths.LatestWorldTowerAnalysisJsonPath));
            Assert.True(File.Exists(paths.LatestEncounterCalibrationJsonPath));
            Assert.True(File.Exists(paths.LatestEncounterSpecificOptimizationJsonPath));
            Assert.True(File.Exists(paths.LatestEliteBuildCertificationJsonPath));
            Assert.True(File.Exists(paths.LatestScalingValidationJsonPath));
            Assert.True(File.Exists(paths.HistoryJsonPath));
            Assert.True(File.Exists(paths.HistoryMarkdownPath));
            Assert.True(File.Exists(paths.HistoryGearPackagesJsonPath));
            Assert.True(File.Exists(paths.HistoryEssenceBuildsJsonPath));
            Assert.True(File.Exists(paths.HistoryBenchmarksJsonPath));
            Assert.True(File.Exists(paths.HistoryBuildCapabilitiesJsonPath));
            Assert.True(File.Exists(paths.HistoryPartyFamiliesJsonPath));
            Assert.True(File.Exists(paths.HistoryPartyFamilyEvaluationJsonPath));
            Assert.True(File.Exists(paths.HistoryEncounterScaleProbesJsonPath));
            Assert.True(File.Exists(paths.HistoryRegionOneReliabilityStudyJsonPath));
            Assert.True(File.Exists(paths.HistoryCombatRatingJsonPath));
            Assert.True(File.Exists(paths.HistoryOptimizerJsonPath));
            Assert.True(File.Exists(paths.HistoryRepresentativeBuildsJsonPath));
            Assert.True(File.Exists(paths.HistoryEssenceMetaAnalysisJsonPath));
            Assert.True(File.Exists(paths.HistoryPowerAnchorsJsonPath));
            Assert.True(File.Exists(paths.HistoryProgressionBandsJsonPath));
            Assert.True(File.Exists(paths.HistoryWorldTowerAnalysisJsonPath));
            Assert.True(File.Exists(paths.HistoryEncounterCalibrationJsonPath));
            Assert.True(File.Exists(paths.HistoryEncounterSpecificOptimizationJsonPath));
            Assert.True(File.Exists(paths.HistoryEliteBuildCertificationJsonPath));
            Assert.True(File.Exists(paths.HistoryScalingValidationJsonPath));
            Assert.True(File.Exists(paths.HistoryFloorProgressionPolicyEvaluationJsonPath));
            Assert.True(File.Exists(paths.HistoryAutomaticFloorProgressionCalibrationJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestJsonPath),
                File.ReadAllText(paths.HistoryJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestMarkdownPath),
                File.ReadAllText(paths.HistoryMarkdownPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestGearPackagesJsonPath),
                File.ReadAllText(paths.HistoryGearPackagesJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestEssenceBuildsJsonPath),
                File.ReadAllText(paths.HistoryEssenceBuildsJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestBenchmarksJsonPath),
                File.ReadAllText(paths.HistoryBenchmarksJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestBuildCapabilitiesJsonPath),
                File.ReadAllText(paths.HistoryBuildCapabilitiesJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestPartyFamiliesJsonPath),
                File.ReadAllText(paths.HistoryPartyFamiliesJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestPartyFamilyEvaluationJsonPath),
                File.ReadAllText(paths.HistoryPartyFamilyEvaluationJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestEncounterScaleProbesJsonPath),
                File.ReadAllText(paths.HistoryEncounterScaleProbesJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestRegionOneReliabilityStudyJsonPath),
                File.ReadAllText(paths.HistoryRegionOneReliabilityStudyJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestCombatRatingJsonPath),
                File.ReadAllText(paths.HistoryCombatRatingJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestOptimizerJsonPath),
                File.ReadAllText(paths.HistoryOptimizerJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestRepresentativeBuildsJsonPath),
                File.ReadAllText(paths.HistoryRepresentativeBuildsJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestEssenceMetaAnalysisJsonPath),
                File.ReadAllText(paths.HistoryEssenceMetaAnalysisJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestPowerAnchorsJsonPath),
                File.ReadAllText(paths.HistoryPowerAnchorsJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestProgressionBandsJsonPath),
                File.ReadAllText(paths.HistoryProgressionBandsJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestWorldTowerAnalysisJsonPath),
                File.ReadAllText(paths.HistoryWorldTowerAnalysisJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestEncounterCalibrationJsonPath),
                File.ReadAllText(paths.HistoryEncounterCalibrationJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestEncounterSpecificOptimizationJsonPath),
                File.ReadAllText(paths.HistoryEncounterSpecificOptimizationJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestEliteBuildCertificationJsonPath),
                File.ReadAllText(paths.HistoryEliteBuildCertificationJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestScalingValidationJsonPath),
                File.ReadAllText(paths.HistoryScalingValidationJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestFloorProgressionPolicyEvaluationJsonPath),
                File.ReadAllText(paths.HistoryFloorProgressionPolicyEvaluationJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestAutomaticFloorProgressionCalibrationJsonPath),
                File.ReadAllText(paths.HistoryAutomaticFloorProgressionCalibrationJsonPath));

            using var json = JsonDocument.Parse(File.ReadAllText(paths.LatestJsonPath));
            Assert.Equal(1337, json.RootElement.GetProperty("metadata").GetProperty("seed").GetInt32());
            Assert.Contains("Deterministic Smoke Simulation", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Region 1 Gear Packages", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("PvE Benchmark Performance", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains(
                $"Build Capability Profiles v{BuildCapabilityProfiler.AlgorithmVersion}",
                File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Attrition Avg Health Deficit", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains(
                $"Deterministic Party Families v{PartyFamilyBuilder.AlgorithmVersion}",
                File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains(
                $"Authoritative Party-Family Encounter Evaluation v{PartyFamilyEncounterEvaluator.AlgorithmVersion}",
                File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Authored-Size Certification Gate", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Authored-Content Progression Ordering", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains(
                $"Optional Encounter Scale Probes v{EncounterScaleProbeAnalyzer.AlgorithmVersion}",
                File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Scale-Probe Performance Evidence", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains(
                $"Optional Region 1 Reliability Fault Injection v{RegionOneReliabilityStudyAnalyzer.AlgorithmVersion}",
                File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Diagnostic Verdict", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Family Contract Verdict", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Add-Clear Lifecycle Evidence", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Repeated Add-Pressure Evidence", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Graded Brood-Payload Response", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Graded Regeneration Response", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Guardian damage taken/s", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Net damage after sustain/s", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Graded Distributed-Attrition Response", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Cleanse-Demand Preconditions", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Progression-Cohort Fidelity Matrix", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Neutral-Reference Search Evidence", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Matched-Genome Progression-Power Probe", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Upstream Population Protocol", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Combat Rating Health", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Essence Optimizer", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Representative Essence Builds", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Essence Meta Analysis", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Power Anchors", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Progression Bands", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("World Tower Content Analysis", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Avg Peak Hostiles", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Terminal Results", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Primary Observations", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Encounter Calibration", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Encounter-Specific Optimization", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Elite Build Certification", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Region 1 Scaling Validation", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Floor-to-Progression Policy Evaluation", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Automatic Floor-to-Progression Calibration", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Region 1 Coordination", File.ReadAllText(paths.LatestMarkdownPath));
            using var gearJson = JsonDocument.Parse(File.ReadAllText(paths.LatestGearPackagesJsonPath));
            Assert.Single(gearJson.RootElement.EnumerateArray());
            using var essenceJson = JsonDocument.Parse(File.ReadAllText(paths.LatestEssenceBuildsJsonPath));
            Assert.Single(essenceJson.RootElement.EnumerateArray());
            using var benchmarkJson = JsonDocument.Parse(File.ReadAllText(paths.LatestBenchmarksJsonPath));
            Assert.Single(benchmarkJson.RootElement.GetProperty("builds").EnumerateArray());
            using var capabilityJson = JsonDocument.Parse(File.ReadAllText(paths.LatestBuildCapabilitiesJsonPath));
            Assert.Single(capabilityJson.RootElement.GetProperty("profiles").EnumerateArray());
            using var partyFamiliesJson = JsonDocument.Parse(File.ReadAllText(paths.LatestPartyFamiliesJsonPath));
            Assert.Single(partyFamiliesJson.RootElement.GetProperty("floors").EnumerateArray());
            using var partyFamilyEvaluationJson = JsonDocument.Parse(
                File.ReadAllText(paths.LatestPartyFamilyEvaluationJsonPath));
            Assert.False(partyFamilyEvaluationJson.RootElement.GetProperty("options").GetProperty("enabled").GetBoolean());
            Assert.Equal(
                "Disabled",
                partyFamilyEvaluationJson.RootElement.GetProperty("certificationVerdict").GetString());
            Assert.Equal(
                PartyFamilyCertificationPolicy.V1.PolicyId,
                partyFamilyEvaluationJson.RootElement.GetProperty("certificationPolicy").GetProperty("policyId").GetString());
            using var encounterScaleProbesJson = JsonDocument.Parse(
                File.ReadAllText(paths.LatestEncounterScaleProbesJsonPath));
            Assert.False(encounterScaleProbesJson.RootElement.GetProperty("options").GetProperty("enabled").GetBoolean());
            Assert.False(encounterScaleProbesJson.RootElement.GetProperty("releaseEligible").GetBoolean());
            Assert.Equal(
                "NotMeasured",
                encounterScaleProbesJson.RootElement.GetProperty("performanceBudgetAssessment").GetString());
            using var reliabilityJson = JsonDocument.Parse(
                File.ReadAllText(paths.LatestRegionOneReliabilityStudyJsonPath));
            Assert.False(reliabilityJson.RootElement.GetProperty("options").GetProperty("enabled").GetBoolean());
            Assert.Equal("Disabled", reliabilityJson.RootElement.GetProperty("verdict").GetString());
            Assert.Equal(
                "Disabled",
                reliabilityJson.RootElement.GetProperty("progressionFidelity").GetProperty("verdict").GetString());
            Assert.Equal(
                ProductionBalanceRunner.BalanceSchemaVersion,
                reliabilityJson.RootElement.GetProperty("populationProtocol").GetProperty("balanceSchemaVersion").GetInt32());
            using var combatRatingJson = JsonDocument.Parse(File.ReadAllText(paths.LatestCombatRatingJsonPath));
            Assert.Equal("Concerning", combatRatingJson.RootElement.GetProperty("classification").GetString());
            using var optimizerJson = JsonDocument.Parse(File.ReadAllText(paths.LatestOptimizerJsonPath));
            Assert.Single(optimizerJson.RootElement.GetProperty("profiles").EnumerateArray());
            using var representativeJson = JsonDocument.Parse(
                File.ReadAllText(paths.LatestRepresentativeBuildsJsonPath));
            Assert.Single(representativeJson.RootElement.GetProperty("profiles").EnumerateArray());
            using var essenceMetaJson = JsonDocument.Parse(
                File.ReadAllText(paths.LatestEssenceMetaAnalysisJsonPath));
            Assert.Single(essenceMetaJson.RootElement.GetProperty("essences").EnumerateArray());
            using var powerAnchorsJson = JsonDocument.Parse(File.ReadAllText(paths.LatestPowerAnchorsJsonPath));
            Assert.Equal(2, powerAnchorsJson.RootElement.GetProperty("anchors").GetArrayLength());
            using var progressionBandsJson = JsonDocument.Parse(
                File.ReadAllText(paths.LatestProgressionBandsJsonPath));
            Assert.Single(progressionBandsJson.RootElement.GetProperty("bands").EnumerateArray());
            using var worldTowerJson = JsonDocument.Parse(
                File.ReadAllText(paths.LatestWorldTowerAnalysisJsonPath));
            var worldTowerFloor = Assert.Single(worldTowerJson.RootElement.GetProperty("floors").EnumerateArray());
            Assert.Equal(100, worldTowerFloor.GetProperty("p10DurationTicks").GetDouble());
            Assert.Equal(100, worldTowerFloor.GetProperty("p90DurationTicks").GetDouble());
            var worldTowerTrial = Assert.Single(worldTowerFloor.GetProperty("trials").EnumerateArray());
            Assert.Equal(
                "None",
                worldTowerTrial.GetProperty("failureDiagnostic").GetProperty("terminalFailure").GetString());
            Assert.Equal(1, Assert.Single(worldTowerTrial.GetProperty("partyNumbers").EnumerateArray()).GetInt32());
            using var encounterCalibrationJson = JsonDocument.Parse(
                File.ReadAllText(paths.LatestEncounterCalibrationJsonPath));
            Assert.Single(encounterCalibrationJson.RootElement.GetProperty("floors").EnumerateArray());
            using var encounterSpecificOptimizationJson = JsonDocument.Parse(
                File.ReadAllText(paths.LatestEncounterSpecificOptimizationJsonPath));
            Assert.Single(encounterSpecificOptimizationJson.RootElement.GetProperty("floors").EnumerateArray());
            using var eliteBuildCertificationJson = JsonDocument.Parse(
                File.ReadAllText(paths.LatestEliteBuildCertificationJsonPath));
            Assert.Equal(
                "DeveloperProfileOnly",
                eliteBuildCertificationJson.RootElement.GetProperty("verdict").GetString());
            using var scalingValidationJson = JsonDocument.Parse(
                File.ReadAllText(paths.LatestScalingValidationJsonPath));
            Assert.Single(scalingValidationJson.RootElement.GetProperty("floors").EnumerateArray());
            using var automaticFloorProgressionJson = JsonDocument.Parse(
                File.ReadAllText(paths.LatestAutomaticFloorProgressionCalibrationJsonPath));
            Assert.Equal(
                "Disabled",
                automaticFloorProgressionJson.RootElement
                    .GetProperty("regionCoordination")
                    .GetProperty("verdict")
                    .GetString());
            Assert.Throws<InvalidOperationException>(() =>
                new BalanceReportWriter().Write(report, outputRoot));
        }
        finally
        {
            if (Directory.Exists(outputRoot))
                Directory.Delete(outputRoot, recursive: true);
        }
    }

    [Fact]
    public void Command_options_reject_unknown_arguments()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--not-a-command"]));

        Assert.Contains("Unknown", exception.Message);
    }

    [Fact]
    public void Command_options_configure_capability_probe_seed_count()
    {
        var options = BalanceCommandOptions.Parse(["--capability-seeds", "3"]);

        Assert.Equal(3, options.CapabilityProbeSeedCount);
        Assert.Contains("--capability-seeds", BalanceCommandOptions.Usage, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_reject_invalid_capability_probe_seed_count()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--capability-seeds", "0"]));

        Assert.Contains("--capability-seeds", exception.Message);
    }

    [Fact]
    public void Command_options_configure_party_family_sample_count()
    {
        var options = BalanceCommandOptions.Parse(["--party-family-samples", "4"]);

        Assert.Equal(4, options.PartyFamilySamplesPerFamily);
        Assert.Contains("--party-family-samples", BalanceCommandOptions.Usage, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_reject_invalid_party_family_sample_count()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--party-family-samples", "0"]));

        Assert.Contains("--party-family-samples", exception.Message);
    }

    [Fact]
    public void Command_options_configure_party_family_simulation_count()
    {
        var options = BalanceCommandOptions.Parse(["--party-family-simulations", "5"]);

        Assert.Equal(5, options.PartyFamilySimulationsPerParty);
        Assert.Contains("--party-family-simulations", BalanceCommandOptions.Usage, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_reject_invalid_party_family_simulation_count()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--party-family-simulations", "0"]));

        Assert.Contains("--party-family-simulations", exception.Message);
    }

    [Fact]
    public void Command_options_enable_and_bound_encounter_scale_probes()
    {
        var options = BalanceCommandOptions.Parse([
            "--scale-probes",
            "--scale-probe-parties", "2",
            "--scale-probe-simulations", "4",
            "--scale-probe-max-ms-per-trial", "250.5",
            "--scale-probe-max-allocated-mb-per-trial", "12.5",
            "--scale-probe-min-ticks-per-second", "1000",
            "--scale-probe-max-peak-memory-mb", "2048"
        ]);

        Assert.True(options.EncounterScaleProbeOptions.Enabled);
        Assert.Equal(2, options.EncounterScaleProbeOptions.PartiesPerSize);
        Assert.Equal(4, options.EncounterScaleProbeOptions.SimulationsPerParty);
        Assert.Equal(250.5, options.EncounterScaleProbeOptions.PerformanceBudget.MaximumMillisecondsPerTrial);
        Assert.Equal(13_107_200, options.EncounterScaleProbeOptions.PerformanceBudget.MaximumAllocatedBytesPerTrial);
        Assert.Equal(1000, options.EncounterScaleProbeOptions.PerformanceBudget.MinimumSimulatedTicksPerSecond);
        Assert.Equal(2_147_483_648, options.EncounterScaleProbeOptions.PerformanceBudget.MaximumProcessPeakWorkingSetBytes);
        Assert.Contains("--scale-probes", BalanceCommandOptions.Usage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--scale-probe-parties", "0")]
    [InlineData("--scale-probe-simulations", "101")]
    [InlineData("--scale-probe-max-ms-per-trial", "0")]
    [InlineData("--scale-probe-max-allocated-mb-per-trial", "0")]
    [InlineData("--scale-probe-min-ticks-per-second", "0")]
    [InlineData("--scale-probe-max-peak-memory-mb", "0")]
    public void Command_options_reject_invalid_encounter_scale_probe_budgets(string argument, string value)
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse([argument, value]));

        Assert.Contains(argument, exception.Message);
    }

    [Fact]
    public void Command_options_enable_and_bound_region_one_reliability_study()
    {
        var options = BalanceCommandOptions.Parse([
            "--reliability-study",
            "--reliability-rosters", "5",
            "--reliability-simulations", "15",
            "--reliability-fault-multiplier", "1.5"
        ]);

        Assert.True(options.RegionOneReliabilityStudyOptions.Enabled);
        Assert.Equal(5, options.RegionOneReliabilityStudyOptions.RostersPerFamily);
        Assert.Equal(15, options.RegionOneReliabilityStudyOptions.SimulationsPerRoster);
        Assert.Equal(1.5, options.RegionOneReliabilityStudyOptions.FaultMultiplier);
        Assert.Contains("--reliability-study", BalanceCommandOptions.Usage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--reliability-rosters", "0")]
    [InlineData("--reliability-simulations", "4")]
    [InlineData("--reliability-fault-multiplier", "1")]
    public void Command_options_reject_invalid_region_one_reliability_budgets(string argument, string value)
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse([argument, value]));

        Assert.Contains(argument, exception.Message);
    }

    [Fact]
    public void Encounter_scale_probe_options_require_unique_supported_sizes_and_unique_overrides()
    {
        Assert.Throws<ArgumentException>(() => new EncounterScaleProbeOptions
        {
            PlayerCounts = [5, 5]
        }.Validate());
        Assert.Throws<ArgumentException>(() => new EncounterScaleProbeOptions
        {
            Overrides =
            [
                new EncounterScaleProbeOverride(1, 10),
                new EncounterScaleProbeOverride(1, 10, HealthMultiplier: 1.1)
            ]
        }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EncounterScaleProbeOverride(1, 10, HealthMultiplier: 4.1).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EncounterScaleProbeOverride(1, 10, GuardianAbilityHealingMultiplier: 4.1).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EncounterScaleProbeOverride(1, 10, GuardianDistributedDamageMultiplier: 4.1).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EncounterScaleProbeOverride(1, 10, GuardianDistributedDamageMultiplier: 0.9).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EncounterScaleProbeOverride(1, 10, GuardianAdditionalSummonCopies: 4).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EncounterScaleProbeOverride(
                1,
                10,
                GuardianAdditionalSummonCopies: 1,
                GuardianAdditionalSummonPotencyMultiplier: 0.24).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterScaleProbeOptions
        {
            PerformanceBudget = new EncounterScaleProbePerformanceBudget(
                MaximumAllocatedBytesPerTrial: 0)
        }.Validate());
    }

    [Fact]
    public void Release_profile_uses_the_reviewed_party_family_simulation_budget_by_default()
    {
        var options = BalanceCommandOptions.Parse(["--certification-profile", "release"]);

        Assert.Equal(
            PartyFamilyCertificationPolicy.V1.MinimumReleasePartiesPerRegularFamily,
            options.PartyFamilySamplesPerFamily);
        Assert.Equal(
            PartyFamilyCertificationPolicy.V1.MinimumReleaseSimulationsPerParty,
            options.PartyFamilySimulationsPerParty);
    }

    [Fact]
    public void Command_options_reject_optimizer_elites_that_fill_the_population()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse([
                "--optimizer-population", "4",
                "--optimizer-elites", "4"
            ]));

        Assert.Contains("Elite count", exception.Message);
    }

    [Fact]
    public void Command_options_reject_representative_count_above_evaluated_population()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse([
                "--optimizer-population", "4",
                "--optimizer-generations", "1",
                "--optimizer-elites", "1",
                "--optimizer-retained", "1",
                "--representative-count", "8"
            ]));

        Assert.Contains("minimum evaluated population", exception.Message);
    }

    [Fact]
    public void Command_options_reject_unknown_progression_curve()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--progression-curve", "wobbly"]));

        Assert.Contains("Invalid progression curve", exception.Message);
    }

    [Fact]
    public void Command_options_configure_world_tower_simulation_count()
    {
        var options = BalanceCommandOptions.Parse(["--tower-simulations", "25"]);

        Assert.Equal(25, options.WorldTowerAnalysisOptions.SimulationsPerFloor);
    }

    [Fact]
    public void Command_options_reject_invalid_world_tower_simulation_count()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--tower-simulations", "0"]));

        Assert.Contains("--tower-simulations", exception.Message);
    }

    [Fact]
    public void Command_options_configure_encounter_calibration_iterations()
    {
        var options = BalanceCommandOptions.Parse(["--calibration-iterations", "9"]);

        Assert.Equal(9, options.EncounterCalibrationOptions.SearchIterations);
    }

    [Fact]
    public void Command_options_reject_invalid_encounter_calibration_iterations()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--calibration-iterations", "0"]));

        Assert.Contains("--calibration-iterations", exception.Message);
    }

    [Fact]
    public void Command_options_enable_assisted_encounter_calibration_explicitly()
    {
        var defaults = BalanceCommandOptions.Parse([]);
        var options = BalanceCommandOptions.Parse([
            "--assisted-calibration",
            "--assisted-calibration-simulations", "25"
        ]);

        Assert.False(defaults.EncounterCalibrationOptions.AssistedCalibrationEnabled);
        Assert.True(options.EncounterCalibrationOptions.AssistedCalibrationEnabled);
        Assert.Equal(25, options.EncounterCalibrationOptions.AssistedProbeSimulations);
        Assert.Contains("--assisted-calibration", BalanceCommandOptions.Usage, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_enable_automatic_floor_progression_calibration_explicitly()
    {
        var defaults = BalanceCommandOptions.Parse([]);
        var options = BalanceCommandOptions.Parse([
            "--floor-progression-calibration",
            "--floor-progression-simulations", "12",
            "--floor-progression-holdout-simulations", "30",
            "--floor-progression-sensitivity-points", "6",
            "--floor-progression-refinement-iterations", "5"
        ]);

        Assert.False(defaults.AutomaticFloorProgressionCalibrationOptions.Enabled);
        Assert.True(options.AutomaticFloorProgressionCalibrationOptions.Enabled);
        Assert.Equal(12, options.AutomaticFloorProgressionCalibrationOptions.SimulationsPerCandidate);
        Assert.Equal(30, options.AutomaticFloorProgressionCalibrationOptions.HoldoutSimulations);
        Assert.Equal(6, options.AutomaticFloorProgressionCalibrationOptions.SensitivityPoints);
        Assert.Equal(5, options.AutomaticFloorProgressionCalibrationOptions.RefinementIterations);
        Assert.Contains("--floor-progression-calibration", BalanceCommandOptions.Usage, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_configure_encounter_specific_optimizer()
    {
        var options = BalanceCommandOptions.Parse([
            "--encounter-candidate-simulations", "7",
            "--encounter-retained", "4"
        ]);

        Assert.Equal(7, options.EncounterSpecificOptimizationOptions.CandidateSimulations);
        Assert.Equal(4, options.EncounterSpecificOptimizationOptions.RetainedBuilds);
    }

    [Fact]
    public void Command_options_reject_invalid_encounter_specific_candidate_simulations()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--encounter-candidate-simulations", "0"]));

        Assert.Contains("--encounter-candidate-simulations", exception.Message);
    }

    [Fact]
    public void Command_options_configure_scaling_validation_sample_sizes()
    {
        var options = BalanceCommandOptions.Parse([
            "--validation-seeds", "12",
            "--validation-simulations", "200",
            "--validation-probe-simulations", "75"
        ]);

        Assert.Equal(12, options.ScalingValidationOptions.HoldoutSeeds);
        Assert.Equal(200, options.ScalingValidationOptions.SimulationsPerSeed);
        Assert.Equal(75, options.ScalingValidationOptions.ProbeSimulationsPerSeed);
    }

    [Fact]
    public void Command_options_reject_too_few_scaling_validation_seeds()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--validation-seeds", "1"]));

        Assert.Contains("--validation-seeds", exception.Message);
    }

    [Fact]
    public void Command_options_configure_meta_simulator_battle_count()
    {
        var options = BalanceCommandOptions.Parse(["--meta-simulator-battles", "2500"]);

        Assert.Equal(2_500, options.EssenceMetaAnalysisOptions.SimulatorBattleCount);
    }

    [Fact]
    public void Command_options_configure_balanced_meta_round_robin()
    {
        var options = BalanceCommandOptions.Parse(["--meta-simulator-rounds-per-matchup", "16"]);

        Assert.Equal(16, options.EssenceMetaAnalysisOptions.SimulatorRoundsPerMatchup);
        Assert.Contains("--meta-simulator-rounds-per-matchup", BalanceCommandOptions.Usage, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_reject_unpaired_meta_round_robin_rounds()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--meta-simulator-rounds-per-matchup", "15"]));

        Assert.Contains("must be even", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_reject_invalid_meta_simulator_battle_count()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--meta-simulator-battles", "0"]));

        Assert.Contains("--meta-simulator-battles", exception.Message);
    }

    [Fact]
    public void Command_options_configure_release_elite_certification_profile()
    {
        var options = BalanceCommandOptions.Parse([
            "--certification-profile", "release",
            "--elite-search-only",
            "--elite-restarts", "6",
            "--elite-population", "100",
            "--elite-generations", "20",
            "--elite-max-generations", "40",
            "--elite-elites", "10",
            "--elite-crossover", "0.4",
            "--elite-valley-beam-width", "16",
            "--elite-valley-beam-depth", "3",
            "--elite-valley-budget", "5000",
            "--elite-valley-prefilter", "256",
            "--elite-bridge-audit",
            "--elite-descriptor-audit",
            "--elite-benchmark-confidence-audit",
            "--elite-confidence-cohort", "384",
            "--elite-confidence-seeds", "446",
            "--elite-confidence-margin", "0.4",
            "--elite-finalists", "8",
            "--elite-local-swap-depth", "2",
            "--elite-two-swap-limit", "0",
            "--elite-restart-refinement", "9",
            "--elite-restart-seeds", "7",
            "--elite-restart-two-swap-limit", "750",
            "--elite-finalist-refinement", "4",
            "--elite-holdout-seeds", "10",
            "--elite-simulations", "160",
            "--elite-party-genomes", "5000",
            "--top-player-builds", "fixtures.json"
        ]);

        Assert.Equal(EliteCertificationProfile.Release, options.EliteCertificationOptions.Profile);
        Assert.True(options.EliteCertificationOptions.SearchOnly);
        Assert.Equal(6, options.EliteCertificationOptions.RestartCount);
        Assert.Equal(100, options.EliteCertificationOptions.PopulationSize);
        Assert.Equal(20, options.EliteCertificationOptions.Generations);
        Assert.Equal(40, options.EliteCertificationOptions.MaximumGenerations);
        Assert.Equal(10, options.EliteCertificationOptions.EliteCount);
        Assert.Equal(0.4, options.EliteCertificationOptions.CrossoverRate);
        Assert.Equal(0, options.EliteCertificationOptions.CoordinatedMutationRate);
        Assert.Equal(0, options.EliteCertificationOptions.ExplorerArchiveSize);
        Assert.Equal(16, options.EliteCertificationOptions.RestartValleyBeamWidth);
        Assert.Equal(3, options.EliteCertificationOptions.RestartValleyBeamDepth);
        Assert.Equal(5_000, options.EliteCertificationOptions.RestartValleyCandidateBudget);
        Assert.Equal(256, options.EliteCertificationOptions.RestartValleyPrefilterLimitPerDepth);
        Assert.True(options.EliteCertificationOptions.BridgeAuditEnabled);
        Assert.Contains("--elite-bridge-audit", BalanceCommandOptions.Usage, StringComparison.Ordinal);
        Assert.True(options.EliteCertificationOptions.DescriptorSeparabilityAuditEnabled);
        Assert.Contains("--elite-descriptor-audit", BalanceCommandOptions.Usage, StringComparison.Ordinal);
        Assert.True(options.EliteCertificationOptions.BenchmarkConfidenceAuditEnabled);
        Assert.Equal(384, options.EliteCertificationOptions.BenchmarkConfidenceAuditCohortSize);
        Assert.Equal(446, options.EliteCertificationOptions.BenchmarkConfidenceAuditSeedCount);
        Assert.Equal(0.4, options.EliteCertificationOptions.BenchmarkConfidenceTargetScoreMargin);
        Assert.Contains("--elite-benchmark-confidence-audit", BalanceCommandOptions.Usage, StringComparison.Ordinal);
        Assert.Equal(8, options.EliteCertificationOptions.FinalistsPerSlotProfile);
        Assert.Equal(10, options.EliteCertificationOptions.HoldoutSeeds);
        Assert.Equal(160, options.EliteCertificationOptions.SimulationsPerSeed);
        Assert.Equal(5_000, options.EliteCertificationOptions.PartyGenomeBudgetPerFloor);
        Assert.Equal(9, options.EliteCertificationOptions.RestartLocalRefinementPassLimit);
        Assert.Equal(7, options.EliteCertificationOptions.RestartRefinementSeedCount);
        Assert.Equal(750, options.EliteCertificationOptions.RestartTwoSwapChallengerLimitPerPass);
        Assert.Equal(4, options.EliteCertificationOptions.FinalistRefinementRoundLimit);
        Assert.Equal("fixtures.json", options.EliteCertificationOptions.TopPlayerBuildsPath);
    }

    [Fact]
    public void Command_options_reject_unknown_elite_certification_profile()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--certification-profile", "production"]));

        Assert.Contains("developer or release", exception.Message);
    }

    [Fact]
    public void Command_options_keep_experimental_elite_search_features_disabled_by_default()
    {
        var options = BalanceCommandOptions.Parse([]).EliteCertificationOptions;

        Assert.False(options.SearchOnly);
        Assert.Equal(0, options.CrossoverRate);
        Assert.Equal(0, options.CoordinatedMutationRate);
        Assert.Equal(0, options.ExplorerArchiveSize);
        Assert.Equal(0, options.StratifiedPortfolioCandidatesPerProfile);
        Assert.Equal(0, options.QualityDiversityIslandCandidateBudgetPerProfile);
        Assert.Equal(0, options.MechanicArchetypeIslandCandidateBudgetPerProfile);
        Assert.Equal(0, options.RestartValleyBeamWidth);
        Assert.Equal(0, options.RestartValleyBeamDepth);
        Assert.Equal(0, options.RestartValleyCandidateBudget);
        Assert.Equal(0, options.RestartValleyPrefilterLimitPerDepth);
        Assert.False(options.BridgeAuditEnabled);
        Assert.False(options.DescriptorSeparabilityAuditEnabled);
        Assert.False(options.BenchmarkConfidenceAuditEnabled);
        Assert.Equal(512, options.BenchmarkConfidenceAuditCohortSize);
        Assert.Equal(16, options.BenchmarkConfidenceAuditSeedCount);
        Assert.Equal(0.25, options.BenchmarkConfidenceTargetScoreMargin);
        Assert.Equal(64, options.PopulationSize);
        Assert.Equal(12, options.Generations);
        Assert.Equal(24, options.MaximumGenerations);
        Assert.Equal(8, options.EliteCount);
    }

    [Fact]
    public void Command_options_raise_implicit_elite_generation_ceiling_with_the_minimum()
    {
        var options = BalanceCommandOptions.Parse(["--elite-generations", "30"]);

        Assert.Equal(30, options.EliteCertificationOptions.Generations);
        Assert.Equal(30, options.EliteCertificationOptions.MaximumGenerations);
    }

    [Fact]
    public void Command_options_configure_opt_in_elite_basin_jumps()
    {
        var options = BalanceCommandOptions.Parse([
            "--elite-basin-jump", "0.2",
            "--elite-explorer-archive", "12"
        ]);

        Assert.Equal(0.2, options.EliteCertificationOptions.CoordinatedMutationRate);
        Assert.Equal(12, options.EliteCertificationOptions.ExplorerArchiveSize);
        Assert.Contains("--elite-basin-jump", BalanceCommandOptions.Usage, StringComparison.Ordinal);
        Assert.Contains("--elite-explorer-archive", BalanceCommandOptions.Usage, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_configure_opt_in_stratified_portfolio()
    {
        var options = BalanceCommandOptions.Parse(["--elite-stratified-portfolio", "256"]);

        Assert.Equal(256, options.EliteCertificationOptions.StratifiedPortfolioCandidatesPerProfile);
        Assert.Contains("--elite-stratified-portfolio", BalanceCommandOptions.Usage, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_configure_opt_in_quality_diversity_island()
    {
        var options = BalanceCommandOptions.Parse(["--elite-quality-island", "256"]);

        Assert.Equal(256, options.EliteCertificationOptions.QualityDiversityIslandCandidateBudgetPerProfile);
        Assert.Contains("--elite-quality-island", BalanceCommandOptions.Usage, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_configure_opt_in_mechanic_archetype_island()
    {
        var options = BalanceCommandOptions.Parse(["--elite-mechanic-island", "256"]);

        Assert.Equal(256, options.EliteCertificationOptions.MechanicArchetypeIslandCandidateBudgetPerProfile);
        Assert.Contains("--elite-mechanic-island", BalanceCommandOptions.Usage, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_reject_combining_mechanic_archetype_island_with_other_experiments()
    {
        var scenarioException = Assert.Throws<BalanceCommandException>(() => BalanceCommandOptions.Parse([
            "--elite-mechanic-island", "256",
            "--elite-quality-island", "256"
        ]));
        var portfolioException = Assert.Throws<BalanceCommandException>(() => BalanceCommandOptions.Parse([
            "--elite-mechanic-island", "256",
            "--elite-stratified-portfolio", "256"
        ]));

        Assert.Contains("isolated experiment", scenarioException.Message, StringComparison.Ordinal);
        Assert.Contains("isolated experiment", portfolioException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_reject_combining_quality_diversity_island_with_other_experiments()
    {
        var portfolioException = Assert.Throws<BalanceCommandException>(() => BalanceCommandOptions.Parse([
            "--elite-quality-island", "256",
            "--elite-stratified-portfolio", "256"
        ]));
        var valleyException = Assert.Throws<BalanceCommandException>(() => BalanceCommandOptions.Parse([
            "--elite-quality-island", "256",
            "--elite-valley-beam-width", "16",
            "--elite-valley-beam-depth", "3",
            "--elite-valley-budget", "5000"
        ]));

        Assert.Contains("isolated experiment", portfolioException.Message, StringComparison.Ordinal);
        Assert.Contains("isolated experiment", valleyException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_reject_combining_stratified_portfolio_with_search_stream_experiments()
    {
        var basinException = Assert.Throws<BalanceCommandException>(() => BalanceCommandOptions.Parse([
            "--elite-stratified-portfolio", "256",
            "--elite-basin-jump", "0.2"
        ]));
        var valleyException = Assert.Throws<BalanceCommandException>(() => BalanceCommandOptions.Parse([
            "--elite-stratified-portfolio", "256",
            "--elite-valley-beam-width", "16",
            "--elite-valley-beam-depth", "3",
            "--elite-valley-budget", "5000"
        ]));

        Assert.Contains("baseline stream stays comparable", basinException.Message, StringComparison.Ordinal);
        Assert.Contains("separate experiments", valleyException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_reject_combining_crossover_and_basin_jumps()
    {
        var exception = Assert.Throws<BalanceCommandException>(() => BalanceCommandOptions.Parse([
            "--elite-crossover", "0.35",
            "--elite-basin-jump", "0.2"
        ]));

        Assert.Contains("separate experiments", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_reject_explorer_archive_without_basin_jumps()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--elite-explorer-archive", "12"]));

        Assert.Contains("requires a positive coordinated mutation rate", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_options_reject_partial_elite_valley_search_configuration()
    {
        var exception = Assert.Throws<BalanceCommandException>(() => BalanceCommandOptions.Parse([
            "--elite-valley-beam-width", "16",
            "--elite-valley-beam-depth", "3"
        ]));

        Assert.Contains("valley search requires", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Command_options_reject_elite_valley_prefilter_without_valley_search()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--elite-valley-prefilter", "256"]));

        Assert.Contains("prefiltering requires valley search", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Command_options_reject_elite_generation_ceiling_below_the_minimum()
    {
        var exception = Assert.Throws<BalanceCommandException>(() => BalanceCommandOptions.Parse([
            "--elite-generations", "20",
            "--elite-max-generations", "10"
        ]));

        Assert.Contains("maximum generations", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Optimizer_adaptive_stop_requires_the_minimum_and_material_plateau()
    {
        var options = new EssenceOptimizerOptions(
            Generations: 3,
            MaximumGenerations: 6,
            RequiredPlateauGenerations: 2,
            PlateauImprovementTolerance: 0.25);
        var generations = new List<EssenceOptimizerGenerationSnapshot>
        {
            OptimizerGeneration(0, 10),
            OptimizerGeneration(1, 10.1),
            OptimizerGeneration(2, 10.2),
            OptimizerGeneration(3, 10.6),
            OptimizerGeneration(4, 10.65)
        };

        Assert.False(EssenceBuildOptimizer.ShouldStopAdaptiveSearch(generations, options));

        generations.Add(OptimizerGeneration(5, 10.7));

        Assert.Equal(2, EssenceBuildOptimizer.GenerationsSinceMaterialImprovement(generations, 0.25));
        Assert.True(EssenceBuildOptimizer.ShouldStopAdaptiveSearch(generations, options));
    }

    [Fact]
    public void Elite_percentile_cohorts_are_centered_on_distinct_target_scores()
    {
        double[] scores = [70, 73.3, 73.5, 75.0, 75.1, 80];

        var p95 = EliteCertificationSearchRules.SelectPercentileCohortIndexes(scores, 73.4, 2);
        var p99 = EliteCertificationSearchRules.SelectPercentileCohortIndexes(scores, 75.05, 2);

        Assert.Equal([2, 1], p95);
        Assert.Equal([4, 3], p99);
        Assert.Empty(p95.Intersect(p99));
    }

    [Fact]
    public void Elite_scenario_challenge_rejects_tradeoffs_but_accepts_stable_improvements()
    {
        var parent = new Dictionary<string, double>
        {
            ["damage"] = 90,
            ["survival"] = 80,
            ["attrition"] = 80
        };
        var tradeoff = new Dictionary<string, double>
        {
            ["damage"] = 95,
            ["survival"] = 60,
            ["attrition"] = 80
        };
        var stableImprovement = new Dictionary<string, double>
        {
            ["damage"] = 92,
            ["survival"] = 79.5,
            ["attrition"] = 80
        };

        Assert.False(EliteCertificationSearchRules.IsScenarioImprovement(parent, tradeoff, "damage", 1));
        Assert.True(EliteCertificationSearchRules.IsScenarioImprovement(parent, stableImprovement, "damage", 1));
    }

    [Theory]
    [InlineData(6, 5, 252)]
    [InlineData(12, 5, 4368)]
    public void Elite_party_search_counts_unique_unordered_genomes_with_repetition(
        int candidateCount,
        int requiredSlots,
        long expected)
    {
        Assert.Equal(expected, EliteCertificationSearchRules.CountPartyGenomes(candidateCount, requiredSlots));
    }

    [Fact]
    public void Elite_certification_policy_round_trips_from_the_versioned_configuration()
    {
        var policyPath = Path.Combine(
            AppContext.BaseDirectory,
            "Configuration",
            "elite-certification-policy.v1.json");

        var policy = EliteCertificationPolicy.Load(policyPath);

        Assert.Equal(EliteCertificationPolicy.V1, policy);
        Assert.Equal(64, policy.CreateFingerprint().Length);
    }

    [Fact]
    public void Top_player_fixtures_reject_stale_content_fingerprints()
    {
        var path = Path.Combine(Path.GetTempPath(), $"elite-fixtures-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, """
                {
                  "schemaVersion": 1,
                  "contentFingerprint": "stale",
                  "builds": [
                    {
                      "id": "fixture-one",
                      "sourceCategory": "TrustedTester",
                      "reviewDate": "2026-08-28",
                      "slotCount": 4,
                      "essenceIds": ["a", "b", "c", "d"],
                      "gearPackageId": "gear",
                      "characterLevel": 30,
                      "progressionState": "region-one",
                      "intendedRole": "generic",
                      "encounterFloor": null,
                      "observedResult": null,
                      "reviewerNote": "reviewed"
                    }
                  ],
                  "parties": []
                }
                """);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                TopPlayerFixtureDocument.Load(path, "current"));

            Assert.Contains("does not match current content", exception.Message);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static BalanceRunReport CreateReport() =>
        new(
            new BalanceRunMetadata(
                "20260827T120000000Z-12345678",
                new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero),
                1337,
                26,
                2,
                "1.0.0.0",
                "abcdef123456"),
            new BalanceContentSummary(10, 5, 2, 3),
            new BalanceSimulationSummary(
                "production-essence-smoke-1v1",
                "Friendly",
                "essence.friendly",
                "Hostile",
                "essence.hostile",
                "Victory",
                42,
                100,
                50,
                50,
                100),
            [CreateGearPackage()],
            [CreateEssenceBuild()],
            CreateBenchmarks(),
            CreateBuildCapabilities(),
            CreatePartyFamilies(),
            CreatePartyFamilyEvaluation(),
            CreateEncounterScaleProbes(),
            CreateRegionOneReliabilityStudy(),
            CreateCombatRatingHealth(),
            CreateOptimizer(),
            CreateRepresentativeBuilds(),
            CreateEssenceMetaAnalysis(),
            CreatePowerAnchors(),
            CreateProgressionBands(),
            CreateWorldTowerAnalysis(),
            CreateEncounterCalibration(),
            CreateEncounterSpecificOptimization(),
            CreateEliteBuildCertification(),
            CreateScalingValidation(),
            CreateFloorProgressionPolicyEvaluation(),
            CreateAutomaticFloorProgressionCalibration());

    private static BuildCapabilitySuiteSnapshot CreateBuildCapabilities() => new(
        BuildCapabilityProfiler.AlgorithmVersion,
        BuildCapabilityProfiler.NormalizationVersion,
        new string('a', 64),
        BuildCapabilityProfiler.PartySupportScenarioId,
        BuildCapabilityProfiler.WaveResponseScenarioId,
        1,
        false,
        [
            new BuildCapabilityProfileSnapshot(
                "E4_RANDOM_001",
                "E4_RANDOM",
                100,
                new string('b', 64),
                Enum.GetValues<BuildCapabilityDimension>()
                    .Select(dimension => new BuildCapabilityMeasurementSnapshot(
                        dimension,
                        10,
                        dimension is BuildCapabilityDimension.FocusSurvivability or BuildCapabilityDimension.AttritionResilience
                            ? "survival_seconds"
                            : "damage_per_second",
                        50,
                        dimension == BuildCapabilityDimension.AttritionResilience
                            ? new Dictionary<string, double>
                            {
                                ["sample"] = 10,
                                ["average_health_deficit_ratio"] = 0.25
                            }
                            : new Dictionary<string, double> { ["sample"] = 10 }))
                    .ToArray(),
                new BuildMechanicCapabilitySnapshot(100, 1, 1, 1, 0, 0, 0, 10, 1.5, 1.5))
        ]);

    private static PartyFamilySuiteSnapshot CreatePartyFamilies() => new(
        PartyFamilyBuilder.AlgorithmVersion,
        1337,
        new PartyFamilyBuilderOptions(1),
        [
            new PartyFamilyFloorSnapshot(
                1,
                "The First Gate",
                1,
                "E4_P50",
                PartyFamilyResponseCatalog.Create(1, "The First Gate"),
                [],
                [],
                [])
        ]);

    private static PartyFamilyEvaluationSuiteSnapshot CreatePartyFamilyEvaluation() => new(
        PartyFamilyEncounterEvaluator.AlgorithmVersion,
        1337,
        new PartyFamilyEvaluationOptions(),
        PartyFamilyCertificationPolicy.V1,
        false,
        [],
        ["Party-family encounter evaluation is disabled for this run."],
        PartyFamilyCertificationVerdict.Disabled,
        ["Party-family encounter evaluation is disabled."]);

    private static EncounterScaleProbeSuiteSnapshot CreateEncounterScaleProbes() => new(
        EncounterScaleProbeAnalyzer.AlgorithmVersion,
        1337,
        new EncounterScaleProbeOptions(),
        false,
        false,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        EncounterScaleProbePerformanceAssessment.NotMeasured,
        EncounterScaleProbePerformanceEnvironmentSnapshot.Capture(),
        [],
        ["Encounter scale probes are disabled for this run."]);

    private static RegionOneReliabilityStudySnapshot CreateRegionOneReliabilityStudy() => new(
        RegionOneReliabilityStudyAnalyzer.AlgorithmVersion,
        1337,
        new RegionOneReliabilityStudyOptions(),
        false,
        false,
        0,
        RegionOneReliabilityVerdict.Disabled,
        [],
        [],
        [],
        ["Region 1 reliability fault injection is disabled for this run."])
    {
        PopulationProtocol = new RegionOneReliabilityPopulationProtocolSnapshot(
            ProductionBalanceRunner.BalanceSchemaVersion,
            1,
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
            new WorldTowerAnalysisOptions())
    };

    private static EliteBuildCertificationSnapshot CreateEliteBuildCertification()
    {
        var holdout = new EliteHoldoutSnapshot(2, 10, 20, 18, 0.9, 0.7, 0.97, 0.27, 100, 100, 0, 0.5);
        var candidate = new EliteCertificationCandidateSnapshot(
            "E4_CERT_test",
            99,
            80,
            new Dictionary<string, double> { ["pve.short-single-target"] = 80 },
            ["essence.test_one"]);
        return new EliteBuildCertificationSnapshot(
            PveBenchmarkRunner.ScoringVersion,
            1337,
            "content-fingerprint",
            "policy-fingerprint",
            EliteCertificationPolicy.V1,
            new EliteCertificationOptions(
                RestartCount: 2,
                PopulationSize: 4,
                Generations: 1,
                EliteCount: 1,
                FinalistsPerSlotProfile: 1,
                LocalSwapDepth: 1,
                HoldoutSeeds: 2,
                SimulationsPerSeed: 10,
                PartyGenomeBudgetPerFloor: 1),
            false,
            10,
            1,
            EliteCertificationVerdict.DeveloperProfileOnly,
            ["Developer profile cannot certify."],
            [
                new EliteCertificationProfileSnapshot(
                    "E4_ELITE",
                    4,
                    1_572_574,
                    10,
                    75,
                    80,
                    80,
                    0.25,
                    true,
                    true,
                    true,
                    true,
                    candidate,
                    candidate,
                    [candidate],
                    [new EliteCertificationRestartSnapshot(1, 123, 79, 80, 4, 4, true, 10, 1, 20)],
                    new EliteLocalChallengeSnapshot(1, 1, 10, 0, true, 0, 0, null, null),
                    new CuratedBuildComparisonSnapshot(3, 79, 80, -1, true, false),
                    EliteCertificationVerdict.DeveloperProfileOnly,
                    ["Developer profile cannot certify."])
            ],
            [
                new EliteCertificationFloorSnapshot(
                    1,
                    "The First Gate",
                    "E4_P75",
                    4,
                    1,
                    1,
                    true,
                    holdout,
                    holdout,
                    holdout,
                    holdout,
                    null,
                    true,
                    true,
                    false,
                    false,
                    false,
                    EliteCertificationVerdict.DeveloperProfileOnly,
                    ["E4_CERT_p95"],
                    ["E4_CERT_p99"],
                    ["E4_CERT_test"],
                    ["Developer profile cannot certify."])
            ]);
    }

    private static EssenceMetaAnalysisSnapshot CreateEssenceMetaAnalysis() =>
        new(
            1,
            new EssenceMetaAnalysisOptions(SimulatorBattleCount: 1),
            1,
            new Dictionary<string, int>
            {
                ["P50"] = 1,
                ["P75"] = 1,
                ["P90"] = 1,
                ["P95"] = 1,
                ["P99"] = 1
            },
            new EssenceMetaSimulatorEvidenceSnapshot("RandomPool", 1, 1, 1, "Rare", "Balanced", 1),
            [
                new EssenceUsageSnapshot(
                    "essence.test_one",
                    "Test Essence One",
                    "monster.test_one",
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    75,
                    0,
                    75,
                    1,
                    0.5,
                    0,
                    "InsufficientData",
                    [])
            ],
            [],
            []);

    private static WorldTowerAnalysisSnapshot CreateWorldTowerAnalysis() =>
        new(
            1,
            new WorldTowerAnalysisOptions(1),
            [
                new WorldTowerFloorAnalysisSnapshot(
                    1,
                    "The First Gate",
                    "Garran, the Gatekeeper",
                    "monster.garran,_the_gatekeeper",
                    5,
                    75,
                    "E4_P75",
                    75,
                    1,
                    161,
                    1.27,
                    1.24,
                    164,
                    164,
                    0.65,
                    1,
                    100,
                    100,
                    0,
                    0.5,
                    WorldTowerDifficultyClassification.TooEasy,
                    ["Observed clear rate is above the configured target window."],
                    [new WorldTowerTrialSnapshot(1, 1234, "Victory", 100, 0, 0.5, 164, 820, ["E4_P75_001"])
                    {
                        PartyNumbers = [1],
                        GuardianHealthRemainingRatio = 0,
                        HostileDamagePerSecond = 12,
                        PrimaryTargetDamageTaken = 120,
                        PartySustainPerSecond = 4,
                        FailureDiagnostic = WorldTowerFailureDiagnosticSnapshot.Success
                    }])
                {
                    P10DurationTicks = 100,
                    P90DurationTicks = 100,
                    AverageHostileDamagePerSecond = 12,
                    AveragePrimaryTargetDamageTaken = 120,
                    AveragePartySustainPerSecond = 4,
                    TerminalFailureCounts = new Dictionary<WorldTowerTerminalFailure, int>
                    {
                        [WorldTowerTerminalFailure.None] = 1
                    },
                    PrimaryObservedFailureModeCounts = new Dictionary<WorldTowerObservedFailureMode, int>
                    {
                        [WorldTowerObservedFailureMode.None] = 1
                    }
                }
            ]);

    private static EncounterCalibrationSnapshot CreateEncounterCalibration() =>
        new(
            1,
            new EncounterCalibrationOptions(SearchIterations: 1),
            false,
            [
                new EncounterCalibrationFloorSnapshot(
                    1,
                    "The First Gate",
                    "Garran, the Gatekeeper",
                    "E4_P75",
                    0.65,
                    1,
                    1.27,
                    1.24,
                    1.5,
                    1.5,
                    1.5,
                    1.905,
                    1.86,
                    0.7,
                    EncounterCalibrationSearchStatus.Converged,
                    true,
                    "Consider the suggested values; developer approval is required.",
                    [
                        new EncounterCalibrationStepSnapshot(
                            1,
                            1,
                            1.5,
                            1.5,
                            1.5,
                            1.905,
                            1.86,
                            0.7,
                            100,
                            0,
                            0.5,
                            WorldTowerDifficultyClassification.OnTarget)
                    ])
            ]);

    private static EncounterSpecificOptimizationSnapshot CreateEncounterSpecificOptimization() =>
        new(
            1,
            1337,
            new EncounterSpecificOptimizationOptions(CandidateSimulations: 1, RetainedBuilds: 1),
            1,
            [
                new EncounterSpecificFloorSnapshot(
                    1,
                    "The First Gate",
                    "Garran, the Gatekeeper",
                    "E4_P75",
                    1,
                    4,
                    1.5,
                    1.5,
                    0.7,
                    1,
                    0.3,
                    75,
                    70,
                    -5,
                    0,
                    EncounterSpecificFindingKind.HardCounter,
                    ["Floor 1 specialized builds outperform the generic profile."],
                    [new EncounterSpecificEssenceSignalSnapshot("essence.test_one", "Test Essence One", 1, 1)],
                    [
                        new EncounterSpecificBuildSnapshot(
                            "E4_OPTIMIZER_001",
                            100,
                            100,
                            1,
                            100,
                            0,
                            0.5,
                            70,
                            ["essence.test_one"])
                    ])
            ]);

    private static ScalingValidationSnapshot CreateScalingValidation() =>
        new(
            1,
            1337,
            new ScalingValidationOptions(HoldoutSeeds: 2, SimulationsPerSeed: 10, ProbeSimulationsPerSeed: 5),
            false,
            90,
            1,
            0,
            0,
            [
                new ScalingValidationFloorSnapshot(
                    1,
                    "The First Gate",
                    "E4_P75",
                    2,
                    10,
                    0.55,
                    0.75,
                    1.5,
                    1.5,
                    EncounterCalibrationSearchStatus.Converged,
                    new ScalingValidationEvaluationSnapshot(20, 13, 0.65, 100, 0, 0.5),
                    0.43,
                    0.82,
                    0.39,
                    0,
                    0,
                    0.8,
                    0.5,
                    true,
                    0.6,
                    -0.05,
                    0.55,
                    -0.1,
                    0.5,
                    0.65,
                    0.8,
                    true,
                    ScalingValidationVerdict.Validated,
                    [])
            ]);

    private static FloorProgressionPolicyEvaluationSnapshot CreateFloorProgressionPolicyEvaluation() =>
        new(
            FloorProgressionPolicyEvaluator.AlgorithmVersion,
            "test-floor-progression-policy",
            1,
            new string('f', 64),
            false,
            FloorProgressionVerdict.Review,
            [],
            ["Pilot policy evidence is incomplete in the report fixture."]);

    private static AutomaticFloorProgressionCalibrationSnapshot CreateAutomaticFloorProgressionCalibration() =>
        new(
            AutomaticFloorProgressionCalibrator.AlgorithmVersion,
            1337,
            new AutomaticFloorProgressionCalibrationOptions(),
            "test-floor-progression-policy",
            new string('f', 64),
            true,
            true,
            false,
            AutomaticFloorProgressionCalibrationVerdict.Disabled,
            0,
            0,
            [],
            ["Automatic calibration is disabled in the report fixture."]);

    private static EssenceOptimizerSnapshot CreateOptimizer() =>
        new(
            1,
            1337,
            new EssenceOptimizerOptions(4, 1, 1, 0.25, 0.1, 8, 1),
            [
                new EssenceOptimizerProfileSnapshot(
                    "E4_OPTIMIZER",
                    4,
                    75,
                    80,
                    5,
                    [
                        new EssenceOptimizerGenerationSnapshot(0, 4, 4, 75, 60, 50, 0.25),
                        new EssenceOptimizerGenerationSnapshot(1, 4, 4, 80, 70, 60, 0.2)
                    ],
                    [
                        new EssenceOptimizerCandidateSnapshot(
                            "E4_OPT_G001_001",
                            1,
                            80,
                            80,
                            ["essence.one", "essence.two", "essence.three", "essence.four"],
                            new Dictionary<string, double> { ["pve.short-single-target"] = 80 })
                    ])
            ]);

    private static EssenceDefinition CreateMetaEssence(string id) => new()
    {
        Id = id,
        Name = id,
        SourceMonsterId = id.Replace("essence.", "monster.", StringComparison.Ordinal)
    };

    private static EssenceOptimizerEvaluatedCandidate CreateMetaCandidate(
        string id,
        double score,
        params string[] essenceIds)
    {
        var template = CreateEssenceBuild();
        var build = template with
        {
            Id = id,
            ProfileId = "META_TEST",
            SlotCount = essenceIds.Length,
            Essences = essenceIds.Select(essenceId => new EssenceBuildSelection(
                essenceId,
                essenceId,
                essenceId.Replace("essence.", "monster.", StringComparison.Ordinal),
                Rarity.Common)).ToArray()
        };
        return new EssenceOptimizerEvaluatedCandidate(
            build,
            new PveBenchmarkBuildSnapshot(id, build.ProfileId, 0, score, []),
            0);
    }

    private static AbilityBalanceSimulationReport CreateMetaSimulatorEvidence(
        params AbilityBalanceEssenceResult[] essenceResults) =>
        new(
            "RandomPool",
            100,
            100,
            1,
            1,
            1337,
            3,
            3,
            3,
            1,
            "Rare",
            "Balanced",
            new Dictionary<string, float>(),
            [],
            [],
            essenceResults,
            []);

    private static RepresentativeBuildLibrarySnapshot CreateRepresentativeBuilds()
    {
        var build = CreateEssenceBuild();
        return new RepresentativeBuildLibrarySnapshot(
            1,
            1337,
            new RepresentativeBuildOptions(1),
            [
                new RepresentativeEssenceProfileSnapshot(
                    "E4_P50",
                    4,
                    50,
                    1,
                    75,
                    75,
                    75,
                    75,
                    0,
                    [
                        new RepresentativeEssenceBuildSnapshot(
                            "E4_P50_001",
                            build.Id,
                            0,
                            50,
                            75,
                            0,
                            build.Essences,
                            build.Character,
                            new Dictionary<string, double> { ["pve.short-single-target"] = 75 })
                    ])
            ]);
    }

    private static PowerAnchorSuiteSnapshot CreatePowerAnchors() =>
        new(
            1,
            [
                CreatePowerAnchor(
                    "WorldTower.Region1.Start",
                    1,
                    "T1_Rare_Exceptional_Balanced",
                    "E4_P75",
                    75,
                    164,
                    1641),
                CreatePowerAnchor(
                    "WorldTower.Region1.End",
                    10,
                    "T1_Epic_Exceptional_Balanced",
                    "E6_P75",
                    85,
                    171,
                    1715)
            ]);

    private static PowerAnchorSnapshot CreatePowerAnchor(
        string id,
        int floor,
        string gearPackageId,
        string profileId,
        double power,
        int displayCr,
        int rawCr) =>
        new(
            new PowerAnchorDefinition(id, floor, gearPackageId, profileId),
            new PowerAnchorPerformanceSnapshot(
                1,
                power,
                power,
                power,
                0,
                0,
                new Dictionary<string, double> { ["pve.test"] = power }),
            new PowerAnchorCombatRatingDistributionSnapshot(
                displayCr,
                displayCr,
                displayCr,
                displayCr,
                rawCr,
                rawCr,
                rawCr,
                rawCr));

    private static ProgressionBandSuiteSnapshot CreateProgressionBands() =>
        new ProgressionBandBuilder().Create(CreatePowerAnchors());

    private static RepresentativeBuildLibrarySnapshot CreateAnchorRepresentativeLibrary(
        bool mismatchStartGear = false) =>
        new(
            1,
            1337,
            new RepresentativeBuildOptions(1),
            [
                CreateAnchorRepresentativeProfile(
                    "E4_P75",
                    4,
                    mismatchStartGear
                        ? "T1_Epic_Exceptional_Balanced"
                        : "T1_Rare_Exceptional_Balanced",
                    75),
                CreateAnchorRepresentativeProfile(
                    "E6_P75",
                    6,
                    "T1_Epic_Exceptional_Balanced",
                    85)
            ]);

    private static RepresentativeBuildLibrarySnapshot CreateScalingValidationRepresentativeLibrary() =>
        new(
            1,
            1337,
            new RepresentativeBuildOptions(1),
            [
                CreateAnchorRepresentativeProfile("E4_P50", 4, "T1_Rare_Exceptional_Balanced", 65),
                CreateAnchorRepresentativeProfile("E4_P75", 4, "T1_Rare_Exceptional_Balanced", 75),
                CreateAnchorRepresentativeProfile("E4_P90", 4, "T1_Rare_Exceptional_Balanced", 85)
            ]);

    private static RepresentativeEssenceProfileSnapshot CreateAnchorRepresentativeProfile(
        string profileId,
        int slotCount,
        string gearPackageId,
        params double[] scores)
    {
        var candidates = scores.Select((score, index) =>
            CreateEvaluatedCandidate(slotCount, index + 1, score)).ToArray();
        var mean = scores.Average();
        return new RepresentativeEssenceProfileSnapshot(
            profileId,
            slotCount,
            75,
            scores.Length,
            mean,
            scores.Min(),
            mean,
            scores.Max(),
            0,
            candidates.Select((candidate, index) =>
                new RepresentativeEssenceBuildSnapshot(
                    $"{profileId}_{index + 1:000}",
                    candidate.Build.Id,
                    0,
                    75,
                    candidate.Benchmark.AggregateScore,
                    0,
                    candidate.Build.Essences,
                    candidate.Build.Character with { GearPackageId = gearPackageId },
                    new Dictionary<string, double>
                    {
                        ["pve.test"] = candidate.Benchmark.AggregateScore
                    }))
                .ToArray());
    }

    private static IReadOnlyList<GearPackageSnapshot> CreateRegionOneGearPackages()
    {
        var start = CreateGearPackage();
        var end = start with
        {
            Definition = new GearPackageDefinition(
                "T1_Epic_Exceptional_Balanced",
                "WorldTower.Region1.Floor10",
                1,
                Rarity.Epic,
                ItemQuality.Exceptional,
                GearPackageArchetype.Balanced),
            CombatRating = start.CombatRating with { DisplayOverall = 171, RawOverall = 1715 }
        };
        return [start, end];
    }

    private static CombatRatingHealthSnapshot CreateCombatRatingHealth() =>
        new(
            1,
            10,
            CombatRatingHealthClassification.Concerning,
            1,
            1,
            new CombatRatingModelSnapshot(75, 0, 0, 0, 0, 0, 0, 0),
            [new CombatRatingBandSnapshot(100, 109, 1, 75, 75, 75, 0, 0, 0, 75, 75)],
            [new CombatRatingPredictionSnapshot("E4_RANDOM_001", "E4_RANDOM", 100, 1_000, 75, 75, 0, 0, 0)],
            [],
            ["Test warning."]);

    private static PveBenchmarkSuiteSnapshot CreateBenchmarks()
    {
        var scenarios = new[]
        {
            new PveBenchmarkScenarioSnapshot(
                "pve.short-single-target",
                "Short Single Target",
                300,
                1,
                "Burst and opening pressure")
        };
        var metrics = new PveBenchmarkMetricsSnapshot(
            "Draw",
            300,
            500,
            100,
            10,
            5,
            20,
            15,
            150,
            50,
            0,
            true,
            0.9);
        return new PveBenchmarkSuiteSnapshot(
            PveBenchmarkRunner.ScoringVersion,
            scenarios,
            [
                new PveBenchmarkBuildSnapshot(
                    "E4_RANDOM_001",
                    "E4_RANDOM",
                    1,
                    75,
                    [new PveBenchmarkComponentSnapshot(scenarios[0].Id, 1234, 75, metrics)])
            ]);
    }

    private static GearPackageSnapshot CreateGearPackage() =>
        new(
            new GearPackageDefinition(
                "T1_Rare_Exceptional_Balanced",
                "WorldTower.Region1.Floor1",
                1,
                Rarity.Rare,
                ItemQuality.Exceptional,
                GearPackageArchetype.Balanced),
            5,
            16,
            new GearPackageCombatRatingSnapshot(25, 16, 100, 1_000, 400, 400, 300, 300, 100, 0),
            new Dictionary<string, float> { ["Power"] = 50 },
            Array.Empty<GearPackageItemSnapshot>());

    private static EssenceBuildSnapshot CreateEssenceBuild() =>
        new(
            "E4_RANDOM_001",
            "E4_RANDOM",
            4,
            123,
            [
                new EssenceBuildSelection(
                    "essence.test_one",
                    "Test Essence One",
                    "monster.test_one",
                    Rarity.Common),
                new EssenceBuildSelection(
                    "essence.test_two",
                    "Test Essence Two",
                    "monster.test_two",
                    Rarity.Common),
                new EssenceBuildSelection(
                    "essence.test_three",
                    "Test Essence Three",
                    "monster.test_three",
                    Rarity.Common),
                new EssenceBuildSelection(
                    "essence.test_four",
                    "Test Essence Four",
                    "monster.test_four",
                    Rarity.Common)
            ],
            new EssenceBuildCharacterSnapshot(
                "T1_Rare_Exceptional_Balanced",
                30,
                4,
                new GearPackageCombatRatingSnapshot(25, 16, 100, 1_000, 400, 400, 300, 300, 100, 0)));

    private static EssenceBuildSnapshot CreateAnalysisBuild(
        string id,
        string profileId,
        int displayCr,
        int rawCr)
    {
        var template = CreateEssenceBuild();
        return template with
        {
            Id = id,
            ProfileId = profileId,
            Character = template.Character with
            {
                CombatRating = template.Character.CombatRating with
                {
                    DisplayOverall = displayCr,
                    RawOverall = rawCr
                }
            }
        };
    }

    private static EssenceOptimizerEvaluatedCandidate CreateEvaluatedCandidate(
        int slotCount,
        int index,
        double score)
    {
        var template = CreateEssenceBuild();
        var build = template with
        {
            Id = $"E{slotCount}_TEST_{index:000}",
            ProfileId = $"E{slotCount}_TEST",
            SlotCount = slotCount,
            Essences = Enumerable.Range(1, slotCount)
                .Select(essenceIndex => new EssenceBuildSelection(
                    $"essence.test_{index}_{essenceIndex}",
                    $"Test Essence {index}-{essenceIndex}",
                    $"monster.test_{index}_{essenceIndex}",
                    Rarity.Common))
                .ToArray(),
            Character = template.Character with { UnlockedEssenceSlots = slotCount }
        };
        var benchmark = new PveBenchmarkBuildSnapshot(
            build.Id,
            build.ProfileId,
            index,
            score,
            [
                new PveBenchmarkComponentSnapshot(
                    "pve.test",
                    index,
                    score,
                    new PveBenchmarkMetricsSnapshot(
                        "Draw",
                        1,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        true,
                        1))
            ]);
        return new EssenceOptimizerEvaluatedCandidate(build, benchmark, 0);
    }

    private static void AssertBand(
        CombatRatingBandSnapshot band,
        int expectedMinimum,
        int expectedMaximum,
        double expectedMedian,
        double expectedP10,
        double expectedP90)
    {
        Assert.Equal(expectedMinimum, band.MinimumDisplayCr);
        Assert.Equal(expectedMaximum, band.MaximumDisplayCr);
        Assert.Equal(expectedMedian, band.MedianPerformance);
        Assert.Equal(expectedP10, band.P10Performance);
        Assert.Equal(expectedP90, band.P90Performance);
    }

    private static void AssertProfile(
        IGrouping<string, EssenceBuildSnapshot> profile,
        string expectedProfile,
        int expectedSlots,
        int expectedLevel,
        string expectedGearPackage)
    {
        Assert.Equal(expectedProfile, profile.Key);
        Assert.Equal(5, profile.Count());
        Assert.Equal(5, profile.Select(build => string.Join('|', build.Essences.Select(x => x.EssenceId)))
            .Distinct(StringComparer.Ordinal)
            .Count());
        Assert.All(profile, build =>
        {
            Assert.Equal(expectedSlots, build.SlotCount);
            Assert.Equal(expectedLevel, build.Character.CharacterLevel);
            Assert.Equal(expectedGearPackage, build.Character.GearPackageId);
        });
    }

    private static void AssertOptimizerProfile(
        EssenceOptimizerProfileSnapshot profile,
        string expectedProfileId,
        int expectedSlots)
    {
        Assert.Equal(expectedProfileId, profile.ProfileId);
        Assert.Equal(expectedSlots, profile.SlotCount);
        Assert.Equal(2, profile.Generations.Count);
        Assert.All(profile.Generations, generation =>
        {
            Assert.Equal(6, generation.PopulationSize);
            Assert.Equal(6, generation.UniqueGenomeCount);
            Assert.InRange(generation.MeanPairwiseSimilarity, 0, 1);
        });
        Assert.True(profile.FinalBestScore >= profile.InitialBestScore);
        Assert.Equal(3, profile.RetainedCandidates.Count);
        Assert.Equal(profile.FinalBestScore, profile.RetainedCandidates[0].AggregateScore);
        Assert.All(profile.RetainedCandidates, candidate =>
        {
            Assert.Equal(expectedSlots, candidate.EssenceIds.Count);
            Assert.Equal(
                expectedSlots,
                candidate.EssenceIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        });
    }

    private static void AssertRepresentativeProfile(
        RepresentativeEssenceProfileSnapshot profile,
        string expectedProfileId,
        int expectedSlots,
        int expectedPercentile)
    {
        Assert.Equal(expectedProfileId, profile.Id);
        Assert.Equal(expectedSlots, profile.SlotCount);
        Assert.Equal(expectedPercentile, profile.TargetPercentile);
        Assert.Equal(10, profile.EvaluatedPopulationSize);
        Assert.Equal(10, profile.Builds.Count);
        Assert.True(profile.MinimumSelectedScore <= profile.MeanSelectedScore);
        Assert.True(profile.MeanSelectedScore <= profile.MaximumSelectedScore);
        Assert.InRange(profile.MeanPairwiseSimilarity, 0, 1);
        Assert.Equal(
            profile.Builds.Count,
            profile.Builds.Select(build => string.Join('|', build.Essences.Select(essence => essence.EssenceId)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
        Assert.All(profile.Builds, build =>
        {
            Assert.Equal(expectedSlots, build.Essences.Count);
            Assert.Equal(5, build.ComponentScores.Count);
            Assert.InRange(build.PopulationPercentile, 0, 100);
            Assert.True(build.Character.UnlockedEssenceSlots >= expectedSlots);
        });
    }

    private static void AssertPowerAnchor(
        PowerAnchorSnapshot anchor,
        string expectedId,
        int expectedFloor,
        string expectedGearPackageId,
        string expectedEssenceProfileId,
        int expectedDisplayCr)
    {
        Assert.Equal(expectedId, anchor.Definition.Id);
        Assert.Equal(expectedFloor, anchor.Definition.Floor);
        Assert.Equal(expectedGearPackageId, anchor.Definition.GearPackageId);
        Assert.Equal(expectedEssenceProfileId, anchor.Definition.EssenceProfileId);
        Assert.Equal(10, anchor.Performance.RepresentativeBuildCount);
        Assert.InRange(
            anchor.Performance.MeanBenchmarkPower,
            anchor.Performance.MinimumBenchmarkPower,
            anchor.Performance.MaximumBenchmarkPower);
        Assert.True(anchor.Performance.PopulationVariance >= 0);
        Assert.Equal(
            Math.Round(Math.Sqrt(anchor.Performance.PopulationVariance), 4, MidpointRounding.AwayFromZero),
            anchor.Performance.PopulationStandardDeviation);
        Assert.Equal(5, anchor.Performance.MeanComponentScores.Count);
        Assert.Equal(expectedDisplayCr, anchor.CombatRating.MinimumDisplayCr);
        Assert.Equal(expectedDisplayCr, anchor.CombatRating.MedianDisplayCr);
        Assert.Equal(expectedDisplayCr, anchor.CombatRating.MeanDisplayCr);
        Assert.Equal(expectedDisplayCr, anchor.CombatRating.MaximumDisplayCr);
    }

    private static EssenceOptimizerOptions CreateTestOptimizerOptions() =>
        new(
            PopulationSize: 6,
            Generations: 1,
            EliteCount: 2,
            MutationRate: 0.25,
            RandomInjectionRate: 0.17,
            DiversityPenalty: 8,
            RetainedCandidates: 3,
            CoordinatedMutationRate: 1);

    private static EncounterSpecificOptimizationOptions CreateTestEncounterOptimizerOptions() =>
        new(CandidateSimulations: 1, RetainedBuilds: 2);

    private static EliteCertificationOptions CreateTestEliteCertificationOptions(bool searchOnly = false) =>
        new(
            RestartCount: 2,
            PopulationSize: 4,
            Generations: searchOnly ? 2 : 1,
            EliteCount: 2,
            FinalistsPerSlotProfile: 1,
            LocalSwapDepth: 1,
            TwoSwapChallengerLimitPerFinalist: 0,
            HoldoutSeeds: 2,
            SimulationsPerSeed: 1,
            PartyGenomeBudgetPerFloor: 1,
            MaximumGenerations: searchOnly ? 2 : 1,
            RestartLocalRefinementPassLimit: 1,
            FinalistRefinementRoundLimit: 0,
            RestartTwoSwapChallengerLimitPerPass: 1,
            RestartRefinementSeedCount: 1,
            SearchOnly: searchOnly,
            CrossoverRate: 0,
            RestartValleyBeamWidth: searchOnly ? 1 : 0,
            RestartValleyBeamDepth: searchOnly ? 1 : 0,
            RestartValleyCandidateBudget: searchOnly ? 1 : 0,
            RestartValleyPrefilterLimitPerDepth: searchOnly ? 1 : 0,
            CoordinatedMutationRate: searchOnly ? 1 : 0,
            ExplorerArchiveSize: searchOnly ? 2 : 0,
            StratifiedPortfolioCandidatesPerProfile: searchOnly ? 0 : 1);

    private static ScalingValidationOptions CreateTestScalingValidationOptions() =>
        new(HoldoutSeeds: 2, SimulationsPerSeed: 1, ProbeSimulationsPerSeed: 1);

    private static EssenceOptimizerGenerationSnapshot OptimizerGeneration(int generation, double bestScore) =>
        new(generation, 4, 4, bestScore, bestScore, bestScore, 0);

    private static void AssertGearPackage(
        GearPackageSnapshot package,
        string expectedId,
        string expectedAnchor,
        Rarity expectedRarity)
    {
        Assert.Equal(expectedId, package.Definition.Id);
        Assert.Equal(expectedAnchor, package.Definition.ProgressionAnchor);
        Assert.Equal(1, package.Definition.Tier);
        Assert.Equal(expectedRarity, package.Definition.Rarity);
        Assert.Equal(ItemQuality.Exceptional, package.Definition.Quality);
        Assert.Equal(GearPackageArchetype.Balanced, package.Definition.Archetype);
        Assert.Equal(7, package.Equipment.Count);
        Assert.All(package.Equipment, item =>
        {
            Assert.Equal(1, item.Tier);
            Assert.Equal(expectedRarity, item.Rarity);
            Assert.Equal(ItemQuality.Exceptional, item.Quality);
            Assert.NotEmpty(item.Modifiers);
        });
        Assert.NotEmpty(package.ProjectedAttributes);
        Assert.True(package.CombatRating.RawOverall > 0);
        Assert.Equal(package.CombatRating.RawOverall / 10, package.CombatRating.DisplayOverall);
    }

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(directory.FullName, "src", "API", "API.LL"),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL")
            })
            {
                if (File.Exists(Path.Combine(candidate, "Data", "combat", "abilities.json")))
                    return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the production API.LL content root.");
    }

    private sealed class FakeEssenceDefinitionRepository(IReadOnlyList<EssenceDefinition> definitions)
        : IEssenceDefinitionRepository
    {
        public IReadOnlyList<EssenceDefinition> GetAll() => definitions;

        public IReadOnlyList<AbilitySpec> GetAllAbilities() => [];

        public EssenceDefinition? GetById(string essenceDefinitionId) =>
            definitions.SingleOrDefault(definition =>
                definition.Id.Equals(essenceDefinitionId, StringComparison.OrdinalIgnoreCase));

        public AbilitySpec? GetAbilityById(string abilityId) => null;
    }

    private sealed class FakeEncounterCalibrationEvaluator(Func<double, double> clearRate)
        : IEncounterCalibrationEvaluator
    {
        public List<EncounterCalibrationEvaluationRequest> Requests { get; } = [];

        public EncounterCalibrationEvaluation Evaluate(EncounterCalibrationEvaluationRequest request)
        {
            Requests.Add(request);
            return new EncounterCalibrationEvaluation(
                request.Simulations,
                clearRate(request.HealthAdjustmentFactor),
                100,
                0,
                0.5);
        }
    }

    private sealed class FakeEncounterBuildEvaluator(Func<EncounterBuildEvaluationRequest, double> clearRate)
        : IEncounterBuildEvaluator
    {
        public List<EncounterBuildEvaluationRequest> Requests { get; } = [];

        public EncounterCalibrationEvaluation EvaluateBuilds(EncounterBuildEvaluationRequest request)
        {
            Requests.Add(request);
            return new EncounterCalibrationEvaluation(
                request.Simulations,
                clearRate(request),
                100,
                0,
                0.5);
        }
    }

    private sealed class FakeScalingValidationEvaluator(
        Func<EncounterCalibrationEvaluationRequest, double> clearRate)
        : IEncounterCalibrationEvaluator
    {
        public List<EncounterCalibrationEvaluationRequest> Requests { get; } = [];

        public EncounterCalibrationEvaluation Evaluate(EncounterCalibrationEvaluationRequest request)
        {
            Requests.Add(request);
            return new EncounterCalibrationEvaluation(
                request.Simulations,
                clearRate(request),
                100,
                0,
                0.5);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
