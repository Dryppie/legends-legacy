import { signal } from '@angular/core';
import { fakeAsync, flushMicrotasks, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthService } from '../api/auth/auth.service';
import { GameBootstrapStateService } from '../api/game-bootstrap/game-bootstrap-state.service';
import { LootHistoryStateService } from '../api/loot-history/loot-history-state.service';
import { GameRealtimeConnection } from './game-realtime/game-realtime-connection.service';
import { GameRealtimeEventRegistry } from './game-realtime/game-realtime-event-registry.service';
import { StateSyncCoordinator } from './game-realtime/state-sync-coordinator.service';
import { RealTimeFacade } from './real-time-facade';

describe('RealTimeFacade', () => {
  it('enables periodic checkpoints only for an authenticated realtime connection', fakeAsync(() => {
    const isAuthenticated = signal(true);
    const connectionStatus = signal<
      'disconnected' | 'connecting' | 'connected' | 'reconnecting'
    >('connecting');
    const stateSync = jasmine.createSpyObj<StateSyncCoordinator>(
      'StateSyncCoordinator',
      [
        'initialize',
        'dispose',
        'reconcile',
        'setPeriodicReconciliationEnabled',
      ],
    );
    stateSync.reconcile.and.returnValue(Promise.resolve());

    TestBed.configureTestingModule({
      providers: [
        RealTimeFacade,
        { provide: AuthService, useValue: { isAuthenticated } },
        {
          provide: GameRealtimeConnection,
          useValue: {
            connectionStatus,
            reconnectCount: signal(0),
            subscribeToWorld: () => Promise.resolve(),
            disconnect: () => Promise.resolve(),
            isConnected: () => connectionStatus() === 'connected',
          },
        },
        {
          provide: GameRealtimeEventRegistry,
          useValue: jasmine.createSpyObj('GameRealtimeEventRegistry', [
            'initialize',
            'dispose',
          ]),
        },
        { provide: StateSyncCoordinator, useValue: stateSync },
        {
          provide: GameBootstrapStateService,
          useValue: { load: () => of(undefined), reload: () => of(undefined) },
        },
        {
          provide: LootHistoryStateService,
          useValue: { initialize: jasmine.createSpy() },
        },
      ],
    });

    const facade = TestBed.inject(RealTimeFacade);
    void facade.initialize();
    TestBed.flushEffects();
    expect(
      stateSync.setPeriodicReconciliationEnabled.calls.mostRecent().args[0],
    ).toBeFalse();

    connectionStatus.set('connected');
    TestBed.flushEffects();
    expect(
      stateSync.setPeriodicReconciliationEnabled.calls.mostRecent().args[0],
    ).toBeTrue();

    connectionStatus.set('reconnecting');
    TestBed.flushEffects();
    expect(
      stateSync.setPeriodicReconciliationEnabled.calls.mostRecent().args[0],
    ).toBeFalse();

    connectionStatus.set('connected');
    isAuthenticated.set(false);
    TestBed.flushEffects();
    expect(
      stateSync.setPeriodicReconciliationEnabled.calls.mostRecent().args[0],
    ).toBeFalse();

    flushMicrotasks();
  }));
});
