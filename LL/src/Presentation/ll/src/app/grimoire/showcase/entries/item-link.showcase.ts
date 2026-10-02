import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgItemLinkComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-item-link-showcase',
  imports: [ShowcaseStoryDirective, LgItemLinkComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="In a sentence"
      notes="Bracketed, in its rarity colour, wrapping with its sentence. Link item names, never a whole sentence."
    >
      <p>
        You found
        <button lgItemLink rarity="Common">Frayed Satchel</button>,
        <button lgItemLink rarity="Rare">Ember Wolf Essence</button>,
        <button lgItemLink rarity="Epic">Soul Prism</button> and
        <button lgItemLink rarity="Legendary">Crown of Cinders</button>.
      </p>
    </ng-template>

    <ng-template
      scStory="Rarities"
      notes="The rarity and meta are also in the tip, on hover and focus, and the rarity is read after the name."
    >
      <div class="sc-col">
        <button lgItemLink rarity="Common" meta="Relic">Frayed Satchel</button>
        <button lgItemLink rarity="Uncommon" meta="Bow · Lv 9">
          Ashwood Bow
        </button>
        <button lgItemLink rarity="Rare" meta="Essence · Lv 12">
          Ember Wolf Essence
        </button>
        <button lgItemLink rarity="Epic" meta="Sword">Ember Fang</button>
        <button lgItemLink rarity="Unique">Prophecy Cache</button>
        <button lgItemLink rarity="Legendary" meta="Head">
          Crown of Cinders
        </button>
        <button lgItemLink rarity="Legacy" meta="Trinket">
          Ashenreach Heirloom
        </button>
      </div>
    </ng-template>

    <ng-template
      scStory="No rarity"
      notes="Without a rarity the name takes the text colour."
    >
      <p>You found <button lgItemLink>Iron Sabre</button>.</p>
    </ng-template>

    <ng-template
      scStory="Text only"
      notes="Without a press it is text, not a button."
    >
      <p>
        Reward:
        <span lgItemLink rarity="Epic" meta="Sword">Ember Fang</span>.
      </p>
    </ng-template>
  `,
})
export class ItemLinkShowcaseComponent extends ShowcaseEntryComponent {}

export const ITEM_LINK_SHOWCASE: ShowcaseEntry = {
  slug: 'item-link',
  name: 'ItemLink',
  tier: 'game',
  summary: 'An item named in text.',
  covers: ['LgItemLinkComponent'],
  readme: 'src/app/grimoire/game/item-link/README.md',
  component: ItemLinkShowcaseComponent,
};
