import {
  ChatTextSegment,
  splitChatMentions,
} from '../../../layout/dashboard/chat/chat-mentions';

const EQUIPMENT_LINK =
  /\[([^\[\]\r\n]{1,64})\]\(equipment:([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})\)/gi;

/** Item names count toward the limit; their brackets and encoded IDs do not. */
export function chatMessageLength(body: string): number {
  return body.replace(EQUIPMENT_LINK, (_token, name: string) => name).length;
}

export function equipmentLinkRanges(body: string) {
  return Array.from(body.matchAll(EQUIPMENT_LINK), (match) => ({
    start: match.index!,
    end: match.index! + match[0].length,
    token: match[0],
    name: match[1],
    id: match[2],
  }));
}

export function expandEquipmentSelection(
  body: string,
  start: number,
  end: number,
) {
  for (const link of equipmentLinkRanges(body)) {
    if (start > link.start && start < link.end) start = link.start;
    if (end > link.start && end < link.end) end = link.end;
  }
  return { start, end };
}

export function insertEquipmentLinkAtSelection(
  body: string,
  token: string,
  start = body.length,
  end = start,
) {
  ({ start, end } = expandEquipmentSelection(body, start, end));
  const prefix = body.slice(0, start);
  const suffix = body.slice(end);
  const inserted =
    (prefix && !/\s$/.test(prefix) ? ' ' : '') +
    token +
    (suffix && !/^\s/.test(suffix) ? ' ' : '');
  const draft = prefix + inserted + suffix;
  return chatMessageLength(draft) <= 200
    ? { draft, caret: prefix.length + inserted.length }
    : null;
}

export function formatEquipmentLink(id: string, name: string): string {
  const label =
    name
      .replace(/[\[\]\r\n]/g, ' ')
      .trim()
      .slice(0, 64) || 'Equipment';
  return `[${label}](equipment:${id})`;
}

export function splitChatEquipmentLinks(
  body: string,
  playerName: string | null | undefined,
): ChatTextSegment[] {
  const segments: ChatTextSegment[] = [];
  let cursor = 0;
  for (const match of body.matchAll(EQUIPMENT_LINK)) {
    if (match.index! > cursor)
      segments.push(
        ...splitChatMentions(body.slice(cursor, match.index), playerName),
      );
    segments.push({
      text: match[1],
      isMention: false,
      isCurrentPlayerMention: false,
      equipmentId: match[2],
    });
    cursor = match.index! + match[0].length;
  }
  if (cursor < body.length || !segments.length)
    segments.push(...splitChatMentions(body.slice(cursor), playerName));
  return segments;
}
