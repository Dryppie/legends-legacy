import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgSkeletonComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-skeleton-showcase',
  imports: [ShowcaseStoryDirective, LgSkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Text"
      width="20rem"
      notes="Lines of the region's text, the last of several shorter. Still: no shimmer. They show after 300ms, so a quick load never flashes them."
    >
      <lg-skeleton count="3" />
    </ng-template>

    <ng-template
      scStory="Rows"
      width="24rem"
      notes="Rows at the region's row height, for a list or a table that is coming."
    >
      <lg-skeleton shape="rows" count="4" />
    </ng-template>

    <ng-template
      scStory="Compact rows"
      width="24rem"
      notes="In a Compact region the rows are Compact too."
    >
      <div data-density="compact"><lg-skeleton shape="rows" count="5" /></div>
    </ng-template>

    <ng-template
      scStory="Block"
      width="20rem"
      notes="A block in the shape of a thumbnail, beside the lines of its row: an item, an Essence, a player."
    >
      <div class="sc-row">
        <lg-skeleton shape="block" />
        <lg-skeleton count="2" width="12rem" />
      </div>
    </ng-template>
  `,
})
export class SkeletonShowcaseComponent extends ShowcaseEntryComponent {}

export const SKELETON_SHOWCASE: ShowcaseEntry = {
  slug: 'skeleton',
  name: 'Skeleton',
  tier: 'primitives',
  summary:
    'The still blocks of the Loading state, in the shape of what is coming.',
  covers: ['LgSkeletonComponent'],
  readme: 'src/app/grimoire/primitives/skeleton/README.md',
  component: SkeletonShowcaseComponent,
};
