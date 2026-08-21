export type RealtimeEventMetadata = {
  updateId?: string;
  occurredAt?: string;
} | null;

export function getRealtimeDeliveryId(
  event: RealtimeEventMetadata,
): string | null {
  return event?.updateId ?? event?.occurredAt ?? null;
}

class BoundedIdentifierCache {
  private readonly identifiers = new Set<string>();
  private readonly insertionOrder: string[] = [];

  constructor(private readonly capacity: number) {
    if (!Number.isSafeInteger(capacity) || capacity < 1) {
      throw new RangeError('Dedupe capacity must be a positive integer.');
    }
  }

  accept(identifier: string): boolean {
    if (this.identifiers.has(identifier)) return false;

    this.identifiers.add(identifier);
    this.insertionOrder.push(identifier);
    while (this.insertionOrder.length > this.capacity) {
      const expired = this.insertionOrder.shift();
      if (expired) this.identifiers.delete(expired);
    }
    return true;
  }

  clear(): void {
    this.identifiers.clear();
    this.insertionOrder.length = 0;
  }
}

/** Deduplicates transport deliveries by envelope update ID. */
export class RealtimeUpdateDeduper {
  private readonly identifiers: BoundedIdentifierCache;

  constructor(capacity = 500) {
    this.identifiers = new BoundedIdentifierCache(capacity);
  }

  shouldProcess(updateId: string | null | undefined): boolean {
    return !updateId || this.identifiers.accept(updateId);
  }

  clear(): void {
    this.identifiers.clear();
  }
}

/** Deduplicates an idempotent business grant independently of its deliveries. */
export class BusinessGrantDeduper {
  private readonly identifiers: BoundedIdentifierCache;

  constructor(capacity = 500) {
    this.identifiers = new BoundedIdentifierCache(capacity);
  }

  shouldApply(grantId: string | null | undefined): boolean {
    return !grantId || this.identifiers.accept(grantId);
  }

  clear(): void {
    this.identifiers.clear();
  }
}

/** Prevents an Angular effect from reprocessing its latest signal value. */
export class RealtimeSignalDeduper {
  private readonly lastDeliveryIds = new Map<string, string>();

  shouldProcess(key: string, event: RealtimeEventMetadata): boolean {
    const deliveryId = getRealtimeDeliveryId(event);
    if (!deliveryId) return true;
    if (this.lastDeliveryIds.get(key) === deliveryId) return false;

    this.lastDeliveryIds.set(key, deliveryId);
    return true;
  }

  clear(): void {
    this.lastDeliveryIds.clear();
  }
}
