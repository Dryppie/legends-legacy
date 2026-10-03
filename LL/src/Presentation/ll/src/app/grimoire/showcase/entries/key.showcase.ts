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
        <kbd lgKey>E</kbd>
        <kbd lgKey>B</kbd>
        <kbd lgKey>A</kbd>
      </div>
    </ng-template>

    <ng-template
      scStory="Named keys"
      notes="Words and symbols widen the cap; it never gets narrower than square."
    >
      <div class="sc-row">
        <kbd lgKey>Esc</kbd>
        <kbd lgKey>↵</kbd>
        <kbd lgKey>Tab</kbd>
        <kbd lgKey>Shift</kbd>
        <kbd lgKey>←</kbd>
        <kbd lgKey>→</kbd>
      </div>
    </ng-template>

    <ng-template
      scStory="Beside a label"
      notes="How KeyHints sets it: the label in ink-muted caption, then the cap."
    >
      <div class="sc-row">
        <p class="sc-cap">Back <kbd lgKey>Esc</kbd></p>
        <p class="sc-cap">Buy <kbd lgKey>B</kbd></p>
        <p class="sc-cap">Select <kbd lgKey>↵</kbd></p>
      </div>
    </ng-template>

    <ng-template scStory="In running text">
      <p>
        Press <kbd lgKey>E</kbd> to level up, or <kbd lgKey>Esc</kbd> to go
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
