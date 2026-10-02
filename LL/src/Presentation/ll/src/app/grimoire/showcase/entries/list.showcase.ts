import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import {
  LgButtonComponent,
  LgIconName,
  LgListComponent,
  LgListRowComponent,
  LgPanelComponent,
  LgPresenceComponent,
  LgRarity,
  LgSlotDirective,
  LgTagComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

interface InventoryItem {
  id: string;
  title: string;
  rarity: LgRarity;
  icon: LgIconName;
  meta: string;
  quantity: number;
  value: string;
  equipped?: boolean;
}

interface Member {
  name: string;
  rank: string;
  score: string;
  online: boolean;
  seen?: string;
}

@Component({
  selector: 'sc-list-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgListComponent,
    LgListRowComponent,
    LgSlotDirective,
    LgPanelComponent,
    LgTagComponent,
    LgPresenceComponent,
    LgButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Inventory"
      notes="Standard rows in a flush Panel: thumbnail, name and rarity code, Tags and meta, quantity, value. Click a row to select it. Values are sell prices in Cinders."
      width="32rem"
    >
      <lg-panel title="Inventory" flush>
        <span lgSlot="aside">5 of 60</span>
        <lg-list label="Inventory">
          @for (it of items; track it.id) {
            <li
              lgListRow
              [title]="it.title"
              [rarity]="it.rarity"
              [icon]="it.icon"
              [meta]="it.meta"
              [quantity]="it.quantity"
              [value]="it.value"
              interactive
              [selected]="selected() === it.id"
              (activate)="selected.set(it.id)"
            >
              @if (it.equipped) {
                <lg-tag lgSlot="tags">Equipped</lg-tag>
              }
            </li>
          }
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Compact"
      notes="Guild members on 32px rows, one line, with Presence trailing. Values are contribution this season."
      width="32rem"
    >
      <lg-panel title="Guild members" flush density="compact">
        <span lgSlot="aside">5 of 24</span>
        <lg-list label="Guild members">
          @for (m of members; track m.name) {
            <li
              lgListRow
              [title]="m.name"
              [meta]="m.rank"
              [value]="m.score"
              interactive
            >
              <lg-presence
                lgSlot="trailing"
                [online]="m.online"
                [lastSeen]="m.seen"
                compact
              />
            </li>
          }
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Comfortable"
      notes="48px rows for a short list in a detail view: meta on its own line."
      width="32rem"
    >
      <lg-panel title="Inventory" flush density="comfortable">
        <lg-list label="Inventory">
          @for (it of items.slice(0, 3); track it.id) {
            <li
              lgListRow
              [title]="it.title"
              [rarity]="it.rarity"
              [icon]="it.icon"
              [meta]="it.meta"
              [quantity]="it.quantity"
              [value]="it.value"
            ></li>
          }
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Trailing action"
      notes="One small Button at the row's end. Trailing actions leave the tab order: Right moves into them, Left back."
      width="32rem"
    >
      <lg-panel title="Inventory" flush>
        <lg-list label="Inventory">
          @for (it of items.slice(2); track it.id) {
            <li
              lgListRow
              [title]="it.title"
              [rarity]="it.rarity"
              [icon]="it.icon"
              [quantity]="it.quantity"
              [value]="it.value"
              interactive
            >
              <button lgButton lgSlot="trailing" size="sm">Sell</button>
            </li>
          }
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Gone"
      notes="A row whose item left while the list was held: muted, its action disabled, a word in meta."
      width="32rem"
    >
      <lg-panel title="Inventory" flush>
        <lg-list label="Inventory">
          <li
            lgListRow
            title="Dire Wolf Essence"
            rarity="Rare"
            icon="essences"
            [quantity]="3"
            value="1,250"
            interactive
          >
            <button lgButton lgSlot="trailing" size="sm">Sell</button>
          </li>
          <li
            lgListRow
            title="Soul Prism"
            rarity="Epic"
            icon="soulstones"
            meta="Sold"
            [quantity]="7"
            value="2,100"
            muted
            interactive
          >
            <button lgButton lgSlot="trailing" size="sm" disabled>Sell</button>
          </li>
          <li
            lgListRow
            title="Iron Sabre"
            rarity="Common"
            icon="colosseum"
            [quantity]="2"
            value="85"
            interactive
          >
            <button lgButton lgSlot="trailing" size="sm">Sell</button>
          </li>
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Zebra"
      notes="rhythm=zebra: every second row on row-stripe, no separators. For wide rows read across, such as rankings."
      width="32rem"
    >
      <lg-panel title="Guild members" flush>
        <lg-list label="Guild members" rhythm="zebra">
          @for (m of members; track m.name) {
            <li
              lgListRow
              [title]="m.name"
              [meta]="m.rank"
              [value]="m.score"
            ></li>
          }
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Spacing"
      notes="rhythm=spacing: nothing drawn; the row height sets them apart. For short lists of about five rows."
      width="32rem"
    >
      <lg-panel title="Guild members" flush>
        <lg-list label="Guild members" rhythm="spacing">
          @for (m of members; track m.name) {
            <li
              lgListRow
              [title]="m.name"
              [meta]="m.rank"
              [value]="m.score"
            ></li>
          }
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Missing column values"
      notes="A column shows when any row uses it; a row with no quantity shows —."
      width="32rem"
    >
      <lg-panel title="Inventory" flush>
        <lg-list label="Inventory">
          <li
            lgListRow
            title="Ember Fang"
            rarity="Epic"
            icon="colosseum"
            [quantity]="1"
            value="9,400"
          ></li>
          <li
            lgListRow
            title="Crown of Cinders"
            rarity="Legendary"
            icon="overview"
            value="48,000"
          ></li>
          <li
            lgListRow
            title="Soul Prism"
            rarity="Epic"
            icon="soulstones"
            [quantity]="7"
            [value]="null"
          ></li>
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Live value"
      notes="A value that changes by itself takes the live-update mark. Press Refresh price to see it."
      width="32rem"
    >
      <lg-panel title="Bazaar" flush>
        <button
          lgButton="quiet"
          lgSlot="aside"
          size="sm"
          (click)="price.set(price() + 50)"
        >
          Refresh price
        </button>
        <lg-list label="Bazaar">
          <li
            lgListRow
            title="Soul Prism"
            rarity="Epic"
            icon="soulstones"
            meta="Material"
            [value]="price()"
            live
          ></li>
          <li
            lgListRow
            title="Dire Wolf Essence"
            rarity="Rare"
            icon="essences"
            meta="Essence · Lv 12"
            value="1,250"
            live
          ></li>
        </lg-list>
      </lg-panel>
    </ng-template>
  `,
})
export class ListShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly selected = signal('wolf');
  protected readonly price = signal(2100);
  protected readonly items: readonly InventoryItem[] = [
    {
      id: 'fang',
      title: 'Ember Fang',
      rarity: 'Epic',
      icon: 'colosseum',
      meta: 'Weapon · Sword · Lv 18',
      quantity: 1,
      value: '9,400',
      equipped: true,
    },
    {
      id: 'crown',
      title: 'Crown of Cinders',
      rarity: 'Legendary',
      icon: 'overview',
      meta: 'Helm · Lv 20',
      quantity: 1,
      value: '48,000',
    },
    {
      id: 'wolf',
      title: 'Dire Wolf Essence',
      rarity: 'Rare',
      icon: 'essences',
      meta: 'Essence · Lv 12',
      quantity: 3,
      value: '1,250',
    },
    {
      id: 'prism',
      title: 'Soul Prism',
      rarity: 'Epic',
      icon: 'soulstones',
      meta: 'Material',
      quantity: 7,
      value: '2,100',
    },
    {
      id: 'sabre',
      title: 'Iron Sabre',
      rarity: 'Common',
      icon: 'colosseum',
      meta: 'Weapon · Sword · Lv 4',
      quantity: 2,
      value: '85',
    },
  ];
  protected readonly members: readonly Member[] = [
    {
      name: 'Aldric Vane',
      rank: 'Guild master',
      score: '12,480',
      online: true,
    },
    { name: 'Maren', rank: 'Officer', score: '9,915', online: true },
    {
      name: 'Kaelen',
      rank: 'Officer',
      score: '8,204',
      online: false,
      seen: '2h ago',
    },
    {
      name: 'Tamsin',
      rank: 'Member',
      score: '4,310',
      online: false,
      seen: '1d ago',
    },
    { name: 'Pip', rank: 'Recruit', score: '0', online: true },
  ];
}

export const LIST_SHOWCASE: ShowcaseEntry = {
  slug: 'list',
  name: 'List',
  tier: 'components',
  summary: 'The list and its rows.',
  covers: ['LgListComponent', 'LgListRowComponent'],
  readme: 'src/app/grimoire/components/list/README.md',
  component: ListShowcaseComponent,
};
