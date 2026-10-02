import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgHeadingComponent } from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-heading-showcase',
  imports: [ShowcaseStoryDirective, LgHeadingComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Ramp"
      notes="Titles in engraved Marcellus, one per level of the ramp."
    >
      <div class="sc-col">
        <h2 lgHeading="folio">Power</h2>
        <h1 lgHeading="screen" sub="Ember">Wolf</h1>
        <h3 lgHeading="section">Pending loot</h3>
        <h4 lgHeading="subsection">Recent trades</h4>
      </div>
    </ng-template>

    <ng-template
      scStory="Folio"
      notes="title-xl, 56px, on an h2. One per screen, and never inside a list."
    >
      <h2 lgHeading="folio">Power</h2>
    </ng-template>

    <ng-template
      scStory="Screen"
      notes="title-lg, 36px, on an h1: the default level."
    >
      <h1 lgHeading>Character</h1>
    </ng-template>

    <ng-template
      scStory="With sub"
      notes="sub sets lighter leading words in ink-muted."
    >
      <h1 lgHeading="screen" sub="Ember">Wolf</h1>
    </ng-template>

    <ng-template scStory="Section" notes="title-md, 24px, on an h3.">
      <h3 lgHeading="section">Pending loot</h3>
    </ng-template>

    <ng-template scStory="Subsection" notes="title-sm, 20px, on an h4.">
      <h4 lgHeading="subsection">Recent trades</h4>
    </ng-template>

    <ng-template
      scStory="Long title"
      notes="A title wraps inside a narrow column; the sub stays on the first line."
      width="22.5rem"
    >
      <h1 lgHeading="screen" sub="Ember">Dire Wolf of the Ash Fields</h1>
    </ng-template>
  `,
})
export class HeadingShowcaseComponent extends ShowcaseEntryComponent {}

export const HEADING_SHOWCASE: ShowcaseEntry = {
  slug: 'heading',
  name: 'Heading',
  tier: 'primitives',
  summary: 'The titles.',
  covers: ['LgHeadingComponent'],
  readme: 'design-system/components/Heading/README.md',
  component: HeadingShowcaseComponent,
};
