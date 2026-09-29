import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
} from '@angular/core';
import { lgFormatNumber } from './grimoire-core';

export type LgMeterTone = 'hp' | 'sp' | 'xp';

/** A value against its maximum, always printed next to the bar. */
@Component({
  selector: 'lg-meter',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class]': 'hostClass()' },
  template: `
    @if (label() || showValue()) {
      <div class="lg-meter__head">
        @if (label()) {
          <span class="lg-meter__label">{{ label() }}</span>
        }
        @if (showValue()) {
          <span class="lg-meter__value">
            {{ format(value()) }}<span class="lg-meter__max"> / {{ format(max()) }}</span>
          </span>
        }
      </div>
    }
    <div
      class="lg-meter__track"
      role="meter"
      aria-valuemin="0"
      [attr.aria-valuemax]="max()"
      [attr.aria-valuenow]="value()"
      [attr.aria-label]="ariaLabel() ?? label() ?? tone()"
    >
      <div class="lg-meter__fill" [style.width.%]="percent()"></div>
    </div>
  `,
})
export class LgMeterComponent {
  readonly value = input.required<number>();
  readonly max = input.required<number>();
  readonly label = input<string>();
  readonly tone = input<LgMeterTone>('hp');
  /** thin: 4px line; bar: 10px framed bar for combat screens. */
  readonly size = input<'thin' | 'bar'>('thin');
  readonly showValue = input(true, { transform: booleanAttribute });
  readonly ariaLabel = input<string>();

  protected readonly format = lgFormatNumber;
  protected readonly percent = computed(() => {
    const max = this.max();
    if (!max || max <= 0) return 0;
    return (Math.max(0, Math.min(this.value(), max)) / max) * 100;
  });
  protected readonly hostClass = computed(
    () => `lg-meter lg-meter--${this.tone()} lg-meter--${this.size()}`,
  );
}
