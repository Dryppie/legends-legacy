import { Injectable, computed, signal } from '@angular/core';
import { finalize } from 'rxjs/operators';
import {
  DungeonActionOutcome,
  ClaimDungeonRewardsResponse,
  DismissFailedDungeonRunResponse,
  DungeonRun,
  DungeonService,
  ExecuteDungeonActionResponse,
  StartDungeonRunResponse,
} from './dungeon.service';
import { DungeonPreviewData } from '../../../../shared/models/Dtos/dungeons/dungeonPreviewData';
import { DungeonRecordsData } from '../../../../shared/models/Dtos/dungeons/dungeonRecordsData';
import { DungeonDifficulty } from '../../../../shared/models/enums/dungeonDifficulty';
import { CombatSessionDto } from '../../../../shared/models/Dtos/combatResultDto';
import { CombatService } from '../../client-side/combat/combat.service';
import { Observable, forkJoin, tap } from 'rxjs';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { CharacterStateService } from '../character/character-state.service';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { VersionedMutationResult } from '../api.service';
import { GameRealtimeStore } from '../../real-time/game-realtime/game-realtime-store.service';

@Injectable({
  providedIn: 'root',
})
export class DungeonStateService {
  /* ─────────── writable signals ─────────── */
  private readonly _dungeons = signal<DungeonPreviewData[]>([]);
  private readonly _activeDungeon = signal<DungeonRun | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _combatSession = signal<CombatSessionDto | null>(null);
  private readonly _lastOutcome = signal<DungeonActionOutcome | null>(null);
  private readonly _message = signal<string | null>(null);
  private readonly _sigilFragments = signal(0);
  private readonly _sigilAssemblyEnabled = signal(false);
  private readonly _sigilAssemblyCost = signal(0);
  private activeDungeonEpoch = 0;
  private dungeonHubEpoch = 0;

  /* ─────────── public, read-only selectors ─────────── */
  readonly lastOutcome = computed(() => this._lastOutcome());
  readonly message = computed(() => this._message());
  readonly combatSession = computed(() => this._combatSession());
  readonly dungeons = computed(() => this._dungeons());
  readonly activeDungeon = computed(() => this._activeDungeon());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly sigilFragments = computed(() => this._sigilFragments());
  readonly sigilAssemblyEnabled = computed(() => this._sigilAssemblyEnabled());
  readonly sigilAssemblyCost = computed(() => this._sigilAssemblyCost());

  readonly hasActiveDungeon = computed(() => !!this._activeDungeon());
  readonly hasAvailableDungeons = computed(() => this._dungeons().length > 0);

  constructor(
    private readonly service: DungeonService,
    private readonly combatService: CombatService,
    private readonly inventoryState: InventoryStateService,
    private readonly characterState: CharacterStateService,
    private readonly toast: ToastService,
    private readonly stateSync: StateSyncCoordinator,
    private readonly domainVersions: DomainVersionTracker,
    private readonly realtimeStore: GameRealtimeStore,
  ) {
    this.stateSync.register('dungeons', 'dungeons', () => this.synchronize());
    this.refresh();
  }

  refresh(): void {
    const requestEpoch = ++this.activeDungeonEpoch;
    this._loading.set(true);
    this._error.set(null);

    this.service
      .getActiveDungeon()
      .pipe(
        finalize(() => {
          if (requestEpoch === this.activeDungeonEpoch) {
            this._loading.set(false);
          }
        }),
      )
      .subscribe({
        next: (activeDungeon) => {
          if (requestEpoch !== this.activeDungeonEpoch) return;
          this._activeDungeon.set(activeDungeon);
          this.loadAvailableDungeons();
        },
        error: (e) => {
          if (requestEpoch !== this.activeDungeonEpoch) return;
          this._error.set(e.message ?? 'Failed to refresh dungeon data');
          this.loadAvailableDungeons();
        },
      });
  }

  private synchronize(): Observable<unknown> {
    const activeDungeonEpoch = ++this.activeDungeonEpoch;
    const dungeonHubEpoch = ++this.dungeonHubEpoch;
    this._loading.set(true);
    this._error.set(null);

    return forkJoin({
      activeDungeon: this.service.getActiveDungeon(),
      hub: this.service.getAvailableDungeons(),
    }).pipe(
      tap({
        next: ({ activeDungeon, hub }) => {
          if (activeDungeonEpoch === this.activeDungeonEpoch) {
            this._activeDungeon.set(activeDungeon);
          }
          if (dungeonHubEpoch === this.dungeonHubEpoch) {
            this._dungeons.set(hub.dungeons);
            this._sigilFragments.set(hub.sigilFragments);
            this._sigilAssemblyEnabled.set(hub.sigilAssemblyEnabled);
            this._sigilAssemblyCost.set(hub.sigilAssemblyCost);
          }
        },
        error: (error) => {
          if (activeDungeonEpoch === this.activeDungeonEpoch) {
            this._error.set(
              error?.message ?? 'Failed to synchronize dungeon data',
            );
          }
        },
      }),
      finalize(() => {
        if (activeDungeonEpoch === this.activeDungeonEpoch) {
          this._loading.set(false);
        }
      }),
    );
  }

  loadAvailableDungeons(): void {
    this.synchronizeAvailableDungeons().subscribe({
      error: () => undefined,
    });
  }

  private synchronizeAvailableDungeons(): Observable<unknown> {
    const requestEpoch = ++this.dungeonHubEpoch;
    return this.service.getAvailableDungeons().pipe(
      tap({
        next: (hub) => {
          if (requestEpoch !== this.dungeonHubEpoch) return;
          this._dungeons.set(hub.dungeons);
          this._sigilFragments.set(hub.sigilFragments);
          this._sigilAssemblyEnabled.set(hub.sigilAssemblyEnabled);
          this._sigilAssemblyCost.set(hub.sigilAssemblyCost);
        },
        error: (e) => {
          if (requestEpoch === this.dungeonHubEpoch) {
            this._error.set(e.message ?? 'Failed to load available dungeons');
          }
        },
      }),
    );
  }

  assembleSigil(dungeonId: string): void {
    if (this._loading()) return;

    this._loading.set(true);
    this._error.set(null);
    this.service
      .assembleSigil(dungeonId)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => {
          this.applyVersionedDungeonState(response, (data) =>
            this.setDungeonHub(data.hub),
          );
          this.inventoryState.applyVersionedInventory(response);
          this.characterState.applyVersionedCharacter(response);
          this.toast.showToast(
            'Sigil assembled',
            response.data.sigilName,
            true,
          );
        },
        error: (error) => {
          const message =
            error?.errorMessage ?? error?.message ?? 'Failed to assemble sigil';
          this._error.set(message);
          this.toast.showToast('Sigil assembly failed', message, false);
        },
      });
  }

  getDungeonRecords(familyId: string): Observable<DungeonRecordsData> {
    return this.service.getDungeonRecords(familyId);
  }

  startDungeon(
    dungeonId: string,
    difficulty: DungeonDifficulty,
    onSuccess?: () => void,
  ): void {
    this._loading.set(true);
    this._error.set(null);

    this.service
      .startDungeon({ dungeonId, dungeonTier: difficulty })
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => {
          this.applyStartDungeon(response);
          onSuccess?.();
        },
        error: (e) => this._error.set(e.message ?? 'Failed to start dungeon'),
      });
  }

  executeAction(actionId: string, payload?: unknown): void {
    const run = this._activeDungeon();
    if (!run?.id) {
      this._error.set('No active dungeon run found');
      return;
    }

    this._loading.set(true);
    this._error.set(null);
    this._message.set(null);

    this.service
      .executeDungeonAction(run.id, { actionId, payload })
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (result) => {
          if (
            this.applyVersionedDungeonState(result, (response) => {
              this.setActiveDungeon(response.run);
              this.setDungeonHub(response.hub);
              this._lastOutcome.set(response.outcome);
              this._combatSession.set(response.combatSession ?? null);
              this._message.set(response.message ?? null);
            })
          ) {
            this.handleActionCombat(result.data);
          }
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to execute dungeon action'),
      });
  }

  fight(): void {
    this.executeAction('fight');
  }

  restAtSite(): void {
    this.executeAction('rest');
  }

  chooseRoute(routeOptionId: string): void {
    this.executeAction('choose_route', { routeOptionId });
  }

  retreat(): void {
    this.executeAction('retreat');
  }

  claimDungeonRewards(
    onSuccess?: (response: ClaimDungeonRewardsResponse) => void,
  ): void {
    if (this._loading()) return;

    this._loading.set(true);
    this._error.set(null);

    this.service
      .claimDungeonRewards()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (result) => {
          this.applyClaimDungeonRewards(result);
          onSuccess?.(result.data);
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to claim dungeon rewards'),
      });
  }

  dismissFailedDungeonRun(onSuccess?: () => void): void {
    this._loading.set(true);
    this._error.set(null);

    this.service
      .dismissFailedDungeonRun()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => {
          this.applyDismissFailedDungeonRun(response);
          onSuccess?.();
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to leave failed dungeon'),
      });
  }

  /* ─────────── optional optimistic helpers ─────────── */
  private handleActionCombat(result: ExecuteDungeonActionResponse): void {
    if (!result.combatSession?.combatResult) return;

    this.combatService.startDungeonCombatSimulation(
      result.combatSession.combatResult,
    );
  }

  private applyClaimDungeonRewards(
    result: VersionedMutationResult<ClaimDungeonRewardsResponse>,
  ): void {
    const response = result.data;
    if (
      this.domainVersions.isCurrent(
        'dungeons',
        result.domainVersions['dungeons'],
      )
    ) {
      this.setActiveDungeon(response.activeRun);
      this.setDungeonHub(response.hub);
      this.realtimeStore.addLoot(
        response.claimedLoot,
        undefined,
        'dungeon-reward',
        response.location,
      );
    }
    this.inventoryState.applyVersionedInventory(result);
    this.characterState.applyVersionedCharacter(result);
  }

  private applyDismissFailedDungeonRun(
    result: VersionedMutationResult<DismissFailedDungeonRunResponse>,
  ): void {
    this.applyVersionedDungeonState(result, (response) => {
      this.setActiveDungeon(response.activeRun);
      this.setDungeonHub(response.hub);
    });
  }

  private applyStartDungeon(
    result: VersionedMutationResult<StartDungeonRunResponse>,
  ): void {
    this.applyVersionedDungeonState(result, (response) => {
      this.setActiveDungeon(response.run);
      this.setDungeonHub(response.hub);
    });
    this.inventoryState.applyVersionedInventory(result);
  }

  private applyVersionedDungeonState<T>(
    result: VersionedMutationResult<T>,
    apply: (response: T) => void,
  ): boolean {
    if (
      !this.domainVersions.isCurrent(
        'dungeons',
        result.domainVersions['dungeons'],
      )
    ) {
      return false;
    }

    apply(result.data);
    return true;
  }

  setActiveDungeon(run: DungeonRun | null): void {
    this.activeDungeonEpoch += 1;
    this._activeDungeon.set(run);
  }

  setDungeons(dungeons: DungeonPreviewData[]): void {
    this.dungeonHubEpoch += 1;
    this._dungeons.set(dungeons);
  }

  private setDungeonHub(hub: {
    dungeons: DungeonPreviewData[];
    sigilFragments: number;
    sigilAssemblyEnabled: boolean;
    sigilAssemblyCost: number;
  }): void {
    this.dungeonHubEpoch += 1;
    this._dungeons.set(hub.dungeons);
    this._sigilFragments.set(hub.sigilFragments);
    this._sigilAssemblyEnabled.set(hub.sigilAssemblyEnabled);
    this._sigilAssemblyCost.set(hub.sigilAssemblyCost);
  }

  clearError(): void {
    this._error.set(null);
  }

  skipDungeonMatch() {
    this.combatService.skipCurrentDungeonMatch();
  }
}
