import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgPageComponent, LgPanelComponent, LgStageComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-stage-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgStageComponent,
    LgPageComponent,
    LgPanelComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="With art"
      notes="The art is darkened, warmed and slightly blurred, under a vignette, a fade to ground at the foot and a film grain."
      width="60rem"
      height="18.75rem"
      flush
    >
      <lg-stage
        image="assets/backgrounds/optimized/tavern.webp"
        label="Tavern"
      />
    </ng-template>

    <ng-template
      scStory="Lore over the art"
      notes="Paragraphs never sit straight on the art: lore goes in a Panel, its contrast surface."
      width="60rem"
      height="18.75rem"
      flush
    >
      <lg-stage image="assets/backgrounds/optimized/tavern.webp" label="Tavern">
        <lg-page label="Tavern">
          <div class="sc-col">
            <lg-panel title="The Tavern">
              <p>Warm ale, cold rumours. Someone here is looking for you.</p>
            </lg-panel>
          </div>
        </lg-page>
      </lg-stage>
    </ng-template>

    <ng-template
      scStory="Focus"
      notes="focus sets the art's background-position: here the top of the Colosseum."
      width="60rem"
      height="18.75rem"
      flush
    >
      <lg-stage
        image="assets/backgrounds/optimized/colosseum.webp"
        focus="top"
        label="Colosseum"
      />
    </ng-template>

    <ng-template
      scStory="Card art"
      notes="A Cards asset works as well as a Background."
      width="60rem"
      height="18.75rem"
      flush
    >
      <lg-stage image="assets/cards/optimized/combatArea.webp" label="Combat" />
    </ng-template>

    <ng-template
      scStory="Without art"
      notes="No image: ground-deep, and no veil or grain, since they belong to the art."
      width="60rem"
      height="18.75rem"
      flush
    >
      <lg-stage label="Tavern">
        <lg-page label="Tavern">
          <div class="sc-col">
            <lg-panel title="The Tavern">
              <p>Warm ale, cold rumours. Someone here is looking for you.</p>
            </lg-panel>
          </div>
        </lg-page>
      </lg-stage>
    </ng-template>
  `,
})
export class StageShowcaseComponent extends ShowcaseEntryComponent {}

export const STAGE_SHOWCASE: ShowcaseEntry = {
  slug: 'stage',
  name: 'Stage',
  tier: 'shell',
  summary: 'The scene backdrop.',
  covers: ['LgStageComponent'],
  readme: 'src/app/grimoire/shell/stage/README.md',
  component: StageShowcaseComponent,
};
