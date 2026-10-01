import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * The information screen frame: a scrolling screen without scene art (Overview, Settings, Leaderboard). It fills its
 * positioned parent and pads for the floating TopBar.
 */
@Component({
  selector: 'lg-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <div class="lg-page" [attr.role]="role() ?? null" [attr.aria-label]="label() ?? null">
      <div class="lg-page__inner" [style.max-width]="maxWidth() ?? null"><ng-content /></div>
    </div>
  `,
})
export class LgPageComponent {
  readonly label = input<string>();
  /** Defaults to page-max (80rem). */
  readonly maxWidth = input<string>();
  readonly role = input<string>();
}
