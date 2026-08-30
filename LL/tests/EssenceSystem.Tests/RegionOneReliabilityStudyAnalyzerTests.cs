using LegendsLegacy.Balance;

namespace EssenceSystem.Tests;

public sealed class RegionOneReliabilityStudyAnalyzerTests
{
    [Fact]
    public void Study_uses_paired_physical_telemetry_to_recover_health_offense_and_regeneration_faults()
    {
        var combat = new FakeReliabilityCombatEvaluator();
        var result = new RegionOneReliabilityStudyAnalyzer(combat).Analyze(
            CreateWorldTower(),
            CreateRepresentatives(),
            CreatePartyFamilies(),
            1337,
            new RegionOneReliabilityStudyOptions
            {
                Enabled = true,
                RostersPerFamily = 3,
                SimulationsPerRoster = 5
            });

        Assert.Equal(RegionOneReliabilityVerdict.Inconclusive, result.Verdict);
        Assert.Equal(1290, result.TotalCombatTrials);
        Assert.Equal(258, combat.Requests.Count);
        Assert.All(result.References, reference =>
        {
            Assert.Equal(RegionOneReliabilityVerdict.Pass, reference.Verdict);
            Assert.Equal(0.5, reference.SelectedDifficultyFactor);
            Assert.Equal(0.6, reference.Families.Single(family =>
                family.Family == PartyFamilyKind.IntendedBalanced).ClearRate);
        });
        var health = result.Faults.Single(fault => fault.Fault == RegionOneReliabilityFaultKind.Health);
        Assert.Equal(RegionOneReliabilityVerdict.Pass, health.Verdict);
        Assert.Equal(EncounterCalibrationParameterGroup.Health, health.RecoveredParameterGroup);
        Assert.Equal(RegionOneReliabilityRecoveryMethod.PairedPhysicalTelemetry, health.RecoveryMethod);
        Assert.Equal(1, health.PhysicalComparison.HostileDamagePerSecondRatio);
        Assert.True(health.InjectionReachedPhysicalTelemetry);
        Assert.True(health.DiagnosticRecoveryMatched);
        Assert.Equal(RegionOneReliabilityVerdict.Pass, health.DiagnosticVerdict);
        Assert.Equal(RegionOneReliabilityFamilyContractVerdict.NotApplicable, health.FamilyContractVerdict);
        var offense = result.Faults.Single(fault => fault.Fault == RegionOneReliabilityFaultKind.Offense);
        Assert.Equal(RegionOneReliabilityVerdict.Pass, offense.Verdict);
        Assert.Equal(EncounterCalibrationParameterGroup.Offense, offense.RecoveredParameterGroup);
        Assert.Equal(RegionOneReliabilityRecoveryMethod.PairedPhysicalTelemetry, offense.RecoveryMethod);
        Assert.Equal(1.3, offense.PhysicalComparison.HostileDamagePerSecondRatio);
        Assert.True(offense.FaultObservable);
        Assert.True(offense.CorrectionVerifiedByFrozenReference);
        var regeneration = result.Faults.Single(fault => fault.Fault == RegionOneReliabilityFaultKind.Regeneration);
        Assert.Equal(RegionOneReliabilityVerdict.Inconclusive, regeneration.Verdict);
        Assert.Equal(RegionOneReliabilityVerdict.Pass, regeneration.DiagnosticVerdict);
        Assert.Equal(
            RegionOneReliabilityFamilyContractVerdict.InsufficientEvidence,
            regeneration.FamilyContractVerdict);
        Assert.Equal(EncounterCalibrationParameterGroup.Regeneration, regeneration.RecoveredParameterGroup);
        Assert.Equal(1.4, regeneration.PhysicalComparison.GuardianSelfSustainPerSecondRatio);
        Assert.True(regeneration.InjectionReachedPhysicalTelemetry);
        Assert.Equal("GuardianAbilityHealing", regeneration.InjectedControl);
        Assert.True(regeneration.FamilyResponse.Applicable);
        Assert.Null(regeneration.FamilyResponse.Matched);
        Assert.Null(regeneration.FamilyResponse.ExpectedAdvantagedFamily);
        Assert.Equal([0.25, 0.50, 0.75, 1.00], regeneration.MechanicDoseResponse
            .Select(dose => dose.DoseFraction));
        Assert.Equal([1.10, 1.20, 1.30, 1.40], regeneration.MechanicDoseResponse
            .Select(dose => dose.AppliedMultiplier));
        Assert.All(regeneration.MechanicDoseResponse, dose => Assert.Equal(45, dose.TrialCount));
        Assert.Equal([11.0, 12.0, 13.0, 14.0], regeneration.MechanicDoseResponse.Select(dose =>
            dose.Families.Single(family => family.Family == PartyFamilyKind.IntendedBalanced)
                .AverageGuardianSelfSustainPerSecond));
        Assert.All(regeneration.MechanicDoseResponse.SelectMany(dose => dose.Families), family =>
            Assert.Equal(20, family.AverageGuardianDamageTakenPerSecond));
        Assert.Equal([9.0, 8.0, 7.0, 6.0], regeneration.MechanicDoseResponse.Select(dose =>
            dose.Families.Single(family => family.Family == PartyFamilyKind.IntendedBalanced)
                .AverageGuardianNetDamagePerSecond));
        var addPressure = result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.AddPressure);
        Assert.Equal(RegionOneReliabilityVerdict.Pass, addPressure.Verdict);
        Assert.Null(addPressure.ExpectedParameterGroup);
        Assert.Equal(WorldTowerObservedFailureMode.AddPressure, addPressure.ExpectedObservedFailureMode);
        Assert.Equal(1, addPressure.ExpectedObservedFailureShare);
        Assert.Equal(2, addPressure.PhysicalComparison.PeakAdditionalHostilesRatio);
        Assert.Equal(RegionOneReliabilityRecoveryMethod.ObservedFailureMode, addPressure.RecoveryMethod);
        Assert.Equal(PartyFamilyKind.MultiTargetSpecialist, addPressure.FamilyResponse.ExpectedAdvantagedFamily);
        Assert.True(addPressure.FamilyResponse.Matched);
        Assert.Equal(RegionOneReliabilityVerdict.Pass, addPressure.DiagnosticVerdict);
        Assert.Equal(RegionOneReliabilityFamilyContractVerdict.Pass, addPressure.FamilyContractVerdict);
        Assert.Contains("reset advantage", addPressure.FamilyResponse.Assessment, StringComparison.Ordinal);
        Assert.Contains("payload response was coherent", addPressure.FamilyResponse.Assessment, StringComparison.Ordinal);
        var referenceAddFamilies = result.References.Single(reference => reference.Floor == 3).Families;
        var referenceMultiTarget = referenceAddFamilies.Single(family =>
            family.Family == PartyFamilyKind.MultiTargetSpecialist);
        var faultMultiTarget = addPressure.Families.Single(family =>
            family.Family == PartyFamilyKind.MultiTargetSpecialist);
        var faultIntended = addPressure.Families.Single(family =>
            family.Family == PartyFamilyKind.IntendedBalanced);
        Assert.Equal(15, referenceMultiTarget.AdditionalHostileSpawnTrialCount);
        Assert.Equal(1, referenceMultiTarget.AdditionalHostileClearRate);
        Assert.Equal(40, referenceMultiTarget.AverageAdditionalHostileClearDurationTicks);
        Assert.Equal(1, faultMultiTarget.AdditionalHostileClearRate);
        Assert.Equal(20, faultMultiTarget.AverageAdditionalHostileClearDurationTicks);
        Assert.Equal(10, faultMultiTarget.AverageHostileSummonsCreated);
        Assert.Equal(3, faultMultiTarget.AverageHostileSummonWaveCount);
        Assert.Equal(3.3333, faultMultiTarget.AverageHostileSummonsPerWave);
        Assert.Equal(200, faultMultiTarget.AverageHostileSummonWaveIntervalTicks);
        Assert.Equal(1, faultMultiTarget.AverageAdditionalHostileWindowCount);
        Assert.Equal(1, faultMultiTarget.AverageClearedAdditionalHostileWindowCount);
        Assert.Equal(75, faultMultiTarget.AverageHostileSummonActiveTicks);
        Assert.Equal(0.75, faultMultiTarget.AverageHostileSummonUptimeRatio);
        Assert.Equal(10, faultMultiTarget.AveragePeakHostileSummons);
        Assert.Equal(0.4, faultIntended.AdditionalHostileClearRate);
        Assert.Equal(50, faultIntended.AverageAdditionalHostileClearDurationTicks);
        Assert.Equal([0.25, 0.50, 0.75, 1.00], addPressure.AddPressurePayloadDoseResponse
            .Select(dose => dose.DuplicateSummonPotencyMultiplier));
        Assert.All(addPressure.AddPressurePayloadDoseResponse, dose => Assert.Equal(60, dose.TrialCount));
        Assert.Equal([1.0, 0.8, 0.6, 0.6], addPressure.AddPressurePayloadDoseResponse.Select(dose =>
            dose.Families.Single(family => family.Family == PartyFamilyKind.MultiTargetSpecialist).ClearRate));
        Assert.Same(addPressure.Families, addPressure.AddPressurePayloadDoseResponse[^1].Families);
        Assert.Equal(addPressure.FamilyResponse, addPressure.AddPressurePayloadDoseResponse[^1].FamilyResponse);
        Assert.StartsWith("Review", addPressure.CalibrationResponse, StringComparison.Ordinal);
        Assert.Null(addPressure.SuggestedCorrectionFactor);
        Assert.False(addPressure.CorrectionVerifiedByFrozenReference);
        var distributedAttrition = result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.DistributedAttrition);
        Assert.Equal(RegionOneReliabilityVerdict.Inconclusive, distributedAttrition.Verdict);
        Assert.Equal(RegionOneReliabilityVerdict.Pass, distributedAttrition.DiagnosticVerdict);
        Assert.Equal(
            RegionOneReliabilityFamilyContractVerdict.InsufficientEvidence,
            distributedAttrition.FamilyContractVerdict);
        Assert.Null(distributedAttrition.ExpectedParameterGroup);
        Assert.Equal(WorldTowerObservedFailureMode.PartyAttrition, distributedAttrition.ExpectedObservedFailureMode);
        Assert.Equal(RegionOneReliabilityRecoveryMethod.ObservedFailureMode, distributedAttrition.RecoveryMethod);
        Assert.Equal(1.5, distributedAttrition.PhysicalComparison.NonPrimaryFriendlyDamageTakenPerSecondRatio);
        Assert.Equal(-0.15, distributedAttrition.PhysicalComparison.FriendlyDamageTakenConcentrationChange);
        Assert.Equal(10, distributedAttrition.PhysicalComparison.FaultAverageInjectedDistributedDamagePerSecond);
        Assert.Equal(5, distributedAttrition.PhysicalComparison.FaultAverageInjectedDistributedDamagePeakTargetsPerWave);
        Assert.Null(distributedAttrition.FamilyResponse.ExpectedAdvantagedFamily);
        Assert.Null(distributedAttrition.FamilyResponse.Matched);
        Assert.Equal([0.25, 0.50, 0.75, 1.00], distributedAttrition.MechanicDoseResponse
            .Select(dose => dose.DoseFraction));
        Assert.Equal([1.10, 1.20, 1.30, 1.40], distributedAttrition.MechanicDoseResponse
            .Select(dose => dose.AppliedMultiplier));
        Assert.All(distributedAttrition.MechanicDoseResponse, dose => Assert.Equal(45, dose.TrialCount));
        Assert.Equal([22.5, 25.0, 27.5, 30.0], distributedAttrition.MechanicDoseResponse.Select(dose =>
            dose.Families.Single(family => family.Family == PartyFamilyKind.IntendedBalanced)
                .AverageNonPrimaryFriendlyDamageTakenPerSecond));
        var fullAttritionDose = distributedAttrition.MechanicDoseResponse[^1];
        Assert.Equal(50, fullAttritionDose.Families.Single(family =>
            family.Family == PartyFamilyKind.IntendedBalanced).RestrictedMeanFirstFriendlyDeathTicks);
        Assert.True(fullAttritionDose.Families.Single(family =>
                        family.Family == PartyFamilyKind.Defensive).RestrictedMeanFirstFriendlyDeathTicks
                    > fullAttritionDose.Families.Single(family =>
                        family.Family == PartyFamilyKind.IntendedBalanced).RestrictedMeanFirstFriendlyDeathTicks);
        Assert.StartsWith("Review", distributedAttrition.CalibrationResponse, StringComparison.Ordinal);
        Assert.Null(distributedAttrition.SuggestedCorrectionFactor);
        Assert.False(distributedAttrition.CorrectionVerifiedByFrozenReference);
        Assert.All(result.Faults.SelectMany(fault => fault.Families).SelectMany(family => family.Rosters), roster =>
            Assert.Equal(5, roster.Trials.Count));
        Assert.Equal(["CleanseDemand"], result.UnsupportedFaults.Select(fault => fault.Fault));
        Assert.False(result.CleanseDemandPrecondition.EvidenceAvailable);
        Assert.False(result.CleanseDemandPrecondition.PrerequisitesSatisfied);
        Assert.False(result.CleanseDemandPrecondition.InjectionImplemented);
        Assert.Equal(RegionOneReliabilityVerdict.Unavailable, result.ProgressionFidelity.Verdict);
        Assert.Equal(0, result.ProgressionFidelity.TotalCombatTrials);
        Assert.Contains("not supplied", result.UnsupportedFaults.Single().Reason, StringComparison.Ordinal);
        Assert.False(result.ProductionContentModified);
        Assert.False(result.ReleaseEligible);
    }

    [Fact]
    public void Study_marks_regeneration_unavailable_when_ability_healing_does_not_reach_self_sustain_telemetry()
    {
        var result = new RegionOneReliabilityStudyAnalyzer(
            new FakeReliabilityCombatEvaluator(guardianHealingCoupled: false)).Analyze(
            CreateWorldTower(),
            CreateRepresentatives(),
            CreatePartyFamilies(),
            1337,
            new RegionOneReliabilityStudyOptions
            {
                Enabled = true,
                RostersPerFamily = 3,
                SimulationsPerRoster = 5
            });

        var regeneration = result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.Regeneration);
        Assert.Equal(RegionOneReliabilityVerdict.Unavailable, result.Verdict);
        Assert.Equal(RegionOneReliabilityVerdict.Unavailable, regeneration.Verdict);
        Assert.False(regeneration.InjectionReachedPhysicalTelemetry);
        Assert.Null(regeneration.PhysicalComparison.GuardianSelfSustainPerSecondRatio);
        Assert.Contains(regeneration.Warnings, warning =>
            warning.Contains("not physically observable", StringComparison.Ordinal));
    }

    [Fact]
    public void Study_keeps_regeneration_diagnostic_recovery_separate_without_an_approved_family_contract()
    {
        var result = new RegionOneReliabilityStudyAnalyzer(
            new FakeReliabilityCombatEvaluator(distortRegenerationFamilyResponse: true)).Analyze(
            CreateWorldTower(),
            CreateRepresentatives(),
            CreatePartyFamilies(),
            1337,
            new RegionOneReliabilityStudyOptions
            {
                Enabled = true,
                RostersPerFamily = 3,
                SimulationsPerRoster = 5
            });

        var regeneration = result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.Regeneration);
        Assert.Equal(RegionOneReliabilityVerdict.Inconclusive, result.Verdict);
        Assert.Equal(RegionOneReliabilityVerdict.Inconclusive, regeneration.Verdict);
        Assert.Equal(RegionOneReliabilityVerdict.Pass, regeneration.DiagnosticVerdict);
        Assert.Equal(
            RegionOneReliabilityFamilyContractVerdict.InsufficientEvidence,
            regeneration.FamilyContractVerdict);
        Assert.True(regeneration.DiagnosticRecoveryMatched);
        Assert.Null(regeneration.FamilyResponse.Matched);
        Assert.Contains("No author-approved Regeneration", regeneration.FamilyResponse.Assessment, StringComparison.Ordinal);
    }

    [Fact]
    public void Study_returns_unavailable_without_running_combat_when_valid_family_material_is_incomplete()
    {
        var combat = new FakeReliabilityCombatEvaluator();
        var families = CreatePartyFamilies();
        var incompleteFloors = families.Floors.Select(floor => floor with
        {
            Families = floor.Families.Select(family =>
                    family.Family == PartyFamilyKind.Defensive
                        ? family with
                        {
                            Parties = family.Parties.Take(2).ToArray(),
                            MaterialStatus = PartyFamilyMaterialStatus.InsufficientFamilyMaterial
                        }
                        : family)
                .ToArray()
        }).ToArray();

        var result = new RegionOneReliabilityStudyAnalyzer(combat).Analyze(
            CreateWorldTower(),
            CreateRepresentatives(),
            families with { Floors = incompleteFloors },
            1337,
            new RegionOneReliabilityStudyOptions { Enabled = true, SimulationsPerRoster = 5 });

        Assert.Equal(RegionOneReliabilityVerdict.Unavailable, result.Verdict);
        Assert.Empty(combat.Requests);
        Assert.All(result.References, reference => Assert.Equal(RegionOneReliabilityVerdict.Unavailable, reference.Verdict));
        Assert.All(result.Faults, fault => Assert.Equal(RegionOneReliabilityVerdict.Unavailable, fault.Verdict));
    }

    [Fact]
    public void Study_keeps_add_pressure_inconclusive_when_multi_target_family_material_is_missing()
    {
        var families = CreatePartyFamilies();
        var floors = families.Floors.Select(floor => floor.Floor == 3
            ? floor with
            {
                Families = floor.Families
                    .Where(family => family.Family != PartyFamilyKind.MultiTargetSpecialist)
                    .ToArray()
            }
            : floor).ToArray();

        var result = new RegionOneReliabilityStudyAnalyzer(new FakeReliabilityCombatEvaluator()).Analyze(
            CreateWorldTower(),
            CreateRepresentatives(),
            families with { Floors = floors },
            1337,
            new RegionOneReliabilityStudyOptions
            {
                Enabled = true,
                RostersPerFamily = 3,
                SimulationsPerRoster = 5
            });

        var addPressure = result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.AddPressure);
        Assert.Equal(RegionOneReliabilityVerdict.Inconclusive, addPressure.Verdict);
        Assert.True(addPressure.InjectionReachedPhysicalTelemetry);
        Assert.True(addPressure.DiagnosticRecoveryMatched);
        Assert.Null(addPressure.FamilyResponse.Matched);
        Assert.Equal(
            RegionOneReliabilityFamilyContractVerdict.InsufficientEvidence,
            addPressure.FamilyContractVerdict);
        Assert.Contains("MultiTargetSpecialist", addPressure.FamilyResponse.Assessment, StringComparison.Ordinal);
    }

    [Fact]
    public void Study_keeps_add_pressure_inconclusive_when_multi_target_lacks_a_physical_reset_advantage()
    {
        var result = new RegionOneReliabilityStudyAnalyzer(
            new FakeReliabilityCombatEvaluator(distortAddPressurePhysicalResponse: true)).Analyze(
            CreateWorldTower(),
            CreateRepresentatives(),
            CreatePartyFamilies(),
            1337,
            new RegionOneReliabilityStudyOptions
            {
                Enabled = true,
                RostersPerFamily = 3,
                SimulationsPerRoster = 5
            });

        var addPressure = result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.AddPressure);
        Assert.Equal(RegionOneReliabilityVerdict.Inconclusive, result.Verdict);
        Assert.Equal(RegionOneReliabilityVerdict.Inconclusive, addPressure.Verdict);
        Assert.True(addPressure.DiagnosticRecoveryMatched);
        Assert.False(addPressure.FamilyResponse.Matched);
        Assert.Contains("reset advantage 0.0%", addPressure.FamilyResponse.Assessment, StringComparison.Ordinal);
    }

    [Fact]
    public void Study_uses_normalized_summon_uptime_when_shorter_defeats_reduce_raw_active_ticks()
    {
        var result = new RegionOneReliabilityStudyAnalyzer(
            new FakeReliabilityCombatEvaluator(shortenAddPressureSpecialistFight: true)).Analyze(
            CreateWorldTower(),
            CreateRepresentatives(),
            CreatePartyFamilies(),
            1337,
            new RegionOneReliabilityStudyOptions
            {
                Enabled = true,
                RostersPerFamily = 3,
                SimulationsPerRoster = 5
            });

        var addPressure = result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.AddPressure);
        var reference = result.References.Single(candidate => candidate.Floor == addPressure.Floor)
            .Families.Single(family => family.Family == PartyFamilyKind.MultiTargetSpecialist);
        var injected = addPressure.Families.Single(family =>
            family.Family == PartyFamilyKind.MultiTargetSpecialist);
        Assert.True(injected.AverageHostileSummonActiveTicks < reference.AverageHostileSummonActiveTicks);
        Assert.True(injected.AverageHostileSummonUptimeRatio > reference.AverageHostileSummonUptimeRatio);
        Assert.Equal(RegionOneReliabilityFamilyContractVerdict.Pass, addPressure.FamilyContractVerdict);
        Assert.Equal(RegionOneReliabilityVerdict.Pass, addPressure.Verdict);
    }

    [Fact]
    public void Study_requires_add_pressure_to_increase_normalized_summon_uptime()
    {
        var result = new RegionOneReliabilityStudyAnalyzer(
            new FakeReliabilityCombatEvaluator(preserveAddPressureSpecialistUptime: true)).Analyze(
            CreateWorldTower(),
            CreateRepresentatives(),
            CreatePartyFamilies(),
            1337,
            new RegionOneReliabilityStudyOptions
            {
                Enabled = true,
                RostersPerFamily = 3,
                SimulationsPerRoster = 5
            });

        var addPressure = result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.AddPressure);
        var reference = result.References.Single(candidate => candidate.Floor == addPressure.Floor)
            .Families.Single(family => family.Family == PartyFamilyKind.MultiTargetSpecialist);
        var injected = addPressure.Families.Single(family =>
            family.Family == PartyFamilyKind.MultiTargetSpecialist);
        Assert.Equal(reference.AverageHostileSummonUptimeRatio, injected.AverageHostileSummonUptimeRatio);
        Assert.Equal(RegionOneReliabilityFamilyContractVerdict.Inconclusive, addPressure.FamilyContractVerdict);
        Assert.Equal(RegionOneReliabilityVerdict.Inconclusive, addPressure.Verdict);
    }

    [Fact]
    public void Study_marks_distributed_attrition_unavailable_when_non_primary_damage_does_not_increase()
    {
        var result = new RegionOneReliabilityStudyAnalyzer(
            new FakeReliabilityCombatEvaluator(distortDistributedAttritionPhysicalResponse: true)).Analyze(
            CreateWorldTower(),
            CreateRepresentatives(),
            CreatePartyFamilies(),
            1337,
            new RegionOneReliabilityStudyOptions
            {
                Enabled = true,
                RostersPerFamily = 3,
                SimulationsPerRoster = 5
            });

        var attrition = result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.DistributedAttrition);
        Assert.Equal(RegionOneReliabilityVerdict.Unavailable, attrition.Verdict);
        Assert.False(attrition.InjectionReachedPhysicalTelemetry);
        Assert.Equal(1.05, attrition.PhysicalComparison.NonPrimaryFriendlyDamageTakenPerSecondRatio);
    }

    [Fact]
    public void Study_keeps_aggregate_concentration_diagnostic_when_direct_distributed_damage_is_attributed()
    {
        var result = new RegionOneReliabilityStudyAnalyzer(
            new FakeReliabilityCombatEvaluator(distortDistributedAttritionConcentrationResponse: true)).Analyze(
            CreateWorldTower(),
            CreateRepresentatives(),
            CreatePartyFamilies(),
            1337,
            new RegionOneReliabilityStudyOptions
            {
                Enabled = true,
                RostersPerFamily = 3,
                SimulationsPerRoster = 5
            });

        var attrition = result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.DistributedAttrition);
        Assert.Equal(RegionOneReliabilityVerdict.Inconclusive, attrition.Verdict);
        Assert.Equal(RegionOneReliabilityVerdict.Pass, attrition.DiagnosticVerdict);
        Assert.Equal(
            RegionOneReliabilityFamilyContractVerdict.InsufficientEvidence,
            attrition.FamilyContractVerdict);
        Assert.True(attrition.InjectionReachedPhysicalTelemetry);
        Assert.Equal(1.5, attrition.PhysicalComparison.NonPrimaryFriendlyDamageTakenPerSecondRatio);
        Assert.Equal(0, attrition.PhysicalComparison.FriendlyDamageTakenConcentrationChange);
    }

    [Fact]
    public void Study_marks_distributed_attrition_unavailable_without_direct_multi_target_attribution()
    {
        var result = new RegionOneReliabilityStudyAnalyzer(
            new FakeReliabilityCombatEvaluator(distortDistributedAttritionDirectAttribution: true)).Analyze(
            CreateWorldTower(),
            CreateRepresentatives(),
            CreatePartyFamilies(),
            1337,
            new RegionOneReliabilityStudyOptions
            {
                Enabled = true,
                RostersPerFamily = 3,
                SimulationsPerRoster = 5
            });

        var attrition = result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.DistributedAttrition);
        Assert.Equal(RegionOneReliabilityVerdict.Unavailable, attrition.Verdict);
        Assert.False(attrition.InjectionReachedPhysicalTelemetry);
        Assert.Equal(0, attrition.PhysicalComparison.FaultAverageInjectedDistributedDamagePerSecond);
    }

    [Fact]
    public void Study_keeps_distributed_diagnostic_recovery_separate_without_an_approved_family_contract()
    {
        var result = new RegionOneReliabilityStudyAnalyzer(
            new FakeReliabilityCombatEvaluator(distortDistributedAttritionFamilyResponse: true)).Analyze(
            CreateWorldTower(),
            CreateRepresentatives(),
            CreatePartyFamilies(),
            1337,
            new RegionOneReliabilityStudyOptions
            {
                Enabled = true,
                RostersPerFamily = 3,
                SimulationsPerRoster = 5
            });

        var attrition = result.Faults.Single(fault =>
            fault.Fault == RegionOneReliabilityFaultKind.DistributedAttrition);
        Assert.Equal(RegionOneReliabilityVerdict.Inconclusive, attrition.Verdict);
        Assert.Equal(RegionOneReliabilityVerdict.Pass, attrition.DiagnosticVerdict);
        Assert.Equal(
            RegionOneReliabilityFamilyContractVerdict.InsufficientEvidence,
            attrition.FamilyContractVerdict);
        Assert.True(attrition.InjectionReachedPhysicalTelemetry);
        Assert.True(attrition.DiagnosticRecoveryMatched);
        Assert.Null(attrition.FamilyResponse.Matched);
        Assert.Contains("No author-approved DistributedAttrition", attrition.FamilyResponse.Assessment, StringComparison.Ordinal);
    }

    [Fact]
    public void Disabled_study_runs_no_combat()
    {
        var combat = new FakeReliabilityCombatEvaluator();
        var result = new RegionOneReliabilityStudyAnalyzer(combat).Analyze(
            CreateWorldTower(),
            CreateRepresentatives(),
            CreatePartyFamilies(),
            1);

        Assert.Equal(RegionOneReliabilityVerdict.Disabled, result.Verdict);
        Assert.Equal(0, result.TotalCombatTrials);
        Assert.Empty(combat.Requests);
    }

    [Fact]
    public void Options_reject_invalid_progression_fidelity_floor_sets()
    {
        Assert.Throws<ArgumentException>(() => new RegionOneReliabilityStudyOptions
        {
            ProgressionFidelityFloors = [3, 3]
        }.Validate());
        Assert.Throws<ArgumentException>(() => new RegionOneReliabilityStudyOptions
        {
            ProgressionFidelityFloors = [0]
        }.Validate());
    }

    [Fact]
    public void Options_reject_invalid_mechanic_dose_fractions()
    {
        Assert.Throws<ArgumentException>(() => new RegionOneReliabilityStudyOptions
        {
            MechanicDoseFractions = [0.25, 0.25, 1]
        }.Validate());
        Assert.Throws<ArgumentException>(() => new RegionOneReliabilityStudyOptions
        {
            MechanicDoseFractions = [0.50, 0.75]
        }.Validate());
    }

    private static WorldTowerAnalysisSnapshot CreateWorldTower() => new(
        WorldTowerContentAnalyzer.AlgorithmVersion,
        new WorldTowerAnalysisOptions(1, MaxTicks: 600),
        [Floor(1, "The Waking Step"), Floor(3, "The Third Vow"), Floor(7, "The Endless Spring")]);

    private static WorldTowerFloorAnalysisSnapshot Floor(int floor, string name) => new(
        floor,
        name,
        "Guardian",
        $"guardian.{floor}",
        5,
        75,
        "E4_P75",
        75,
        1,
        100,
        1,
        1,
        100,
        null,
        0.65,
        0,
        100,
        100,
        5,
        0,
        WorldTowerDifficultyClassification.TooHard,
        [],
        [Trial(victory: false, WorldTowerObservedFailureMode.PartyAttrition)]);

    private static RepresentativeBuildLibrarySnapshot CreateRepresentatives() => new(
        1,
        1337,
        new RepresentativeBuildOptions(1),
        [
            new RepresentativeEssenceProfileSnapshot(
                "E4_P75",
                4,
                75,
                1,
                75,
                75,
                75,
                75,
                0,
                [
                    new RepresentativeEssenceBuildSnapshot(
                        "build",
                        "build",
                        0,
                        75,
                        75,
                        0,
                        [],
                        new EssenceBuildCharacterSnapshot(
                            "gear",
                            30,
                            4,
                            new GearPackageCombatRatingSnapshot(1, 1, 100, 1_000, 0, 0, 0, 0, 0, 0)),
                        new Dictionary<string, double>())
                ])
        ]);

    private static PartyFamilySuiteSnapshot CreatePartyFamilies() => new(
        PartyFamilyBuilder.AlgorithmVersion,
        1337,
        new PartyFamilyBuilderOptions(3),
        [PartyFloor(1, "The Waking Step"), PartyFloor(3, "The Third Vow"), PartyFloor(7, "The Endless Spring")]);

    private static PartyFamilyFloorSnapshot PartyFloor(int floor, string name) => new(
        floor,
        name,
        5,
        "E4_P75",
        PartyFamilyResponseCatalog.Create(floor, name),
        [
            Family(PartyFamilyKind.IntendedBalanced),
            Family(PartyFamilyKind.Defensive),
            Family(PartyFamilyKind.SingleTargetSpecialist),
            ..(floor == 3 ? new[] { Family(PartyFamilyKind.MultiTargetSpecialist) } : [])
        ],
        [],
        []);

    private static PartyFamilySnapshot Family(PartyFamilyKind kind) => new(
        kind,
        PartyFamilyDisposition.ShouldSucceed,
        3,
        Enumerable.Range(1, 3).Select(index => new PartyFamilyPartySnapshot(
                $"{kind}-{index}".PadLeft(64, '0'),
                index,
                Enumerable.Range(1, 5).Select(_ => new PartyFamilyMemberSnapshot(
                        "build",
                        "build",
                        "representative-p75",
                        "cache"))
                    .ToArray(),
                new Dictionary<BuildCapabilityDimension, double>(),
                null,
                0,
                []))
            .ToArray(),
        "test");

    private static WorldTowerTrialSnapshot Trial(
        bool victory,
        WorldTowerObservedFailureMode mode,
        int trial = 1,
        double hostileDamagePerSecond = 40,
        double guardianHealthRemainingRatio = 0.05,
        int durationTicks = 100,
        int guardianPassiveRegeneration = 0,
        int guardianAbilityHealing = 0,
        int peakActiveHostileCombatants = 1,
        int finalActiveHostileCombatants = 1,
        int? firstAdditionalHostileTick = null,
        int? firstAdditionalHostileClearTick = null,
        int totalHostileSummons = 0,
        int additionalHostileWindowCount = 0,
        int clearedAdditionalHostileWindowCount = 0,
        int hostileSummonActiveTicks = 0,
        int hostileSummonWaveCount = 0,
        int hostileSummonWaveIntervalCount = 0,
        int hostileSummonWaveIntervalTotalTicks = 0,
        int peakActiveHostileSummons = 0,
        double guardianDamageTakenPerSecond = 20,
        double nonPrimaryFriendlyDamageTakenPerSecond = 20,
        double friendlyDamageTakenConcentration = 0.4,
        int guardianInjectedDistributedDamage = 0,
        double guardianInjectedDistributedDamagePerSecond = 0,
        int guardianInjectedDistributedDamageHitCount = 0,
        int guardianInjectedDistributedDamageWaveCount = 0,
        int guardianInjectedDistributedDamagePeakTargetsPerWave = 0) =>
        new(
            trial,
            trial,
            victory ? "Victory" : "Defeat",
            durationTicks,
            victory ? 0 : 5,
            victory ? 0.8 : 0,
            100,
            500,
            ["build"])
        {
            PeakActiveHostileCombatants = peakActiveHostileCombatants,
            FinalActiveHostileCombatants = finalActiveHostileCombatants,
            FirstAdditionalHostileTick = firstAdditionalHostileTick,
            FirstAdditionalHostileClearTick = firstAdditionalHostileClearTick,
            TotalHostileSummons = totalHostileSummons,
            AdditionalHostileWindowCount = additionalHostileWindowCount,
            ClearedAdditionalHostileWindowCount = clearedAdditionalHostileWindowCount,
            HostileSummonActiveTicks = hostileSummonActiveTicks,
            HostileSummonWaveCount = hostileSummonWaveCount,
            HostileSummonWaveIntervalCount = hostileSummonWaveIntervalCount,
            HostileSummonWaveIntervalTotalTicks = hostileSummonWaveIntervalTotalTicks,
            PeakActiveHostileSummons = peakActiveHostileSummons,
            HostileDamagePerSecond = hostileDamagePerSecond,
            GuardianHealthRemainingRatio = guardianHealthRemainingRatio,
            GuardianPassiveRegeneration = guardianPassiveRegeneration,
            GuardianAbilityHealing = guardianAbilityHealing,
            GuardianTotalSelfSustain = checked(guardianPassiveRegeneration + guardianAbilityHealing),
            GuardianDamageTakenPerSecond = guardianDamageTakenPerSecond,
            NonPrimaryFriendlyDamageTakenPerSecond = nonPrimaryFriendlyDamageTakenPerSecond,
            FriendlyDamageTakenConcentration = friendlyDamageTakenConcentration,
            GuardianInjectedDistributedDamage = guardianInjectedDistributedDamage,
            GuardianInjectedDistributedDamagePerSecond = guardianInjectedDistributedDamagePerSecond,
            GuardianInjectedDistributedDamageHitCount = guardianInjectedDistributedDamageHitCount,
            GuardianInjectedDistributedDamageWaveCount = guardianInjectedDistributedDamageWaveCount,
            GuardianInjectedDistributedDamagePeakTargetsPerWave = guardianInjectedDistributedDamagePeakTargetsPerWave,
            FirstFriendlyDeathTick = victory ? null : 50,
            GuardianRegenerationTimeline =
            [
                new WorldTowerRegenerationPointSnapshot(
                    durationTicks,
                    100,
                    1_000,
                    guardianPassiveRegeneration,
                    1,
                    0)
            ],
            FailureDiagnostic = victory
                ? WorldTowerFailureDiagnosticSnapshot.Success
                : new WorldTowerFailureDiagnosticSnapshot(
                    WorldTowerTerminalFailure.PartyDefeated,
                    mode,
                    0.8,
                    [],
                    WorldTowerContentAnalyzer.FailureRuleVersion,
                    null,
                    [])
        };

    private sealed class FakeReliabilityCombatEvaluator(
        bool guardianHealingCoupled = true,
        bool distortRegenerationFamilyResponse = false,
        bool distortAddPressurePhysicalResponse = false,
        bool shortenAddPressureSpecialistFight = false,
        bool preserveAddPressureSpecialistUptime = false,
        bool distortDistributedAttritionPhysicalResponse = false,
        bool distortDistributedAttritionConcentrationResponse = false,
        bool distortDistributedAttritionFamilyResponse = false,
        bool distortDistributedAttritionDirectAttribution = false)
        : IEncounterScaleProbeCombatEvaluator
    {
        public List<EncounterScaleProbeCombatRequest> Requests { get; } = [];

        public IReadOnlyList<WorldTowerTrialSnapshot> EvaluateScaleProbe(EncounterScaleProbeCombatRequest request)
        {
            Requests.Add(request);
            var value = request.AppliedOverride;
            var isHealthFault = value.HealthMultiplier > value.OffenseMultiplier;
            var isOffenseFault = value.OffenseMultiplier > value.HealthMultiplier;
            var isRegenerationFault = value.GuardianAbilityHealingMultiplier > 1;
            var isAddPressureFault = value.GuardianAdditionalSummonCopies > 0;
            var isDistributedAttritionFault = value.GuardianDistributedDamageMultiplier > 1;
            var referenceFactor = value.HealthMultiplier;
            var clearCount = isHealthFault || isOffenseFault || isRegenerationFault || isAddPressureFault
                             || isDistributedAttritionFault
                ? 0
                : Math.Abs(referenceFactor - 0.5) < 0.0001
                    ? 3
                    : referenceFactor < 0.5
                        ? request.Simulations
                        : 0;
            if (isRegenerationFault && distortRegenerationFamilyResponse)
            {
                var regenerationRequestIndex = Requests.Count(candidate =>
                    candidate.AppliedOverride.GuardianAbilityHealingMultiplier > 1) - 1;
                var familyIndex = regenerationRequestIndex / 3;
                if (familyIndex == 1)
                    clearCount = 3;
            }
            if (isAddPressureFault)
            {
                var addRequestIndex = Requests.Count(candidate =>
                    candidate.AppliedOverride.GuardianAdditionalSummonCopies > 0
                    && candidate.AppliedOverride.GuardianAdditionalSummonPotencyMultiplier
                    == value.GuardianAdditionalSummonPotencyMultiplier) - 1;
                var familyIndex = addRequestIndex / 3;
                if (familyIndex == 3)
                {
                    clearCount = value.GuardianAdditionalSummonPotencyMultiplier switch
                    {
                        0.25 => 5,
                        0.50 => 4,
                        _ => 3
                    };
                }
            }
            if (isDistributedAttritionFault)
            {
                var attritionRequestIndex = Requests.Count(candidate =>
                    candidate.AppliedOverride.GuardianDistributedDamageMultiplier > 1) - 1;
                var familyIndex = attritionRequestIndex / 3;
                if (familyIndex == 1 && !distortDistributedAttritionFamilyResponse)
                    clearCount = 3;
            }
            var currentAddFamilyIndex = isAddPressureFault
                ? (Requests.Count(candidate =>
                    candidate.AppliedOverride.GuardianAdditionalSummonCopies > 0
                    && candidate.AppliedOverride.GuardianAdditionalSummonPotencyMultiplier
                    == value.GuardianAdditionalSummonPotencyMultiplier) - 1) / 3
                : -1;
            var mode = isOffenseFault || isHealthFault || isDistributedAttritionFault
                ? WorldTowerObservedFailureMode.PartyAttrition
                : isRegenerationFault
                    ? WorldTowerObservedFailureMode.BossSustainDominance
                    : isAddPressureFault
                        ? WorldTowerObservedFailureMode.AddPressure
                    : WorldTowerObservedFailureMode.Other;
            var hostileDamagePerSecond = isOffenseFault ? 52 : isDistributedAttritionFault ? 46 : 40;
            var guardianHealthRemainingRatio = isHealthFault || isOffenseFault || isRegenerationFault ? 0.30 : 0.05;
            var durationTicks = isHealthFault
                ? 115
                : isOffenseFault
                    ? 80
                    : isAddPressureFault && currentAddFamilyIndex == 3 && shortenAddPressureSpecialistFight
                        ? 50
                        : 100;
            var guardianAbilityHealing = request.Floor == 7 && guardianHealingCoupled
                ? isRegenerationFault
                    ? (int)Math.Round(100 * value.GuardianAbilityHealingMultiplier)
                    : 100
                : 0;
            return Enumerable.Range(1, request.Simulations)
                .Select(trial => Trial(
                    trial <= clearCount,
                    mode,
                    trial,
                    hostileDamagePerSecond,
                    guardianHealthRemainingRatio,
                    durationTicks,
                    guardianAbilityHealing: guardianAbilityHealing,
                    peakActiveHostileCombatants: request.Floor == 3
                        ? isAddPressureFault ? 11 : 6
                        : 1,
                    finalActiveHostileCombatants: request.Floor == 3
                        ? isAddPressureFault ? 5 : 3
                        : 1,
                    firstAdditionalHostileTick: request.Floor == 3 ? 10 : null,
                    firstAdditionalHostileClearTick: request.Floor != 3
                        ? null
                        : !isAddPressureFault
                            ? 50
                            : currentAddFamilyIndex == 3
                                ? 30
                                : trial % 2 == 0
                                    ? 60
                                    : null,
                    totalHostileSummons: request.Floor == 3 ? isAddPressureFault ? 10 : 5 : 0,
                    additionalHostileWindowCount: request.Floor == 3
                        ? isAddPressureFault ? 1 : 3
                        : 0,
                    clearedAdditionalHostileWindowCount: request.Floor == 3
                        ? !isAddPressureFault
                            ? 3
                            : currentAddFamilyIndex == 3
                                || distortAddPressurePhysicalResponse && currentAddFamilyIndex == 0
                                ? 1
                                : 0
                        : 0,
                    hostileSummonActiveTicks: request.Floor == 3
                        ? isAddPressureFault && currentAddFamilyIndex == 3
                            ? shortenAddPressureSpecialistFight
                                ? 49
                                : preserveAddPressureSpecialistUptime ? 50 : 75
                            : isAddPressureFault ? 90 : 50
                        : 0,
                    hostileSummonWaveCount: request.Floor == 3 ? 3 : 0,
                    hostileSummonWaveIntervalCount: request.Floor == 3 ? 2 : 0,
                    hostileSummonWaveIntervalTotalTicks: request.Floor == 3 ? 400 : 0,
                    peakActiveHostileSummons: request.Floor == 3 ? isAddPressureFault ? 10 : 5 : 0,
                    nonPrimaryFriendlyDamageTakenPerSecond: isDistributedAttritionFault
                        ? distortDistributedAttritionPhysicalResponse
                            ? 21
                            : 20 + 25 * (value.GuardianDistributedDamageMultiplier - 1)
                        : 20,
                    friendlyDamageTakenConcentration: isDistributedAttritionFault
                        ? distortDistributedAttritionConcentrationResponse
                            ? 0.4
                            : 0.4 - 0.375 * (value.GuardianDistributedDamageMultiplier - 1)
                        : 0.4,
                    guardianInjectedDistributedDamage: isDistributedAttritionFault
                        && !distortDistributedAttritionDirectAttribution ? 100 : 0,
                    guardianInjectedDistributedDamagePerSecond: isDistributedAttritionFault
                        && !distortDistributedAttritionDirectAttribution ? 10 : 0,
                    guardianInjectedDistributedDamageHitCount: isDistributedAttritionFault
                        && !distortDistributedAttritionDirectAttribution ? 5 : 0,
                    guardianInjectedDistributedDamageWaveCount: isDistributedAttritionFault
                        && !distortDistributedAttritionDirectAttribution ? 1 : 0,
                    guardianInjectedDistributedDamagePeakTargetsPerWave: isDistributedAttritionFault
                        && !distortDistributedAttritionDirectAttribution ? 5 : 0))
                .ToArray();
        }
    }
}
