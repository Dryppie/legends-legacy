import {
  findDraftMention,
  formatChatMention,
  insertChatMention,
  splitChatMentions,
} from './chat-mentions';

describe('chat mentions', () => {
  it('highlights every mention and identifies only the exact recipient', () => {
    const segments = splitChatMentions(
      'Hey @Ember, ask @ASH and @Ashen!',
      'Ash',
    );
    expect(
      segments
        .filter((segment) => segment.isMention)
        .map((segment) => segment.text),
    ).toEqual(['@Ember', '@ASH', '@Ashen']);
    expect(
      segments
        .filter((segment) => segment.isCurrentPlayerMention)
        .map((segment) => segment.text),
    ).toEqual(['@ASH']);
    expect(segments.map((segment) => segment.text).join('')).toBe(
      'Hey @Ember, ask @ASH and @Ashen!',
    );
  });

  it('does not match names inside emails, URLs or longer names', () => {
    const segments = splitChatMentions(
      'ember@Ash.test https://game/@Ash @Ashen @Ash-knight @Ash_1',
      'Ash',
    );
    expect(
      segments.some((segment) => segment.isCurrentPlayerMention),
    ).toBeFalse();
    expect(segments.filter((segment) => segment.isMention).length).toBe(3);
  });

  it('preserves manually typed multi-word mentions and repeated mentions', () => {
    const segments = splitChatMentions(
      'Hey @ember knight, @Ember Knight!',
      'Ember Knight',
    );
    expect(
      segments.filter((segment) => segment.isCurrentPlayerMention).length,
    ).toBe(2);
  });

  it('renders selected names containing spaces or punctuation for every viewer', () => {
    for (const name of [
      'Ember Knight',
      'A.B',
      'A"B',
      'A\\B',
      '<img src=x>',
      'Ægir',
    ]) {
      const body = `Hey ${formatChatMention(name)}!`;
      const recipient = splitChatMentions(body, name);
      const other = splitChatMentions(body, 'Someone Else');
      expect(
        recipient
          .filter((segment) => segment.isCurrentPlayerMention)
          .map((segment) => segment.text),
      ).toEqual([`@${name}`]);
      expect(
        other
          .filter((segment) => segment.isMention)
          .map((segment) => segment.text),
      ).toEqual([`@${name}`]);
      expect(
        other.some((segment) => segment.isCurrentPlayerMention),
      ).toBeFalse();
    }
    expect(
      splitChatMentions('@"Ember Knight"', 'Ember').some(
        (segment) => segment.isCurrentPlayerMention,
      ),
    ).toBeFalse();
  });

  it('leaves malformed quoted mentions and HTML as text', () => {
    const body = '<img src=x onerror=alert(1)> @"Ember';
    expect(splitChatMentions(body, 'Ember')).toEqual([
      { text: body, isMention: false, isCurrentPlayerMention: false },
    ]);
  });

  it('finds only the tag at the caret, including its untyped suffix', () => {
    expect(findDraftMention('Hi @Ember, ready?', 6)).toEqual({
      start: 3,
      end: 9,
      query: 'Em',
    });
    expect(findDraftMention('@Ash and @Em', 12)).toEqual({
      start: 9,
      end: 12,
      query: 'Em',
    });
    expect(findDraftMention('Hi @', 4)).toEqual({
      start: 3,
      end: 4,
      query: '',
    });
    expect(findDraftMention('a@Em', 4)).toBeNull();
    expect(findDraftMention('@Em ready', 9)).toBeNull();
    expect(findDraftMention('@Em ', 4)).toBeNull();
    expect(findDraftMention('@"Ember Knight" ', 15)).toBeNull();
    expect(findDraftMention('', 0)).toBeNull();
  });

  it('inserts a name in the middle without losing surrounding text', () => {
    const body = 'Hi @Ember, ready?';
    const result = insertChatMention(
      body,
      findDraftMention(body, 6)!,
      'Ember Knight',
    );
    expect(result).toEqual({ draft: 'Hi @"Ember Knight", ready?', caret: 18 });
  });

  it('adds a space at the end and rejects an insertion exceeding the message limit', () => {
    expect(
      insertChatMention('@Em', findDraftMention('@Em', 3)!, 'Ember'),
    ).toEqual({ draft: '@Ember ', caret: 7 });
    const body = 'x'.repeat(190) + ' @Em';
    expect(
      insertChatMention(
        body,
        findDraftMention(body, body.length)!,
        'Ember Knight',
      ),
    ).toBeNull();
  });
});
