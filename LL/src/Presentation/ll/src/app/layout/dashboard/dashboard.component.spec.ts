import {
  clampFloatingDrawerPosition,
  getFloatingDrawerVerticalAnchor,
} from './dashboard.component';

describe('clampFloatingDrawerPosition', () => {
  it('keeps the floating chat drawer inside the viewport', () => {
    expect(
      clampFloatingDrawerPosition(
        { left: 900, verticalOffset: -40, verticalAnchor: 'bottom' },
        320,
        480,
        1024,
        768,
      ),
    ).toEqual({ left: 696, verticalOffset: 8, verticalAnchor: 'bottom' });
  });

  it('handles a drawer that is larger than the available viewport', () => {
    expect(
      clampFloatingDrawerPosition(
        { left: 100, verticalOffset: 100, verticalAnchor: 'top' },
        500,
        700,
        400,
        600,
      ),
    ).toEqual({ left: 8, verticalOffset: 8, verticalAnchor: 'top' });
  });
});

describe('getFloatingDrawerVerticalAnchor', () => {
  it('anchors a drawer to the nearest vertical viewport edge', () => {
    expect(getFloatingDrawerVerticalAnchor(12, 60, 800)).toBe('top');
    expect(getFloatingDrawerVerticalAnchor(600, 780, 800)).toBe('bottom');
  });
});
