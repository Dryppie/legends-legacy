import { Injectable, computed, signal } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { ActiveDungeonRun, DungeonService } from './dungeon.service';
import { DungeonPreviewData } from '../../../../shared/models/Dtos/dungeons/dungeonPreviewData';
import { DungeonDifficulty } from '../../../../shared/models/enums/dungeonDifficulty';

@Injectable({
  providedIn: 'root',
})
export class DungeonStateService {
  /* ─────────── writable signals ─────────── */
  private readonly _dungeons = signal<DungeonPreviewData[]>([]);
  private readonly _activeDungeon = signal<ActiveDungeonRun | null>(null);
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

  progressDungeon(): void {
    this._loading.set(true);
    this._error.set(null);

    this.service
      .progressDungeon()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (run) => {
          this._activeDungeon.set(run);
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to progress dungeon'),
      });
  }

  leaveDungeon(): void {
    this._loading.set(true);
    this._error.set(null);

    this.service
      .leaveDungeon()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: () => {
          this._activeDungeon.set(null);
          this.loadAvailableDungeons();
        },
        error: (e) => this._error.set(e.message ?? 'Failed to leave dungeon'),
      });
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
          this.loadAvailableDungeons();
        },
        error: (e) =>
          this._error.set(e.message ?? 'Failed to claim dungeon rewards'),
      });
  }

  /* ─────────── optional optimistic helpers ─────────── */
  setActiveDungeon(run: ActiveDungeonRun | null): void {
    this._activeDungeon.set(run);
  }

  setDungeons(dungeons: DungeonPreviewData[]): void {
    this._dungeons.set(dungeons);
  }

  clearError(): void {
    this._error.set(null);
  }
}
