import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import {
  LgButtonComponent,
  LgCurrencyPillComponent,
  LgGameShellComponent,
  LgObjectiveComponent,
  LgShellTopComponent,
  LgObjectivePanelComponent,
  LgTopBarCenterComponent,
  LgTopBarComponent,
  LgTrackComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-top-bar-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgTopBarComponent,
    LgTopBarCenterComponent,
    LgCurrencyPillComponent,
    LgObjectiveComponent,
    LgTrackComponent,
    LgButtonComponent,
    LgGameShellComponent,
    LgShellTopComponent,
    LgObjectivePanelComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Default"
      notes="Who you are and what you carry: the name, the level in gilt, and the currencies at the end."
      width="60rem"
      flush
    >
      <lg-top-bar heading="Aldric Vane" eyebrow="Lv. 42">
        <button
          lgCurrencyPill
          name="Cinders"
          [amount]="12480"
          iconSrc="assets/game-emblems/cinders-v1-64.webp"
          short
          toggle
        ></button>
        <span
          lgCurrencyPill
          name="Soulstones"
          [amount]="36"
          iconSrc="assets/game-emblems/soulstones-v1-64.webp"
        ></span>
      </lg-top-bar>
    </ng-template>

    <ng-template
      scStory="Pinned quest"
      notes="The centre holds one now thing: with no run in progress, the pinned quest's Objective, shown here with its tracker open."
      width="60rem"
      height="15.5rem"
      flush
    >
      <lg-top-bar heading="Aldric Vane" eyebrow="Lv. 42">
        <lg-top-bar-center>
          <lg-objective
            kicker="Quest"
            heading="The First Hunt"
            objective="Defeat wolves in the Whispering Woods"
            [current]="3"
            [required]="5"
            [(open)]="questOpen"
          >
            <lg-objective-panel>
              <div class="sc-col">
                <p class="sc-cap">Welcome to Legends Legacy · Chain 1 of 4</p>
                <ul class="sc-col">
                  <li><b>3 / 5</b> Defeat wolves in the Whispering Woods</li>
                  <li><b>0 / 1</b> Attune an Essence</li>
                </ul>
                <button lgButton="link" size="sm">Open Quests</button>
              </div>
            </lg-objective-panel>
          </lg-objective>
        </lg-top-bar-center>
        <button
          lgCurrencyPill
          name="Cinders"
          [amount]="12480"
          iconSrc="assets/game-emblems/cinders-v1-64.webp"
          short
          toggle
        ></button>
        <span
          lgCurrencyPill
          name="Soulstones"
          [amount]="36"
          iconSrc="assets/game-emblems/soulstones-v1-64.webp"
        ></span>
      </lg-top-bar>
    </ng-template>

    <ng-template
      scStory="Run in progress"
      notes="During a dungeon run or tower climb the run's Track takes the centre."
      width="60rem"
      flush
    >
      <lg-top-bar heading="Aldric Vane" eyebrow="Lv. 42">
        <lg-top-bar-center>
          <lg-track
            [steps]="5"
            [current]="2"
            tone="hp"
            startLabel="Floor 3"
            endLabel="Boss"
            label="World Tower"
          />
        </lg-top-bar-center>
        <button
          lgCurrencyPill
          name="Cinders"
          [amount]="12480"
          iconSrc="assets/game-emblems/cinders-v1-64.webp"
          short
          toggle
        ></button>
        <span
          lgCurrencyPill
          name="Soulstones"
          [amount]="36"
          iconSrc="assets/game-emblems/soulstones-v1-64.webp"
        ></span>
      </lg-top-bar>
    </ng-template>

    <ng-template
      scStory="Menu button"
      notes="showMenu adds the menu button, which shows in a GameShell under 60rem and opens its rail drawer."
      width="40rem"
      height="3.5rem"
      flush
    >
      <lg-game-shell height="3.5rem">
        <lg-shell-top>
          <lg-top-bar heading="Aldric Vane" eyebrow="Lv. 42" showMenu>
            <button
              lgCurrencyPill
              name="Cinders"
              [amount]="12480"
              iconSrc="assets/game-emblems/cinders-v1-64.webp"
              short
              toggle
            ></button>
          </lg-top-bar>
        </lg-shell-top>
      </lg-game-shell>
    </ng-template>

    <ng-template
      scStory="Narrow"
      notes="Under 40rem the currency names give way to their art; the centre Track drops its end labels when it gets narrow."
      width="36rem"
      flush
    >
      <lg-top-bar heading="Aldric Vane" eyebrow="Lv. 42">
        <lg-top-bar-center>
          <lg-track
            [steps]="5"
            [current]="2"
            tone="hp"
            startLabel="Floor 3"
            endLabel="Boss"
            label="World Tower"
          />
        </lg-top-bar-center>
        <button
          lgCurrencyPill
          name="Cinders"
          [amount]="12480"
          iconSrc="assets/game-emblems/cinders-v1-64.webp"
          short
          toggle
        ></button>
        <span
          lgCurrencyPill
          name="Soulstones"
          [amount]="36"
          iconSrc="assets/game-emblems/soulstones-v1-64.webp"
        ></span>
      </lg-top-bar>
    </ng-template>

    <ng-template
      scStory="Long title"
      notes="At the narrowest the name truncates and the currencies scroll sideways inside the bar."
      width="24rem"
      flush
    >
      <lg-top-bar heading="Aldric Vane the Unbroken" eyebrow="Lv. 42">
        <button
          lgCurrencyPill
          name="Cinders"
          [amount]="12480"
          iconSrc="assets/game-emblems/cinders-v1-64.webp"
          short
          toggle
        ></button>
        <span
          lgCurrencyPill
          name="Soulstones"
          [amount]="36"
          iconSrc="assets/game-emblems/soulstones-v1-64.webp"
        ></span>
      </lg-top-bar>
    </ng-template>
  `,
})
export class TopBarShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly questOpen = signal(true);
}

export const TOP_BAR_SHOWCASE: ShowcaseEntry = {
  slug: 'top-bar',
  name: 'TopBar',
  tier: 'shell',
  summary: 'The top bar.',
  covers: ['LgTopBarComponent', 'LgTopBarCenterComponent'],
  readme: 'src/app/grimoire/shell/top-bar/README.md',
  component: TopBarShowcaseComponent,
};
