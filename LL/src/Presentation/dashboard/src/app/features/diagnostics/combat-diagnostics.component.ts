import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin, Subscription, switchMap, timer } from 'rxjs';
import {
  AbilityBalanceAuditHistoryEntry,
  AbilityBalanceAuditRequest,
  AbilityBalanceCombinationResult,
  AbilityBalanceEssenceResult,
  AbilityBalanceBattleSummary,
  AbilityBalanceSimulationReport,
  AbilityBalanceSimulationRequest,
  AbilityBalanceTeamLoadout,
  AbilityCatalogBehaviorDiagnosticReport,
  AbilityCatalogBehaviorScenarioResult,
  AbilityCatalogCoverageGap,
  AbilityCatalogCoverageReport,
  AbilityCatalogDiagnosticReport,
  AbilityCatalogRuntimeLoadoutCheck,
  AbilityCatalogSummonDiagnostic,
  CombatCharacterProfile,
  CombatCharacterProfileCatalogDocument,
  CombatCharacterProfileCatalogValidationIssue,
  CombatCharacterProfileCatalogValidationReport,
  CombatCharacterProfileGenerationReport,
  CombatCharacterProfileTeam,
  RegionOneContentDiagnosticReport,
  RegionOneContentEntryDiagnostic,
  WorldTowerAuditCampaign,
  WorldTowerCalibrationCertificationIssue,
  WorldTowerCalibrationCertificationReport,
  WorldTowerProfileShadowCalibrationIssue,
  WorldTowerProfileShadowCalibrationReport,
  WorldTowerProfileScenarioRequirement,
} from '../../shared/models/diagnostics/ability-catalog-diagnostics';
import { DiagnosticsService } from '../../core/services/api/diagnostics/diagnostics.service';
import { EssenceCatalogService } from '../../core/services/api/essences/essence-catalog.service';
import {
  EssenceCatalogEssence,
  EssenceCatalogReport,
} from '../../shared/models/essences/essence-catalog';

type DiagnosticsTab = 'catalog' | 'coverage' | 'behaviors' | 'region-one' | 'balance';

@Component({
  selector: 'app-combat-diagnostics',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './combat-diagnostics.component.html',
})
export class CombatDiagnosticsComponent implements OnInit, OnDestroy {
  private readonly savedCombinationsKey = 'll.balance.savedCombinations';
  private readonly auditHistoryKey = 'll.balance.auditHistory';
  private auditRequest?: Subscription;
  private profileRequest?: Subscription;
  private profileBatchRequest?: Subscription;
  private worldTowerRequirementsRequest?: Subscription;
  private profileCatalogRequest?: Subscription;
  private worldTowerShadowRequest?: Subscription;
  private worldTowerCertificationRequest?: Subscription;
  private worldTowerCampaignRequest?: Subscription;
  private worldTowerCampaignPolling?: Subscription;
  private essenceById = new Map<string, EssenceCatalogEssence>();
  readonly tabs: { id: DiagnosticsTab; label: string }[] = [
    { id: 'catalog', label: 'Catalog' },
    { id: 'coverage', label: 'Coverage' },
    { id: 'behaviors', label: 'Behaviors' },
    { id: 'region-one', label: 'Region 1' },
    { id: 'balance', label: 'Balance' },
  ];

  activeTab: DiagnosticsTab = 'catalog';
  catalogReport: AbilityCatalogDiagnosticReport | null = null;
  coverageReport: AbilityCatalogCoverageReport | null = null;
  behaviorReport: AbilityCatalogBehaviorDiagnosticReport | null = null;
  regionOneReport: RegionOneContentDiagnosticReport | null = null;
  essenceCatalog: EssenceCatalogReport | null = null;
  balanceReport: AbilityBalanceSimulationReport | null = null;
  hoveredEssence: EssenceCatalogEssence | null = null;
  essenceTooltipLeft = 0;
  essenceTooltipTop = 0;
  savedCombinations: AbilityBalanceTeamLoadout[] = [];
  balanceSettings = {
    battleCount: 250,
    teamSize: 1,
    essencesPerParticipant: 3,
    randomSeed: 1337,
    topResults: 25,
    candidatePoolSize: 25,
    equipmentTier: 10,
    equipmentRarity: 'Epic',
    equipmentProfile: 'Balanced',
  };
  readonly equipmentTiers = Array.from({ length: 10 }, (_, index) => index + 1);
  readonly equipmentRarities = [
    'Common',
    'Uncommon',
    'Rare',
    'Epic',
    'Unique',
    'Legendary',
  ];
  readonly equipmentProfiles = ['Balanced', 'Offense', 'Sustain', 'Defensive', 'Area'];
  auditSettings = {
    teamSize: 3,
    essencesPerParticipant: 5,
    candidatePoolSize: 500,
    screeningBattleCount: 10000,
    finalistCount: 24,
    finalistBattleCount: 34,
    validationBattleCount: 100,
    randomSeeds: '1337, 2027, 9001',
    equipmentTier: 10,
    equipmentRarity: 'Epic',
    equipmentProfile: 'Balanced',
  };
  profileSettings = {
    contentType: 'WorldTower',
    equipmentQuality: 'Standard',
    teamsPerFamily: 1,
    randomSeed: 1337,
    portfolioMode: 'Expanded',
    minimumSourceBattles: 100,
    minimumMatchupBattles: 100,
    maximumConfidenceWidth95: 0.25,
    maximumSeedScoreSpread: 0.15,
    maximumEssenceOverlap: 0.8,
    requireMultiSeedStability: true,
  };
  readonly profileContentTypes = [
    'Idle',
    'Dungeon',
    'Arena',
    'Tournament',
    'Raid',
    'WorldTower',
    'RegionBoss',
    'QuestTraining',
  ];
  readonly equipmentQualities = ['Standard', 'Fine', 'Exceptional'];
  audits: AbilityBalanceAuditHistoryEntry[] = [];
  selectedAudit: AbilityBalanceAuditHistoryEntry | null = null;
  isRunningAudit = false;
  auditError: string | null = null;
  profileReport: CombatCharacterProfileGenerationReport | null = null;
  isGeneratingProfiles = false;
  profileError: string | null = null;
  worldTowerProfileRequirements: WorldTowerProfileScenarioRequirement[] = [];
  batchRequirementAuditId: Record<string, string> = {};
  isGeneratingProfileBatch = false;
  profileBatchError: string | null = null;
  approvedProfileCatalog: CombatCharacterProfileCatalogValidationReport | null = null;
  profileCatalogCandidate: CombatCharacterProfileCatalogDocument | null = null;
  profileCatalogValidation: CombatCharacterProfileCatalogValidationReport | null = null;
  isValidatingProfileCatalog = false;
  profileCatalogError: string | null = null;
  worldTowerShadowSettings = {
    minimumFloor: 1,
    maximumFloor: 15,
    sampleCount: 10,
    requireExpandedPortfolio: true,
    metaWeight: 0.25,
    typicalWeight: 0.4,
    roleSpecialistWeight: 0.2,
    resilienceWeight: 0.15,
  };
  worldTowerShadowReport: WorldTowerProfileShadowCalibrationReport | null = null;
  isRunningWorldTowerShadow = false;
  worldTowerShadowError: string | null = null;
  worldTowerCertificationSettings = {
    minimumFloor: 1,
    maximumFloor: 15,
    sampleCount: 100,
    minimumSampleCount: 100,
    monotonicTolerance: 0.02,
    maximumTimeoutRate: 0.05,
    requireExpandedPortfolio: true,
    metaWeight: 0.25,
    typicalWeight: 0.4,
    roleSpecialistWeight: 0.2,
    resilienceWeight: 0.15,
    baseRandomSeed: 1337,
    seedManifestId: 'world-tower-certification-v1',
  };
  worldTowerCertificationReport: WorldTowerCalibrationCertificationReport | null = null;
  isRunningWorldTowerCertification = false;
  worldTowerCertificationError: string | null = null;
  worldTowerCampaignSettings = {
    minimumFloor: 1,
    maximumFloor: 15,
    discoveryEquipmentTier: 1,
    discoveryEquipmentRarity: 'Epic',
    discoveryEquipmentProfile: 'Balanced',
    runCandidateVerification: true,
    smokeSampleCount: 10,
    certificationSampleCount: 100,
  };
  worldTowerCampaigns: WorldTowerAuditCampaign[] = [];
  selectedWorldTowerCampaign: WorldTowerAuditCampaign | null = null;
  isCreatingWorldTowerCampaign = false;
  worldTowerCampaignError: string | null = null;
  isSimulating = false;
  simulationError: string | null = null;
  isLoading = false;
  error: string | null = null;

  constructor(
    private diagnosticsService: DiagnosticsService,
    private essenceCatalogService: EssenceCatalogService,
  ) {}

  ngOnInit(): void {
    this.loadSavedCombinations();
    this.loadAuditHistory();
    this.loadApprovedProfileCatalog();
    this.loadWorldTowerProfileRequirements();
    this.loadWorldTowerAuditCampaigns();
    this.loadReports();
  }

  ngOnDestroy(): void {
    this.auditRequest?.unsubscribe();
    this.profileRequest?.unsubscribe();
    this.profileBatchRequest?.unsubscribe();
    this.worldTowerRequirementsRequest?.unsubscribe();
    this.profileCatalogRequest?.unsubscribe();
    this.worldTowerShadowRequest?.unsubscribe();
    this.worldTowerCertificationRequest?.unsubscribe();
    this.worldTowerCampaignRequest?.unsubscribe();
    this.worldTowerCampaignPolling?.unsubscribe();
  }

  loadReports(): void {
    this.isLoading = true;
    this.error = null;

    forkJoin({
      catalog: this.diagnosticsService.getAbilityCatalog(),
      coverage: this.diagnosticsService.getAbilityCatalogCoverage(),
      behavior: this.diagnosticsService.getAbilityCatalogBehaviors(),
      regionOne: this.diagnosticsService.getRegionOneContent(),
      essences: this.essenceCatalogService.getCatalog(),
    })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (reports) => {
          this.catalogReport = reports.catalog;
          this.coverageReport = reports.coverage;
          this.behaviorReport = reports.behavior;
          this.regionOneReport = reports.regionOne;
          this.essenceCatalog = reports.essences;
          this.indexEssences(reports.essences);
        },
        error: (error: Error) => {
          this.error = error.message || 'Unable to load diagnostics.';
        },
      });
  }

  setActiveTab(tab: DiagnosticsTab): void {
    this.activeTab = tab;
  }

  runRandomSimulation(): void {
    this.runSimulation(null);
  }

  runSavedSimulation(): void {
    if (this.savedCombinations.length < 2) {
      this.simulationError = 'Save at least two combinations before running a saved round robin.';
      return;
    }

    this.runSimulation(this.savedCombinations);
  }

  saveTop(count: number): void {
    const ranked = this.balanceReport?.rankedCombinations ?? [];
    if (ranked.length === 0) return;

    const bySignature = new Map<string, AbilityBalanceTeamLoadout>();
    for (const saved of this.savedCombinations) {
      bySignature.set(this.createSignature(saved), saved);
    }

    for (const combination of ranked.slice(0, count)) {
      bySignature.set(combination.signature, {
        participants: combination.participants.map((participant) => ({
          essenceIds: [...participant.essenceIds],
        })),
      });
    }

    this.savedCombinations = [...bySignature.values()];
    this.persistSavedCombinations();
  }

  clearSavedCombinations(): void {
    this.savedCombinations = [];
    this.persistSavedCombinations();
  }

  private runSimulation(candidateTeams: AbilityBalanceTeamLoadout[] | null): void {
    this.isSimulating = true;
    this.simulationError = null;

    const request: AbilityBalanceSimulationRequest = {
      battleCount: Number(this.balanceSettings.battleCount) || 100,
      teamSize: Number(this.balanceSettings.teamSize) || 1,
      essencesPerParticipant:
        Number(this.balanceSettings.essencesPerParticipant) || 1,
      randomSeed: Number(this.balanceSettings.randomSeed) || 1337,
      topResults: Number(this.balanceSettings.topResults) || 25,
      candidatePoolSize: Number(this.balanceSettings.candidatePoolSize) || 25,
      candidateTeams,
      equipmentTier: Number(this.balanceSettings.equipmentTier) || 10,
      equipmentRarity: this.balanceSettings.equipmentRarity,
      equipmentProfile: this.balanceSettings.equipmentProfile,
    };

    this.diagnosticsService
      .runAbilityBalanceSimulation(request)
      .pipe(finalize(() => (this.isSimulating = false)))
      .subscribe({
        next: (report) => {
          this.balanceReport = report;
        },
        error: (error: Error) => {
          this.simulationError =
            error.message || 'Unable to run balance simulation.';
        },
      });
  }

  get isReady(): boolean {
    return Boolean(
        this.catalogReport &&
        this.catalogReport.failures.length === 0 &&
        this.coverageReport?.isComplete &&
        this.behaviorReport?.isComplete &&
        this.behaviorReport.hasFullAbilityCoverage &&
        this.regionOneReport?.staleAreaCount === 0,
    );
  }

  get failedScenarios(): AbilityCatalogBehaviorScenarioResult[] {
    return this.behaviorReport?.scenarios.filter((scenario) => !scenario.passed) ?? [];
  }

  get sortedScenarios(): AbilityCatalogBehaviorScenarioResult[] {
    return [...(this.behaviorReport?.scenarios ?? [])].sort((left, right) => {
      if (left.passed !== right.passed) {
        return left.passed ? 1 : -1;
      }

      return left.abilityId.localeCompare(right.abilityId);
    });
  }

  get failedLoadouts(): AbilityCatalogRuntimeLoadoutCheck[] {
    return this.coverageReport?.runtimeLoadoutChecks.filter((check) => !check.isReady) ?? [];
  }

  get rankedCombinations(): AbilityBalanceCombinationResult[] {
    return this.balanceReport?.rankedCombinations ?? [];
  }

  get incompleteRegionOneEntries(): RegionOneContentEntryDiagnostic[] {
    return this.regionOneReport?.entries.filter((entry) => !entry.isComplete) ?? [];
  }

  get battleSummaries(): AbilityBalanceBattleSummary[] {
    return this.balanceReport?.battleSummaries ?? [];
  }

  get estimatedAuditBattles(): number {
    const seeds = this.parseAuditSeeds();
    const finalists = Math.max(2, Number(this.auditSettings.finalistCount) || 2);
    return (
      seeds.length * Math.max(1, Number(this.auditSettings.screeningBattleCount) || 1) +
      seeds.length * (finalists * (finalists - 1) / 2) *
        Math.max(1, Number(this.auditSettings.finalistBattleCount) || 1) +
      20 * Math.max(1, Number(this.auditSettings.validationBattleCount) || 1)
    );
  }

  get comparisonAudit(): AbilityBalanceAuditHistoryEntry | null {
    const selected = this.selectedAudit;
    if (!selected) return null;
    return (
      this.audits.find(
        (audit) =>
          audit.id !== selected.id &&
          audit.request.teamSize === selected.request.teamSize &&
          audit.request.essencesPerParticipant === selected.request.essencesPerParticipant &&
          audit.request.equipmentTier === selected.request.equipmentTier &&
          audit.request.equipmentRarity === selected.request.equipmentRarity &&
          audit.request.equipmentProfile === selected.request.equipmentProfile,
      ) ?? null
    );
  }

  runFullAudit(): void {
    this.isRunningAudit = true;
    this.auditError = null;
    const request: AbilityBalanceAuditRequest = {
      teamSize: Number(this.auditSettings.teamSize) || 3,
      essencesPerParticipant: Number(this.auditSettings.essencesPerParticipant) || 5,
      candidatePoolSize: Number(this.auditSettings.candidatePoolSize) || 1000,
      screeningBattleCount: Number(this.auditSettings.screeningBattleCount) || 25000,
      finalistCount: Number(this.auditSettings.finalistCount) || 100,
      finalistBattleCount: Number(this.auditSettings.finalistBattleCount) || 10,
      validationBattleCount: Number(this.auditSettings.validationBattleCount) || 200,
      randomSeeds: this.parseAuditSeeds(),
      equipmentTier: Number(this.auditSettings.equipmentTier) || 10,
      equipmentRarity: this.auditSettings.equipmentRarity,
      equipmentProfile: this.auditSettings.equipmentProfile,
    };

    this.auditRequest = this.diagnosticsService
      .runAbilityBalanceAudit(request)
      .pipe(finalize(() => (this.isRunningAudit = false)))
      .subscribe({
        next: (report) => {
          const audit: AbilityBalanceAuditHistoryEntry = {
            id: `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`,
            request,
            report,
            completedAtUtc: new Date().toISOString(),
          };
          this.audits = [audit, ...this.audits].slice(0, 50);
          this.assignRequirementAudits();
          this.selectedAudit = audit;
          this.profileReport = null;
          this.profileError = null;
          this.persistAuditHistory();
        },
        error: (error: Error) => {
          this.auditError = error.message || 'Unable to run balance audit.';
        },
      });
  }

  cancelAudit(): void {
    this.auditRequest?.unsubscribe();
    this.auditRequest = undefined;
    this.isRunningAudit = false;
  }

  selectAudit(audit: AbilityBalanceAuditHistoryEntry): void {
    this.selectedAudit = audit;
    this.profileReport = null;
    this.profileError = null;
  }

  generateCharacterProfiles(): void {
    const audit = this.selectedAudit;
    if (!audit) {
      this.profileError = 'Select or run a completed balance audit first.';
      return;
    }

    this.isGeneratingProfiles = true;
    this.profileError = null;
    this.profileRequest = this.diagnosticsService
      .generateCombatCharacterProfiles({
        auditId: audit.id,
        audit: audit.report,
        contentType: this.profileSettings.contentType,
        equipmentQuality: this.profileSettings.equipmentQuality,
        teamsPerFamily: Number(this.profileSettings.teamsPerFamily) || 1,
        randomSeed: Number(this.profileSettings.randomSeed) || 1337,
        portfolioMode: this.profileSettings.portfolioMode,
        minimumSourceBattles: Number(this.profileSettings.minimumSourceBattles) || 1,
        minimumMatchupBattles: Number(this.profileSettings.minimumMatchupBattles) || 1,
        maximumConfidenceWidth95: Number(this.profileSettings.maximumConfidenceWidth95),
        maximumSeedScoreSpread: Number(this.profileSettings.maximumSeedScoreSpread),
        maximumEssenceOverlap: Number(this.profileSettings.maximumEssenceOverlap),
        requireMultiSeedStability: this.profileSettings.requireMultiSeedStability,
      })
      .pipe(finalize(() => (this.isGeneratingProfiles = false)))
      .subscribe({
        next: (report) => {
          this.profileReport = report;
        },
        error: (error: Error) => {
          this.profileError = error.message || 'Unable to generate character profiles.';
        },
      });
  }

  exportCharacterProfiles(): void {
    if (!this.profileReport) return;
    this.download(
      `combat-character-profiles-${this.profileReport.auditId}.json`,
      JSON.stringify(this.profileReport, null, 2),
      'application/json',
    );
  }

  createWorldTowerAuditCampaign(): void {
    this.isCreatingWorldTowerCampaign = true;
    this.worldTowerCampaignError = null;
    this.worldTowerCampaignRequest?.unsubscribe();
    this.worldTowerCampaignRequest = this.diagnosticsService
      .createWorldTowerAuditCampaign({
        minimumFloor: Number(this.worldTowerCampaignSettings.minimumFloor) || 1,
        maximumFloor: Number(this.worldTowerCampaignSettings.maximumFloor) || 15,
        candidatePoolSize: Number(this.auditSettings.candidatePoolSize) || 500,
        screeningBattleCount: Number(this.auditSettings.screeningBattleCount) || 10000,
        finalistCount: Number(this.auditSettings.finalistCount) || 24,
        finalistBattleCount: Number(this.auditSettings.finalistBattleCount) || 34,
        validationBattleCount: Number(this.auditSettings.validationBattleCount) || 100,
        randomSeeds: this.parseAuditSeeds(),
        teamsPerFamily: Number(this.profileSettings.teamsPerFamily) || 1,
        profileRandomSeed: Number(this.profileSettings.randomSeed) || 1337,
        minimumSourceBattles: Number(this.profileSettings.minimumSourceBattles) || 100,
        minimumMatchupBattles: Number(this.profileSettings.minimumMatchupBattles) || 100,
        maximumConfidenceWidth95: Number(this.profileSettings.maximumConfidenceWidth95),
        maximumSeedScoreSpread: Number(this.profileSettings.maximumSeedScoreSpread),
        maximumEssenceOverlap: Number(this.profileSettings.maximumEssenceOverlap),
        requireMultiSeedStability: this.profileSettings.requireMultiSeedStability,
        discoveryEquipmentTier:
          Number(this.worldTowerCampaignSettings.discoveryEquipmentTier) || 1,
        discoveryEquipmentRarity: this.worldTowerCampaignSettings.discoveryEquipmentRarity,
        discoveryEquipmentProfile: this.worldTowerCampaignSettings.discoveryEquipmentProfile,
        runCandidateVerification: this.worldTowerCampaignSettings.runCandidateVerification,
        smokeSampleCount: Number(this.worldTowerCampaignSettings.smokeSampleCount) || 10,
        certificationSampleCount:
          Number(this.worldTowerCampaignSettings.certificationSampleCount) || 100,
      })
      .pipe(finalize(() => (this.isCreatingWorldTowerCampaign = false)))
      .subscribe({
        next: (campaign) => {
          this.upsertWorldTowerCampaign(campaign);
          this.selectedWorldTowerCampaign = campaign;
          this.watchWorldTowerCampaign(campaign.id);
        },
        error: (error: Error) => {
          this.worldTowerCampaignError =
            error.message || 'Unable to start the World Tower audit campaign.';
        },
      });
  }

  selectWorldTowerCampaign(campaign: WorldTowerAuditCampaign): void {
    this.selectedWorldTowerCampaign = campaign;
    if (this.isWorldTowerCampaignActive(campaign)) {
      this.watchWorldTowerCampaign(campaign.id);
    } else {
      this.worldTowerCampaignPolling?.unsubscribe();
    }
  }

  selectWorldTowerCampaignById(id: string): void {
    const campaign = this.worldTowerCampaigns.find((candidate) => candidate.id === id);
    if (campaign) this.selectWorldTowerCampaign(campaign);
  }

  cancelWorldTowerAuditCampaign(): void {
    const campaign = this.selectedWorldTowerCampaign;
    if (!campaign) return;
    this.worldTowerCampaignError = null;
    this.diagnosticsService.cancelWorldTowerAuditCampaign(campaign.id).subscribe({
      next: (updated) => {
        this.upsertWorldTowerCampaign(updated);
        this.selectedWorldTowerCampaign = updated;
      },
      error: (error: Error) => {
        this.worldTowerCampaignError = error.message || 'Unable to cancel the campaign.';
      },
    });
  }

  retryWorldTowerAuditCampaign(): void {
    const campaign = this.selectedWorldTowerCampaign;
    if (!campaign) return;
    this.worldTowerCampaignError = null;
    this.diagnosticsService.retryWorldTowerAuditCampaign(campaign.id).subscribe({
      next: (updated) => {
        this.upsertWorldTowerCampaign(updated);
        this.selectedWorldTowerCampaign = updated;
        this.watchWorldTowerCampaign(updated.id);
      },
      error: (error: Error) => {
        this.worldTowerCampaignError = error.message || 'Unable to retry the campaign.';
      },
    });
  }

  stageWorldTowerCampaignCatalog(): void {
    const campaign = this.selectedWorldTowerCampaign;
    if (!campaign?.catalogIsValid) return;
    this.diagnosticsService.getWorldTowerAuditCampaignCatalog(campaign.id).subscribe({
      next: (catalog) => {
        this.profileCatalogCandidate = catalog;
        this.profileCatalogValidation = null;
        this.validateProfileCatalog();
      },
      error: (error: Error) => {
        this.worldTowerCampaignError = error.message || 'Unable to load the campaign catalog.';
      },
    });
  }

  exportWorldTowerCampaignCatalog(): void {
    const campaign = this.selectedWorldTowerCampaign;
    if (!campaign?.catalogIsValid || !campaign.isPromotionReady) return;
    this.diagnosticsService.getWorldTowerAuditCampaignCatalog(campaign.id).subscribe({
      next: (catalog) => this.download(
        'combat-character-profiles.json',
        JSON.stringify(catalog, null, 2),
        'application/json',
      ),
      error: (error: Error) => {
        this.worldTowerCampaignError = error.message || 'Unable to export the campaign catalog.';
      },
    });
  }

  exportWorldTowerCampaignEvidence(): void {
    const campaign = this.selectedWorldTowerCampaign;
    if (!campaign) return;
    this.diagnosticsService.getWorldTowerAuditCampaignEvidence(campaign.id).subscribe({
      next: (evidence) => this.download(
        `world-tower-audit-campaign-${campaign.id}.json`,
        JSON.stringify(evidence, null, 2),
        'application/json',
      ),
      error: (error: Error) => {
        this.worldTowerCampaignError = error.message || 'Unable to export campaign evidence.';
      },
    });
  }

  isWorldTowerCampaignActive(campaign: WorldTowerAuditCampaign): boolean {
    return [
      'Queued',
      'RunningAudits',
      'GeneratingCatalog',
      'RunningCandidateSmoke',
      'RunningCandidateCertification',
    ].includes(campaign.status);
  }

  trackWorldTowerCampaign(_: number, campaign: WorldTowerAuditCampaign): string {
    return campaign.id;
  }

  trackWorldTowerCampaignAudit(_: number, audit: { id: string }): string {
    return audit.id;
  }

  generateWorldTowerProfileBatch(): void {
    const selected = this.worldTowerProfileRequirements
      .map((requirement) => ({
        requirement,
        audit: this.audits.find(
          (candidate) => candidate.id === this.batchRequirementAuditId[requirement.scenarioId],
        ),
      }))
      .filter((entry): entry is { requirement: WorldTowerProfileScenarioRequirement; audit: AbilityBalanceAuditHistoryEntry } => !!entry.audit);
    if (selected.length === 0) {
      this.profileBatchError = 'Run and select at least one audit matching a required Tower scenario.';
      return;
    }

    this.isGeneratingProfileBatch = true;
    this.profileBatchError = null;
    this.profileBatchRequest = this.diagnosticsService
      .generateCombatCharacterProfileBatch({
        requests: selected.map(({ requirement, audit }) => ({
          auditId: `${audit.id}:${requirement.scenarioId}`,
          audit: audit.report,
          contentType: 'WorldTower',
          equipmentQuality: requirement.equipmentQuality,
          teamsPerFamily: Number(this.profileSettings.teamsPerFamily) || 1,
          randomSeed: Number(this.profileSettings.randomSeed) || 1337,
          portfolioMode: 'Expanded',
          minimumSourceBattles: Number(this.profileSettings.minimumSourceBattles) || 1,
          minimumMatchupBattles: Number(this.profileSettings.minimumMatchupBattles) || 1,
          maximumConfidenceWidth95: Number(this.profileSettings.maximumConfidenceWidth95),
          maximumSeedScoreSpread: Number(this.profileSettings.maximumSeedScoreSpread),
          maximumEssenceOverlap: Number(this.profileSettings.maximumEssenceOverlap),
          requireMultiSeedStability: this.profileSettings.requireMultiSeedStability,
          targetTeamSize: requirement.teamSize,
          targetEquipmentTier: requirement.equipmentTier,
          targetEquipmentRarity: requirement.equipmentRarity,
        })),
      })
      .pipe(finalize(() => (this.isGeneratingProfileBatch = false)))
      .subscribe({
        next: (report) => {
          const validation = report.catalogValidation;
          if (!validation.isValid) {
            this.profileCatalogValidation = validation;
            this.profileBatchError = 'The generated batch failed catalog validation. Review the issues below.';
            return;
          }
          const base = this.profileCatalogCandidate
            ?? this.approvedProfileCatalog?.normalizedCatalog
            ?? { schemaVersion: 1, catalogVersion: 1, profileSets: [] };
          const generatedKeys = new Set(
            validation.normalizedCatalog.profileSets.map((profileSet) => this.profileSetKey(profileSet)),
          );
          this.profileCatalogCandidate = {
            schemaVersion: validation.normalizedCatalog.schemaVersion,
            catalogVersion: validation.normalizedCatalog.catalogVersion,
            profileSets: [
              ...base.profileSets.filter(
                (profileSet) => !generatedKeys.has(this.profileSetKey(profileSet)),
              ),
              ...validation.normalizedCatalog.profileSets,
            ],
          };
          this.profileCatalogValidation = null;
          this.validateProfileCatalog();
        },
        error: (error: Error) => {
          this.profileBatchError = error.message || 'Unable to generate the World Tower profile batch.';
        },
      });
  }

  stageGeneratedProfileSet(): void {
    if (!this.profileReport) return;
    const base = this.profileCatalogCandidate
      ?? this.approvedProfileCatalog?.normalizedCatalog
      ?? { schemaVersion: 1, catalogVersion: 1, profileSets: [] };
    const replacementKey = this.profileSetKey(this.profileReport);
    const retained = base.profileSets.filter(
      (profileSet) => this.profileSetKey(profileSet) !== replacementKey,
    );
    this.profileCatalogCandidate = {
      schemaVersion: 1,
      catalogVersion: 1,
      profileSets: [...retained, this.profileReport],
    };
    this.profileCatalogValidation = null;
    this.profileCatalogError = null;
  }

  retireProfileSet(profileSet: CombatCharacterProfileGenerationReport): void {
    if (!this.profileCatalogCandidate) return;
    this.profileCatalogCandidate = {
      ...this.profileCatalogCandidate,
      profileSets: this.profileCatalogCandidate.profileSets.filter(
        (candidate) => candidate.auditId !== profileSet.auditId,
      ),
    };
    this.profileCatalogValidation = null;
  }

  importProfileCatalog(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.profileCatalogError = null;
    file.text()
      .then((text) => {
        const parsed = JSON.parse(text) as Partial<CombatCharacterProfileCatalogDocument>
          & Partial<CombatCharacterProfileGenerationReport>;
        if (Array.isArray(parsed.profileSets)) {
          this.profileCatalogCandidate = parsed as CombatCharacterProfileCatalogDocument;
        } else if (Array.isArray(parsed.teams)) {
          this.profileCatalogCandidate = {
            schemaVersion: 1,
            catalogVersion: 1,
            profileSets: [parsed as CombatCharacterProfileGenerationReport],
          };
        } else {
          throw new Error('The file is neither a profile catalog nor a generated profile report.');
        }
        this.profileCatalogValidation = null;
        this.validateProfileCatalog();
      })
      .catch((error: Error) => {
        this.profileCatalogError = error.message || 'Unable to import the profile catalog.';
      });
  }

  validateProfileCatalog(): void {
    if (!this.profileCatalogCandidate) return;
    this.isValidatingProfileCatalog = true;
    this.profileCatalogError = null;
    this.profileCatalogRequest?.unsubscribe();
    this.profileCatalogRequest = this.diagnosticsService
      .validateCombatCharacterProfileCatalog(this.profileCatalogCandidate)
      .pipe(finalize(() => (this.isValidatingProfileCatalog = false)))
      .subscribe({
        next: (report) => {
          this.profileCatalogValidation = report;
          if (report.isValid) {
            this.profileCatalogCandidate = report.normalizedCatalog;
          }
        },
        error: (error: Error) => {
          this.profileCatalogError = error.message || 'Unable to validate the profile catalog.';
        },
      });
  }

  exportValidatedProfileCatalog(): void {
    const validation = this.profileCatalogValidation;
    if (!validation?.isValid) return;
    this.download(
      'combat-character-profiles.json',
      JSON.stringify(validation.normalizedCatalog, null, 2),
      'application/json',
    );
  }

  runWorldTowerShadowCalibration(): void {
    this.isRunningWorldTowerShadow = true;
    this.worldTowerShadowError = null;
    this.worldTowerShadowReport = null;
    this.worldTowerShadowRequest = this.diagnosticsService
      .runWorldTowerProfileShadowCalibration({
        minimumFloor: Number(this.worldTowerShadowSettings.minimumFloor) || 1,
        maximumFloor: Number(this.worldTowerShadowSettings.maximumFloor) || 15,
        sampleCount: Number(this.worldTowerShadowSettings.sampleCount) || 10,
        requireExpandedPortfolio: this.worldTowerShadowSettings.requireExpandedPortfolio,
        weightPolicy: {
          meta: Number(this.worldTowerShadowSettings.metaWeight),
          typical: Number(this.worldTowerShadowSettings.typicalWeight),
          roleSpecialist: Number(this.worldTowerShadowSettings.roleSpecialistWeight),
          resilience: Number(this.worldTowerShadowSettings.resilienceWeight),
        },
      })
      .pipe(finalize(() => (this.isRunningWorldTowerShadow = false)))
      .subscribe({
        next: (report) => {
          this.worldTowerShadowReport = report;
        },
        error: (error: Error) => {
          this.worldTowerShadowError = error.message || 'Unable to run World Tower shadow calibration.';
        },
      });
  }

  exportWorldTowerShadowCalibration(): void {
    if (!this.worldTowerShadowReport) return;
    this.download(
      'world-tower-profile-shadow-calibration.json',
      JSON.stringify(this.worldTowerShadowReport, null, 2),
      'application/json',
    );
  }

  runWorldTowerCalibrationCertification(): void {
    this.isRunningWorldTowerCertification = true;
    this.worldTowerCertificationError = null;
    this.worldTowerCertificationReport = null;
    this.worldTowerCertificationRequest = this.diagnosticsService
      .runWorldTowerCalibrationCertification({
        minimumFloor: Number(this.worldTowerCertificationSettings.minimumFloor) || 1,
        maximumFloor: Number(this.worldTowerCertificationSettings.maximumFloor) || 15,
        sampleCount: Number(this.worldTowerCertificationSettings.sampleCount) || 100,
        minimumSampleCount:
          Number(this.worldTowerCertificationSettings.minimumSampleCount) || 100,
        monotonicTolerance:
          Number(this.worldTowerCertificationSettings.monotonicTolerance),
        maximumTimeoutRate:
          Number(this.worldTowerCertificationSettings.maximumTimeoutRate),
        requireExpandedPortfolio:
          this.worldTowerCertificationSettings.requireExpandedPortfolio,
        weightPolicy: {
          meta: Number(this.worldTowerCertificationSettings.metaWeight),
          typical: Number(this.worldTowerCertificationSettings.typicalWeight),
          roleSpecialist: Number(this.worldTowerCertificationSettings.roleSpecialistWeight),
          resilience: Number(this.worldTowerCertificationSettings.resilienceWeight),
        },
        baseRandomSeed: Number(this.worldTowerCertificationSettings.baseRandomSeed) || 1337,
        seedManifestId:
          this.worldTowerCertificationSettings.seedManifestId.trim()
          || 'world-tower-certification-v1',
      })
      .pipe(finalize(() => (this.isRunningWorldTowerCertification = false)))
      .subscribe({
        next: (report) => {
          this.worldTowerCertificationReport = report;
        },
        error: (error: Error) => {
          this.worldTowerCertificationError =
            error.message || 'Unable to run World Tower calibration certification.';
        },
      });
  }

  exportWorldTowerCalibrationCertification(): void {
    if (!this.worldTowerCertificationReport) return;
    this.download(
      'world-tower-calibration-certification.json',
      JSON.stringify(this.worldTowerCertificationReport, null, 2),
      'application/json',
    );
  }

  trackProfileSet(_: number, profileSet: CombatCharacterProfileGenerationReport): string {
    return profileSet.auditId;
  }

  profileCountPerTeam(profileSet: CombatCharacterProfileGenerationReport): number {
    return profileSet.teams.length > 0 ? profileSet.teams[0].profiles.length : 0;
  }

  trackProfileCatalogIssue(_: number, issue: CombatCharacterProfileCatalogValidationIssue): string {
    return `${issue.code}:${issue.path}`;
  }

  trackWorldTowerRequirement(_: number, requirement: WorldTowerProfileScenarioRequirement): string {
    return requirement.scenarioId;
  }

  matchingAudits(requirement: WorldTowerProfileScenarioRequirement): AbilityBalanceAuditHistoryEntry[] {
    return this.audits.filter((audit) =>
      audit.request.teamSize === 5
      && audit.request.essencesPerParticipant === requirement.essencesPerParticipant);
  }

  trackWorldTowerShadowIssue(_: number, issue: WorldTowerProfileShadowCalibrationIssue): string {
    return `${issue.floorNumber ?? 'catalog'}:${issue.code}`;
  }

  trackWorldTowerCertificationIssue(
    _: number,
    issue: WorldTowerCalibrationCertificationIssue,
  ): string {
    return `${issue.floorNumber ?? 'global'}:${issue.code}`;
  }

  exportAuditCsv(): void {
    const results = this.selectedAudit?.report?.essenceResults;
    if (!results?.length) return;
    const rows = [
      [
        'Essence',
        'Classification',
        'Score',
        'Score Delta',
        'Adjusted Delta',
        'Confidence Lower',
        'Confidence Upper',
        'Battles',
        'Team Appearances',
        'Average Damage Done',
        'Average Damage Taken',
        'Average Duration',
      ],
      ...results.map((result) => [
        result.displayName,
        result.classification,
        result.score,
        result.scoreDelta,
        result.adjustedScoreDelta,
        result.confidenceLower,
        result.confidenceUpper,
        result.battles,
        result.teamAppearances,
        result.averageDamageDone,
        result.averageDamageTaken,
        result.averageDuration,
      ]),
    ];
    this.download(
      `ability-balance-${this.selectedAudit!.id}.csv`,
      rows.map((row) => row.map((value) => this.csvValue(value)).join(',')).join('\n'),
      'text/csv',
    );
  }

  exportAuditJson(): void {
    if (!this.selectedAudit?.report) return;
    this.download(
      `ability-balance-${this.selectedAudit.id}.json`,
      JSON.stringify(this.selectedAudit, null, 2),
      'application/json',
    );
  }

  comparisonDelta(essence: AbilityBalanceEssenceResult): number | null {
    const previous = this.comparisonAudit?.report?.essenceResults.find(
      (candidate) => candidate.essenceId === essence.essenceId,
    );
    return previous
      ? essence.adjustedScoreDelta - previous.adjustedScoreDelta
      : null;
  }

  classificationClass(classification: string): string {
    switch (classification) {
      case 'Overperforming':
        return 'text-red-200 border-red-300/50 bg-red-950/30';
      case 'Underperforming':
        return 'text-sky-200 border-sky-300/50 bg-sky-950/30';
      case 'Healthy':
        return 'text-emerald-200 border-emerald-300/50 bg-emerald-950/30';
      default:
        return 'text-light_gray border-light_gray/40 bg-black/20';
    }
  }

  showEssenceTooltip(essenceId: string, event: MouseEvent | FocusEvent): void {
    this.hoveredEssence = this.findEssence(essenceId);
    if (!this.hoveredEssence) return;

    if (event instanceof MouseEvent) {
      this.positionEssenceTooltip(event);
      return;
    }

    const bounds = (event.currentTarget as HTMLElement).getBoundingClientRect();
    this.setEssenceTooltipPosition(bounds.right, bounds.top);
  }

  positionEssenceTooltip(event: MouseEvent): void {
    if (!this.hoveredEssence) return;
    this.setEssenceTooltipPosition(event.clientX, event.clientY);
  }

  hideEssenceTooltip(): void {
    this.hoveredEssence = null;
  }

  formatCooldown(cooldownTicks: number): string {
    return `${Math.round(cooldownTicks) / 10}s cooldown`;
  }

  essenceDisplayName(essence: AbilityBalanceEssenceResult): string {
    return this.findEssence(essence.essenceId)?.name ?? essence.displayName;
  }

  trackAudit(_: number, audit: AbilityBalanceAuditHistoryEntry): string {
    return audit.id;
  }

  trackProfileTeam(_: number, team: CombatCharacterProfileTeam): string {
    return team.id;
  }

  trackProfile(_: number, profile: CombatCharacterProfile): string {
    return profile.id;
  }

  trackEssence(_: number, essence: AbilityBalanceEssenceResult): string {
    return essence.essenceId;
  }

  trackScenario(_: number, scenario: AbilityCatalogBehaviorScenarioResult): string {
    return scenario.behaviorId;
  }

  trackCoverageGap(_: number, gap: AbilityCatalogCoverageGap): string {
    return `${gap.essenceId}:${gap.slot}:${gap.legacyAbilityId}`;
  }

  trackLoadout(_: number, check: AbilityCatalogRuntimeLoadoutCheck): string {
    return check.essenceId;
  }

  trackSummon(_: number, summon: AbilityCatalogSummonDiagnostic): string {
    return summon.id;
  }

  trackRegionOneEntry(_: number, entry: RegionOneContentEntryDiagnostic): string {
    return `${entry.sourceType}:${entry.sourceName}:${entry.creatureKey}`;
  }

  trackText(_: number, value: string): string {
    return value;
  }

  trackCombination(
    _: number,
    combination: AbilityBalanceCombinationResult,
  ): string {
    return combination.signature;
  }

  formatPercent(value: number): string {
    return `${Math.round(value * 1000) / 10}%`;
  }

  formatNumber(value: number): string {
    return `${Math.round(value * 10) / 10}`;
  }

  formatOptionalPercent(value: number | null): string {
    return value === null ? '—' : this.formatPercent(value);
  }

  private loadApprovedProfileCatalog(): void {
    this.profileCatalogRequest = this.diagnosticsService
      .getCombatCharacterProfileCatalog()
      .subscribe({
        next: (report) => {
          this.approvedProfileCatalog = report;
          this.profileCatalogCandidate = report.normalizedCatalog;
          this.profileCatalogValidation = report;
        },
        error: (error: Error) => {
          this.profileCatalogError = error.message || 'Unable to load the approved profile catalog.';
        },
      });
  }

  private loadWorldTowerProfileRequirements(): void {
    this.worldTowerRequirementsRequest = this.diagnosticsService
      .getWorldTowerProfileRequirements()
      .subscribe({
        next: (requirements) => {
          this.worldTowerProfileRequirements = requirements;
          this.assignRequirementAudits();
        },
        error: (error: Error) => {
          this.profileBatchError = error.message || 'Unable to load World Tower profile requirements.';
        },
      });
  }

  private loadWorldTowerAuditCampaigns(): void {
    this.diagnosticsService.getWorldTowerAuditCampaigns().subscribe({
      next: (campaigns) => {
        this.worldTowerCampaigns = campaigns;
        const active = campaigns.find((campaign) => this.isWorldTowerCampaignActive(campaign));
        this.selectedWorldTowerCampaign = active ?? campaigns[0] ?? null;
        if (active) this.watchWorldTowerCampaign(active.id);
      },
      error: (error: Error) => {
        this.worldTowerCampaignError = error.message || 'Unable to load audit campaigns.';
      },
    });
  }

  private watchWorldTowerCampaign(id: string): void {
    this.worldTowerCampaignPolling?.unsubscribe();
    this.worldTowerCampaignPolling = timer(0, 2000)
      .pipe(switchMap(() => this.diagnosticsService.getWorldTowerAuditCampaign(id)))
      .subscribe({
        next: (campaign) => {
          this.upsertWorldTowerCampaign(campaign);
          if (this.selectedWorldTowerCampaign?.id === id) {
            this.selectedWorldTowerCampaign = campaign;
          }
          if (!this.isWorldTowerCampaignActive(campaign)) {
            this.worldTowerCampaignPolling?.unsubscribe();
          }
        },
        error: (error: Error) => {
          this.worldTowerCampaignError = error.message || 'Unable to refresh campaign progress.';
          this.worldTowerCampaignPolling?.unsubscribe();
        },
      });
  }

  private upsertWorldTowerCampaign(campaign: WorldTowerAuditCampaign): void {
    this.worldTowerCampaigns = [
      campaign,
      ...this.worldTowerCampaigns.filter((candidate) => candidate.id !== campaign.id),
    ].sort((left, right) => right.createdAtUtc.localeCompare(left.createdAtUtc));
  }

  private assignRequirementAudits(): void {
    for (const requirement of this.worldTowerProfileRequirements) {
      if (this.batchRequirementAuditId[requirement.scenarioId]) continue;
      const audit = this.matchingAudits(requirement)[0];
      if (audit) this.batchRequirementAuditId[requirement.scenarioId] = audit.id;
    }
  }

  private profileSetKey(profileSet: CombatCharacterProfileGenerationReport): string {
    if (profileSet.scenario?.id) return profileSet.scenario.id.toLowerCase();
    const profile = profileSet.teams[0]?.profiles[0];
    return [
      profileSet.contentType,
      profile?.equipmentTier ?? '',
      profile?.equipmentRarity ?? '',
      profile?.equipmentQuality ?? '',
      profile?.equipmentProfile ?? '',
    ].join('|').toLowerCase();
  }

  private loadSavedCombinations(): void {
    try {
      const raw = localStorage.getItem(this.savedCombinationsKey);
      this.savedCombinations = raw ? JSON.parse(raw) : [];
    } catch {
      this.savedCombinations = [];
    }
  }

  private findEssence(essenceId: string): EssenceCatalogEssence | null {
    return this.essenceById.get(essenceId.toLowerCase()) ?? null;
  }

  private indexEssences(catalog: EssenceCatalogReport): void {
    this.essenceById = new Map<string, EssenceCatalogEssence>();
    for (const region of catalog.regions) {
      for (const area of region.areas) {
        for (const monster of area.monsters) {
          for (const essence of monster.essences) {
            this.essenceById.set(essence.id.toLowerCase(), essence);
          }
        }
      }
    }
  }

  private setEssenceTooltipPosition(anchorX: number, anchorY: number): void {
    const tooltipWidth = Math.min(448, window.innerWidth - 24);
    const tooltipHeightEstimate = 360;
    const gap = 14;
    this.essenceTooltipLeft = Math.max(
      12,
      Math.min(anchorX + gap, window.innerWidth - tooltipWidth - 12),
    );
    this.essenceTooltipTop = Math.max(
      12,
      Math.min(anchorY + gap, window.innerHeight - tooltipHeightEstimate - 12),
    );
  }

  private loadAuditHistory(): void {
    try {
      const raw = localStorage.getItem(this.auditHistoryKey);
      this.audits = raw ? JSON.parse(raw) : [];
      this.selectedAudit = this.audits[0] ?? null;
    } catch {
      this.audits = [];
      this.selectedAudit = null;
    }
  }

  private persistAuditHistory(): void {
    for (let count = this.audits.length; count > 0; count--) {
      try {
        localStorage.setItem(
          this.auditHistoryKey,
          JSON.stringify(this.audits.slice(0, count)),
        );
        return;
      } catch {
        // Keep the complete in-memory portfolio and try a smaller persisted history.
      }
    }
  }

  private parseAuditSeeds(): number[] {
    const seeds = this.auditSettings.randomSeeds
      .split(',')
      .map((value) => Number(value.trim()))
      .filter((value) => Number.isInteger(value) && value !== 0);
    return [...new Set(seeds)].slice(0, 10).length
      ? [...new Set(seeds)].slice(0, 10)
      : [1337, 2027, 9001];
  }

  private download(filename: string, content: string, type: string): void {
    const url = URL.createObjectURL(new Blob([content], { type }));
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  private csvValue(value: string | number): string {
    const text = `${value}`;
    return `"${text.replace(/"/g, '""')}"`;
  }

  private persistSavedCombinations(): void {
    localStorage.setItem(
      this.savedCombinationsKey,
      JSON.stringify(this.savedCombinations),
    );
  }

  private createSignature(team: AbilityBalanceTeamLoadout): string {
    return team.participants
      .map((participant) => [...participant.essenceIds].sort().join('+'))
      .sort()
      .join(' | ');
  }
}
