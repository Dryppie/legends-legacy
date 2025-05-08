import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import {
  BehaviorSubject,
  catchError,
  map,
  Observable,
  of,
  throwError,
} from 'rxjs';
import { CombatResultDto } from '../../../../shared/models/Dtos/combatResultDto';
import { CombatService } from '../../client-side/combat/combat.service';
import { ColosseumMatchResult } from '../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { ColosseumRank } from '../../../../shared/models/Dtos/colosseum/colosseumRank';
import { ArenaTicketStatus } from '../../../../shared/models/Dtos/colosseum/arenaTicketStatus';

@Injectable({
  providedIn: 'root',
})
export class ColosseumService {
  private arenaTicketStatusSubject =
    new BehaviorSubject<ArenaTicketStatus | null>(null);
  arenaTicketStatus$ = this.arenaTicketStatusSubject.asObservable();

  constructor(
    private apiService: ApiService,
    private combatService: CombatService,
  ) {}

  public getArenaOpponents(): Observable<CharacterDto[]> {
    return this.apiService.get('colosseum/getArenaOpponents').pipe(
      map((opponents) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
        return opponents;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error('Failed to get arena opponents'));
      }),
    );
  }

  getArenaTicketStatus() {
    this.apiService
      .get('colosseum/getArenaTicketStatus')
      .pipe(
        map((arenaTicketStatus) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
          return arenaTicketStatus;
        }),

        catchError(() => {
          // this.toastService.showToast(
          //   'Login Failed',
          //   'Wrong email or password',
          //   'error',
          //   't',
          // );
          return throwError(() => new Error('Failed to get arena opponents'));
        }),
      )
      .subscribe((status) => this.arenaTicketStatusSubject.next(status));
  }

  getColosseumRankings(): Observable<ColosseumRank[]> {
    return this.apiService.get('colosseum/getRankings').pipe(
      map((opponents) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
        return opponents;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error('Failed to get arena opponents'));
      }),
    );
  }

  getColosseumMatchResults(): Observable<ColosseumMatchResult[]> {
    return this.apiService.get('colosseum/getColosseumMatchResults').pipe(
      map((opponents) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
        return opponents;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error('Failed to get arena opponents'));
      }),
    );
  }

  public startArenaBattle(enemyId: string): void {
    this.apiService
      .post('colosseum/startArenaBattle', enemyId)
      .pipe(
        map((match) => {
          return match;
        }),
        catchError((error) => {
          return throwError(() => new Error('Failed to start match'));
        }),
      )
      .subscribe((result: CombatResultDto | null) => {
        if (result) {
          this.combatService.startColosseumMatchSimulation(result);
        }
      });
  }
}
