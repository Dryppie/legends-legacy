import {
  fakeAsync,
  flushMicrotasks,
  TestBed,
  tick,
} from '@angular/core/testing';
import { HubConnectionState } from '@microsoft/signalr';
import { AuthService } from '../../api/auth/auth.service';
import { GameRealtimeConnection } from './game-realtime-connection.service';
import { GameRealtimeDiagnostics } from './game-realtime-diagnostics.service';

describe('GameRealtimeConnection', () => {
  let service: GameRealtimeConnection;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        GameRealtimeConnection,
        {
          provide: AuthService,
          useValue: {
            getAccessToken: () => 'token',
            isAuthenticated: () => true,
          },
        },
        {
          provide: GameRealtimeDiagnostics,
          useValue: jasmine.createSpyObj('GameRealtimeDiagnostics', [
            'start',
            'recordReceive',
          ]),
        },
      ],
    });

    service = TestBed.inject(GameRealtimeConnection);
  });

  it('announces a reconnect only after all desired audiences are restored', fakeAsync(() => {
    const invoke = jasmine
      .createSpy('invoke')
      .and.returnValues(
        Promise.reject(new Error('transient subscription failure')),
        Promise.resolve(),
      );
    const internals = service as unknown as {
      hub: { state: HubConnectionState; invoke: typeof invoke };
      retryConnections: boolean;
      worldSubscriptionRequested: boolean;
      completeAutomaticReconnect(generation: number): Promise<void>;
    };
    internals.hub = { state: HubConnectionState.Connected, invoke };
    internals.retryConnections = true;
    internals.worldSubscriptionRequested = true;

    void internals.completeAutomaticReconnect(0);
    flushMicrotasks();

    expect(service.connectionStatus()).toBe('disconnected');
    expect(service.reconnectCount()).toBe(0);
    expect(invoke).toHaveBeenCalledTimes(1);

    tick(1_000);
    flushMicrotasks();

    expect(invoke).toHaveBeenCalledTimes(2);
    expect(service.connectionStatus()).toBe('connected');
    expect(service.reconnectCount()).toBe(1);
  }));

  it('drops a forbidden raid audience without blocking other subscriptions', async () => {
    const invoke = jasmine
      .createSpy('invoke')
      .and.callFake((method: string) =>
        method === 'SubscribeToRaid'
          ? Promise.reject(new Error('Forbidden - not a member of that raid.'))
          : Promise.resolve(),
      );
    const internals = service as unknown as {
      hub: { state: HubConnectionState; invoke: typeof invoke };
      guildSubscriptions: Set<string>;
      activeGuildSubscriptions: Set<string>;
      raidSubscriptions: Set<string>;
      resubscribeAudiences(): Promise<void>;
    };
    internals.hub = { state: HubConnectionState.Connected, invoke };
    internals.guildSubscriptions.add('guild-1');
    internals.raidSubscriptions.add('raid-1');

    await internals.resubscribeAudiences();

    expect(internals.activeGuildSubscriptions.has('guild-1')).toBeTrue();
    expect(internals.raidSubscriptions.has('raid-1')).toBeFalse();
  });
});
