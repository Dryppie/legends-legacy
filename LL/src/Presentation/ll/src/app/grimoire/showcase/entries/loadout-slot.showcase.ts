import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgLoadoutAbility, LgLoadoutSlotComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-loadout-slot-showcase',
  imports: [ShowcaseStoryDirective, LgLoadoutSlotComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Attuned"
      width="23rem"
      notes="A rarity-edged slot, the name in its rarity colour, its two abilities and a neutral Attuned Tag."
    >
      <lg-loadout-slot
        [index]="0"
        state="attuned"
        name="Ember Wolf Essence"
        rarity="Rare"
        [active]="active"
        [passive]="passive"
        interactive
      />
    </ng-template>

    <ng-template
      scStory="Open"
      width="23rem"
      notes="The empty frame and Empty, with the hint saying what fills it. No Tag."
    >
      <lg-loadout-slot
        [index]="1"
        state="open"
        hint="Choose an Essence to attune"
        interactive
      />
    </ng-template>

    <ng-template
      scStory="Locked"
      width="23rem"
      notes="A dashed frame, a Locked Tag and the unlock condition printed as the name. It stays a focusable button, aria-disabled."
    >
      <lg-loadout-slot
        [index]="2"
        state="locked"
        reason="Unlocks at level 20"
        interactive
      />
    </ng-template>

    <ng-template
      scStory="Ready"
      width="23rem"
      notes="The attention diamond at the end of the head, after the Tag. A locked slot takes none."
    >
      <div class="sc-col">
        <lg-loadout-slot
          [index]="1"
          state="open"
          hint="Choose an Essence to attune"
          ready="Essence ready to attune"
          interactive
        />
        <lg-loadout-slot
          [index]="0"
          name="Ember Wolf Essence"
          rarity="Rare"
          [ready]="true"
          interactive
        />
        <lg-loadout-slot
          [index]="2"
          state="locked"
          reason="Unlocks at level 20"
          [ready]="true"
          interactive
        />
      </div>
    </ng-template>

    <ng-template
      scStory="Not interactive"
      width="23rem"
      notes="Without a press the edge is line, not line-strong, and there is no hover."
    >
      <div class="sc-col">
        <lg-loadout-slot
          [index]="0"
          name="Ember Wolf Essence"
          rarity="Rare"
          [active]="active"
          [passive]="passive"
        />
        <lg-loadout-slot [index]="1" hint="Choose an Essence" />
        <lg-loadout-slot
          [index]="2"
          state="locked"
          reason="Unlocks at level 20"
        />
      </div>
    </ng-template>

    <ng-template
      scStory="Compact attuned"
      width="23rem"
      notes="One short row for a full loadout: the name leads, the slot number is for screen readers, and there is no Attuned Tag."
    >
      <lg-loadout-slot
        compact
        [index]="0"
        state="attuned"
        name="Ember Wolf Essence"
        rarity="Rare"
        [active]="active"
        [passive]="passive"
        interactive
      />
    </ng-template>

    <ng-template
      scStory="Compact open"
      width="23rem"
      notes="An open compact slot says Empty; here with an Essence ready to attune."
    >
      <lg-loadout-slot
        compact
        [index]="1"
        state="open"
        ready="Essence ready to attune"
        interactive
      />
    </ng-template>

    <ng-template
      scStory="Compact locked"
      width="23rem"
      notes="A locked compact slot keeps its Locked Tag and condition."
    >
      <lg-loadout-slot
        compact
        [index]="2"
        state="locked"
        reason="Unlocks at level 20"
        interactive
      />
    </ng-template>

    <ng-template
      scStory="Compact loadout"
      width="23rem"
      notes="A full loadout, stacked as in the Character Overview."
    >
      <div class="sc-col">
        <lg-loadout-slot
          compact
          [index]="0"
          state="attuned"
          name="Ember Wolf Essence"
          rarity="Rare"
          [active]="active"
          [passive]="passive"
          interactive
        />
        <lg-loadout-slot
          compact
          [index]="1"
          state="open"
          ready="Essence ready to attune"
          interactive
        />
        <lg-loadout-slot
          compact
          [index]="2"
          state="locked"
          reason="Unlocks at level 20"
          interactive
        />
      </div>
    </ng-template>
  `,
})
export class LoadoutSlotShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly active: LgLoadoutAbility = {
    name: 'Cinder Bite',
    cooldown: '12s',
  };
  protected readonly passive: LgLoadoutAbility = { name: 'Pack Instinct' };
}

export const LOADOUT_SLOT_SHOWCASE: ShowcaseEntry = {
  slug: 'loadout-slot',
  name: 'LoadoutSlot',
  tier: 'game',
  summary: 'An Essence loadout slot.',
  covers: ['LgLoadoutSlotComponent'],
  readme: 'src/app/grimoire/game/loadout-slot/README.md',
  component: LoadoutSlotShowcaseComponent,
};
