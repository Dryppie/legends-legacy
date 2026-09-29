import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { lgUniqueId } from './grimoire-core';

export interface LgLedgerRow {
  id?: string;
  label: string;
  /** Already formatted, units included. */
  value: string;
  /** Second line under the value, e.g. "240 Armor Rating". */
  sub?: string;
  /** What the row means; shown on hover and keyboard focus. */
  description?: string;
  /** Gilt footnote in the explanation. */
  tipMeta?: string;
  muted?: boolean;
}

/**
 * Titled label/value rows joined by dotted leaders, each able to explain itself.
 * Put several in `<div class="lg-ledgergrid">`.
 */
@Component({
  selector: 'lg-ledger',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
    class: 'lg-ledger',
    role: 'region',
    '[attr.aria-labelledby]': 'id + "-title"',
  },
  template: `
    <h3 class="lg-ledger__title" [id]="id + '-title'">{{ title() }}</h3>
    <dl class="lg-ledger__rows">
      @for (row of rows(); track row.id ?? row.label; let i = $index) {
        <div
          class="lg-ledger__row"
          [class.is-muted]="!!row.muted"
          [attr.tabindex]="row.description ? 0 : null"
          [attr.aria-describedby]="row.description ? id + '-tip-' + i : null"
        >
          <dt class="lg-ledger__label">{{ row.label }}</dt>
          <span class="lg-ledger__leader" aria-hidden="true"></span>
          <dd class="lg-ledger__value">
            <span class="lg-ledger__num">{{ row.value }}</span>
          </dd>
          @if (row.sub) {
            <dd class="lg-ledger__sub">{{ row.sub }}</dd>
          }
          @if (row.description) {
            <div class="lg-ledger__tip" role="tooltip" [id]="id + '-tip-' + i">
              <strong>{{ row.label }}</strong>
              <p>{{ row.description }}</p>
              @if (row.tipMeta) {
                <span class="lg-ledger__tipmeta">{{ row.tipMeta }}</span>
              }
            </div>
          }
        </div>
      }
    </dl>
  `,
})
export class LgLedgerComponent {
  readonly title = input.required<string>();
  readonly rows = input.required<readonly LgLedgerRow[]>();
  protected readonly id = lgUniqueId('lg-ledger');
}
