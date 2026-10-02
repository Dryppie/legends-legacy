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
        <lg-item-link rarity="Common" interactive>Frayed Satchel</lg-item-link>,
        <lg-item-link rarity="Rare" interactive>Ember Wolf Essence</lg-item-link
        >, <lg-item-link rarity="Epic" interactive>Soul Prism</lg-item-link> and
        <lg-item-link rarity="Legendary" interactive
          >Crown of Cinders</lg-item-link
        >.
      </p>
    </ng-template>

    <ng-template
      scStory="Rarities"
      notes="The rarity and meta are also in the title, and the rarity is read after the name."
    >
      <div class="sc-col">
        <lg-item-link rarity="Common" meta="Relic" interactive
          >Frayed Satchel</lg-item-link
        >
        <lg-item-link rarity="Uncommon" meta="Bow · Lv 9" interactive
          >Ashwood Bow</lg-item-link
        >
        <lg-item-link rarity="Rare" meta="Essence · Lv 12" interactive
          >Ember Wolf Essence</lg-item-link
        >
        <lg-item-link rarity="Epic" meta="Sword" interactive
          >Ember Fang</lg-item-link
        >
        <lg-item-link rarity="Unique" interactive>Prophecy Cache</lg-item-link>
        <lg-item-link rarity="Legendary" meta="Head" interactive
          >Crown of Cinders</lg-item-link
        >
        <lg-item-link rarity="Legacy" meta="Trinket" interactive
          >Ashenreach Heirloom</lg-item-link
        >
      </div>
    </ng-template>

    <ng-template
      scStory="No rarity"
      notes="Without a rarity the name takes the text colour."
    >
      <p>You found <lg-item-link interactive>Iron Sabre</lg-item-link>.</p>
    </ng-template>

    <ng-template
      scStory="Text only"
      notes="Without a press it is text, not a button."
    >
      <p>
        Reward:
        <lg-item-link rarity="Epic" meta="Sword">Ember Fang</lg-item-link>.
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
