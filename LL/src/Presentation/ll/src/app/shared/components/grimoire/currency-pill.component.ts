import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { lgFormatNumber, lgFormatShort } from './grimoire-core';

/** Cinders or Soulstones with their art and amount. */
@Component({
  selector: 'lg-currency-pill',
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <ng-template #body>
      @if (iconSrc(); as src) {
        <img class="lg-currency__icon" [src]="src" alt="" width="18" height="18" />
      }
      <span class="lg-currency__amount">{{ display() }}</span>
      <span class="lg-currency__name">{{ name() }}</span>
    </ng-template>
    @if (interactive()) {
      <button type="button" class="lg-currency" [attr.title]="tooltip()" (click)="activate.emit()">
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
  /** core/currencies/Coins.svg for Cinders, Diamonds.svg for Soulstones. */
  readonly iconSrc = input<string>();
  /** Abbreviate (12.5k); the full amount stays in the tooltip. */
  readonly short = input(false, { transform: booleanAttribute });
  /** Renders a button, e.g. to toggle the number format. */
  readonly interactive = input(false, { transform: booleanAttribute });
  readonly activate = output<void>();

  protected readonly display = computed(() =>
    this.short() ? lgFormatShort(this.amount()) : lgFormatNumber(this.amount()),
  );
  protected readonly tooltip = computed(
    () => `${lgFormatNumber(this.amount())} ${this.name()}`,
  );
}
