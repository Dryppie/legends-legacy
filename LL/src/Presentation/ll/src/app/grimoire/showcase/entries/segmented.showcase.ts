import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { LG_SEGMENTED } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-segmented-showcase',
  imports: [ShowcaseStoryDirective, ...LG_SEGMENTED],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Default"
      notes="Joined segments in one well; the chosen one takes the arcana-soft fill and an arcana ring. The arrow keys move the choice."
    >
      <lg-segmented label="Text size" [(value)]="size">
        <lg-segment value="default">Default</lg-segment>
        <lg-segment value="large">Large</lg-segment>
        <lg-segment value="extra">Extra large</lg-segment>
      </lg-segmented>
    </ng-template>

    <ng-template scStory="Two choices">
      <lg-segmented label="Reading font" value="marcellus">
        <lg-segment value="marcellus">Marcellus</lg-segment>
        <lg-segment value="system">System</lg-segment>
      </lg-segmented>
    </ng-template>

    <ng-template
      scStory="Label hidden"
      notes="Where the setting's name is already beside it, the label is for screen readers only."
    >
      <div class="sc-row">
        <span class="sc-cap">Damage numbers</span>
        <lg-segmented label="Damage numbers" labelHidden value="short">
          <lg-segment value="full">Full</lg-segment>
          <lg-segment value="short">Short</lg-segment>
          <lg-segment value="off">Off</lg-segment>
        </lg-segmented>
      </div>
    </ng-template>

    <ng-template
      scStory="A disabled segment"
      notes="Shown, stepped back, passed over by the keys."
    >
      <lg-segmented label="Chat layout" value="docked">
        <lg-segment value="docked">Docked</lg-segment>
        <lg-segment value="floating">Floating</lg-segment>
        <lg-segment value="hidden" disabled>Hidden</lg-segment>
      </lg-segmented>
    </ng-template>

    <ng-template scStory="Disabled">
      <lg-segmented label="Text size" value="large" disabled>
        <lg-segment value="default">Default</lg-segment>
        <lg-segment value="large">Large</lg-segment>
      </lg-segmented>
    </ng-template>

    <ng-template scStory="Densities" notes="As tall as the region's controls.">
      <div class="sc-col">
        @for (d of densities; track d) {
          <div [attr.data-density]="d">
            <lg-segmented [label]="d" value="b">
              <lg-segment value="a">First</lg-segment>
              <lg-segment value="b">Second</lg-segment>
            </lg-segmented>
          </div>
        }
      </div>
    </ng-template>
  `,
})
export class SegmentedShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly size = signal<unknown>('default');
  protected readonly densities = ['comfortable', 'standard', 'compact'];
}

export const SEGMENTED_SHOWCASE: ShowcaseEntry = {
  slug: 'segmented',
  name: 'Segmented',
  tier: 'primitives',
  summary: 'Two to four short choices, side by side.',
  covers: ['LgSegmentedComponent', 'LgSegmentComponent'],
  readme: 'src/app/grimoire/primitives/segmented/README.md',
  component: SegmentedShowcaseComponent,
};
