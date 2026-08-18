import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';
import { CombatService } from '../../client-side/combat/combat.service';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { GameEventService } from '../../real-time/game-event.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
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
          provide: GameEventService,
          useValue: { reconnectCount: signal(0) },
        },
        { provide: InventoryStateService, useValue: {} },
        { provide: CharacterStateService, useValue: {} },
        { provide: ToastService, useValue: {} },
        { provide: StateSyncCoordinator, useValue: stateSync },
      ],
    });
  });

  it('refreshes dungeon availability when inventory is invalidated', () => {
    TestBed.inject(DungeonStateService);
    const registration = stateSync.register.calls
      .allArgs()
      .find(
        ([scope, key]) => scope === 'inventory' && key === 'dungeons-inventory',
      );

    expect(registration).toBeDefined();
    dungeonService.getAvailableDungeons.calls.reset();

    const refresh = registration?.[2] as () => Observable<unknown>;
    refresh().subscribe();

    expect(dungeonService.getAvailableDungeons).toHaveBeenCalledTimes(1);
  });
});
