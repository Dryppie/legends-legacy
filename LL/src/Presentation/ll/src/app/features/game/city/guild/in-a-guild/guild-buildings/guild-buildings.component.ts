import { DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Component, computed, effect, signal } from '@angular/core';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import {
  GuildBuilding,
  GuildBuildingType,
} from '../../../../../../shared/models/Dtos/guild/guildBuilding';
import { RegularButtonComponent } from '../../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { HumanizeEnumPipe } from '../../../../../../shared/pipes/enums/humanize-enum.pipe';
import { NumberFormatPipe } from '../../../../../../shared/pipes/number-format/number-format.pipe';

@Component({
    selector: 'app-guild-buildings',
    imports: [
        DatePipe,
        NgIf,
        NgFor,
        NgClass,
        NumberFormatPipe,
        RegularButtonComponent,
        HumanizeEnumPipe,
    ],
    templateUrl: './guild-buildings.component.html'
})
export class GuildBuildingsComponent {
  readonly overview;
  readonly selected = signal<GuildBuilding | null>(null);

  readonly selectedCost = computed(() => {
    const cost = this.selected()?.nextCost ?? {};
    return Object.entries(cost).map(([resource, amount]) => ({
      resource,
      amount: amount ?? 0,
    }));
  });

  readonly selectedActionText = computed(() => {
    const building = this.selected();
    if (!building) return 'Select';
    return building.canConstruct ? 'Construct' : 'Upgrade';
  });

  readonly canActOnSelected = computed(() => {
    const building = this.selected();
    if (!building || this.state.loading()) return false;
    return building.canConstruct || building.canUpgrade;
  });

  constructor(private readonly state: GuildStateService) {
    this.overview = this.state.buildings;

    effect(
      () => {
        const buildings = this.overview()?.buildings ?? [];
        const current = this.selected();
        if (buildings.length === 0) return;

        const refreshed = current
          ? buildings.find((building) => building.definition.type === current.definition.type)
          : buildings[0];
        this.selected.set(refreshed ?? buildings[0]);
      },
      { allowSignalWrites: true },
    );
  }

  select(building: GuildBuilding): void {
    this.selected.set(building);
  }

  actOnSelected(): void {
    const building = this.selected();
    if (!building) return;

    if (building.canConstruct) {
      this.state.constructBuilding(building.definition.type as GuildBuildingType);
      return;
    }

    if (building.canUpgrade) {
      this.state.upgradeBuilding(building);
    }
  }

  isSelected(building: GuildBuilding): boolean {
    return this.selected()?.definition.type === building.definition.type;
  }

  buildingStatusLabel(building: GuildBuilding): string {
    if (building.status === 'UnderConstruction') return 'Constructing';
    if (building.status === 'Upgrading') return `Upgrading to ${building.targetLevel}`;
    if (building.level <= 0) return 'Unbuilt';
    return `Level ${building.level}`;
  }

  buildingCardClass(building: GuildBuilding): string {
    if (this.isSelected(building)) return 'border-primary bg-primary/10';
    if (building.lockedReason) return 'border-zinc-700/70 opacity-80';
    return 'border-zinc-300/30 hover:bg-zinc-800/30';
  }

  statusBadgeClass(building: GuildBuilding): string {
    if (building.status !== 'Active') return 'll-badge-accent';
    if (building.canConstruct || building.canUpgrade) return 'll-badge-success';
    if (building.lockedReason) return 'll-badge-muted';
    return 'll-badge-muted';
  }

  supplyProgress(building: GuildBuilding): number {
    const required = building.nextCost?.GuildSupplies ?? 0;
    const available = this.overview()?.guildSupplies ?? 0;
    if (required <= 0) return 100;
    return Math.min(100, (available / required) * 100);
  }

  benefitState(
    building: GuildBuilding,
    benefit: { level: number; isImplemented: boolean },
  ): 'active' | 'future' | 'planned' {
    if (!benefit.isImplemented) return 'planned';
    return building.level >= benefit.level ? 'active' : 'future';
  }

  benefitStateLabel(
    building: GuildBuilding,
    benefit: { level: number; isImplemented: boolean },
  ): string {
    const state = this.benefitState(building, benefit);
    if (state === 'active') return 'Active';
    if (state === 'future') return `Level ${benefit.level}`;
    return 'Planned';
  }

  benefitClass(
    building: GuildBuilding,
    benefit: { level: number; isImplemented: boolean },
  ): string {
    const state = this.benefitState(building, benefit);
    if (state === 'active') return 'border-primary/70 bg-primary/10';
    if (state === 'future') return 'border-zinc-500/50 bg-zinc-900/40';
    return 'border-zinc-700/70 bg-black/20 opacity-80';
  }
}
