# Frontend UI Improvement Backlog

This notes what is still missing after the first shared-token and shared-class pass across Overview, Inventory, Essences, Soulstones, Guild, Colosseum, Marketplace, Leaderboard, Settings, and the active Dungeon run page.

## Priority 1: Shared Surfaces

These should come next because they affect many pages at once.

1. **Dashboard shell and navigation**

   - Status: first pass completed. Dashboard frame, sidebar shell, sidebar nav items, mobile overlays, top/mobile navigation, currency pills, and floating chat trigger now use shared shell primitives.
   - Remaining files: `src/app/layout/dashboard/dashboard.component.html`, `src/app/layout/dashboard/sidebar/sidebar.component.html`, `src/app/layout/dashboard/sidebar/sidebar-item/sidebar-item.component.html`, `src/app/layout/dashboard/navbar/navbar.component.html`, `src/app/layout/dashboard/navbar/navbutton/navbutton.component.html`.
   - Remaining work: visual QA the shell at mobile/tablet/desktop widths, then continue into the chat and loot tracker surfaces if we want the full dashboard chrome aligned.
   - Why it matters: these are visible on almost every game screen, so inconsistencies here make the whole app feel less unified.

2. **Item, equipment, and market listing components**

   - Status: first pass completed. Shared item row, icon, chip, tooltip/detail, section, and stat-row classes now cover inventory rows, marketplace listing rows, marketplace inventory rows, base item details, item details, equipment details, and equipment overview rows.
   - Remaining files: `src/app/shared/components/item/item.component.html`, `src/app/shared/components/base-item/base-item.component.html`, `src/app/shared/components/equipment/equipment-display/equipment-display.component.html`, `src/app/shared/components/market-place/market-place-inventory-item/market-place-inventory-item.component.html`, `src/app/shared/components/market-place/market-place-listing-item/market-place-listing-item.component.html`.
   - Remaining work: visual QA tooltip density and rarity contrast, then continue into market filters/pages and the equipment modal shells.
   - Why it matters: items appear across inventory, marketplace, crafting, equipment, rewards, and tooltips.

3. **Tabs, filters, and segmented controls**

   - Status: first pass completed. Shared tabs, filter tabs, list filters, selectable list filters, and the Marketplace filter wrapper now use shared segmented, toolbar, button, card, and empty-state primitives.
   - Remaining files: `src/app/shared/components/custom-components/tabs/tabs.component.html`, `src/app/shared/components/custom-components/tabs/filter-tabs/filter-tabs.component.html`, `src/app/shared/components/list-filters/list-filter/list-filter.component.html`, `src/app/shared/components/list-filters/selectable-list-filter/selectable-list-filter.component.html`, `src/app/shared/components/market-place/market-place-filter/market-place-filter.component.html`.
   - Remaining work: visual QA pages that consume these shared controls, especially dense mobile tab sets and projected selectable list rows.
   - Why it matters: many migrated pages still inherit older visual patterns through these shared components.

4. **Modal, popup, and toast system**

   - Status: first pass completed. Shared modal container, modal child panels, app update popup, session summary popups, colosseum result, and toast now use shared modal/panel/button/state primitives.
   - Remaining files: `src/app/shared/components/modal-container/modal-container.component.html`, `src/app/shared/components/modal-container/*`, `src/app/shared/components/app-update-popup/app-update-popup.component.html`, `src/app/shared/components/session-summary-popup/session-summary-popup.component.html`, `src/app/shared/components/colosseum/colosseum-result/colosseum-result.component.html`, `src/app/shared/components/toast/toast.component.html`.
   - Remaining work: visual QA overlay stacking, modal max-height behavior, and focus handling/trapping for keyboard users.
   - Why it matters: overlays are currently one of the fastest ways to see old and new UI styles side by side.

## Priority 2: Feature Pages

These are the highest-value remaining page-level migrations.

1. **Professions, Crafting, and Tempering**

   - Status: first pass completed. Crafting and Tempering item detail panels, recipe filters, level inputs, recipe cards, tempering queue cards, progress bars, action buttons, and empty states now use shared UI primitives.
   - Remaining files: `src/app/features/game/professions/crafting/regular-crafting/regular-crafting.component.html`, `src/app/features/game/professions/crafting/tempering/tempering.component.html`, plus shared profession components.
   - Remaining work: visual QA the dense mobile layouts and continue into any shared profession header/queue components if they receive more screens.
   - Why it matters: these screens have a lot of dense operational UI and still carry many hardcoded borders, backgrounds, and text colors.

2. **Dungeon list, dungeon records, and dungeon cards**

   - Status: first pass completed. Dungeon records, leaderboard tabs, tier cards, loading/error states, reward cards, gathering cards, difficulty setup, power checks, entry requirements, and empty states now use shared primitives.
   - Remaining files: `src/app/features/game/world/region/dungeons/dungeons.component.html`, `src/app/shared/components/dungeons/dungeon-card/dungeon-card.component.html`.
   - Remaining work: visual QA the image-heavy dungeon card at mobile/tablet/desktop sizes and decide whether the hero-specific overlay/shadow treatment should become its own reusable media-card primitive.
   - Why it matters: the active Dungeon run page now uses the shared language, but the list/entry page still does not.

3. **Combat views and combat area cards**

   - Status: first pass completed. Combat queue, combat header, team panels, unit selectors, status badges, current combat summary, entity stats, combat overview rows, health bar shell, and combat area card metadata now use shared primitives.
   - Remaining files: `src/app/shared/components/combat/combat.component.html`, `src/app/shared/components/combat/combat-log/combat-log.component.html`, `src/app/shared/components/combat/combat-log/combat-stats-card/combat-stats-card.component.html`, `src/app/shared/components/combat/combat-entity-stats/combat-entity-stats.component.html`, `src/app/shared/components/combat/combat-overview/*`, `src/app/shared/components/combat/health-bar/health-bar.component.html`, `src/app/shared/components/combat/combat-area-card/combat-area-card.component.html`.
   - Remaining work: visual QA the image-heavy avatar/area cards, then decide whether their gradient image frames should become a reusable media-card primitive alongside dungeon cards.
   - Why it matters: combat is a core repeated experience and has many bespoke visual patterns.

4. **World and region shell**

   - Status: first pass completed. Region header, tab content shell, empty area state, and empty raid state now use shared primitives.
   - Remaining files: `src/app/features/game/world/region/region.component.html`, `src/app/features/game/world/region/raids/raids.component.html`.
   - Remaining work: visual QA area-card wrapping and tab height behavior across mobile/tablet/desktop widths.
   - Why it matters: this screen hosts areas, dungeons, and raids, so it should anchor those child views consistently.

5. **Guild rankings**

   - Status: first pass completed. Guild leaderboard header, summary stats, table header, rows, horizontal overflow behavior, and empty state now use shared primitives.
   - Remaining file: `src/app/features/game/city/guild/in-a-guild/guild-rankings/guild-rankings.component.html`.
   - Remaining work: visual QA compact/mobile widths and confirm the ranking sort metric is still the intended design signal.
   - Why it matters: the generic leaderboard was migrated, but this guild-specific table still uses older styling.

6. **Placeholder pages**
   - Status: first pass completed. Placeholder routes now render route-safe shared panels and empty states instead of default Angular placeholder text.
   - Remaining files: `src/app/features/game/character/equipment-view/equipment-view.component.html`, `src/app/features/game/city/colosseum/champions-market/champions-market.component.html`, `src/app/features/game/city/colosseum/tournament-grounds/tournament-grounds.component.html`.
   - Remaining work: replace these route-safe states with real feature surfaces when the corresponding gameplay systems are designed.
   - Why it matters: placeholder text breaks the polished app feel immediately if a user reaches those routes.

## Priority 3: Design Foundation Gaps

These are not tied to one page, but they will reduce repetition and make future screens easier to build.

1. **Angular wrapper components for the most repeated primitives**

   - Candidates: page shell, page header, panel header, empty state, state message, modal shell, table/list shell, icon button, and section toolbar.
   - Current state: shared CSS classes exist, but templates still repeat a lot of class combinations.

2. **Form field system**

   - Missing classes or components: label, helper text, validation text, input group, checkbox/toggle styling, number input, and compact search fields.
   - Current state: `ll-input` and `ll-select` exist, but field composition is still page-specific.

3. **Rarity, quality, and status tokens**

   - Missing work: define consistent tokens/classes for item rarity, resource state, success, warning, danger, info, muted, and disabled.
   - Current state: several components still use local red/green/zinc/primary classes.

4. **Responsive shell rules**

   - Missing work: standard page gutters, max widths, scroll containers, sticky toolbar rules, and mobile-safe table/list behavior.
   - Current state: pages mix `pr-4`, `p-2`, `overflow-y-auto`, and fixed grid columns directly.

5. **Accessibility pass**

   - Missing work: focus states, modal roles/focus handling, `aria-selected` for tabs/segments, `aria-expanded` for collapsible navigation, button semantics for clickable divs, and keyboard behavior for overlays.
   - Current state: visual consistency improved first; semantics still need a deliberate pass.

6. **Visual QA workflow**

   - Missing work: run the app, capture desktop and mobile screenshots for the migrated pages, and compare layout density, overflow, and text wrapping.
   - Current state: build passes, but browser screenshot review has not been completed for this UI pass.

7. **Public/auth theme decision**
   - Remaining files: `src/app/features/public/landing/*`, `src/app/features/public/login/login.component.html`, `src/app/features/public/signup/signup.component.html`.
   - Missing work: decide whether public/auth screens should share the game UI primitives or have a separate, documented public theme.
   - Current state: these screens still use direct colors and local composition.

## Suggested Next Order

1. Dashboard shell and navigation.
2. Item, equipment, and marketplace listing components.
3. Tabs, filters, forms, modals, and toast primitives.
4. Crafting and Tempering pages.
5. Dungeon list, dungeon records, and dungeon cards.
6. Combat views and combat area cards.
7. Guild rankings, placeholder pages, and smaller remaining feature shells.
8. Accessibility and visual QA pass.

## Not Included

- No backend, API, persistence, or game-state work is implied by this backlog.
- No route changes are needed for the listed cleanup.
- No EF migrations, configuration changes, deployment changes, or external UI libraries are expected.
