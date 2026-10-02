import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgItemLinkComponent, LgRarity, LgRarityComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-rarity-showcase',
  imports: [ShowcaseStoryDirective, LgRarityComponent, LgItemLinkComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Codes"
      notes="The seven rarities: the code in the rarity's hue, its name for screen readers and in the tip on hover. Never colour alone."
    >
      <div class="sc-row sc-row--start">
        @for (r of rarities; track r) {
          <div class="sc-cell">
            <lg-rarity [rarity]="r" />
            <p class="sc-cap">{{ r }}</p>
          </div>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="After a name"
      notes="Beside an item's name, as a List row sets it: the name in the hue, the code after it."
    >
      <div class="sc-col">
        @for (item of items; track item.name) {
          <p
            class="lg-type-body-compact"
            style="margin: 0; display: flex; gap: var(--lg-inline-sm); align-items: baseline"
          >
            <span lgItemLink [rarity]="item.rarity">{{ item.name }}</span>
            <lg-rarity [rarity]="item.rarity" />
          </p>
        }
      </div>
    </ng-template>
  `,
})
export class RarityShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly rarities: readonly LgRarity[] = [
    'Common',
    'Uncommon',
    'Rare',
    'Epic',
    'Unique',
    'Legendary',
    'Legacy',
  ];
  protected readonly items: readonly { name: string; rarity: LgRarity }[] = [
    { name: 'Frayed Satchel', rarity: 'Common' },
    { name: 'Ember Wolf Essence', rarity: 'Rare' },
    { name: 'Crown of Cinders', rarity: 'Legendary' },
  ];
}

export const RARITY_SHOWCASE: ShowcaseEntry = {
  slug: 'rarity',
  name: 'Rarity',
  tier: 'primitives',
  summary: 'The rarity mark: code, hue and name.',
  covers: ['LgRarityComponent'],
  readme: 'src/app/grimoire/primitives/rarity/README.md',
  component: RarityShowcaseComponent,
};
