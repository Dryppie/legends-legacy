import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgTagComponent } from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-tag-showcase',
  imports: [ShowcaseStoryDirective, LgTagComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="New"
      notes="Arcana on arcana-soft, for new, ready, claimable or actionable things only. The + is not read."
    >
      <div class="sc-row">
        <lg-tag state="new" label="New quest" />
        <lg-tag state="new" />
        <lg-tag state="ready" />
        <lg-tag state="claimable" />
      </div>
    </ng-template>

    <ng-template
      scStory="In progress"
      notes="The value follows the word, out of capitals."
    >
      <lg-tag state="in-progress" value="3 / 5" />
    </ng-template>

    <ng-template
      scStory="Completed"
      notes="Completed adds ✓ in ink; claimed is outlined in ink-muted."
    >
      <div class="sc-row">
        <lg-tag state="completed" />
        <lg-tag state="claimed" />
      </div>
    </ng-template>

    <ng-template
      scStory="Expiring"
      notes="Expires in 2h is outlined in warning; expired is neutral."
    >
      <div class="sc-row">
        <lg-tag state="expiring" value="2h" />
        <lg-tag state="expired" />
      </div>
    </ng-template>

    <ng-template scStory="Failed" notes="✕ in danger.">
      <lg-tag state="failed" />
    </ng-template>

    <ng-template
      scStory="Locked"
      notes="Outlined in ink-muted with a dashed edge: a gate the player will pass."
    >
      <lg-tag state="locked" />
    </ng-template>

    <ng-template
      scStory="Insufficient"
      notes="A shortfall, outlined in warning."
    >
      <lg-tag state="insufficient" value="12 Soulstones" />
    </ng-template>

    <ng-template
      scStory="Ownership"
      notes="Facts with no colour of their own are neutral. A label replaces the state's word where the state names something."
    >
      <div class="sc-row">
        <lg-tag state="equipped" />
        <lg-tag state="attuned" />
        <lg-tag state="assigned" label="In Preset 2" />
        <lg-tag state="listed" value="1,200 Cinders" />
        <lg-tag state="escrow" />
        <lg-tag state="borrowed" />
        <lg-tag state="undiscovered" />
        <lg-tag>Relic</lg-tag>
      </div>
    </ng-template>

    <ng-template
      scStory="Outcomes"
      notes="Status tones are outlined, never filled. Success adds ✓ itself."
    >
      <div class="sc-row">
        <lg-tag tone="success">Upgraded</lg-tag>
        <lg-tag tone="warning">Short by 12</lg-tag>
        <lg-tag tone="danger">Defeated</lg-tag>
      </div>
    </ng-template>

    <ng-template
      scStory="Effects"
      notes="The frame says the polarity: beneficial keeps its corners, harmful has them cut. Screen readers hear beneficial or harmful after the name."
    >
      <div class="sc-row">
        <lg-tag tone="beneficial">Empower</lg-tag>
        <lg-tag tone="beneficial" value="12s">Haste</lg-tag>
        <lg-tag tone="harmful" value="6s">Weaken</lg-tag>
        <lg-tag tone="harmful">Bleeding</lg-tag>
      </div>
    </ng-template>

    <ng-template
      scStory="Rarities"
      notes="Outlined in their rarity colour, and always naming the rarity."
    >
      <div class="sc-row">
        <lg-tag tone="common">Common</lg-tag>
        <lg-tag tone="uncommon">Uncommon</lg-tag>
        <lg-tag tone="rare">Rare</lg-tag>
        <lg-tag tone="epic">Epic</lg-tag>
        <lg-tag tone="unique">Unique</lg-tag>
        <lg-tag tone="legendary">Legendary</lg-tag>
        <lg-tag tone="legacy">Legacy</lg-tag>
      </div>
    </ng-template>
  `,
})
export class TagShowcaseComponent extends ShowcaseEntryComponent {}

export const TAG_SHOWCASE: ShowcaseEntry = {
  slug: 'tag',
  name: 'Tag',
  tier: 'primitives',
  summary: 'The status label.',
  covers: ['LgTagComponent'],
  readme: 'design-system/components/Tag/README.md',
  component: TagShowcaseComponent,
};
