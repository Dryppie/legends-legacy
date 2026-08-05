import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin, Subscription } from 'rxjs';
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
  RegionOneContentDiagnosticReport,
  RegionOneContentEntryDiagnostic,
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
    teamSize: 2,
    essencesPerParticipant: 5,
    candidatePoolSize: 1000,
    screeningBattleCount: 250000,
    finalistCount: 100,
    finalistBattleCount: 500,
    validationBattleCount: 200,
    randomSeeds: '1337, 2027, 9001',
    equipmentTier: 10,
    equipmentRarity: 'Epic',
    equipmentProfile: 'Balanced',
  };
  audits: AbilityBalanceAuditHistoryEntry[] = [];
  selectedAudit: AbilityBalanceAuditHistoryEntry | null = null;
  isRunningAudit = false;
  auditError: string | null = null;
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
    this.loadReports();
  }

  ngOnDestroy(): void {
    this.auditRequest?.unsubscribe();
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
      (finalists * (finalists - 1) / 2) *
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
      teamSize: Number(this.auditSettings.teamSize) || 2,
      essencesPerParticipant: Number(this.auditSettings.essencesPerParticipant) || 5,
      candidatePoolSize: Number(this.auditSettings.candidatePoolSize) || 1000,
      screeningBattleCount: Number(this.auditSettings.screeningBattleCount) || 250000,
      finalistCount: Number(this.auditSettings.finalistCount) || 100,
      finalistBattleCount: Number(this.auditSettings.finalistBattleCount) || 500,
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
          this.audits = [audit, ...this.audits].slice(0, 10);
          this.selectedAudit = audit;
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
    try {
      localStorage.setItem(this.auditHistoryKey, JSON.stringify(this.audits));
    } catch {
      this.audits = this.audits.slice(0, 3);
      try {
        localStorage.setItem(this.auditHistoryKey, JSON.stringify(this.audits));
      } catch {
        // The completed result remains available for this page session.
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
