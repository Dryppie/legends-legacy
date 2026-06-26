import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { catchError, Observable, throwError } from 'rxjs';
import { CombatService } from '../../client-side/combat/combat.service';
import { ColosseumMatchResult } from '../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { ArenaTicketStatus } from '../../../../shared/models/Dtos/colosseum/arenaTicketStatus';
import { ArenaOpponentPreview } from '../../../../shared/models/Dtos/colosseum/arenaOpponentPreview';
import { LeaderboardEntry } from '../../../../shared/models/Dtos/leaderboard/leaderboardEntry';
import {
  ArenaDefenseStatus,
  ColosseumStatus,
} from '../../../../shared/models/Dtos/colosseum/colosseumStatus';
import { StartArenaBattleResponse } from '../../../../shared/models/Dtos/colosseum/startArenaBattleResponse';
import {
  ChampionMarket,
  ChampionMarketPurchaseResponse,
} from '../../../../shared/models/Dtos/colosseum/championMarket';

@Injectable({
  providedIn: 'root',
})
export class ColosseumService {
  constructor(
    private apiService: ApiService,
    private combatService: CombatService,
  ) {}

  public getArenaOpponents(): Observable<ArenaOpponentPreview[]> {
    return this.apiService.get('colosseum/opponents').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get arena opponents'));
      }),
    );
  }

  getStatus(): Observable<ColosseumStatus> {
    return this.apiService.get('colosseum/status').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get colosseum status'));
      }),
    );
  }

  updateDefenseSnapshot(): Observable<{ data?: ArenaDefenseStatus }> {
    return this.apiService.post('colosseum/defense-snapshot').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to update arena defense'));
      }),
    );
  }

  getChampionMarket(): Observable<ChampionMarket> {
    return this.apiService.get('colosseum/market').pipe(
      catchError(() => {
        return throwError(() => new Error("Failed to get champion's market"));
      }),
    );
  }

  purchaseChampionMarketItem(
    itemId: string,
    quantity = 1,
  ): Observable<ChampionMarketPurchaseResponse> {
    return this.apiService
      .post('colosseum/market/purchase', { itemId, quantity })
      .pipe(
        catchError((err) => {
          return throwError(
            () =>
              new Error(
                err.message ?? "Failed to purchase champion's market item",
              ),
          );
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
    opponentId: string,
  ): Observable<StartArenaBattleResponse> {
    return this.apiService.post('colosseum/battle', { opponentId }).pipe(
      catchError((err) => {
        return throwError(
          () => new Error(err.message ?? 'Failed to start match'),
        );
      }),
    );
  }

  skipColosseumMatch() {
    this.combatService.skipCurrentColosseum();
  }
}
