import { ShowcaseEntry } from './showcase.types';
import { BUTTON_SHOWCASE } from './entries/button.showcase';
import { ICON_SHOWCASE } from './entries/icon.showcase';
import { HEADING_SHOWCASE } from './entries/heading.showcase';
import { KEY_SHOWCASE } from './entries/key.showcase';
import { NUMERALS_SHOWCASE } from './entries/numerals.showcase';
import { TAG_SHOWCASE } from './entries/tag.showcase';
import { TOOLTIP_SHOWCASE } from './entries/tooltip.showcase';
import { TABS_SHOWCASE } from './entries/tabs.showcase';
import { METER_SHOWCASE } from './entries/meter.showcase';
import { SECTION_RULE_SHOWCASE } from './entries/section-rule.showcase';
import { PANEL_SHOWCASE } from './entries/panel.showcase';
import { PAGE_SHOWCASE } from './entries/page.showcase';
import { PAGE_HEADER_SHOWCASE } from './entries/page-header.showcase';
import { BANNER_SHOWCASE } from './entries/banner.showcase';
import { FOLIO_SHOWCASE } from './entries/folio.showcase';
import { NOTICE_SHOWCASE } from './entries/notice.showcase';
import { LEDGER_SHOWCASE } from './entries/ledger.showcase';
import { STAT_FIGURE_SHOWCASE } from './entries/stat-figure.showcase';
import { DELTA_SHOWCASE } from './entries/delta.showcase';
import { TRACK_SHOWCASE } from './entries/track.showcase';
import { LIST_SHOWCASE } from './entries/list.showcase';
import { SEARCH_FIELD_SHOWCASE } from './entries/search-field.showcase';
import { KEY_HINTS_SHOWCASE } from './entries/key-hints.showcase';
import { ITEM_SLOT_SHOWCASE } from './entries/item-slot.showcase';
import { ITEM_LINK_SHOWCASE } from './entries/item-link.showcase';
import { RARITY_SHOWCASE } from './entries/rarity.showcase';
import { LOADOUT_SLOT_SHOWCASE } from './entries/loadout-slot.showcase';
import { SIGIL_SHOWCASE } from './entries/sigil.showcase';
import { CONSTELLATION_SHOWCASE } from './entries/constellation.showcase';
import { EMBLEM_SHOWCASE } from './entries/emblem.showcase';
import { LEVEL_PLATE_SHOWCASE } from './entries/level-plate.showcase';
import { CURRENCY_PILL_SHOWCASE } from './entries/currency-pill.showcase';
import { PROFILE_IDENTITY_SHOWCASE } from './entries/profile-identity.showcase';
import { PRESENCE_SHOWCASE } from './entries/presence.showcase';
import { JOURNEY_CARD_SHOWCASE } from './entries/journey-card.showcase';
import { GAME_SHELL_SHOWCASE } from './entries/game-shell.showcase';
import { NAV_RAIL_SHOWCASE } from './entries/nav-rail.showcase';
import { TOP_BAR_SHOWCASE } from './entries/top-bar.showcase';
import { STAGE_SHOWCASE } from './entries/stage.showcase';
import { ACTIVITY_SHOWCASE } from './entries/activity.showcase';
import { OBJECTIVE_SHOWCASE } from './entries/objective.showcase';
import { CHRONICLE_SHOWCASE } from './entries/chronicle.showcase';

/**
 * Every showcase entry, by tier, in the order the navigation lists them. A new Grimoire component adds its entry here.
 */
export const SHOWCASE_ENTRIES: readonly ShowcaseEntry[] = [
  // Primitives
  BUTTON_SHOWCASE,
  ICON_SHOWCASE,
  HEADING_SHOWCASE,
  KEY_SHOWCASE,
  NUMERALS_SHOWCASE,
  TAG_SHOWCASE,
  RARITY_SHOWCASE,
  TOOLTIP_SHOWCASE,
  TABS_SHOWCASE,
  METER_SHOWCASE,
  SECTION_RULE_SHOWCASE,
  // Components
  PANEL_SHOWCASE,
  PAGE_SHOWCASE,
  PAGE_HEADER_SHOWCASE,
  BANNER_SHOWCASE,
  FOLIO_SHOWCASE,
  NOTICE_SHOWCASE,
  LEDGER_SHOWCASE,
  STAT_FIGURE_SHOWCASE,
  DELTA_SHOWCASE,
  TRACK_SHOWCASE,
  LIST_SHOWCASE,
  SEARCH_FIELD_SHOWCASE,
  KEY_HINTS_SHOWCASE,
  // Game
  ITEM_SLOT_SHOWCASE,
  ITEM_LINK_SHOWCASE,
  LOADOUT_SLOT_SHOWCASE,
  SIGIL_SHOWCASE,
  CONSTELLATION_SHOWCASE,
  EMBLEM_SHOWCASE,
  LEVEL_PLATE_SHOWCASE,
  CURRENCY_PILL_SHOWCASE,
  PROFILE_IDENTITY_SHOWCASE,
  PRESENCE_SHOWCASE,
  JOURNEY_CARD_SHOWCASE,
  // Shell
  GAME_SHELL_SHOWCASE,
  NAV_RAIL_SHOWCASE,
  TOP_BAR_SHOWCASE,
  STAGE_SHOWCASE,
  ACTIVITY_SHOWCASE,
  OBJECTIVE_SHOWCASE,
  CHRONICLE_SHOWCASE,
];
