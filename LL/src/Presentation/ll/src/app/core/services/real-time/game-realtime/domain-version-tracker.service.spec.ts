import { DomainVersionTracker } from './domain-version-tracker.service';

describe('DomainVersionTracker', () => {
  it('keeps the highest observed version for each domain', () => {
    const tracker = new DomainVersionTracker();

    tracker.observe({ inventory: 5, equipment: 2 });
    tracker.observe({ inventory: 3, equipment: 4 });

    expect(tracker.latest('inventory')).toBe(5);
    expect(tracker.latest('equipment')).toBe(4);
    expect(tracker.isCurrent('inventory', 4)).toBeFalse();
    expect(tracker.isCurrent('inventory', 5)).toBeTrue();
  });
});
