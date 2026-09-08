export interface ChatTextSegment {
  text: string;
  isMention: boolean;
  isCurrentPlayerMention: boolean;
}

export interface DraftMention {
  start: number;
  end: number;
  query: string;
}

const DELIMITER = /[\s.,!?;:()[\]{}"']/u;
const SIMPLE_NAME = /^[\p{L}\p{N}\p{M}_-]+$/u;
const SIMPLE_MENTION = /^@([\p{L}\p{N}\p{M}_-]+)/u;
// Quoting makes names containing spaces or punctuation unambiguous in stored
// chat text, without changing the chat transport or rendering user HTML.
const QUOTED_MENTION = /^@("(?:[^"\\]|\\.)*")/u;

export function formatChatMention(name: string): string {
  return `@${SIMPLE_NAME.test(name) ? name : JSON.stringify(name)}`;
}

export function splitChatMentions(
  body: string,
  playerName: string | null | undefined,
): ChatTextSegment[] {
  const currentName = playerName?.trim().toLowerCase();
  const segments: ChatTextSegment[] = [];
  let cursor = 0;

  for (let start = 0; start < body.length; start++) {
    if (
      body[start] !== '@' ||
      (start > 0 && !DELIMITER.test(body[start - 1]))
    ) {
      continue;
    }

    const remainder = body.slice(start);
    const quoted = QUOTED_MENTION.exec(remainder);
    let name: string | undefined;
    let length = 0;
    if (quoted) {
      try {
        name = JSON.parse(quoted[1]) as string;
        length = quoted[0].length;
      } catch {
        continue;
      }
    } else if (!remainder.startsWith('@"')) {
      const simple = SIMPLE_MENTION.exec(remainder);
      name = simple?.[1];
      length = simple?.[0].length ?? 0;
      // Keep manually typed mentions of existing multi-word names working.
      const legacyName = playerName?.trim();
      if (
        legacyName &&
        legacyName.length >= (name?.length ?? 0) &&
        remainder.slice(1, legacyName.length + 1).toLowerCase() ===
          currentName &&
        (start + legacyName.length + 1 === body.length ||
          DELIMITER.test(body[start + legacyName.length + 1]))
      ) {
        name = remainder.slice(1, legacyName.length + 1);
        length = legacyName.length + 1;
      }
    }

    const end = start + length;
    if (!name || (end < body.length && !DELIMITER.test(body[end]))) continue;
    if (start > cursor) {
      segments.push({
        text: body.slice(cursor, start),
        isMention: false,
        isCurrentPlayerMention: false,
      });
    }
    segments.push({
      text: `@${name}`,
      isMention: true,
      isCurrentPlayerMention: name.toLowerCase() === currentName,
    });
    cursor = end;
    start = end - 1;
  }

  if (cursor < body.length || !segments.length) {
    segments.push({
      text: body.slice(cursor),
      isMention: false,
      isCurrentPlayerMention: false,
    });
  }
  return segments;
}

/** Only replace the mention at the caret, preserving the rest of the draft. */
export function findDraftMention(
  draft: string,
  caret: number,
): DraftMention | null {
  const start = draft.lastIndexOf('@', caret - 1);
  if (
    start < 0 ||
    caret <= start ||
    (start > 0 && !DELIMITER.test(draft[start - 1]))
  )
    return null;

  const prefix = draft.slice(start + 1, caret);
  if (prefix.startsWith('"')) {
    const quoted = QUOTED_MENTION.exec(draft.slice(start));
    if (quoted && caret >= start + quoted[0].length) return null;
    const query = prefix.slice(1);
    if (/["\\\r\n]/u.test(query)) return null;
    return { start, end: quoted ? start + quoted[0].length : caret, query };
  }

  if (prefix && !SIMPLE_NAME.test(prefix)) return null;
  const suffix = /^[\p{L}\p{N}\p{M}_-]*/u.exec(draft.slice(caret))![0];
  return { start, end: caret + suffix.length, query: prefix };
}

export function insertChatMention(
  draft: string,
  mention: DraftMention,
  name: string,
  maxLength = 200,
): { draft: string; caret: number } | null {
  const token = formatChatMention(name);
  const suffix = draft.slice(mention.end);
  const separator = !suffix || !DELIMITER.test(suffix[0]) ? ' ' : '';
  const nextDraft = draft.slice(0, mention.start) + token + separator + suffix;
  if (nextDraft.length > maxLength) return null;
  return {
    draft: nextDraft,
    caret:
      mention.start +
      token.length +
      (separator.length || (/^\s/u.test(suffix) ? 1 : 0)),
  };
}
