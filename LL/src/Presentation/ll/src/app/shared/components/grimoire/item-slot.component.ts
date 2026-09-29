import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LG_RARITY_CODES, LgRarity, lgFormatNumber } from './grimoire-core';
import { LgIconComponent } from './icon.component';
import { LgIconName } from './grimoire-icons';

/** Framed square for an item, essence or equipment slot, edged in its rarity. */
@Component({
  selector: 'lg-item-slot',
  imports: [LgIconComponent, NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <ng-template #body>
      <span class="lg-slot__frame">
        @if (image(); as src) {
          <img class="lg-slot__img" [src]="src" alt="" />
        } @else if (icon(); as iconName) {
          <lg-icon [name]="iconName" [size]="26" />
        }
        @if (rarity(); as r) {
          <span class="lg-slot__code" [attr.title]="r">{{ codes[r] }}</span>
        }
        @if (quantity() > 1) {
          <span class="lg-slot__qty">×{{ format(quantity()) }}</span>
        }
      </span>
      @if (displayName() && caption()) {
        <span class="lg-slot__caption">
          <span class="lg-slot__name">{{ displayName() }}</span>
          @if (meta()) {
            <span class="lg-slot__meta">{{ meta() }}</span>
          }
        </span>
      }
    </ng-template>

    @if (interactive()) {
      <button
        type="button"
        [class]="classes()"
        [attr.aria-pressed]="selected()"
        [attr.aria-label]="accessibleLabel()"
        (click)="activate.emit()"
      >
        <ng-container [ngTemplateOutlet]="body" />
      </button>
    } @else {
      <div [class]="classes()"><ng-container [ngTemplateOutlet]="body" /></div>
    }
  `,
})
export class LgItemSlotComponent {
  readonly name = input<string>();
  /** Shown when empty: "Off-hand". */
  readonly slotLabel = input<string>();
  readonly meta = input<string>();
  readonly rarity = input<LgRarity>();
  readonly image = input<string>();
  readonly icon = input<LgIconName>();
  readonly quantity = input(1);
  readonly selected = input(false, { transform: booleanAttribute });
  readonly size = input<'sm' | 'md' | 'wide'>('md');
  /** false hides the caption under the frame. */
  readonly caption = input(true, { transform: booleanAttribute });
  readonly interactive = input(false, { transform: booleanAttribute });
  readonly activate = output<void>();

  protected readonly codes = LG_RARITY_CODES;
  protected readonly format = lgFormatNumber;
  protected readonly displayName = computed(() => this.name() ?? this.slotLabel());
  protected readonly classes = computed(() =>
    [
      'lg-slot',
      this.rarity() ? `lg-slot--${this.rarity()!.toLowerCase()}` : '',
      this.selected() ? 'is-selected' : '',
      !this.image() && !this.icon() ? 'is-empty' : '',
      this.size() !== 'md' ? `lg-slot--${this.size()}` : '',
    ]
      .filter(Boolean)
      .join(' '),
  );
  protected readonly accessibleLabel = computed(() =>
    [this.displayName(), this.rarity(), this.quantity() > 1 ? `x${this.quantity()}` : null]
      .filter(Boolean)
      .join(', '),
  );
}
