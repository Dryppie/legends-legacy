import {
  ChatChannelType,
  ChatMessageDto,
  ChatService,
  mergeChatMessagesChronologically,
} from './chat.service';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Subject, of } from 'rxjs';
import { ChatApiService } from '../chat-api.service';
import { CharacterService } from '../../api/character/character.service';
import { GuildStateService } from '../../api/guild/guild-state.service';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { AuthService } from '../../api/auth/auth.service';
import { RaidService } from '../../api/raid/raid.service';

describe('mergeChatMessagesChronologically', () => {
  it('places history received after a live message into chronological order', () => {
    const liveMessage = messageAt('live', '2026-08-11T12:00:00Z');
    const history = [
      messageAt('oldest', '2026-08-11T10:00:00Z'),
      messageAt('middle', '2026-08-11T11:00:00Z'),
    ];

    const result = mergeChatMessagesChronologically([liveMessage], history);

    expect(result.map((message) => message.id)).toEqual([
      'oldest',
      'middle',
      'live',
    ]);
  });

  it('deduplicates messages and orders equal timestamps deterministically', () => {
    const duplicate = messageAt('b', '2026-08-11T10:00:00Z');

    const result = mergeChatMessagesChronologically(
      [duplicate],
      [messageAt('a', '2026-08-11T10:00:00Z'), duplicate],
    );

    expect(result.map((message) => message.id)).toEqual(['a', 'b']);
  });
});

describe('ChatService guild connection synchronization', () => {
  let service: ChatService;
  let auth: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthService>(
      'AuthService',
      ['refreshSession', 'ensureValidToken', 'getAccessToken'],
      {
        identity: signal<string | null>(null),
        authenticationContextVersion: signal(0),
        currentCharacter: signal(null),
      },
    );
    auth.ensureValidToken.and.returnValue(of(1));
    auth.getAccessToken.and.returnValue('access-token');

    const transientEvents = new Subject<any>();
    const gameEvents = {
      eventEnvelope$: () => transientEvents.asObservable(),
    };

    TestBed.configureTestingModule({
      providers: [
        ChatService,
        { provide: ChatApiService, useValue: { get: () => of([]) } },
        { provide: CharacterService, useValue: {} },
        {
          provide: GuildStateService,
          useValue: { guild: signal(null) },
        },
        { provide: GameRealtimeEventRegistry, useValue: gameEvents },
        {
          provide: RaidService,
          useValue: {
            activeRaidId: signal(null),
            activeRaidChatId: signal(null),
            getActiveRaid: () => of(null),
            clearActiveRaid: () => undefined,
          },
        },
        { provide: AuthService, useValue: auth },
      ],
    });

    service = TestBed.inject(ChatService);
  });

  it('waits for a fresh guild claim before reconnecting to guild chat', async () => {
    const refreshCompleted = new Subject<number>();
    const operations: string[] = [];
    auth.refreshSession.and.returnValue(refreshCompleted);
    spyOn<any>(service, 'stopHubConnection').and.callFake(async () => {
      operations.push('stop');
    });
    spyOn(service, 'connectAndLoad').and.callFake(async () => {
      operations.push('connect');
    });

    const connection = (service as any).connectForContext(
      'guild-2',
      true,
      true,
      true,
      false,
    );
    await Promise.resolve();

    expect(operations).toEqual(['stop']);
    expect(auth.refreshSession).toHaveBeenCalledOnceWith();

    refreshCompleted.next(1);
    refreshCompleted.complete();
    await connection;

    expect(operations).toEqual(['stop', 'connect']);
  });

  it('does not refresh authentication for an unchanged guild context', async () => {
    spyOn(service, 'connectAndLoad').and.resolveTo();

    await (service as any).connectForContext(
      'guild-1',
      false,
      false,
      false,
      false,
    );

    expect(auth.refreshSession).not.toHaveBeenCalled();
  });

  it('coalesces queued refresh context changes before starting SignalR', async () => {
    let releaseQueue!: () => void;
    (service as any).connectionQueue = new Promise<void>((resolve) => {
      releaseQueue = resolve;
    });
    const connectForContext = spyOn<any>(
      service,
      'connectForContext',
    ).and.resolveTo();

    const initialContext = (service as any).enqueueConnectionForContext(
      undefined,
      false,
      false,
      false,
      false,
    );
    const hydratedContext = (service as any).enqueueConnectionForContext(
      'guild-1',
      false,
      true,
      true,
      false,
    );

    releaseQueue();
    await Promise.all([initialContext, hydratedContext]);

    expect(connectForContext).toHaveBeenCalledOnceWith(
      'guild-1',
      false,
      true,
      true,
      false,
      undefined,
    );
  });

  it('shares an in-flight SignalR start between concurrent callers', async () => {
    let finishStart!: () => void;
    const start = new Promise<void>((resolve) => {
      finishStart = resolve;
    });
    const startHubConnection = spyOn<any>(
      service,
      'startHubConnection',
    ).and.returnValue(start);

    const first = (service as any).buildHubConnection();
    const second = (service as any).buildHubConnection();

    expect(startHubConnection).toHaveBeenCalledTimes(1);
    finishStart();
    await Promise.all([first, second]);
  });

  it('wakes the connection effect after the unavailable delay', fakeAsync(() => {
    const initialTrigger = (service as any).connectionRetryTrigger();
    (service as any).unavailableUntil = Date.now() + 100;

    (service as any).scheduleConnectionRetry();
    tick(50);
    (service as any).unavailableUntil = Date.now() + 100;
    (service as any).scheduleConnectionRetry();
    tick(99);
    expect((service as any).connectionRetryTrigger()).toBe(initialTrigger);

    tick(1);
    expect((service as any).connectionRetryTrigger()).toBe(initialTrigger + 1);
  }));

  it('keeps non-raid messages visible while raid membership synchronizes', async () => {
    const generalMessage = messageAt('general', '2026-08-11T10:00:00Z');
    const previousRaidMessage = {
      ...messageAt('old-raid', '2026-08-11T10:01:00Z'),
      channelType: ChatChannelType.Raid,
      contextKey: 'raid-1',
    };
    (service as any).messageList.set([generalMessage, previousRaidMessage]);
    spyOn(service, 'connectAndLoad').and.rejectWith(
      new Error('Raid membership has not reached chat yet.'),
    );

    await expectAsync(
      (service as any).connectForContext(
        undefined,
        false,
        false,
        false,
        true,
        'raid-2',
      ),
    ).toBeRejected();

    expect((service as any).messageList()).toEqual([generalMessage]);
  });

  it('reloads raid history when the membership lifecycle message arrives', async () => {
    (service as any).activeRaidId = 'raid-1';
    const loadHistory = spyOn(service, 'loadHistory').and.resolveTo();

    (service as any).recoverActiveRaidHistory({
      ...messageAt('raid-opened', '2026-08-11T10:00:00Z'),
      channelType: ChatChannelType.Raid,
      contextKey: 'raid-1',
    });
    await (service as any).raidHistoryRecoveryPromise;

    expect(loadHistory).toHaveBeenCalledOnceWith(
      undefined,
      50,
      undefined,
      'raid-1',
    );
    expect((service as any).loadedRaidHistoryId).toBe('raid-1');
  });
});

function messageAt(id: string, sentAt: string): ChatMessageDto {
  return {
    id,
    channelType: ChatChannelType.General,
    contextKey: 'general',
    senderId: 'sender-id',
    senderName: 'Sender',
    body: 'Message',
    sentAt,
  };
}
