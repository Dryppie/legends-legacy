import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { LgCheckboxComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-checkbox-showcase',
  imports: [ShowcaseStoryDirective, LgCheckboxComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Default"
      notes="Unchecked, a well with a line-strong edge; checked, arcana-glow with a tick."
    >
      <div class="sc-col">
        <lg-checkbox [(checked)]="nobility">Show my Nobility</lg-checkbox>
        <lg-checkbox>Hide offline members</lg-checkbox>
      </div>
    </ng-template>

    <ng-template
      scStory="Mixed"
      notes="Some of a group: a dash. A press checks it."
    >
      <div class="sc-col">
        <lg-checkbox [checked]="false" [indeterminate]="true"
          >All channels</lg-checkbox
        >
        <lg-checkbox [checked]="true">General</lg-checkbox>
        <lg-checkbox>Trade</lg-checkbox>
      </div>
    </ng-template>

    <ng-template
      scStory="Disabled"
      notes="The words and the edge step back; it takes no press."
    >
      <div class="sc-col">
        <lg-checkbox disabled>Show my Nobility</lg-checkbox>
        <lg-checkbox [checked]="true" disabled
          >Remember this device</lg-checkbox
        >
      </div>
    </ng-template>

    <ng-template
      scStory="Long words"
      width="16rem"
      notes="The words wrap beside the box."
    >
      <lg-checkbox [checked]="true"
        >Show loot from other players in the Chronicle's Loot
        channel</lg-checkbox
      >
    </ng-template>
  `,
})
export class CheckboxShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly nobility = signal(true);
}

export const CHECKBOX_SHOWCASE: ShowcaseEntry = {
  slug: 'checkbox',
  name: 'Checkbox',
  tier: 'primitives',
  summary: 'A choice to tick, with its words.',
  covers: ['LgCheckboxComponent'],
  readme: 'src/app/grimoire/primitives/checkbox/README.md',
  component: CheckboxShowcaseComponent,
};
