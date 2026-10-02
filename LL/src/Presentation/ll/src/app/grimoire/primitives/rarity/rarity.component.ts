import { ChangeDetectionStrategy, Component, ElementRef, booleanAttribute, computed, inject, input } from '@angular/core';
import { LG_RARITY_CODES, LgRarity } from '../../core/grimoire-core';
import { LgTooltipController } from '../tooltip/tooltip.directive';

/**
 * The rarity mark: the rarity's code (E for Epic) in the rarity's hue, and its name — for screen readers, after a
 * pause (", Epic"), and in the tip on hover. Rarity is never colour alone (Foundations · Colour, Registries); the seven
 * hues are the game's own and never change. `<lg-rarity rarity="Epic" />`
 */
@Component({
  selector: 'lg-rarity',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': "'lg-rarity lg-rarity--' + rarity().toLowerCase()",
    '(mouseenter)': 'tip() && tooltip.enter()',
    '(mouseleave)': 'tip() && tooltip.leave()',
  },
  template: `<span class="lg-rarity__code" aria-hidden="true">{{ code() }}</span><span class="lg-sr" [attr.id]="nameId() ?? null"
      >, {{ rarity() }}</span
    >`,
  styleUrl: './rarity.component.css',
})
export class LgRarityComponent {
  readonly rarity = input.required<LgRarity>();
  /** An id for the element that holds the spoken name, so a control's aria-labelledby can name it (a List row's action). */
  readonly nameId = input<string>();
  /** false: no tip on hover, when the mark sits in a control whose own tip matters more (a blocked ItemSlot). */
  readonly tip = input(true, { transform: booleanAttribute });

  protected readonly code = computed(() => LG_RARITY_CODES[this.rarity()] ?? this.rarity());
  protected readonly tooltip = new LgTooltipController(
    inject<ElementRef<HTMLElement>>(ElementRef).nativeElement,
    () => this.rarity(),
    // The name is already in the mark's own words for screen readers: no description.
    { description: () => '' },
  );
}
