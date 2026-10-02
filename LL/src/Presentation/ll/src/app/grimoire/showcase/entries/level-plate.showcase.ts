import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgLevelPlateComponent, LgLevelPlateStat } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-level-plate-showcase',
  imports: [ShowcaseStoryDirective, LgLevelPlateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="With stats"
      width="21.75rem"
      notes="The level numeral in gilt Marcellus, the kicker running vertically, two headline stats and the progress: 350 / 24,500 EXP."
    >
      <lg-level-plate
        [level]="142"
        [xp]="350"
        [xpMax]="24500"
        [aside]="stats"
      />
    </ng-template>

    <ng-template
      scStory="Other progress"
      width="16.75rem"
      notes="Another progress with xpUnit, here an Essence's."
    >
      <lg-level-plate [level]="14" [xp]="7" [xpMax]="10" xpUnit="Essence" />
    </ng-template>

    <ng-template
      scStory="Stat units"
      width="21.75rem"
      notes="An aside stat can take a unit: 24.8%."
    >
      <lg-level-plate
        [level]="17"
        [xp]="8420"
        [xpMax]="12000"
        xpUnit="Combat XP"
        [aside]="unitStats"
      />
    </ng-template>

    <ng-template
      scStory="Level only"
      notes="Without xpMax there is no progress bar."
    >
      <lg-level-plate [level]="3" />
    </ng-template>
  `,
})
export class LevelPlateShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly stats: LgLevelPlateStat[] = [
    { icon: 'colosseum', label: 'Power', value: 281 },
    { icon: 'overview', label: 'Armor', value: 74 },
  ];
  protected readonly unitStats: LgLevelPlateStat[] = [
    { label: 'Combat Rating', value: 1284, icon: 'leaderboard' },
    { label: 'Crit', value: 24.8, unit: '%' },
  ];
}

export const LEVEL_PLATE_SHOWCASE: ShowcaseEntry = {
  slug: 'level-plate',
  name: 'LevelPlate',
  tier: 'game',
  summary: 'The level display.',
  covers: ['LgLevelPlateComponent'],
  readme: 'src/app/grimoire/game/level-plate/README.md',
  component: LevelPlateShowcaseComponent,
};
