import { signal, WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
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
import { InventoryItem } from '../../../../shared/models/inventoryItem';

describe('DungeonStateService dungeon actions', () => {
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
  let stateSync: jasmine.SpyObj<StateSyncCoordinator>;
  let inventoryItems: WritableSignal<InventoryItem[]>;

  beforeEach(() => {
    inventoryItems = signal<InventoryItem[]>([]);
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
    stateSync = jasmine.createSpyObj<StateSyncCoordinator>(
      'StateSyncCoordinator',
      ['register'],
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
            items: inventoryItems.asReadonly(),
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
        { provide: StateSyncCoordinator, useValue: stateSync },
      ],
    });
  });

  it('does not couple general inventory invalidations to dungeon availability', () => {
    TestBed.inject(DungeonStateService);
    expect(stateSync.register).not.toHaveBeenCalledWith(
      'inventory',
      'dungeons-inventory',
      jasmine.any(Function),
    );
  });

  function sigilStack(quantity: number): InventoryItem {
    return {
      id: 'sigil-stack',
      quantity,
      itemInstance: {
        id: 'sigil-instance',
        itemBase: { id: 'sigil_goblin_mines' },
      },
    } as InventoryItem;
  }

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

  it('updates Sigil counts and entry availability after loot and removal without opening a dungeon', () => {
    dungeonService.getAvailableDungeons.and.returnValue(of(dungeonHub(0)));
    const state = TestBed.inject(DungeonStateService);
    TestBed.flushEffects();
    dungeonService.getAvailableDungeons.calls.reset();
    dungeonService.getActiveDungeon.calls.reset();

    dungeonService.getAvailableDungeons.and.returnValue(of(dungeonHub(1)));
    inventoryItems.set([sigilStack(1)]);
    TestBed.flushEffects();

    expect(state.dungeons()[0].entryRequirements?.[0].ownedAmount).toBe(1);
    expect(state.dungeons()[0].canEnter).toBeTrue();
    expect(dungeonService.getAvailableDungeons).toHaveBeenCalledTimes(1);
    expect(dungeonService.getActiveDungeon).not.toHaveBeenCalled();

    dungeonService.getAvailableDungeons.and.returnValue(of(dungeonHub(0)));
    inventoryItems.set([]);
    TestBed.flushEffects();

    expect(state.dungeons()[0].entryRequirements?.[0].ownedAmount).toBe(0);
    expect(state.dungeons()[0].canEnter).toBeFalse();
    expect(dungeonService.getAvailableDungeons).toHaveBeenCalledTimes(2);
  });

  it('ignores unrelated loot, unchanged totals, and entry counts already applied by a mutation', () => {
    inventoryItems.set([sigilStack(2)]);
    dungeonService.getAvailableDungeons.and.returnValue(of(dungeonHub(2)));
    const state = TestBed.inject(DungeonStateService);
    TestBed.flushEffects();
    dungeonService.getAvailableDungeons.calls.reset();

    const resource = {
      ...sigilStack(10),
      itemInstance: {
        id: 'resource-instance',
        itemBase: { id: 'iron_ore' },
      },
    } as InventoryItem;
    inventoryItems.set([
      sigilStack(1),
      { ...sigilStack(1), id: 'second-stack' },
      resource,
    ]);
    TestBed.flushEffects();
    expect(dungeonService.getAvailableDungeons).not.toHaveBeenCalled();

    state.setDungeons(dungeonHub(1).dungeons);
    inventoryItems.set([sigilStack(1), resource]);
    TestBed.flushEffects();
    expect(dungeonService.getAvailableDungeons).not.toHaveBeenCalled();
  });

  it('keeps the latest Sigil count and server entry restrictions when an older map refresh finishes late', () => {
    dungeonService.getAvailableDungeons.and.returnValue(of(dungeonHub(0)));
    const state = TestBed.inject(DungeonStateService);
    TestBed.flushEffects();
    const oldRefresh = new Subject<DungeonHubData>();
    dungeonService.getAvailableDungeons.and.returnValue(oldRefresh);
    state.loadAvailableDungeons();

    // Owning a Sigil does not bypass the server's other entry requirements.
    dungeonService.getAvailableDungeons.and.returnValue(
      of(dungeonHub(1, false)),
    );
    inventoryItems.set([sigilStack(1)]);
    TestBed.flushEffects();
    oldRefresh.next(dungeonHub(0));
    oldRefresh.complete();

    expect(state.dungeons()[0].entryRequirements?.[0].ownedAmount).toBe(1);
    expect(state.dungeons()[0].canEnter).toBeFalse();
  });

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
