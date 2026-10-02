import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { lgFormatNumber, lgFormatShort } from '../../core/grimoire-format';

/**
 * The currency amount: Cinders or Soulstones with their art. An abbreviated pill (`short`) is always a button: without
 * `interactive` it toggles between 12.5k and 12,480 itself, so the full figure is one click, tap or key away. Screen
 * readers hear the full amount. A live amount reserves its widest width, so the TopBar does not reflow as it ticks.
 */
@Component({
  selector: 'lg-currency-pill',
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    style: 'display: contents',
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
  },
  template: `
    <ng-template #body
      >@if (iconSrc(); as src) {<img [src]="src" alt="" width="18" height="18" class="lg-currency__icon" />}<span
        class="lg-currency__amount"
        [style.min-width]="reserveCh() + 'ch'"
        aria-hidden="true"
        >{{ amountText() }}</span
      ><span class="lg-currency__name" aria-hidden="true">{{ name() }}</span
      ><span class="lg-sr">{{ fullText() }}</span></ng-template
    >
    @if (isButton()) {
      <button type="button" class="lg-currency" [attr.title]="tooltip()" (click)="press()">
        <ng-container [ngTemplateOutlet]="body" />
      </button>
    } @else {
      <span class="lg-currency" [attr.title]="tooltip()"><ng-container [ngTemplateOutlet]="body" /></span>
    }
  `,
})
export class LgCurrencyPillComponent {
  readonly name = input.required<string>();
  readonly amount = input.required<number>();
  /** assets/Currency/Cinders.webp for Cinders, Soulstones.webp for Soulstones. */
  readonly iconSrc = input<string>();
  /** Abbreviate (12.5k). */
  readonly short = input(false, { transform: booleanAttribute });
  /** Characters of width to reserve from the start. */
  readonly reserve = input<number>(0);
  readonly title = input<string>();
  /** Renders a button that emits `activate`, instead of toggling the format itself. */
  readonly interactive = input(false, { transform: booleanAttribute });
  readonly activate = output<void>();

  private readonly full = signal(false);
  private widest = 0;
  private readonly ownToggle = computed(() => this.short() && !this.interactive());
  protected readonly isButton = computed(() => this.interactive() || this.ownToggle());
  protected readonly amountText = computed(() =>
    this.short() && !this.full() ? lgFormatShort(this.amount()) : lgFormatNumber(this.amount()),
  );
  protected readonly reserveCh = computed(() => {
    this.widest = Math.max(this.widest, this.amountText().length);
    return Math.max(this.widest, this.reserve() || 0);
  });
  protected readonly fullText = computed(() => `${lgFormatNumber(this.amount())} ${this.name()}`);
  protected readonly tooltip = computed(() => this.title() || this.fullText());

  protected press(): void {
    if (this.ownToggle()) this.full.update((f) => !f);
    else this.activate.emit();
  }
}
