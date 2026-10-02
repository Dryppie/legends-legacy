import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgTrackComponent } from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-track-showcase',
  imports: [ShowcaseStoryDirective, LgTrackComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Gilt"
      notes="Done steps are filled, the current step is ringed, the rest are hollow. Each step can carry its own label."
      width="35rem"
    >
      <lg-track
        [steps]="5"
        [current]="1"
        startLabel="Ascension"
        endLabel="Tier V"
        label="Legacy Ascension"
        [labels]="tiers"
      />
    </ng-template>

    <ng-template
      scStory="Health"
      notes="tone=hp, for the floors of the World Tower."
      width="35rem"
    >
      <lg-track
        [steps]="7"
        [current]="4"
        tone="hp"
        startLabel="Floor 1"
        endLabel="Boss"
        label="World Tower"
      />
    </ng-template>

    <ng-template
      scStory="Arcana"
      notes="tone=arcana, without end labels."
      width="35rem"
    >
      <lg-track
        [steps]="4"
        [current]="1"
        [labels]="stages"
        tone="arcana"
        label="Journey"
      />
    </ng-template>

    <ng-template
      scStory="First step"
      notes="Nothing done yet: the first diamond is ringed."
      width="35rem"
    >
      <lg-track
        [steps]="5"
        [current]="0"
        startLabel="First Hunt"
        endLabel="Journey complete"
      />
    </ng-template>

    <ng-template
      scStory="Last step"
      notes="Every step before the last is done."
      width="35rem"
    >
      <lg-track
        [steps]="5"
        [current]="4"
        startLabel="First Hunt"
        endLabel="Journey complete"
      />
    </ng-template>

    <ng-template
      scStory="Nine steps"
      notes="Up to about nine steps; beyond that, use a Meter."
      width="35rem"
    >
      <lg-track
        [steps]="9"
        [current]="5"
        tone="hp"
        startLabel="Floor 1"
        endLabel="Boss"
        label="World Tower"
      />
    </ng-template>
  `,
})
export class TrackShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly tiers = [
    'Tier I',
    'Tier II',
    'Tier III',
    'Tier IV',
    'Tier V',
  ];
  protected readonly stages = [
    'First Hunt',
    'Shenic Journey',
    'Tower',
    'Legacy',
  ];
}

export const TRACK_SHOWCASE: ShowcaseEntry = {
  slug: 'track',
  name: 'Track',
  tier: 'components',
  summary: 'The milestone track.',
  covers: ['LgTrackComponent'],
  readme: 'design-system/components/Track/README.md',
  component: TrackShowcaseComponent,
};
