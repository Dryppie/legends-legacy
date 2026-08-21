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
import {
  CharacterActionsStateService,
  isOfflineCombatCatchUpRequest,
} from './character-actions.state.service';
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
  let craftingHandler: jasmine.SpyObj<CraftingActionHandler>;
  let inventory: jasmine.SpyObj<InventoryStateService>;
  let logoutCount: ReturnType<typeof signal<number>>;

  beforeEach(() => {
    actions = jasmine.createSpyObj<CharacterActionsService>(
      'CharacterActionsService',
      [
        'startCombat',
        'startCrafting',
        'resumeTempering',
        'resolveCurrentAction',
        'stop',
      ],
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
    craftingHandler = jasmine.createSpyObj<CraftingActionHandler>(
      'CraftingActionHandler',
      ['handle', 'clear'],
    );
    inventory = jasmine.createSpyObj<InventoryStateService>(
      'InventoryStateService',
      ['load'],
    );
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
          useValue: craftingHandler,
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
          useValue: inventory,
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

  it('lets polling stop after a resolve error instead of re-emitting the overdue action', () => {
    spyOn(console, 'error');
    const overdueTempering: CharacterActionDto = {
      ...combatAction(),
      characterActionType: CharacterActionType.Crafting,
      nextResolutionAtUtc: new Date(Date.now() - 1_000),
      nextResolutionAt: new Date(Date.now() - 1_000),
      revision: 'overdue-tempering-revision',
    };
    const resolveError = new Error('save failed');
    actions.resolveCurrentAction.and.returnValue(
      throwError(() => resolveError),
    );
    service.initializeFromBootstrap(overdueTempering);
    const fetch = polling.start.calls.mostRecent().args[0];
    let observedError: unknown;
    let emittedAction = false;

    fetch().subscribe({
      next: () => (emittedAction = true),
      error: (error) => (observedError = error),
    });

    expect(observedError).toBe(resolveError);
    expect(emittedAction).toBeFalse();
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

  it('keeps stopped combat visible but allows Tempering to queue during its lock', fakeAsync(() => {
    const deadline = new Date(Date.now() + 10_000);
    const stoppingCombat: CharacterActionDto = {
      ...combatAction(),
      blockedUntilUtc: deadline,
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
    expect(service.canStartAction(CharacterActionType.Crafting)).toBeTrue();

    tick(10_001);

    expect(service.displayCurrentAction()).toBeFalse();
    expect(service.isActionCooldown()).toBeFalse();
    expect(service.canStartAction(CharacterActionType.Crafting)).toBeTrue();
  }));

  it('waits for Combat quit to finish before sending queued Tempering', () => {
    const stopResult = new Subject<void>();
    const payload = {
      queueId: '8f6cb596-94df-4a84-b6f2-4b4d6384e065',
      itemInstanceId: '6c79774b-d048-4698-9c04-c77e481c7aa2',
    };
    actions.stop.and.returnValue(stopResult.asObservable());
    actions.startCrafting.and.returnValue(of(true));
    service.applyCurrentActionSnapshot(combatAction());

    service.stopAction();
    service.startAction(CharacterActionType.Crafting, payload);

    expect(actions.startCrafting).not.toHaveBeenCalled();
    expect(service.canStartAction(CharacterActionType.Crafting)).toBeTrue();

    stopResult.next();
    stopResult.complete();

    expect(actions.startCrafting).toHaveBeenCalledOnceWith(payload);
    expect(polling.start).toHaveBeenCalled();
  });

  it('restores authoritative action state when stopping fails', () => {
    const activeAction = combatAction();
    actions.stop.and.returnValue(throwError(() => new Error('offline')));
    actions.resolveCurrentAction.and.returnValue(of(activeAction));
    service.applyCurrentActionSnapshot(activeAction);

    service.stopAction();

    expect(actions.resolveCurrentAction).toHaveBeenCalledTimes(1);
    expect(service.currentAction()).toBe(activeAction);
    expect(service.currentAction()?.isDeleted).toBeFalse();
    expect(polling.start).toHaveBeenCalled();
  });

  it('clears the previous idle-combat encounter after Tempering starts', () => {
    const payload = {
      queueId: '8f6cb596-94df-4a84-b6f2-4b4d6384e065',
      itemInstanceId: '6c79774b-d048-4698-9c04-c77e481c7aa2',
    };
    actions.startCrafting.and.returnValue(of(true));

    service.startAction(CharacterActionType.Crafting, payload);

    expect(combat.clearAllCombat).toHaveBeenCalledTimes(1);
    expect(polling.start).toHaveBeenCalled();
  });

  it('allows Combat to replace active Tempering immediately', () => {
    service.applyCurrentActionSnapshot({
      ...combatAction(),
      characterActionType: CharacterActionType.Crafting,
      updatedAt: new Date(Date.now() - 1_000),
      nextResolutionAt: new Date(Date.now() - 1_000),
      revision: 'active-crafting-revision',
      isDeleted: false,
    });

    expect(service.canStartAction(CharacterActionType.Crafting)).toBeTrue();
    expect(service.canStartAction(CharacterActionType.Combat)).toBeTrue();
  });

  it('keeps the server-provided paused Tempering queue when Combat starts', fakeAsync(() => {
    service.applyCurrentActionSnapshot({
      ...combatAction(),
      characterActionType: CharacterActionType.Crafting,
      updatedAt: new Date(Date.now() - 1_000),
      revision: 'active-crafting-revision',
      isDeleted: false,
    });
    const pausedQueue = [{ id: 'paused-item' }] as never[];
    const startedCombat = {
      ...combatAction(),
      temperingQueueItems: pausedQueue,
    };
    actions.startCombat.and.returnValue(of(startedCombat));

    service.startAction(CharacterActionType.Combat, { areaId: 'lumo-ruins' });
    TestBed.flushEffects();
    flushMicrotasks();

    expect(service.currentAction()).toBe(startedCombat);
    expect(craftingHandler.handle).toHaveBeenCalledWith(startedCombat);
    expect(craftingHandler.clear).not.toHaveBeenCalled();
    expect(inventory.load).not.toHaveBeenCalled();
  }));

  it('resumes a paused Tempering queue', () => {
    const resumedAction = {
      ...combatAction(),
      characterActionType: CharacterActionType.Crafting,
      temperingQueueItems: [{ id: 'paused-item' }] as never[],
      revision: 'resumed-tempering-revision',
    };
    actions.resumeTempering.and.returnValue(of(resumedAction));

    service.resumeTempering();

    expect(actions.resumeTempering).toHaveBeenCalled();
    expect(service.currentAction()).toBe(resumedAction);
    expect(polling.start).toHaveBeenCalled();
  });

  it('blocks Combat while Tempering still carries an inherited Combat lock', () => {
    service.applyCurrentActionSnapshot({
      ...combatAction(),
      characterActionType: CharacterActionType.Crafting,
      blockedUntilUtc: new Date(Date.now() + 5_000),
      revision: 'combat-queued-crafting-revision',
      isDeleted: false,
    });

    expect(service.canStartAction(CharacterActionType.Crafting)).toBeTrue();
    expect(service.canStartAction(CharacterActionType.Combat)).toBeFalse();
  });

  it('keeps queued Tempering pending until the inherited Combat lock expires', fakeAsync(() => {
    const pendingTempering: CharacterActionDto = {
      ...combatAction(),
      characterActionType: CharacterActionType.Crafting,
      blockedUntilUtc: new Date(Date.now() + 5_000),
      nextResolutionAtUtc: new Date(Date.now() + 15_000),
      nextResolutionAt: new Date(Date.now() + 15_000),
      revision: 'pending-tempering-revision',
      isDeleted: false,
    };

    service.initializeFromBootstrap(pendingTempering);
    const applyAction = polling.start.calls.mostRecent().args[1];
    applyAction(pendingTempering);
    TestBed.flushEffects();
    flushMicrotasks();

    expect(service.isTemperingPendingCombatUnlock()).toBeTrue();
    expect(service.temperingCombatUnlockSeconds()).toBeGreaterThan(0);

    tick(5_001);

    expect(service.isTemperingPendingCombatUnlock()).toBeFalse();
    expect(service.temperingCombatUnlockSeconds()).toBe(0);
  }));

  it('allows Tempering to replace active Combat but blocks another Combat start', () => {
    service.applyCurrentActionSnapshot(combatAction());

    expect(service.canStartAction(CharacterActionType.Crafting)).toBeTrue();
    expect(service.canStartAction(CharacterActionType.Combat)).toBeFalse();
  });

  it('allows a new action immediately after Tempering is stopped', () => {
    actions.stop.and.returnValue(of(undefined));
    service.applyCurrentActionSnapshot({
      ...combatAction(),
      characterActionType: CharacterActionType.Crafting,
      nextResolutionAtUtc: new Date(Date.now() + 10_000),
      nextResolutionAt: new Date(Date.now() + 10_000),
      revision: 'active-crafting-revision',
      isDeleted: false,
    });

    service.stopAction();

    expect(service.isActionCooldown()).toBeFalse();
    expect(service.canStartAction(CharacterActionType.Combat)).toBeTrue();
    expect(service.canStartAction(CharacterActionType.Crafting)).toBeTrue();
  });

  it('allows an immediate action after stopping Combat once its initial lock expired', () => {
    actions.stop.and.returnValue(of(undefined));
    service.applyCurrentActionSnapshot({
      ...combatAction(),
      blockedUntilUtc: new Date(Date.now() - 1_000),
      nextResolutionAtUtc: new Date(Date.now() + 9_000),
      nextResolutionAt: new Date(Date.now() + 9_000),
      revision: 'unlocked-combat-revision',
    });

    service.stopAction();

    expect(service.isActionCooldown()).toBeFalse();
    expect(service.canStartAction(CharacterActionType.Crafting)).toBeTrue();
  });

  it('preserves the original Combat lock when queued Tempering is stopped', () => {
    actions.stop.and.returnValue(of(undefined));
    service.applyCurrentActionSnapshot({
      ...combatAction(),
      characterActionType: CharacterActionType.Crafting,
      blockedUntilUtc: new Date(Date.now() + 5_000),
      nextResolutionAtUtc: new Date(Date.now() + 15_000),
      nextResolutionAt: new Date(Date.now() + 15_000),
      revision: 'combat-queued-crafting-revision',
    });

    service.stopAction();

    expect(service.isActionCooldown()).toBeTrue();
    expect(service.canStartAction(CharacterActionType.Combat)).toBeFalse();
    expect(service.canStartAction(CharacterActionType.Crafting)).toBeTrue();
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

  it('shows offline catch-up while an overdue resolve request is in flight', () => {
    const now = Date.now();
    const overdueAction: CharacterActionDto = {
      ...combatAction(),
      nextResolutionAtUtc: new Date(now - 24 * 60 * 60 * 1_000),
      resolutionIntervalMs: 10_000,
      revision: 'overdue-combat-revision',
    };
    const resolveResult = new Subject<CharacterActionDto | null>();
    actions.resolveCurrentAction.and.returnValue(resolveResult.asObservable());
    service.applyCurrentActionSnapshot(overdueAction);

    service.refreshCurrentAction();

    expect(service.resolvingOfflineProgress()).toBeTrue();

    resolveResult.next({
      ...overdueAction,
      nextResolutionAtUtc: new Date(now + 10_000),
      hasMoreDueWork: false,
      revision: 'caught-up-combat-revision',
    });
    resolveResult.complete();

    expect(service.resolvingOfflineProgress()).toBeFalse();
  });
});

describe('isOfflineCombatCatchUpRequest', () => {
  it('detects a combat backlog before the server response is available', () => {
    const now = Date.parse('2026-08-18T12:00:00Z');

    expect(
      isOfflineCombatCatchUpRequest(
        {
          ...combatAction(),
          nextResolutionAtUtc: new Date('2026-08-18T11:59:40Z'),
          resolutionIntervalMs: 10_000,
        },
        now,
      ),
    ).toBeTrue();
  });

  it('does not classify a routine single-encounter poll as offline catch-up', () => {
    const now = Date.parse('2026-08-18T12:00:00Z');

    expect(
      isOfflineCombatCatchUpRequest(
        {
          ...combatAction(),
          nextResolutionAtUtc: new Date('2026-08-18T12:00:00Z'),
          resolutionIntervalMs: 10_000,
        },
        now,
      ),
    ).toBeFalse();
  });
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
