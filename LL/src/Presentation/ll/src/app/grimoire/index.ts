/*
 * Grimoire, the Legend's Legacy design system: its public API, imported as `@grimoire`. Its rules and token values
 * are in design-system/ (tokens.json, icons.json, docs/); each part's guidelines are the README.md beside it. Import
 * single components, or spread LG_GRIMOIRE into a standalone component's `imports` to get every component and
 * directive at once. Test harnesses are in `@grimoire/testing`.
 */

export * from './tokens/tokens';
export * from './core/grimoire-core';
export * from './core/grimoire-format';
export * from './core/grimoire-numerals';
export * from './core/grimoire-states';
export * from './core/grimoire-a11y';
export * from './core/grimoire-announcer';
export * from './core/grimoire-tip';
export * from './core/grimoire-blocked';
export * from './core/grimoire-motion';
export * from './core/grimoire-ornament';
export * from './core/grimoire-icons';
export * from './core/grimoire-styles';
export * from './components/banner/banner.component';
export * from './primitives/button/button.component';
export * from './shell/chronicle/chronicle.component';
export * from './game/constellation/constellation.component';
export * from './game/currency-pill/currency-pill.component';
export * from './components/delta/delta.component';
export * from './game/emblem/emblem.component';
export * from './components/folio/folio.component';
export * from './shell/game-shell/game-shell.component';
export * from './primitives/heading/heading.component';
export * from './primitives/icon/icon.component';
export * from './game/item-link/item-link.component';
export * from './game/item-slot/item-slot.component';
export * from './game/journey-card/journey-card.component';
export * from './primitives/key/key.component';
export * from './components/key-hints/key-hints.component';
export * from './components/ledger/ledger.component';
export * from './game/level-plate/level-plate.component';
export * from './components/list/list.component';
export * from './game/loadout-slot/loadout-slot.component';
export * from './primitives/meter/meter.component';
export * from './primitives/option/option.component';
export * from './shell/nav-rail/nav-rail.component';
export * from './components/page-header/page-header.component';
export * from './components/page/page.component';
export * from './components/panel/panel.component';
export * from './game/presence/presence.component';
export * from './game/profile-identity/profile-identity.component';
export * from './primitives/rarity/rarity.component';
export * from './shell/activity/activity.component';
export * from './shell/objective/objective.component';
export * from './components/notice/notice.component';
export * from './components/search-field/search-field.component';
export * from './primitives/section-rule/section-rule.component';
export * from './game/sigil/sigil.component';
export * from './shell/stage/stage.component';
export * from './components/stat-figure/stat-figure.component';
export * from './primitives/tabs/tabs.component';
export * from './primitives/tag/tag.component';
export * from './primitives/tooltip/tooltip.directive';
export * from './shell/top-bar/top-bar.component';
export * from './components/track/track.component';

import { LgSlotDirective } from './core/grimoire-core';
import { LgBlockedDirective } from './core/grimoire-blocked';
import { LgTooltipDirective } from './primitives/tooltip/tooltip.directive';
import { LgLiveListDirective } from './core/grimoire-motion';
import { LG_NUMERAL_PIPES } from './core/grimoire-numerals';
import { LgBannerAsideComponent, LgBannerComponent, LgBannerFooterComponent } from './components/banner/banner.component';
import { LgButtonComponent } from './primitives/button/button.component';
import { LgChronicleComponent, LgChronicleTextDirective } from './shell/chronicle/chronicle.component';
import { LgConstellationComponent } from './game/constellation/constellation.component';
import { LgCurrencyPillComponent } from './game/currency-pill/currency-pill.component';
import { LgDeltaComponent } from './components/delta/delta.component';
import { LgEmblemComponent } from './game/emblem/emblem.component';
import {
  LgFolioActionsComponent,
  LgFolioComponent,
  LgFolioEmblemComponent,
  LgFolioFooterComponent,
  LgFolioLoreComponent,
} from './components/folio/folio.component';
import { LgGameShellComponent } from './shell/game-shell/game-shell.component';
import { LgHeadingComponent } from './primitives/heading/heading.component';
import { LgIconComponent } from './primitives/icon/icon.component';
import { LgItemLinkComponent } from './game/item-link/item-link.component';
import { LgItemSlotComponent } from './game/item-slot/item-slot.component';
import { LgJourneyCardComponent } from './game/journey-card/journey-card.component';
import { LgKeyComponent } from './primitives/key/key.component';
import { LgKeyHintsComponent } from './components/key-hints/key-hints.component';
import { LgLedgerComponent, LgLedgerRowComponent } from './components/ledger/ledger.component';
import { LgLevelPlateComponent } from './game/level-plate/level-plate.component';
import {
  LgListComponent,
  LgListRowActionComponent,
  LgListRowComponent,
  LgListRowTrailingComponent,
} from './components/list/list.component';
import { LgLoadoutSlotComponent } from './game/loadout-slot/loadout-slot.component';
import { LgMeterComponent } from './primitives/meter/meter.component';
import { LgOptionComponent } from './primitives/option/option.component';
import { LgNavRailComponent } from './shell/nav-rail/nav-rail.component';
import { LgPageHeaderActionsComponent, LgPageHeaderComponent } from './components/page-header/page-header.component';
import { LgPageComponent } from './components/page/page.component';
import { LgPanelComponent, LgPanelHeaderComponent, LgPanelTitleComponent } from './components/panel/panel.component';
import { LgPresenceComponent } from './game/presence/presence.component';
import { LgProfileFactComponent, LgProfileIdentityComponent } from './game/profile-identity/profile-identity.component';
import { LgRarityComponent } from './primitives/rarity/rarity.component';
import { LgActivityComponent } from './shell/activity/activity.component';
import { LgObjectiveComponent } from './shell/objective/objective.component';
import { LgNoticeActionsComponent, LgNoticeComponent } from './components/notice/notice.component';
import { LgSearchFieldComponent } from './components/search-field/search-field.component';
import { LgSectionRuleComponent } from './primitives/section-rule/section-rule.component';
import { LgSigilComponent } from './game/sigil/sigil.component';
import { LgStageComponent } from './shell/stage/stage.component';
import { LgStatFigureComponent } from './components/stat-figure/stat-figure.component';
import {
  LgTabComponent,
  LgTabLinkComponent,
  LgTabNavComponent,
  LgTabPanelComponent,
  LgTabsComponent,
} from './primitives/tabs/tabs.component';
import { LgTagComponent } from './primitives/tag/tag.component';
import { LgTopBarComponent } from './shell/top-bar/top-bar.component';
import { LgTrackComponent } from './components/track/track.component';

/** Every Grimoire component, directive and pipe, for a standalone `imports` array. */
export const LG_GRIMOIRE = [
  LgSlotDirective,
  LgBlockedDirective,
  LgTooltipDirective,
  LgLiveListDirective,
  LgBannerComponent,
  LgBannerAsideComponent,
  LgBannerFooterComponent,
  LgButtonComponent,
  LgChronicleTextDirective,
  LgChronicleComponent,
  LgConstellationComponent,
  LgCurrencyPillComponent,
  LgDeltaComponent,
  LgEmblemComponent,
  LgFolioComponent,
  LgFolioEmblemComponent,
  LgFolioLoreComponent,
  LgFolioActionsComponent,
  LgFolioFooterComponent,
  LgGameShellComponent,
  LgHeadingComponent,
  LgIconComponent,
  LgItemLinkComponent,
  LgItemSlotComponent,
  LgJourneyCardComponent,
  LgKeyComponent,
  LgKeyHintsComponent,
  LgLedgerComponent,
  LgLedgerRowComponent,
  LgLevelPlateComponent,
  LgListComponent,
  LgListRowComponent,
  LgListRowActionComponent,
  LgListRowTrailingComponent,
  LgLoadoutSlotComponent,
  LgMeterComponent,
  LgOptionComponent,
  LgNavRailComponent,
  LgPageHeaderComponent,
  LgPageHeaderActionsComponent,
  LgPageComponent,
  LgPanelComponent,
  LgPanelHeaderComponent,
  LgPanelTitleComponent,
  LgPresenceComponent,
  LgProfileIdentityComponent,
  LgProfileFactComponent,
  LgRarityComponent,
  LgActivityComponent,
  LgObjectiveComponent,
  LgNoticeComponent,
  LgNoticeActionsComponent,
  LgSearchFieldComponent,
  LgSectionRuleComponent,
  LgSigilComponent,
  LgStageComponent,
  LgStatFigureComponent,
  LgTabsComponent,
  LgTabComponent,
  LgTabPanelComponent,
  LgTabNavComponent,
  LgTabLinkComponent,
  LgTagComponent,
  LgTopBarComponent,
  LgTrackComponent,
  ...LG_NUMERAL_PIPES,
] as const;
