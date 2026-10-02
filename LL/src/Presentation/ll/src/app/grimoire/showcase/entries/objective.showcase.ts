import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import {
  LgButtonComponent,
  LgObjectiveComponent,
  LgSlotDirective,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-objective-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgObjectiveComponent,
    LgButtonComponent,
    LgSlotDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="With progress"
      notes="The kicker, the quest's title, and the current objective with its count in tabular figures."
      width="29.25rem"
    >
      <lg-objective
        kicker="Quest"
        title="The First Hunt"
        objective="Defeat wolves in the Whispering Woods"
        [current]="3"
        [required]="5"
      />
    </ng-template>

    <ng-template
      scStory="Without progress"
      notes="No count while the objective has nothing to count; the title alone is enough."
      width="29.25rem"
    >
      <div class="sc-col">
        <div class="sc-cell">
          <p class="sc-cap">Objective without a count</p>
          <lg-objective
            kicker="Quest"
            title="Paths of the Ember"
            objective="Choose your reward"
          />
        </div>
        <div class="sc-cell">
          <p class="sc-cap">Title only</p>
          <lg-objective kicker="Quest" title="The First Hunt" />
        </div>
        <div class="sc-cell">
          <p class="sc-cap">No kicker</p>
          <lg-objective
            title="The First Hunt"
            objective="Defeat wolves"
            [current]="3"
            [required]="5"
          />
        </div>
      </div>
    </ng-template>

    <ng-template
      scStory="With tracker"
      notes="With a panel it is a disclosure button with an edge, closed at first; Tracker open shows its popover."
      width="29.25rem"
    >
      <lg-objective
        kicker="Quest"
        title="The First Hunt"
        objective="Defeat wolves in the Whispering Woods"
        [current]="3"
        [required]="5"
      >
        <div lgSlot="panel" class="sc-col">
          <p class="sc-cap">Welcome to Legends Legacy · Chain 1 of 4</p>
          <ul class="sc-col">
            <li><b>3 / 5</b> Defeat wolves in the Whispering Woods</li>
            <li><b>0 / 1</b> Attune an Essence</li>
          </ul>
          <button lgButton="link" size="sm">Open Quests</button>
        </div>
      </lg-objective>
    </ng-template>

    <ng-template
      scStory="Tracker open"
      notes="Open: the summary takes the raised wash and the panel rises in beneath it. Escape, a click outside or the button close it."
      width="29.25rem"
      height="16.5rem"
    >
      <lg-objective
        kicker="Quest"
        title="The First Hunt"
        objective="Defeat wolves in the Whispering Woods"
        [current]="3"
        [required]="5"
        [(open)]="trackerOpen"
      >
        <div lgSlot="panel" class="sc-col">
          <p class="sc-cap">Welcome to Legends Legacy · Chain 1 of 4</p>
          <ul class="sc-col">
            <li><b>3 / 5</b> Defeat wolves in the Whispering Woods</li>
            <li><b>0 / 1</b> Attune an Essence</li>
          </ul>
          <button lgButton="link" size="sm">Open Quests</button>
        </div>
      </lg-objective>
    </ng-template>

    <ng-template
      scStory="Long text"
      notes="Title and objective truncate; the count keeps its width."
      width="22rem"
    >
      <lg-objective
        kicker="Quest"
        title="The Long Road Through the Ashen Wastes of Shenic"
        objective="Defeat the Ember Knights guarding the old temple gates"
        [current]="1250"
        [required]="2000"
      />
    </ng-template>
  `,
})
export class ObjectiveShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly trackerOpen = signal(true);
}

export const OBJECTIVE_SHOWCASE: ShowcaseEntry = {
  slug: 'objective',
  name: 'Objective',
  tier: 'shell',
  summary: 'The pinned quest.',
  covers: ['LgObjectiveComponent'],
  readme: 'src/app/grimoire/shell/objective/README.md',
  component: ObjectiveShowcaseComponent,
};
