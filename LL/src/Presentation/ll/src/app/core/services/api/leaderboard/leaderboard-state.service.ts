import { computed, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { LeaderboardBoard } from '../../../../shared/models/Dtos/leaderboard/leaderboard';
import { LeaderboardService } from './leaderboard.service';

@Injectable({ providedIn: 'root' })
export class LeaderboardStateService {
  private readonly _board = signal<LeaderboardBoard | null>(null);
  private readonly _activeKey = signal<string | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private currentCursor: string | null = null;
  private currentSearch: string | null = null;
  private requestSequence = 0;

  readonly board = computed(() => this._board());
  readonly activeKey = computed(() => this._activeKey());
  readonly loading = computed(() => this._loading());
  readonly refreshing = computed(
    () => this._loading() && this._board() !== null,
  );
  readonly error = computed(() => this._error());

  constructor(private readonly leaderboardService: LeaderboardService) {}

  load(boardKey: string, force = false): void {
    if (!force && this._activeKey() === boardKey && this._board()) return;

    const isChangingBoard = this._activeKey() !== boardKey;
    this.request(boardKey, null, null, isChangingBoard);
  }

  loadPage(cursor: string): void {
    const boardKey = this._activeKey();
    if (boardKey && cursor) this.request(boardKey, cursor, null, false);
  }

  jumpToParticipant(search: string): void {
    const boardKey = this._activeKey();
    if (!boardKey) return;

    const normalizedSearch = search.trim();
    this.request(
      boardKey,
      null,
      normalizedSearch.length > 0 ? normalizedSearch : null,
      false,
    );
  }

  clearJump(): void {
    const boardKey = this._activeKey();
    if (boardKey) this.request(boardKey, null, null, false);
  }

  refresh(): void {
    const boardKey = this._activeKey();
    if (boardKey) {
      this.request(boardKey, this.currentCursor, this.currentSearch, false);
    }
  }

  private request(
    boardKey: string,
    cursor: string | null,
    search: string | null,
    clearBoard: boolean,
  ): void {
    const requestId = ++this.requestSequence;
    this._activeKey.set(boardKey);
    this.currentCursor = cursor;
    this.currentSearch = search;
    this._error.set(null);
    this._loading.set(true);
    if (clearBoard) this._board.set(null);

    this.leaderboardService
      .getLeaderboard(boardKey, cursor, search)
      .pipe(
        finalize(() => {
          if (requestId === this.requestSequence) this._loading.set(false);
        }),
      )
      .subscribe({
        next: (board) => {
          if (requestId === this.requestSequence) this._board.set(board);
        },
        error: (error) => {
          if (requestId !== this.requestSequence) return;
          this._error.set(
            error.errorMessage ?? error.message ?? 'Failed to load leaderboard',
          );
        },
      });
  }
}
