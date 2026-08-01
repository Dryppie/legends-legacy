import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, Input, computed, effect, signal } from '@angular/core';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import {
  GuildBuilding,
  GuildBuildingType,
} from '../../../../../../shared/models/Dtos/guild/guildBuilding';
import { RegularButtonComponent } from '../../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { NumberFormatPipe } from '../../../../../../shared/pipes/number-format/number-format.pipe';

@Component({
  selector: 'app-guild-buildings',
  imports: [DatePipe, NgIf, NgFor, NumberFormatPipe, RegularButtonComponent],
  templateUrl: './guild-buildings.component.html',
  styleUrl: './guild-buildings.component.scss',
})
export class GuildBuildingsComponent {
  private readonly hiddenBuildingTypes = new Set<GuildBuildingType>([
    'RaidHall',
    'WarRoom',
    'Workshop',
    'TrainingGrounds',
    'EssenceSanctum',
  ]);

  @Input() memberCap = 10;

  readonly maximumMemberCap = 20;
  readonly overview;
  readonly selected = signal<GuildBuilding | null>(null);

  readonly visibleBuildings = computed(() =>
    (this.overview()?.buildings ?? []).filter(
      (building) =>
        !this.hiddenBuildingTypes.has(building.definition.type),
    ),
  );

  readonly establishedBuildings = computed(() =>
    this.visibleBuildings().filter(
      (building) =>
        building.definition.isPermanent || building.level > 0,
    ),
  );

  readonly builtCount = computed(
    () =>
      this.visibleBuildings().filter(
        (building) => building.definition.isPermanent || building.level > 0,
      ).length,
  );

  readonly availableBuildings = computed(() => {
    const hallLevel = this.overview()?.guildHallLevel ?? 0;
    return this.visibleBuildings().filter(
      (building) =>
        !building.definition.isPermanent &&
        building.level <= 0 &&
        building.definition.requiredGuildHallLevel <= hallLevel,
    );
  });

  readonly lockedBuildings = computed(() => {
    const hallLevel = this.overview()?.guildHallLevel ?? 0;
    return this.visibleBuildings().filter(
      (building) =>
        !building.definition.isPermanent &&
        building.level <= 0 &&
        building.definition.requiredGuildHallLevel > hallLevel,
    );
  });

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
    if (this.selectedSupplyShortfall() > 0) {
      return `${this.selectedSupplyShortfall().toLocaleString()} supplies short`;
    }
    return building.level <= 0 ? 'Build' : 'Upgrade';
  });

  readonly selectedSupplyCost = computed(
    () => this.selected()?.nextCost?.GuildSupplies ?? 0,
  );

  readonly selectedSupplyShortfall = computed(() =>
    Math.max(
      0,
      this.selectedSupplyCost() - (this.overview()?.guildSupplies ?? 0),
    ),
  );

  readonly canActOnSelected = computed(() => {
    const building = this.selected();
    if (!building || this.state.loading()) return false;
    return building.canConstruct || building.canUpgrade;
  });

  constructor(private readonly state: GuildStateService) {
    this.overview = this.state.buildings;

    effect(() => {
      const buildings = this.visibleBuildings();
      const current = this.selected();
      if (buildings.length === 0) return;

      const refreshed = current
        ? buildings.find(
            (building) => building.definition.type === current.definition.type,
          )
        : buildings[0];
      this.selected.set(refreshed ?? buildings[0]);
    });
  }

  select(building: GuildBuilding): void {
    this.selected.set(building);
  }

  actOnSelected(): void {
    const building = this.selected();
    if (!building) return;

    if (building.canConstruct) {
      this.state.constructBuilding(
        building.definition.type as GuildBuildingType,
      );
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
    if (building.level <= 0) return 'Not built';
    return `Level ${building.level}`;
  }

  supplyProgress(building: GuildBuilding): number {
    const required = building.nextCost?.GuildSupplies ?? 0;
    const available = this.overview()?.guildSupplies ?? 0;
    if (required <= 0) return 100;
    return Math.min(100, (available / required) * 100);
  }

  levelSegments(building: GuildBuilding): number[] {
    return Array.from(
      { length: building.definition.maxLevel },
      (_, index) => index + 1,
    );
  }

  nextMemberCap(): number {
    return Math.min(this.maximumMemberCap, this.memberCap + 1);
  }

  isNextBenefit(
    building: GuildBuilding,
    benefit: { level: number; isImplemented: boolean },
  ): boolean {
    const nextLevel = Math.min(
      ...building.definition.benefits
        .filter(
          (candidate) =>
            candidate.isImplemented && candidate.level > building.level,
        )
        .map((candidate) => candidate.level),
    );

    return benefit.isImplemented && benefit.level === nextLevel;
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
    if (this.isNextBenefit(building, benefit)) return 'Next';
    if (state === 'future') return `Level ${benefit.level}`;
    return 'Planned';
  }
}
