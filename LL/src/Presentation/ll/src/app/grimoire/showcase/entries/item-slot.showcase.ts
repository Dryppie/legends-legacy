import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { LgItemSlotComponent, LgShortfall } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-item-slot-showcase',
  imports: [ShowcaseStoryDirective, LgItemSlotComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Common"
      notes="Rarity is carried by the edge, the name's colour and the code in the corner (C). It never glows."
    >
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          icon="inventory"
          rarity="Common"
          name="Frayed Satchel"
          meta="Relic"
        ></button>
        <button
          lgItemSlot
          icon="inventory"
          rarity="Common"
          name="Frayed Satchel"
          size="sm"
          [caption]="false"
        ></button>
      </div>
    </ng-template>

    <ng-template scStory="Uncommon" notes="Code UC.">
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          icon="colosseum"
          rarity="Uncommon"
          name="Ashwood Bow"
          meta="Bow · Lv 9"
        ></button>
        <button
          lgItemSlot
          icon="colosseum"
          rarity="Uncommon"
          name="Ashwood Bow"
          size="sm"
          [caption]="false"
        ></button>
      </div>
    </ng-template>

    <ng-template scStory="Rare" notes="Code R. The quantity sits bottom end.">
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          icon="essences"
          rarity="Rare"
          name="Ember Wolf Essence"
          meta="7 of 10"
          [quantity]="7"
        ></button>
        <button
          lgItemSlot
          icon="essences"
          rarity="Rare"
          name="Ember Wolf Essence"
          size="sm"
          [caption]="false"
          [quantity]="7"
        ></button>
      </div>
    </ng-template>

    <ng-template scStory="Epic" notes="Code E.">
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          icon="soulstones"
          rarity="Epic"
          name="Soul Prism"
          meta="Necklace"
        ></button>
        <button
          lgItemSlot
          icon="soulstones"
          rarity="Epic"
          name="Soul Prism"
          size="sm"
          [caption]="false"
        ></button>
      </div>
    </ng-template>

    <ng-template scStory="Unique" notes="Code U.">
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          icon="prophecies"
          rarity="Unique"
          name="Prophecy Cache"
          [quantity]="3"
        ></button>
        <button
          lgItemSlot
          icon="prophecies"
          rarity="Unique"
          name="Prophecy Cache"
          size="sm"
          [caption]="false"
          [quantity]="3"
        ></button>
      </div>
    </ng-template>

    <ng-template
      scStory="Legendary"
      notes="Code L. No halo, bloom or shimmer, even here."
    >
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          icon="achievements"
          rarity="Legendary"
          name="Crown of Cinders"
          meta="Head"
        ></button>
        <button
          lgItemSlot
          icon="achievements"
          rarity="Legendary"
          name="Crown of Cinders"
          size="sm"
          [caption]="false"
        ></button>
      </div>
    </ng-template>

    <ng-template scStory="Legacy" notes="Code LG.">
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          icon="legacy-ascension"
          rarity="Legacy"
          name="Ashenreach Heirloom"
          meta="Trinket"
        ></button>
        <button
          lgItemSlot
          icon="legacy-ascension"
          rarity="Legacy"
          name="Ashenreach Heirloom"
          size="sm"
          [caption]="false"
        ></button>
      </div>
    </ng-template>

    <ng-template
      scStory="Empty"
      notes="A dashed inner frame and the slot label; the label alone until the slot's icon is drawn."
    >
      <div lgItemSlot slotLabel="Off-hand" meta="Empty"></div>
    </ng-template>

    <ng-template
      scStory="Equipped"
      notes="The word leads the meta line, and the in-use square takes the bottom start corner."
    >
      <button
        lgItemSlot
        icon="combat-styles"
        rarity="Rare"
        name="Ashen Blade"
        state="equipped"
        meta="Main hand"
      ></button>
    </ng-template>

    <ng-template
      scStory="Attuned"
      notes="Like Equipped: the word in the meta line and the in-use square."
    >
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          icon="essences"
          rarity="Rare"
          name="Ember Wolf Essence"
          state="attuned"
          meta="Slot 1"
        ></button>
        <button
          lgItemSlot
          icon="essences"
          rarity="Rare"
          name="Ember Wolf Essence"
          state="attuned"
          size="sm"
          [caption]="false"
        ></button>
      </div>
    </ng-template>

    <ng-template scStory="Listed" notes="A word state leads the meta line.">
      <button
        lgItemSlot
        icon="inventory"
        rarity="Uncommon"
        name="Warded Pack"
        state="listed"
        meta="1,200 Cinders"
      ></button>
    </ng-template>

    <ng-template
      scStory="Claimable"
      notes="Claimable draws the attention diamond, top end."
    >
      <button
        lgItemSlot
        icon="prophecies"
        rarity="Unique"
        name="Prophecy Cache"
        state="claimable"
        [quantity]="3"
      ></button>
    </ng-template>

    <ng-template
      scStory="Not owned"
      notes="The art fades and the name drops to muted."
    >
      <div
        lgItemSlot
        icon="achievements"
        rarity="Epic"
        name="Crown of Ash"
        state="not-owned"
      ></div>
    </ng-template>

    <ng-template
      scStory="Undiscovered"
      notes="The art and the name are withheld, the rarity too; any hint goes in the meta line."
    >
      <div class="sc-row sc-row--start">
        <div lgItemSlot state="undiscovered" meta="Floors 10–20"></div>
        <div
          lgItemSlot
          state="undiscovered"
          rarity="Epic"
          icon="inventory"
          [ready]="true"
        ></div>
      </div>
    </ng-template>

    <ng-template
      scStory="Locked"
      notes="A dashed frame, the name disabled and the condition printed under it. It stays a focusable button, aria-disabled."
    >
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          slotLabel="Bag slot 9"
          state="locked"
          reason="Unlocks at level 20"
        ></button>
        <div
          lgItemSlot
          name="Tower Key"
          state="locked"
          reason="Clear floor 10"
        ></div>
      </div>
    </ng-template>

    <ng-template
      scStory="Unavailable"
      notes="Blocked with a reason, printed under the name."
    >
      <button
        lgItemSlot
        icon="prophecies"
        rarity="Unique"
        name="Prophecy Cache"
        state="unavailable"
        reason="Inventory full"
      ></button>
    </ng-template>

    <ng-template scStory="Restricted">
      <button
        lgItemSlot
        icon="combat-styles"
        rarity="Rare"
        name="Warden’s Plate"
        state="restricted"
        reason="Officers only"
      ></button>
    </ng-template>

    <ng-template
      scStory="Insufficient"
      notes="The reason is built from the shortfall. Without a caption it opens in the reason tip."
    >
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          icon="soulstones"
          rarity="Epic"
          name="Soul Prism"
          state="insufficient"
          [shortfall]="shortfall"
        ></button>
        <button
          lgItemSlot
          icon="essences"
          name="Ember Wolf Essence"
          [caption]="false"
          state="insufficient"
          [shortfall]="shortfallTwo"
        ></button>
      </div>
    </ng-template>

    <ng-template
      scStory="Cooldown"
      notes="The reason is built from the seconds left."
    >
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          icon="soulstones"
          name="Healing Draught"
          [quantity]="4"
          state="cooldown"
          [remaining]="12"
        ></button>
        <button
          lgItemSlot
          icon="essences"
          name="Ember Wolf Essence"
          [caption]="false"
          state="cooldown"
          [remaining]="3725"
        ></button>
      </div>
    </ng-template>

    <ng-template
      scStory="Favourite"
      notes="Until the ribbon is drawn, or when the in-use square holds the corner, the word goes in the meta line."
    >
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          icon="combat-styles"
          rarity="Rare"
          name="Ashen Blade"
          state="equipped"
          meta="Main hand"
          favourite
        ></button>
        <button
          lgItemSlot
          icon="inventory"
          rarity="Rare"
          name="Warded Pack"
          state="listed"
          meta="1,200 Cinders"
          favourite
        ></button>
      </div>
    </ng-template>

    <ng-template
      scStory="Ready"
      notes="The attention diamond, top end: true, or the words, which join the accessible name. A blocked slot takes none."
    >
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          icon="combat-styles"
          rarity="Epic"
          name="Ashen Blade"
          state="equipped"
          meta="Main hand"
          ready="Upgrade available"
        ></button>
        <button
          lgItemSlot
          icon="soulstones"
          rarity="Epic"
          name="Soul Prism"
          [ready]="true"
          [quantity]="2"
        ></button>
        <button
          lgItemSlot
          icon="inventory"
          rarity="Common"
          name="Frayed Satchel"
          state="locked"
          reason="Unlocks at level 20"
          [ready]="true"
        ></button>
      </div>
    </ng-template>

    <ng-template
      scStory="Selected"
      notes="A solid 2px arcana-glow ring, drawn flat. Press another slot to move it."
    >
      <div class="sc-row sc-row--start">
        @for (item of selectable; track item.id) {
          <button
            lgItemSlot
            [icon]="item.icon"
            [rarity]="item.rarity"
            [name]="item.name"
            [meta]="item.meta"
            [selected]="selectedId() === item.id"
            (click)="selectedId.set(item.id)"
          ></button>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="Sizes"
      notes="sm 64px, md 112px, and wide 176px for 16:9 profession cards; xs, 32px with the code alone, is a compact LoadoutSlot's frame."
    >
      <div class="sc-row sc-row--start">
        <div
          lgItemSlot
          icon="essences"
          rarity="Rare"
          name="Ember Wolf Essence"
          size="sm"
          [caption]="false"
        ></div>
        <div
          lgItemSlot
          icon="essences"
          rarity="Rare"
          name="Ember Wolf Essence"
          meta="7 of 10"
          size="md"
        ></div>
        <div
          lgItemSlot
          image="assets/cards/optimized/woodcutting.webp"
          name="Woodcutting"
          meta="Profession · Lv. 12"
          size="wide"
        ></div>
      </div>
    </ng-template>

    <ng-template
      scStory="No caption"
      notes="In a grid the marks carry what the caption would say: code top start, attention top end, ownership bottom start, quantity bottom end."
    >
      <div class="sc-row sc-row--start">
        <button
          lgItemSlot
          icon="essences"
          rarity="Rare"
          name="Ember Wolf Essence"
          [caption]="false"
          size="sm"
          state="attuned"
          ready="Can be raised"
        ></button>
        <button
          lgItemSlot
          icon="soulstones"
          rarity="Epic"
          name="Soul Prism"
          [caption]="false"
          size="sm"
          [ready]="true"
          [quantity]="2"
        ></button>
        <button
          lgItemSlot
          icon="inventory"
          rarity="Common"
          name="Frayed Satchel"
          [caption]="false"
          size="sm"
          state="locked"
          reason="Unlocks at level 20"
          [ready]="true"
        ></button>
        <button
          lgItemSlot
          icon="prophecies"
          name="Ember Hoard"
          [caption]="false"
          size="sm"
          state="locked"
          reason="Clear Floor 10 to unlock"
        ></button>
      </div>
    </ng-template>

    <ng-template
      scStory="Long name"
      notes="Names truncate; the frame keeps its width."
    >
      <button
        lgItemSlot
        icon="achievements"
        rarity="Legendary"
        name="Crown of the Ashenreach Cinder King"
        meta="Head · Lv 40"
      ></button>
    </ng-template>
  `,
})
export class ItemSlotShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly shortfall: LgShortfall[] = [
    { amount: 250, name: 'Cinders' },
  ];
  protected readonly shortfallTwo: LgShortfall[] = [
    { amount: 120, name: 'Cinders' },
    { amount: 2, name: 'Soulstones' },
  ];
  protected readonly selectable = [
    {
      id: 'satchel',
      icon: 'inventory',
      rarity: 'Common',
      name: 'Frayed Satchel',
      meta: 'Relic',
    },
    {
      id: 'wolf',
      icon: 'essences',
      rarity: 'Rare',
      name: 'Ember Wolf Essence',
      meta: '7 of 10',
    },
    {
      id: 'prism',
      icon: 'soulstones',
      rarity: 'Epic',
      name: 'Soul Prism',
      meta: 'Necklace',
    },
    {
      id: 'crown',
      icon: 'achievements',
      rarity: 'Legendary',
      name: 'Crown of Cinders',
      meta: 'Head',
    },
  ] as const;
  protected readonly selectedId = signal<string>('wolf');
}

export const ITEM_SLOT_SHOWCASE: ShowcaseEntry = {
  slug: 'item-slot',
  name: 'ItemSlot',
  tier: 'game',
  summary: 'The item frame.',
  covers: ['LgItemSlotComponent'],
  readme: 'src/app/grimoire/game/item-slot/README.md',
  component: ItemSlotShowcaseComponent,
};
