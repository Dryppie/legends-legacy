import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LgDeltaComponent, LgDeltaPolarity } from './delta.component';
import { lgFormatNumber, lgMissing, lgPlainValue } from './grimoire-format';

/**
 * The compact stat: a label and a value, with an optional change shown by a Delta. The change's colour comes from
 * `deltaPolarity` (the game's rules), never from its sign. Lay tiles out in `<div class="lg-statgrid">`.
 */
@Component({
  selector: 'lg-stat-tile',
  imports: [LgDeltaComponent, NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <ng-template #valueTpl
      ><span class="lg-stattile__value"
        >{{ shown() }}@if (suffix() && !missing()) {<small class="lg-unit">{{ suffix() }}</small>}</span
      ></ng-template
    >
    <div class="lg-stattile" [class.has-delta]="delta() != null">
      <span class="lg-stattile__label">{{ label() }}</span>
      @if (delta() != null) {
        <span class="lg-stattile__figure"
          ><ng-container [ngTemplateOutlet]="valueTpl" /><lg-delta
            extraClass="lg-stattile__delta"
            [direction]="direction()"
            [polarity]="polarity()"
            [value]="deltaValue()"
        /></span>
      } @else {
        <ng-container [ngTemplateOutlet]="valueTpl" />
      }
    </div>
  `,
})
export class LgStatTileComponent {
  readonly label = input.required<string>();
  readonly value = input.required<number | string | null>();
  readonly suffix = input<string>();
  /** The change since the last look; 0 shows ±0. */
  readonly delta = input<number | null>();
  /** Whether the change helps the player: better, worse or neutral. */
  readonly deltaPolarity = input<LgDeltaPolarity>('neutral');
  /** The change as shown, when it differs from the number ("1.2s"). */
  readonly deltaText = input<string>();

  protected readonly missing = computed(() => lgMissing(this.value()));
  protected readonly shown = computed(() => lgPlainValue(this.value()));
  protected readonly direction = computed(() => {
    const d = this.delta() ?? 0;
    return d > 0 ? 'up' : d < 0 ? 'down' : 'none';
  });
  protected readonly polarity = computed<LgDeltaPolarity>(() =>
    this.delta() === 0 ? 'neutral' : this.deltaPolarity() || 'neutral',
  );
  protected readonly deltaValue = computed(
    () => this.deltaText() ?? lgFormatNumber(Math.abs(this.delta() ?? 0)) + (this.suffix() || ''),
  );
}
