import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { LgTab, LgTabStripComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-tab-strip-showcase',
  imports: [ShowcaseStoryDirective, LgTabStripComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Primary"
      notes="What you are browsing. The active tab is a tile block with an arcana underline. Left and Right move and select."
    >
      <lg-tab-strip label="Archive" [tabs]="archive" [(activeId)]="section" />
    </ng-template>

    <ng-template
      scStory="Secondary"
      notes="How it is filtered: label-sized capitals, active in ink with the same underline, the count in arcana."
    >
      <lg-tab-strip
        label="Filter"
        level="secondary"
        [tabs]="filters"
        [(activeId)]="filter"
      />
    </ng-template>

    <ng-template
      scStory="Two levels"
      notes="Primary over secondary; never three levels."
    >
      <div class="sc-col">
        <lg-tab-strip label="Archive" [tabs]="archive" [(activeId)]="section" />
        <lg-tab-strip
          label="Filter"
          level="secondary"
          [tabs]="filters"
          [(activeId)]="filter"
        />
      </div>
    </ng-template>

    <ng-template
      scStory="Compact"
      notes="32px tabs, for a table or list header."
    >
      <div class="sc-col">
        <lg-tab-strip
          label="Archive, compact"
          density="compact"
          [tabs]="archive"
          [(activeId)]="section"
        />
        <lg-tab-strip
          label="Filter, compact"
          level="secondary"
          density="compact"
          [tabs]="filters"
          [(activeId)]="filter"
        />
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
            <lg-tab-strip
              [label]="'Archive, ' + d"
              [density]="d"
              [tabs]="archive"
              activeId="creatures"
            />
            <lg-tab-strip
              [label]="'Filter, ' + d"
              level="secondary"
              [density]="d"
              [tabs]="filters"
              activeId="all"
            />
          </div>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="Counts"
      notes="A count after the label, 0 included; on the active tab it takes the tab's colour."
    >
      <div class="sc-col">
        <lg-tab-strip
          label="Inventory"
          level="secondary"
          [tabs]="items"
          activeId="all"
        />
        <lg-tab-strip
          label="Inventory"
          level="secondary"
          [tabs]="items"
          activeId="armor"
        />
      </div>
    </ng-template>

    <ng-template
      scStory="Overflow"
      notes="Primary tabs scroll sideways inside the strip when they no longer fit; secondary tabs wrap."
      width="20rem"
    >
      <div class="sc-col">
        <lg-tab-strip label="Archive" [tabs]="archive" activeId="regions" />
        <lg-tab-strip
          label="Filter"
          level="secondary"
          [tabs]="filters"
          activeId="ready"
        />
      </div>
    </ng-template>
  `,
})
export class TabStripShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly archive: readonly LgTab[] = [
    { id: 'creatures', label: 'Creatures' },
    { id: 'regions', label: 'Regions' },
    { id: 'dungeons', label: 'Dungeons' },
    { id: 'tower', label: 'World Tower' },
  ];
  protected readonly filters: readonly LgTab[] = [
    { id: 'all', label: 'All' },
    { id: 'disc', label: 'Discovered' },
    { id: 'ready', label: 'Essence ready', count: 2 },
    { id: 'unseen', label: 'Unseen' },
  ];
  protected readonly items: readonly LgTab[] = [
    { id: 'all', label: 'All', count: 12 },
    { id: 'weapons', label: 'Weapons' },
    { id: 'armor', label: 'Armor', count: 0 },
  ];
  protected readonly densities = [
    'comfortable',
    'standard',
    'compact',
  ] as const;
  protected readonly section = signal('creatures');
  protected readonly filter = signal('all');
}

export const TAB_STRIP_SHOWCASE: ShowcaseEntry = {
  slug: 'tab-strip',
  name: 'TabStrip',
  tier: 'primitives',
  summary: 'The tabs.',
  covers: ['LgTabStripComponent'],
  readme: 'src/app/grimoire/primitives/tab-strip/README.md',
  component: TabStripShowcaseComponent,
};
