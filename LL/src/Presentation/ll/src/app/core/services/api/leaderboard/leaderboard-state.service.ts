import { computed, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { LeaderboardDto } from '../../../../shared/models/Dtos/leaderboard/leaderboardDto';
import { LeaderboardService } from './leaderboard.service';

@Injectable({
  providedIn: 'root',
})
export class LeaderboardStateService {
  private readonly _leaderboard = signal<LeaderboardDto | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  /* ---------- public, read-only signals ---------- */
  readonly leaderboard = computed(() => this._leaderboard());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly hasLoaded = computed(() => this._leaderboard() !== null);

  constructor(private leaderboardService: LeaderboardService) {
    this.load();
  }

  load(): void {
    if (this._leaderboard()) return; // already cached
    this.refresh();
  }

  refresh(): void {
    if (this._loading()) return;

    this._loading.set(true);

    this.leaderboardService
      .getLeaderboard()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (data) => this._leaderboard.set(data),
        error: (err) =>
          this._error.set(err.message ?? 'Failed to load leaderboard'),
      });
  }

  /** Ready-made selectors for convenience */
  readonly topWealth = computed(() => this._leaderboard()?.wealth ?? []);
  readonly topCombat = computed(() => this._leaderboard()?.combat ?? []);
  readonly topProfessions = computed(
    () => this._leaderboard()?.professions ?? {},
  );

  byProfession = (key: string) =>
    computed(() => this._leaderboard()?.professions?.[key] ?? []);
}
