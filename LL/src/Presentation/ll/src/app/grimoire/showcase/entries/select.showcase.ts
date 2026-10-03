import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  LgFieldComponent,
  LgOptionComponent,
  LgSelectComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

const RARITIES = ['Common', 'Uncommon', 'Rare', 'Epic', 'Unique', 'Legendary'];

@Component({
  selector: 'sc-select-showcase',
  imports: [
    ShowcaseStoryDirective,
    ReactiveFormsModule,
    LgSelectComponent,
    LgOptionComponent,
    LgFieldComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Placeholder"
      width="20rem"
      notes="A well, as an Input is, with the expand chevron at its end; the placeholder in ink-muted until a choice. The Field's label names it."
    >
      <lg-field label="Rarity">
        <lg-select placeholder="Any rarity">
          @for (r of rarities; track r) {
            <lg-option [value]="r">{{ r }}</lg-option>
          }
        </lg-select>
      </lg-field>
    </ng-template>

    <ng-template scStory="Chosen" width="20rem" notes="The choice in ink.">
      <lg-field label="Rarity">
        <lg-select [formControl]="chosen">
          @for (r of rarities; track r) {
            <lg-option [value]="r">{{ r }}</lg-option>
          }
        </lg-select>
      </lg-field>
    </ng-template>

    <ng-template
      scStory="Error"
      width="20rem"
      notes="Touched and still empty: the Field's error under it, its edge in danger."
    >
      <lg-field
        label="Character class"
        [messages]="{ required: 'Choose a class.' }"
      >
        <lg-select [formControl]="missing" placeholder="Choose…">
          <lg-option value="warrior">Warrior</lg-option>
          <lg-option value="mage">Mage</lg-option>
        </lg-select>
      </lg-field>
    </ng-template>

    <ng-template
      scStory="Disabled"
      width="20rem"
      notes="The words and the edge step back."
    >
      <lg-field label="Rarity">
        <lg-select [formControl]="disabled">
          @for (r of rarities; track r) {
            <lg-option [value]="r">{{ r }}</lg-option>
          }
        </lg-select>
      </lg-field>
    </ng-template>

    <ng-template
      scStory="Open the list"
      width="20rem"
      height="22rem"
      notes="Interactive: a press or the down arrow opens the options on the overlay beneath it, the chosen one marked by the arcana-glow bar; arrows move, typing finds, Enter chooses, Escape closes. A disabled option is passed over."
    >
      <lg-field label="Rarity">
        <lg-select [formControl]="open" placeholder="Any rarity">
          @for (r of rarities; track r) {
            <lg-option [value]="r" [disabled]="r === 'Unique'">{{
              r
            }}</lg-option>
          }
        </lg-select>
      </lg-field>
    </ng-template>
  `,
})
export class SelectShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly rarities = RARITIES;
  protected readonly chosen = new FormControl<string | null>('Epic');
  protected readonly missing = new FormControl<string | null>(
    null,
    Validators.required,
  );
  protected readonly disabled = new FormControl<string | null>({
    value: 'Rare',
    disabled: true,
  });
  protected readonly open = new FormControl<string | null>('Rare');

  constructor() {
    super();
    this.missing.markAsTouched();
  }
}

export const SELECT_SHOWCASE: ShowcaseEntry = {
  slug: 'select',
  name: 'Select',
  tier: 'primitives',
  summary: 'One choice from a list, in a field.',
  covers: ['LgSelectComponent'],
  readme: 'src/app/grimoire/primitives/select/README.md',
  component: SelectShowcaseComponent,
};
