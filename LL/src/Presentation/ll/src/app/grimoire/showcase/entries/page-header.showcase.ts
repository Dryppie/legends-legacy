import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LG_PAGE_HEADER,
  LgButtonComponent,
  LgSearchFieldComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-page-header-showcase',
  imports: [
    ShowcaseStoryDirective,
    ...LG_PAGE_HEADER,
    LgButtonComponent,
    LgSearchFieldComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="With search"
      notes="The section's icon in a gilt diamond, eyebrow, heading, summary, and the screen's actions on one line, in lg-page-header-actions."
      width="60rem"
    >
      <lg-page-header
        icon="overview"
        eyebrow="Character"
        heading="Overview"
        summary="Stats, combat rating, and Essence loadout"
      >
        <lg-page-header-actions>
          <lg-search-field
            placeholder="Search character by name…"
            label="Search character by name"
          />
          <button lgButton size="sm">Search</button>
          <button lgButton="quiet" size="sm">Refresh</button>
        </lg-page-header-actions>
      </lg-page-header>
    </ng-template>

    <ng-template scStory="One action" width="60rem">
      <lg-page-header
        icon="leaderboard"
        eyebrow="City"
        heading="Leaderboard"
        summary="Top Legends by Combat Rating this season"
      >
        <lg-page-header-actions>
          <button lgButton>Refresh</button>
        </lg-page-header-actions>
      </lg-page-header>
    </ng-template>

    <ng-template scStory="No actions" width="60rem">
      <lg-page-header
        icon="overview"
        eyebrow="Character"
        heading="Overview"
        summary="Stats, combat rating, and Essence loadout"
      />
    </ng-template>

    <ng-template
      scStory="Title only"
      notes="Without an icon, eyebrow or summary: the heading and its rule."
      width="60rem"
    >
      <lg-page-header heading="Settings" />
    </ng-template>

    <ng-template
      scStory="Narrow"
      notes="Below Medium (44rem) the actions wrap under the title."
      width="32rem"
    >
      <lg-page-header
        icon="overview"
        eyebrow="Character"
        heading="Overview"
        summary="Stats, combat rating, and Essence loadout"
      >
        <lg-page-header-actions>
          <lg-search-field
            placeholder="Search character by name…"
            label="Search character by name"
          />
          <button lgButton size="sm">Search</button>
          <button lgButton="quiet" size="sm">Refresh</button>
        </lg-page-header-actions>
      </lg-page-header>
    </ng-template>
  `,
})
export class PageHeaderShowcaseComponent extends ShowcaseEntryComponent {}

export const PAGE_HEADER_SHOWCASE: ShowcaseEntry = {
  slug: 'page-header',
  name: 'PageHeader',
  tier: 'components',
  summary: 'The information screen heading.',
  covers: ['LgPageHeaderComponent', 'LgPageHeaderActionsComponent'],
  readme: 'src/app/grimoire/components/page-header/README.md',
  component: PageHeaderShowcaseComponent,
};
