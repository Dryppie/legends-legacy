import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';
import { LootHistoryEntry } from '../../../../shared/models/loot-history';
import { GameRealtimeStore } from '../../real-time/game-realtime/game-realtime-store.service';
import {
  StateSyncCoordinator,
  StateSyncRefresh,
} from '../../real-time/game-realtime/state-sync-coordinator.service';
import { LootHistoryStateService } from './loot-history-state.service';
import { LootHistoryService } from './loot-history.service';

describe('LootHistoryStateService', () => {
  it('registers an authoritative reload for loot-history revisions', async () => {
    const entries = [{ id: 'history-entry' }] as LootHistoryEntry[];
    const api = jasmine.createSpyObj<LootHistoryService>('LootHistoryService', [
      'getRecent',
      'clear',
    ]);
    api.getRecent.and.returnValue(of(entries));
    const store = jasmine.createSpyObj<GameRealtimeStore>('GameRealtimeStore', [
      'setLootHistory',
      'clearLootHistory',
    ]);
    let refresh: StateSyncRefresh | undefined;
    const stateSync = jasmine.createSpyObj<StateSyncCoordinator>(
      'StateSyncCoordinator',
      ['register'],
    );
    stateSync.register.and.callFake((_scope, _key, registeredRefresh) => {
      refresh = registeredRefresh;
      return () => undefined;
    });

    TestBed.configureTestingModule({
      providers: [
        LootHistoryStateService,
        { provide: LootHistoryService, useValue: api },
        { provide: GameRealtimeStore, useValue: store },
        { provide: StateSyncCoordinator, useValue: stateSync },
      ],
    });

    const state = TestBed.inject(LootHistoryStateService);
    state.initialize();
    state.initialize();

    expect(stateSync.register).toHaveBeenCalledOnceWith(
      'loot-history',
      'loot-history',
      jasmine.any(Function),
      jasmine.any(Function),
      false,
    );

    await firstValueFrom(
      refresh!({
        scope: 'loot-history',
        key: 'loot-history',
        targetRevision: 2,
      }) as ReturnType<LootHistoryStateService['reload']>,
    );

    expect(api.getRecent).toHaveBeenCalledTimes(1);
    expect(store.setLootHistory).toHaveBeenCalledOnceWith(entries);
  });

  it('clears the retained store only after the server succeeds', async () => {
    const api = jasmine.createSpyObj<LootHistoryService>('LootHistoryService', [
      'getRecent',
      'clear',
    ]);
    api.clear.and.returnValue(of(3));
    const store = jasmine.createSpyObj<GameRealtimeStore>('GameRealtimeStore', [
      'setLootHistory',
      'clearLootHistory',
    ]);

    TestBed.configureTestingModule({
      providers: [
        LootHistoryStateService,
        { provide: LootHistoryService, useValue: api },
        { provide: GameRealtimeStore, useValue: store },
        {
          provide: StateSyncCoordinator,
          useValue: jasmine.createSpyObj('StateSyncCoordinator', ['register']),
        },
      ],
    });

    const state = TestBed.inject(LootHistoryStateService);
    expect(await firstValueFrom(state.clear())).toBe(3);
    expect(store.clearLootHistory).toHaveBeenCalledTimes(1);
  });
});
