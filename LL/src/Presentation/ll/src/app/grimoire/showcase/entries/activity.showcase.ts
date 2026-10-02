import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgActivityComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-activity-showcase',
  imports: [ShowcaseStoryDirective, LgActivityComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Idle"
      notes="Nothing under way: the word and an empty bar."
      width="17rem"
    >
      <div lgActivity label="Idle"></div>
    </ng-template>

    <ng-template
      scStory="In progress"
      notes="The action, the time left in tabular figures and a thin bar with no hue."
      width="17rem"
    >
      <div
        lgActivity
        label="Engaged in Combat"
        remaining="00:12"
        [progress]="0.42"
      ></div>
    </ng-template>

    <ng-template
      scStory="Interactive"
      notes="With a way to the action it is a button; progress here is value of max."
      width="17rem"
    >
      <button
        lgActivity
        label="Engaged in Combat"
        remaining="00:12"
        [value]="3"
        [max]="10"
      ></button>
    </ng-template>

    <ng-template
      scStory="No open label"
      notes="openLabel set to empty: still a button, without the line under the bar."
      width="17rem"
    >
      <button
        lgActivity
        label="Woodcutting"
        remaining="01:45"
        [progress]="0.7"
        openLabel=""
      ></button>
    </ng-template>

    <ng-template
      scStory="Long label"
      notes="The action wraps; the time keeps its width."
      width="17rem"
    >
      <button
        lgActivity
        label="Gathering Ember Wolf Essence in the Whispering Woods"
        remaining="1:04:12"
        [progress]="0.18"
      ></button>
    </ng-template>

    <ng-template
      scStory="Compact"
      notes="The compact rail's mark: a ring the progress rises in, the ✦ and the live dot, over one short word."
      width="7rem"
    >
      <button
        lgActivity
        label="Engaged in Combat"
        short="Battling"
        remaining="00:12"
        [progress]="0.42"
        openLabel=""
        compact
      ></button>
    </ng-template>

    <ng-template scStory="Compact idle" width="7rem">
      <div lgActivity label="Idle" compact></div>
    </ng-template>

    <ng-template
      scStory="Compact without a short word"
      notes="Without short, the label is the word under the mark."
      width="7rem"
    >
      <div
        lgActivity
        label="Mining"
        remaining="00:40"
        [progress]="0.25"
        compact
      ></div>
    </ng-template>
  `,
})
export class ActivityShowcaseComponent extends ShowcaseEntryComponent {}

export const ACTIVITY_SHOWCASE: ShowcaseEntry = {
  slug: 'activity',
  name: 'Activity',
  tier: 'shell',
  summary: 'The current action.',
  covers: ['LgActivityComponent'],
  readme: 'src/app/grimoire/shell/activity/README.md',
  component: ActivityShowcaseComponent,
};
