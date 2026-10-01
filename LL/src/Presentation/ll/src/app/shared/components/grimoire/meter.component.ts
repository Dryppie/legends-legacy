import { ChangeDetectionStrategy, Component, booleanAttribute, computed, input } from '@angular/core';
import { lgCx } from './grimoire-core';
import { LG_NBSP, lgFormatNumber } from './grimoire-format';

export type LgMeterTone = 'hp' | 'sp' | 'xp';

/**
 * A value against its maximum, always printed next to the bar. The value reserves the width of max / max, so a live
 * value never moves the label or the bar. The fill scales to the value over duration-slow; `live` (combat playback)
 * follows ticks over duration-fast.
 */
@Component({
  selector: 'lg-meter',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <div [class]="classes()">
      @if (label() || showValue()) {
        <div class="lg-meter__head">
          @if (label()) {
            <span class="lg-meter__label">{{ label() }}</span>
          }
          @if (showValue()) {
            <span class="lg-meter__value" [style.min-width]="reserve() + 'ch'"
              >{{ format(value()) }}<span class="lg-meter__max">{{ nbsp }}/{{ nbsp }}{{ format(max()) }}</span
              >@if (unit()) {<span class="lg-unit">{{ nbsp }}{{ unit() }}</span>}</span
            >
          }
        </div>
      }
      <div
        class="lg-meter__track"
        role="meter"
        aria-valuemin="0"
        [attr.aria-valuemax]="max()"
        [attr.aria-valuenow]="value()"
        [attr.aria-valuetext]="valueText()"
        [attr.aria-label]="ariaLabel() || label() || tone()"
      >
        <div class="lg-meter__fill" [style.--lg-meter-p]="fraction()"></div>
      </div>
    </div>
  `,
})
export class LgMeterComponent {
  readonly value = input.required<number>();
  readonly max = input.required<number>();
  readonly label = input<string>();
  readonly tone = input<LgMeterTone>('hp');
  /** thin: a line; bar: a framed bar for combat screens. */
  readonly size = input<'thin' | 'bar'>('thin');
  readonly showValue = input(true, { transform: booleanAttribute });
  /** A word unit after the numbers ("HP"). */
  readonly unit = input<string>();
  /** Follows rapid ticks (combat playback) over duration-fast. */
  readonly live = input(false, { transform: booleanAttribute });
  readonly ariaLabel = input<string>();

  protected readonly format = lgFormatNumber;
  protected readonly nbsp = LG_NBSP;
  protected readonly fraction = computed(() => {
    const max = this.max() || 0;
    const v = Math.max(0, Math.min(this.value() || 0, max));
    const pct = max ? (v / max) * 100 : 0;
    return String(pct / 100);
  });
  protected readonly reserve = computed(
    () => lgFormatNumber(this.max() || 0).length * 2 + 3 + (this.unit() ? String(this.unit()).length + 1 : 0),
  );
  protected readonly valueText = computed(
    () => `${lgFormatNumber(this.value())} of ${lgFormatNumber(this.max() || 0)}${this.unit() ? ' ' + this.unit() : ''}`,
  );
  protected readonly classes = computed(() =>
    lgCx('lg-meter', 'lg-meter--' + this.tone(), 'lg-meter--' + (this.size() || 'thin'), this.live() && 'is-live'),
  );
}
