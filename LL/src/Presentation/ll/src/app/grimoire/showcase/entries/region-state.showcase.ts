import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LG_PANEL,
  LG_REGION_STATE,
  LgButtonComponent,
  LgSkeletonComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-region-state-showcase',
  imports: [
    ShowcaseStoryDirective,
    ...LG_REGION_STATE,
    ...LG_PANEL,
    LgButtonComponent,
    LgSkeletonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Empty"
      width="32rem"
      notes="Nothing exists yet, so it says what would fill the region and offers a way to make it. It stands in the Panel's own inset."
    >
      <lg-panel>
        <lg-panel-header
          ><lg-panel-title>Your listings</lg-panel-title></lg-panel-header
        >
        <lg-region-state state="empty" heading="No listings yet."
          >List an item to sell it.
          <lg-region-state-actions
            ><button lgButton size="sm">
              List an item
            </button></lg-region-state-actions
          >
        </lg-region-state>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="No results"
      width="32rem"
      notes="A filter hid everything, so it offers a way to undo the filter."
    >
      <lg-panel>
        <lg-panel-header
          ><lg-panel-title>Guild members</lg-panel-title></lg-panel-header
        >
        <lg-region-state state="no-results" heading="No matching players">
          <lg-region-state-actions
            ><button lgButton="link">
              Clear filters
            </button></lg-region-state-actions
          >
        </lg-region-state>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Error"
      width="32rem"
      notes="The ✕ and its sentence in danger, with a way to try again. Announced politely: the region failed to load."
    >
      <lg-panel>
        <lg-panel-header
          ><lg-panel-title>Guild roster</lg-panel-title></lg-panel-header
        >
        <lg-region-state state="error" heading="Couldn't load the roster.">
          <lg-region-state-actions
            ><button lgButton size="sm">
              Try again
            </button></lg-region-state-actions
          >
        </lg-region-state>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Loading"
      width="32rem"
      notes="Busy: still blocks in the shape of what is coming after 300ms, then its words after a second, in room kept for them, said once."
    >
      <lg-panel>
        <lg-panel-header
          ><lg-panel-title>Guild roster</lg-panel-title></lg-panel-header
        >
        <lg-region-state state="loading" heading="Loading members…">
          <lg-skeleton shape="rows" count="4" />
        </lg-region-state>
      </lg-panel>
    </ng-template>
  `,
})
export class RegionStateShowcaseComponent extends ShowcaseEntryComponent {}

export const REGION_STATE_SHOWCASE: ShowcaseEntry = {
  slug: 'region-state',
  name: 'Region state',
  tier: 'components',
  summary:
    'What a region shows in place of its content: loading, empty, no results, error.',
  covers: ['LgRegionStateComponent', 'LgRegionStateActionsComponent'],
  readme: 'src/app/grimoire/components/region-state/README.md',
  component: RegionStateShowcaseComponent,
};
