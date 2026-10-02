import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input } from '@angular/core';
import { LgRarity } from '../../core/grimoire-core';
import { LgTooltipController } from '../../primitives/tooltip/tooltip.directive';

/**
 * An item named in text: bracketed, in its rarity colour; the rarity is named for screen readers, and the rarity and
 * `meta` show in the tip on hover and focus. Its host is the element: `<button lgItemLink>` to open the item (a press
 * is the native `(click)`), `<a lgItemLink>` to go to it, `<span lgItemLink>` to name it.
 */
@Component({
  selector: 'button[lgItemLink], a[lgItemLink], span[lgItemLink]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
    '[attr.type]': "isButton ? 'button' : null",
    '(mouseenter)': 'tooltip.enter()',
    '(mouseleave)': 'tooltip.leave()',
    '(focus)': 'tooltip.focus()',
    '(blur)': 'tooltip.blur()',
  },
  template: `[<span class="lg-itemlink__name"><ng-content /></span>]@if (rarity(); as r) {<span class="lg-sr">, {{ r }}</span>}`,
  styleUrl: './item-link.component.css',
})
export class LgItemLinkComponent {
  readonly rarity = input<LgRarity>();
  readonly meta = input<string>();

  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  protected readonly isButton = this.el.tagName === 'BUTTON';

  protected readonly classes = computed(() => {
    const r = this.rarity();
    return r ? 'lg-itemlink lg-itemlink--' + r.toLowerCase() : 'lg-itemlink';
  });
  /** The rarity is already in its words for screen readers; the tip adds the meta, which is its description. */
  protected readonly tooltip = new LgTooltipController(
    this.el,
    () => {
      const r = this.rarity();
      return r ? r + (this.meta() ? ' · ' + this.meta() : '') : this.meta() || null;
    },
    { description: () => this.meta() ?? '' },
  );
}
