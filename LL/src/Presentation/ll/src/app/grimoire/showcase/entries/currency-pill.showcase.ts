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
        <lg-currency-pill name="Cinders" [amount]="12480" [iconSrc]="cinders" />
        <lg-currency-pill
          name="Soulstones"
          [amount]="36"
          [iconSrc]="soulstones"
        />
      </div>
    </ng-template>

    <ng-template
      scStory="Short"
      notes="Abbreviated, it is always a button: a press toggles between 12.5k and 12,480. Screen readers hear the full amount."
    >
      <lg-currency-pill
        name="Cinders"
        [amount]="12480"
        [iconSrc]="cinders"
        short
      />
    </ng-template>

    <ng-template
      scStory="With a press"
      notes="With a press of its own (interactive) it keeps its format and emits activate; the edge is line-strong."
    >
      <div class="sc-row">
        <lg-currency-pill
          name="Cinders"
          [amount]="12480"
          [iconSrc]="cinders"
          short
          interactive
        />
        <lg-currency-pill
          name="Soulstones"
          [amount]="36"
          [iconSrc]="soulstones"
          interactive
        />
      </div>
    </ng-template>

    <ng-template
      scStory="Reserved width"
      notes="Below, room held for six characters from the start, so a live amount never reflows the TopBar."
    >
      <div class="sc-col">
        <lg-currency-pill name="Cinders" [amount]="980" [iconSrc]="cinders" />
        <lg-currency-pill
          name="Cinders"
          [amount]="980"
          [iconSrc]="cinders"
          [reserve]="6"
        />
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
