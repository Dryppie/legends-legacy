import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LgKeyHint,
  LgKeyHintsComponent,
} from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-key-hints-showcase',
  imports: [ShowcaseStoryDirective, LgKeyHintsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Default"
      notes="The keyboard shortcuts at the stage's foot: muted labels and Key caps. Only keys that work on this screen."
    >
      <lg-key-hints [hints]="three" />
    </ng-template>

    <ng-template scStory="Two hints">
      <lg-key-hints [hints]="two" />
    </ng-template>

    <ng-template scStory="Four hints" notes="Four is the most a screen shows.">
      <lg-key-hints [hints]="four" />
    </ng-template>
  `,
})
export class KeyHintsShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly three: readonly LgKeyHint[] = [
    { label: 'Back', key: 'Esc' },
    { label: 'Buy', key: 'B' },
    { label: 'Select', key: '↵' },
  ];
  protected readonly two: readonly LgKeyHint[] = [
    { label: 'Back', key: 'Esc' },
    { label: 'Select', key: '↵' },
  ];
  protected readonly four: readonly LgKeyHint[] = [
    { label: 'Back', key: 'Esc' },
    { label: 'View breakdown', key: 'E' },
    { label: 'Attune', key: 'A' },
    { label: 'Select', key: '↵' },
  ];
}

export const KEY_HINTS_SHOWCASE: ShowcaseEntry = {
  slug: 'key-hints',
  name: 'KeyHints',
  tier: 'components',
  summary: 'The keyboard shortcut hints.',
  covers: ['LgKeyHintsComponent'],
  readme: 'design-system/components/KeyHints/README.md',
  component: KeyHintsShowcaseComponent,
};
