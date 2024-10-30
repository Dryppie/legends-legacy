import { Injectable } from '@angular/core';
import {
  BehaviorSubject,
  EMPTY,
  Observable,
  ReplaySubject,
  Subscription,
  catchError,
  expand,
  interval,
  mergeMap,
  of,
  tap,
  throwError,
  timer,
} from 'rxjs';
import { ApiService } from '../api/api.service'; // Import the shared API service
import { CharacterActionDto } from '../../../shared/models/Dtos/characterActionDto'; // Import CharacterActionDto model
import { environment } from '../../../../environments/environment';
import { CombatService } from '../combat/combat.service';
import { CombatResultDto } from '../../../shared/models/Dtos/combatResultDto';

@Injectable({
  providedIn: 'root',
})
export class CharacterActionsService {
  private currentActionSubject = new BehaviorSubject<CharacterActionDto | null>(
    null,
  );
  public currentAction$ = this.currentActionSubject.asObservable();

  private pollingSubscription: Subscription | null = null;

  constructor(
    private apiService: ApiService,
    private combatService: CombatService,
  ) {
    this.init();
  }

  private init(): void {
    // Initial fetch on service initialization
    this.getCharacterAction();
  }

  getCharacterAction(): void {
    this.apiService
      .get('CharacterActions')
      .pipe(
        catchError((error) => {
          console.error('Failed to fetch character action:', error);
          return of(null); // Continue with null to avoid breaking the stream
        }),
      )
      .subscribe((action: CharacterActionDto | null) => {
        this.setCurrentAction(action);
        if (!action) this.stopPolling();

        this.startPolling();

        if (action?.characterActionType === 0) {
          this.combatService.startCombatSimulation(
            action.combatResult as CombatResultDto,
          );
        }
      });
  }

  startCharacterAction(): void {
    const characterAction = {
      characterActionType: 1,
      lootTableId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    };
    this.apiService
      .post('CharacterActions', characterAction)
      .pipe(
        catchError((error) => {
          console.error('Failed to start character action:', error);
          return of(null);
        }),
      )
      .subscribe(() => {
        this.getCharacterAction();
      });
  }

  stopCharacterAction(): void {
    this.apiService
      .delete('CharacterActions')
      .pipe(
        catchError((error) => {
          console.error('Failed to delete character action:', error);
          return of(null);
        }),
      )
      .subscribe(() => {
        this.clearCurrentAction();
        this.stopPolling();
      });
  }

  private setCurrentAction(action: CharacterActionDto | null): void {
    this.currentActionSubject.next(action);
  }

  private startPolling(): void {
    if (this.pollingSubscription && !this.pollingSubscription.closed) {
      // Polling is already active
      return;
    }

    this.pollingSubscription = this.apiService
      .get('CharacterActions')
      .pipe(
        expand((action: CharacterActionDto | null) => {
          if (!action) {
            this.stopPolling();
            return EMPTY;
          }

          this.setCurrentAction(action);
          // if CombatAction
          if (action.characterActionType === 0) {
            // Calculate the next interval based on updatedAt - now
            const updatedAt = new Date(action.updatedAt).getTime();
            const now = Date.now();
            let nextInterval = updatedAt - now;

            if (nextInterval <= 0) {
              nextInterval = 0; // Call immediately if the time has already passed
            }

            return timer(nextInterval).pipe(
              mergeMap(() => this.apiService.get('CharacterActions')),
              catchError((error) => {
                console.error('Polling error:', error);
                this.stopPolling();
                return EMPTY;
              }),
            );
          }

          // Compute the next interval based on updatedAt
          const updatedAt = new Date(action.updatedAt).getTime();
          const now = Date.now();
          const timeSinceUpdate = now - updatedAt;
          let nextInterval = environment.baseDuration * 1000 - timeSinceUpdate;

          if (nextInterval <= 0) {
            nextInterval = 0; // Call immediately if the time has already passed
          }

          return timer(nextInterval).pipe(
            mergeMap(() => this.apiService.get('CharacterActions')),
            catchError((error) => {
              console.error('Polling error:', error);
              this.stopPolling();
              return EMPTY;
            }),
          );
        }),
        catchError((error) => {
          console.error('Polling error:', error);
          this.stopPolling();
          return EMPTY;
        }),
      )
      .subscribe((action: CharacterActionDto | null) => {
        if (!action) this.stopPolling();

        this.setCurrentAction(action);

        if (action?.combatResult) {
          this.combatService.startCombatSimulation(action.combatResult);
        }
      });
  }

  private stopPolling(): void {
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
      this.pollingSubscription = null;
    }
  }

  clearCurrentAction(): void {
    this.setCurrentAction(null);
    this.stopPolling();
  }
}
