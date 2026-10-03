import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  computed,
  inject,
  input,
} from '@angular/core';
import { lgCx } from '../../core/grimoire-core';
import { lgCheckOrnamentRule } from '../../core/grimoire-ornament';

export type LgSectionRuleVariant = 'band' | 'ornament' | 'hairline';

/** The rule's aside, at its end: a count or a quiet link ("3 of 5"). */
@Component({
  selector: 'lg-section-rule-aside',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-rule__aside' },
  template: '<ng-content />',
  styles: `
    :host { margin-left: auto; font-size: var(--lg-text-caption); line-height: var(--lg-leading-caption); color: var(--lg-ink-muted); }
    :host-context(.lg-rule--end) { margin-left: 0; }
    :host-context(.lg-rule--hairline) { order: 2; margin-left: 0; }
  `,
})
export class LgSectionRuleAsideComponent {}

/**
 * The dividers: a label band (Status, Biography), the lattice ornament (at most one per surface), or a hairline.
 * Extra content for a band goes in an `lg-section-rule-aside`.
 */
@Component({
  selector: 'lg-section-rule',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
    role: 'separator',
    '[attr.aria-label]': 'label() || null',
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
      <ng-content select="lg-section-rule-aside" />
    }
  `,
  styleUrl: './section-rule.component.css',
})
export class LgSectionRuleComponent {
  readonly variant = input<LgSectionRuleVariant>('band');
  readonly label = input<string>();
  readonly align = input<'start' | 'end'>('start');

  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  protected readonly classes = computed(() =>
    this.variant() === 'ornament'
      ? 'lg-rule lg-rule--ornament'
      : lgCx('lg-rule', 'lg-rule--' + (this.variant() || 'band'), this.align() === 'end' && 'lg-rule--end'),
  );

  constructor() {
    afterNextRender(() => {
      if (this.variant() === 'ornament') lgCheckOrnamentRule(this.el);
    });
  }
}
