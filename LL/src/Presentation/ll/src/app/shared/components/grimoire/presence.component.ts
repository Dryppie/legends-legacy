import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
} from '@angular/core';

/** Online status for another player; the words carry the meaning, not the dot. */
@Component({
  selector: 'lg-presence',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': "online() ? 'lg-presence is-online' : 'lg-presence is-offline'",
    '[attr.title]': 'tooltip()',
  },
  template: `<span class="lg-presence__dot" aria-hidden="true"></span>{{ text() }}`,
})
export class LgPresenceComponent {
  readonly online = input.required<boolean>();
  /** Already formatted: "3 h ago". */
  readonly lastSeen = input<string | null>();
  /** Drop the "Last seen" prefix for tables. */
  readonly compact = input(false, { transform: booleanAttribute });

  protected readonly text = computed(() => {
    if (this.online()) return 'Online';
    const seen = this.lastSeen();
    if (this.compact()) return seen || 'Unknown';
    return `Last seen ${seen || 'unknown'}`;
  });
  protected readonly tooltip = computed(() =>
    this.online() ? 'Online' : `Last seen ${this.lastSeen() || 'unknown'}`,
  );
}
