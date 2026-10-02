/*
 * What screen readers are told (Foundations · Accessibility · Live regions), on the CDK's LiveAnnouncer (D-134).
 */
import { Injectable, OnDestroy, inject } from '@angular/core';
import { LiveAnnouncer } from '@angular/cdk/a11y';

export interface LgAnnounceOptions {
  /** Lines with the same key replace each other while they wait, and the same words aren't repeated within 5s. */
  key?: string;
  /** For errors only: said at once, interrupting. */
  assertive?: boolean;
}

/**
 * The game's one announcer. Polite lines go out one at a time, at most every 1.5s; a line with the key of one still
 * waiting replaces it (ten loot drops become one line); the same words under the same key aren't said again within 5s;
 * at most three lines wait. `assertive` is for errors only and interrupts. Inject it where a part says something:
 * `private readonly announcer = inject(LgAnnouncer);`, then `this.announcer.announce('Locked. Unlocks at level 20')`.
 */
@Injectable({ providedIn: 'root' })
export class LgAnnouncer implements OnDestroy {
  private readonly live = inject(LiveAnnouncer);
  private readonly queue: { key: string; text: string }[] = [];
  private readonly last = new Map<string, { text: string; at: number }>();
  private timer: ReturnType<typeof setTimeout> | null = null;

  announce(text: string, options: LgAnnounceOptions = {}): void {
    if (!text) return;
    const key = options.key || text;
    const seen = this.last.get(key);
    if (seen && seen.text === text && Date.now() - seen.at < 5000) return;
    if (options.assertive) {
      void this.live.announce(text, 'assertive');
      this.last.set(key, { text, at: Date.now() });
      return;
    }
    const waiting = this.queue.findIndex((q) => q.key === key);
    if (waiting >= 0) this.queue[waiting].text = text;
    else this.queue.push({ key, text });
    if (this.queue.length > 3) this.queue.shift();
    if (!this.timer) this.next();
  }

  ngOnDestroy(): void {
    if (this.timer) clearTimeout(this.timer);
    this.timer = null;
    this.queue.length = 0;
  }

  private next(): void {
    const line = this.queue.shift();
    if (!line) {
      this.timer = null;
      return;
    }
    void this.live.announce(line.text, 'polite');
    this.last.set(line.key, { text: line.text, at: Date.now() });
    this.timer = setTimeout(() => this.next(), 1500);
  }
}
