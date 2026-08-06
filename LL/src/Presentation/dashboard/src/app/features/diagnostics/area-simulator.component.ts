import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { DiagnosticsService } from '../../core/services/api/diagnostics/diagnostics.service';
import {
  AreaSimulationAreaOption,
  AreaSimulationOptions,
  AreaSimulationReport,
  AreaSimulationRequest,
  RegionAreaBalanceReport,
} from '../../shared/models/diagnostics/area-simulation';
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../shared/components/custom-components/dropdown/dropdown.component';

@Component({
  selector: 'app-area-simulator',
  standalone: true,
  imports: [CommonModule, FormsModule, DropdownComponent],
  templateUrl: './area-simulator.component.html',
})
export class AreaSimulatorComponent implements OnInit {
  options: AreaSimulationOptions | null = null;
  report: AreaSimulationReport | null = null;
  regionReport: RegionAreaBalanceReport | null = null;
  loadingOptions = false;
  simulating = false;
  analyzingRegion = false;
  error: string | null = null;

  request: AreaSimulationRequest = {
    areaId: '',
    encounterCount: 100,
    randomSeed: 73901,
    characterProfile: 'Balanced',
    buildId: '',
  };

  constructor(private readonly diagnostics: DiagnosticsService) {}

  ngOnInit(): void {
    this.loadingOptions = true;
    this.diagnostics
      .getAreaSimulationOptions()
      .pipe(finalize(() => (this.loadingOptions = false)))
      .subscribe({
        next: (options) => {
          this.options = options;
          const first = options.areas[0];
          if (first) {
            this.request.areaId = first.id;
            this.request.buildId = first.defaultBuildId;
          }
        },
        error: (error: Error) => {
          this.error = error.message || 'Unable to load area simulator options.';
        },
      });
  }

  runSimulation(): void {
    if (!this.request.areaId || !this.request.buildId || this.simulating) return;
    this.simulating = true;
    this.error = null;
    this.report = null;
    this.diagnostics
      .runAreaSimulation(this.request)
      .pipe(finalize(() => (this.simulating = false)))
      .subscribe({
        next: (report) => (this.report = report),
        error: (error: Error) => {
          this.error = error.message || 'Area simulation failed.';
        },
      });
  }

  analyzeFullRegion(): void {
    const area = this.selectedArea;
    if (!area || this.analyzingRegion) return;
    this.analyzingRegion = true;
    this.error = null;
    this.regionReport = null;
    this.diagnostics
      .analyzeRegionAreaBalance({
        regionKey: area.regionKey,
        encountersPerProfile: Math.min(this.request.encounterCount, 250),
        randomSeed: this.request.randomSeed,
      })
      .pipe(finalize(() => (this.analyzingRegion = false)))
      .subscribe({
        next: (report) => (this.regionReport = report),
        error: (error: Error) => {
          this.error = error.message || 'Region analysis failed.';
        },
      });
  }

  get selectedArea(): AreaSimulationAreaOption | null {
    return this.options?.areas.find((area) => area.id === this.request.areaId) ?? null;
  }

  get areaDropdownOptions(): readonly DropdownOption<string>[] {
    return (this.options?.areas ?? []).map((area) => ({
      label: `${area.globalStep}. ${area.name}`,
      value: area.id,
      detail: `Level ${area.levelRequirement}`,
    }));
  }

  get profileDropdownOptions(): readonly DropdownOption<string>[] {
    return (this.options?.profiles ?? []).map((profile) => ({
      label: profile,
      value: profile,
    }));
  }

  get buildDropdownOptions(): readonly DropdownOption<string>[] {
    return (this.options?.builds ?? []).map((build) => ({
        label: `Tier ${build.tier} ${build.rarity}`,
        value: build.id,
        detail: build.quality,
      }));
  }

  selectArea(selection: DropdownSelection<string>): void {
    this.request.areaId = selection.main;
    const area = this.selectedArea;
    if (area) this.request.buildId = area.defaultBuildId;
  }

  selectProfile(selection: DropdownSelection<string>): void {
    this.request.characterProfile = selection.main;
  }

  selectBuild(selection: DropdownSelection<string>): void {
    this.request.buildId = selection.main;
  }

  statusClass(status: string): string {
    if (status === 'In tolerance') return 'll-text-success';
    if (status === 'Too easy') return 'll-text-warning';
    return 'll-text-danger';
  }

  trackComposition(_: number, item: { composition: string }): string {
    return item.composition;
  }

  trackArea(_: number, item: { areaId: string }): string {
    return item.areaId;
  }
}
