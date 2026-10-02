import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LG_NBSP,
  LgNumeric,
  lgFormatFraction,
  lgFormatNumber,
  lgFormatPercent,
  lgFormatRange,
  lgFormatTimes,
  lgFormatUnit,
  LgValuePipe,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

interface NumSample {
  label: string;
  value: LgNumeric;
}

@Component({
  selector: 'sc-numerals-showcase',
  imports: [ShowcaseStoryDirective, LgValuePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Numbers"
      notes="Thousands separators, a true minus (U+2212), 0 for zero, and one precision per stat, trailing zero included."
    >
      <div class="sc-row sc-row--start">
        @for (s of numbers; track s.label) {
          <div class="sc-cell">
            <p class="sc-cap">{{ s.label }}</p>
            @let v = s.value | lgValue;
            <span
              >{{ v.number }}<span class="lg-unit">{{ v.unit }}</span></span
            >
          </div>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="Units"
      notes="A unit follows its number, smaller and muted. Symbol units attach; word units take a non-breaking space."
    >
      <div class="sc-row sc-row--start">
        @for (s of units; track s.label) {
          <div class="sc-cell">
            <p class="sc-cap">{{ s.label }}</p>
            @let v = s.value | lgValue;
            <span
              >{{ v.number }}<span class="lg-unit">{{ v.unit }}</span></span
            >
          </div>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="Signs"
      notes="A range with an en dash, a multiplier with ×, and a fraction with a spaced slash stay whole; only a unit after them is split off."
    >
      <div class="sc-row sc-row--start">
        @for (s of signs; track s.label) {
          <div class="sc-cell">
            <p class="sc-cap">{{ s.label }}</p>
            @let v = s.value | lgValue;
            <span
              >{{ v.number }}<span class="lg-unit">{{ v.unit }}</span></span
            >
          </div>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="Unknown"
      notes="An unknown value, or one that does not apply, shows an em dash; never an empty cell."
    >
      <div class="sc-row sc-row--start">
        @for (s of unknown; track s.label) {
          <div class="sc-cell">
            <p class="sc-cap">{{ s.label }}</p>
            @let v = s.value | lgValue;
            <span
              >{{ v.number }}<span class="lg-unit">{{ v.unit }}</span></span
            >
          </div>
        }
      </div>
    </ng-template>
  `,
})
export class NumeralsShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly numbers: readonly NumSample[] = [
    { label: 'Thousands', value: 12480 },
    { label: 'Power', value: 1284 },
    { label: 'True minus', value: -12 },
    { label: 'Zero', value: 0 },
    { label: 'Attack Speed', value: lgFormatNumber(1.1, 2) },
  ];
  protected readonly units: readonly NumSample[] = [
    { label: 'Crit Chance', value: lgFormatPercent(24.8, 1) },
    { label: 'Health Regen', value: lgFormatUnit(84, 'HP/5s') },
    { label: 'Threat', value: lgFormatUnit(184.6, 'threat/s', 1) },
    { label: 'Cooldown', value: lgFormatUnit(12, 's') },
    { label: 'Life Steal', value: lgFormatPercent(0) },
    { label: 'Armor', value: lgFormatUnit(240, 'Armor Rating') },
    { label: 'Upgrade cost', value: lgFormatUnit(40, 'Soulstones') },
  ];
  protected readonly signs: readonly NumSample[] = [
    { label: 'Range', value: lgFormatRange(12, 18) },
    { label: 'Multiplier', value: lgFormatTimes(1.5, 1) },
    { label: 'Fraction', value: lgFormatFraction(3120, 4150) },
    { label: 'Price range', value: lgFormatRange(1205, 1340) },
    { label: 'Health', value: lgFormatFraction(3120, 4150) + LG_NBSP + 'HP' },
  ];
  protected readonly unknown: readonly NumSample[] = [
    { label: 'Magic Penetration', value: null },
    { label: 'Not a number', value: NaN },
    { label: 'Empty', value: '' },
  ];
}

export const NUMERALS_SHOWCASE: ShowcaseEntry = {
  slug: 'numerals',
  name: 'Numerals',
  tier: 'primitives',
  summary: 'Numbers set by the numeral rules, through the numeral pipes.',
  covers: [],
  readme: 'design-system/docs/02-foundations/03-numerals.md',
  component: NumeralsShowcaseComponent,
};
