import { ChatChannelType, ChatMessageDto } from '../../core/services/ll-chat/chat-service/chat.service';
import { LootHistoryEntry } from '../../shared/models/loot-history';
import {
  ChatLineContext,
  chronicleChannelId,
  chronicleChannelLabel,
  chronicleChannels,
  lootLine,
  toChronicleLine,
  withDayBreaks,
} from './chat-chronicle';

describe('chat-chronicle', () => {
  const msg = (over: Partial<ChatMessageDto>): ChatMessageDto => ({
    id: 'm1',
    channelType: ChatChannelType.General,
    contextKey: 'general',
    senderId: 'c2',
    senderName: 'Maren',
    body: 'Hello',
    sentAt: '2026-10-01T10:00:00',
    ...over,
  });
  const ctx: ChatLineContext = {
    characterId: 'c1',
    mentioned: (m) => m.body.includes('@Aldric'),
    noble: (id) => id === 'c2',
  };

  it('maps channels to the Chronicle ids', () => {
    expect(chronicleChannelId(ChatChannelType.General, 'general')).toBe('general');
    expect(chronicleChannelId(ChatChannelType.General, 'trade')).toBe('trade');
    expect(chronicleChannelId(ChatChannelType.General, 'help')).toBe('help');
    expect(chronicleChannelId(ChatChannelType.Guild, 'g1')).toBe('guild');
    expect(chronicleChannelId(ChatChannelType.Whisper, 'w')).toBe('whisper');
  });

  it('tags world messages as World', () => {
    expect(chronicleChannelLabel(msg({ channelType: ChatChannelType.System, senderName: 'World' }))).toBe('World');
    expect(chronicleChannelLabel(msg({ contextKey: 'trade' }))).toBe('Trade');
  });

  it('makes a chat line with author, Nobility and mention', () => {
    const line = toChronicleLine(msg({ body: 'hi @Aldric' }), ctx);
    expect(line).toEqual(
      jasmine.objectContaining({ id: 'm1', channel: 'general', kind: 'chat', author: 'Maren', noble: true, mention: true }),
    );
  });

  it('prefers the title display name', () => {
    expect(toChronicleLine(msg({ senderTitleDisplayName: 'Maren the Bold' }), ctx).author).toBe('Maren the Bold');
  });

  it('makes whispers from and to', () => {
    const from = toChronicleLine(msg({ channelType: ChatChannelType.Whisper, contextKey: 'w' }), ctx);
    expect(from).toEqual(jasmine.objectContaining({ direction: 'from', author: 'Maren', noble: true }));
    const to = toChronicleLine(
      msg({ channelType: ChatChannelType.Whisper, contextKey: 'w', senderId: 'c1', targetCharacterId: 'c3', targetCharacterName: 'Tamsin' }),
      ctx,
    );
    expect(to).toEqual(jasmine.objectContaining({ direction: 'to', author: 'Tamsin', noble: false }));
  });

  it('sets system lines without an author', () => {
    const line = toChronicleLine(msg({ channelType: ChatChannelType.System, senderName: 'World' }), ctx);
    expect(line.kind).toBe('system');
    expect(line.author).toBeUndefined();
  });

  it('makes a loot entry a Loot line', () => {
    const entry = {
      id: 'l1',
      item: { quantity: 2, itemInstance: { displayName: 'Wolf Pelt' } },
      source: 'combat-reward',
      receivedAt: '2026-10-01T10:00:00',
    } as unknown as LootHistoryEntry;
    expect(lootLine(entry)).toEqual(jasmine.objectContaining({ id: 'loot:l1', channel: 'loot', kind: 'loot', text: '2 × Wolf Pelt' }));
  });

  it('puts a day break before the first line of each day', () => {
    const sources = [msg({ id: 'a', sentAt: '2026-09-30T22:00:00' }), msg({ id: 'b', sentAt: '2026-10-01T09:00:00' }), msg({ id: 'c', sentAt: '2026-10-01T10:00:00' })];
    const lines = sources.map((m) => toChronicleLine(m, ctx));
    const out = withDayBreaks(sources, lines, 'general');
    expect(out.map((l) => l.kind)).toEqual(['day', 'chat', 'day', 'chat', 'chat']);
    expect(out[0].channel).toBe('general');
  });

  it('lists the rooms, then Loot when it is shown', () => {
    const rooms = [
      { label: 'All', channelType: ChatChannelType.General, contextKey: 'all' },
      { label: 'Trade', channelType: ChatChannelType.General, contextKey: 'trade' },
    ];
    expect(chronicleChannels(rooms, true).map((c) => c.id)).toEqual(['all', 'trade', 'loot']);
    expect(chronicleChannels(rooms, false).map((c) => c.id)).toEqual(['all', 'trade']);
  });
});
