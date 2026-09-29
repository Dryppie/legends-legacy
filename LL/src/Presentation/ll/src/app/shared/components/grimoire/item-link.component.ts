import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';
import { LgRarity } from './grimoire-core';

/** An item named inline in text, bracketed, in its rarity colour. */
@Component({
  selector: 'lg-item-link',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    @if (interactive()) {
      <button type="button" [class]="classes()" [attr.title]="tooltip()" (click)="activate.emit()">
        [<ng-content />]
      </button>
    } @else {
      <span [class]="classes()" [attr.title]="tooltip()">[<ng-content />]</span>
    }
  `,
})
export class LgItemLinkComponent {
  readonly rarity = input<LgRarity>();
  readonly meta = input<string>();
  readonly interactive = input(false, { transform: booleanAttribute });
  readonly activate = output<void>();

  protected readonly classes = computed(() => {
    const rarity = this.rarity();
    return rarity ? `lg-itemlink lg-itemlink--${rarity.toLowerCase()}` : 'lg-itemlink';
  });
  protected readonly tooltip = computed(() => {
    const parts = [this.rarity(), this.meta()].filter(Boolean);
    return parts.length ? parts.join(' · ') : null;
  });
}
