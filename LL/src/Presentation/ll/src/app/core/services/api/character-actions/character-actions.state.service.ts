import { Injectable, signal, computed, effect } from '@angular/core';
import {
  CharacterActionDto,
  StartCombatActionRequest,
  StartCraftingActionRequest,
} from '../../../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../../../shared/models/enums/characterActionType';
import { CombatActionHandler } from './handlers/combat-action-handler';
import { CraftingActionHandler } from './handlers/crafting-action-handler';
import { CharacterActionsPollingService } from './helpers/characterActionsPollingService';
import {
  catchError,
  finalize,
  Observable,
  of,
  tap,
} from 'rxjs';
import { CharacterActionsService } from './character-actions.service';
import { CharacterActionTypePersistenceService } from './helpers/character-action-type-persistence.service';
import { GameService } from '../../client-side/game/game.service';
import { CombatService } from '../../client-side/combat/combat.service';
import { InventoryStateService } from '../inventory/inventory-state.service';

@Injectable({ providedIn: 'root' })
export class CharacterActionsStateService {
  private readonly _showAction = signal(false);
  readonly showAction = computed(() => this._showAction());

  private readonly _currentAction = signal<CharacterActionDto | null>(null);
  readonly currentAction = this._currentAction;

  private readonly _loadingCombat = signal(false);
  readonly loadingCombat = computed(() => this._loadingCombat());

  private readonly _loadingActionRefresh = signal(false);
  readonly loadingActionRefresh = computed(() => this._loadingActionRefresh());
  private activeActionRefreshes = 0;
  private actionRefreshLoadingTimeout: ReturnType<typeof setTimeout> | null =
    null;

  private readonly _startTime = signal<number | null>(null);
  private readonly _tickingDuration = signal<number>(0);
  readonly tickingDuration = computed(() => {
    const ms = this._tickingDuration();
    const sec = Math.floor(ms / 1000) % 60;
    const min = Math.floor(ms / 60000) % 60;
    const hr = Math.floor(ms / 3600000);
    return `${hr}h ${min}m ${sec}s`;
  });

  readonly isCombatAction = computed(
    () =>
      this._currentAction()?.characterActionType === CharacterActionType.Combat,
  );

  readonly isCraftingAction = computed(
    () =>
      this._currentAction()?.characterActionType ===
      CharacterActionType.Crafting,
  );

  readonly isActiveAction = computed(
    () =>
      !!this._currentAction() &&
      !this._currentAction()!.isDeleted &&
      this._currentAction()!.characterActionType !== CharacterActionType.Idle,
  );

  constructor(
    private readonly actionsService: CharacterActionsService,
    private readonly polling: CharacterActionsPollingService,
    private readonly persistence: CharacterActionTypePersistenceService,
    private readonly combatHandler: CombatActionHandler,
    private readonly craftingHandler: CraftingActionHandler,
    private readonly gameService: GameService,
    private readonly combatService: CombatService,
    private readonly inventoryState: InventoryStateService,
  ) {
    // When action changes, route to handler + update display
    effect(() => {
      const action = this._currentAction();
      queueMicrotask(() => this.updateDisplay(action));

      if (!action) return;

      switch (action.characterActionType) {
        case CharacterActionType.Combat:
          queueMicrotask(() => {
            this._loadingCombat.set(false);
            this.combatHandler.handle(action);
          });
          break;
        case CharacterActionType.Crafting:
          queueMicrotask(() => this.craftingHandler.handle(action));
          break;
      }
    });

    effect((onCleanup) => {
      const start = this._startTime();
      if (start === null) return;

      const intervalId = setInterval(() => {
        this._tickingDuration.set(Date.now() - start);
      }, 1000);

      onCleanup(() => clearInterval(intervalId));
    });
  }

  init(): void {
    this.startPolling();
  }

  initializeFromBootstrap(action: CharacterActionDto | null): void {
    this.startPolling(action);
  }

  private startPolling(initialAction?: CharacterActionDto | null): void {
    this._startTime.set(Date.now());
    this.polling.start(
      () =>
        this.trackActionRefresh(
          this.actionsService.getCurrentAction().pipe(
            catchError((err) => {
              console.error('[Polling] Failed to fetch current action', err);
              return of(null);
            }),
          ),
        ),
      (action) => {
        this.applyActionUpdate(action);
      },
      initialAction,
    );
  }

  startAction(
    type: CharacterActionType,
    payload: StartCombatActionRequest | StartCraftingActionRequest,
  ): void {
    this.persistence.set(type);
    let call$: Observable<boolean>;

    let isCombat = false;
    switch (type) {
      case CharacterActionType.Combat:
        this._loadingCombat.set(true);
        call$ = this.actionsService.startCombat(
          payload as StartCombatActionRequest,
        );
        isCombat = true;
        break;
      case CharacterActionType.Crafting:
        call$ = this.actionsService.startCrafting(
          payload as StartCraftingActionRequest,
        );
        break;
      default:
        console.warn('Unknown action type', type);
        return;
    }

    call$
      .pipe(
        tap((success) => {
          if (!success) {
            this.reset();
          } else {
            this.startPolling();
            if (isCombat) {
              this.gameService.startCombat();
            }
          }
        }),
        catchError((err) => {
          console.error('Failed to start action', err);
          this.reset();
          return of(false);
        }),
      )
      .subscribe();
  }

  stopAction(): void {
    const wasCraftingAction = this.isCraftingAction();
    this.handleDeletionOfCurrentAction();

    this.actionsService
      .stop()
      .pipe(
        tap(() => {
          this.clear();
          if (wasCraftingAction) {
            this.inventoryState.load(true);
          }
        }),
        catchError((err) => {
          console.error('Failed to stop action', err);
          return of(null);
        }),
      )
      .subscribe();
  }

  handleDeletionOfCurrentAction() {
    const currentAction = this._currentAction();
    if (!currentAction) return;

    const updated = {
      ...currentAction,
      isDeleted: true,
      craftingActionDetails: undefined,
      combatActionDetails: undefined,
    };
    this.updateDisplay(updated);
    this._currentAction.set(updated);
  }

  displayCurrentAction(): boolean {
    return this.showAction();
  }

  clear(): void {
    this.polling.stop();
    this.persistence.clear();

    this._tickingDuration.set(0);
    this._startTime.set(null);

    const action = this._currentAction();
    if (!action) return;

    this._currentAction.set({ ...action, isDeleted: true });
  }

  reset(): void {
    this.clear();
    this._currentAction.set(null);
  }

  updateDisplay(action: CharacterActionDto | null): void {
    if (!action) {
      this._showAction.set(false);
      return;
    }

    if (!action.isDeleted) {
      this._showAction.set(true);
      return;
    }

    const updatedAt = new Date(action.updatedAt).getTime();
    const now = Date.now();

    if (updatedAt < now) {
      this._showAction.set(false);
    } else {
      this._showAction.set(true);
      setTimeout(() => {
        this._showAction.set(false);
        this.combatService.clearAllCombat();
        this.gameService.hideCombat();
        this._currentAction.set(null);
        this.hide();
      }, updatedAt - now);
    }
  }

  hide(): void {
    this._showAction.set(false);
  }

  applyRealtimeIdleCombat(action: CharacterActionDto): void {
    this.applyActionUpdate(action);
  }

  refreshCurrentAction(): void {
    this.trackActionRefresh(
      this.actionsService.getCurrentAction().pipe(
        catchError((err) => {
          console.error('[Manual Refresh] Failed to fetch current action', err);
          return of(null);
        }),
      ),
    ).subscribe((action) => this.applyActionUpdate(action));
  }

  private applyActionUpdate(action: CharacterActionDto | null): void {
    const currentKey = this.getActionUpdateKey(this._currentAction());
    const nextKey = this.getActionUpdateKey(action);

    if (currentKey && nextKey && currentKey === nextKey) {
      return;
    }

    this._currentAction.set(action);
    if (action) {
      this.persistence.set(action.characterActionType);
    } else {
      this.persistence.clear();
    }
  }

  private getActionUpdateKey(action: CharacterActionDto | null): string | null {
    if (!action) return null;

    const combat = action.combatSession?.combatResult;
    if (combat) {
      return [
        action.characterActionType,
        action.updatedAt,
        action.isDeleted,
        combat.startedAt,
        combat.outcome,
        combat.duration,
      ].join('|');
    }

    return [
      action.characterActionType,
      action.updatedAt,
      action.isDeleted,
      action.combatSession ? 'combat-session' : 'no-combat-session',
      action.craftingActionDetails?.craftingQueueItems?.length ?? 0,
    ].join('|');
  }

  private trackActionRefresh(
    request$: Observable<CharacterActionDto | null>,
  ): Observable<CharacterActionDto | null> {
    this.activeActionRefreshes += 1;

    if (!this.actionRefreshLoadingTimeout) {
      this.actionRefreshLoadingTimeout = setTimeout(() => {
        if (this.activeActionRefreshes > 0 && this.shouldShowActionRefreshLoading()) {
          this._loadingActionRefresh.set(true);
        }
      }, 250);
    }

    return request$.pipe(
      finalize(() => {
        this.activeActionRefreshes = Math.max(0, this.activeActionRefreshes - 1);

        if (this.activeActionRefreshes > 0) {
          return;
        }

        if (this.actionRefreshLoadingTimeout) {
          clearTimeout(this.actionRefreshLoadingTimeout);
          this.actionRefreshLoadingTimeout = null;
        }

        this._loadingActionRefresh.set(false);
      }),
    );
  }

  private shouldShowActionRefreshLoading(): boolean {
    const action = this._currentAction();

    if (!action) {
      return true;
    }

    if (action.characterActionType !== CharacterActionType.Combat) {
      return true;
    }

    return !action.combatSession?.combatResult;
  }
}
