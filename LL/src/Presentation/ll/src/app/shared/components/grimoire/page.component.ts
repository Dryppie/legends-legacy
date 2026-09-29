import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  input,
} from '@angular/core';

/**
 * Scrolling frame for information screens without scene art (Overview, Settings,
 * Leaderboard). Fills its positioned parent and pads for the floating TopBar;
 * set `embedded` to use it inside the current dashboard layout.
 */
@Component({
  selector: 'lg-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': "embedded() ? 'lg-page lg-page--embedded' : 'lg-page'",
    '[attr.aria-label]': 'label() ?? null',
    '[attr.role]': "label() ? 'region' : null",
  },
  template: `
    <div class="lg-page__inner" [style.max-width]="maxWidth() ?? null">
      <ng-content />
    </div>
  `,
})
export class LgPageComponent {
  readonly label = input<string>();
  readonly maxWidth = input<string>();
  /** Flow in the parent (no absolute fill, no TopBar padding). */
  readonly embedded = input(false, { transform: booleanAttribute });
}
