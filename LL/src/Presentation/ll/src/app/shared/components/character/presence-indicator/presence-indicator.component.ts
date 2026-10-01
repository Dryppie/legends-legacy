import { Component, computed, input } from '@angular/core';
import { NgIf } from '@angular/common';
import { lastSeenLabel as elapsedLabel } from './last-seen';

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
