import { Injectable, signal } from '@angular/core';
import {
  TournamentBracket,
  TournamentDetails,
  TournamentGroundsStatus,
  TournamentHallOfFameEntry,
  TournamentHistoryEntry,
  TournamentRewardGrant,
  TournamentSeasonLeaderboardEntry,
} from '../../../../shared/models/Dtos/colosseum/tournamentGrounds';

@Injectable({ providedIn: 'root' })
export class TournamentGroundsViewStateService {
  readonly status = signal<TournamentGroundsStatus | null>(null);
  readonly details = signal<TournamentDetails | null>(null);
  readonly bracket = signal<TournamentBracket | null>(null);
  readonly rewards = signal<TournamentRewardGrant[]>([]);
  readonly history = signal<TournamentHistoryEntry[]>([]);
  readonly hallOfFame = signal<TournamentHallOfFameEntry[]>([]);
  readonly seasonLeaderboard = signal<TournamentSeasonLeaderboardEntry[]>([]);
  readonly selectedRoundNumber = signal<number | null>(null);

  hasSnapshot = false;
  serverClockOffsetMs = 0;

  markSnapshotLoaded(): void {
    this.hasSnapshot = true;
  }

  updateDetails(details: TournamentDetails): void {
    this.details.set(details);

    const status = this.status();
    if (status?.currentTournament?.id === details.summary.id) {
      this.status.set({ ...status, currentTournament: details.summary });
    }
  }
}
