import { CommonModule } from '@angular/common';
import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { DefaultHeaderComponent } from '../../../../../shared/components/default-header/default-header.component';
import {
  TowerContributionKind,
  TowerFloorDetail,
  TowerFloorSummary,
  TowerHallOfFameEntry,
  TowerOverview,
  TowerRallyMode,
  TowerRallySummary,
  WorldTowerService,
} from '../../../../../core/services/api/world-tower/world-tower.service';
import { GameEventService } from '../../../../../core/services/real-time/game-event.service';

@Component({
  selector: 'app-tower-overview',
  imports: [CommonModule, RouterLink, DefaultHeaderComponent],
  templateUrl: './tower-overview.component.html',
  styleUrls: ['../tower-page.scss', './tower-overview.component.scss'],
})
export class TowerOverviewComponent implements OnInit {
  private readonly tower = inject(WorldTowerService);
  private readonly router = inject(Router);
  private readonly events = inject(GameEventService);
  private lastRealtimeUpdateId: string | null = null;
  readonly overview = signal<TowerOverview | null>(null);
  readonly selectedFloor = signal<TowerFloorDetail | null>(null);
  readonly selectedFloorNumber = signal<number | null>(null);
  readonly loading = signal(true);
  readonly loadingFloor = signal(false);
  readonly action = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly shopOpen = signal(false);
  readonly preparationExpanded = signal(true);
  readonly expeditionSlotDots = Array.from({ length: 10 }, (_, index) => index);

  constructor() {
    effect(
      () => {
        const envelope = this.events.eventEnvelope.WorldTowerRallyUpdated();
        if (
          !envelope?.updateId ||
          envelope.updateId === this.lastRealtimeUpdateId
        ) {
          return;
        }

        this.lastRealtimeUpdateId = envelope.updateId;
        this.refreshFromRealtime(envelope.payload.floorNumber);
      },
      { allowSignalWrites: true },
    );
  }

  ngOnInit(): void {
    this.load();
  }

  openShop(): void {
    this.shopOpen.set(true);
  }

  closeShop(): void {
    this.shopOpen.set(false);
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.tower
      .getOverview()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (overview) => {
          this.overview.set(overview);
          const selected =
            overview.currentFloor ??
            [...overview.floors]
              .reverse()
              .find((floor) => floor.state !== 'Locked') ??
            null;
          if (selected) this.selectFloor(selected);
        },
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  orderedFloors(overview: TowerOverview): TowerFloorSummary[] {
    return [...overview.floors].reverse();
  }

  selectFloor(floor: TowerFloorSummary): void {
    if (this.selectedFloorNumber() === floor.floorNumber) {
      return;
    }

    this.selectedFloorNumber.set(floor.floorNumber);
    this.selectedFloor.set(null);
    this.loadingFloor.set(true);
    this.error.set(null);
    this.tower
      .getFloor(floor.floorNumber)
      .pipe(finalize(() => this.loadingFloor.set(false)))
      .subscribe({
        next: (detail) => {
          this.selectedFloor.set(detail);
          this.preparationExpanded.set(
            detail.preparation.weeklyCharacterContribution <
              detail.preparation.weeklyCharacterCap &&
              !this.isPreparationComplete(detail),
          );
        },
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  togglePreparation(): void {
    this.preparationExpanded.update((expanded) => !expanded);
  }

  createRally(mode: TowerRallyMode): void {
    const floor = this.selectedFloor();
    if (!floor || this.action()) return;

    this.action.set(`create-${mode}`);
    this.error.set(null);
    this.tower
      .createRally(floor.floorNumber, mode)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (rally) =>
          void this.router.navigate([
            '/game/world/tower/expeditions',
            rally.id,
          ]),
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  contribute(kind: TowerContributionKind): void {
    const floor = this.selectedFloor();
    if (!floor || this.action()) return;

    this.action.set(kind);
    this.error.set(null);
    this.tower
      .contribute(floor.floorNumber, kind)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (detail) => {
          this.selectedFloor.set(detail);
          this.refreshOverviewSummary(detail);
        },
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  preparationEffect(
    floor: TowerFloorDetail,
    kind: Exclude<TowerContributionKind, 'Research'>,
  ): number {
    switch (kind) {
      case 'SupplyWeapons':
        return floor.preparation.supplyWeaponsPercent;
      case 'InscribeWards':
        return floor.preparation.inscribeWardsPercent;
      case 'ScoutWeakPoints':
        return floor.preparation.scoutWeakPointsPercent;
    }
  }

  recruitingRallyCount(floor: TowerFloorDetail): number {
    return floor.activeRallies.filter((rally) => rally.status === 'Recruiting')
      .length;
  }

  inBattleRallyCount(floor: TowerFloorDetail): number {
    return floor.activeRallies.filter((rally) => rally.status === 'InProgress')
      .length;
  }

  rallyStatusLabel(rally: TowerRallySummary): string {
    switch (rally.status) {
      case 'Ready':
        return 'Roster Full';
      case 'InProgress':
        return 'In Battle';
      default:
        return rally.status;
    }
  }

  rallyActionLabel(
    rally: TowerRallySummary,
    currentCharacterRallyId: string | null,
  ): string {
    if (rally.status === 'InProgress') return 'View';
    if (rally.id === currentCharacterRallyId) return 'View';
    if (rally.status === 'Ready') return 'Full';
    return 'Apply';
  }

  rallyOccupancyPercent(rally: TowerRallySummary): number {
    if (rally.requiredSlots <= 0) return 0;
    return Math.min(100, (rally.participantCount / rally.requiredSlots) * 100);
  }

  isPreparationMaxed(
    floor: TowerFloorDetail,
    kind: Exclude<TowerContributionKind, 'Research'>,
  ): boolean {
    return (
      this.preparationEffect(floor, kind) >=
      floor.preparation.maximumEffectPercent
    );
  }

  isPreparationComplete(floor: TowerFloorDetail): boolean {
    return (
      this.isPreparationMaxed(floor, 'SupplyWeapons') &&
      this.isPreparationMaxed(floor, 'InscribeWards') &&
      this.isPreparationMaxed(floor, 'ScoutWeakPoints')
    );
  }

  researchLimitReached(floor: TowerFloorDetail): boolean {
    return floor.weeklyResearchContribution >= floor.weeklyResearchCap;
  }

  researchActionLabel(floor: TowerFloorDetail): string {
    if (floor.scoutingProgress >= 100) return 'Scouting complete';
    if (this.researchLimitReached(floor)) return 'Weekly limit reached';
    return 'Scout now';
  }

  preparationContributionDisabled(
    floor: TowerFloorDetail,
    kind: Exclude<TowerContributionKind, 'Research'>,
  ): boolean {
    return (
      !!this.action() ||
      floor.state === 'Locked' ||
      floor.state === 'Cleared' ||
      floor.preparation.weeklyCharacterContribution >=
        floor.preparation.weeklyCharacterCap ||
      this.isPreparationMaxed(floor, kind)
    );
  }

  preparationActionLabel(
    floor: TowerFloorDetail,
    kind: Exclude<TowerContributionKind, 'Research'>,
  ): string {
    if (floor.state === 'Locked') return 'Floor locked';
    if (this.isPreparationMaxed(floor, kind)) return 'Maxed';
    if (
      floor.preparation.weeklyCharacterContribution >=
      floor.preparation.weeklyCharacterCap
    ) {
      return 'Weekly limit';
    }
    return 'Contribute';
  }

  rosterSummary(record: TowerHallOfFameEntry): string {
    const visible = record.participants
      .slice(0, 3)
      .map((entry) => entry.characterName);
    const remaining = record.participants.length - visible.length;
    return `${visible.join(', ')}${remaining > 0 ? `, +${remaining}` : ''}`;
  }

  duration(seconds: number): string {
    const total = Math.max(0, seconds);
    return `${Math.floor(total / 60)}:${(total % 60).toString().padStart(2, '0')}`;
  }

  floorLabel(floorNumber: number): string {
    return floorNumber.toString().padStart(2, '0');
  }

  floorStateLabel(state: TowerFloorSummary['state']): string {
    return state === 'Rallying' ? 'Expedition forming' : state;
  }

  private refreshOverviewSummary(detail: TowerFloorDetail): void {
    const overview = this.overview();
    if (!overview) return;

    this.overview.set({
      ...overview,
      floors: overview.floors.map((floor) =>
        floor.floorNumber === detail.floorNumber
          ? {
              ...floor,
              state: detail.state,
              scoutingProgress: detail.scoutingProgress,
            }
          : floor,
      ),
    });
  }

  private refreshFromRealtime(floorNumber: number): void {
    this.tower.getOverview().subscribe({
      next: (overview) => this.overview.set(overview),
    });
    if (this.selectedFloorNumber() === floorNumber) {
      this.tower.getFloor(floorNumber).subscribe({
        next: (detail) => this.selectedFloor.set(detail),
      });
    }
  }

  private errorMessage(error: unknown): string {
    return (
      (error as { errorMessage?: string })?.errorMessage ??
      'The Tower could not be reached.'
    );
  }
}
