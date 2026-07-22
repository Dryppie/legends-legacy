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
import { EventBusService } from '../../client-side/event-bus/event-bus.service';

export type IdleCombatPhase =
  | 'idle'
  | 'starting'
  | 'active'
  | 'resolving'
  | 'stopping'
  | 'error';

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
  private readonly _idleCombatPhase = signal<IdleCombatPhase>('idle');
  readonly idleCombatPhase = computed(() => this._idleCombatPhase());
  private activeActionRefreshes = 0;
  private actionRefreshLoadingTimeout: ReturnType<typeof setTimeout> | null =
    null;

  private readonly _startTime = signal<number | null>(null);
  private readonly _tickingDuration = signal<number>(0);
  private resetVersion = 0;
  private openCombatWhenHydrated = false;
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
    private readonly eventBus: EventBusService,
  ) {
    // When action changes, route to handler + update display
    effect(() => {
      const action = this._currentAction();
      queueMicrotask(() => this.updateDisplay(action));

      if (!action) return;

      switch (action.characterActionType) {
        case CharacterActionType.Combat:
          queueMicrotask(() => {
            if (!action.combatSession?.combatResult) return;
            this._loadingCombat.set(false);
            this.combatHandler.handle(action);
            this._idleCombatPhase.set('active');
            this.gameService.resumeCombat();
            if (this.openCombatWhenHydrated) {
              this.openCombatWhenHydrated = false;
              this.gameService.showCombat();
            }
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

    effect(
      () => {
        if (this.eventBus.logout()) {
          this.reset();
        }
      },
      { allowSignalWrites: true },
    );
  }

  init(): void {
    this.startPolling();
  }

  initializeFromBootstrap(action: CharacterActionDto | null): void {
    this.openCombatWhenHydrated = false;
    this.startPolling(action);
  }

  private startPolling(initialAction?: CharacterActionDto | null): void {
    this._startTime.set(Date.now());
    const requestVersion = this.resetVersion;

    this.polling.start(
      () =>
        this.trackActionRefresh(
          this.resolveCurrentActionRequest().pipe(
            catchError((err) => {
              console.error('[Polling] Failed to fetch current action', err);
              this._idleCombatPhase.set('error');
              return of(this._currentAction());
            }),
          ),
        ),
      (action) => {
        if (requestVersion !== this.resetVersion) return;
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
    let call$: Observable<boolean | CharacterActionDto>;

    let isCombat = false;
    switch (type) {
      case CharacterActionType.Combat:
        this._loadingCombat.set(true);
        this._idleCombatPhase.set('starting');
        this.openCombatWhenHydrated = true;
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
        tap((result) => {
          if (!result) {
            this.reset();
          } else {
            if (isCombat) {
              this.applyActionUpdate(result as CharacterActionDto);
            }
            this.startPolling(
              isCombat ? (result as CharacterActionDto) : undefined,
            );
          }
        }),
        catchError((err) => {
          console.error('Failed to start action', err);
          if (isCombat) this._idleCombatPhase.set('error');
          this.reset();
          return of(false);
        }),
      )
      .subscribe();
  }

  stopAction(): void {
    const wasCraftingAction = this.isCraftingAction();
    if (this.isCombatAction()) this._idleCombatPhase.set('stopping');
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
    this.resetVersion += 1;
    this.clear();
    this.activeActionRefreshes = 0;
    if (this.actionRefreshLoadingTimeout) {
      clearTimeout(this.actionRefreshLoadingTimeout);
      this.actionRefreshLoadingTimeout = null;
    }
    this._loadingActionRefresh.set(false);
    this._loadingCombat.set(false);
    this._idleCombatPhase.set('idle');
    this.openCombatWhenHydrated = false;
    this._showAction.set(false);
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

  refreshCurrentAction(): void {
    const requestVersion = this.resetVersion;

    this.trackActionRefresh(
      this.resolveCurrentActionRequest().pipe(
        catchError((err) => {
          console.error('[Manual Refresh] Failed to fetch current action', err);
          this._idleCombatPhase.set('error');
          return of(this._currentAction());
        }),
      ),
    ).subscribe((action) => {
      if (requestVersion !== this.resetVersion) return;
      this.applyActionUpdate(action);
    });
  }

  private applyActionUpdate(action: CharacterActionDto | null): void {
    const current = this._currentAction();
    const currentKey = this.getActionUpdateKey(current);
    const nextKey = this.getActionUpdateKey(action);

    if (currentKey && nextKey && currentKey === nextKey) {
      return;
    }

    if (current && action && this.isOlderUpdate(current, action)) return;

    this._currentAction.set(action);
    if (action) {
      this.persistence.set(action.characterActionType);
    } else {
      this.persistence.clear();
    }
  }

  private isOlderUpdate(
    current: CharacterActionDto,
    candidate: CharacterActionDto,
  ): boolean {
    if (current.characterActionType !== candidate.characterActionType) {
      return false;
    }

    const currentBoundary = new Date(
      current.nextResolutionAt ?? current.updatedAt,
    ).getTime();
    const candidateBoundary = new Date(
      candidate.nextResolutionAt ?? candidate.updatedAt,
    ).getTime();

    if (candidateBoundary < currentBoundary) return true;

    // A concurrent/early resolver can legitimately report that no new encounter
    // was due. Never let that less-hydrated response replace the encounter that
    // is already on screen at the same boundary.
    return (
      candidateBoundary === currentBoundary &&
      !!current.combatSession?.combatResult &&
      !candidate.combatSession?.combatResult
    );
  }

  private getActionUpdateKey(action: CharacterActionDto | null): string | null {
    if (!action) return null;

    if (action.revision) {
      return `${action.characterActionType}|${action.revision}|${action.isDeleted}`;
    }

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

  private resolveCurrentActionRequest(): Observable<CharacterActionDto | null> {
    if (
      this._currentAction()?.characterActionType === CharacterActionType.Combat
    ) {
      this._idleCombatPhase.set('resolving');
    }

    return this.actionsService.resolveCurrentAction().pipe(
      tap((action) => {
        if (action?.combatSession?.combatResult) {
          this._idleCombatPhase.set('active');
        }
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
