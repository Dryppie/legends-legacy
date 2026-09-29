import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { lgFormatNumber } from './grimoire-core';

/** Compact label/value tile. Lay tiles out in `<div class="lg-statgrid">`. */
@Component({
  selector: 'lg-stat-tile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-stattile' },
  template: `
    <span class="lg-stattile__label">{{ label() }}</span>
    <span class="lg-stattile__value">
      {{ format(value()) }}
      @if (suffix()) {
        <small>{{ suffix() }}</small>
      }
    </span>
    @if (delta(); as change) {
      <span
        class="lg-stattile__delta"
        [class.is-up]="change > 0"
        [class.is-down]="change < 0"
        [attr.aria-label]="(change > 0 ? 'up ' : 'down ') + abs(change)"
      >
        {{ change > 0 ? '▲' : '▼' }} {{ abs(change) }}
      </span>
    }
  `,
})
export class LgStatTileComponent {
  readonly label = input.required<string>();
  readonly value = input.required<number | string>();
  readonly suffix = input<string>();
  /** Change since last look; hidden when 0 or unset. */
  readonly delta = input<number>();
  protected readonly format = lgFormatNumber;
  protected readonly abs = Math.abs;
}
