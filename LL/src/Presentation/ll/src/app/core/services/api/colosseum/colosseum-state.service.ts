import { computed, effect, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';
import {
  ColosseumService,
  StartArenaBattleResponse,
} from './colosseum.service';
import { CombatService } from '../../client-side/combat/combat.service';
import { ArenaOpponentPreview } from '../../../../shared/models/Dtos/colosseum/arenaOpponentPreview';
import { ArenaTicketStatus } from '../../../../shared/models/Dtos/colosseum/arenaTicketStatus';
import { ColosseumMatchResult } from '../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { LeaderboardEntry } from '../../../../shared/models/Dtos/leaderboard/leaderboardEntry';
import { GameEventService } from '../../real-time/game-event.service';
import { CharacterStateService } from '../character/character-state.service';
import { ArenaBattleCompletedMsg } from '../../real-time/colosseum/arena-battle-completed';

@Injectable({ providedIn: 'root' })
export class ColosseumStateService {
  private readonly _allOpponents = signal<ArenaOpponentPreview[]>([]);
  private readonly _opponents = signal<ArenaOpponentPreview[]>([]);
  private readonly _arenaTicketStatus = signal<ArenaTicketStatus | null>(null);
  private readonly _rankings = signal<LeaderboardEntry[]>([]);
  private readonly _previousMatches = signal<ColosseumMatchResult[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _notificationCount = signal(0);
  private hasLoaded = false;
  private lastArenaBattleCompletedEvent: unknown;

  readonly opponents = computed(() => this._opponents());
  readonly arenaTicketStatus = computed(() => this._arenaTicketStatus());
  readonly rankings = computed(() => this._rankings());
  readonly previousMatches = computed(() => this._previousMatches());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly notificationCount = computed(() => this._notificationCount());

  constructor(
    private readonly colosseumService: ColosseumService,
    private readonly combatService: CombatService,
    private readonly eventService: GameEventService,
    private readonly characterState: CharacterStateService,
  ) {
    effect(
      () => {
        const reconnectCount = this.eventService.reconnectCount();
        if (reconnectCount > 0 && this.hasLoaded) {
          this.refresh();
        }
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const event = this.eventService.event.ArenaBattleCompletedMsg();
        const characterId = this.characterState.currentCharacterId();
        if (
          !event ||
          event === this.lastArenaBattleCompletedEvent ||
          !characterId ||
          !this.isParticipant(event, characterId)
        ) {
          return;
        }

        this.lastArenaBattleCompletedEvent = event;
        this.applyArenaRating(event, characterId);
        this.addNotification();

        if (this.hasLoaded) {
          this.loadArenaOpponents();
          this.loadColosseumRankings();
          this.loadColosseumMatchResults();
        }
      },
      { allowSignalWrites: true },
    );
  }

  refresh(): void {
    this.hasLoaded = true;
    this.loadArenaTicketStatus();
    this.loadArenaOpponents();
    this.loadColosseumRankings();
    this.loadColosseumMatchResults();
  }

  loadArenaTicketStatus(): void {
    this.colosseumService.getArenaTicketStatus().subscribe({
      next: (status) => this.applyTicketStatus(status),
      error: (err) =>
        this._error.set(err.message ?? 'Failed to load arena tickets'),
    });
  }

  loadArenaOpponents(): void {
    this.colosseumService.getArenaOpponents().subscribe({
      next: (data) => {
        this._allOpponents.set(data);
        this.pickRandomOpponents();
      },
      error: (err) =>
        this._error.set(err.message ?? 'Failed to load arena opponents'),
    });
  }

  loadColosseumRankings(): void {
    this.colosseumService.getColosseumRankings().subscribe({
      next: (data) => this._rankings.set(data.sort((a, b) => a.rank - b.rank)),
      error: (err) =>
        this._error.set(err.message ?? 'Failed to load arena rankings'),
    });
  }

  loadColosseumMatchResults(): void {
    this.colosseumService.getColosseumMatchResults().subscribe({
      next: (data) =>
        this._previousMatches.set(
          data.sort(
            (a, b) =>
              new Date(b.playedAt).getTime() - new Date(a.playedAt).getTime(),
          ),
        ),
      error: (err) =>
        this._error.set(err.message ?? 'Failed to load arena match results'),
    });
  }

  pickRandomOpponents(): void {
    this._opponents.set(
      this._allOpponents()
        .map((opponent) => ({ ...opponent }))
        .sort(() => Math.random() - 0.5)
        .slice(0, 5)
        .sort((a, b) => b.opponentRating - a.opponentRating),
    );
  }

  startArenaBattle(enemyId: string): void {
    this._loading.set(true);
    this._error.set(null);

    this.colosseumService
      .startArenaBattle(enemyId)
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (response) => this.applyStartBattleResponse(response),
        error: (err) =>
          this._error.set(err.message ?? 'Failed to start arena battle'),
      });
  }

  skipColosseumMatch(): void {
    this.colosseumService.skipColosseumMatch();
  }

  private applyStartBattleResponse(response: StartArenaBattleResponse): void {
    this.applyTicketStatus(response.arenaTicketStatus);
    this.combatService.startColosseumMatchSimulation(response.battle);
  }

  private applyTicketStatus(status: ArenaTicketStatus): void {
    this._arenaTicketStatus.set(status);
  }

  markNotificationsSeen(): void {
    this._notificationCount.set(0);
  }

  private isParticipant(
    event: ArenaBattleCompletedMsg,
    characterId: string,
  ): boolean {
    return event.characterId === characterId || event.enemyId === characterId;
  }

  private applyArenaRating(
    event: ArenaBattleCompletedMsg,
    characterId: string,
  ): void {
    const character = this.characterState.currentCharacter();
    if (!character) return;

    this.characterState.updateCharacter({
      ...character,
      arenaRating:
        event.characterId === characterId
          ? event.characterRatingAfter
          : event.enemyRatingAfter,
    });
  }

  private addNotification(): void {
    this._notificationCount.update((count) => count + 1);
  }
}
