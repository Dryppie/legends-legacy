import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { LgButtonComponent, LgIconButtonComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-icon-button-showcase',
  imports: [ShowcaseStoryDirective, LgIconButtonComponent, LgButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Quiet"
      notes="The default: the icon in ink-muted, no edge until hovered, when the surface-raised wash fades in and the icon turns ink. Its label is its name and its tooltip."
    >
      <div class="sc-row">
        <button lgIconButton="close" label="Close"></button>
        <button lgIconButton="expand" label="Show details"></button>
      </div>
    </ng-template>

    <ng-template
      scStory="Framed"
      notes="The primary Button's surface and edge, for a row of Buttons: as tall as they are."
    >
      <div class="sc-row">
        <button lgButton>Filter</button>
        <button
          lgIconButton="expand"
          variant="framed"
          label="More filters"
        ></button>
      </div>
    </ng-template>

    <ng-template
      scStory="Pressed"
      notes="A toggle (pressed set): on is Selected, an arcana-glow edge inside; its label says what a press does next."
    >
      <div class="sc-row">
        <button
          lgIconButton="expand"
          [label]="open() ? 'Hide details' : 'Show details'"
          [pressed]="open()"
          (click)="open.set(!open())"
        ></button>
        <button
          lgIconButton="expand"
          label="Show details"
          [pressed]="false"
        ></button>
      </div>
    </ng-template>

    <ng-template
      scStory="Sizes"
      notes="Without a size it follows its region's density, as a Button does; md reads Standard (40px) and sm Compact (32px)."
    >
      <div class="sc-row">
        <span data-density="comfortable"
          ><button lgIconButton="close" label="Close"></button
        ></span>
        <button lgIconButton="close" label="Close" size="md"></button>
        <button lgIconButton="close" label="Close" size="sm"></button>
      </div>
    </ng-template>

    <ng-template
      scStory="Blocked and disabled"
      notes="Blocked stays focusable and answers a press with its reason, the action named above it; Locked's framed edge is dashed. Plain disabled leaves the Tab order."
    >
      <div class="sc-row">
        <button
          lgIconButton="expand"
          label="Sort by level"
          state="unavailable"
          reason="Not while the list loads"
        ></button>
        <button
          lgIconButton="expand"
          variant="framed"
          label="Filter by set"
          state="locked"
          reason="Unlocks at level 20"
        ></button>
        <button lgIconButton="close" label="Remove filter" disabled></button>
      </div>
    </ng-template>
  `,
})
export class IconButtonShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly open = signal(true);
}

export const ICON_BUTTON_SHOWCASE: ShowcaseEntry = {
  slug: 'icon-button',
  name: 'Icon button',
  tier: 'primitives',
  summary: 'A common action shown by its icon alone, named by its label.',
  covers: ['LgIconButtonComponent'],
  readme: 'src/app/grimoire/primitives/icon-button/README.md',
  component: IconButtonShowcaseComponent,
};
