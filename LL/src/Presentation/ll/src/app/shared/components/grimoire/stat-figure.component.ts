import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { lgFormatNumber } from './grimoire-core';

/** A headline number with its label and one line of explanation. */
@Component({
  selector: 'lg-stat-figure',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': "size() === 'sm' ? 'lg-figure lg-figure--sm' : 'lg-figure'",
    '[attr.title]': 'title() ?? null',
  },
  template: `
    <span class="lg-figure__label">{{ label() }}</span>
    <span class="lg-figure__value">{{ format(value()) }}</span>
    @if (caption()) {
      <span class="lg-figure__caption">{{ caption() }}</span>
    }
  `,
})
export class LgStatFigureComponent {
  readonly label = input.required<string>();
  readonly value = input.required<number | string>();
  readonly caption = input<string>();
  /** Full explanation, shown as a tooltip. */
  readonly title = input<string>();
  readonly size = input<'md' | 'sm'>('md');
  protected readonly format = lgFormatNumber;
}
