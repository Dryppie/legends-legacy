import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgStatTileComponent } from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-stat-tile-showcase',
  imports: [ShowcaseStoryDirective, LgStatTileComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Stat grid"
      notes="Tiles laid out in lg-statgrid: two a row in a Stacked region such as the Folio."
      width="23.75rem"
    >
      <div class="lg-statgrid">
        <lg-stat-tile label="Armor" [value]="45" />
        <lg-stat-tile
          label="Resist"
          [value]="30"
          [delta]="4"
          deltaPolarity="better"
        />
        <lg-stat-tile label="Crit" [value]="12" suffix="%" />
        <lg-stat-tile
          label="Dodge"
          [value]="8"
          suffix="%"
          [delta]="-2"
          deltaPolarity="worse"
        />
        <lg-stat-tile
          label="Cooldown"
          [value]="6.8"
          suffix="s"
          [delta]="-1.2"
          deltaPolarity="better"
        />
        <lg-stat-tile label="Block" [value]="5" suffix="%" [delta]="0" />
      </div>
    </ng-template>

    <ng-template
      scStory="Value"
      notes="A tile fill with the value in numeral-stat and the label in short capitals. No edge."
      width="23.75rem"
    >
      <div class="lg-statgrid">
        <lg-stat-tile label="Armor" [value]="45" />
        <lg-stat-tile label="Power" [value]="1284" />
      </div>
    </ng-template>

    <ng-template
      scStory="With a unit"
      notes="The suffix is set smaller and muted right after the value."
      width="23.75rem"
    >
      <div class="lg-statgrid">
        <lg-stat-tile label="Crit" [value]="12" suffix="%" />
        <lg-stat-tile label="Cooldown" [value]="12" suffix="s" />
      </div>
    </ng-template>

    <ng-template
      scStory="Better"
      notes="The delta's colour comes from deltaPolarity, never from the sign: a cooldown falling is better."
      width="23.75rem"
    >
      <div class="lg-statgrid">
        <lg-stat-tile
          label="Resist"
          [value]="30"
          [delta]="4"
          deltaPolarity="better"
        />
        <lg-stat-tile
          label="Cooldown"
          [value]="6.8"
          suffix="s"
          [delta]="-1.2"
          deltaPolarity="better"
        />
      </div>
    </ng-template>

    <ng-template scStory="Worse" width="23.75rem">
      <div class="lg-statgrid">
        <lg-stat-tile
          label="Dodge"
          [value]="8"
          suffix="%"
          [delta]="-2"
          deltaPolarity="worse"
        />
      </div>
    </ng-template>

    <ng-template
      scStory="Unchanged"
      notes="A delta of 0 shows ±0."
      width="23.75rem"
    >
      <div class="lg-statgrid">
        <lg-stat-tile label="Block" [value]="5" suffix="%" [delta]="0" />
        <lg-stat-tile label="Armor" [value]="240" [delta]="0" />
      </div>
    </ng-template>

    <ng-template
      scStory="Not judged"
      notes="Without deltaPolarity the delta is neutral: the tile cannot know the stat's rules."
      width="23.75rem"
    >
      <div class="lg-statgrid">
        <lg-stat-tile label="Threat" [value]="184" [delta]="12" />
      </div>
    </ng-template>

    <ng-template
      scStory="Delta text"
      notes="deltaText when the default formatting will not do, such as Attack Speed's two places."
      width="23.75rem"
    >
      <div class="lg-statgrid">
        <lg-stat-tile
          label="Speed"
          value="1.12"
          [delta]="0.1"
          deltaText="0.10"
          deltaPolarity="better"
        />
      </div>
    </ng-template>

    <ng-template
      scStory="No value"
      notes="A missing value shows an em dash, never an empty tile, and drops its suffix."
      width="23.75rem"
    >
      <div class="lg-statgrid">
        <lg-stat-tile label="Block" [value]="null" suffix="%" />
        <lg-stat-tile label="Pierce" [value]="null" />
      </div>
    </ng-template>

    <ng-template
      scStory="Long label"
      notes="In a Stacked region a tile with a Delta wants a one-word label."
      width="23.75rem"
    >
      <div class="lg-statgrid">
        <lg-stat-tile
          label="Magic Penetration"
          [value]="1284"
          [delta]="12"
          deltaPolarity="better"
        />
        <lg-stat-tile
          label="Health Regen"
          [value]="84"
          [delta]="-6"
          deltaPolarity="worse"
        />
      </div>
    </ng-template>
  `,
})
export class StatTileShowcaseComponent extends ShowcaseEntryComponent {}

export const STAT_TILE_SHOWCASE: ShowcaseEntry = {
  slug: 'stat-tile',
  name: 'StatTile',
  tier: 'components',
  summary: 'The compact stat.',
  covers: ['LgStatTileComponent'],
  readme: 'design-system/components/StatTile/README.md',
  component: StatTileShowcaseComponent,
};
