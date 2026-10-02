import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { LgSigilComponent, LgSigilLabelPosition } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-sigil-showcase',
  imports: [ShowcaseStoryDirective, LgSigilComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Default"
      notes="A number in a verdigris hex, its name beside it. With a press it is a toggle button."
    >
      <div class="sc-row">
        <div lgSigil [value]="24" label="Strength"></div>
        <button lgSigil [value]="11" label="Power"></button>
      </div>
    </ng-template>

    <ng-template
      scStory="Selected"
      notes="The edge turns arcana-glow and the badge scales to 1.08. Press the other one to move it."
    >
      <div class="sc-row">
        <button
          lgSigil
          [value]="11"
          label="Power"
          [state]="selected() === 'power' ? 'selected' : 'default'"
          (click)="selected.set('power')"
        ></button>
        <button
          lgSigil
          size="lg"
          [value]="9"
          label="Armor"
          [state]="selected() === 'armor' ? 'selected' : 'default'"
          (click)="selected.set('armor')"
        ></button>
      </div>
    </ng-template>

    <ng-template scStory="Ready" notes="An arcana-glow diamond: can be raised.">
      <div class="sc-row">
        <div
          lgSigil
          [value]="3"
          label="Resistance"
          labelPosition="left"
          state="ready"
        ></div>
        <div
          lgSigil
          [value]="18"
          label="Dex"
          labelPosition="left"
          size="sm"
          state="ready"
        ></div>
      </div>
    </ng-template>

    <ng-template
      scStory="Locked"
      notes="The hex empties and the value greys. With a press it stays a focusable button, and the condition opens in the reason tip."
    >
      <div class="sc-row">
        <button
          lgSigil
          [value]="1"
          label="Life Steal"
          labelPosition="bottom"
          state="locked"
          reason="Unlocks at level 20"
        ></button>
        <div lgSigil [value]="9" label="Int" state="locked"></div>
      </div>
    </ng-template>

    <ng-template scStory="Sizes" notes="sm 40px, md 52px, lg 68px.">
      <div class="sc-row">
        <div lgSigil size="sm" [value]="4" label="Tenacity"></div>
        <div lgSigil size="md" [value]="11" label="Power"></div>
        <div lgSigil size="lg" [value]="9" label="Armor"></div>
      </div>
    </ng-template>

    <ng-template
      scStory="Label positions"
      width="40rem"
      notes="Right by default; left, top, bottom, or none."
    >
      <div class="sc-grid" style="--sc-cell: 10rem">
        @for (p of positions; track p.position) {
          <div class="sc-cell">
            <p class="sc-cap">{{ p.position }}</p>
            <div
              lgSigil
              [value]="p.value"
              [label]="p.label"
              [labelPosition]="p.position"
            ></div>
          </div>
        }
      </div>
    </ng-template>
  `,
})
export class SigilShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly selected = signal<'power' | 'armor'>('armor');
  protected readonly positions: {
    position: LgSigilLabelPosition;
    value: string | number;
    label: string;
  }[] = [
    { position: 'right', value: 42, label: 'Power' },
    { position: 'left', value: 18, label: 'Armor' },
    { position: 'top', value: 11, label: 'Resistance' },
    { position: 'bottom', value: '9%', label: 'Crit Chance' },
    { position: 'none', value: 7, label: 'Dodge' },
  ];
}

export const SIGIL_SHOWCASE: ShowcaseEntry = {
  slug: 'sigil',
  name: 'Sigil',
  tier: 'game',
  summary: 'The hex stat badge.',
  covers: ['LgSigilComponent'],
  readme: 'src/app/grimoire/game/sigil/README.md',
  component: SigilShowcaseComponent,
};
