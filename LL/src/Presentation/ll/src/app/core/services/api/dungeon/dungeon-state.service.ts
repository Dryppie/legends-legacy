import { Injectable, computed, effect, signal } from '@angular/core';
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
import { Observable } from 'rxjs';
import { GameEventService } from '../../real-time/game-event.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { CharacterStateService } from '../character/character-state.service';

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

  /* ─────────── public, read-only selectors ─────────── */
  readonly lastOutcome = computed(() => this._lastOutcome());
  readonly message = computed(() => this._message());
  readonly combatSession = computed(() => this._combatSession());
  readonly dungeons = computed(() => this._dungeons());
  readonly activeDungeon = computed(() => this._activeDungeon());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());

  readonly hasActiveDungeon = computed(() => !!this._activeDungeon());
  readonly hasAvailableDungeons = computed(() => this._dungeons().length > 0);

  constructor(
    private readonly service: DungeonService,
    private readonly combatService: CombatService,
    private readonly eventService: GameEventService,
    private readonly inventoryState: InventoryStateService,
    private readonly characterState: CharacterStateService,
  ) {
    this.refresh();

    effect(
      () => {
        const reconnectCount = this.eventService.reconnectCount();
        if (reconnectCount > 0) {
          this.refresh();
        }
      },
      { allowSignalWrites: true },
    );
  }

  refresh(): void {
    this._loading.set(true);
    this._error.set(null);

    this.service
      .getActiveDungeon()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (activeDungeon) => {
          this._activeDungeon.set(activeDungeon);
          this.loadAvailableDungeons();
        },
        error: (e) => {
          this._error.set(e.message ?? 'Failed to refresh dungeon data');
          this.loadAvailableDungeons();
        },
      });
  }

  loadAvailableDungeons(): void {
    this.service.getAvailableDungeons().subscribe({
      next: (dungeons) => this._dungeons.set(dungeons),
      error: (e) =>
        this._error.set(e.message ?? 'Failed to load available dungeons'),
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
          this._activeDungeon.set(result.run);
          this._lastOutcome.set(result.outcome);
          this._combatSession.set(result.combatSession ?? null);
          this._message.set(result.message ?? null);

          this.handleActionCombat(result);
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to execute dungeon action'),
      });
  }

  fight(): void {
    this.executeAction('fight');
  }

  continueAtCheckpoint(): void {
    this.executeAction('continue');
  }

  withdraw(): void {
    this.executeAction('withdraw');
  }

  leaveDungeon(): void {
    this.executeAction('leave');
  }

  chooseEventAction(actionId: string, payload?: unknown): void {
    this.executeAction(actionId, payload);
  }

  claimDungeonRewards(onSuccess?: () => void): void {
    this._loading.set(true);
    this._error.set(null);

    this.service
      .claimDungeonRewards()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => {
          this.applyClaimDungeonRewards(response);
          onSuccess?.();
          this.loadAvailableDungeons();
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
          this.loadAvailableDungeons();
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

  private applyClaimDungeonRewards(response: ClaimDungeonRewardsResponse): void {
    this._activeDungeon.set(response.activeRun);
    this.inventoryState.setInventory(response.inventoryItems, response.claimedLoot);
    this.characterState.updateCharacter(response.character);
  }

  private applyDismissFailedDungeonRun(
    response: DismissFailedDungeonRunResponse,
  ): void {
    this._activeDungeon.set(response.activeRun);
  }

  private applyStartDungeon(response: StartDungeonRunResponse): void {
    this._activeDungeon.set(response.run);

    if (response.inventoryItems) {
      this.inventoryState.setInventory(response.inventoryItems);
    }
  }

  setActiveDungeon(run: DungeonRun | null): void {
    this._activeDungeon.set(run);
  }

  setDungeons(dungeons: DungeonPreviewData[]): void {
    this._dungeons.set(dungeons);
  }

  clearError(): void {
    this._error.set(null);
  }

  skipDungeonMatch() {
    this.combatService.skipCurrentDungeonMatch();
  }
}
