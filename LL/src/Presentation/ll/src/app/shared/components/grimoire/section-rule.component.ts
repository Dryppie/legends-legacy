import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  computed,
  contentChildren,
  input,
  viewChild,
} from '@angular/core';
import { LgSlotDirective, lgCx, lgHasSlot } from './grimoire-core';
import { lgCheckOrnamentRule } from './grimoire-ornament';

export type LgSectionRuleVariant = 'band' | 'ornament' | 'hairline';

/**
 * The dividers: a label band (Status, Biography), the lattice ornament (at most one per surface), or a hairline.
 * Extra content for a band goes in `lgSlot="aside"`.
 */
@Component({
  selector: 'lg-section-rule',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    @if (variant() === 'ornament') {
      <div #ornament class="lg-rule lg-rule--ornament" role="separator" [attr.aria-label]="label() || null">
        <span class="lg-rule__lattice" aria-hidden="true"></span>
        @if (label()) {
          <span class="lg-rule__label">{{ label() }}</span>
          <span class="lg-rule__lattice" aria-hidden="true"></span>
        }
      </div>
    } @else {
      <div [class]="classes()" role="separator" [attr.aria-label]="label() || null">
        @if (label()) {
          <span class="lg-rule__label">{{ label() }}</span>
        }
        @if (has('aside')) {
          <span class="lg-rule__aside"><ng-content select="[lgSlot=aside]" /></span>
        }
      </div>
    }
  `,
})
export class LgSectionRuleComponent {
  readonly variant = input<LgSectionRuleVariant>('band');
  readonly label = input<string>();
  readonly align = input<'start' | 'end'>('start');

  private readonly slots = contentChildren(LgSlotDirective);
  private readonly ornament = viewChild<ElementRef<HTMLElement>>('ornament');
  protected readonly classes = computed(() =>
    lgCx('lg-rule', 'lg-rule--' + (this.variant() || 'band'), this.align() === 'end' && 'lg-rule--end'),
  );

  constructor() {
    afterNextRender(() => lgCheckOrnamentRule(this.ornament()?.nativeElement ?? null));
  }

  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }
}
