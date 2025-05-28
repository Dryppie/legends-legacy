import { Injectable } from '@angular/core';
import {
  BehaviorSubject,
  EMPTY,
  Observable,
  Subscription,
  catchError,
  expand,
  map,
  mergeMap,
  of,
  retry,
  take,
  tap,
  timer,
} from 'rxjs';
import { ApiService } from '../../api/api.service';
import {
  CharacterActionDto,
  StartCombatActionRequest,
  StartCraftingActionRequest,
} from '../../../../shared/models/Dtos/characterActionDto';
import { environment } from '../../../../../environments/environment';
import { NamedStorageKeys } from '../../../common/enums/named-storage-keys';
import { CharacterActionType } from '../../../../shared/models/enums/characterActionType';
import { GameService } from '../../client-side/game/game.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { CombatService } from '../../client-side/combat/combat.service';
import { SessionSummaryService } from '../../client-side/session-summary/session-summary.service';
import { CraftingService } from '../crafting/crafting.service';
import { LevelingService } from '../../client-side/leveling/leveling.service';
import { ProfessionType } from '../../../../shared/models/Dtos/characterProfession';

@Injectable({
  providedIn: 'root',
})
export class CharacterActionsService {
  private isInitialized = false;
  private currentActionSubject = new BehaviorSubject<CharacterActionDto | null>(
    null,
  );
  private displayCurrentActionSubject = new BehaviorSubject<boolean>(false);

  public currentAction$ = this.currentActionSubject.asObservable();
  public displayCurrentAction$ =
    this.displayCurrentActionSubject.asObservable();

  private loadingCombatActionSubject = new BehaviorSubject<boolean>(false);
  public loadingCombatAction$ = this.loadingCombatActionSubject.asObservable();

  private pollingSubscription: Subscription | null = null;

  constructor(
    private apiService: ApiService,
    private combatService: CombatService,
    private gameService: GameService,
    private eventBusService: EventBusService,
    private sessionSummaryService: SessionSummaryService,
    private craftingService: CraftingService,
    private levelingService: LevelingService,
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
    this.loadingCombatActionSubject.next(true);

    this.gameService.startCombat();
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
        }
      });
  }

  startGatheringAction(gatheringNodeId: string): void {
    this.setCAT(CharacterActionType.Gathering);

    this.apiService
      .post('CharacterActions/StartGathering', gatheringNodeId)
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

  startCraftingAction(
    craftingAction: StartCraftingActionRequest,
  ): Observable<boolean> {
    this.setCAT(CharacterActionType.Crafting);

    return this.apiService
      .post('CharacterActions/StartCrafting', craftingAction)
      .pipe(
        tap((success) => {
          if (success) {
            this.getCharacterAction();
          }
        }),
        map((success) => success),
        catchError((error) => {
          this.clearCAT();
          return of(false);
        }),
      );
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

  // Helper to wrap API GET call with loading logic.
  private fetchCharacterAction() {
    return this.apiService.get('CharacterActions').pipe(
      retry({
        count: 3, // Retry up to 3 times (total attempts = 4)
        delay: (error, retryCount) => {
          // Exponential backoff: 0.5s, 1s, 2s...
          const backoffTime = Math.pow(2, retryCount) * 500;
          console.warn(
            `Retry attempt #${retryCount}. Backing off for ${backoffTime}ms.`,
          );
          return timer(backoffTime);
        },

        resetOnSuccess: true, // If true, a successful request resets the retry counter
      }),
      catchError((error) => {
        console.error('Error fetching character action:', error);
        this.stopPolling();
        // Returning EMPTY will terminate the polling observable
        return EMPTY;
      }),
    );
  }

  private startPolling(): void {
    if (this.pollingSubscription && !this.pollingSubscription.closed) {
      // Polling is already active
      return;
    }
    this.pollingSubscription = this.fetchCharacterAction()
      .pipe(
        expand((action: CharacterActionDto | null) => {
          if (
            !action ||
            action.isDeleted ||
            action.characterActionType === CharacterActionType.Idle
          ) {
            this.stopPolling();
            return EMPTY;
          }

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
              mergeMap(() => this.fetchCharacterAction()),
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
            mergeMap(() => this.fetchCharacterAction()),
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
        if (!action || action.isDeleted) this.stopPolling();
        this.setCurrentAction(action);

        this.handleCombatAction(action);
        this.handleCraftingAction(action);
        this.handleGatheringAction(action);

        this.setDisplayCurrentAction(action);
      });
  }

  private handleCraftingAction(action: CharacterActionDto | null) {
    if (
      action?.characterActionType === CharacterActionType.Crafting &&
      action.temperingSession
    ) {
      this.craftingService.setQueue(
        action.craftingActionDetails?.craftingQueueItems ?? [],
      );
      this.sessionSummaryService.loadCraftingSince(action.temperingSession);
      const summary = action.temperingSession.temperingSummary;
      if (summary.armorForgingExperience > 0)
        this.levelingService.gainProfessionExperience(
          ProfessionType.ArmorForging,
          summary.armorForgingExperience,
        );
      if (summary.jewelryCraftingExperience > 0)
        this.levelingService.gainProfessionExperience(
          ProfessionType.JewelryCrafting,
          summary.jewelryCraftingExperience,
        );
      if (summary.weaponSmithingExperience > 0)
        this.levelingService.gainProfessionExperience(
          ProfessionType.WeaponSmithing,
          summary.weaponSmithingExperience,
        );
      if (action.craftingActionDetails?.craftingQueueItems.length === 0) {
        this.clearCurrentAction();
      }
    }
  }

  private handleCombatAction(action: CharacterActionDto | null) {
    if (action?.characterActionType === CharacterActionType.Combat) {
      this.loadingCombatActionSubject.next(false);
      this.combatService.startCombatSimulation(action);
      this.sessionSummaryService.loadCombatSince(action.combatSession);
    }
  }

  private handleGatheringAction(action: CharacterActionDto | null) {
    if (
      action?.characterActionType === CharacterActionType.Gathering &&
      action?.gatheringSession
    ) {
      const summary = action.gatheringSession.gatheringSummary;
      this.sessionSummaryService.loadGatheringSince(action.gatheringSession);
      this.levelingService.gainProfessionExperience(
        summary.professionType,
        summary.totalExperience,
      );
    }
  }

  stopPolling(): void {
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
      this.pollingSubscription = null;
    }
    this.craftingService.setQueue([]);
  }

  setDisplayCurrentAction(action: CharacterActionDto | null) {
    if (!action) {
      this.displayCurrentActionSubject.next(false);
      return;
    }

    if (!action.isDeleted) {
      this.displayCurrentActionSubject.next(true);
      return;
    }

    if (new Date(action.updatedAt).getTime() < Date.now()) {
      this.displayCurrentActionSubject.next(false);
    } else {
      // If updatedAt is after now(), we know it's a combat action, and therefore, we hide current action after a dynamic delay
      this.displayCurrentActionSubject.next(true);
      const updatedAt = new Date(action.updatedAt).getTime();
      const now = Date.now();
      const delay = Math.max(updatedAt - now, 0);

      setTimeout(() => {
        this.displayCurrentActionSubject.next(false);
        this.combatService.clearAllCombat();
        this.gameService.hideCombat();
      }, delay);
    }
  }

  clearCurrentAction(): void {
    this.clearCAT();
    this.stopPolling();
    this.currentAction$.pipe(take(1)).subscribe((action) => {
      if (!action) return;
      action.isDeleted = true;
      this.setDisplayCurrentAction(action);
      this.setCurrentAction(action);
    });
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
    this.setCurrentAction(null);

    this.isInitialized = false;
  }
}
