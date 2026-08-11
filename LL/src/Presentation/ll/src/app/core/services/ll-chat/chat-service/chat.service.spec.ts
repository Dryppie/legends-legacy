import {
  ChatChannelType,
  ChatMessageDto,
  mergeChatMessagesChronologically,
} from './chat.service';

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
