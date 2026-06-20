import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { finalize, forkJoin } from 'rxjs';
import {
  AbilityCatalogV2BehaviorDiagnosticReport,
  AbilityCatalogV2BehaviorScenarioResult,
  AbilityCatalogV2CoverageGap,
  AbilityCatalogV2CoverageReport,
  AbilityCatalogV2DiagnosticReport,
  AbilityCatalogV2RuntimeLoadoutCheck,
  AbilityCatalogV2SummonDiagnostic,
} from '../../shared/models/diagnostics/ability-catalog-v2-behavior-diagnostics';
import { DiagnosticsService } from '../../core/services/api/diagnostics/diagnostics.service';

type DiagnosticsTab = 'catalog' | 'coverage' | 'behaviors';

@Component({
  selector: 'app-combat-diagnostics',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './combat-diagnostics.component.html',
})
export class CombatDiagnosticsComponent implements OnInit {
  readonly tabs: { id: DiagnosticsTab; label: string }[] = [
    { id: 'catalog', label: 'Catalog' },
    { id: 'coverage', label: 'Coverage' },
    { id: 'behaviors', label: 'Behaviors' },
  ];

  activeTab: DiagnosticsTab = 'catalog';
  catalogReport: AbilityCatalogV2DiagnosticReport | null = null;
  coverageReport: AbilityCatalogV2CoverageReport | null = null;
  behaviorReport: AbilityCatalogV2BehaviorDiagnosticReport | null = null;
  isLoading = false;
  error: string | null = null;

  constructor(private diagnosticsService: DiagnosticsService) {}

  ngOnInit(): void {
    this.loadReports();
  }

  loadReports(): void {
    this.isLoading = true;
    this.error = null;

    forkJoin({
      catalog: this.diagnosticsService.getAbilityCatalogV2(),
      coverage: this.diagnosticsService.getAbilityCatalogV2Coverage(),
      behavior: this.diagnosticsService.getAbilityCatalogV2Behaviors(),
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

  get isReady(): boolean {
    return Boolean(
        this.catalogReport &&
        this.catalogReport.failures.length === 0 &&
        this.coverageReport?.isComplete &&
        this.behaviorReport?.isComplete &&
        this.behaviorReport.hasFullAbilityCoverage,
    );
  }

  get failedScenarios(): AbilityCatalogV2BehaviorScenarioResult[] {
    return this.behaviorReport?.scenarios.filter((scenario) => !scenario.passed) ?? [];
  }

  get sortedScenarios(): AbilityCatalogV2BehaviorScenarioResult[] {
    return [...(this.behaviorReport?.scenarios ?? [])].sort((left, right) => {
      if (left.passed !== right.passed) {
        return left.passed ? 1 : -1;
      }

      return left.abilityId.localeCompare(right.abilityId);
    });
  }

  get failedLoadouts(): AbilityCatalogV2RuntimeLoadoutCheck[] {
    return this.coverageReport?.runtimeLoadoutChecks.filter((check) => !check.isReady) ?? [];
  }

  trackScenario(_: number, scenario: AbilityCatalogV2BehaviorScenarioResult): string {
    return scenario.behaviorId;
  }

  trackCoverageGap(_: number, gap: AbilityCatalogV2CoverageGap): string {
    return `${gap.essenceId}:${gap.slot}:${gap.legacyAbilityId}`;
  }

  trackLoadout(_: number, check: AbilityCatalogV2RuntimeLoadoutCheck): string {
    return check.essenceId;
  }

  trackSummon(_: number, summon: AbilityCatalogV2SummonDiagnostic): string {
    return summon.id;
  }

  trackText(_: number, value: string): string {
    return value;
  }
}
