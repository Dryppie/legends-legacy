import { ChangeDetectionStrategy, Component, booleanAttribute, input } from '@angular/core';
import { LgDensity } from '../../core/grimoire-core';

/** The content box: a framed group with a small-caps head. Head extras go in `lgSlot="aside"`. */
@Component({
  selector: 'lg-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    style: 'display: contents',
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
  },
  template: `
    <section
      class="lg-panel"
      [class.lg-panel--flush]="flush()"
      [attr.data-density]="density() ?? null"
      [attr.aria-label]="title() || null"
    >
      @if (title()) {
        <header class="lg-panel__head" [class.is-end]="titleAlign() === 'end'">
          <span class="lg-panel__title">{{ title() }}</span><ng-content select="[lgSlot=aside]" />
        </header>
      }
      <div class="lg-panel__body"><ng-content /></div>
    </section>
  `,
})
export class LgPanelComponent {
  readonly title = input<string>();
  readonly titleAlign = input<'start' | 'end'>('start');
  /** No body padding: for a List or table that runs edge to edge. */
  readonly flush = input(false, { transform: booleanAttribute });
  readonly density = input<LgDensity>();
}
