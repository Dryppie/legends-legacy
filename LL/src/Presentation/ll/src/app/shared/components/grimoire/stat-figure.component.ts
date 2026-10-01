import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { lgPlainValue } from './grimoire-format';

/** The headline number, with its label and one line of explanation. */
@Component({
  selector: 'lg-stat-figure',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    style: 'display: contents',
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
  },
  template: `
    <div [class]="size() ? 'lg-figure lg-figure--' + size() : 'lg-figure'" [attr.title]="title() ?? null">
      <span class="lg-figure__label">{{ label() }}</span>
      <span class="lg-figure__value">{{ shown() }}</span>
      @if (caption()) {
        <span class="lg-figure__caption">{{ caption() }}</span>
      }
    </div>
  `,
})
export class LgStatFigureComponent {
  readonly label = input.required<string>();
  readonly value = input.required<number | string | null>();
  readonly caption = input<string>();
  /** The full explanation, shown as a tooltip. */
  readonly title = input<string>();
  readonly size = input<'md' | 'sm'>();
  protected readonly shown = computed(() => lgPlainValue(this.value()));
}
