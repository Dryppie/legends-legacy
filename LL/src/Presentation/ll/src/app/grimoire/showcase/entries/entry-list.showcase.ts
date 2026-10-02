import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import {
  LgEntry,
  LgEntryListComponent,
} from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

const NAMES = [
  'Alpha Wolf',
  'Bloodfang Wolf',
  'Dire Wolf',
  'Ember Wolf',
  'Horned Wolf',
  'Blackjaw Spider',
  'Giant Spider',
  'Cave Bat',
  'Ember Knight',
];

/**
 * The catalog's creature list: Horned Wolf has a point to spend (the attention diamond), Blackjaw Spider is new,
 * Cave Bat is undiscovered and the current Creature Focus (its one Tag), Ember Knight is locked.
 */
const CREATURES: readonly LgEntry[] = NAMES.map((n): LgEntry => {
  if (n === 'Cave Bat')
    return {
      id: n,
      name: 'Undiscovered',
      tag: 'Creature Focus',
      tagTone: 'neutral',
    };
  return {
    id: n,
    name: n,
    tag: n === 'Blackjaw Spider' ? '+ New' : undefined,
    locked: n === 'Ember Knight',
    reason: n === 'Ember Knight' ? 'Clear Floor 10 to unlock' : undefined,
    ready:
      n === 'Horned Wolf'
        ? '1 point to spend'
        : n === 'Ember Knight' || undefined,
  };
});

@Component({
  selector: 'sc-entry-list-showcase',
  imports: [ShowcaseStoryDirective, LgEntryListComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Standard"
      notes="24px names on 40px rows; the selected one takes the bar and the wash. The list scrolls, and its ends fade. Up and Down move the selection."
      width="22rem"
      height="28rem"
    >
      <lg-entry-list
        label="Creatures"
        [items]="creatures"
        [(activeId)]="active"
      />
    </ng-template>

    <ng-template
      scStory="Compact"
      notes="17px names on 32px rows, for a long roster."
      width="20rem"
      height="24rem"
    >
      <lg-entry-list
        label="Creatures, compact"
        density="compact"
        [items]="creatures"
        [(activeId)]="active"
      />
    </ng-template>

    <ng-template
      scStory="Locked"
      notes="A locked entry is never selected; hover or focus it, or move onto it with the arrows, to see how it unlocks."
      width="22rem"
      height="14rem"
    >
      <lg-entry-list
        label="Creatures"
        [items]="locked"
        [(activeId)]="lockedActive"
      />
    </ng-template>

    <ng-template
      scStory="Ready"
      notes="ready draws the attention diamond at the row's end; a locked entry takes none."
      width="22rem"
      height="14rem"
    >
      <lg-entry-list label="Creatures" [items]="ready" activeId="slime" />
    </ng-template>

    <ng-template
      scStory="No fade"
      notes="fade=false: the ends do not fade out."
      width="22rem"
      height="28rem"
    >
      <lg-entry-list
        label="Creatures"
        [items]="creatures"
        activeId="Ember Wolf"
        [fade]="false"
      />
    </ng-template>

    <ng-template
      scStory="Narrow"
      notes="Names truncate; a Tag keeps its words."
      width="16rem"
      height="28rem"
    >
      <lg-entry-list
        label="Creatures"
        [items]="creatures"
        activeId="Ember Wolf"
      />
    </ng-template>
  `,
})
export class EntryListShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly creatures = CREATURES;
  protected readonly active = signal('Ember Wolf');
  protected readonly lockedActive = signal('wolf');
  protected readonly locked: readonly LgEntry[] = [
    { id: 'wolf', name: 'Ember Wolf', tag: 'New' },
    { id: 'slime', name: 'Blue Slime' },
    {
      id: 'drake',
      name: 'Ash Drake',
      locked: true,
      reason: 'Defeat the Ash Drake in Shenic',
    },
    { id: 'imp', name: 'Cinder Imp', tag: 'Rare', tagTone: 'rare' },
  ];
  protected readonly ready: readonly LgEntry[] = [
    { id: 'wolf', name: 'Ember Wolf', tag: 'New', ready: '1 point to spend' },
    { id: 'slime', name: 'Blue Slime', ready: true },
    {
      id: 'drake',
      name: 'Ash Drake',
      locked: true,
      reason: 'Defeat the Ash Drake in Shenic',
      ready: true,
    },
    {
      id: 'bat',
      name: 'Undiscovered',
      tag: 'Creature Focus',
      tagTone: 'neutral',
    },
  ];
}

export const ENTRY_LIST_SHOWCASE: ShowcaseEntry = {
  slug: 'entry-list',
  name: 'EntryList',
  tier: 'components',
  summary: 'The browsable name list.',
  covers: ['LgEntryListComponent'],
  readme: 'design-system/components/EntryList/README.md',
  component: EntryListShowcaseComponent,
};
