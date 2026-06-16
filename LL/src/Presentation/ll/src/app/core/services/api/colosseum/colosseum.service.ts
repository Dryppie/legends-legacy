import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { catchError, Observable, throwError } from 'rxjs';
import { CombatResultDto } from '../../../../shared/models/Dtos/combatResultDto';
import { CombatService } from '../../client-side/combat/combat.service';
import { ColosseumMatchResult } from '../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { ArenaTicketStatus } from '../../../../shared/models/Dtos/colosseum/arenaTicketStatus';
import { ArenaOpponentPreview } from '../../../../shared/models/Dtos/colosseum/arenaOpponentPreview';
import { LeaderboardEntry } from '../../../../shared/models/Dtos/leaderboard/leaderboardEntry';

export interface StartArenaBattleResponse {
  battle: CombatResultDto;
  arenaTicketStatus: ArenaTicketStatus;
}

@Injectable({
  providedIn: 'root',
})
export class ColosseumService {
  constructor(
    private apiService: ApiService,
    private combatService: CombatService,
  ) {}

  public getArenaOpponents(): Observable<ArenaOpponentPreview[]> {
    return this.apiService.get('colosseum/getArenaOpponents').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get arena opponents'));
      }),
    );
  }

  getArenaTicketStatus(): Observable<ArenaTicketStatus> {
    return this.apiService.get('colosseum/getArenaTicketStatus').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get arena tickets'));
      }),
    );
  }

  getColosseumRankings(): Observable<LeaderboardEntry[]> {
    return this.apiService.get('colosseum/getRankings').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get arena rankings'));
      }),
    );
  }

  getColosseumMatchResults(): Observable<ColosseumMatchResult[]> {
    return this.apiService.get('colosseum/getColosseumMatchResults').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get arena match results'));
      }),
    );
  }

  public startArenaBattle(
    enemyId: string,
  ): Observable<StartArenaBattleResponse> {
    return this.apiService.post('colosseum/startArenaBattle', enemyId).pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to start match'));
      }),
    );
  }

  skipColosseumMatch() {
    this.combatService.skipCurrentColosseum();
  }
}
