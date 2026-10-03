import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { lgPlainValue } from '../../core/grimoire-format';

/**
 * The headline number, with its label and one line of explanation. The full explanation, where there is one, is the
 * host's own `title` attribute (its native tooltip).
 */
@Component({
  selector: 'lg-stat-figure',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': "size() ? 'lg-figure lg-figure--' + size() : 'lg-figure'" },
  template: `
    <span class="lg-figure__label">{{ label() }}</span>
    <span class="lg-figure__value">{{ shown() }}</span>
    @if (caption()) {
      <span class="lg-figure__caption">{{ caption() }}</span>
    }
  `,
  styleUrl: './stat-figure.component.css',
})
export class LgStatFigureComponent {
  readonly label = input.required<string>();
  readonly value = input.required<number | string | null>();
  readonly caption = input<string>();
  readonly size = input<'md' | 'sm'>();
  protected readonly shown = computed(() => lgPlainValue(this.value()));
}
