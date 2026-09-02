import { isFocusedBetaRegionAllowed } from './focused-beta-journey.guard';

describe('focused Beta journey region access', () => {
  it('keeps new players in Shenic and releases other regions at level 30', () => {
    expect(isFocusedBetaRegionAllowed('shenic')).toBeTrue();
    expect(isFocusedBetaRegionAllowed('SHENIC')).toBeTrue();
    expect(isFocusedBetaRegionAllowed('meran')).toBeFalse();
    expect(isFocusedBetaRegionAllowed('meran', 30)).toBeTrue();
    expect(isFocusedBetaRegionAllowed(null)).toBeFalse();
    expect(isFocusedBetaRegionAllowed(null, 30)).toBeFalse();
  });
});
