import { Component, computed, input } from '@angular/core';
import { NgIf } from '@angular/common';

/**
 * Shows whether a character is currently active.
 *
 * "Online" mirrors the backend `isOnline` flag, which is true when the
 * character produced an action inside PlayerActivityConstants.OnlineWindow.
 * Otherwise a relative "last seen" label is shown so inactive members are easy
 * to spot.
 */
@Component({
  selector: 'app-presence-indicator',
  imports: [NgIf],
  templateUrl: './presence-indicator.component.html',
})
export class PresenceIndicatorComponent {
  readonly isOnline = input.required<boolean>();
  readonly lastSeenAt = input<string | null | undefined>(null);

  /** Drops the "Last seen " prefix and adds a grey dot, for tight table cells. */
  readonly compact = input(false);

  readonly offlineLabel = computed(() => {
    const elapsed = elapsedLabel(this.lastSeenAt());
    if (!elapsed) return this.compact() ? 'Unknown' : 'Last seen unknown';
    return this.compact() ? elapsed : `Last seen ${elapsed}`;
  });

  readonly offlineTitle = computed(() => {
    const elapsed = elapsedLabel(this.lastSeenAt());
    return elapsed ? `Last seen ${elapsed}` : 'Last seen unknown';
  });
}

function elapsedLabel(lastSeenAt: string | null | undefined): string | null {
  if (!lastSeenAt) return null;

  const timestamp = new Date(lastSeenAt).getTime();
  if (Number.isNaN(timestamp)) return null;

  const elapsedMinutes = Math.floor(
    Math.max(0, Date.now() - timestamp) / 60_000,
  );
  if (elapsedMinutes < 1) return 'just now';
  if (elapsedMinutes < 60) {
    return `${elapsedMinutes} ${elapsedMinutes === 1 ? 'minute' : 'minutes'} ago`;
  }

  const elapsedHours = Math.floor(elapsedMinutes / 60);
  if (elapsedHours < 24) {
    return `${elapsedHours} ${elapsedHours === 1 ? 'hour' : 'hours'} ago`;
  }

  const elapsedDays = Math.floor(elapsedHours / 24);
  return `${elapsedDays} ${elapsedDays === 1 ? 'day' : 'days'} ago`;
}
