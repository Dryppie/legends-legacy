import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LgButtonComponent,
  LgJourneyCardComponent,
  LgJourneyCardActionsComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-journey-card-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgJourneyCardComponent,
    LgButtonComponent,
    LgJourneyCardActionsComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Default"
      width="60rem"
      notes="Where the player is in the journey, what to do now and what unlocks next. The next unlock takes its own column from Medium up."
    >
      <div class="lg-region">
        <lg-journey-card
          [stages]="stages"
          phase="Shenic Journey"
          heading="Develop your Shenic build"
          summary="Fight in the newest available area, strengthen your loadout, and prepare for the next Shenic challenge."
          objective="Choose a current quest or return to the World Map."
          nextUnlock="A third Essence slot at level 20"
        >
          <lg-journey-card-actions>
            <button lgButton="solid" size="sm" icon="world-map">
              Open World Map
            </button>
            <button lgButton size="sm">Optional: Review Loadout</button>
          </lg-journey-card-actions>
        </lg-journey-card>
      </div>
    </ng-template>

    <ng-template
      scStory="Narrow"
      width="36rem"
      notes="Below 44rem the next unlock moves under the main block."
    >
      <div class="lg-region">
        <lg-journey-card
          [stages]="stages"
          phase="Shenic Journey"
          heading="Develop your Shenic build"
          summary="Fight in the newest available area, strengthen your loadout, and prepare for the next Shenic challenge."
          objective="Choose a current quest or return to the World Map."
          nextUnlock="A third Essence slot at level 20"
        >
          <lg-journey-card-actions>
            <button lgButton="solid" size="sm" icon="world-map">
              Open World Map
            </button>
            <button lgButton size="sm">Optional: Review Loadout</button>
          </lg-journey-card-actions>
        </lg-journey-card>
      </div>
    </ng-template>

    <ng-template
      scStory="First stage"
      width="60rem"
      notes="The Track marks the current stage among all of them."
    >
      <div class="lg-region">
        <lg-journey-card
          [stages]="stages"
          phase="First Hunt"
          heading="Continue the First Steps"
          summary="Open the Quest Journal to continue the guided introduction."
          objective="Open the Quest Journal and follow the highlighted objective."
          nextUnlock="Soul Archive after completing your First Hunt"
        >
          <lg-journey-card-actions>
            <button lgButton="solid" size="sm" icon="quest-journal">
              Open Quests
            </button>
            <button lgButton size="sm">Optional: Review Tutorial</button>
          </lg-journey-card-actions>
        </lg-journey-card>
      </div>
    </ng-template>

    <ng-template
      scStory="Journey complete"
      width="60rem"
      notes="The last stage, with its own label for what lies ahead."
    >
      <div class="lg-region">
        <lg-journey-card
          [stages]="stages"
          phase="Journey Complete"
          heading="Shenic Beta journey complete"
          summary="You reached level 30, cleared the Heart of the Hollow, and completed the focused Beta journey."
          objective="Review the build that carried you through Shenic and the choices you made along the way."
          nextUnlockLabel="Future aspiration"
          nextUnlock="Future Shenic chapters beyond the focused Beta"
        >
          <lg-journey-card-actions>
            <button lgButton="solid" size="sm" icon="essences">
              Review Your Build
            </button>
            <button lgButton size="sm">
              Optional: Review Completed Quests
            </button>
          </lg-journey-card-actions>
        </lg-journey-card>
      </div>
    </ng-template>

    <ng-template
      scStory="Title only"
      width="60rem"
      notes="Only the phase and the title: no Track, objective or next unlock."
    >
      <div class="lg-region">
        <lg-journey-card phase="Tower" heading="Climb" />
      </div>
    </ng-template>
  `,
})
export class JourneyCardShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly stages = [
    'First Hunt',
    'Claim Your Power',
    'Prepare Your Gear',
    'Enter Shenic',
    'Shenic Journey',
    'Journey Complete',
  ];
}

export const JOURNEY_CARD_SHOWCASE: ShowcaseEntry = {
  slug: 'journey-card',
  name: 'JourneyCard',
  tier: 'game',
  summary: 'The next-step guide.',
  covers: ['LgJourneyCardComponent', 'LgJourneyCardActionsComponent'],
  readme: 'src/app/grimoire/game/journey-card/README.md',
  component: JourneyCardShowcaseComponent,
};
