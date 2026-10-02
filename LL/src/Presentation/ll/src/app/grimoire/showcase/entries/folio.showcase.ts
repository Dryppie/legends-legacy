import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LG_FOLIO,
  LG_LEDGER,
  LgButtonComponent,
  LgEmblemComponent,
  LgFolioEffect,
  LgItemSlotComponent,
  LgLedgerRow,
  LgTrackComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-folio-showcase',
  imports: [
    ShowcaseStoryDirective,
    ...LG_FOLIO,
    LgButtonComponent,
    LgEmblemComponent,
    LgItemSlotComponent,
    ...LG_LEDGER,
    LgTrackComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Attribute"
      notes="The emblem, eyebrow, heading, lore with its key nouns in bold, the ornament rule, effects, one action and a Track in the footer, each region its own element."
      width="22.5rem"
      height="44rem"
      flush
    >
      <lg-folio
        [cornerSrc]="corner"
        eyebrow="Attribute"
        heading="Power"
        [effects]="powerEffects"
      >
        <lg-folio-emblem>
          <lg-emblem [points]="6" [size]="148" />
        </lg-folio-emblem>
        <lg-folio-lore
          >Raw force behind every blow and spell. With <b>Power</b> as your
          weapon, you break what others only bend.</lg-folio-lore
        >
        <lg-folio-actions>
          <button lgButton hotkey="E">View breakdown</button>
        </lg-folio-actions>
        <lg-folio-footer>
          <lg-track
            [steps]="5"
            [current]="1"
            startLabel="Ascension"
            endLabel="Tier V"
            label="Legacy Ascension"
          />
        </lg-folio-footer>
      </lg-folio>
    </ng-template>

    <ng-template
      scStory="Item"
      notes="With a rarity it is an item context: the heading takes the rarity colour, a Tag names it, and magnitudes turn ink."
      width="22.5rem"
      height="44rem"
      flush
    >
      <lg-folio
        [cornerSrc]="corner"
        rarity="Epic"
        eyebrow="Essence"
        heading="Soul Prism"
        [effects]="prismEffects"
      >
        <lg-folio-emblem>
          <lg-item-slot rarity="Epic" icon="essences" [caption]="false" />
        </lg-folio-emblem>
        <lg-folio-lore
          >A shard of a warden’s heart, still humming with the <b>Prism</b> it
          once guarded.</lg-folio-lore
        >
        <lg-folio-actions>
          <button lgButton="solid" hotkey="A">Attune</button>
        </lg-folio-actions>
      </lg-folio>
    </ng-template>

    <ng-template scStory="Legendary item" width="22.5rem" height="44rem" flush>
      <lg-folio
        [cornerSrc]="corner"
        rarity="Legendary"
        eyebrow="Helm · Lv 20"
        heading="Crown of Cinders"
        [effects]="crownEffects"
      >
        <lg-folio-emblem>
          <lg-item-slot rarity="Legendary" icon="overview" [caption]="false" />
        </lg-folio-emblem>
        <lg-folio-actions>
          <button lgButton="solid" hotkey="E">Equip</button>
        </lg-folio-actions>
      </lg-folio>
    </ng-template>

    <ng-template
      scStory="Start aligned"
      notes="align=start left-aligns the content for stat-heavy details; a Ledger inside a Folio is Comfortable."
      width="22.5rem"
      height="44rem"
      flush
    >
      <lg-folio
        [cornerSrc]="corner"
        align="start"
        eyebrow="Creature"
        heading="Wolf"
        headingSub="Ember"
        [effects]="wolfEffects"
      >
        <lg-folio-lore
          >The Ember Wolf is found in the ash fields east of Shenic. It hunts in
          pairs.</lg-folio-lore
        >
        <lg-ledger heading="Combat">
          @for (row of wolfStats; track row.label) {
            <div
              lgLedgerRow
              [label]="row.label"
              [value]="row.value"
              [sub]="row.sub"
              [description]="row.description"
              [tipMeta]="row.tipMeta"
              [muted]="!!row.muted"
            ></div>
          }
        </lg-ledger>
      </lg-folio>
    </ng-template>

    <ng-template
      scStory="Title only"
      notes="The least a Folio holds: a heading, start-aligned, with no lore or effects."
      width="22.5rem"
      height="20rem"
      flush
    >
      <lg-folio heading="Details" align="start" />
    </ng-template>
  `,
})
export class FolioShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly corner = 'assets/grimoire/corner-ornament.svg';
  protected readonly powerEffects: readonly LgFolioEffect[] = [
    { value: '+12%', text: 'damage from equipment' },
    { value: '+8', text: 'from active essences' },
  ];
  protected readonly prismEffects: readonly LgFolioEffect[] = [
    { value: '+14', text: 'Power' },
    { value: '+6%', text: 'magical damage' },
  ];
  protected readonly crownEffects: readonly LgFolioEffect[] = [
    { value: '+240', text: 'Armor Rating' },
    { value: '+4%', text: 'Crit Chance' },
  ];
  protected readonly wolfEffects: readonly (string | LgFolioEffect)[] = [
    'Your attacks burn.',
    { value: '+12%', text: 'damage from equipment' },
  ];
  protected readonly wolfStats: readonly LgLedgerRow[] = [
    { label: 'Max Health', value: '4,150' },
    { label: 'Power', value: '142' },
    { label: 'Attack Speed', value: '1.12' },
    { label: 'Armor', value: '38%' },
  ];
}

export const FOLIO_SHOWCASE: ShowcaseEntry = {
  slug: 'folio',
  name: 'Folio',
  tier: 'components',
  summary: 'The detail panel.',
  covers: [
    'LgFolioComponent',
    'LgFolioEmblemComponent',
    'LgFolioLoreComponent',
    'LgFolioActionsComponent',
    'LgFolioFooterComponent',
  ],
  readme: 'src/app/grimoire/components/folio/README.md',
  component: FolioShowcaseComponent,
};
