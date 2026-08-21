import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { finalize, forkJoin } from 'rxjs';
import {
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

type DiagnosticsTab = 'catalog' | 'coverage' | 'behaviors' | 'region-one';

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
    { id: 'region-one', label: 'Region 1' },
  ];

  activeTab: DiagnosticsTab = 'catalog';
  catalogReport: AbilityCatalogDiagnosticReport | null = null;
  coverageReport: AbilityCatalogCoverageReport | null = null;
  behaviorReport: AbilityCatalogBehaviorDiagnosticReport | null = null;
  regionOneReport: RegionOneContentDiagnosticReport | null = null;
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
      catalog: this.diagnosticsService.getAbilityCatalog(),
      coverage: this.diagnosticsService.getAbilityCatalogCoverage(),
      behavior: this.diagnosticsService.getAbilityCatalogBehaviors(),
      regionOne: this.diagnosticsService.getRegionOneContent(),
    })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (reports) => {
          this.catalogReport = reports.catalog;
          this.coverageReport = reports.coverage;
          this.behaviorReport = reports.behavior;
          this.regionOneReport = reports.regionOne;
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
        this.behaviorReport.hasFullAbilityCoverage &&
        this.regionOneReport?.staleAreaCount === 0,
    );
  }

  get failedScenarios(): AbilityCatalogBehaviorScenarioResult[] {
    return this.behaviorReport?.scenarios.filter((scenario) => !scenario.passed) ?? [];
  }

  get sortedScenarios(): AbilityCatalogBehaviorScenarioResult[] {
    return [...(this.behaviorReport?.scenarios ?? [])].sort((left, right) => {
      if (left.passed !== right.passed) return left.passed ? 1 : -1;
      return left.abilityId.localeCompare(right.abilityId);
    });
  }

  get failedLoadouts(): AbilityCatalogRuntimeLoadoutCheck[] {
    return this.coverageReport?.runtimeLoadoutChecks.filter((check) => !check.isReady) ?? [];
  }

  get incompleteRegionOneEntries(): RegionOneContentEntryDiagnostic[] {
    return this.regionOneReport?.entries.filter((entry) => !entry.isComplete) ?? [];
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
}
