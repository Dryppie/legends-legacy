import {
  ChangeDetectionStrategy,
  Component,
  input,
} from '@angular/core';

/** Framed box with a small-caps header. Header extras go in `lgSlot="aside"`. */
@Component({
  selector: 'lg-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
    class: 'lg-panel',
    role: 'region',
    '[attr.aria-label]': 'title() ?? null',
  },
  template: `
    @if (title()) {
      <header class="lg-panel__head" [class.is-end]="titleAlign() === 'end'">
        <span class="lg-panel__title">{{ title() }}</span>
        <ng-content select="[lgSlot=aside]" />
      </header>
    }
    <div class="lg-panel__body"><ng-content /></div>
  `,
})
export class LgPanelComponent {
  readonly title = input<string>();
  readonly titleAlign = input<'start' | 'end'>('start');
}
