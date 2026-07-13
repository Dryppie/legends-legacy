import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { EssenceCatalogService } from '../../core/services/api/essences/essence-catalog.service';
import {
  EssenceCatalogArea,
  EssenceCatalogEffect,
  EssenceCatalogMonster,
  EssenceCatalogRegion,
  EssenceCatalogReport,
} from '../../shared/models/essences/essence-catalog';

@Component({
  selector: 'app-essence-catalog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './essence-catalog.component.html',
})
export class EssenceCatalogComponent implements OnInit {
  report: EssenceCatalogReport | null = null;
  selectedRegionId = '';
  selectedAreaId = '';
  selectedMonsterId = '';
  isLoading = false;
  error: string | null = null;

  constructor(private essenceCatalogService: EssenceCatalogService) {}

  ngOnInit(): void {
    this.loadCatalog();
  }

  loadCatalog(): void {
    this.isLoading = true;
    this.error = null;

    this.essenceCatalogService
      .getCatalog()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (report) => {
          this.report = report;
          this.selectedRegionId = report.regions[0]?.id ?? '';
          this.selectFirstArea();
        },
        error: (error: Error) => {
          this.error = error.message || 'Unable to load essence catalog.';
        },
      });
  }

  onRegionChanged(): void {
    this.selectFirstArea();
  }

  onAreaChanged(): void {
    this.selectFirstMonster();
  }

  get selectedRegion(): EssenceCatalogRegion | null {
    return this.report?.regions.find((region) => region.id === this.selectedRegionId) ?? null;
  }

  get selectedArea(): EssenceCatalogArea | null {
    return this.selectedRegion?.areas.find((area) => area.id === this.selectedAreaId) ?? null;
  }

  get selectedMonster(): EssenceCatalogMonster | null {
    return this.selectedArea?.monsters.find((monster) => monster.id === this.selectedMonsterId) ?? null;
  }

  trackRegion(_: number, region: EssenceCatalogRegion): string {
    return region.id;
  }

  trackArea(_: number, area: EssenceCatalogArea): string {
    return area.id;
  }

  trackMonster(_: number, monster: EssenceCatalogMonster): string {
    return monster.id;
  }

  trackText(_: number, value: string): string {
    return value;
  }

  trackEffect(_: number, effect: EssenceCatalogEffect): string {
    return effect.id;
  }

  formatEffectValue(effect: EssenceCatalogEffect): string {
    const parts = [`${effect.operation} ${effect.baseValue}`];
    if (effect.attribute) parts.push(effect.attribute);
    if (effect.statusId) parts.push(effect.statusId);
    if (effect.summonId) parts.push(effect.summonId);
    if (effect.durationTicks > 0) parts.push(`${effect.durationTicks} ticks`);
    if (effect.intervalTicks > 0) parts.push(`every ${effect.intervalTicks}`);
    return parts.join(' / ');
  }

  private selectFirstArea(): void {
    this.selectedAreaId = this.selectedRegion?.areas[0]?.id ?? '';
    this.selectFirstMonster();
  }

  private selectFirstMonster(): void {
    this.selectedMonsterId = this.selectedArea?.monsters[0]?.id ?? '';
  }
}
