import { CommonModule } from '@angular/common';
import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import {
  NavigationTab,
  NavigationTabsComponent,
} from '../../../../../shared/components/custom-components/tabs/navigation-tabs/navigation-tabs.component';
import { DefaultHeaderComponent } from '../../../../../shared/components/default-header/default-header.component';
import {
  TowerContributionKind,
  TowerFloorDetail,
  TowerFloorSummary,
  TowerHallOfFameEntry,
  TowerOverview,
  TowerRallyMode,
  WorldTowerService,
} from '../../../../../core/services/api/world-tower/world-tower.service';
import { GameEventService } from '../../../../../core/services/real-time/game-event.service';

type TowerWorkspaceTab = 'scouting' | 'preparation' | 'rally';

@Component({
  selector: 'app-tower-overview',
  imports: [
    CommonModule,
    RouterLink,
    NavigationTabsComponent,
    DefaultHeaderComponent,
  ],
  templateUrl: './tower-overview.component.html',
  styleUrl: '../tower-page.scss',
})
export class TowerOverviewComponent implements OnInit {
  private readonly tower = inject(WorldTowerService);
  private readonly router = inject(Router);
  private readonly events = inject(GameEventService);
  private lastRealtimeUpdateId: string | null = null;
  readonly overview = signal<TowerOverview | null>(null);
  readonly selectedFloor = signal<TowerFloorDetail | null>(null);
  readonly selectedFloorNumber = signal<number | null>(null);
  readonly activeTab = signal<TowerWorkspaceTab>('scouting');
  readonly loading = signal(true);
  readonly loadingFloor = signal(false);
  readonly action = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly shopOpen = signal(false);
  readonly workspaceTabs: readonly NavigationTab[] = [
    { key: 'scouting', label: 'Scouting' },
    { key: 'preparation', label: 'Preparation' },
    { key: 'rally', label: 'Expedition' },
  ];

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
    this.activeTab.set('scouting');
    this.loadingFloor.set(true);
    this.error.set(null);
    this.tower
      .getFloor(floor.floorNumber)
      .pipe(finalize(() => this.loadingFloor.set(false)))
      .subscribe({
        next: (detail) => this.selectedFloor.set(detail),
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  setTab(tab: string): void {
    if (tab === 'scouting' || tab === 'preparation' || tab === 'rally') {
      this.activeTab.set(tab);
    }
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

  isPreparationMaxed(
    floor: TowerFloorDetail,
    kind: Exclude<TowerContributionKind, 'Research'>,
  ): boolean {
    return (
      this.preparationEffect(floor, kind) >=
      floor.preparation.maximumEffectPercent
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
