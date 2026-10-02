import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgStatFigureComponent } from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-stat-figure-showcase',
  imports: [ShowcaseStoryDirective, LgStatFigureComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Default"
      notes="The screen's one headline figure: Marcellus in gilt, in proportional figures, with its label above and one line of explanation below."
    >
      <lg-stat-figure
        label="Combat Rating"
        value="1,284"
        caption="Permanent attributes and the equipped build"
        title="Combat Rating summarizes permanent combat attributes and the equipped build."
      />
    </ng-template>

    <ng-template
      scStory="Small"
      notes="size=sm sets the value at title-lg, 36px: use it beside a LevelPlate. A number value is formatted for you."
    >
      <div class="sc-row sc-row--start">
        <lg-stat-figure size="sm" label="Achievement Points" [value]="1240" />
        <lg-stat-figure size="sm" label="Arena Rating" [value]="1530" />
      </div>
    </ng-template>

    <ng-template
      scStory="Small with caption"
      notes="The full explanation is in the title, shown as a tooltip."
    >
      <lg-stat-figure
        size="sm"
        label="Combat Rating"
        [value]="1284"
        caption="Permanent"
        title="All sources"
      />
    </ng-template>

    <ng-template
      scStory="Level"
      notes="The level as a figure beside the identity, with its progress in the caption."
    >
      <lg-stat-figure
        label="Level"
        [value]="17"
        caption="8,420 / 12,000 Combat XP"
      />
    </ng-template>

    <ng-template
      scStory="Unavailable"
      notes="No value yet: an em dash, and the caption says why."
    >
      <lg-stat-figure
        label="Combat Rating"
        [value]="null"
        caption="Unavailable"
      />
    </ng-template>
  `,
})
export class StatFigureShowcaseComponent extends ShowcaseEntryComponent {}

export const STAT_FIGURE_SHOWCASE: ShowcaseEntry = {
  slug: 'stat-figure',
  name: 'StatFigure',
  tier: 'components',
  summary: 'The headline number.',
  covers: ['LgStatFigureComponent'],
  readme: 'design-system/components/StatFigure/README.md',
  component: StatFigureShowcaseComponent,
};
