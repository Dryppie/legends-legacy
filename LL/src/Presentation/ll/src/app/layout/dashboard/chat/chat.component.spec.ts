import { signal } from '@angular/core';
import {
  ChatComponent,
  fallbackFromUnavailableGuildChannel,
  fallbackFromUnavailableRaidChannel,
  getChatSendErrorMessage,
  getWireErrorMessage,
  isInlineGuildSystemMessage,
  isWorldSystemMessage,
  parseWireCommand,
  splitCurrentPlayerMentions,
  startsNewChatDay,
} from './chat.component';
import {
  ChatChannelType,
  ChatMessageDto,
} from '../../../core/services/ll-chat/chat-service/chat.service';

describe('ChatComponent message submission', () => {
  it('ignores repeated sends while the current message is still in flight', async () => {
    let completeSend!: () => void;
    const pendingSend = new Promise<void>((resolve) => {
      completeSend = resolve;
    });
    const sendPublic = jasmine
      .createSpy('sendPublic')
      .and.returnValue(pendingSend);
    const component = Object.assign(Object.create(ChatComponent.prototype), {
      userInfoLoaded: true,
      userInfo: { isRegisteredUser: true },
      draft: 'Hello world',
      sendError: '',
      isSending: false,
      activeChannel: {
        type: ChatChannelType.General,
        contextKey: 'general',
      },
      chat: { sendPublic },
    }) as ChatComponent;

    const firstSend = component.send();
    const repeatedSend = component.send();

    expect(component.isSending).toBeTrue();
    expect(sendPublic).toHaveBeenCalledOnceWith(
      ChatChannelType.General,
      'general',
      'Hello world',
    );

    completeSend();
    await Promise.all([firstSend, repeatedSend]);

    expect(component.isSending).toBeFalse();
    expect(component.draft).toBe('');
  });
});

describe('fallbackFromUnavailableGuildChannel', () => {
  it('selects All when guild membership disappears from the Guild channel', () => {
    expect(
      fallbackFromUnavailableGuildChannel(
        { type: ChatChannelType.Guild, contextKey: 'guild' },
        false,
      ),
    ).toEqual({ type: ChatChannelType.General, contextKey: 'all' });
  });

  it('keeps other channel selections unchanged without a guild', () => {
    const activeChannel = {
      type: ChatChannelType.Trade,
      contextKey: 'trade',
    };

    expect(fallbackFromUnavailableGuildChannel(activeChannel, false)).toBe(
      activeChannel,
    );
  });
});

describe('fallbackFromUnavailableRaidChannel', () => {
  it('selects All when the raid channel closes or changes', () => {
    expect(
      fallbackFromUnavailableRaidChannel(
        { type: ChatChannelType.Raid, contextKey: 'raid-1' },
        null,
      ),
    ).toEqual({ type: ChatChannelType.General, contextKey: 'all' });
  });

  it('keeps the active channel for the same raid run', () => {
    const active = { type: ChatChannelType.Raid, contextKey: 'raid-1' };
    expect(fallbackFromUnavailableRaidChannel(active, 'raid-1')).toBe(active);
  });
});

describe('ChatComponent raid room identity', () => {
  it('keeps a stable tracking key when the derived Raid room is recreated', () => {
    const component = Object.assign(Object.create(ChatComponent.prototype), {
      guild: signal(null),
      raidId: signal('raid-1'),
      availableRooms: [
        {
          label: 'All',
          contextKey: 'all',
          channelType: ChatChannelType.General,
        },
      ],
    }) as ChatComponent;

    const firstRaidRoom = component.visibleRooms.find(
      (room) => room.channelType === ChatChannelType.Raid,
    )!;
    const secondRaidRoom = component.visibleRooms.find(
      (room) => room.channelType === ChatChannelType.Raid,
    )!;

    expect(firstRaidRoom).not.toBe(secondRaidRoom);
    expect(component.trackRoom(0, firstRaidRoom)).toBe(
      component.trackRoom(0, secondRaidRoom),
    );
  });
});

describe('isInlineGuildSystemMessage', () => {
  it('recognizes explicitly flagged guild notices', () => {
    expect(
      isInlineGuildSystemMessage({
        channelType: ChatChannelType.Guild,
        body: 'A guild notice.',
        isSystemGenerated: true,
      }),
    ).toBeTrue();
  });

  it('recognizes building target notices created before the flag existed', () => {
    expect(
      isInlineGuildSystemMessage({
        channelType: ChatChannelType.Guild,
        body: 'set the current building target to Guild Hall level 2.',
      }),
    ).toBeTrue();
  });
});

describe('isWorldSystemMessage', () => {
  const systemMessage: ChatMessageDto = {
    id: 'message-id',
    channelType: ChatChannelType.System,
    contextKey: 'system',
    senderId: '00000000-0000-0000-0000-000000000000',
    senderName: 'System',
    body: 'Achievement unlocked.',
    sentAt: new Date(),
  };

  it('recognizes world announcements by their system sender', () => {
    expect(
      isWorldSystemMessage({ ...systemMessage, senderName: 'World' }),
    ).toBeTrue();
  });

  it('keeps personal system messages separate', () => {
    expect(isWorldSystemMessage(systemMessage)).toBeFalse();
  });
});

describe('getChatSendErrorMessage', () => {
  it('replaces transport failures with an actionable availability message', () => {
    const result = getChatSendErrorMessage(
      new TypeError(
        'Failed to complete negotiation with the server: TypeError: Failed to fetch',
      ),
    );

    expect(result).toBe(
      'Chat is temporarily unavailable. Check your connection and try again.',
    );
  });

  it('preserves useful account guidance', () => {
    const result = getChatSendErrorMessage(
      new Error('Register your account before writing in chat.'),
    );

    expect(result).toBe('Register your account before writing in chat.');
  });

  it('does not expose unknown technical errors', () => {
    const result = getChatSendErrorMessage(
      new Error('Internal transport implementation detail'),
    );

    expect(result).toBe("Your message couldn't be sent. Please try again.");
  });
});

describe('parseWireCommand', () => {
  it('parses a valid Cinders wire case-insensitively', () => {
    expect(parseWireCommand('/WIRE Ember 250 cInDeRs')).toEqual({
      isWire: true,
      command: { recipientName: 'Ember', amount: 250 },
    });
  });

  it('supports player names containing spaces', () => {
    expect(parseWireCommand('/wire Ember Knight 250 Cinders')).toEqual({
      isWire: true,
      command: { recipientName: 'Ember Knight', amount: 250 },
    });
  });

  it('rejects malformed and non-positive wire commands', () => {
    expect(parseWireCommand('/wire Ember Cinders')).toEqual({
      isWire: true,
      command: null,
    });
    expect(parseWireCommand('/wire Ember 0 Cinders')).toEqual({
      isWire: true,
      command: null,
    });
  });

  it('does not intercept ordinary chat messages', () => {
    expect(parseWireCommand('Selling ore')).toEqual({ isWire: false });
  });
});

describe('splitCurrentPlayerMentions', () => {
  it('marks an exact mention of the current player case-insensitively', () => {
    expect(
      splitCurrentPlayerMentions('Hey @ember knight, ready?', 'Ember Knight'),
    ).toEqual([
      { text: 'Hey ', isCurrentPlayerMention: false },
      { text: '@ember knight', isCurrentPlayerMention: true },
      { text: ', ready?', isCurrentPlayerMention: false },
    ]);
  });

  it('does not mark a mention intended for another player', () => {
    expect(splitCurrentPlayerMentions('Hey @Ember', 'Ash')).toEqual([
      { text: 'Hey @Ember', isCurrentPlayerMention: false },
    ]);
  });

  it('does not treat a longer name or an email fragment as a mention', () => {
    expect(
      splitCurrentPlayerMentions('@EmberKnight ember@Ember.test', 'Ember'),
    ).toEqual([
      {
        text: '@EmberKnight ember@Ember.test',
        isCurrentPlayerMention: false,
      },
    ]);
  });

  it('marks every exact mention in the same message', () => {
    expect(splitCurrentPlayerMentions('@Ember and @Ember!', 'Ember')).toEqual([
      { text: '@Ember', isCurrentPlayerMention: true },
      { text: ' and ', isCurrentPlayerMention: false },
      { text: '@Ember', isCurrentPlayerMention: true },
      { text: '!', isCurrentPlayerMention: false },
    ]);
  });
});

describe('getWireErrorMessage', () => {
  it('shows an insufficient balance error without exposing transport details', () => {
    expect(
      getWireErrorMessage({
        errorMessage: 'You do not have enough Cinders for this wire.',
      }),
    ).toBe('You do not have enough Cinders for this wire.');
  });
});

describe('startsNewChatDay', () => {
  const messages: ChatMessageDto[] = [
    chatMessageAt(new Date(2026, 7, 10, 20, 55)),
    chatMessageAt(new Date(2026, 7, 10, 21, 10)),
    chatMessageAt(new Date(2026, 7, 11, 11, 10)),
  ];

  it('starts the list and each local calendar day with a separator', () => {
    expect(startsNewChatDay(messages, 0)).toBeTrue();
    expect(startsNewChatDay(messages, 1)).toBeFalse();
    expect(startsNewChatDay(messages, 2)).toBeTrue();
  });
});

function chatMessageAt(sentAt: Date): ChatMessageDto {
  return {
    id: sentAt.toISOString(),
    channelType: ChatChannelType.General,
    contextKey: 'general',
    senderId: 'sender-id',
    senderName: 'Sender',
    body: 'Message',
    sentAt,
  };
}
