import { toDisplayedCombatRating } from './combat-rating-display';

describe('toDisplayedCombatRating', () => {
  it('shows Combat Rating at one tenth of its internal value rounded down', () => {
    expect(toDisplayedCombatRating(1000)).toBe(100);
    expect(toDisplayedCombatRating(753)).toBe(75);
    expect(toDisplayedCombatRating(759)).toBe(75);
    expect(toDisplayedCombatRating(760)).toBe(76);
  });
});
