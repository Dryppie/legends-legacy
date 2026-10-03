import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  input,
  model,
  signal,
} from '@angular/core';
import {
  LgActivityComponent,
  LgButtonComponent,
  LgHeadingComponent,
  LgIconName,
  LgNavItemComponent,
  LgNavRailComponent,
  LgNavRailFooterComponent,
  LgNavRailHeaderComponent,
  LgNavSectionComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

/** A destination as the stories hold it: a game's rail adapter builds the same from its own data. */
export interface ScNavItem {
  id: string;
  title: string;
  description?: string;
  icon?: LgIconName;
  badge?: string | number;
  badgeLabel?: string;
  locked?: boolean;
  reason?: string;
  ready?: boolean | string;
}

export interface ScNavSection {
  label: string;
  items: readonly ScNavItem[];
}

/** The game's sections, each destination with its description (D-104). No routes: the items are plain `#` links. */
export const SC_NAV: readonly ScNavSection[] = [
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
export const SC_NAV_PLAIN: readonly ScNavSection[] = SC_NAV.map((section) => ({
  label: section.label,
  items: section.items.map(({ description: _description, ...item }) => item),
}));

const NAV_MARKS: readonly ScNavSection[] = [
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

const NAV_LOCKED: readonly ScNavSection[] = [
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

const NAV_LONG: readonly ScNavSection[] = [
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

/**
 * A rail composed from the stories' data, as a game's rail adapter composes it (rail-grimoire): a section per group,
 * an `a[lgNavItem]` per destination. There are no routes here, so the items are `#` links, a press moves `current`, and
 * the current item's `aria-current` is set by hand, where the game uses `routerLinkActive`. The header and footer pass
 * through.
 */
@Component({
  selector: 'sc-nav-rail',
  imports: [LgNavRailComponent, LgNavSectionComponent, LgNavItemComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: block; height: 100%' },
  template: `
    <lg-nav-rail [compact]="compact()">
      <ng-content
        select="lg-nav-rail-header"
        ngProjectAs="lg-nav-rail-header"
      />
      @for (section of sections(); track section.label) {
        <lg-nav-section [label]="section.label">
          @for (item of section.items; track item.id) {
            <a
              lgNavItem
              [attr.href]="item.locked ? null : '#'"
              [attr.aria-current]="item.id === current() ? 'page' : null"
              [icon]="item.icon"
              [description]="item.description"
              [badge]="item.badge"
              [badgeLabel]="item.badgeLabel"
              [ready]="item.ready"
              [locked]="!!item.locked"
              [reason]="item.reason"
              (click)="$event.preventDefault(); current.set(item.id)"
              >{{ item.title }}</a
            >
          }
        </lg-nav-section>
      }
      <ng-content
        select="lg-nav-rail-footer"
        ngProjectAs="lg-nav-rail-footer"
      />
    </lg-nav-rail>
  `,
})
export class ScNavRailComponent {
  readonly sections = input.required<readonly ScNavSection[]>();
  /** The current destination's id. */
  readonly current = model<string>();
  readonly compact = input(false, { transform: booleanAttribute });
}

@Component({
  selector: 'sc-nav-rail-showcase',
  imports: [
    ShowcaseStoryDirective,
    ScNavRailComponent,
    LgNavRailHeaderComponent,
    LgNavRailFooterComponent,
    LgActivityComponent,
    LgButtonComponent,
    LgHeadingComponent,
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
      <sc-nav-rail [sections]="nav" [(current)]="active" />
    </ng-template>

    <ng-template
      scStory="Titles only"
      notes="Without descriptions, items are single 30px rows."
      width="16rem"
      height="41.5rem"
      flush
    >
      <sc-nav-rail [sections]="navPlain" current="world-map" />
    </ng-template>

    <ng-template
      scStory="Compact"
      notes="Icons only, for the compact rail the player chose: each title is its tooltip and accessible name."
      width="4rem"
      height="54.5rem"
      flush
    >
      <sc-nav-rail [sections]="nav" [(current)]="compactActive" compact />
    </ng-template>

    <ng-template
      scStory="Badges and ready marks"
      notes="An item's end holds one mark: Locked, else the count badge, else the attention diamond."
      width="16rem"
      height="16rem"
      flush
    >
      <sc-nav-rail [sections]="navMarks" current="quests" />
    </ng-template>

    <ng-template
      scStory="Badges and ready marks, compact"
      notes="Compact: the badge sits on the icon's top end corner, the diamond on the item's."
      width="4rem"
      height="16rem"
      flush
    >
      <sc-nav-rail [sections]="navMarks" compact />
    </ng-template>

    <ng-template
      scStory="Locked"
      notes="Locked items stay in the Tab order with the lock marker; hover or focus shows how they unlock, and a click never navigates."
      width="16rem"
      height="18rem"
      flush
    >
      <sc-nav-rail [sections]="navLocked" current="overview" />
    </ng-template>

    <ng-template
      scStory="Locked, compact"
      notes="In compact the reason tip names the item too."
      width="4rem"
      height="18rem"
      flush
    >
      <sc-nav-rail [sections]="navLocked" current="overview" compact />
    </ng-template>

    <ng-template
      scStory="Current action in the header"
      notes="In the game the header holds the current action as an Activity."
      width="16rem"
      height="60rem"
      flush
    >
      <sc-nav-rail [sections]="nav" current="world-map">
        <lg-nav-rail-header>
          <button
            lgActivity
            label="Engaged in Combat"
            remaining="00:12"
            [progress]="0.42"
          ></button>
        </lg-nav-rail-header>
      </sc-nav-rail>
    </ng-template>

    <ng-template
      scStory="Current action, compact"
      notes="The compact Activity heads the compact rail."
      width="4rem"
      height="54.5rem"
      flush
    >
      <sc-nav-rail [sections]="nav" current="world-map" compact>
        <lg-nav-rail-header>
          <button
            lgActivity
            label="Engaged in Combat"
            short="Battling"
            remaining="00:12"
            [progress]="0.42"
            openLabel=""
            compact
          ></button>
        </lg-nav-rail-header>
      </sc-nav-rail>
    </ng-template>

    <ng-template
      scStory="Header and footer"
      notes="A wordmark in the header slot, extras in the footer."
      width="16rem"
      height="48rem"
      flush
    >
      <sc-nav-rail [sections]="navPlain" current="overview">
        <lg-nav-rail-header>
          <h2 lgHeading="section">Legend's Legacy</h2>
        </lg-nav-rail-header>
        <lg-nav-rail-footer>
          <button lgButton="link" size="sm">Patch notes</button>
        </lg-nav-rail-footer>
      </sc-nav-rail>
    </ng-template>

    <ng-template
      scStory="Long text"
      notes="Titles and descriptions truncate rather than wrap."
      width="16rem"
      height="16rem"
      flush
    >
      <sc-nav-rail [sections]="navLong" current="overview" />
    </ng-template>
  `,
})
export class NavRailShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly nav = SC_NAV;
  protected readonly navPlain = SC_NAV_PLAIN;
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
  covers: [
    'LgNavRailComponent',
    'LgNavSectionComponent',
    'LgNavItemComponent',
    'LgNavRailHeaderComponent',
    'LgNavRailFooterComponent',
  ],
  readme: 'src/app/grimoire/shell/nav-rail/README.md',
  component: NavRailShowcaseComponent,
};
