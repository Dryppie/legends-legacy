import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { catchError, map, Observable, of, throwError } from 'rxjs';
import { CombatResultDto } from '../../../../shared/models/Dtos/combatResultDto';
import { CombatService } from '../../client-side/combat/combat.service';

@Injectable({
  providedIn: 'root',
})
export class ColosseumService {
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
