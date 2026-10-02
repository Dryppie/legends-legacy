import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgKeyComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-key-showcase',
  imports: [ShowcaseStoryDirective, LgKeyComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Letter"
      notes="A single key: a small rectangle at radius-control, in code-style capitals."
    >
      <div class="sc-row">
        <lg-key>E</lg-key>
        <lg-key>B</lg-key>
        <lg-key>A</lg-key>
      </div>
    </ng-template>

    <ng-template
      scStory="Named keys"
      notes="Words and symbols widen the cap; it never gets narrower than square."
    >
      <div class="sc-row">
        <lg-key>Esc</lg-key>
        <lg-key>↵</lg-key>
        <lg-key>Tab</lg-key>
        <lg-key>Shift</lg-key>
        <lg-key>←</lg-key>
        <lg-key>→</lg-key>
      </div>
    </ng-template>

    <ng-template
      scStory="Beside a label"
      notes="How KeyHints sets it: the label in ink-muted caption, then the cap."
    >
      <div class="sc-row">
        <p class="sc-cap">Back <lg-key>Esc</lg-key></p>
        <p class="sc-cap">Buy <lg-key>B</lg-key></p>
        <p class="sc-cap">Select <lg-key>↵</lg-key></p>
      </div>
    </ng-template>

    <ng-template scStory="In running text">
      <p>
        Press <lg-key>E</lg-key> to level up, or <lg-key>Esc</lg-key> to go
        back.
      </p>
    </ng-template>
  `,
})
export class KeyShowcaseComponent extends ShowcaseEntryComponent {}

export const KEY_SHOWCASE: ShowcaseEntry = {
  slug: 'key',
  name: 'Key cap',
  tier: 'primitives',
  summary: 'A keyboard key, drawn as a cap.',
  covers: ['LgKeyComponent'],
  readme: 'src/app/grimoire/primitives/key/README.md',
  component: KeyShowcaseComponent,
};
