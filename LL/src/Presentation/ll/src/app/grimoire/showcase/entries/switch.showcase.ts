import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { LgSwitchComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-switch-showcase',
  imports: [ShowcaseStoryDirective, LgSwitchComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Off and on"
      notes="Off: the thumb at the start in ink-muted. On: the arcana-soft track and the thumb at the end in arcana-glow. A press works at once."
    >
      <div class="sc-col">
        <lg-switch>Compact lists</lg-switch>
        <lg-switch [(checked)]="newLook">New look</lg-switch>
      </div>
    </ng-template>

    <ng-template
      scStory="Disabled"
      notes="The words, the track and the thumb step back."
    >
      <div class="sc-col">
        <lg-switch disabled>Compact lists</lg-switch>
        <lg-switch [checked]="true" disabled>New look</lg-switch>
      </div>
    </ng-template>

    <ng-template
      scStory="Long words"
      width="16rem"
      notes="The words wrap beside the track."
    >
      <lg-switch [checked]="true"
        >Show damage numbers over the combatants in battle</lg-switch
      >
    </ng-template>
  `,
})
export class SwitchShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly newLook = signal(true);
}

export const SWITCH_SHOWCASE: ShowcaseEntry = {
  slug: 'switch',
  name: 'Switch',
  tier: 'primitives',
  summary: 'A setting, on or off at once.',
  covers: ['LgSwitchComponent'],
  readme: 'src/app/grimoire/primitives/switch/README.md',
  component: SwitchShowcaseComponent,
};
