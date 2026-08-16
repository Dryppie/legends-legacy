import { signal } from '@angular/core';
import {
  fakeAsync,
  flushMicrotasks,
  TestBed,
  tick,
} from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../../../shared/models/enums/characterActionType';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { GameService } from '../../client-side/game/game.service';
import { CombatService } from '../../client-side/combat/combat.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { CharacterActionsService } from './character-actions.service';
import { CharacterActionsStateService } from './character-actions.state.service';
import { CombatActionHandler } from './handlers/combat-action-handler';
import { CraftingActionHandler } from './handlers/crafting-action-handler';
import { CharacterActionTypePersistenceService } from './helpers/character-action-type-persistence.service';
import { CharacterActionsPollingService } from './helpers/characterActionsPollingService';

describe('CharacterActionsStateService', () => {
  let service: CharacterActionsStateService;
  let actions: jasmine.SpyObj<CharacterActionsService>;
  let polling: jasmine.SpyObj<CharacterActionsPollingService>;
  let router: jasmine.SpyObj<Router>;
  let combat: jasmine.SpyObj<CombatService>;
  let logoutCount: ReturnType<typeof signal<number>>;

  beforeEach(() => {
    actions = jasmine.createSpyObj<CharacterActionsService>(
      'CharacterActionsService',
      ['startCombat', 'startCrafting', 'resolveCurrentAction', 'stop'],
    );
    polling = jasmine.createSpyObj<CharacterActionsPollingService>(
      'CharacterActionsPollingService',
      ['start', 'stop'],
    );
    router = jasmine.createSpyObj<Router>('Router', ['navigate'], {
      url: '/game/combat',
    });
    router.navigate.and.resolveTo(true);
    combat = jasmine.createSpyObj<CombatService>('CombatService', [
      'clearAllCombat',
      'stop',
    ]);
    logoutCount = signal(0);

    TestBed.configureTestingModule({
      providers: [
        CharacterActionsStateService,
        { provide: CharacterActionsService, useValue: actions },
        { provide: CharacterActionsPollingService, useValue: polling },
        {
          provide: CharacterActionTypePersistenceService,
          useValue: jasmine.createSpyObj('ActionPersistence', ['set', 'clear']),
        },
        {
          provide: CombatActionHandler,
          useValue: jasmine.createSpyObj('CombatActionHandler', ['handle']),
        },
        {
          provide: CraftingActionHandler,
          useValue: jasmine.createSpyObj('CraftingActionHandler', ['handle']),
        },
        {
          provide: GameService,
          useValue: jasmine.createSpyObj('GameService', [
            'resumeCombat',
            'endCombat',
          ]),
        },
        {
          provide: CombatService,
          useValue: combat,
        },
        {
          provide: InventoryStateService,
          useValue: jasmine.createSpyObj('InventoryStateService', ['load']),
        },
        { provide: EventBusService, useValue: { logout: logoutCount } },
        { provide: Router, useValue: router },
      ],
    });

    service = TestBed.inject(CharacterActionsStateService);
  });

  it('stores the returned action and navigates immediately after combat starts', () => {
    const action = combatAction();
    actions.startCombat.and.returnValue(of(action));

    service.startAction(CharacterActionType.Combat, { areaId: 'lumo-ruins' });

    expect(service.currentAction()).toBe(action);
    expect(service.idleCombatPhase()).toBe('active');
    expect(polling.start).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/game/combat']);
  });

  it('opens confirmed combat even if replacing the previous poller fails', () => {
    const action = combatAction();
    actions.startCombat.and.returnValue(of(action));
    polling.start.and.throwError(new Error('stale poller teardown failed'));

    service.startAction(CharacterActionType.Combat, { areaId: 'lumo-ruins' });

    expect(service.currentAction()).toBe(action);
    expect(router.navigate).toHaveBeenCalledWith(['/game/combat']);
    expect(service.idleCombatError()).toBeNull();
  });

  it('waits for the confirmed start response before opening combat', () => {
    const startResult = new Subject<CharacterActionDto>();
    actions.startCombat.and.returnValue(startResult.asObservable());

    service.startAction(CharacterActionType.Combat, { areaId: 'lumo-ruins' });

    expect(router.navigate).not.toHaveBeenCalled();
    expect(service.loadingCombat()).toBeTrue();
    expect(service.currentAction()).toBeNull();

    startResult.next(combatAction());
    startResult.complete();

    expect(service.currentAction()).not.toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/game/combat']);
  });

  it('replaces stale cached combat state with the successful start response', () => {
    const staleAction: CharacterActionDto = {
      ...combatAction(),
      updatedAt: new Date('2026-08-08T12:00:20Z'),
      nextResolutionAt: new Date('2026-08-08T12:00:20Z'),
      revision: 'stale-combat-revision',
      isDeleted: true,
    };
    const startedAction = combatAction();
    actions.startCombat.and.returnValue(of(startedAction));

    service.initializeFromBootstrap(staleAction);
    const applyBootstrapAction = polling.start.calls.mostRecent().args[1];
    applyBootstrapAction(staleAction);

    service.startAction(CharacterActionType.Combat, { areaId: 'lumo-ruins' });

    expect(service.currentAction()).toBe(startedAction);
  });

  it('does not let delayed cleanup from a deleted action erase newly started combat', fakeAsync(() => {
    const deletedAt = new Date(Date.now() + 1_000);
    const deletedAction: CharacterActionDto = {
      ...combatAction(),
      characterActionType: CharacterActionType.Crafting,
      updatedAt: deletedAt,
      nextResolutionAt: deletedAt,
      revision: 'deleted-crafting-revision',
      isDeleted: true,
    };
    const startedAction = combatAction();
    actions.startCombat.and.returnValue(of(startedAction));

    service.initializeFromBootstrap(deletedAction);
    const applyDeletedAction = polling.start.calls.mostRecent().args[1];
    applyDeletedAction(deletedAction);
    flushMicrotasks();

    service.startAction(CharacterActionType.Combat, { areaId: 'lumo-ruins' });
    tick(1_001);

    expect(service.currentAction()).toBe(startedAction);
    expect(combat.clearAllCombat).not.toHaveBeenCalled();
  }));

  it('keeps a stopped combat visible and blocks new actions until its deadline', fakeAsync(() => {
    const deadline = new Date(Date.now() + 10_000);
    const stoppingCombat: CharacterActionDto = {
      ...combatAction(),
      updatedAt: deadline,
      nextResolutionAt: deadline,
      revision: 'stopping-combat-revision',
      isDeleted: true,
    };

    service.applyCurrentActionSnapshot(stoppingCombat);
    TestBed.flushEffects();
    flushMicrotasks();

    expect(service.displayCurrentAction()).toBeTrue();
    expect(service.isActionCooldown()).toBeTrue();
    expect(service.canStartAction(CharacterActionType.Combat)).toBeFalse();
    expect(service.canStartAction(CharacterActionType.Crafting)).toBeFalse();

    tick(10_001);

    expect(service.displayCurrentAction()).toBeFalse();
    expect(service.isActionCooldown()).toBeFalse();
    expect(service.canStartAction(CharacterActionType.Crafting)).toBeTrue();
  }));

  it('allows more Tempering items during Tempering but blocks Combat', () => {
    service.applyCurrentActionSnapshot({
      ...combatAction(),
      characterActionType: CharacterActionType.Crafting,
      updatedAt: new Date(Date.now() - 1_000),
      nextResolutionAt: new Date(Date.now() - 1_000),
      revision: 'active-crafting-revision',
      isDeleted: false,
    });

    expect(service.canStartAction(CharacterActionType.Crafting)).toBeTrue();
    expect(service.canStartAction(CharacterActionType.Combat)).toBeFalse();
  });

  it('does not treat later action changes as another logout', fakeAsync(() => {
    logoutCount.set(1);
    TestBed.flushEffects();

    const startedAction = combatAction();
    actions.startCombat.and.returnValue(of(startedAction));
    service.startAction(CharacterActionType.Combat, { areaId: 'lumo-ruins' });
    TestBed.flushEffects();

    expect(service.currentAction()).toBe(startedAction);
    expect(router.navigate).toHaveBeenCalledWith(['/game/combat']);
  }));

  it('recovers an already-started combat after an ambiguous start error', () => {
    const action = combatAction();
    actions.startCombat.and.returnValue(
      throwError(() => new Error('Connection closed')),
    );
    actions.resolveCurrentAction.and.returnValue(of(action));

    service.startAction(CharacterActionType.Combat, { areaId: 'lumo-ruins' });

    expect(actions.resolveCurrentAction).toHaveBeenCalled();
    expect(service.currentAction()).toBe(action);
    expect(router.navigate).toHaveBeenCalledWith(['/game/combat']);
    expect(service.idleCombatError()).toBeNull();
  });

  it('reconciles combat when the start response has no action body', () => {
    const action = combatAction();
    actions.startCombat.and.returnValue(
      of(null as unknown as CharacterActionDto),
    );
    actions.resolveCurrentAction.and.returnValue(of(action));

    service.startAction(CharacterActionType.Combat, { areaId: 'lumo-ruins' });

    expect(actions.resolveCurrentAction).toHaveBeenCalled();
    expect(service.currentAction()).toBe(action);
    expect(router.navigate).toHaveBeenCalledWith(['/game/combat']);
    expect(service.idleCombatError()).toBeNull();
  });

  it('keeps offline progress visibly resolving until the final chunk is handled', fakeAsync(() => {
    const pendingAction: CharacterActionDto = {
      ...combatAction(),
      hasPendingCombatResolution: true,
      revision: 'pending-combat-revision',
    };

    service.initializeFromBootstrap(pendingAction);
    const applyUpdate = polling.start.calls.mostRecent().args[1];
    applyUpdate(pendingAction);
    TestBed.flushEffects();
    flushMicrotasks();

    expect(service.resolvingOfflineProgress()).toBeTrue();
    expect(service.idleCombatPhase()).toBe('resolving');

    applyUpdate({
      ...pendingAction,
      hasPendingCombatResolution: false,
      revision: 'completed-combat-revision',
      combatSession: {
        from: new Date('2026-08-08T12:00:00Z'),
        to: new Date('2026-08-08T12:16:40Z'),
        combatResult: {} as never,
        combatSummary: {
          totalBattles: 100,
          wins: 100,
          losses: 0,
          draws: 0,
          totalExperience: 400,
          totalGold: 0,
          totalCinders: 12,
          totalSoulstones: 3,
        },
      },
    });
    TestBed.flushEffects();
    flushMicrotasks();

    expect(service.resolvingOfflineProgress()).toBeFalse();
    expect(service.idleCombatPhase()).toBe('active');
  }));
});

function combatAction(): CharacterActionDto {
  const nextResolutionAt = new Date('2026-08-08T12:00:10Z');

  return {
    characterActionType: CharacterActionType.Combat,
    lootTableId: 'lumo-ruins',
    updatedAt: nextResolutionAt,
    nextResolutionAt,
    revision: 'combat-revision',
    isDeleted: false,
  };
}
