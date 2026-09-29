import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

export type LgSectionRuleVariant = 'band' | 'ornament' | 'hairline';

/**
 * Divider: a label band (Status, Biography), the diamond-chain ornament, or a hairline.
 * Project extra content into `lgSlot="aside"`.
 */
@Component({
  selector: 'lg-section-rule',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'hostClass()',
    role: 'separator',
    '[attr.aria-label]': 'label() ?? null',
  },
  template: `
    @if (variant() === 'ornament') {
      <span class="lg-rule__lattice" aria-hidden="true"></span>
      @if (label()) {
        <span class="lg-rule__label">{{ label() }}</span>
        <span class="lg-rule__lattice" aria-hidden="true"></span>
      }
    } @else {
      @if (label()) {
        <span class="lg-rule__label">{{ label() }}</span>
      }
      <span class="lg-rule__aside"><ng-content select="[lgSlot=aside]" /></span>
    }
  `,
})
export class LgSectionRuleComponent {
  readonly variant = input<LgSectionRuleVariant>('band');
  readonly label = input<string>();
  readonly align = input<'start' | 'end'>('start');
  protected readonly hostClass = computed(() =>
    [
      'lg-rule',
      `lg-rule--${this.variant()}`,
      this.align() === 'end' ? 'lg-rule--end' : '',
    ]
      .filter(Boolean)
      .join(' '),
  );
}
