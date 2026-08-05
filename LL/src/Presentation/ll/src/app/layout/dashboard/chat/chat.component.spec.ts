import {
  getChatSendErrorMessage,
  isWorldSystemMessage,
} from './chat.component';
import {
  ChatChannelType,
  ChatMessageDto,
} from '../../../core/services/ll-chat/chat-service/chat.service';

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
