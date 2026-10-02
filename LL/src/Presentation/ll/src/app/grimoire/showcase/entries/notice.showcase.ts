import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LgButtonComponent,
  LgNoticeComponent,
  LgSlotDirective,
} from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-notice-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgNoticeComponent,
    LgSlotDirective,
    LgButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Info"
      notes="The title says what happened; the azure start bar backs it. A status, announced politely."
      width="44rem"
    >
      <lg-notice title="Multiplayer access restricted">
        You can keep playing solo content; markets, transfers, guilds and
        rankings are closed until 14 Oct 2026.
      </lg-notice>
    </ng-template>

    <ng-template scStory="Warning" notes="The amber bar." width="44rem">
      <lg-notice tone="warning" title="Catching up offline progress"
        >Resolving stored actions and rewards.</lg-notice
      >
    </ng-template>

    <ng-template
      scStory="Danger"
      notes="An alert, announced at once, with its way out at the end: usually one small Button."
      width="44rem"
    >
      <lg-notice tone="danger" title="Offline progress paused">
        The server did not answer. Your progress is safe.
        <button lgButton lgSlot="action" size="sm">Retry</button>
      </lg-notice>
    </ng-template>

    <ng-template
      scStory="Busy"
      notes="busy adds a pulsing dot in the tone, an indeterminate progressbar named by busyLabel."
      width="44rem"
    >
      <lg-notice
        tone="warning"
        title="Catching up offline progress"
        busy
        busyLabel="Catching up"
      >
        Resolving stored actions and rewards.
      </lg-notice>
    </ng-template>

    <ng-template
      scStory="Title only"
      notes="text=false when there is no detail under the title."
      width="44rem"
    >
      <lg-notice title="Multiplayer access restricted" [text]="false" />
    </ng-template>

    <ng-template
      scStory="Narrow"
      notes="The text wraps; the action keeps its width."
      width="22rem"
    >
      <lg-notice tone="danger" title="Offline progress paused">
        The server did not answer. Your progress is safe.
        <button lgButton lgSlot="action" size="sm">Retry</button>
      </lg-notice>
    </ng-template>

    <ng-template
      scStory="Stacked"
      notes="One notice per cause; several stack at the head of the stage."
      width="44rem"
    >
      <div class="sc-grid" style="--sc-cell: 30rem">
        <lg-notice title="Multiplayer access restricted">
          You can keep playing solo content; markets, transfers, guilds and
          rankings are closed until 14 Oct 2026.
        </lg-notice>
        <lg-notice
          tone="warning"
          title="Catching up offline progress"
          busy
          busyLabel="Catching up"
        >
          Resolving stored actions and rewards.
        </lg-notice>
        <lg-notice tone="danger" title="Offline progress paused">
          The server did not answer. Your progress is safe.
          <button lgButton lgSlot="action" size="sm">Retry</button>
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
  covers: ['LgNoticeComponent'],
  readme: 'design-system/components/Notice/README.md',
  component: NoticeShowcaseComponent,
};
