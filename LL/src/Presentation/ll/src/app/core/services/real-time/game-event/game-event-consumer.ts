export type GameEventMetadata = {
  updateId?: string;
  occurredAt?: string;
} | null;

export function getGameEventId(event: GameEventMetadata): string | null {
  return event?.updateId ?? event?.occurredAt ?? null;
}

export class GameEventDeduper {
  private readonly lastUpdateIds = new Map<string, string>();

  shouldProcess(key: string, event: GameEventMetadata): boolean {
    const updateId = getGameEventId(event);
    if (!updateId) return true;
    if (this.lastUpdateIds.get(key) === updateId) return false;

    this.lastUpdateIds.set(key, updateId);
    return true;
  }
}
