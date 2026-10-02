import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import {
  LgActivityComponent,
  LgButtonComponent,
  LgHeadingComponent,
  LgNavRailComponent,
  LgNavSection,
  LgSlotDirective,
} from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

/** The game's sections, each destination with its description (D-104). No routes: the items are plain `#` links. */
const NAV: readonly LgNavSection[] = [
  {
    label: 'Character',
    items: [
      {
        id: 'overview',
        title: 'Overview',
        description: 'Stats, vitals, loadout',
        icon: 'overview',
      },
      {
        id: 'inventory',
        title: 'Inventory',
        description: 'Items, gear, misc',
        icon: 'inventory',
        badge: 3,
        badgeLabel: '3 new items',
      },
      {
        id: 'essences',
        title: 'Essences',
        description: 'Archive, attune, ascend',
        icon: 'essences',
      },
      {
        id: 'combat-styles',
        title: 'Combat Styles',
        description: 'Train, refine, combine',
        icon: 'combat-styles',
      },
      {
        id: 'achievements',
        title: 'Achievements',
        description: 'Records and titles',
        icon: 'achievements',
      },
      {
        id: 'soulstones',
        title: 'Soulstones',
        description: 'Permanent upgrades',
        icon: 'soulstones',
      },
    ],
  },
  {
    label: 'World',
    items: [
      {
        id: 'world-map',
        title: 'World Map',
        description: 'Travel and explore',
        icon: 'world-map',
      },
      {
        id: 'world-tower',
        title: 'World Tower',
        description: 'Conquer the World Tower',
        icon: 'legacy-ascension',
      },
      {
        id: 'quests',
        title: 'Quests',
        description: 'Objectives and rewards',
        icon: 'quest-journal',
        badge: 1,
        badgeLabel: '1 quest ready to turn in',
      },
      {
        id: 'prophecies',
        title: 'Prophecies',
        description: 'Daily and weekly omens',
        icon: 'prophecies',
        ready: 'Daily cache ready',
      },
    ],
  },
  {
    label: 'City',
    items: [
      {
        id: 'guild',
        title: 'Guild',
        description: 'Guild headquarters',
        icon: 'guild',
      },
      {
        id: 'colosseum',
        title: 'Colosseum',
        description: 'Tournaments and battles',
        icon: 'colosseum',
        locked: true,
        reason: 'Unlocks at level 20',
        ready: true,
      },
      {
        id: 'cinder-bazaar',
        title: 'Cinder Bazaar',
        description: 'List and buy items',
        icon: 'cinder-bazaar',
      },
      {
        id: 'leaderboard',
        title: 'Leaderboard',
        description: 'Rankings and records',
        icon: 'leaderboard',
      },
    ],
  },
  {
    label: 'System',
    items: [
      {
        id: 'settings',
        title: 'Settings',
        description: 'Account and preferences',
        icon: 'settings',
      },
    ],
  },
];

/** The same sections without descriptions. */
const NAV_PLAIN: readonly LgNavSection[] = NAV.map((section) => ({
  label: section.label,
  items: section.items.map(({ description: _description, ...item }) => item),
}));

const NAV_MARKS: readonly LgNavSection[] = [
  {
    label: 'World',
    items: [
      {
        id: 'quests',
        title: 'Quests',
        icon: 'quest-journal',
        badge: 2,
        badgeLabel: '2 quests ready',
        ready: true,
      },
      {
        id: 'prophecies',
        title: 'Prophecies',
        icon: 'prophecies',
        ready: 'Daily cache ready',
      },
      {
        id: 'world-tower',
        title: 'World Tower',
        icon: 'legacy-ascension',
        ready: true,
        badgeLabel: 'Floor reward waiting',
      },
      {
        id: 'colosseum',
        title: 'Colosseum',
        icon: 'colosseum',
        locked: true,
        reason: 'Unlocks at level 20',
        ready: true,
      },
    ],
  },
];

const NAV_LOCKED: readonly LgNavSection[] = [
  {
    label: 'Character',
    items: [
      {
        id: 'overview',
        title: 'Overview',
        description: 'Stats, vitals, loadout',
        icon: 'overview',
      },
      {
        id: 'essences',
        title: 'Essences',
        description: 'Archive, attune, ascend',
        icon: 'essences',
        locked: true,
        reason: 'Unlocks after your first hunt',
      },
    ],
  },
  {
    label: 'City',
    items: [
      {
        id: 'guild',
        title: 'Guild',
        description: 'Guild headquarters',
        icon: 'guild',
        locked: true,
        reason: 'Unlocks at level 10',
      },
      {
        id: 'colosseum',
        title: 'Colosseum',
        description: 'Tournaments and battles',
        icon: 'colosseum',
        locked: true,
        reason: 'Unlocks at level 20',
      },
    ],
  },
];

const NAV_LONG: readonly LgNavSection[] = [
  {
    label: 'Character',
    items: [
      {
        id: 'overview',
        title: 'Overview',
        description: 'Stats, vitals, loadout, resistances and active effects',
        icon: 'overview',
      },
      {
        id: 'combat-styles',
        title: 'Combat Styles and Weapon Masteries',
        description: 'Train, refine, combine',
        icon: 'combat-styles',
        badge: 12,
        badgeLabel: '12 new styles',
      },
      {
        id: 'legacy',
        title: 'Legacy Ascension',
        description: 'Ascend and carry your legacy into the next life',
        icon: 'legacy-ascension',
        locked: true,
        reason: 'Unlocks once you reach level 50 and clear the World Tower',
      },
    ],
  },
];

@Component({
  selector: 'sc-nav-rail-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgNavRailComponent,
    LgActivityComponent,
    LgButtonComponent,
    LgHeadingComponent,
    LgSlotDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="With descriptions"
      notes="The game's rail: a short line under each title, gilt icons, and the current item's gilt bar and wash. Click an item to move there."
      width="16rem"
      height="54.5rem"
      flush
    >
      <lg-nav-rail
        [sections]="nav"
        [activeId]="active()"
        (navigate)="active.set($event)"
      />
    </ng-template>

    <ng-template
      scStory="Titles only"
      notes="Without descriptions, items are single 30px rows."
      width="16rem"
      height="41.5rem"
      flush
    >
      <lg-nav-rail [sections]="navPlain" activeId="world-map" />
    </ng-template>

    <ng-template
      scStory="Compact"
      notes="Icons only, for the compact rail the player chose: each title is its tooltip and accessible name."
      width="4rem"
      height="54.5rem"
      flush
    >
      <lg-nav-rail
        [sections]="nav"
        [activeId]="compactActive()"
        (navigate)="compactActive.set($event)"
        compact
      />
    </ng-template>

    <ng-template
      scStory="Badges and ready marks"
      notes="An item's end holds one mark: Locked, else the count badge, else the attention diamond."
      width="16rem"
      height="16rem"
      flush
    >
      <lg-nav-rail [sections]="navMarks" activeId="quests" />
    </ng-template>

    <ng-template
      scStory="Badges and ready marks, compact"
      notes="Compact: the badge sits on the icon's top end corner, the diamond on the item's."
      width="4rem"
      height="16rem"
      flush
    >
      <lg-nav-rail [sections]="navMarks" compact />
    </ng-template>

    <ng-template
      scStory="Locked"
      notes="Locked items stay in the Tab order with the lock marker; hover or focus shows how they unlock, and a click never navigates."
      width="16rem"
      height="18rem"
      flush
    >
      <lg-nav-rail [sections]="navLocked" activeId="overview" />
    </ng-template>

    <ng-template
      scStory="Locked, compact"
      notes="In compact the reason tip names the item too."
      width="4rem"
      height="18rem"
      flush
    >
      <lg-nav-rail [sections]="navLocked" activeId="overview" compact />
    </ng-template>

    <ng-template
      scStory="Current action in the header"
      notes="In the game the header holds the current action as an Activity."
      width="16rem"
      height="60rem"
      flush
    >
      <lg-nav-rail [sections]="nav" activeId="world-map">
        <lg-activity
          lgSlot="header"
          label="Engaged in Combat"
          remaining="00:12"
          [progress]="0.42"
          interactive
        />
      </lg-nav-rail>
    </ng-template>

    <ng-template
      scStory="Current action, compact"
      notes="The compact Activity heads the compact rail."
      width="4rem"
      height="54.5rem"
      flush
    >
      <lg-nav-rail [sections]="nav" activeId="world-map" compact>
        <lg-activity
          lgSlot="header"
          label="Engaged in Combat"
          short="Battling"
          remaining="00:12"
          [progress]="0.42"
          openLabel=""
          interactive
          compact
        />
      </lg-nav-rail>
    </ng-template>

    <ng-template
      scStory="Header and footer"
      notes="A wordmark in the header slot, extras in the footer."
      width="16rem"
      height="48rem"
      flush
    >
      <lg-nav-rail [sections]="navPlain" activeId="overview">
        <h2 lgHeading="section" lgSlot="header">Legend's Legacy</h2>
        <button lgButton="link" size="sm" lgSlot="footer">Patch notes</button>
      </lg-nav-rail>
    </ng-template>

    <ng-template
      scStory="Long text"
      notes="Titles and descriptions truncate rather than wrap."
      width="16rem"
      height="16rem"
      flush
    >
      <lg-nav-rail [sections]="navLong" activeId="overview" />
    </ng-template>
  `,
})
export class NavRailShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly nav = NAV;
  protected readonly navPlain = NAV_PLAIN;
  protected readonly navMarks = NAV_MARKS;
  protected readonly navLocked = NAV_LOCKED;
  protected readonly navLong = NAV_LONG;
  protected readonly active = signal('overview');
  protected readonly compactActive = signal('overview');
}

export const NAV_RAIL_SHOWCASE: ShowcaseEntry = {
  slug: 'nav-rail',
  name: 'NavRail',
  tier: 'shell',
  summary: 'The main navigation.',
  covers: ['LgNavRailComponent'],
  readme: 'design-system/components/NavRail/README.md',
  component: NavRailShowcaseComponent,
};
