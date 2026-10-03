import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { LG_RADIO_GROUP } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-radio-showcase',
  imports: [ShowcaseStoryDirective, ...LG_RADIO_GROUP],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Default"
      notes="One choice among a few, all in view: the chosen one's edge turns arcana with an arcana-glow square inside."
    >
      <lg-radio-group label="Chat layout" [(value)]="layout">
        <lg-radio value="docked">Docked</lg-radio>
        <lg-radio value="floating">Floating drawer</lg-radio>
      </lg-radio-group>
    </ng-template>

    <ng-template scStory="In a row" notes="Short choices can sit in a row.">
      <lg-radio-group label="Sidebar" orientation="horizontal" value="full">
        <lg-radio value="full">Full</lg-radio>
        <lg-radio value="compact">Compact</lg-radio>
      </lg-radio-group>
    </ng-template>

    <ng-template
      scStory="A disabled choice"
      notes="Shown, stepped back, and never chosen."
    >
      <lg-radio-group label="Loot rule" value="need">
        <lg-radio value="need">Need before greed</lg-radio>
        <lg-radio value="free">Free for all</lg-radio>
        <lg-radio value="master" disabled>Master looter</lg-radio>
      </lg-radio-group>
    </ng-template>

    <ng-template scStory="Disabled" notes="The whole group, legend and all.">
      <lg-radio-group label="Chat layout" value="docked" disabled>
        <lg-radio value="docked">Docked</lg-radio>
        <lg-radio value="floating">Floating drawer</lg-radio>
      </lg-radio-group>
    </ng-template>
  `,
})
export class RadioShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly layout = signal<unknown>('docked');
}

export const RADIO_SHOWCASE: ShowcaseEntry = {
  slug: 'radio',
  name: 'Radio group',
  tier: 'primitives',
  summary: 'One choice among a few, all in view.',
  covers: ['LgRadioGroupComponent', 'LgRadioComponent'],
  readme: 'src/app/grimoire/primitives/radio/README.md',
  component: RadioShowcaseComponent,
};
