import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { CombatService } from '../../client-side/combat/combat.service';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { GameRealtimeStore } from '../../real-time/game-realtime/game-realtime-store.service';
import { CharacterStateService } from '../character/character-state.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { DungeonStateService } from './dungeon-state.service';
import { DungeonService } from './dungeon.service';

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

  beforeEach(() => {
    dungeonService = jasmine.createSpyObj<DungeonService>('DungeonService', [
      'getActiveDungeon',
      'getAvailableDungeons',
      'claimDungeonRewards',
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
        { provide: CombatService, useValue: {} },
        {
          provide: GameRealtimeEventRegistry,
          useValue: { reconnectCount: signal(0) },
        },
        {
          provide: InventoryStateService,
          useValue: { applyVersionedInventory: jasmine.createSpy() },
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
