import { Injectable, computed, signal } from '@angular/core';

export const NOTIFICATION_SURFACE = {
  Sidebar: 'sidebar',
  Header: 'header',
  Page: 'page',
} as const;

export const SIDEBAR_NOTIFICATION = {
  Guild: 'guild',
  Colosseum: 'colosseum',
  Prophecies: 'prophecies',
} as const;

export type NotificationSurface =
  (typeof NOTIFICATION_SURFACE)[keyof typeof NOTIFICATION_SURFACE];

interface NotificationEntry {
  count: number;
  seen: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly _entries = signal<Record<string, NotificationEntry>>({});

  readonly entries = computed(() => this._entries());

  count(surface: NotificationSurface, key: string): number {
    return this._entries()[this.entryKey(surface, key)]?.count ?? 0;
  }

  increment(surface: NotificationSurface, key: string, amount = 1): void {
    const entryKey = this.entryKey(surface, key);

    this._entries.update((entries) => {
      const current = entries[entryKey] ?? { count: 0, seen: true };

      return {
        ...entries,
        [entryKey]: {
          count: current.count + amount,
          seen: false,
        },
      };
    });
  }

  initializeCount(
    surface: NotificationSurface,
    key: string,
    count: number,
  ): void {
    const entryKey = this.entryKey(surface, key);

    this._entries.update((entries) => {
      const current = entries[entryKey];
      if (current?.seen || (current?.count ?? 0) > 0) return entries;

      return {
        ...entries,
        [entryKey]: {
          count,
          seen: false,
        },
      };
    });
  }

  setCount(surface: NotificationSurface, key: string, count: number): void {
    if (count <= 0) {
      this.markSeen(surface, key);
      return;
    }

    const entryKey = this.entryKey(surface, key);
    this._entries.update((entries) => ({
      ...entries,
      [entryKey]: {
        count,
        seen: false,
      },
    }));
  }

  markSeen(surface: NotificationSurface, key: string): void {
    const entryKey = this.entryKey(surface, key);

    this._entries.update((entries) => ({
      ...entries,
      [entryKey]: {
        count: 0,
        seen: true,
      },
    }));
  }

  private entryKey(surface: NotificationSurface, key: string): string {
    return `${surface}:${key}`;
  }
}
