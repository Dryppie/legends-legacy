import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { LG_TABS } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

/** A tab of the stories: its key, its words and a count. */
interface Tab {
  key: string;
  label: string;
  count?: number;
}

@Component({
  selector: 'sc-tabs-showcase',
  imports: [ShowcaseStoryDirective, ...LG_TABS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Primary"
      notes="What you are browsing. The selected tab is a tile block with an arcana underline. Left and Right move and select; Home and End jump."
    >
      <lg-tabs label="Archive" [(selected)]="section">
        @for (tab of archive; track tab.key) {
          <button lgTab [key]="tab.key">{{ tab.label }}</button>
        }
      </lg-tabs>
    </ng-template>

    <ng-template
      scStory="Secondary"
      notes="How it is filtered: label-sized capitals, selected in ink with the same underline, the count in arcana."
    >
      <lg-tabs label="Filter" level="secondary" [(selected)]="filter">
        @for (tab of filters; track tab.key) {
          <button lgTab [key]="tab.key" [count]="tab.count">
            {{ tab.label }}
          </button>
        }
      </lg-tabs>
    </ng-template>

    <ng-template
      scStory="Two levels"
      notes="Primary over secondary; never three levels."
    >
      <div class="sc-col">
        <lg-tabs label="Archive" [(selected)]="section">
          @for (tab of archive; track tab.key) {
            <button lgTab [key]="tab.key">{{ tab.label }}</button>
          }
        </lg-tabs>
        <lg-tabs label="Filter" level="secondary" [(selected)]="filter">
          @for (tab of filters; track tab.key) {
            <button lgTab [key]="tab.key" [count]="tab.count">
              {{ tab.label }}
            </button>
          }
        </lg-tabs>
      </div>
    </ng-template>

    <ng-template
      scStory="Compact"
      notes="32px tabs, for a table or list header."
    >
      <div class="sc-col">
        <lg-tabs
          label="Archive, compact"
          density="compact"
          [(selected)]="section"
        >
          @for (tab of archive; track tab.key) {
            <button lgTab [key]="tab.key">{{ tab.label }}</button>
          }
        </lg-tabs>
        <lg-tabs
          label="Filter, compact"
          level="secondary"
          density="compact"
          [(selected)]="filter"
        >
          @for (tab of filters; track tab.key) {
            <button lgTab [key]="tab.key" [count]="tab.count">
              {{ tab.label }}
            </button>
          }
        </lg-tabs>
      </div>
    </ng-template>

    <ng-template
      scStory="Densities"
      notes="A primary tab is the control height, 44, 40 or 32px; a secondary tab is 32, 28 or 24px. The text keeps its style."
    >
      <div class="sc-col">
        @for (d of densities; track d) {
          <div class="sc-cell">
            <p class="sc-cap">{{ d }}</p>
            <lg-tabs
              [label]="'Archive, ' + d"
              [density]="d"
              selected="creatures"
            >
              @for (tab of archive; track tab.key) {
                <button lgTab [key]="tab.key">{{ tab.label }}</button>
              }
            </lg-tabs>
            <lg-tabs
              [label]="'Filter, ' + d"
              level="secondary"
              [density]="d"
              selected="all"
            >
              @for (tab of filters; track tab.key) {
                <button lgTab [key]="tab.key" [count]="tab.count">
                  {{ tab.label }}
                </button>
              }
            </lg-tabs>
          </div>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="Counts"
      notes="A count after the words, 0 included; on the selected tab it takes the tab's colour."
    >
      <div class="sc-col">
        <lg-tabs label="Inventory" level="secondary" selected="all">
          @for (tab of items; track tab.key) {
            <button lgTab [key]="tab.key" [count]="tab.count">
              {{ tab.label }}
            </button>
          }
        </lg-tabs>
        <lg-tabs label="Inventory" level="secondary" selected="armor">
          @for (tab of items; track tab.key) {
            <button lgTab [key]="tab.key" [count]="tab.count">
              {{ tab.label }}
            </button>
          }
        </lg-tabs>
      </div>
    </ng-template>

    <ng-template
      scStory="Overflow"
      notes="Primary tabs scroll sideways inside the strip when they no longer fit; secondary tabs wrap."
      width="20rem"
    >
      <div class="sc-col">
        <lg-tabs label="Archive" selected="regions">
          @for (tab of archive; track tab.key) {
            <button lgTab [key]="tab.key">{{ tab.label }}</button>
          }
        </lg-tabs>
        <lg-tabs label="Filter" level="secondary" selected="ready">
          @for (tab of filters; track tab.key) {
            <button lgTab [key]="tab.key" [count]="tab.count">
              {{ tab.label }}
            </button>
          }
        </lg-tabs>
      </div>
    </ng-template>

    <ng-template
      scStory="Panels"
      notes="lg-tab-panel after the tabs: the selected tab's panel shows, labelled by its tab."
      width="32rem"
    >
      <lg-tabs label="Archive" [(selected)]="section">
        @for (tab of panels; track tab.key) {
          <button lgTab [key]="tab.key">{{ tab.label }}</button>
        }
        @for (tab of panels; track tab.key) {
          <lg-tab-panel [key]="tab.key">
            <p class="lg-type-body" style="margin: var(--lg-space-3) 0 0">
              {{ tab.text }}
            </p>
          </lg-tab-panel>
        }
      </lg-tabs>
    </ng-template>

    <ng-template
      scStory="Route tabs"
      notes='nav lgTabNav with a lgTabLink: each link a route, with routerLinkActive and ariaCurrentWhenActive="page" in the game. The current one looks selected; each link is its own tab stop.'
    >
      <nav lgTabNav label="Archive sections">
        @for (tab of archive; track tab.key) {
          <a
            lgTabLink
            [href]="'#' + tab.key"
            [attr.aria-current]="tab.key === 'regions' ? 'page' : null"
            >{{ tab.label }}</a
          >
        }
      </nav>
    </ng-template>
  `,
})
export class TabsShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly archive: readonly Tab[] = [
    { key: 'creatures', label: 'Creatures' },
    { key: 'regions', label: 'Regions' },
    { key: 'dungeons', label: 'Dungeons' },
    { key: 'tower', label: 'World Tower' },
  ];
  protected readonly panels: readonly (Tab & { text: string })[] = [
    {
      key: 'creatures',
      label: 'Creatures',
      text: 'Every creature you have met, and what it dropped.',
    },
    {
      key: 'regions',
      label: 'Regions',
      text: 'Every region you have walked, and what is left to find.',
    },
    {
      key: 'dungeons',
      label: 'Dungeons',
      text: 'Every dungeon you have cleared, floor by floor.',
    },
  ];
  protected readonly filters: readonly Tab[] = [
    { key: 'all', label: 'All' },
    { key: 'disc', label: 'Discovered' },
    { key: 'ready', label: 'Essence ready', count: 2 },
    { key: 'unseen', label: 'Unseen' },
  ];
  protected readonly items: readonly Tab[] = [
    { key: 'all', label: 'All', count: 12 },
    { key: 'weapons', label: 'Weapons' },
    { key: 'armor', label: 'Armor', count: 0 },
  ];
  protected readonly densities = [
    'comfortable',
    'standard',
    'compact',
  ] as const;
  protected readonly section = signal('creatures');
  protected readonly filter = signal('all');
}

export const TABS_SHOWCASE: ShowcaseEntry = {
  slug: 'tabs',
  name: 'Tabs',
  tier: 'primitives',
  summary: 'The tabs, their panels, and route tabs.',
  covers: [
    'LgTabsComponent',
    'LgTabComponent',
    'LgTabPanelComponent',
    'LgTabNavComponent',
    'LgTabLinkComponent',
  ],
  readme: 'src/app/grimoire/primitives/tabs/README.md',
  component: TabsShowcaseComponent,
};
