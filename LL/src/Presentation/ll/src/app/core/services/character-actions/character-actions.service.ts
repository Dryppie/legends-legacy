import { Injectable } from '@angular/core';
import {
  BehaviorSubject,
  EMPTY,
  Subscription,
  catchError,
  expand,
  mergeMap,
  of,
  timer,
} from 'rxjs';
import { ApiService } from '../api/api.service';
import {
  CharacterActionDto,
  StartCombatActionRequest,
  StartGatheringActionRequest,
} from '../../../shared/models/Dtos/characterActionDto';
import { environment } from '../../../../environments/environment';
import { CombatService } from '../combat/combat.service';
import { NamedStorageKeys } from '../../common/enums/named-storage-keys';
import { CharacterActionType } from '../../../shared/models/enums/characterActionType';
import { GameService } from '../game/game.service';
import { EventBusService } from '../event-bus/event-bus.service';

@Injectable({
  providedIn: 'root',
})
export class CharacterActionsService {
  private isInitialized = false;
  private currentActionSubject = new BehaviorSubject<CharacterActionDto | null>(
    null,
  );
  public currentAction$ = this.currentActionSubject.asObservable();

  private loadingStartCombatSubject = new BehaviorSubject<boolean>(false);
  public loadingStartCombat$ = this.loadingStartCombatSubject.asObservable();

  private pollingSubscription: Subscription | null = null;

  constructor(
    private apiService: ApiService,
    private combatService: CombatService,
    private gameService: GameService,
    private eventBusService: EventBusService,
  ) {
    this.eventBusService.logout$.subscribe(() => {
      this.handleLogout();
    });
    this.eventBusService.currentActionSubject$.subscribe(() => {
      this.getCharacterAction();
    });
  }

  init(): void {
    if (this.isInitialized) {
      return; // Initialization already done
    }
    this.isInitialized = true;
    // Initial fetch on app initialization
    this.getCharacterAction();
  }

  getCharacterAction(): void {
    this.stopPolling();
    this.startPolling();
  }

  startCombatAction(startCombatActionRequest: StartCombatActionRequest): void {
    this.setCAT(CharacterActionType.Combat);
    this.loadingStartCombatSubject.next(true);

    this.apiService
      .post('CharacterActions/StartCombat', startCombatActionRequest)
      .pipe(
        catchError((error) => {
          console.error('Failed to start character action:', error);
          this.clearCAT();
          return of(null);
        }),
      )
      .subscribe((success) => {
        if (success) {
          this.getCharacterAction();
          this.gameService.startCombat();
        }
      });
  }

  startGatheringAction(gatheringAction: StartGatheringActionRequest): void {
    this.setCAT(CharacterActionType.Gathering);
    this.apiService
      .post('CharacterActions/StartGathering', gatheringAction)
      .pipe(
        catchError((error) => {
          this.clearCAT();
          console.error('Failed to start character action:', error);
          return of(null);
        }),
      )
      .subscribe((success) => {
        if (success) this.getCharacterAction();
      });
  }

  stopCharacterAction(): void {
    this.clearCurrentAction();
    this.apiService
      .delete('CharacterActions')
      .pipe(
        catchError((error) => {
          console.error('Failed to delete character action:', error);
          return of(null);
        }),
      )
      .subscribe(() => {});
  }

  private setCurrentAction(action: CharacterActionDto | null): void {
    this.currentActionSubject.next(action);
    if (action) this.setCAT(action.characterActionType);
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
          if (!action || action.isDeleted) {
            this.stopPolling();
            return EMPTY;
          }

          this.setCurrentAction(action);
          // if CombatAction
          if (action.characterActionType === CharacterActionType.Combat) {
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

        if (action?.characterActionType === CharacterActionType.Combat) {
          this.combatService.startCombatSimulation(action);
        }
      });
  }

  stopPolling(): void {
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
      this.pollingSubscription = null;
    }
  }

  clearCurrentAction(): void {
    this.clearCAT();
    this.stopPolling();
    this.setCurrentAction(null);
  }

  setCAT(characterActionType: CharacterActionType): void {
    localStorage.setItem(
      NamedStorageKeys.CharacterActionType,
      characterActionType,
    );
  }

  getCAT(): string | null {
    return localStorage.getItem(NamedStorageKeys.CharacterActionType);
  }

  clearCAT(): void {
    localStorage.removeItem(NamedStorageKeys.CharacterActionType);
  }

  private handleLogout(): void {
    this.clearCurrentAction();

    this.isInitialized = false;
    // Then call init() again once a new user logs in (if that’s your intended flow).
  }
}
