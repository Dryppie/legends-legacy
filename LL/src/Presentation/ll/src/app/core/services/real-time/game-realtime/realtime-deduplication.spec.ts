import {
  BusinessGrantDeduper,
  RealtimeSignalDeduper,
  RealtimeUpdateDeduper,
} from './realtime-deduplication';

describe('realtime deduplication', () => {
  it('evicts the oldest update ID at its configured capacity', () => {
    const deduper = new RealtimeUpdateDeduper(2);

    expect(deduper.shouldProcess('update-1')).toBeTrue();
    expect(deduper.shouldProcess('update-2')).toBeTrue();
    expect(deduper.shouldProcess('update-1')).toBeFalse();
    expect(deduper.shouldProcess('update-3')).toBeTrue();
    expect(deduper.shouldProcess('update-1')).toBeTrue();
  });

  it('keeps transport updates and business grants in separate namespaces', () => {
    const updates = new RealtimeUpdateDeduper();
    const grants = new BusinessGrantDeduper();

    expect(updates.shouldProcess('same-id')).toBeTrue();
    expect(grants.shouldApply('same-id')).toBeTrue();
    expect(updates.shouldProcess('same-id')).toBeFalse();
    expect(grants.shouldApply('same-id')).toBeFalse();
  });

  it('deduplicates effect re-entry per signal key', () => {
    const deduper = new RealtimeSignalDeduper();
    const envelope = { updateId: 'delivery-id' };

    expect(deduper.shouldProcess('guild', envelope)).toBeTrue();
    expect(deduper.shouldProcess('guild', envelope)).toBeFalse();
    expect(deduper.shouldProcess('quest', envelope)).toBeTrue();
  });
});
