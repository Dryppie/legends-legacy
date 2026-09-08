import { signal } from '@angular/core';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { CombatService } from '../../client-side/combat/combat.service';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { GameRealtimeStore } from '../../real-time/game-realtime/game-realtime-store.service';
import { CharacterStateService } from '../character/character-state.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { DungeonStateService } from './dungeon-state.service';
import {
  DungeonActionOutcome,
  DungeonRun,
  DungeonRunStatus,
  DungeonService,
} from './dungeon.service';
import { CombatSessionDto } from '../../../../shared/models/Dtos/combatResultDto';
import { DungeonHubData } from '../../../../shared/models/Dtos/dungeons/dungeonPreviewData';

describe('DungeonStateService dungeon actions', () => {
  it('uses the Treasury opening action', () => {
    const state = Object.create(DungeonStateService.prototype) as DungeonStateService;
    spyOn(state, 'executeAction');
    state.openTreasury();
    expect(state.executeAction).toHaveBeenCalledOnceWith('open_treasury');
  });
  it('uses the canonical rest action at a Rest Site', () => {
    const state = Object.create(
      DungeonStateService.prototype,
    ) as DungeonStateService;
    spyOn(state, 'executeAction');

    state.restAtSite();

    expect(state.executeAction).toHaveBeenCalledOnceWith('rest');
  });
});

describe('DungeonStateService synchronization', () => {
  let dungeonService: jasmine.SpyObj<DungeonService>;
  let stateSync: StateSyncCoordinator;

  beforeEach(() => {
    dungeonService = jasmine.createSpyObj<DungeonService>('DungeonService', [
      'getActiveDungeon',
      'getAvailableDungeons',
      'claimDungeonRewards',
      'executeDungeonAction',
    ]);
    dungeonService.getActiveDungeon.and.returnValue(of(null));
    dungeonService.getAvailableDungeons.and.returnValue(
      of({
        sigilFragments: 0,
        sigilAssemblyEnabled: false,
        sigilAssemblyCost: 0,
        dungeons: [],
      }),
    );

    TestBed.configureTestingModule({
      providers: [
        DungeonStateService,
        { provide: DungeonService, useValue: dungeonService },
        {
          provide: CombatService,
          useValue: jasmine.createSpyObj<CombatService>('CombatService', [
            'startDungeonCombatSimulation',
          ]),
        },
        {
          provide: GameRealtimeEventRegistry,
          useValue: { reconnectCount: signal(0) },
        },
        {
          provide: InventoryStateService,
          useValue: {
            items: jasmine.createSpy('items'),
            applyVersionedInventory: jasmine.createSpy(),
          },
        },
        {
          provide: CharacterStateService,
          useValue: { applyVersionedCharacter: jasmine.createSpy() },
        },
        { provide: ToastService, useValue: {} },
        {
          provide: GameRealtimeStore,
          useValue: { addLoot: jasmine.createSpy('addLoot') },
        },
        StateSyncCoordinator,
      ],
    });
    stateSync = TestBed.inject(StateSyncCoordinator);
    spyOn(stateSync, 'register').and.callThrough();
  });

  afterEach(() => stateSync.dispose());

  it('does not couple general inventory invalidations to dungeon availability', () => {
    TestBed.inject(DungeonStateService);
    expect(stateSync.register).not.toHaveBeenCalledWith(
      'inventory',
      'dungeons-inventory',
      jasmine.any(Function),
    );
  });

  function dungeonHub(
    ownedAmount: number,
    canEnter = ownedAmount > 0,
  ): DungeonHubData {
    return {
      sigilFragments: 0,
      sigilAssemblyEnabled: false,
      sigilAssemblyCost: 0,
      dungeons: [
        {
          id: 'goblin_mines',
          region: 1,
          number: 1,
          title: 'Goblin Mines',
          lore: '',
          rewards: [],
          unlockedDifficulties: [],
          sigilItemId: 'sigil_goblin_mines',
          canEnter,
          entryRequirements: [
            {
              itemId: 'sigil_goblin_mines',
              name: 'Goblin Sigil',
              requiredAmount: 1,
              ownedAmount,
            },
          ],
        },
      ],
    };
  }

  it('applies a Sigil invalidation before dungeon metadata has loaded and ignores the older response', fakeAsync(() => {
    const initialHub = new Subject<DungeonHubData>();
    dungeonService.getAvailableDungeons.and.returnValue(initialHub);
    const state = TestBed.inject(DungeonStateService);
    expect(state.dungeons()).toEqual([]);

    dungeonService.getAvailableDungeons.and.returnValue(of(dungeonHub(1)));
    stateSync.acceptInvalidation({
      scope: 'dungeons',
      revision: 1,
      reason: 'Sigil looted',
    });
    tick(51);
    initialHub.next(dungeonHub(0));
    initialHub.complete();

    expect(state.dungeons()[0].entryRequirements?.[0].ownedAmount).toBe(1);
    expect(state.dungeons()[0].canEnter).toBeTrue();
    expect(stateSync.status()[0].appliedRevision).toBe(1);
    expect(TestBed.inject(InventoryStateService).items).not.toHaveBeenCalled();
  }));

  it('catches up on Sigil invalidations received before the dungeon state service exists', fakeAsync(() => {
    stateSync.acceptInvalidation({
      scope: 'dungeons',
      revision: 2,
      reason: 'Sigil looted',
    });
    tick(51);
    const state = TestBed.inject(DungeonStateService);
    dungeonService.getAvailableDungeons.and.returnValue(of(dungeonHub(2)));
    tick(51);

    expect(state.dungeons()[0].entryRequirements?.[0].ownedAmount).toBe(2);
    expect(stateSync.status()[0].appliedRevision).toBe(2);
  }));

  it('retries a failed Sigil refresh and only acknowledges it after success', fakeAsync(() => {
    dungeonService.getAvailableDungeons.and.returnValue(of(dungeonHub(0)));
    const state = TestBed.inject(DungeonStateService);
    dungeonService.getAvailableDungeons.calls.reset();
    dungeonService.getAvailableDungeons.and.returnValues(
      throwError(() => new Error('offline')),
      of(dungeonHub(1, false)),
    );
    stateSync.acceptInvalidation({
      scope: 'dungeons',
      revision: 3,
      reason: 'Sigil looted',
    });
    tick(51);
    expect(stateSync.status()[0]).toEqual(
      jasmine.objectContaining({
        appliedRevision: 0,
        stale: true,
        retryAttempt: 1,
      }),
    );

    tick(1000);
    expect(state.dungeons()[0].entryRequirements?.[0].ownedAmount).toBe(1);
    // The refreshed server response still enforces other entry requirements.
    expect(state.dungeons()[0].canEnter).toBeFalse();
    expect(state.error()).toBeNull();
    expect(stateSync.status()[0]).toEqual(
      jasmine.objectContaining({
        appliedRevision: 3,
        stale: false,
        retryAttempt: 0,
      }),
    );
    expect(dungeonService.getAvailableDungeons).toHaveBeenCalledTimes(2);
  }));

  it('coalesces dungeon revisions and ignores unrelated inventory invalidations', fakeAsync(() => {
    dungeonService.getAvailableDungeons.and.returnValue(of(dungeonHub(2)));
    const state = TestBed.inject(DungeonStateService);
    dungeonService.getAvailableDungeons.calls.reset();
    stateSync.acceptInvalidation({
      scope: 'inventory',
      revision: 1,
      reason: 'Ordinary loot',
    });
    tick(51);
    expect(dungeonService.getAvailableDungeons).not.toHaveBeenCalled();

    dungeonService.getAvailableDungeons.and.returnValue(of(dungeonHub(0)));
    stateSync.acceptInvalidation({
      scope: 'dungeons',
      revision: 1,
      reason: 'Sigil consumed',
    });
    stateSync.acceptInvalidation({
      scope: 'dungeons',
      revision: 2,
      reason: 'Sigil consumed',
    });
    stateSync.acceptInvalidation({
      scope: 'dungeons',
      revision: 2,
      reason: 'Duplicate',
    });
    tick(51);
    expect(dungeonService.getAvailableDungeons).toHaveBeenCalledOnceWith();
    expect(state.dungeons()[0].entryRequirements?.[0].ownedAmount).toBe(0);
    expect(state.dungeons()[0].canEnter).toBeFalse();
    expect(stateSync.status()[0].appliedRevision).toBe(2);
  }));

  for (const status of [
    DungeonRunStatus.Active,
    DungeonRunStatus.Completed,
    DungeonRunStatus.Failed,
  ]) {
    it(`applies ${status} battle results without opening combat when instant-skip is on`, () => {
      const state = TestBed.inject(DungeonStateService);
      const run = { id: 'run-1', status } as DungeonRun;
      const combatSession = { combatResult: {} } as CombatSessionDto;
      dungeonService.executeDungeonAction.and.returnValue(
        of({
          data: {
            run,
            hub: {
              sigilFragments: 9,
              sigilAssemblyEnabled: true,
              sigilAssemblyCost: 3,
              dungeons: [],
            },
            outcome: DungeonActionOutcome.CombatVictory,
            combatSession,
            message: 'Battle resolved',
          },
          domainVersions: { dungeons: 2 },
        }),
      );
      state.setActiveDungeon({ id: 'run-1' } as DungeonRun);
      expect(state.instantSkipBattles()).toBeFalse();
      state.toggleInstantSkipBattles();

      state.fight();

      expect(dungeonService.executeDungeonAction).toHaveBeenCalledOnceWith(
        'run-1',
        { actionId: 'fight', payload: undefined },
      );
      expect(state.activeDungeon()).toBe(run);
      expect(state.sigilFragments()).toBe(9);
      expect(state.combatSession()).toBe(combatSession);
      expect(state.message()).toBe('Battle resolved');
      expect(state.loading()).toBeFalse();
      const combat = TestBed.inject(CombatService);
      expect(combat.startDungeonCombatSimulation).not.toHaveBeenCalled();

      state.toggleInstantSkipBattles();
      state.fight();

      expect(combat.startDungeonCombatSimulation).toHaveBeenCalledOnceWith(
        combatSession.combatResult,
      );
    });
  }

  it('applies the versioned claim hub without a follow-up availability GET', () => {
    dungeonService.claimDungeonRewards.and.returnValue(
      of({
        data: {
          activeRun: null,
          hub: {
            sigilFragments: 9,
            sigilAssemblyEnabled: true,
            sigilAssemblyCost: 3,
            dungeons: [],
          },
          inventoryItems: [],
          claimedLoot: [],
          location: 'Goblin Mines',
          character: {} as never,
        },
        domainVersions: { dungeons: 2, inventory: 2, character: 2 },
      }),
    );
    const state = TestBed.inject(DungeonStateService);
    dungeonService.getAvailableDungeons.calls.reset();

    state.claimDungeonRewards();

    expect(state.sigilFragments()).toBe(9);
    expect(dungeonService.getAvailableDungeons).not.toHaveBeenCalled();
    expect(
      TestBed.inject(InventoryStateService).applyVersionedInventory,
    ).toHaveBeenCalled();
    expect(
      TestBed.inject(CharacterStateService).applyVersionedCharacter,
    ).toHaveBeenCalled();
    expect(TestBed.inject(GameRealtimeStore).addLoot).toHaveBeenCalledOnceWith(
      [],
      undefined,
      'dungeon-reward',
      'Goblin Mines',
    );
  });

  it('does not apply a claim hub older than the observed dungeon version', () => {
    TestBed.inject(DomainVersionTracker).observe({ dungeons: 3 });
    dungeonService.claimDungeonRewards.and.returnValue(
      of({
        data: {
          activeRun: null,
          hub: {
            sigilFragments: 9,
            sigilAssemblyEnabled: true,
            sigilAssemblyCost: 3,
            dungeons: [],
          },
          inventoryItems: [],
          claimedLoot: [],
          location: 'Goblin Mines',
          character: {} as never,
        },
        domainVersions: { dungeons: 2, inventory: 4, character: 4 },
      }),
    );
    const state = TestBed.inject(DungeonStateService);

    state.claimDungeonRewards();

    expect(state.sigilFragments()).toBe(0);
  });
});
