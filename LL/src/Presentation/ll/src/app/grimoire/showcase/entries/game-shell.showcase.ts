import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import {
  LG_FOLIO,
  LG_PANEL,
  LgActivityComponent,
  LgButtonComponent,
  LgChronicleChannel,
  LgChronicleComponent,
  LgChronicleMessage,
  LgChroniclePosition,
  LgCurrencyPillComponent,
  LgGameShellComponent,
  LgKeyHint,
  LgKeyHintsComponent,
  LG_LIST,
  LgNavRailComponent,
  LgNavSection,
  LgObjectiveComponent,
  LgPageComponent,
  LgPageHeaderComponent,
  LgSlotDirective,
  LgStageComponent,
  LgTopBarComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

/** The game's rail. No routes: the items are plain `#` links, so a click stays on the showcase. */
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
      },
      {
        id: 'cinder-bazaar',
        title: 'Cinder Bazaar',
        description: 'List and buy items',
        icon: 'cinder-bazaar',
      },
    ],
  },
];

const CHANNELS: readonly LgChronicleChannel[] = [
  { id: 'all', label: 'All' },
  { id: 'general', label: 'General' },
  { id: 'trade', label: 'Trade' },
  { id: 'guild', label: 'Guild', unread: 3 },
  { id: 'whisper', label: 'Whispers', unread: 1 },
];

const MESSAGES: readonly LgChronicleMessage[] = [
  {
    id: 1,
    channel: 'general',
    channelLabel: 'General',
    time: '21:02',
    author: 'Maren',
    text: 'Anyone seen the Ember Knight near Shenic tonight?',
  },
  {
    id: 2,
    channel: 'system',
    channelLabel: 'System',
    kind: 'system',
    time: '21:03',
    text: 'The World Tower resets in 2 hours.',
  },
  {
    id: 3,
    channel: 'trade',
    channelLabel: 'Trade',
    time: '21:04',
    author: 'Oswin',
    text: 'WTS [Ember Wolf Essence] ×3 — 400 Cinders each',
  },
  {
    id: 4,
    channel: 'guild',
    channelLabel: 'Guild',
    time: '21:05',
    author: 'Kaelen',
    text: 'Raid at 22:00. Bring resist gear, the boss burns.',
  },
  {
    id: 5,
    channel: 'loot',
    channelLabel: 'Loot',
    kind: 'loot',
    time: '21:06',
    text: 'You found [Soul Prism] and 120 Cinders.',
  },
  {
    id: 6,
    channel: 'whisper',
    channelLabel: 'Whisper',
    time: '21:08',
    author: 'Kaelen',
    direction: 'from',
    text: 'Can you tank tonight?',
  },
];

const HINTS: readonly LgKeyHint[] = [
  { label: 'Back', key: 'Esc' },
  { label: 'Travel', key: '↵' },
];

@Component({
  selector: 'sc-game-shell-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgGameShellComponent,
    LgNavRailComponent,
    LgActivityComponent,
    LgTopBarComponent,
    LgObjectiveComponent,
    LgCurrencyPillComponent,
    ...LG_FOLIO,
    LgButtonComponent,
    LgKeyHintsComponent,
    LgChronicleComponent,
    LgPageComponent,
    LgPageHeaderComponent,
    ...LG_PANEL,
    ...LG_LIST,
    LgStageComponent,
    LgSlotDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Docked chat"
      notes="Rail, TopBar, the Page, the Folio and KeyHints; docked, the Chronicle sits under the Folio below 96rem."
      width="75rem"
      height="42rem"
      flush
    >
      <lg-game-shell height="42rem">
        <lg-nav-rail lgSlot="rail" [sections]="nav" activeId="world-map">
          <button
            lgActivity
            lgSlot="header"
            label="Engaged in Combat"
            remaining="00:12"
            [progress]="0.42"
          ></button>
        </lg-nav-rail>
        <lg-top-bar lgSlot="top" title="Aldric Vane" eyebrow="Lv. 42" showMenu>
          <lg-objective
            lgSlot="center"
            kicker="Quest"
            title="The First Hunt"
            objective="Defeat wolves in the Whispering Woods"
            [current]="3"
            [required]="5"
          />
          <button
            lgCurrencyPill
            name="Cinders"
            [amount]="12480"
            iconSrc="assets/game-emblems/cinders-v1-64.webp"
            short
            toggle
          ></button>
          <span
            lgCurrencyPill
            name="Soulstones"
            [amount]="36"
            iconSrc="assets/game-emblems/soulstones-v1-64.webp"
          ></span>
        </lg-top-bar>
        <lg-folio lgSlot="folio" eyebrow="Region" heading="Shenic">
          <lg-folio-lore
            >Temples older than the roads that lead to them.</lg-folio-lore
          >
          <lg-folio-actions>
            <button lgButton hotkey="↵">Travel</button>
          </lg-folio-actions>
        </lg-folio>
        <lg-key-hints lgSlot="hints" [hints]="hints" />
        <lg-chronicle
          lgSlot="chronicle"
          [channels]="channels"
          [messages]="messages"
          [composer]="false"
        />
        <lg-page label="World Map">
          <lg-page-header
            heading="World Map"
            eyebrow="World"
            icon="world-map"
            summary="Travel and explore."
          />
          <lg-panel flush>
            <lg-panel-header>
              <lg-panel-title>Regions</lg-panel-title>
            </lg-panel-header>
            <lg-list label="Regions" selected="Shenic">
              <li lgListRow name="Whispering Woods" meta="Levels 1–10">
                <button lgListRowAction></button>
              </li>
              <li lgListRow name="Shenic" meta="Levels 10–20">
                <button lgListRowAction></button>
              </li>
              <li lgListRow name="Ashen Wastes" meta="Levels 20–30">
                <button lgListRowAction></button>
              </li>
            </lg-list>
          </lg-panel>
        </lg-page>
      </lg-game-shell>
    </ng-template>

    <ng-template
      scStory="Floating chat"
      notes="Floating drawer: the Chronicle floats over the stage beside the Folio. Drag it by the grip; arrow keys nudge it."
      width="75rem"
      height="42rem"
      flush
    >
      <lg-game-shell
        height="42rem"
        chatLayout="floating"
        [(chroniclePosition)]="floatPosition"
      >
        <lg-nav-rail lgSlot="rail" [sections]="nav" activeId="world-map" />
        <lg-top-bar lgSlot="top" title="World Map" eyebrow="Shenic" showMenu>
          <button
            lgCurrencyPill
            name="Cinders"
            [amount]="12480"
            iconSrc="assets/game-emblems/cinders-v1-64.webp"
            short
            toggle
          ></button>
        </lg-top-bar>
        <lg-folio lgSlot="folio" eyebrow="Region" heading="Shenic">
          <lg-folio-lore
            >Temples older than the roads that lead to them.</lg-folio-lore
          >
          <lg-folio-actions>
            <button lgButton hotkey="↵">Travel</button>
          </lg-folio-actions>
        </lg-folio>
        <lg-key-hints lgSlot="hints" [hints]="hints" />
        <lg-chronicle
          lgSlot="chronicle"
          [channels]="channels"
          [messages]="messages"
          [composer]="false"
        />
        <lg-stage
          image="assets/backgrounds/optimized/temple.webp"
          label="World Map"
        />
      </lg-game-shell>
    </ng-template>

    <ng-template
      scStory="Chat collapsed"
      notes="Docked without a Folio the Chronicle owns the right column; collapsed, it is a vertical strip and the stage takes the space."
      width="75rem"
      height="42rem"
      flush
    >
      <lg-game-shell height="42rem">
        <lg-nav-rail lgSlot="rail" [sections]="nav" activeId="world-map" />
        <lg-top-bar lgSlot="top" title="Aldric Vane" eyebrow="Lv. 42">
          <button
            lgCurrencyPill
            name="Cinders"
            [amount]="12480"
            iconSrc="assets/game-emblems/cinders-v1-64.webp"
            short
            toggle
          ></button>
          <span
            lgCurrencyPill
            name="Soulstones"
            [amount]="36"
            iconSrc="assets/game-emblems/soulstones-v1-64.webp"
          ></span>
        </lg-top-bar>
        <lg-chronicle
          lgSlot="chronicle"
          [channels]="channels"
          [messages]="messages"
          [(open)]="chatOpen"
          [composer]="false"
        />
        <lg-page label="World Map">
          <lg-page-header
            heading="World Map"
            eyebrow="World"
            icon="world-map"
            summary="Travel and explore."
          />
          <lg-panel flush>
            <lg-panel-header>
              <lg-panel-title>Regions</lg-panel-title>
            </lg-panel-header>
            <lg-list label="Regions" selected="Shenic">
              <li lgListRow name="Whispering Woods" meta="Levels 1–10">
                <button lgListRowAction></button>
              </li>
              <li lgListRow name="Shenic" meta="Levels 10–20">
                <button lgListRowAction></button>
              </li>
              <li lgListRow name="Ashen Wastes" meta="Levels 20–30">
                <button lgListRowAction></button>
              </li>
            </lg-list>
          </lg-panel>
        </lg-page>
      </lg-game-shell>
    </ng-template>

    <ng-template
      scStory="With backdrop"
      notes="The game's backdrop at Level 0: the rail and the docked Chronicle take the translucent surface, and the TopBar becomes a band above the stage."
      width="75rem"
      height="42rem"
      flush
    >
      <lg-game-shell
        height="42rem"
        backdrop="assets/backgrounds/optimized/background.webp"
      >
        <lg-nav-rail lgSlot="rail" [sections]="nav" activeId="world-map">
          <button
            lgActivity
            lgSlot="header"
            label="Engaged in Combat"
            remaining="00:12"
            [progress]="0.42"
          ></button>
        </lg-nav-rail>
        <lg-top-bar lgSlot="top" title="Aldric Vane" eyebrow="Lv. 42" showMenu>
          <lg-objective
            lgSlot="center"
            kicker="Quest"
            title="The First Hunt"
            objective="Defeat wolves in the Whispering Woods"
            [current]="3"
            [required]="5"
          />
          <button
            lgCurrencyPill
            name="Cinders"
            [amount]="12480"
            iconSrc="assets/game-emblems/cinders-v1-64.webp"
            short
            toggle
          ></button>
          <span
            lgCurrencyPill
            name="Soulstones"
            [amount]="36"
            iconSrc="assets/game-emblems/soulstones-v1-64.webp"
          ></span>
        </lg-top-bar>
        <lg-chronicle
          lgSlot="chronicle"
          [channels]="channels"
          [messages]="messages"
          [composer]="false"
        />
        <lg-page label="World Map">
          <lg-page-header
            heading="World Map"
            eyebrow="World"
            icon="world-map"
            summary="Travel and explore."
          />
          <lg-panel flush>
            <lg-panel-header>
              <lg-panel-title>Regions</lg-panel-title>
            </lg-panel-header>
            <lg-list label="Regions" selected="Shenic">
              <li lgListRow name="Whispering Woods" meta="Levels 1–10">
                <button lgListRowAction></button>
              </li>
              <li lgListRow name="Shenic" meta="Levels 10–20">
                <button lgListRowAction></button>
              </li>
              <li lgListRow name="Ashen Wastes" meta="Levels 20–30">
                <button lgListRowAction></button>
              </li>
            </lg-list>
          </lg-panel>
        </lg-page>
      </lg-game-shell>
    </ng-template>

    <ng-template
      scStory="Compact rail"
      notes="The compact rail the player chose: the column narrows to the icons, with the compact Activity in a band as tall as the TopBar."
      width="75rem"
      height="42rem"
      flush
    >
      <lg-game-shell
        height="42rem"
        backdrop="assets/backgrounds/optimized/background.webp"
      >
        <lg-nav-rail
          lgSlot="rail"
          [sections]="nav"
          activeId="world-map"
          compact
        >
          <button
            lgActivity
            lgSlot="header"
            label="Engaged in Combat"
            short="Battling"
            remaining="00:12"
            [progress]="0.42"
            openLabel=""
            compact
          ></button>
        </lg-nav-rail>
        <lg-top-bar lgSlot="top" title="Aldric Vane" eyebrow="Lv. 42" showMenu>
          <button
            lgCurrencyPill
            name="Cinders"
            [amount]="12480"
            iconSrc="assets/game-emblems/cinders-v1-64.webp"
            short
            toggle
          ></button>
          <span
            lgCurrencyPill
            name="Soulstones"
            [amount]="36"
            iconSrc="assets/game-emblems/soulstones-v1-64.webp"
          ></span>
        </lg-top-bar>
        <lg-folio lgSlot="folio" eyebrow="Region" heading="Shenic">
          <lg-folio-lore
            >Temples older than the roads that lead to them.</lg-folio-lore
          >
          <lg-folio-actions>
            <button lgButton hotkey="↵">Travel</button>
          </lg-folio-actions>
        </lg-folio>
        <lg-chronicle
          lgSlot="chronicle"
          [channels]="channels"
          [messages]="messages"
          [composer]="false"
        />
        <lg-page label="World Map">
          <lg-page-header
            heading="World Map"
            eyebrow="World"
            icon="world-map"
            summary="Travel and explore."
          />
          <lg-panel flush>
            <lg-panel-header>
              <lg-panel-title>Regions</lg-panel-title>
            </lg-panel-header>
            <lg-list label="Regions" selected="Shenic">
              <li lgListRow name="Whispering Woods" meta="Levels 1–10">
                <button lgListRowAction></button>
              </li>
              <li lgListRow name="Shenic" meta="Levels 10–20">
                <button lgListRowAction></button>
              </li>
              <li lgListRow name="Ashen Wastes" meta="Levels 20–30">
                <button lgListRowAction></button>
              </li>
            </lg-list>
          </lg-panel>
        </lg-page>
      </lg-game-shell>
    </ng-template>

    <ng-template
      scStory="Narrow"
      notes="Under 60rem the rail becomes a drawer: the TopBar's menu button opens it, Escape or the scrim closes it."
      width="48rem"
      height="42rem"
      flush
    >
      <lg-game-shell height="42rem">
        <lg-nav-rail lgSlot="rail" [sections]="nav" activeId="world-map" />
        <lg-top-bar lgSlot="top" title="Aldric Vane" eyebrow="Lv. 42" showMenu>
          <lg-objective
            lgSlot="center"
            kicker="Quest"
            title="The First Hunt"
            objective="Defeat wolves in the Whispering Woods"
            [current]="3"
            [required]="5"
          />
          <button
            lgCurrencyPill
            name="Cinders"
            [amount]="12480"
            iconSrc="assets/game-emblems/cinders-v1-64.webp"
            short
            toggle
          ></button>
        </lg-top-bar>
        <lg-page label="World Map">
          <lg-page-header
            heading="World Map"
            eyebrow="World"
            icon="world-map"
            summary="Travel and explore."
          />
          <lg-panel flush>
            <lg-panel-header>
              <lg-panel-title>Regions</lg-panel-title>
            </lg-panel-header>
            <lg-list label="Regions" selected="Shenic">
              <li lgListRow name="Whispering Woods" meta="Levels 1–10">
                <button lgListRowAction></button>
              </li>
              <li lgListRow name="Shenic" meta="Levels 10–20">
                <button lgListRowAction></button>
              </li>
              <li lgListRow name="Ashen Wastes" meta="Levels 20–30">
                <button lgListRowAction></button>
              </li>
            </lg-list>
          </lg-panel>
        </lg-page>
      </lg-game-shell>
    </ng-template>
  `,
})
export class GameShellShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly nav = NAV;
  protected readonly channels = CHANNELS;
  protected readonly messages = MESSAGES;
  protected readonly hints = HINTS;
  protected readonly floatPosition = signal<LgChroniclePosition | null>(null);
  protected readonly chatOpen = signal(false);
}

export const GAME_SHELL_SHOWCASE: ShowcaseEntry = {
  slug: 'game-shell',
  name: 'GameShell',
  tier: 'shell',
  summary: 'The screen frame.',
  covers: ['LgGameShellComponent'],
  readme: 'src/app/grimoire/shell/game-shell/README.md',
  component: GameShellShowcaseComponent,
};
