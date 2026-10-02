import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LgSectionRuleComponent,
  LgSlotDirective,
} from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-section-rule-showcase',
  imports: [ShowcaseStoryDirective, LgSectionRuleComponent, LgSlotDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Band"
      notes="A line strip with an ink caption, for data groups in stat columns. Data groups only."
      width="26.25rem"
    >
      <lg-section-rule label="Status" />
    </ng-template>

    <ng-template
      scStory="End aligned"
      notes="align=end moves the label to the end."
      width="26.25rem"
    >
      <lg-section-rule label="Biography" align="end" />
    </ng-template>

    <ng-template
      scStory="With aside"
      notes="Extra content for a band goes in the aside slot."
      width="26.25rem"
    >
      <lg-section-rule label="Status"
        ><span lgSlot="aside">Live</span></lg-section-rule
      >
    </ng-template>

    <ng-template
      scStory="Hairline"
      notes="A hairline drawn beside its label, in ink-muted, and its aside: for any other group, where space and a heading are not enough."
      width="26.25rem"
    >
      <lg-section-rule variant="hairline" label="Rewards"
        ><span lgSlot="aside">3 of 5</span></lg-section-rule
      >
    </ng-template>

    <ng-template
      scStory="Plain hairline"
      notes="No label: the rule alone."
      width="26.25rem"
    >
      <lg-section-rule variant="hairline" align="end" />
    </ng-template>

    <ng-template
      scStory="Ornament"
      notes="A lattice of hollow diamonds masked in gilt, between lore and effects. At most once per surface."
      width="26.25rem"
    >
      <lg-section-rule variant="ornament" label="Effects" />
    </ng-template>
  `,
})
export class SectionRuleShowcaseComponent extends ShowcaseEntryComponent {}

export const SECTION_RULE_SHOWCASE: ShowcaseEntry = {
  slug: 'section-rule',
  name: 'SectionRule',
  tier: 'primitives',
  summary: 'The dividers.',
  covers: ['LgSectionRuleComponent'],
  readme: 'design-system/components/SectionRule/README.md',
  component: SectionRuleShowcaseComponent,
};
