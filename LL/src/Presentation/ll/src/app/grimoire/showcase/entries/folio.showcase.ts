import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LgButtonComponent,
  LgEmblemComponent,
  LgFolioComponent,
  LgFolioEffect,
  LgItemSlotComponent,
  LgLedgerComponent,
  LgLedgerRow,
  LgSlotDirective,
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
    LgFolioComponent,
    LgSlotDirective,
    LgButtonComponent,
    LgEmblemComponent,
    LgItemSlotComponent,
    LgLedgerComponent,
    LgTrackComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Attribute"
      notes="Emblem, eyebrow, title, lore with its key nouns in bold, the ornament rule, effects, one action and a Track in the footer."
      width="22.5rem"
      height="44rem"
      flush
    >
      <lg-folio
        [cornerSrc]="corner"
        eyebrow="Attribute"
        title="Power"
        [effects]="powerEffects"
      >
        <lg-emblem lgSlot="emblem" [points]="6" [size]="148" />
        <span lgSlot="lore"
          >Raw force behind every blow and spell. With <b>Power</b> as your
          weapon, you break what others only bend.</span
        >
        <button lgButton lgSlot="actions" hotkey="E">View breakdown</button>
        <lg-track
          lgSlot="footer"
          [steps]="5"
          [current]="1"
          startLabel="Ascension"
          endLabel="Tier V"
          label="Legacy Ascension"
        />
      </lg-folio>
    </ng-template>

    <ng-template
      scStory="Item"
      notes="With a rarity it is an item context: the title takes the rarity colour, a Tag names it, and magnitudes turn ink."
      width="22.5rem"
      height="44rem"
      flush
    >
      <lg-folio
        [cornerSrc]="corner"
        rarity="Epic"
        eyebrow="Essence"
        title="Soul Prism"
        [effects]="prismEffects"
      >
        <lg-item-slot
          lgSlot="emblem"
          rarity="Epic"
          icon="essences"
          [caption]="false"
        />
        <span lgSlot="lore"
          >A shard of a warden’s heart, still humming with the <b>Prism</b> it
          once guarded.</span
        >
        <button lgButton="solid" lgSlot="actions" hotkey="A">Attune</button>
      </lg-folio>
    </ng-template>

    <ng-template scStory="Legendary item" width="22.5rem" height="44rem" flush>
      <lg-folio
        [cornerSrc]="corner"
        rarity="Legendary"
        eyebrow="Helm · Lv 20"
        title="Crown of Cinders"
        [effects]="crownEffects"
      >
        <lg-item-slot
          lgSlot="emblem"
          rarity="Legendary"
          icon="overview"
          [caption]="false"
        />
        <button lgButton="solid" lgSlot="actions" hotkey="E">Equip</button>
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
        title="Wolf"
        titleSub="Ember"
        lore="The Ember Wolf is found in the ash fields east of Shenic. It hunts in pairs."
        [effects]="wolfEffects"
      >
        <lg-ledger title="Combat" [rows]="wolfStats" />
      </lg-folio>
    </ng-template>

    <ng-template
      scStory="Title only"
      notes="The least a Folio holds: a title, start-aligned, with no lore or effects."
      width="22.5rem"
      height="20rem"
      flush
    >
      <lg-folio title="Details" align="start" />
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
  covers: ['LgFolioComponent'],
  readme: 'src/app/grimoire/components/folio/README.md',
  component: FolioShowcaseComponent,
};
