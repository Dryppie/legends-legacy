import {
  insertEquipmentLinkAtSelection,
  chatMessageLength,
  equipmentLinkRanges,
  formatEquipmentLink,
  splitChatEquipmentLinks,
} from './chat-equipment-links';
import { Rarity } from '../../models/enums/rarity';

describe('Chat equipment links', () => {
  const id = '2b84eb39-110d-4b01-aacd-72caef024eba';
  const token = formatEquipmentLink(id, 'Phoenix Mace');

  it('round-trips every rarity in both message segments and editable tokens without increasing the character count', () => {
    for (const rarity of Object.values(Rarity)) {
      const link = formatEquipmentLink(id, 'Phoenix Mace', rarity);
      expect(link).toBe(`[Phoenix Mace](equipment:${id}:${rarity})`);
      expect(splitChatEquipmentLinks(link, null)[0].equipmentRarity).toBe(
        rarity,
      );
      expect(equipmentLinkRanges(link)[0].rarity).toBe(rarity);
      expect(equipmentLinkRanges(link)[0].token).toBe(link);
      expect(chatMessageLength(link)).toBe(12);
    }
  });

  it('reads legacy links and normalizes valid rarity values while leaving unknown ones as text', () => {
    expect(equipmentLinkRanges(token)[0].rarity).toBeUndefined();
    expect(equipmentLinkRanges(`[Mace](equipment:${id}:ePiC)`)[0].rarity).toBe(
      Rarity.Epic,
    );
    const invalid = `[Mace](equipment:${id}:Invalid)`;
    expect(equipmentLinkRanges(invalid)).toEqual([]);
    expect(chatMessageLength(invalid)).toBe(invalid.length);
  });

  it('renders multiple links alongside mentions and ordinary text', () => {
    const segments = splitChatEquipmentLinks(
      `Hi @Ash, ${token} and ${token}!`,
      'Ash',
    );
    expect(
      segments.filter((x) => x.equipmentId).map((x) => x.equipmentId),
    ).toEqual([id, id]);
    expect(
      segments.find((x) => x.isMention)?.isCurrentPlayerMention,
    ).toBeTrue();
    expect(segments.map((x) => x.text).join('')).toBe(
      'Hi @Ash, Phoenix Mace and Phoenix Mace!',
    );
  });

  it('leaves malformed links and HTML as literal text', () => {
    const body = '[Fake](equipment:invalid) <img src=x onerror=alert(1)>';
    expect(splitChatEquipmentLinks(body, null)).toEqual([
      { text: body, isMention: false, isCurrentPlayerMention: false },
    ]);
  });

  it('keeps mentions inside equipment labels from becoming character links', () => {
    const segments = splitChatEquipmentLinks(
      formatEquipmentLink(id, '@Ash Mace'),
      'Ash',
    );
    expect(segments.length).toBe(1);
    expect(segments[0].isMention).toBeFalse();
  });

  it('preserves the draft and enforces the name length limit without cutting a link', () => {
    expect(insertEquipmentLinkAtSelection('/trade Selling', token)?.draft).toBe(
      `/trade Selling ${token}`,
    );
    expect(
      insertEquipmentLinkAtSelection(
        'x'.repeat(200 - 'Phoenix Mace'.length - 1),
        token,
      )?.draft,
    ).toBe('x'.repeat(187) + ' ' + token);
    expect(
      insertEquipmentLinkAtSelection(
        'x'.repeat(200 - 'Phoenix Mace'.length),
        token,
      ),
    ).toBeNull();
  });

  it('counts only item names, while counting surrounding text and malformed links in full', () => {
    expect(chatMessageLength(token)).toBe(12);
    expect(chatMessageLength(`${token} ${token} ${token}`)).toBe(38);
    expect(chatMessageLength(`Selling ${token}!`)).toBe(21);
    expect(chatMessageLength('[Fake](equipment:invalid)')).toBe(
      '[Fake](equipment:invalid)'.length,
    );
    expect(chatMessageLength('x'.repeat(200))).toBe(200);
  });

  it('formats labels containing delimiters into a valid link', () => {
    const segments = splitChatEquipmentLinks(
      formatEquipmentLink(id, '[Phoenix]\nMace'),
      null,
    );
    expect(segments[0].equipmentId).toBe(id);
    expect(segments[0].text).toBe('Phoenix  Mace');
  });
});
