import { Injectable, computed, signal } from '@angular/core';
import { finalize } from 'rxjs/operators';
import {
  DungeonRun,
  DungeonService,
  ExecuteDungeonActionRequest,
} from './dungeon.service';
import { DungeonPreviewData } from '../../../../shared/models/Dtos/dungeons/dungeonPreviewData';
import { DungeonDifficulty } from '../../../../shared/models/enums/dungeonDifficulty';

@Injectable({
  providedIn: 'root',
})
export class DungeonStateService {
  /* ─────────── writable signals ─────────── */
  private readonly _dungeons = signal<DungeonPreviewData[]>([]);
  private readonly _activeDungeon = signal<DungeonRun | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  /* ─────────── public, read-only selectors ─────────── */
  readonly dungeons = computed(() => this._dungeons());
  readonly activeDungeon = computed(() => this._activeDungeon());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());

  readonly hasActiveDungeon = computed(() => !!this._activeDungeon());
  readonly hasAvailableDungeons = computed(() => this._dungeons().length > 0);

  constructor(private readonly service: DungeonService) {
    this.refresh();
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
          console.log(activeDungeon);
          // this.loadAvailableDungeons();
        },
        error: (e) => {
          this._error.set(e.message ?? 'Failed to refresh dungeon data');
          // this.loadAvailableDungeons();
        },
      });
  }

  // loadAvailableDungeons(): void {
  //   this.service.getAvailableDungeons().subscribe({
  //     next: (dungeons) => this._dungeons.set(dungeons),
  //     error: (e) =>
  //       this._error.set(e.message ?? 'Failed to load available dungeons'),
  //   });
  // }

  startDungeon(dungeonId: string, difficulty: DungeonDifficulty): void {
    this._loading.set(true);
    this._error.set(null);

    this.service
      .startDungeon({ dungeonId, difficulty })
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (run) => {
          this._activeDungeon.set(run);
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

    this.service
      .executeDungeonAction(run.id, { actionId, payload })
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (updatedRun) => {
          this._activeDungeon.set(updatedRun);
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

  claimDungeonRewards(): void {
    this._loading.set(true);
    this._error.set(null);

    this.service
      .claimDungeonRewards()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: () => {
          this._activeDungeon.set(null);
          // this.loadAvailableDungeons();
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to claim dungeon rewards'),
      });
  }

  /* ─────────── optional optimistic helpers ─────────── */
  setActiveDungeon(run: DungeonRun | null): void {
    this._activeDungeon.set(run);
  }

  setDungeons(dungeons: DungeonPreviewData[]): void {
    this._dungeons.set(dungeons);
  }

  clearError(): void {
    this._error.set(null);
  }
}
