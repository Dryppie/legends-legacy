import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import {
  AbilityBalanceCombinationResult,
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
} from '../../shared/models/diagnostics/ability-catalog-diagnostics';
import { DiagnosticsService } from '../../core/services/api/diagnostics/diagnostics.service';

type DiagnosticsTab = 'catalog' | 'coverage' | 'behaviors' | 'balance';

@Component({
  selector: 'app-combat-diagnostics',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './combat-diagnostics.component.html',
})
export class CombatDiagnosticsComponent implements OnInit {
  private readonly savedCombinationsKey = 'll.balance.savedCombinations';
  readonly tabs: { id: DiagnosticsTab; label: string }[] = [
    { id: 'catalog', label: 'Catalog' },
    { id: 'coverage', label: 'Coverage' },
    { id: 'behaviors', label: 'Behaviors' },
    { id: 'balance', label: 'Balance' },
  ];

  activeTab: DiagnosticsTab = 'catalog';
  catalogReport: AbilityCatalogDiagnosticReport | null = null;
  coverageReport: AbilityCatalogCoverageReport | null = null;
  behaviorReport: AbilityCatalogBehaviorDiagnosticReport | null = null;
  balanceReport: AbilityBalanceSimulationReport | null = null;
  savedCombinations: AbilityBalanceTeamLoadout[] = [];
  balanceSettings = {
    battleCount: 250,
    teamSize: 1,
    essencesPerParticipant: 3,
    randomSeed: 1337,
    topResults: 25,
    candidatePoolSize: 25,
  };
  isSimulating = false;
  simulationError: string | null = null;
  isLoading = false;
  error: string | null = null;

  constructor(private diagnosticsService: DiagnosticsService) {}

  ngOnInit(): void {
    this.loadSavedCombinations();
    this.loadReports();
  }

  loadReports(): void {
    this.isLoading = true;
    this.error = null;

    forkJoin({
      catalog: this.diagnosticsService.getAbilityCatalog(),
      coverage: this.diagnosticsService.getAbilityCatalogCoverage(),
      behavior: this.diagnosticsService.getAbilityCatalogBehaviors(),
    })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (reports) => {
          this.catalogReport = reports.catalog;
          this.coverageReport = reports.coverage;
          this.behaviorReport = reports.behavior;
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
        this.behaviorReport.hasFullAbilityCoverage,
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

  get battleSummaries(): AbilityBalanceBattleSummary[] {
    return this.balanceReport?.battleSummaries ?? [];
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

  private loadSavedCombinations(): void {
    try {
      const raw = localStorage.getItem(this.savedCombinationsKey);
      this.savedCombinations = raw ? JSON.parse(raw) : [];
    } catch {
      this.savedCombinations = [];
    }
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
