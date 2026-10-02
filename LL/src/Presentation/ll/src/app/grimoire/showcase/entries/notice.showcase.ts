import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LG_NOTICE, LgButtonComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-notice-showcase',
  imports: [ShowcaseStoryDirective, ...LG_NOTICE, LgButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Info"
      notes="The heading says what happened; the azure start bar backs it. A status, announced politely."
      width="44rem"
    >
      <lg-notice heading="Multiplayer access restricted">
        You can keep playing solo content; markets, transfers, guilds and
        rankings are closed until 14 Oct 2026.
      </lg-notice>
    </ng-template>

    <ng-template scStory="Warning" notes="The amber bar." width="44rem">
      <lg-notice tone="warning" heading="Catching up offline progress"
        >Resolving stored actions and rewards.</lg-notice
      >
    </ng-template>

    <ng-template
      scStory="Danger"
      notes="An alert, announced at once, with its way out at the end in lg-notice-actions: usually one small Button."
      width="44rem"
    >
      <lg-notice tone="danger" heading="Offline progress paused">
        The server did not answer. Your progress is safe.
        <lg-notice-actions>
          <button lgButton size="sm">Retry</button>
        </lg-notice-actions>
      </lg-notice>
    </ng-template>

    <ng-template
      scStory="Busy"
      notes="busy adds a pulsing dot in the tone, an indeterminate progressbar named by busyLabel."
      width="44rem"
    >
      <lg-notice
        tone="warning"
        heading="Catching up offline progress"
        busy
        busyLabel="Catching up"
      >
        Resolving stored actions and rewards.
      </lg-notice>
    </ng-template>

    <ng-template
      scStory="Title only"
      notes="Without content there is no detail: the heading alone."
      width="44rem"
    >
      <lg-notice heading="Multiplayer access restricted" />
    </ng-template>

    <ng-template
      scStory="Narrow"
      notes="The text wraps; the action keeps its width."
      width="22rem"
    >
      <lg-notice tone="danger" heading="Offline progress paused">
        The server did not answer. Your progress is safe.
        <lg-notice-actions>
          <button lgButton size="sm">Retry</button>
        </lg-notice-actions>
      </lg-notice>
    </ng-template>

    <ng-template
      scStory="Stacked"
      notes="One notice per cause; several stack at the head of the stage."
      width="44rem"
    >
      <div class="sc-grid" style="--sc-cell: 30rem">
        <lg-notice heading="Multiplayer access restricted">
          You can keep playing solo content; markets, transfers, guilds and
          rankings are closed until 14 Oct 2026.
        </lg-notice>
        <lg-notice
          tone="warning"
          heading="Catching up offline progress"
          busy
          busyLabel="Catching up"
        >
          Resolving stored actions and rewards.
        </lg-notice>
        <lg-notice tone="danger" heading="Offline progress paused">
          The server did not answer. Your progress is safe.
          <lg-notice-actions>
            <button lgButton size="sm">Retry</button>
          </lg-notice-actions>
        </lg-notice>
      </div>
    </ng-template>
  `,
})
export class NoticeShowcaseComponent extends ShowcaseEntryComponent {}

export const NOTICE_SHOWCASE: ShowcaseEntry = {
  slug: 'notice',
  name: 'Notice',
  tier: 'components',
  summary: 'The persistent notice.',
  covers: ['LgNoticeComponent', 'LgNoticeActionsComponent'],
  readme: 'src/app/grimoire/components/notice/README.md',
  component: NoticeShowcaseComponent,
};
