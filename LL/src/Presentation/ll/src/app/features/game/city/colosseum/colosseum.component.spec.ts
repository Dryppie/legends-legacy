import { colosseumTabIndex } from './colosseum.component';

describe('colosseumTabIndex', () => {
  it('opens the tournament grounds tab for tournament deep links', () => {
    expect(colosseumTabIndex('tournaments')).toBe(1);
    expect(colosseumTabIndex('TOURNAMENTS')).toBe(1);
  });

  it('maps the remaining tab deep links', () => {
    expect(colosseumTabIndex('market')).toBe(2);
    expect(colosseumTabIndex('rankings')).toBe(3);
    expect(colosseumTabIndex('record')).toBe(4);
  });

  it('falls back to the arena tab for missing or unknown deep links', () => {
    expect(colosseumTabIndex(null)).toBe(0);
    expect(colosseumTabIndex('unknown')).toBe(0);
  });
});
