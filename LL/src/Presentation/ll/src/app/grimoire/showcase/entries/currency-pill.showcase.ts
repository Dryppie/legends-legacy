import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgCurrencyPillComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-currency-pill-showcase',
  imports: [ShowcaseStoryDirective, LgCurrencyPillComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Full figure"
      notes="Art, amount, name. A pill that only shows an amount has a line edge and is not focusable."
    >
      <div class="sc-row">
        <span
          lgCurrencyPill
          name="Cinders"
          [amount]="12480"
          [iconSrc]="cinders"
        ></span>
        <span
          lgCurrencyPill
          name="Soulstones"
          [amount]="36"
          [iconSrc]="soulstones"
        ></span>
      </div>
    </ng-template>

    <ng-template
      scStory="Short"
      notes="Abbreviated, it is a button: with toggle a press switches between 12.5k and 12,480. Screen readers hear the full amount, and the tip shows it."
    >
      <button
        lgCurrencyPill
        name="Cinders"
        [amount]="12480"
        [iconSrc]="cinders"
        short
        toggle
      ></button>
    </ng-template>

    <ng-template
      scStory="With a press"
      notes="A button whose press is your own (click) keeps its format: the TopBar switches every pill at once. The edge is line-strong."
    >
      <div class="sc-row">
        <button
          lgCurrencyPill
          name="Cinders"
          [amount]="12480"
          [iconSrc]="cinders"
          short
        ></button>
        <button
          lgCurrencyPill
          name="Soulstones"
          [amount]="36"
          [iconSrc]="soulstones"
        ></button>
      </div>
    </ng-template>

    <ng-template
      scStory="Reserved width"
      notes="Below, room held for six characters from the start, so a live amount never reflows the TopBar."
    >
      <div class="sc-col">
        <span
          lgCurrencyPill
          name="Cinders"
          [amount]="980"
          [iconSrc]="cinders"
        ></span>
        <span
          lgCurrencyPill
          name="Cinders"
          [amount]="980"
          [iconSrc]="cinders"
          [reserve]="6"
        ></span>
      </div>
    </ng-template>
  `,
})
export class CurrencyPillShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly cinders = 'assets/game-emblems/cinders-v1-64.webp';
  protected readonly soulstones = 'assets/game-emblems/soulstones-v1-64.webp';
}

export const CURRENCY_PILL_SHOWCASE: ShowcaseEntry = {
  slug: 'currency-pill',
  name: 'CurrencyPill',
  tier: 'game',
  summary: 'The currency amount.',
  covers: ['LgCurrencyPillComponent'],
  readme: 'src/app/grimoire/game/currency-pill/README.md',
  component: CurrencyPillShowcaseComponent,
};
