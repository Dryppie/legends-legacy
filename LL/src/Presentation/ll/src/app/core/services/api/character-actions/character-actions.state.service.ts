import { Injectable, signal, computed, effect } from '@angular/core';
import {
  CharacterActionDto,
  StartCombatActionRequest,
  StartCraftingActionRequest,
} from '../../../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../../../shared/models/enums/characterActionType';
import { CombatActionHandler } from './handlers/combat-action-handler';
import { CraftingActionHandler } from './handlers/crafting-action-handler';
import { GatheringActionHandler } from './handlers/gathering-action-handler';
import { CharacterActionsPollingService } from './helpers/characterActionsPollingService';
import { catchError, of, Observable, tap } from 'rxjs';
import { CharacterActionsService } from './character-actions.service';
import { CharacterActionTypePersistenceService } from './helpers/character-action-type-persistence.service';
import { GameService } from '../../client-side/game/game.service';
import { CombatService } from '../../client-side/combat/combat.service';

@Injectable({ providedIn: 'root' })
export class CharacterActionsStateService {
  private readonly _showAction = signal(false);
  readonly showAction = computed(() => this._showAction());

  private readonly _currentAction = signal<CharacterActionDto | null>(null);
  readonly currentAction = this._currentAction;

  private readonly _loadingCombat = signal(false);
  readonly loadingCombat = computed(() => this._loadingCombat());

  readonly isCombatAction = computed(
    () =>
      this._currentAction()?.characterActionType === CharacterActionType.Combat,
  );

  readonly isCraftingAction = computed(
    () =>
      this._currentAction()?.characterActionType ===
      CharacterActionType.Crafting,
  );

  readonly isGatheringAction = computed(
    () =>
      this._currentAction()?.characterActionType ===
      CharacterActionType.Gathering,
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
    private readonly gatheringHandler: GatheringActionHandler,
    private readonly gameService: GameService,
    private readonly combatService: CombatService,
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
        case CharacterActionType.Gathering:
          queueMicrotask(() => this.gatheringHandler.handle(action));
          break;
      }
    });
  }

  init(): void {
    this.startPolling();
  }

  private startPolling(): void {
    this.polling.start(
      () =>
        this.actionsService.getCurrentAction().pipe(
          catchError((err) => {
            console.error('[Polling] Failed to fetch current action', err);
            return of(null);
          }),
        ),
      (action) => {
        this._currentAction.set(action);
        if (action) {
          this.persistence.set(action.characterActionType);
        }
      },
    );
  }

  startAction(
    type: CharacterActionType,
    payload: StartCombatActionRequest | StartCraftingActionRequest | string,
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
      case CharacterActionType.Gathering:
        call$ = this.actionsService.startGathering(payload as string);
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
    this.handleDeletionOfCurrentAction();

    this.actionsService
      .stop()
      .pipe(
        tap(() => this.clear()),
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
      gatheringActionDetails: undefined,
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
}
