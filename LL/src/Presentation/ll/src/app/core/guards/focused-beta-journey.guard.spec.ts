import { isFocusedBetaRegionAllowed } from './focused-beta-journey.guard';

describe('focused Beta journey region access', () => {
  it('allows Shenic and rejects other or missing region ids', () => {
    expect(isFocusedBetaRegionAllowed('shenic')).toBeTrue();
    expect(isFocusedBetaRegionAllowed('SHENIC')).toBeTrue();
    expect(isFocusedBetaRegionAllowed('meran')).toBeFalse();
    expect(isFocusedBetaRegionAllowed(null)).toBeFalse();
  });
});
