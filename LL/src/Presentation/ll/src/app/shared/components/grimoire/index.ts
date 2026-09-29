/*
 * Grimoire — Legend's Legacy design system, Angular edition.
 * Import single components, or spread LG_GRIMOIRE into a standalone
 * component's `imports` to get every component and directive at once.
 */

export * from './grimoire-core';
export * from './grimoire-icons';
export * from './banner.component';
export * from './button.component';
export * from './chronicle.component';
export * from './constellation.component';
export * from './currency-pill.component';
export * from './emblem.component';
export * from './entry-list.component';
export * from './folio.component';
export * from './game-shell.component';
export * from './heading.component';
export * from './icon.component';
export * from './item-link.component';
export * from './item-slot.component';
export * from './journey-card.component';
export * from './key-hints.component';
export * from './ledger.component';
export * from './level-plate.component';
export * from './loadout-slot.component';
export * from './meter.component';
export * from './nav-rail.component';
export * from './page-header.component';
export * from './page.component';
export * from './panel.component';
export * from './presence.component';
export * from './search-field.component';
export * from './section-rule.component';
export * from './sigil.component';
export * from './stage.component';
export * from './stat-figure.component';
export * from './stat-tile.component';
export * from './tab-strip.component';
export * from './tag.component';
export * from './top-bar.component';
export * from './track.component';

import { LgSlotDirective } from './grimoire-core';
import { LgBannerComponent } from './banner.component';
import { LgButtonComponent } from './button.component';
import { LgChronicleTextDirective, LgChronicleComponent } from './chronicle.component';
import { LgConstellationComponent } from './constellation.component';
import { LgCurrencyPillComponent } from './currency-pill.component';
import { LgEmblemComponent } from './emblem.component';
import { LgEntryListComponent } from './entry-list.component';
import { LgFolioComponent } from './folio.component';
import { LgGameShellComponent } from './game-shell.component';
import { LgHeadingComponent } from './heading.component';
import { LgIconComponent } from './icon.component';
import { LgItemLinkComponent } from './item-link.component';
import { LgItemSlotComponent } from './item-slot.component';
import { LgJourneyCardComponent } from './journey-card.component';
import { LgKeyHintsComponent } from './key-hints.component';
import { LgLedgerComponent } from './ledger.component';
import { LgLevelPlateComponent } from './level-plate.component';
import { LgLoadoutSlotComponent } from './loadout-slot.component';
import { LgMeterComponent } from './meter.component';
import { LgNavRailComponent } from './nav-rail.component';
import { LgPageHeaderComponent } from './page-header.component';
import { LgPageComponent } from './page.component';
import { LgPanelComponent } from './panel.component';
import { LgPresenceComponent } from './presence.component';
import { LgSearchFieldComponent } from './search-field.component';
import { LgSectionRuleComponent } from './section-rule.component';
import { LgSigilComponent } from './sigil.component';
import { LgStageComponent } from './stage.component';
import { LgStatFigureComponent } from './stat-figure.component';
import { LgStatTileComponent } from './stat-tile.component';
import { LgTabStripComponent } from './tab-strip.component';
import { LgTagComponent } from './tag.component';
import { LgTopBarComponent } from './top-bar.component';
import { LgTrackComponent } from './track.component';

/** Every Grimoire component and directive, for a standalone `imports` array. */
export const LG_GRIMOIRE = [
  LgSlotDirective,
  LgBannerComponent,
  LgButtonComponent,
  LgChronicleTextDirective,
  LgChronicleComponent,
  LgConstellationComponent,
  LgCurrencyPillComponent,
  LgEmblemComponent,
  LgEntryListComponent,
  LgFolioComponent,
  LgGameShellComponent,
  LgHeadingComponent,
  LgIconComponent,
  LgItemLinkComponent,
  LgItemSlotComponent,
  LgJourneyCardComponent,
  LgKeyHintsComponent,
  LgLedgerComponent,
  LgLevelPlateComponent,
  LgLoadoutSlotComponent,
  LgMeterComponent,
  LgNavRailComponent,
  LgPageHeaderComponent,
  LgPageComponent,
  LgPanelComponent,
  LgPresenceComponent,
  LgSearchFieldComponent,
  LgSectionRuleComponent,
  LgSigilComponent,
  LgStageComponent,
  LgStatFigureComponent,
  LgStatTileComponent,
  LgTabStripComponent,
  LgTagComponent,
  LgTopBarComponent,
  LgTrackComponent,
] as const;
