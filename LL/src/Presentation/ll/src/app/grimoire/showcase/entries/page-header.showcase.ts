import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LgButtonComponent,
  LgPageHeaderComponent,
  LgSearchFieldComponent,
  LgSlotDirective,
} from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-page-header-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgPageHeaderComponent,
    LgSlotDirective,
    LgButtonComponent,
    LgSearchFieldComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="With search"
      notes="The section's icon in a gilt diamond, eyebrow, title, summary, and the screen's actions on one line."
      width="60rem"
    >
      <lg-page-header
        icon="overview"
        eyebrow="Character"
        title="Overview"
        summary="Stats, combat rating, and Essence loadout"
      >
        <lg-search-field
          lgSlot="actions"
          placeholder="Search character by name…"
          label="Search character by name"
        />
        <button lgButton lgSlot="actions" size="sm">Search</button>
        <button lgButton="quiet" lgSlot="actions" size="sm">Refresh</button>
      </lg-page-header>
    </ng-template>

    <ng-template scStory="One action" width="60rem">
      <lg-page-header
        icon="leaderboard"
        eyebrow="City"
        title="Leaderboard"
        summary="Top Legends by Combat Rating this season"
      >
        <button lgButton lgSlot="actions">Refresh</button>
      </lg-page-header>
    </ng-template>

    <ng-template scStory="No actions" width="60rem">
      <lg-page-header
        icon="overview"
        eyebrow="Character"
        title="Overview"
        summary="Stats, combat rating, and Essence loadout"
      />
    </ng-template>

    <ng-template
      scStory="Title only"
      notes="Without an icon, eyebrow or summary: the title and its rule."
      width="60rem"
    >
      <lg-page-header title="Settings" />
    </ng-template>

    <ng-template
      scStory="Narrow"
      notes="Below Medium (44rem) the actions wrap under the title."
      width="32rem"
    >
      <lg-page-header
        icon="overview"
        eyebrow="Character"
        title="Overview"
        summary="Stats, combat rating, and Essence loadout"
      >
        <lg-search-field
          lgSlot="actions"
          placeholder="Search character by name…"
          label="Search character by name"
        />
        <button lgButton lgSlot="actions" size="sm">Search</button>
        <button lgButton="quiet" lgSlot="actions" size="sm">Refresh</button>
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
  covers: ['LgPageHeaderComponent'],
  readme: 'design-system/components/PageHeader/README.md',
  component: PageHeaderShowcaseComponent,
};
