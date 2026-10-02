import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgDeltaComponent, LgDeltaDirection, LgDeltaPolarity } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

interface DeltaSample {
  label: string;
  now: string;
  direction: LgDeltaDirection;
  value: string;
  polarity: LgDeltaPolarity;
}

@Component({
  selector: 'sc-delta-showcase',
  imports: [ShowcaseStoryDirective, LgDeltaComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Comparison"
      notes="Every change shows its direction and its sign; the colour says whether it helps the player."
    >
      <div class="sc-row sc-row--start">
        @for (d of comparison; track d.label) {
          <div class="sc-cell">
            <p class="sc-cap">{{ d.label }}</p>
            <span
              >{{ d.now }}
              <lg-delta
                [direction]="d.direction"
                [value]="d.value"
                [polarity]="d.polarity"
            /></span>
          </div>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="Better"
      notes="Polarity follows the player's benefit, not the sign: a cooldown falling from 8s to 6.8s is better."
    >
      <div class="sc-row sc-row--start">
        @for (d of better; track d.label) {
          <div class="sc-cell">
            <p class="sc-cap">{{ d.label }}</p>
            <span
              >{{ d.now }}
              <lg-delta
                [direction]="d.direction"
                [value]="d.value"
                [polarity]="d.polarity"
            /></span>
          </div>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="Worse"
      notes="A worse delta is not danger: a lower stat is not a loss or a failure."
    >
      <div class="sc-row sc-row--start">
        @for (d of worse; track d.label) {
          <div class="sc-cell">
            <p class="sc-cap">{{ d.label }}</p>
            <span
              >{{ d.now }}
              <lg-delta
                [direction]="d.direction"
                [value]="d.value"
                [polarity]="d.polarity"
            /></span>
          </div>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="Unchanged"
      notes="±0 with no glyph, in delta-neutral. Screen readers hear 0, unchanged."
    >
      <div class="sc-cell">
        <p class="sc-cap">Attack Speed</p>
        <span
          >1.12 <lg-delta direction="none" value="0" polarity="neutral"
        /></span>
      </div>
    </ng-template>

    <ng-template
      scStory="Not judged"
      notes="Neutral with a direction, for a change the game does not judge, such as Threat."
    >
      <div class="sc-row sc-row--start">
        <div class="sc-cell">
          <p class="sc-cap">Threat</p>
          <span
            >184.6 <lg-delta direction="up" value="12.5" polarity="neutral"
          /></span>
        </div>
        <div class="sc-cell">
          <p class="sc-cap">Threat</p>
          <span
            >172.1 <lg-delta direction="down" value="12.5" polarity="neutral"
          /></span>
        </div>
      </div>
    </ng-template>

    <ng-template
      scStory="Sizes"
      notes="It takes the size of the text around it: caption on a Ledger row's second line, body elsewhere."
    >
      <div class="sc-col">
        <span
          >Power 154 <lg-delta direction="up" value="12%" polarity="better"
        /></span>
        <p class="sc-cap">
          Power 154 <lg-delta direction="up" value="12%" polarity="better" />
        </p>
      </div>
    </ng-template>
  `,
})
export class DeltaShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly better: readonly DeltaSample[] = [
    {
      label: 'Power',
      now: '154',
      direction: 'up',
      value: '12',
      polarity: 'better',
    },
    {
      label: 'Cooldown',
      now: '6.8s',
      direction: 'down',
      value: '1.2s',
      polarity: 'better',
    },
  ];
  protected readonly worse: readonly DeltaSample[] = [
    {
      label: 'Crit Chance',
      now: '9%',
      direction: 'down',
      value: '0.5%',
      polarity: 'worse',
    },
    {
      label: 'Damage Taken',
      now: '14%',
      direction: 'up',
      value: '2%',
      polarity: 'worse',
    },
  ];
  protected readonly comparison: readonly DeltaSample[] = [
    ...this.better,
    ...this.worse,
    {
      label: 'Attack Speed',
      now: '1.12',
      direction: 'none',
      value: '0',
      polarity: 'neutral',
    },
  ];
}

export const DELTA_SHOWCASE: ShowcaseEntry = {
  slug: 'delta',
  name: 'Delta',
  tier: 'components',
  summary: 'The stat change.',
  covers: ['LgDeltaComponent'],
  readme: 'src/app/grimoire/components/delta/README.md',
  component: DeltaShowcaseComponent,
};
