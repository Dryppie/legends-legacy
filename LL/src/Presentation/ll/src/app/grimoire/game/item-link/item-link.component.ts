import { ChangeDetectionStrategy, Component, booleanAttribute, computed, input, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LgRarity } from '../../core/grimoire-core';

/** An item named in text: bracketed, in its rarity colour; the rarity is named for screen readers. */
@Component({
  selector: 'lg-item-link',
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <ng-template #body>[<ng-content />]@if (rarity(); as r) {<span class="lg-sr">, {{ r }}</span>}</ng-template>
    @if (interactive()) {
      <button type="button" [class]="classes()" [attr.title]="tooltip()" (click)="activate.emit()"
        ><ng-container [ngTemplateOutlet]="body"
      /></button>
    } @else {
      <span [class]="classes()" [attr.title]="tooltip()"><ng-container [ngTemplateOutlet]="body" /></span>
    }
  `,
})
export class LgItemLinkComponent {
  readonly rarity = input<LgRarity>();
  readonly meta = input<string>();
  /** Renders a button that emits `activate`. */
  readonly interactive = input(false, { transform: booleanAttribute });
  readonly activate = output<void>();

  protected readonly classes = computed(() => {
    const r = this.rarity();
    return r ? 'lg-itemlink lg-itemlink--' + r.toLowerCase() : 'lg-itemlink';
  });
  protected readonly tooltip = computed(() => {
    const r = this.rarity();
    return r ? r + (this.meta() ? ' · ' + this.meta() : '') : this.meta() || null;
  });
}
