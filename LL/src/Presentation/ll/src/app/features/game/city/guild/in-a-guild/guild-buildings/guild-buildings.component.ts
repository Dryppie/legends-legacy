import { NgFor, NgIf, NgTemplateOutlet } from '@angular/common';
import { Component, Input, computed, effect, signal } from '@angular/core';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import {
  GuildBuilding,
  GuildBuildingType,
} from '../../../../../../shared/models/Dtos/guild/guildBuilding';
import { RegularButtonComponent } from '../../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { NumberFormatPipe } from '../../../../../../shared/pipes/number-format/number-format.pipe';
import { LocalDatePipe } from '../../../../../../shared/pipes/local-date/local-date.pipe';

@Component({
  selector: 'app-guild-buildings',
  imports: [
    LocalDatePipe,
    NgIf,
    NgFor,
    NgTemplateOutlet,
    NumberFormatPipe,
    RegularButtonComponent,
  ],
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
      (building) => !this.hiddenBuildingTypes.has(building.definition.type),
    ),
  );

  readonly builtCount = computed(
    () =>
      this.visibleBuildings().filter(
        (building) => building.definition.isPermanent || building.level > 0,
      ).length,
  );

  readonly establishedBuildings = computed(() =>
    this.visibleBuildings().filter(
      (building) => building.definition.isPermanent || building.level > 0,
    ),
  );

  readonly availableBuildings = computed(() =>
    this.visibleBuildings().filter(
      (building) => !this.isBuildingLocked(building),
    ),
  );

  readonly readyToBuildBuildings = computed(() =>
    this.availableBuildings().filter(
      (building) =>
        !building.definition.isPermanent &&
        building.level <= 0 &&
        this.supplyShortfall(building) === 0,
    ),
  );

  readonly awaitingSuppliesBuildings = computed(() =>
    this.availableBuildings().filter(
      (building) =>
        !building.definition.isPermanent &&
        building.level <= 0 &&
        this.supplyShortfall(building) > 0,
    ),
  );

  readonly lockedBuildings = computed(() =>
    this.visibleBuildings().filter((building) =>
      this.isBuildingLocked(building),
    ),
  );

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

  readonly isSelectedCurrentTarget = computed(() => {
    const building = this.selected();
    const target = this.overview()?.currentTarget;
    return !!building && target?.type === building.definition.type;
  });

  readonly canSetSelectedAsTarget = computed(() => {
    const building = this.selected();
    const overview = this.overview();
    return !!(
      building &&
      overview?.canManageBuildings &&
      building.level < building.definition.maxLevel &&
      !this.isSelectedCurrentTarget() &&
      !this.state.loading()
    );
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

  setSelectedAsTarget(): void {
    const building = this.selected();
    if (!building || !this.canSetSelectedAsTarget()) return;
    this.state.setBuildingTarget(building);
  }

  isSelected(building: GuildBuilding): boolean {
    return this.selected()?.definition.type === building.definition.type;
  }

  supplyProgress(building: GuildBuilding): number {
    const required = building.nextCost?.GuildSupplies ?? 0;
    const available = this.overview()?.guildSupplies ?? 0;
    if (required <= 0) return 100;
    return Math.min(100, (available / required) * 100);
  }

  isCurrentTarget(building: GuildBuilding): boolean {
    return this.overview()?.currentTarget?.type === building.definition.type;
  }

  isBuildingLocked(building: GuildBuilding): boolean {
    return (
      !building.definition.isPermanent &&
      building.level <= 0 &&
      building.definition.requiredGuildHallLevel >
        (this.overview()?.guildHallLevel ?? 0)
    );
  }

  buildingCardStatusType(
    building: GuildBuilding,
  ): 'target' | 'locked' | 'ready' | 'needs-supplies' | 'complete' {
    if (this.isCurrentTarget(building)) return 'target';
    if (this.isBuildingLocked(building)) return 'locked';
    if (building.level >= building.definition.maxLevel) return 'complete';
    return this.supplyShortfall(building) > 0 ? 'needs-supplies' : 'ready';
  }

  buildingCardStatus(building: GuildBuilding): string {
    switch (this.buildingCardStatusType(building)) {
      case 'target':
        return 'Current target';
      case 'locked':
        return 'Locked';
      case 'complete':
        return 'Max level';
      case 'ready':
        return building.level <= 0 ? 'Ready to build' : 'Ready to upgrade';
      default:
        return 'Needs supplies';
    }
  }

  buildingProgressLabel(building: GuildBuilding): string {
    if (this.isBuildingLocked(building)) {
      return `Requires Guild Hall ${building.definition.requiredGuildHallLevel}`;
    }
    if (building.level >= building.definition.maxLevel) {
      return 'Maximum level reached';
    }
    return building.level > 0
      ? `Progress to level ${building.level + 1}`
      : 'Supplies gathered';
  }

  supplyShortfall(building: GuildBuilding): number {
    return Math.max(
      0,
      (building.nextCost?.GuildSupplies ?? 0) -
        (this.overview()?.guildSupplies ?? 0),
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
