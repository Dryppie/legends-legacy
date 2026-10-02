import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LgLedgerComponent,
  LgLedgerRow,
} from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-ledger-showcase',
  imports: [ShowcaseStoryDirective, LgLedgerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Default"
      notes="Labels and values joined by dotted leaders. Hover, focus or click a row to see what it means."
      width="20rem"
    >
      <lg-ledger title="Offense" [rows]="offense" />
    </ng-template>

    <ng-template
      scStory="Sub-line"
      notes="sub is a second line under the value; tipMeta is a footnote in the explanation."
      width="20rem"
    >
      <lg-ledger title="Defense" [rows]="defense" />
    </ng-template>

    <ng-template
      scStory="Mixed values"
      notes="Values align on the decimal point; a unit is set smaller and muted; a range or a word stays whole."
      width="20rem"
    >
      <lg-ledger title="Offense" [rows]="mixed" />
    </ng-template>

    <ng-template
      scStory="Missing values"
      notes="Never empty: a value that does not apply shows —, and zero shows 0."
      width="20rem"
    >
      <lg-ledger title="Offense" [rows]="missing" />
    </ng-template>

    <ng-template
      scStory="Densities"
      notes="48, 40 and 32px rows; compact sets values in numeral-compact."
      width="68rem"
    >
      <div class="sc-grid" style="--sc-cell: 18rem">
        @for (d of densities; track d) {
          <div>
            <p class="sc-cap">{{ d }}</p>
            <lg-ledger title="Offense" [rows]="offense" [density]="d" />
          </div>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="Grid"
      notes="Several Ledgers in lg-ledgergrid: two a row at Medium and Narrow."
      width="40rem"
    >
      <div class="lg-region">
        <div class="lg-ledgergrid">
          <lg-ledger title="Offense" [rows]="offense" />
          <lg-ledger title="Defense" [rows]="defense" />
        </div>
      </div>
    </ng-template>

    <ng-template
      scStory="Wide grid"
      notes="Four a row in a Wide region (68rem and up)."
      width="74rem"
    >
      <div class="lg-region">
        <div class="lg-ledgergrid">
          <lg-ledger title="Offense" [rows]="offense" />
          <lg-ledger title="Defense" [rows]="defense" />
          <lg-ledger title="Recovery" [rows]="recovery" />
          <lg-ledger title="Utility" [rows]="utility" />
        </div>
      </div>
    </ng-template>
  `,
})
export class LedgerShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly densities = [
    'comfortable',
    'standard',
    'compact',
  ] as const;
  protected readonly offense: readonly LgLedgerRow[] = [
    {
      label: 'Power',
      value: '142',
      description: 'Raw force behind every blow and spell.',
    },
    {
      label: 'Crit Chance',
      value: '9.5%',
      description: 'Chance for a hit to land as a critical strike.',
    },
    {
      label: 'Attack Speed',
      value: '1.12',
      description: 'Attacks per second before cooldowns and abilities.',
    },
    {
      label: 'Armor Penetration',
      value: '12',
      description: "Ignores part of the target's Armor.",
    },
  ];
  protected readonly defense: readonly LgLedgerRow[] = [
    {
      label: 'Max Health',
      value: '4,150',
      description: 'Health you enter every fight with.',
    },
    {
      label: 'Armor',
      value: '38%',
      sub: '240 Armor Rating',
      description: 'Armor is combined and converted with diminishing returns.',
      tipMeta: 'From equipment: 240 Armor Rating',
    },
    {
      label: 'Block',
      value: '4%',
      description: 'Chance to block part of an incoming hit.',
    },
  ];
  protected readonly mixed: readonly LgLedgerRow[] = [
    {
      label: 'Power',
      value: 1284,
      description: 'How hard you hit.',
      tipMeta: 'From equipment',
    },
    { label: 'Crit Chance', value: '24.8%', sub: '+3% from Essences' },
    {
      label: 'Health Regen',
      value: '84 HP/5s',
      description: 'Health restored every five seconds.',
    },
    { label: 'Range', value: '12–18' },
    { label: 'Style', value: 'Reaper' },
  ];
  protected readonly missing: readonly LgLedgerRow[] = [
    { label: 'Power', value: 1284 },
    { label: 'Armor Penetration', value: 0 },
    { label: 'Magic Penetration', value: null, muted: true },
  ];
  protected readonly recovery: readonly LgLedgerRow[] = [
    { label: 'Restoration', value: '18' },
    { label: 'Healing Power', value: '6%' },
    { label: 'Health Regen', value: '84 HP/5s' },
    { label: 'Life Steal', value: '2.5%' },
  ];
  protected readonly utility: readonly LgLedgerRow[] = [
    { label: 'Ability Haste', value: '15' },
    { label: 'Tenacity', value: '10%' },
    { label: 'Cooldown', value: '1.5s' },
    { label: 'Threat', value: '184.6 threat/s' },
  ];
}

export const LEDGER_SHOWCASE: ShowcaseEntry = {
  slug: 'ledger',
  name: 'Ledger',
  tier: 'components',
  summary: 'The labelled value list.',
  covers: ['LgLedgerComponent'],
  readme: 'design-system/components/Ledger/README.md',
  component: LedgerShowcaseComponent,
};
