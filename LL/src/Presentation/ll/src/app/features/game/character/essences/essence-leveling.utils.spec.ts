import {
  canSpendEssenceDust,
  essenceDustActionLabel,
  essenceDustLevelingDescription,
} from './essence-leveling.utils';

describe('Essence leveling presentation', () => {
  it('only enables leveling when the Essence is below its cap and Dust is available', () => {
    expect(canSpendEssenceDust(6, 10, 1)).toBeTrue();
    expect(canSpendEssenceDust(10, 10, 1)).toBeFalse();
    expect(canSpendEssenceDust(6, 10, 0)).toBeFalse();
  });

  it('disables repeated leveling while a request is in flight', () => {
    expect(canSpendEssenceDust(6, 10, 1, true)).toBeFalse();
    expect(essenceDustActionLabel(6, 10, 1, true)).toBe('Leveling Up…');
  });

  it('explains Dust leveling and the current cap', () => {
    expect(essenceDustLevelingDescription(6, 10, true, 1)).toBe(
      '1 Dust grants 1 level. Level 6 / 10. You have 1 Dust.',
    );
    expect(essenceDustLevelingDescription(10, 10, true, 1)).toContain(
      'Ascend to unlock more levels',
    );
    expect(essenceDustLevelingDescription(100, 100, false, 1)).toContain(
      'Maximum Essence level reached',
    );
  });
});
