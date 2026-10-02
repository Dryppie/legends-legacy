import { ChatChannelType, ChatMessageDto } from '../../core/services/ll-chat/chat-service/chat.service';
import { formatLocalDate } from '../../shared/pipes/local-date/local-date.pipe';
import { LgChronicleChannel, LgChronicleMessage } from '@grimoire';
import { LootHistoryEntry } from '../../shared/models/loot-history';
import { isInlineChannelSystemMessage, isWorldSystemMessage, startsNewChatDay } from '../dashboard/chat/chat.component';

/** The Chronicle's channel id for a chat channel: its hue owner's name (Registries · Channels). */
export function chronicleChannelId(type: ChatChannelType, contextKey?: string): string {
  if (type === ChatChannelType.General) {
    return contextKey === 'trade' ? 'trade' : contextKey === 'help' ? 'help' : 'general';
  }
  return String(type).toLowerCase();
}

/** The tag a line carries in a merged feed, in words. */
export function chronicleChannelLabel(m: ChatMessageDto): string {
  if (m.channelType === ChatChannelType.Invites) return 'Invites';
  if (isWorldSystemMessage(m)) return 'World';
  const id = chronicleChannelId(m.channelType, m.contextKey);
  return id.charAt(0).toUpperCase() + id.slice(1);
}

export interface ChatLineContext {
  characterId: string | null | undefined;
  /** Mentions the player and was written by someone else. */
  mentioned: (m: ChatMessageDto) => boolean;
  noble: (characterId: string) => boolean;
}

/** One chat message as a Chronicle line. The text is drawn by the host's template, from the source message. */
export function toChronicleLine(m: ChatMessageDto, ctx: ChatLineContext): LgChronicleMessage {
  const channel = chronicleChannelId(m.channelType, m.contextKey);
  const base = {
    id: m.id,
    channel,
    channelLabel: chronicleChannelLabel(m),
    time: formatLocalDate(m.sentAt, 'shortTime') ?? undefined,
    text: m.body,
  };
  if (m.channelType === ChatChannelType.System || m.channelType === ChatChannelType.Invites || isInlineChannelSystemMessage(m)) {
    return { ...base, kind: 'system' };
  }
  if (m.linkedItem) return { ...base, kind: 'system' };
  if (m.channelType === ChatChannelType.Whisper) {
    const mine = m.senderId === ctx.characterId;
    const otherId = mine ? (m.targetCharacterId ?? '') : m.senderId;
    return {
      ...base,
      kind: 'chat',
      author: (mine ? m.targetCharacterTitleDisplayName || m.targetCharacterName : m.senderTitleDisplayName || m.senderName) ?? '',
      direction: mine ? 'to' : 'from',
      noble: !!otherId && ctx.noble(otherId),
      mention: ctx.mentioned(m),
    };
  }
  return {
    ...base,
    kind: 'chat',
    author: m.senderTitleDisplayName?.trim() || m.senderName,
    noble: ctx.noble(m.senderId),
    mention: ctx.mentioned(m),
  };
}

/** A loot entry as a Loot channel line (D-005): what was found, where, in the game log's italic. */
export function lootLine(entry: LootHistoryEntry): LgChronicleMessage {
  return {
    id: 'loot:' + entry.id,
    channel: 'loot',
    channelLabel: 'Loot',
    kind: 'loot',
    time: formatLocalDate(entry.receivedAt, 'shortTime') ?? undefined,
    text: `${entry.item.quantity} × ${entry.item.itemInstance.displayName ?? entry.item.itemInstance.itemBase?.name ?? 'Item'}`,
  };
}

/** Day breaks (D-112) before the first line of each day, in the channel being shown so its filter keeps them. */
export function withDayBreaks(
  sources: readonly { sentAt: Date | string }[],
  lines: readonly LgChronicleMessage[],
  channel: string,
): LgChronicleMessage[] {
  const out: LgChronicleMessage[] = [];
  lines.forEach((line, i) => {
    if (startsNewChatDay(sources as ChatMessageDto[], i)) {
      out.push({
        id: 'day:' + line.id,
        channel: channel === 'all' ? line.channel : channel,
        kind: 'day',
        text: formatLocalDate(sources[i].sentAt, 'mediumDate') ?? '',
      });
    }
    out.push(line);
  });
  return out;
}

/** The tabs: the old chat's visible rooms, then Loot, in the Chronicle's shape. */
export function chronicleChannels(rooms: readonly { label: string; channelType: ChatChannelType; contextKey: string }[], loot: boolean): LgChronicleChannel[] {
  const tabs = rooms.map((r) => ({
    id: r.contextKey === 'all' ? 'all' : chronicleChannelId(r.channelType, r.contextKey),
    label: r.label,
  }));
  return loot ? [...tabs, { id: 'loot', label: 'Loot' }] : tabs;
}
