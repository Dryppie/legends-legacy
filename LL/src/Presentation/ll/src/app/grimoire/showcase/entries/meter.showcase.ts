import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgMeterComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-meter-showcase',
  imports: [ShowcaseStoryDirective, LgMeterComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Thin"
      notes="A 4px line, for stat columns and plates. The value is always printed beside it, the maximum muted."
      width="20.5rem"
    >
      <div class="sc-grid">
        <lg-meter tone="hp" label="HP" [value]="4150" [max]="4150" />
        <lg-meter tone="sp" label="SP" [value]="520" [max]="780" />
        <lg-meter tone="xp" label="EXP" [value]="350" [max]="24500" />
      </div>
    </ng-template>

    <ng-template
      scStory="Bar"
      notes="A 10px framed bar, for combat screens."
      width="23rem"
    >
      <div class="sc-grid">
        <lg-meter tone="hp" size="bar" label="HP" [value]="3120" [max]="4150" />
        <lg-meter tone="sp" size="bar" label="SP" [value]="200" [max]="780" />
      </div>
    </ng-template>

    <ng-template
      scStory="With a unit"
      notes="A word unit after the maximum, smaller and muted."
      width="23rem"
    >
      <div class="sc-grid">
        <lg-meter
          tone="hp"
          label="Health"
          unit="HP"
          [value]="3120"
          [max]="4150"
        />
        <lg-meter
          tone="sp"
          size="bar"
          label="Stamina"
          unit="SP"
          [value]="40"
          [max]="120"
        />
      </div>
    </ng-template>

    <ng-template
      scStory="Empty"
      notes="Nothing left: the track alone, and the value still printed."
      width="23rem"
    >
      <div class="sc-grid">
        <lg-meter tone="hp" size="bar" label="HP" [value]="0" [max]="4150" />
        <lg-meter tone="xp" label="EXP" [value]="0" [max]="24500" />
      </div>
    </ng-template>

    <ng-template
      scStory="Live"
      notes="Combat playback follows each tick over duration-fast. The value reserves the width of max / max, so a ticking value never moves the label or the bar."
      width="23rem"
    >
      <div class="sc-grid">
        <lg-meter
          tone="hp"
          size="bar"
          label="HP"
          live
          [value]="4150"
          [max]="4150"
        />
        <lg-meter
          tone="hp"
          size="bar"
          label="HP"
          live
          [value]="980"
          [max]="4150"
        />
        <lg-meter
          tone="hp"
          size="bar"
          label="HP"
          live
          [value]="7"
          [max]="4150"
        />
      </div>
    </ng-template>

    <ng-template
      scStory="Without the value"
      notes="showValue off, named by ariaLabel: only where the numbers are printed elsewhere, as under a LevelPlate."
      width="20.5rem"
    >
      <lg-meter
        tone="xp"
        ariaLabel="Combat XP"
        [showValue]="false"
        [value]="8420"
        [max]="12000"
      />
    </ng-template>
  `,
})
export class MeterShowcaseComponent extends ShowcaseEntryComponent {}

export const METER_SHOWCASE: ShowcaseEntry = {
  slug: 'meter',
  name: 'Meter',
  tier: 'primitives',
  summary: 'The progress bar.',
  covers: ['LgMeterComponent'],
  readme: 'src/app/grimoire/primitives/meter/README.md',
  component: MeterShowcaseComponent,
};
