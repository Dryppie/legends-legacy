import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LG_LEDGER,
  LG_PANEL,
  LgLedgerRow,
  LgListComponent,
  LgListRowComponent,
  LgPageComponent,
  LgPageHeaderComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-page-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgPageComponent,
    LgPageHeaderComponent,
    ...LG_PANEL,
    ...LG_LEDGER,
    LgListComponent,
    LgListRowComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Default"
      notes="Fills its positioned parent, scrolls on its own and pads its top for the floating TopBar."
      width="60rem"
      height="24rem"
      flush
    >
      <lg-page label="Leaderboard">
        <lg-page-header
          icon="leaderboard"
          eyebrow="City"
          heading="Leaderboard"
          summary="Top Legends by Combat Rating this season"
        />
        <lg-panel>
          <lg-panel-header>
            <lg-panel-title>Season standings</lg-panel-title>
          </lg-panel-header>
          <p>Page scrolls on its own; the TopBar floats above it.</p>
        </lg-panel>
      </lg-page>
    </ng-template>

    <ng-template
      scStory="Max width"
      notes="maxWidth caps the content, which centres; the default is page-max (80rem)."
      width="60rem"
      height="24rem"
      flush
    >
      <lg-page label="Leaderboard" maxWidth="36rem" role="region">
        <lg-page-header
          icon="leaderboard"
          eyebrow="City"
          heading="Leaderboard"
          summary="Top Legends by Combat Rating this season"
        />
        <lg-panel>
          <lg-panel-header>
            <lg-panel-title>Season standings</lg-panel-title>
          </lg-panel-header>
          <p>Page scrolls on its own; the TopBar floats above it.</p>
        </lg-panel>
      </lg-page>
    </ng-template>

    <ng-template
      scStory="Flow"
      notes="In a frame that is not GameShell (D-097): in the normal flow, filling its parent's height, with no room for a TopBar. The frame gives the gutters."
      width="60rem"
      height="24rem"
    >
      <lg-page label="Guild" flow>
        <lg-page-header
          icon="guild"
          eyebrow="City"
          heading="Guild"
          summary="A Page with flow, inside the game’s own frame"
        />
        <lg-panel>
          <lg-panel-header>
            <lg-panel-title>Members</lg-panel-title>
          </lg-panel-header>
          <p>No room is kept for a TopBar; the frame gives the gutters.</p>
        </lg-panel>
      </lg-page>
    </ng-template>

    <ng-template
      scStory="Aside layout"
      notes="lg-aside: a main column and a side column (aside-width) from Medium up; each column is a region of its own."
      width="60rem"
      height="24rem"
      flush
    >
      <lg-page label="Overview">
        <lg-page-header
          icon="overview"
          eyebrow="Character"
          heading="Overview"
          summary="Stats, combat rating, and Essence loadout"
        />
        <div class="lg-aside">
          <div class="lg-aside__main">
            <div class="lg-ledgergrid lg-ledgergrid--2">
              <lg-ledger heading="Offense">
                @for (row of offense; track row.label) {
                  <div
                    lgLedgerRow
                    [label]="row.label"
                    [value]="row.value"
                    [sub]="row.sub"
                    [description]="row.description"
                    [tipMeta]="row.tipMeta"
                    [muted]="!!row.muted"
                  ></div>
                }
              </lg-ledger>
              <lg-ledger heading="Defense">
                @for (row of defense; track row.label) {
                  <div
                    lgLedgerRow
                    [label]="row.label"
                    [value]="row.value"
                    [sub]="row.sub"
                    [description]="row.description"
                    [tipMeta]="row.tipMeta"
                    [muted]="!!row.muted"
                  ></div>
                }
              </lg-ledger>
            </div>
          </div>
          <aside class="lg-aside__side">
            <lg-panel flush>
              <lg-panel-header>
                <lg-panel-title>Essence loadout</lg-panel-title>
                <span>2 / 2</span>
              </lg-panel-header>
              <lg-list label="Essence loadout">
                <li
                  lgListRow
                  name="Dire Wolf Essence"
                  rarity="Rare"
                  icon="essences"
                  meta="Essence · Lv 12"
                ></li>
                <li
                  lgListRow
                  name="Soul Prism"
                  rarity="Epic"
                  icon="soulstones"
                  meta="Essence · Lv 18"
                ></li>
              </lg-list>
            </lg-panel>
          </aside>
        </div>
      </lg-page>
    </ng-template>

    <ng-template
      scStory="Aside stacked"
      notes="Below Medium (44rem) the side column drops under the main one."
      width="34rem"
      height="28rem"
      flush
    >
      <lg-page label="Overview">
        <div class="lg-aside">
          <div class="lg-aside__main">
            <lg-ledger heading="Offense">
              @for (row of offense; track row.label) {
                <div
                  lgLedgerRow
                  [label]="row.label"
                  [value]="row.value"
                  [sub]="row.sub"
                  [description]="row.description"
                  [tipMeta]="row.tipMeta"
                  [muted]="!!row.muted"
                ></div>
              }
            </lg-ledger>
          </div>
          <aside class="lg-aside__side">
            <lg-panel flush>
              <lg-panel-header>
                <lg-panel-title>Essence loadout</lg-panel-title>
                <span>2 / 2</span>
              </lg-panel-header>
              <lg-list label="Essence loadout">
                <li
                  lgListRow
                  name="Dire Wolf Essence"
                  rarity="Rare"
                  icon="essences"
                  meta="Essence · Lv 12"
                ></li>
                <li
                  lgListRow
                  name="Soul Prism"
                  rarity="Epic"
                  icon="soulstones"
                  meta="Essence · Lv 18"
                ></li>
              </lg-list>
            </lg-panel>
          </aside>
        </div>
      </lg-page>
    </ng-template>
  `,
})
export class PageShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly offense: readonly LgLedgerRow[] = [
    { label: 'Power', value: '142' },
    { label: 'Crit Chance', value: '9.5%' },
    { label: 'Attack Speed', value: '1.12' },
    { label: 'Armor Penetration', value: '12' },
  ];
  protected readonly defense: readonly LgLedgerRow[] = [
    { label: 'Max Health', value: '4,150' },
    { label: 'Armor', value: '38%', sub: '240 Armor Rating' },
    { label: 'Block', value: '4%' },
  ];
}

export const PAGE_SHOWCASE: ShowcaseEntry = {
  slug: 'page',
  name: 'Page',
  tier: 'components',
  summary: 'The information screen frame.',
  covers: ['LgPageComponent'],
  readme: 'src/app/grimoire/components/page/README.md',
  component: PageShowcaseComponent,
};
