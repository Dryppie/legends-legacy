import { ChangeDetectionStrategy, Component, booleanAttribute, computed, input } from '@angular/core';
import { lgCx } from './grimoire-core';

/**
 * The information screen frame: a scrolling screen without scene art (Overview, Settings, Leaderboard). It fills its
 * positioned parent and pads for the floating TopBar. With `flow` (D-097) it sits in a frame that is not GameShell —
 * the game's own frame while screens migrate — in the normal flow, filling its parent's height, with no room for a TopBar.
 */
@Component({
  selector: 'lg-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <div [class]="classes()" [attr.role]="role() ?? null" [attr.aria-label]="label() ?? null">
      <div class="lg-page__inner" [style.max-width]="maxWidth() ?? null"><ng-content /></div>
    </div>
  `,
})
export class LgPageComponent {
  readonly label = input<string>();
  /** Defaults to page-max (80rem). */
  readonly maxWidth = input<string>();
  readonly role = input<string>();
  /** In a host frame that is not GameShell: in the normal flow, no room kept for a TopBar; the host gives the gutters. */
  readonly flow = input(false, { transform: booleanAttribute });

  protected readonly classes = computed(() => lgCx('lg-page', this.flow() && 'lg-page--flow'));
}
