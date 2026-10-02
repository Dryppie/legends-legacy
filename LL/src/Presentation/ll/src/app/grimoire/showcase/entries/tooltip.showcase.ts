import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgButtonComponent, LgTooltipDirective } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-tooltip-showcase',
  imports: [ShowcaseStoryDirective, LgButtonComponent, LgTooltipDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Words"
      notes="Hover or focus a control to see its tooltip. The words are also its description, so screen readers hear them."
    >
      <div class="sc-row">
        <button
          lgButton="quiet"
          size="sm"
          lgTooltip="Sort by rarity, then by name"
        >
          Sort
        </button>
        <button
          lgButton="quiet"
          size="sm"
          lgTooltip="Show only items you can equip now"
        >
          Filter
        </button>
      </div>
    </ng-template>

    <ng-template
      scStory="Explanation"
      notes="What something means, as a Ledger row explains itself: a heading, the explanation and a footnote. A press pins it, so touch can read it."
    >
      <button lgButton="quiet" size="sm" lgTooltipPin [lgTooltip]="armor">
        Armor
      </button>
    </ng-template>

    <ng-template
      scStory="Beside the element"
      notes='lgTooltipPlace="end" sets it to the right of its element, as the compact rail does; with no room there it goes below.'
    >
      <button
        lgButton="quiet"
        size="sm"
        lgTooltip="Overview"
        lgTooltipPlace="end"
      >
        Overview
      </button>
    </ng-template>

    <ng-template
      scStory="Template"
      notes="Content of your own, from an ng-template; lgTooltipDescription gives screen readers its words."
    >
      <ng-template #rich
        ><b>Precision</b>: hit chance and critical chance</ng-template
      >
      <button
        lgButton="quiet"
        size="sm"
        [lgTooltip]="rich"
        lgTooltipDescription="Precision: hit chance and critical chance"
      >
        Precision
      </button>
    </ng-template>
  `,
})
export class TooltipShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly armor = {
    title: 'Armor',
    text: 'Reduces the physical damage you take.',
    meta: '240 Armor Rating from equipment',
    kind: 'explanation' as const,
  };
}

export const TOOLTIP_SHOWCASE: ShowcaseEntry = {
  slug: 'tooltip',
  name: 'Tooltip',
  tier: 'primitives',
  summary: 'A short explanation beside the thing under the pointer or focus.',
  covers: ['LgTooltipDirective', 'LgTooltipController'],
  readme: 'src/app/grimoire/primitives/tooltip/README.md',
  component: TooltipShowcaseComponent,
};
