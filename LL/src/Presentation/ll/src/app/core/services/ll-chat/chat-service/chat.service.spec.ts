import {
  ChatChannelType,
  ChatMessageDto,
  ChatService,
  mergeChatMessagesChronologically,
} from './chat.service';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Subject, of } from 'rxjs';
import { ChatApiService } from '../chat-api.service';
import { CharacterService } from '../../api/character/character.service';
import { GuildStateService } from '../../api/guild/guild-state.service';
import { GameEventService } from '../../real-time/game-event.service';
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

    const emptyEnvelope = signal(undefined);
    const gameEvents = {
      eventEnvelope: {
        AchievementUnlockedMsg: emptyEnvelope,
        PlayerTransferMsg: emptyEnvelope,
        GuildVaultChatMessageMsg: emptyEnvelope,
      },
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
        { provide: GameEventService, useValue: gameEvents },
        {
          provide: RaidService,
          useValue: {
            activeRaidId: signal(null),
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

    await (service as any).connectForContext('guild-1', false, false, false);

    expect(auth.refreshSession).not.toHaveBeenCalled();
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
