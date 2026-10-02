import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  booleanAttribute,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { lgFormatNumber, lgFormatShort } from '../../core/grimoire-format';
import { lgStateWarn } from '../../core/grimoire-states';
import { LgTooltipController } from '../../primitives/tooltip/tooltip.directive';

/**
 * The currency amount: Cinders or Soulstones with their art. Its host is the element: `<button lgCurrencyPill>` when a
 * press does something (your `(click)`, or `toggle`), `<span lgCurrencyPill>` to show it. An abbreviated pill (`short`)
 * is a button, so the full figure is one click, tap or key away: with `toggle` it switches between 12.5k and 12,480
 * itself; without, your (click) decides (the TopBar switches every pill). Screen readers hear the full amount, and the
 * tip shows it on hover and focus. A live amount reserves its widest width, so the TopBar does not reflow as it ticks.
 */
@Component({
  selector: 'button[lgCurrencyPill], span[lgCurrencyPill]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-currency',
    '[attr.type]': "isButton ? 'button' : null",
    '(click)': 'press()',
    '(mouseenter)': 'tip.enter()',
    '(mouseleave)': 'tip.leave()',
    '(focus)': 'tip.focus()',
    '(blur)': 'tip.blur()',
  },
  template: `@if (iconSrc(); as src) {<img [src]="src" alt="" width="18" height="18" class="lg-currency__icon" />}<span
      class="lg-currency__amount"
      [style.min-width]="reserveCh() + 'ch'"
      aria-hidden="true"
      >{{ amountText() }}</span
    ><span class="lg-currency__name" aria-hidden="true">{{ name() }}</span
    ><span class="lg-sr">{{ fullText() }}</span>`,
  styleUrl: './currency-pill.component.css',
})
export class LgCurrencyPillComponent {
  readonly name = input.required<string>();
  readonly amount = input.required<number>();
  /** assets/Currency/Cinders.webp for Cinders, Soulstones.webp for Soulstones. */
  readonly iconSrc = input<string>();
  /** Abbreviate (12.5k). Use a button host. */
  readonly short = input(false, { transform: booleanAttribute });
  /** On a button: a press switches between the short and the full figure. */
  readonly toggle = input(false, { transform: booleanAttribute });
  /** Characters of width to reserve from the start. */
  readonly reserve = input<number>(0);
  /** The tip's words, when they are not the full amount ("Toggle abbreviated currency values"). */
  readonly tooltip = input<string>();

  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  protected readonly isButton = this.el.tagName === 'BUTTON';
  private readonly full = signal(false);
  private widest = 0;
  protected readonly amountText = computed(() =>
    this.short() && !this.full() ? lgFormatShort(this.amount()) : lgFormatNumber(this.amount()),
  );
  protected readonly reserveCh = computed(() => {
    this.widest = Math.max(this.widest, this.amountText().length);
    return Math.max(this.widest, this.reserve() || 0);
  });
  protected readonly fullText = computed(() => `${lgFormatNumber(this.amount())} ${this.name()}`);
  /** The full amount is already its words for screen readers; other tip words are its description. */
  protected readonly tip = new LgTooltipController(this.el, () => this.tooltip() || this.fullText(), {
    description: () => this.tooltip() ?? '',
  });

  constructor() {
    effect(() => {
      if (this.short() && !this.isButton)
        lgStateWarn('currency-short', 'A short CurrencyPill is a button, so the full figure is a press away');
    });
  }

  protected press(): void {
    if (this.isButton && this.toggle()) this.full.update((f) => !f);
  }
}
