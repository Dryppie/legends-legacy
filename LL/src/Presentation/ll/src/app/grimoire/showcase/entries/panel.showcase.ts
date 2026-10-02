import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LgListComponent,
  LgListRowComponent,
  LgPanelComponent,
  LgSlotDirective,
  LgTagComponent,
} from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-panel-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgPanelComponent,
    LgSlotDirective,
    LgTagComponent,
    LgListComponent,
    LgListRowComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="With aside"
      notes="Head extras, a Tag or a count, sit at the end of the head."
      width="20rem"
    >
      <lg-panel title="Pending loot">
        <lg-tag lgSlot="aside">4 items</lg-tag>
        <p>Retreat to secure your pending loot.</p>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Title at end"
      notes="titleAlign=end sets the title at the end of the head."
      width="20rem"
    >
      <lg-panel title="Lore" titleAlign="end">
        <p>
          The Ember Wolf is found in the ash fields east of Shenic. It hunts in
          pairs.
        </p>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="No title"
      notes="Without a title there is no head: only the body."
      width="20rem"
    >
      <lg-panel>
        <p>Retreat to secure your pending loot.</p>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Flush"
      notes="No body padding: a List runs edge to edge, and its names line up with the title."
      width="26rem"
    >
      <lg-panel title="Inventory" flush>
        <span lgSlot="aside">3 of 60</span>
        <lg-list label="Inventory">
          <li
            lgListRow
            title="Ember Fang"
            rarity="Epic"
            icon="colosseum"
            meta="Weapon · Sword · Lv 18"
            [quantity]="1"
            value="9,400"
          ></li>
          <li
            lgListRow
            title="Dire Wolf Essence"
            rarity="Rare"
            icon="essences"
            meta="Essence · Lv 12"
            [quantity]="3"
            value="1,250"
          ></li>
          <li
            lgListRow
            title="Iron Sabre"
            rarity="Common"
            icon="colosseum"
            meta="Weapon · Sword · Lv 4"
            [quantity]="2"
            value="85"
          ></li>
        </lg-list>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Densities"
      notes="Padding follows the density: 24px comfortable, 16px standard, 12px compact."
      width="58rem"
    >
      <div class="sc-grid" style="--sc-cell: 16rem">
        @for (d of densities; track d) {
          <div>
            <p class="sc-cap">{{ d }}</p>
            <lg-panel title="Pending loot" [density]="d">
              <lg-tag lgSlot="aside">4 items</lg-tag>
              <p>Retreat to secure your pending loot.</p>
            </lg-panel>
          </div>
        }
      </div>
    </ng-template>
  `,
})
export class PanelShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly densities = [
    'comfortable',
    'standard',
    'compact',
  ] as const;
}

export const PANEL_SHOWCASE: ShowcaseEntry = {
  slug: 'panel',
  name: 'Panel',
  tier: 'components',
  summary: 'The content box.',
  covers: ['LgPanelComponent'],
  readme: 'design-system/components/Panel/README.md',
  component: PanelShowcaseComponent,
};
