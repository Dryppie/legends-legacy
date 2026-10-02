import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LG_PANEL,
  LgListComponent,
  LgListRowComponent,
  LgTagComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-panel-showcase',
  imports: [
    ShowcaseStoryDirective,
    ...LG_PANEL,
    LgTagComponent,
    LgListComponent,
    LgListRowComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="With aside"
      notes="Head extras, a Tag or a count, follow the title and sit at the end of the head."
      width="20rem"
    >
      <lg-panel>
        <lg-panel-header>
          <lg-panel-title>Pending loot</lg-panel-title>
          <lg-tag>4 items</lg-tag>
        </lg-panel-header>
        <p>Retreat to secure your pending loot.</p>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="Title at end"
      notes="align=end on the header sets the title at the end of the head."
      width="20rem"
    >
      <lg-panel>
        <lg-panel-header align="end">
          <lg-panel-title>Lore</lg-panel-title>
        </lg-panel-header>
        <p>
          The Ember Wolf is found in the ash fields east of Shenic. It hunts in
          pairs.
        </p>
      </lg-panel>
    </ng-template>

    <ng-template
      scStory="No title"
      notes="Without a header there is no head: only the body."
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
      <lg-panel flush>
        <lg-panel-header>
          <lg-panel-title>Inventory</lg-panel-title>
          <span>3 of 60</span>
        </lg-panel-header>
        <lg-list label="Inventory">
          <li
            lgListRow
            name="Ember Fang"
            rarity="Epic"
            icon="colosseum"
            meta="Weapon · Sword · Lv 18"
            [quantity]="1"
            amount="9,400"
          ></li>
          <li
            lgListRow
            name="Dire Wolf Essence"
            rarity="Rare"
            icon="essences"
            meta="Essence · Lv 12"
            [quantity]="3"
            amount="1,250"
          ></li>
          <li
            lgListRow
            name="Iron Sabre"
            rarity="Common"
            icon="colosseum"
            meta="Weapon · Sword · Lv 4"
            [quantity]="2"
            amount="85"
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
            <lg-panel [density]="d">
              <lg-panel-header>
                <lg-panel-title>Pending loot</lg-panel-title>
                <lg-tag>4 items</lg-tag>
              </lg-panel-header>
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
  covers: [
    'LgPanelComponent',
    'LgPanelHeaderComponent',
    'LgPanelTitleComponent',
  ],
  readme: 'src/app/grimoire/components/panel/README.md',
  component: PanelShowcaseComponent,
};
