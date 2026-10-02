import { ChangeDetectionStrategy, Component, booleanAttribute, inject, input } from '@angular/core';
import { LG_SHELL } from '../../core/grimoire-core';

/**
 * The information screen frame: a scrolling screen without scene art (Overview, Settings, Leaderboard). It fills its
 * positioned parent and pads for the floating TopBar. With `flow` (D-097) it sits in a frame that is not GameShell —
 * the game's own frame while screens migrate — in the normal flow, filling its parent's height, with no room for a TopBar.
 * Inside a GameShell it is dense (D-120): its content sits stack-lg apart and fills the stage to the gutter.
 */
@Component({
  selector: 'lg-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-page',
    '[class.lg-page--flow]': 'flow()',
    '[class.lg-page--dense]': 'dense',
    '[attr.aria-label]': 'label() ?? null',
  },
  template: `<div class="lg-page__inner" [style.max-width]="maxWidth() ?? null"><ng-content /></div>`,
  styleUrl: './page.component.css',
})
export class LgPageComponent {
  readonly label = input<string>();
  /** Defaults to page-max (80rem). */
  readonly maxWidth = input<string>();
  /** In a host frame that is not GameShell: in the normal flow, no room kept for a TopBar; the host gives the gutters. */
  readonly flow = input(false, { transform: booleanAttribute });

  /** In the game's frame (D-120). */
  protected readonly dense = !!inject(LG_SHELL, { optional: true });
}
