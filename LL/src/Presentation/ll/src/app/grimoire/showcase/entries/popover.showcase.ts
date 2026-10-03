import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { LG_POPOVER, LgButtonComponent, LgCheckboxComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-popover-showcase',
  imports: [
    ShowcaseStoryDirective,
    ...LG_POPOVER,
    LgButtonComponent,
    LgCheckboxComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Open"
      width="20rem"
      height="16rem"
      notes="A Level 2 float on the overlay beneath its element (below-start here: lined up with its start), a non-modal dialog named by its label. Its element says it is expanded."
    >
      <button
        lgButton
        [lgPopoverTrigger]="filters"
        lgPopoverPlace="below-start"
        [(lgPopoverOpen)]="open"
      >
        Filters
      </button>
      <lg-popover #filters label="Filters" width="16rem">
        <div class="sc-col">
          <lg-checkbox [checked]="true">Equipped</lg-checkbox>
          <lg-checkbox>Favourites</lg-checkbox>
          <lg-checkbox>Listed on the Bazaar</lg-checkbox>
        </div>
      </lg-popover>
    </ng-template>

    <ng-template
      scStory="Beside its element"
      width="26rem"
      height="12rem"
      notes="lgPopoverPlace=end opens it beside its element; below-start lines it up with its start."
    >
      <button
        lgButton="quiet"
        [lgPopoverTrigger]="note"
        lgPopoverPlace="end"
        [(lgPopoverOpen)]="besideOpen"
      >
        Set bonus
      </button>
      <lg-popover #note label="Set bonus" width="14rem">
        <p class="sc-cap">
          2 of 4 Ashen pieces: +6% Power. 4 of 4: your hits burn for 3 seconds.
        </p>
      </lg-popover>
    </ng-template>

    <ng-template
      scStory="Press to open"
      width="20rem"
      height="16rem"
      notes="Interactive: a press opens it and focus moves inside; Escape, a second press or Tab past its end close it with focus back on the button; a press outside closes it where you pressed."
    >
      <button lgButton [lgPopoverTrigger]="more" lgPopoverPlace="below-start">
        Filters
      </button>
      <lg-popover #more label="Filters" width="16rem">
        <div class="sc-col">
          <lg-checkbox>Equipped</lg-checkbox>
          <lg-checkbox>Favourites</lg-checkbox>
          <button lgButton size="sm">Apply</button>
        </div>
      </lg-popover>
    </ng-template>
  `,
})
export class PopoverShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly open = signal(true);
  protected readonly besideOpen = signal(true);
}

export const POPOVER_SHOWCASE: ShowcaseEntry = {
  slug: 'popover',
  name: 'Popover',
  tier: 'primitives',
  summary: 'A float that opens on a press, holding controls of its own.',
  covers: ['LgPopoverComponent', 'LgPopoverTriggerDirective'],
  readme: 'src/app/grimoire/primitives/popover/README.md',
  component: PopoverShowcaseComponent,
};
