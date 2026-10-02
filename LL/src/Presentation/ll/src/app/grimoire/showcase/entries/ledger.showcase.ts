import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LG_LEDGER, LgDeltaPolarity, LgLedgerRow } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

/** A secondary stat with its change since the last look, for the two-up stories. */
interface StatChange {
  label: string;
  value: string;
  delta?: number;
  polarity?: LgDeltaPolarity;
  deltaText?: string;
}

@Component({
  selector: 'sc-ledger-showcase',
  imports: [ShowcaseStoryDirective, ...LG_LEDGER],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Default"
      notes="Labels and values joined by dotted leaders, one div lgLedgerRow each. Hover, focus or click a row to see what it means; Up, Down, Home, End and a label's first letters move between them."
      width="20rem"
    >
      <lg-ledger heading="Offense">
        <div
          lgLedgerRow
          label="Power"
          value="142"
          description="Raw force behind every blow and spell."
        ></div>
        <div
          lgLedgerRow
          label="Crit Chance"
          value="9.5%"
          description="Chance for a hit to land as a critical strike."
        ></div>
        <div
          lgLedgerRow
          label="Attack Speed"
          value="1.12"
          description="Attacks per second before cooldowns and abilities."
        ></div>
        <div
          lgLedgerRow
          label="Armor Penetration"
          value="12"
          description="Ignores part of the target's Armor."
        ></div>
      </lg-ledger>
    </ng-template>

    <ng-template
      scStory="Sub-line"
      notes="sub is a second line under the value; tipMeta is a footnote in the explanation."
      width="20rem"
    >
      <lg-ledger heading="Defense">
        @for (row of defense; track row.label) {
          <div
            lgLedgerRow
            [label]="row.label"
            [value]="row.value"
            [sub]="row.sub"
            [description]="row.description"
            [tipMeta]="row.tipMeta"
          ></div>
        }
      </lg-ledger>
    </ng-template>

    <ng-template
      scStory="Mixed values"
      notes="Values align on the decimal point; a unit is set smaller and muted; a range or a word stays whole."
      width="20rem"
    >
      <lg-ledger heading="Offense">
        @for (row of mixed; track row.label) {
          <div
            lgLedgerRow
            [label]="row.label"
            [value]="row.value"
            [sub]="row.sub"
            [description]="row.description"
            [tipMeta]="row.tipMeta"
          ></div>
        }
      </lg-ledger>
    </ng-template>

    <ng-template
      scStory="Missing values"
      notes="Never empty: a value that does not apply shows —, and zero shows 0."
      width="20rem"
    >
      <lg-ledger heading="Offense">
        @for (row of missing; track row.label) {
          <div
            lgLedgerRow
            [label]="row.label"
            [value]="row.value"
            [muted]="!!row.muted"
          ></div>
        }
      </lg-ledger>
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
            <lg-ledger heading="Offense" [density]="d">
              @for (row of offense; track row.label) {
                <div
                  lgLedgerRow
                  [label]="row.label"
                  [value]="row.value"
                  [description]="row.description"
                ></div>
              }
            </lg-ledger>
          </div>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="Two-up"
      notes="columns=2 sets the rows two a line, each column aligning its own values: secondary stats in a narrow column such as the Folio's, with their changes under the values. One short word a label; a longer one truncates. It replaces StatTile (D-137)."
      width="18.5rem"
    >
      <lg-ledger heading="Defenses" columns="2" density="comfortable">
        @for (s of changes; track s.label) {
          <div
            lgLedgerRow
            [label]="s.label"
            [value]="s.value"
            [delta]="s.delta"
            [deltaPolarity]="s.polarity ?? 'neutral'"
            [deltaText]="s.deltaText"
          ></div>
        }
      </lg-ledger>
    </ng-template>

    <ng-template
      scStory="Deltas"
      notes="A delta's colour comes from deltaPolarity, never from its sign: a cooldown falling is better. Without it the change is neutral; 0 shows ±0; deltaText when the default formatting will not do."
      width="20rem"
    >
      <lg-ledger heading="Since your last look">
        @for (s of judged; track s.label) {
          <div
            lgLedgerRow
            [label]="s.label"
            [value]="s.value"
            [delta]="s.delta"
            [deltaPolarity]="s.polarity ?? 'neutral'"
            [deltaText]="s.deltaText"
          ></div>
        }
      </lg-ledger>
    </ng-template>

    <ng-template
      scStory="Comparison"
      notes="An item compared with what is equipped: the new value, and under it the change and the current value (Patterns)."
      width="20rem"
    >
      <lg-ledger heading="If equipped">
        <div
          lgLedgerRow
          label="Power"
          value="154"
          [delta]="12"
          deltaPolarity="better"
          sub="now 142"
        ></div>
        <div
          lgLedgerRow
          label="Crit Chance"
          value="8.5%"
          [delta]="-1"
          deltaPolarity="worse"
          sub="now 9.5%"
        ></div>
      </lg-ledger>
    </ng-template>

    <ng-template
      scStory="Grid"
      notes="Several Ledgers in lg-ledgergrid: two a row at Medium and Narrow."
      width="40rem"
    >
      <div class="lg-region">
        <div class="lg-ledgergrid">
          @for (group of groups.slice(0, 2); track group.heading) {
            <lg-ledger [heading]="group.heading">
              @for (row of group.rows; track row.label) {
                <div
                  lgLedgerRow
                  [label]="row.label"
                  [value]="row.value"
                  [sub]="row.sub"
                  [description]="row.description"
                  [tipMeta]="row.tipMeta"
                ></div>
              }
            </lg-ledger>
          }
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
          @for (group of groups; track group.heading) {
            <lg-ledger [heading]="group.heading">
              @for (row of group.rows; track row.label) {
                <div
                  lgLedgerRow
                  [label]="row.label"
                  [value]="row.value"
                  [sub]="row.sub"
                  [description]="row.description"
                  [tipMeta]="row.tipMeta"
                ></div>
              }
            </lg-ledger>
          }
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
  protected readonly groups = [
    { heading: 'Offense', rows: this.offense },
    { heading: 'Defense', rows: this.defense },
    { heading: 'Recovery', rows: this.recovery },
    { heading: 'Utility', rows: this.utility },
  ];
  protected readonly changes: readonly StatChange[] = [
    { label: 'Armor', value: '45' },
    { label: 'Resist', value: '30', delta: 4, polarity: 'better' },
    { label: 'Crit', value: '12%' },
    { label: 'Dodge', value: '8%', delta: -2, polarity: 'worse' },
    {
      label: 'Speed',
      value: '1.12',
      delta: 0.1,
      deltaText: '0.10',
      polarity: 'better',
    },
    { label: 'Block', value: '5%', delta: 0 },
  ];
  protected readonly judged: readonly StatChange[] = [
    { label: 'Resist', value: '30', delta: 4, polarity: 'better' },
    { label: 'Cooldown', value: '6.8s', delta: -1.2, polarity: 'better' },
    { label: 'Dodge', value: '8%', delta: -2, polarity: 'worse' },
    { label: 'Block', value: '5%', delta: 0 },
    { label: 'Threat', value: '184', delta: 12 },
    {
      label: 'Attack Speed',
      value: '1.12',
      delta: 0.1,
      deltaText: '0.10',
      polarity: 'better',
    },
  ];
}

export const LEDGER_SHOWCASE: ShowcaseEntry = {
  slug: 'ledger',
  name: 'Ledger',
  tier: 'components',
  summary: 'The labelled value list.',
  covers: ['LgLedgerComponent', 'LgLedgerRowComponent'],
  readme: 'src/app/grimoire/components/ledger/README.md',
  component: LedgerShowcaseComponent,
};
