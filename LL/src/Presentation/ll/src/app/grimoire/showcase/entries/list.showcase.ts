import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import {
  LG_LIST,
  LG_PANEL,
  LgButtonComponent,
  LgIconName,
  LgListRowState,
  LgPresenceComponent,
  LgRarity,
  LgTagComponent,
  LgTagTone,
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

/** A row of the scene list. */
interface Creature {
  id: string;
  name: string;
  tag?: string;
  tagTone?: LgTagTone;
  state?: LgListRowState;
  /** Locked: how it unlocks. */
  reason?: string;
  ready?: boolean | string;
}

const NAMES = [
  'Alpha Wolf',
  'Bloodfang Wolf',
  'Dire Wolf',
  'Ember Wolf',
  'Horned Wolf',
  'Blackjaw Spider',
  'Giant Spider',
  'Cave Bat',
  'Ember Knight',
];

/**
 * The creature list: Horned Wolf has a point to spend (the attention diamond), Blackjaw Spider is new,
 * Cave Bat is undiscovered and the current Creature Focus (its one Tag), Ember Knight is locked.
 */
const CREATURES: readonly Creature[] = NAMES.map((n): Creature => {
  if (n === 'Cave Bat')
    return {
      id: n,
      name: 'Undiscovered',
      tag: 'Creature Focus',
      tagTone: 'neutral',
    };
  return {
    id: n,
    name: n,
    tag: n === 'Blackjaw Spider' ? '+ New' : undefined,
    state: n === 'Ember Knight' ? 'locked' : undefined,
    reason: n === 'Ember Knight' ? 'Clear Floor 10 to unlock' : undefined,
    ready:
      n === 'Horned Wolf'
        ? '1 point to spend'
        : n === 'Ember Knight' || undefined,
  };
});

@Component({
  selector: 'sc-list-showcase',
  imports: [
    ShowcaseStoryDirective,
    ...LG_LIST,
    ...LG_PANEL,
    LgTagComponent,
    LgPresenceComponent,
    LgButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Inventory"
      notes="Standard rows in a flush Panel: thumbnail, name and rarity code, Tags and meta, quantity, amount. Each row's action covers it; press one to select it ([(selected)]). Amounts are sell prices in Cinders."
      width="32rem"
    >
      <lg-panel flush>
        <lg-panel-header>
          <lg-panel-title>Inventory</lg-panel-title>
          <span>5 of 60</span>
        </lg-panel-header>
        <lg-list label="Inventory" [(selected)]="selected">
          @for (it of items; track it.id) {
            <li
              lgListRow
              [key]="it.id"
              [name]="it.title"
              [rarity]="it.rarity"
              [icon]="it.icon"
              [meta]="it.meta"
              [quantity]="it.quantity"
              [amount]="it.value"
            >
              <button lgListRowAction></button>
              @if (it.equipped) {
                <lg-tag>Equipped</lg-tag>
              }
            </li>
          }
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Compact"
      notes="Guild members on 32px rows, one line, with Presence in the trailing region. Amounts are contribution this season."
      width="32rem"
    >
      <lg-panel flush density="compact">
        <lg-panel-header>
          <lg-panel-title>Guild members</lg-panel-title>
          <span>5 of 24</span>
        </lg-panel-header>
        <lg-list label="Guild members">
          @for (m of members; track m.name) {
            <li lgListRow [name]="m.name" [meta]="m.rank" [amount]="m.score">
              <button lgListRowAction></button>
              <lg-list-row-trailing>
                <lg-presence [online]="m.online" [lastSeen]="m.seen" compact />
              </lg-list-row-trailing>
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
      <lg-panel flush density="comfortable">
        <lg-panel-header>
          <lg-panel-title>Inventory</lg-panel-title>
        </lg-panel-header>
        <lg-list label="Inventory">
          @for (it of items.slice(0, 3); track it.id) {
            <li
              lgListRow
              [name]="it.title"
              [rarity]="it.rarity"
              [icon]="it.icon"
              [meta]="it.meta"
              [quantity]="it.quantity"
              [amount]="it.value"
            ></li>
          }
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Trailing action"
      notes="One small Button in the trailing region. In a row with an action, it leaves the tab order: Right moves into it, Left back."
      width="32rem"
    >
      <lg-panel flush>
        <lg-panel-header>
          <lg-panel-title>Inventory</lg-panel-title>
        </lg-panel-header>
        <lg-list label="Inventory">
          @for (it of items.slice(2); track it.id) {
            <li
              lgListRow
              [name]="it.title"
              [rarity]="it.rarity"
              [icon]="it.icon"
              [quantity]="it.quantity"
              [amount]="it.value"
            >
              <button lgListRowAction></button>
              <lg-list-row-trailing>
                <button lgButton size="sm">Sell</button>
              </lg-list-row-trailing>
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
      <lg-panel flush>
        <lg-panel-header>
          <lg-panel-title>Inventory</lg-panel-title>
        </lg-panel-header>
        <lg-list label="Inventory">
          <li
            lgListRow
            name="Dire Wolf Essence"
            rarity="Rare"
            icon="essences"
            [quantity]="3"
            amount="1,250"
          >
            <button lgListRowAction></button>
            <lg-list-row-trailing>
              <button lgButton size="sm">Sell</button>
            </lg-list-row-trailing>
          </li>
          <li
            lgListRow
            name="Soul Prism"
            rarity="Epic"
            icon="soulstones"
            meta="Sold"
            [quantity]="7"
            amount="2,100"
            muted
          >
            <button lgListRowAction></button>
            <lg-list-row-trailing>
              <button lgButton size="sm" disabled>Sell</button>
            </lg-list-row-trailing>
          </li>
          <li
            lgListRow
            name="Iron Sabre"
            rarity="Common"
            icon="colosseum"
            [quantity]="2"
            amount="85"
          >
            <button lgListRowAction></button>
            <lg-list-row-trailing>
              <button lgButton size="sm">Sell</button>
            </lg-list-row-trailing>
          </li>
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Zebra"
      notes="rhythm=zebra: every second row on row-stripe, no separators. For wide rows read across, such as rankings."
      width="32rem"
    >
      <lg-panel flush>
        <lg-panel-header>
          <lg-panel-title>Guild members</lg-panel-title>
        </lg-panel-header>
        <lg-list label="Guild members" rhythm="zebra">
          @for (m of members; track m.name) {
            <li
              lgListRow
              [name]="m.name"
              [meta]="m.rank"
              [amount]="m.score"
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
      <lg-panel flush>
        <lg-panel-header>
          <lg-panel-title>Guild members</lg-panel-title>
        </lg-panel-header>
        <lg-list label="Guild members" rhythm="spacing">
          @for (m of members; track m.name) {
            <li
              lgListRow
              [name]="m.name"
              [meta]="m.rank"
              [amount]="m.score"
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
      <lg-panel flush>
        <lg-panel-header>
          <lg-panel-title>Inventory</lg-panel-title>
        </lg-panel-header>
        <lg-list label="Inventory">
          <li
            lgListRow
            name="Ember Fang"
            rarity="Epic"
            icon="colosseum"
            [quantity]="1"
            amount="9,400"
          ></li>
          <li
            lgListRow
            name="Crown of Cinders"
            rarity="Legendary"
            icon="overview"
            amount="48,000"
          ></li>
          <li
            lgListRow
            name="Soul Prism"
            rarity="Epic"
            icon="soulstones"
            [quantity]="7"
            [amount]="null"
          ></li>
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Live value"
      notes="An amount that changes by itself takes the live-update mark. Press Refresh price to see it."
      width="32rem"
    >
      <lg-panel flush>
        <lg-panel-header>
          <lg-panel-title>Bazaar</lg-panel-title>
          <button lgButton="quiet" size="sm" (click)="price.set(price() + 50)">
            Refresh price
          </button>
        </lg-panel-header>
        <lg-list label="Bazaar">
          <li
            lgListRow
            name="Soul Prism"
            rarity="Epic"
            icon="soulstones"
            meta="Material"
            [amount]="price()"
            live
          ></li>
          <li
            lgListRow
            name="Dire Wolf Essence"
            rarity="Rare"
            icon="essences"
            meta="Essence · Lv 12"
            amount="1,250"
            live
          ></li>
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Selectable"
      notes="selectable: the rows are options of a listbox, for a choice with nothing to do on each row. Up and Down move the selection, wrapping."
      width="32rem"
    >
      <lg-panel flush>
        <lg-panel-header>
          <lg-panel-title>Loadout presets</lg-panel-title>
        </lg-panel-header>
        <lg-list label="Loadout presets" selectable [(selected)]="preset">
          <li lgListRow name="Hunter" key="hunter" meta="3 Essences"></li>
          <li lgListRow name="Warden" key="warden" meta="2 Essences"></li>
          <li
            lgListRow
            name="Reaper"
            key="reaper"
            state="locked"
            reason="Unlocks at level 30"
          ></li>
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Scene"
      notes="variant=scene, the browsable name list over stage art: 24px names on 40px rows; the selected one takes the bar and the wash. The list scrolls, and its ends fade. Up and Down move the selection."
      width="22rem"
      height="28rem"
    >
      <lg-list
        label="Creatures"
        variant="scene"
        selectable
        [(selected)]="active"
      >
        @for (c of creatures; track c.id) {
          <li
            lgListRow
            [key]="c.id"
            [name]="c.name"
            [state]="c.state"
            [reason]="c.reason"
            [ready]="c.ready"
          >
            @if (c.tag) {
              <lg-tag [tone]="c.tagTone || 'new'">{{ c.tag }}</lg-tag>
            }
          </li>
        }
      </lg-list>
    </ng-template>

    <ng-template
      scStory="Scene compact"
      notes="17px names on 32px rows, for a long roster."
      width="20rem"
      height="24rem"
    >
      <lg-list
        label="Creatures, compact"
        variant="scene"
        selectable
        density="compact"
        [(selected)]="active"
      >
        @for (c of creatures; track c.id) {
          <li
            lgListRow
            [key]="c.id"
            [name]="c.name"
            [state]="c.state"
            [reason]="c.reason"
            [ready]="c.ready"
          >
            @if (c.tag) {
              <lg-tag [tone]="c.tagTone || 'new'">{{ c.tag }}</lg-tag>
            }
          </li>
        }
      </lg-list>
    </ng-template>

    <ng-template
      scStory="Locked"
      notes="A locked row is never selected; hover or focus it, or move onto it with the arrows, to see how it unlocks."
      width="22rem"
      height="14rem"
    >
      <lg-list
        label="Creatures"
        variant="scene"
        selectable
        [(selected)]="lockedActive"
      >
        @for (c of locked; track c.id) {
          <li
            lgListRow
            [key]="c.id"
            [name]="c.name"
            [state]="c.state"
            [reason]="c.reason"
          >
            @if (c.tag) {
              <lg-tag [tone]="c.tagTone || 'new'">{{ c.tag }}</lg-tag>
            }
          </li>
        }
      </lg-list>
    </ng-template>

    <ng-template
      scStory="Ready"
      notes="ready draws the attention diamond at the row's end; a locked row takes none."
      width="22rem"
      height="14rem"
    >
      <lg-list label="Creatures" variant="scene" selectable selected="slime">
        @for (c of ready; track c.id) {
          <li
            lgListRow
            [key]="c.id"
            [name]="c.name"
            [state]="c.state"
            [reason]="c.reason"
            [ready]="c.ready"
          >
            @if (c.tag) {
              <lg-tag [tone]="c.tagTone || 'new'">{{ c.tag }}</lg-tag>
            }
          </li>
        }
      </lg-list>
    </ng-template>

    <ng-template
      scStory="No fade"
      notes="fade=false: the ends do not fade out."
      width="22rem"
      height="28rem"
    >
      <lg-list
        label="Creatures"
        variant="scene"
        selectable
        selected="Ember Wolf"
        [fade]="false"
      >
        @for (c of creatures; track c.id) {
          <li
            lgListRow
            [key]="c.id"
            [name]="c.name"
            [state]="c.state"
            [reason]="c.reason"
            [ready]="c.ready"
          >
            @if (c.tag) {
              <lg-tag [tone]="c.tagTone || 'new'">{{ c.tag }}</lg-tag>
            }
          </li>
        }
      </lg-list>
    </ng-template>

    <ng-template
      scStory="Narrow"
      notes="Names truncate; a Tag keeps its words."
      width="16rem"
      height="28rem"
    >
      <lg-list
        label="Creatures"
        variant="scene"
        selectable
        selected="Ember Wolf"
      >
        @for (c of creatures; track c.id) {
          <li
            lgListRow
            [key]="c.id"
            [name]="c.name"
            [state]="c.state"
            [reason]="c.reason"
            [ready]="c.ready"
          >
            @if (c.tag) {
              <lg-tag [tone]="c.tagTone || 'new'">{{ c.tag }}</lg-tag>
            }
          </li>
        }
      </lg-list>
    </ng-template>
  `,
})
export class ListShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly selected = signal<string | null>('wolf');
  protected readonly price = signal(2100);
  protected readonly preset = signal<string | null>('warden');
  protected readonly creatures = CREATURES;
  protected readonly active = signal<string | null>('Ember Wolf');
  protected readonly lockedActive = signal<string | null>('wolf');
  protected readonly locked: readonly Creature[] = [
    { id: 'wolf', name: 'Ember Wolf', tag: 'New' },
    { id: 'slime', name: 'Blue Slime' },
    {
      id: 'drake',
      name: 'Ash Drake',
      state: 'locked',
      reason: 'Defeat the Ash Drake in Shenic',
    },
    { id: 'imp', name: 'Cinder Imp', tag: 'Rare', tagTone: 'rare' },
  ];
  protected readonly ready: readonly Creature[] = [
    { id: 'wolf', name: 'Ember Wolf', tag: 'New', ready: '1 point to spend' },
    { id: 'slime', name: 'Blue Slime', ready: true },
    {
      id: 'drake',
      name: 'Ash Drake',
      state: 'locked',
      reason: 'Defeat the Ash Drake in Shenic',
      ready: true,
    },
    {
      id: 'bat',
      name: 'Undiscovered',
      tag: 'Creature Focus',
      tagTone: 'neutral',
    },
  ];
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
  summary: 'The list and its rows, and the scene name list.',
  covers: [
    'LgListComponent',
    'LgListRowComponent',
    'LgListRowActionComponent',
    'LgListRowTrailingComponent',
  ],
  readme: 'src/app/grimoire/components/list/README.md',
  component: ListShowcaseComponent,
};
