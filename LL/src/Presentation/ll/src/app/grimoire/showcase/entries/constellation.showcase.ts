import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import {
  LgConstellationComponent,
  LgConstellationItem,
  LgConstellationRing,
  LgStageComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-constellation-showcase',
  imports: [ShowcaseStoryDirective, LgConstellationComponent, LgStageComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Over the stage"
      width="40rem"
      height="25rem"
      flush
      notes="Sigils on thin orbit rings over the stage art; a strong ring is gilt, and nodes are gilt ticks across their ring. One tab stop: arrow keys, Home and End move, Enter or Space selects."
    >
      <lg-stage
        image="assets/backgrounds/optimized/background.webp"
        label="Attributes"
      >
        <lg-constellation
          [width]="640"
          [height]="400"
          [rings]="rings"
          [nodes]="nodes"
          [items]="items"
          [selectedId]="selected()"
          (select)="selected.set($event)"
        />
      </lg-stage>
    </ng-template>

    <ng-template
      scStory="Ready and locked"
      width="43rem"
      notes="A ready Sigil takes its diamond; a locked one stays in the arrow-key order and opens its condition in the reason tip."
    >
      <lg-constellation
        [items]="stateItems"
        [rings]="stateRings"
        [nodes]="stateNodes"
        selectedId="str"
      />
    </ng-template>

    <ng-template
      scStory="Not selectable"
      width="40rem"
      height="25rem"
      flush
      notes="Without selection the Sigils are figures, not buttons, and nothing is a tab stop."
    >
      <lg-stage
        image="assets/backgrounds/optimized/background.webp"
        label="Attributes"
      >
        <lg-constellation
          [width]="640"
          [height]="400"
          [rings]="rings"
          [nodes]="nodes"
          [items]="items"
          [selectable]="false"
        />
      </lg-stage>
    </ng-template>
  `,
})
export class ConstellationShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly rings: LgConstellationRing[] = [
    { cx: 300, cy: 200, r: 150 },
    { cx: 380, cy: 170, r: 90, strong: true },
    { cx: 250, cy: 280, r: 180 },
  ];
  protected readonly nodes = [
    { x: 300, y: 50 },
    { x: 450, y: 200 },
  ];
  protected readonly items: LgConstellationItem[] = [
    { id: 'power', label: 'Power', value: 42, x: 360, y: 90 },
    {
      id: 'armor',
      label: 'Armor',
      value: 18,
      x: 170,
      y: 170,
      labelPosition: 'left',
    },
    {
      id: 'res',
      label: 'Resistance',
      value: 11,
      x: 400,
      y: 250,
      state: 'ready',
    },
    {
      id: 'crit',
      label: 'Crit Chance',
      value: '9%',
      x: 220,
      y: 330,
      labelPosition: 'left',
    },
  ];
  protected readonly selected = signal('power');

  protected readonly stateItems: LgConstellationItem[] = [
    { id: 'str', label: 'Strength', value: 24, x: 300, y: 200 },
    {
      id: 'dex',
      label: 'Dexterity',
      value: 18,
      x: 600,
      y: 300,
      labelPosition: 'left',
      size: 'sm',
      state: 'ready',
    },
    {
      id: 'int',
      label: 'Intellect',
      value: 9,
      x: 500,
      y: 500,
      state: 'locked',
      reason: 'Unlocks at level 30',
    },
  ];
  protected readonly stateRings: LgConstellationRing[] = [
    { cx: 500, cy: 350, r: 200 },
    { cx: 500, cy: 350, r: 300, strong: true },
  ];
  protected readonly stateNodes = [
    { x: 700, y: 350 },
    { x: 500, y: 50 },
  ];
}

export const CONSTELLATION_SHOWCASE: ShowcaseEntry = {
  slug: 'constellation',
  name: 'Constellation',
  tier: 'game',
  summary: 'The stat star chart.',
  covers: ['LgConstellationComponent'],
  readme: 'src/app/grimoire/game/constellation/README.md',
  component: ConstellationShowcaseComponent,
};
