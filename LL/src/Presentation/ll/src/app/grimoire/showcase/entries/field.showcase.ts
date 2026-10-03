import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { LgFieldComponent, LgInputComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-field-showcase',
  imports: [
    ShowcaseStoryDirective,
    ReactiveFormsModule,
    LgFieldComponent,
    LgInputComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Default"
      width="22rem"
      notes="The label above the input, a hint under it: what to enter."
    >
      <lg-field label="Character name" hint="3 to 16 letters">
        <input lgInput [formControl]="name" />
      </lg-field>
    </ng-template>

    <ng-template
      scStory="Error"
      width="22rem"
      notes="Once the player has touched it and it is invalid, the error takes the hint's place: the ✕ and the words in danger, the edge in danger. It says what to do."
    >
      <lg-field
        label="Character name"
        hint="3 to 16 letters"
        [messages]="{ required: 'Name your character.' }"
      >
        <input lgInput [formControl]="touchedEmpty" />
      </lg-field>
    </ng-template>

    <ng-template
      scStory="Placeholder"
      width="22rem"
      notes="A placeholder is an example, never the label."
    >
      <lg-field label="Guild tag">
        <input lgInput placeholder="e.g. EMBR" />
      </lg-field>
    </ng-template>

    <ng-template
      scStory="Textarea"
      width="22rem"
      notes="Two lines at least; it grows downward when the player drags it."
    >
      <lg-field label="Guild message" hint="Shown to every member">
        <textarea lgInput [formControl]="note"></textarea>
      </lg-field>
    </ng-template>

    <ng-template
      scStory="Disabled"
      width="22rem"
      notes="The words and the edge step back."
    >
      <lg-field label="Email" hint="Bound to your account">
        <input lgInput [formControl]="locked" />
      </lg-field>
    </ng-template>

    <ng-template
      scStory="Densities"
      width="22rem"
      notes="Comfortable, Standard and Compact: the input is as tall as the region's controls."
    >
      <div class="sc-col">
        <div data-density="comfortable">
          <lg-field label="Comfortable"
            ><input lgInput value="Aldric"
          /></lg-field>
        </div>
        <div data-density="standard">
          <lg-field label="Standard"><input lgInput value="Aldric" /></lg-field>
        </div>
        <div data-density="compact">
          <lg-field label="Compact"><input lgInput value="Aldric" /></lg-field>
        </div>
      </div>
    </ng-template>
  `,
})
export class FieldShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly name = new FormControl('Aldric', [
    Validators.required,
    Validators.minLength(3),
  ]);
  protected readonly touchedEmpty = new FormControl('', Validators.required);
  protected readonly note = new FormControl(
    'Raid tonight at the usual hour. Bring fire resistance.',
  );
  protected readonly locked = new FormControl({
    value: 'aldric@example.com',
    disabled: true,
  });

  constructor() {
    super();
    this.touchedEmpty.markAsTouched();
  }
}

export const FIELD_SHOWCASE: ShowcaseEntry = {
  slug: 'field',
  name: 'Field',
  tier: 'primitives',
  summary: 'A labelled input, with its hint or error.',
  covers: ['LgFieldComponent', 'LgInputComponent'],
  readme: 'src/app/grimoire/primitives/field/README.md',
  component: FieldShowcaseComponent,
};
