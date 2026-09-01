import { Injectable, signal, computed, effect, untracked } from '@angular/core';
import {
  CharacterActionDto,
  StartCombatActionRequest,
  StartCraftingActionRequest,
} from '../../../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../../../shared/models/enums/characterActionType';
import { CombatActionHandler } from './handlers/combat-action-handler';
import { CraftingActionHandler } from './handlers/crafting-action-handler';
import { CharacterActionsPollingService } from './helpers/characterActionsPollingService';
import { catchError, finalize, Observable, of, tap, throwError } from 'rxjs';
import { CharacterActionsService } from './character-actions.service';
import { CharacterActionTypePersistenceService } from './helpers/character-action-type-persistence.service';
import { GameService } from '../../client-side/game/game.service';
import { CombatService } from '../../client-side/combat/combat.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { CraftingService } from '../crafting/crafting.service';
import { TemperingActionState } from '../../../../shared/models/Dtos/temperingQueueMutationDto';
import { CraftingQueueItem } from '../../../../shared/models/profession';

export type IdleCombatPhase =
  | 'idle'
  | 'starting'
  | 'active'
  | 'resolving'
  | 'stopping'
  | 'error';

const DEFAULT_IDLE_COMBAT_RESOLUTION_INTERVAL_MS = 10_000;

export function isOfflineCombatCatchUpRequest(
  action: CharacterActionDto | null,
  now: number,
): boolean {
  if (
    !action ||
    action.isDeleted ||
    action.characterActionType !== CharacterActionType.Combat
  ) {
    return false;
  }

  if (action.hasMoreDueWork) {
    return true;
  }

  const boundaryValue = action.nextResolutionAtUtc ?? null;
  if (!boundaryValue) return false;

  const boundary = new Date(boundaryValue).getTime();
  if (!Number.isFinite(boundary)) return false;

  const configuredInterval = action.resolutionIntervalMs;
  const interval =
    typeof configuredInterval === 'number' && configuredInterval > 0
      ? configuredInterval
      : DEFAULT_IDLE_COMBAT_RESOLUTION_INTERVAL_MS;

  // One overdue interval means at least two encounters are waiting. The
  // dashboard separately debounces this state, so routine fast polls remain
  // invisible even when they happen exactly on an encounter boundary.
  return boundary <= now - interval;
}

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
  private readonly _resumingTempering = signal(false);
  readonly resumingTempering = computed(() => this._resumingTempering());
  private readonly _resolvingOfflineProgress = signal(false);
  readonly resolvingOfflineProgress = computed(() =>
    this._resolvingOfflineProgress(),
  );
  private readonly _idleCombatPhase = signal<IdleCombatPhase>('idle');
  readonly idleCombatPhase = computed(() => this._idleCombatPhase());
  private readonly _idleCombatError = signal<string | null>(null);
  readonly idleCombatError = computed(() => this._idleCombatError());
  private activeActionRefreshes = 0;
  private actionRefreshLoadingTimeout: ReturnType<typeof setTimeout> | null =
    null;
  private activeOfflineCatchUpRequests = 0;
  private readonly _stoppingAction = signal(false);
  readonly stoppingAction = computed(() => this._stoppingAction());
  private pendingStartAfterStop: {
    type: CharacterActionType;
    payload: StartCombatActionRequest | StartCraftingActionRequest;
  } | null = null;

  private readonly _startTime = signal<number | null>(null);
  private readonly _tickingDuration = signal<number>(0);
  private resetVersion = 0;
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

  readonly isTemperingPendingCombatUnlock = computed(() => {
    // Re-evaluate time-based phase changes even when the persisted action
    // snapshot itself has not changed.
    this._tickingDuration();
    const action = this._currentAction();
    if (
      !action ||
      action.isDeleted ||
      action.characterActionType !== CharacterActionType.Crafting ||
      !action.blockedUntilUtc
    ) {
      return false;
    }

    return new Date(action.blockedUntilUtc).getTime() > Date.now();
  });

  readonly temperingCombatUnlockSeconds = computed(() => {
    this._tickingDuration();
    const action = this._currentAction();
    if (!action?.blockedUntilUtc) return 0;

    return Math.max(
      0,
      Math.ceil(
        (new Date(action.blockedUntilUtc).getTime() - Date.now()) / 1_000,
      ),
    );
  });

  readonly isActiveAction = computed(
    () =>
      !!this._currentAction() &&
      !this._currentAction()!.isDeleted &&
      this._currentAction()!.characterActionType !== CharacterActionType.Idle,
  );

  readonly isActionCooldown = computed(() => {
    const action = this._currentAction();
    if (!action?.isDeleted) return false;

    return this.switchUnlockDeadline(action) > Date.now();
  });

  constructor(
    private readonly actionsService: CharacterActionsService,
    private readonly polling: CharacterActionsPollingService,
    private readonly persistence: CharacterActionTypePersistenceService,
    private readonly combatHandler: CombatActionHandler,
    private readonly craftingHandler: CraftingActionHandler,
    private readonly gameService: GameService,
    private readonly combatService: CombatService,
    private readonly eventBus: EventBusService,
    private readonly router: Router,
    private readonly inventoryState: InventoryStateService,
    private readonly craftingService: CraftingService,
  ) {
    // When action changes, route to handler + update display
    effect(() => {
      const action = this._currentAction();
      queueMicrotask(() => this.updateDisplay(action));

      if (!action) return;
      queueMicrotask(() => this.craftingHandler.handle(action));

      switch (action.characterActionType) {
        case CharacterActionType.Combat:
          queueMicrotask(() => {
            if (action.isDeleted || !action.combatSession?.combatResult) return;
            this._loadingCombat.set(false);
            this.combatHandler.handle(action);
            const hasPendingResolution = action.hasMoreDueWork ?? false;
            this._idleCombatPhase.set(
              hasPendingResolution ? 'resolving' : 'active',
            );
            if (!hasPendingResolution) {
              this._resolvingOfflineProgress.set(false);
            }
            this.gameService.resumeCombat();
          });
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
          untracked(() => this.reset());
        }
      },
      { allowSignalWrites: true },
    );
  }

  init(): void {
    this.startPolling();
  }

  initializeFromBootstrap(action: CharacterActionDto | null): void {
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
              this.setIdleCombatError(err);
              return throwError(() => err);
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
    // Quitting and starting another action are separate HTTP requests. Preserve
    // the player's click immediately, but serialize the requests so a late
    // combat-delete cannot remove the newly created tempering queue.
    if (this._stoppingAction() && type === CharacterActionType.Crafting) {
      this.pendingStartAfterStop = { type, payload };
      this.persistence.set(type);
      return;
    }

    this.persistence.set(type);
    if (type === CharacterActionType.Crafting) {
      this.startCraftingAction(payload as StartCraftingActionRequest);
      return;
    }

    let call$: Observable<CharacterActionDto>;

    switch (type) {
      case CharacterActionType.Combat:
        this._loadingCombat.set(true);
        this._idleCombatPhase.set('starting');
        call$ = this.actionsService.startCombat(
          payload as StartCombatActionRequest,
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
            throw new Error(
              'Combat start completed without returning an action.',
            );
          } else {
            this.acceptStartedCombat(result);
          }
        }),
        catchError((err) => {
          console.error('Failed to start action', err);
          return this.recoverStartedCombat(err);
        }),
      )
      .subscribe();
  }

  private startCraftingAction(payload: StartCraftingActionRequest): void {
    this.actionsService
      .startCrafting(payload)
      .pipe(
        tap((result) => {
          const applied = this.inventoryState.applyVersionedInventoryDelta(
            result,
            (data) => {
              const removedIds = new Set(data.removedInventoryItemIds);
              this.inventoryState.setInventory(
                this.inventoryState
                  .items()
                  .filter(
                    (item) => !removedIds.has(item.itemInstance.id),
                  ),
              );
            },
          );
          if (!applied) this.refreshCurrentAction();

          this.combatService.clearAllCombat();
          this.applyTemperingQueueMutation(
            result.data.action ?? null,
            this.craftingService.currentQueue,
          );
          this.startPolling(this._currentAction());
        }),
        catchError((error) => {
          console.error('Failed to start Tempering', error);
          this.craftingService.setQueue(
            this.craftingService.currentQueue.filter(
              (item) => item.id !== payload.queueId,
            ),
          );
          this.inventoryState.load(true);
          this.refreshCurrentAction(true);
          return of(null);
        }),
      )
      .subscribe();
  }

  stopAction(): void {
    if (this._stoppingAction()) return;

    if (this.isCombatAction()) this._idleCombatPhase.set('stopping');
    this._resolvingOfflineProgress.set(false);
    this._stoppingAction.set(true);
    this.handleDeletionOfCurrentAction();
    // Tear down the old action synchronously. The retained deleted snapshot
    // carries the effective combat restart boundary, so Tempering remains clickable.
    // Doing this in the response callback could clear a successor action.
    this.clear();

    let stopSucceeded = false;
    this.actionsService
      .stop()
      .pipe(
        tap(() => {
          stopSucceeded = true;
        }),
        catchError((err) => {
          console.error('Failed to stop action', err);
          this.pendingStartAfterStop = null;
          this.refreshCurrentAction(true);
          return of(null);
        }),
        finalize(() => {
          this._stoppingAction.set(false);
          const pendingStart = this.pendingStartAfterStop;
          this.pendingStartAfterStop = null;
          if (stopSucceeded && pendingStart) {
            this.startAction(pendingStart.type, pendingStart.payload);
          }
        }),
      )
      .subscribe();
  }

  handleDeletionOfCurrentAction() {
    const currentAction = this._currentAction();
    if (!currentAction) return;

    const unlockDeadline = this.stopUnlockDeadline(currentAction);
    const waitsForSwitchUnlock = unlockDeadline > Date.now();
    const updated = {
      ...currentAction,
      isDeleted: true,
      updatedAt: waitsForSwitchUnlock ? currentAction.updatedAt : new Date(),
      blockedUntilUtc: waitsForSwitchUnlock
        ? new Date(unlockDeadline)
        : null,
      nextResolutionAtUtc: null,
      craftingActionDetails: undefined,
      combatActionDetails: undefined,
    };
    this.updateDisplay(updated);
    this._currentAction.set(updated);
  }

  displayCurrentAction(): boolean {
    return this.showAction();
  }

  canStartAction(type: CharacterActionType): boolean {
    // Keep time-based switch eligibility reactive while an active combat
    // snapshot itself remains unchanged.
    this._tickingDuration();
    const action = this._currentAction();
    if (!action) return true;
    if (this.isActionCooldown()) {
      return type === CharacterActionType.Crafting;
    }
    if (
      action.isDeleted ||
      action.characterActionType === CharacterActionType.Idle
    ) {
      return true;
    }

    if (
      action.characterActionType === CharacterActionType.Crafting &&
      type === CharacterActionType.Combat
    ) {
      return !(
        action.blockedUntilUtc &&
        new Date(action.blockedUntilUtc).getTime() > Date.now()
      );
    }

    if (
      action.characterActionType === CharacterActionType.Combat &&
      type === CharacterActionType.Combat
    ) {
      return true;
    }

    return (
      type === CharacterActionType.Crafting &&
      (action.characterActionType === CharacterActionType.Crafting ||
        action.characterActionType === CharacterActionType.Combat)
    );
  }

  clear(): void {
    this.polling.stop();
    this.persistence.clear();

    this._tickingDuration.set(0);
    this._startTime.set(null);
    this._resolvingOfflineProgress.set(false);

    const action = this._currentAction();
    if (!action) return;

    this._currentAction.set({ ...action, isDeleted: true });
  }

  reset(): void {
    this.resetVersion += 1;
    this.pendingStartAfterStop = null;
    this.clear();
    this.activeActionRefreshes = 0;
    if (this.actionRefreshLoadingTimeout) {
      clearTimeout(this.actionRefreshLoadingTimeout);
      this.actionRefreshLoadingTimeout = null;
    }
    this._loadingActionRefresh.set(false);
    this._resumingTempering.set(false);
    this.activeOfflineCatchUpRequests = 0;
    this._resolvingOfflineProgress.set(false);
    this._loadingCombat.set(false);
    this._idleCombatPhase.set('idle');
    this._idleCombatError.set(null);
    this._showAction.set(false);
    this._currentAction.set(null);
    this.craftingHandler.clear();
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

    const deadline = this.switchUnlockDeadline(action);
    const now = Date.now();

    if (deadline < now) {
      this._showAction.set(false);
    } else {
      this._showAction.set(true);
      const deletedActionKey = this.getActionUpdateKey(action);
      setTimeout(() => {
        if (
          this.getActionUpdateKey(this._currentAction()) !== deletedActionKey
        ) {
          return;
        }

        this._showAction.set(false);
        this.combatService.clearAllCombat();
        this._currentAction.set(null);
        this.hide();
      }, deadline - now);
    }
  }

  hide(): void {
    this._showAction.set(false);
  }

  refreshCurrentAction(restartPolling = false): void {
    const requestVersion = this.resetVersion;

    this.trackActionRefresh(
      this.resolveCurrentActionRequest().pipe(
        catchError((err) => {
          console.error('[Manual Refresh] Failed to fetch current action', err);
          this.setIdleCombatError(err);
          return of(this._currentAction());
        }),
      ),
    ).subscribe((action) => {
      if (requestVersion !== this.resetVersion) return;
      this.applyActionUpdate(action, restartPolling);
      if (restartPolling) this.startPolling(action);
    });
  }

  resumeTempering(): void {
    if (this._resumingTempering()) return;

    this._resumingTempering.set(true);
    this.persistence.set(CharacterActionType.Crafting);
    this.actionsService
      .resumeTempering()
      .pipe(
        tap((action) => {
          this._currentAction.set(action);
          this.startPolling(action);
        }),
        catchError((err) => {
          console.error('Failed to resume Tempering', err);
          const currentAction = this._currentAction();
          if (currentAction) {
            this.persistence.set(currentAction.characterActionType);
          } else {
            this.persistence.clear();
          }
          return of(null);
        }),
        finalize(() => this._resumingTempering.set(false)),
      )
      .subscribe();
  }

  applyCurrentActionSnapshot(action: CharacterActionDto | null): void {
    this.applyActionUpdate(action);
  }

  applyTemperingQueueMutation(
    state: TemperingActionState | null,
    queue: CraftingQueueItem[],
  ): void {
    if (!state) {
      this.refreshCurrentAction();
      return;
    }

    const current = this._currentAction();
    const isCrafting =
      state.characterActionType === CharacterActionType.Crafting;
    const isCombat = state.characterActionType === CharacterActionType.Combat;
    const action: CharacterActionDto = {
      ...(current ?? {
        lootTableId: '',
        revision: state.revision,
        updatedAt: state.updatedAt,
        isDeleted: state.isDeleted,
        characterActionType: state.characterActionType,
      }),
      characterActionType: state.characterActionType,
      updatedAt: new Date(state.updatedAt),
      nextResolutionAtUtc: state.nextResolutionAtUtc
        ? new Date(state.nextResolutionAtUtc)
        : null,
      blockedUntilUtc: state.blockedUntilUtc
        ? new Date(state.blockedUntilUtc)
        : null,
      scheduleGeneration: state.scheduleGeneration,
      revision: state.revision,
      isDeleted: state.isDeleted,
      resolutionIntervalMs:
        state.resolutionIntervalMs ??
        current?.resolutionIntervalMs ??
        DEFAULT_IDLE_COMBAT_RESOLUTION_INTERVAL_MS,
      processedCount: 0,
      hasMoreDueWork: false,
      temperingQueueItems: [...queue],
      craftingActionDetails: isCrafting
        ? { craftingQueueItems: [...queue] }
        : undefined,
      combatActionDetails: isCombat
        ? current?.combatActionDetails
        : undefined,
      combatSession: isCombat ? current?.combatSession : undefined,
      temperingSession: isCrafting ? current?.temperingSession : undefined,
    };

    this.applyActionUpdate(action, true);
  }

  private applyActionUpdate(
    action: CharacterActionDto | null,
    authoritative = false,
  ): void {
    if (
      action?.hasMoreDueWork &&
      this._idleCombatError() === null
    ) {
      this._resolvingOfflineProgress.set(true);
      this._idleCombatPhase.set('resolving');
    }

    const current = this._currentAction();
    const currentKey = this.getActionUpdateKey(current);
    const nextKey = this.getActionUpdateKey(action);

    if (!authoritative && currentKey && nextKey && currentKey === nextKey) {
      return;
    }

    if (
      !authoritative &&
      current &&
      action &&
      this.isOlderUpdate(current, action)
    ) {
      return;
    }

    const shouldOpenAutoResumedCombat =
      action?.autoResumedFromTempering === true && this.isTemperingRoute();
    this._currentAction.set(action);
    if (action) {
      this.persistence.set(action.characterActionType);
    } else {
      this.persistence.clear();
    }

    if (shouldOpenAutoResumedCombat) {
      void this.router.navigate(['/game/combat']);
    }
  }

  private isTemperingRoute(): boolean {
    return (
      this.router.url.startsWith('/game/professions/crafting') &&
      /(?:\?|&)tab=tempering(?:&|$)/.test(this.router.url)
    );
  }

  private isOlderUpdate(
    current: CharacterActionDto,
    candidate: CharacterActionDto,
  ): boolean {
    if (current.characterActionType !== candidate.characterActionType) {
      return false;
    }

    const currentBoundary = new Date(
      current.nextResolutionAtUtc ?? current.updatedAt,
    ).getTime();
    const candidateBoundary = new Date(
      candidate.nextResolutionAtUtc ?? candidate.updatedAt,
    ).getTime();

    if (candidateBoundary < currentBoundary) return true;

    // A concurrent/early resolver can legitimately report that no new encounter
    // was due. Never let that less-hydrated response replace the encounter that
    // is already on screen at the same boundary.
    return (
      candidateBoundary === currentBoundary &&
      this.hasRenderableCombatResult(current) &&
      !this.hasRenderableCombatResult(candidate)
    );
  }

  private hasRenderableCombatResult(action: CharacterActionDto): boolean {
    const result = action.combatSession?.combatResult;
    return !!result?.playerTeam?.length && !!result.enemyTeam?.length;
  }

  private getActionUpdateKey(action: CharacterActionDto | null): string | null {
    if (!action) return null;

    const craftingQueueOrder =
      (
        action.temperingQueueItems ??
        action.craftingActionDetails?.craftingQueueItems
      )
        ?.map((item) => item.id)
        .join(',') ?? '';

    if (action.revision) {
      return `${action.characterActionType}|${action.revision}|${action.isDeleted}|${craftingQueueOrder}`;
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
      craftingQueueOrder,
    ].join('|');
  }

  private trackActionRefresh(
    request$: Observable<CharacterActionDto | null>,
  ): Observable<CharacterActionDto | null> {
    this.activeActionRefreshes += 1;

    if (!this.actionRefreshLoadingTimeout) {
      this.actionRefreshLoadingTimeout = setTimeout(() => {
        if (
          this.activeActionRefreshes > 0 &&
          this.shouldShowActionRefreshLoading()
        ) {
          this._loadingActionRefresh.set(true);
        }
      }, 250);
    }

    return request$.pipe(
      finalize(() => {
        this.activeActionRefreshes = Math.max(
          0,
          this.activeActionRefreshes - 1,
        );

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
    const actionAtRequestStart = this._currentAction();
    const isOfflineCatchUp = isOfflineCombatCatchUpRequest(
      actionAtRequestStart,
      Date.now(),
    );

    if (
      actionAtRequestStart?.characterActionType === CharacterActionType.Combat
    ) {
      this._idleCombatPhase.set('resolving');
      if (isOfflineCatchUp) {
        this.activeOfflineCatchUpRequests += 1;
        this._resolvingOfflineProgress.set(true);
      }
    }

    return this.actionsService.resolveCurrentAction().pipe(
      tap((action) => {
        this._idleCombatError.set(null);
        if (
          action?.characterActionType === CharacterActionType.Combat &&
          !action.isDeleted
        ) {
          this._idleCombatPhase.set(
            action.hasMoreDueWork ? 'resolving' : 'active',
          );
        }
      }),
      finalize(() => {
        if (isOfflineCatchUp) {
          this.activeOfflineCatchUpRequests = Math.max(
            0,
            this.activeOfflineCatchUpRequests - 1,
          );
        }

        const currentAction = this._currentAction();
        const serverReportsMoreWork = currentAction?.hasMoreDueWork ?? false;
        if (this.activeOfflineCatchUpRequests === 0 && !serverReportsMoreWork) {
          this._resolvingOfflineProgress.set(false);
        }
      }),
    );
  }

  retryIdleCombatResolution(): void {
    this._idleCombatError.set(null);
    this.refreshCurrentAction();
  }

  private acceptStartedCombat(action: CharacterActionDto): void {
    if (
      action.characterActionType !== CharacterActionType.Combat ||
      action.isDeleted
    ) {
      this.failCombatStart(
        new Error('The server did not return an active combat action.'),
      );
      return;
    }

    this._idleCombatError.set(null);
    this._resolvingOfflineProgress.set(false);
    this._loadingCombat.set(false);
    this._idleCombatPhase.set('active');
    // A successful command response is authoritative. Polling updates still
    // use freshness checks, but a previously cached/deleted combat action must
    // never prevent the newly started action from becoming visible.
    this._currentAction.set(action);
    this.persistence.set(action.characterActionType);

    // Starting the follow-up poller is secondary to opening the combat that the
    // server has already accepted. Replacing an older action poller must not be
    // able to throw here and prevent the route change.
    void this.router.navigate(['/game/combat']);

    try {
      this.startPolling(action);
    } catch (error) {
      console.error('Failed to start combat polling', error);
    }
  }

  private recoverStartedCombat(
    startError: unknown,
  ): Observable<CharacterActionDto | null> {
    return this.actionsService.resolveCurrentAction().pipe(
      tap((action) => {
        if (
          action?.characterActionType === CharacterActionType.Combat &&
          !action.isDeleted
        ) {
          this.acceptStartedCombat(action);
          return;
        }

        this.failCombatStart(startError);
      }),
      catchError((recoveryError) => {
        console.error(
          'Failed to reconcile combat after the start request failed',
          recoveryError,
        );
        this.failCombatStart(startError);
        return of(null);
      }),
    );
  }

  private failCombatStart(error: unknown): void {
    this.reset();
    this.setIdleCombatError(error);
  }

  private setIdleCombatError(error: unknown): void {
    this._resolvingOfflineProgress.set(false);
    this._idleCombatPhase.set('error');

    if (error instanceof HttpErrorResponse && error.status === 409) {
      this._idleCombatError.set(
        'Your progress was resolved by another request. Refresh to load the latest action state.',
      );
      return;
    }

    this._idleCombatError.set(
      'Action could not be resolved - service might be unavailable. Retry again.',
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

  private switchUnlockDeadline(action: CharacterActionDto): number {
    const deadline = action.blockedUntilUtc ?? action.updatedAt;
    return new Date(deadline).getTime();
  }

  private stopUnlockDeadline(action: CharacterActionDto): number {
    const switchDeadline = this.switchUnlockDeadline(action);
    if (
      action.characterActionType !== CharacterActionType.Combat ||
      !action.nextResolutionAtUtc
    ) {
      return switchDeadline;
    }

    return Math.max(
      switchDeadline,
      new Date(action.nextResolutionAtUtc).getTime(),
    );
  }
}
