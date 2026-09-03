# Frontend UX Improvement Plan

## Frontend Structure Notes

- Target service: `LL/src/Presentation/ll`, the Angular game frontend.
- Framework: Angular 18 with standalone components and lazy route trees.
- App shell: authenticated game routes render inside `layout/dashboard`, with sidebar, navbar, chat, loot tracker, and a textured dark fantasy panel surface.
- Main route groups: `character`, `world`, `city`, `professions`, and `settings`.
- Styling approach: Tailwind utility classes with global theme helpers such as `bg-texture`, `border-light_gray`, `border-gradient`, `primary-gradient`, `text-primary`, and compact dark panels.
- State approach: Angular services with signals/computed values in feature state services, plus RxJS where older app shell state still uses observables.
- Shared UI patterns: `app-default-header`, `app-regular-button`, `app-tabs`, `app-filter-tabs`, dungeon/combat/item/equipment/essence components, modal/popover/help components.

## Content Density Principles

- Do not make pages verbose simply to feel more helpful or polished.
- Put each fact in the one place where the player most needs it; avoid repeating the same status, progress, reward, requirement, or explanation in multiple panels.
- If a piece of information appears twice, remove the less actionable copy instead of restyling both.
- Prefer concise labels and scannable values over paragraphs when the player is making a repeatable game decision.
- Use helper text only when it explains a consequence, lock, empty state, or non-obvious action.

## Slice Audit

| Slice | Player Goal | Current UX Friction | Frontend-Only Improvements | Risk |
| --- | --- | --- | --- | --- |
| Active dungeon run page | Understand the current room, choose the next action, track rewards, and claim or leave safely. | Center action, pending rewards, and upcoming rooms have similar weight; loading/error/success feedback is not prominent; disabled actions do not always explain why; mobile layout can feel like three separate panels without a clear priority. | Add a clearer run header, stronger current-room action panel, non-redundant reward summaries, visible feedback states, concise helper copy for checkpoint/event/final states, and more obvious primary/secondary actions. | Low |
| Dungeon list and records | Pick a dungeon difficulty, understand entry requirements, preview rewards, and inspect records. | Visual presentation is strong, but requirements and disabled entry reasons can be easy to miss in the large preview. | Improve requirement grouping, disabled entry explanation, and selected difficulty state. | Low |
| Combat view | Follow battle state, focused units, outcome, and battle log. | Team thumbnails use placeholder-looking background values; combat summary/log area is commented out for current view; battle state hierarchy is split across large avatar and stat sections. | Improve team focus affordances, add clearer loading/outcome states, and restore or refine log visibility if data is available. | Medium |
| Character overview | Check combat rating, stats, and Essence loadout. | Good structure already, but search and self-profile context compete; attributes are dense and require scanning. | Add clearer self-vs-search context, stronger primary stat grouping, and empty/loading states. | Low |
| Inventory and equipment | Browse items, inspect gear, and equip it. | Dense equipment detail and selection controls can be tightened on mobile. | Improve selected item summary and empty states. | Medium |
| Essence management | Browse Essences, understand abilities/progression, manage loadouts, absorb. | Powerful but dense; archive, selected Essence, progression, and loadout controls compete for attention. | Improve selected Essence hierarchy, action eligibility explanations, and loadout editing clarity. | Medium |
| Profession gathering/crafting | Choose resource nodes or crafting actions and monitor progression. | Gathering cards are listed with minimal page-level guidance; empty/loading states are not obvious from the container. | Add page-level header/help, improve grid spacing, and clarify available actions/requirements. | Low |
| Marketplace | Buy/sell resources, equipment, and Essences with Cinders. | Header is functional but plain; selected category and buy/sell flow can feel utilitarian compared with game screens. | Improve market header, category summary, empty states, and Cinders context. | Medium |
| Guild | Manage membership, buildings, missions, shop, rankings, or find/create a guild. | Parent slice delegates well, but no-guild and in-guild paths likely need separate focused passes. | Audit subviews independently; improve no-guild onboarding and in-guild action hierarchy. | Medium |
| Navigation/sidebar | Move between core game loops and see current activity. | Strong game shell, but current dungeon/action cards are compact and may not clearly advertise required next action. | Improve active activity cards and notification clarity without changing route structure. | Low |

## First Slice: Active Dungeon Run Page

### Why This Slice First

The active dungeon run page is part of the core combat/reward loop and players may return to it repeatedly during a session. It is also relatively low risk because the necessary data and actions already exist in `DungeonPageComponent`; the first pass can improve hierarchy, feedback, and explanations without changing backend APIs, dungeon rules, rewards, or combat behavior.

### UX Problems To Address

- Make the screen answer "Where am I?" with a clear run header and progress summary.
- Make the current room and next action the strongest visual priority.
- Surface loading, error, and success messages near the action area.
- Make pending rewards easier to scan without overpowering the current-room decision.
- Clarify checkpoint/event decisions so players understand what "Continue", "Withdraw", "Accept", and "Ignore" mean.
- Improve mobile stacking by placing the high-priority action panel before secondary detail.
- Keep the page concise: do not repeat progress, status, room descriptions, or event explanations in multiple panels.

### Out Of Scope For This Pass

- No backend API changes.
- No changes to dungeon generation, combat, reward amounts, entry costs, or balance.
- No redesign of the dungeon list, records view, combat component, sidebar, or shared button system.
- No new UI framework or broad theme refactor.

## Second Slice: Dungeon List And Entry Preview

### Why This Slice Next

The dungeon list is the doorway into the active dungeon loop. Players need to quickly choose a difficulty, understand whether they can enter, and see the meaningful reward promise before starting a run. This is low risk because the data already exists in the dungeon preview DTO and can be clarified in the expanded card without changing backend behavior.

### UX Problems To Address

- Make selected difficulty and locked difficulties easier to distinguish.
- Make the primary action state obvious: ready to enter, missing entry items, or under the recommended combat rating.
- Keep entry requirements and combat rating close to the Enter action.
- Keep reward preview scannable without over-explaining every item.

### Out Of Scope For This Pass

- No changes to dungeon records.
- No changes to available dungeon data, requirements, rewards, or difficulty unlock rules.
- No redesign of the active run page.

## Third Slice: Combat View

### Why This Slice Next

Combat is the moment players watch the outcome of dungeon, idle, and arena decisions. The component already had the core data, avatars, log, and stats, so the safest improvement was to clarify the view hierarchy and targeting affordances without changing combat flow.

### UX Problems Addressed

- Added one consistent battle header with battle context and current status.
- Moved the idle combat wins/losses/XP summary into the header so it reads as session context.
- Replaced tiny unlabeled selector squares with readable unit focus buttons.
- Grouped player and enemy teams into clear side panels.
- Removed the central V/S lane from active combat so the player and enemy sides remain the focus.
- Improved the queued idle-combat state so the countdown, flavor line, and stop action read as one focused panel.

### Out Of Scope For This Pass

- No changes to combat rules, result processing, event handling, or battle timing.
- No changes to avatar rendering, entity stat calculations, or combat log internals.
- No new combat actions or backend API changes.

## Fourth Slice: Character Overview

### Why This Slice Next

The character overview was already in a good place, so this pass stayed restrained. The main opportunity was not to redesign the stat layout, but to make profile context and request feedback clearer when the player searches for another character.

### UX Problems Addressed

- Added visible loading and error states for character overview requests.
- Made the page show whether it is displaying the current character or a searched profile.
- Routed character searches through the URL query string so browser Back returns to the previous profile.
- Labeled empty Essence slots as open so the loadout rail reads consistently.

### Out Of Scope For This Pass

- No changes to character stats, combat rating, Essence loadout data, or backend APIs.
- No broad redesign of the overview cards or attribute sections.
- No changes to the dedicated Essence management page.

## Fifth Slice: Equipment List

### Why This Slice Next

The inventory page already works well, so this pass focused only on the equipment list inside it. The goal was to make carried gear easier to scan without changing the broader inventory browsing flow.

### UX Problems Addressed

- Made the Equipment tab title and helper text specific to gear instead of generic inventory.
- Added compact equipment row metadata for slot type, rarity, and potential.
- Kept the metadata scoped to the Equipment tab so the general inventory list stays familiar.

### Out Of Scope For This Pass

- No changes to inventory categories, item data, equipment stats, scrap rules, or backend APIs.
- No redesign of the equipped gear panel or equipment modals.
- No changes to resources or Essence inventory rows.

## Sixth Slice: Essence Management

### Why This Slice Next

Essence management is powerful but dense, and the existing page already had strong archive, filter, progression, and loadout foundations. This pass focused on small state cues that help the player understand the selected Essence and draft loadout without changing the page structure.

### UX Problems Addressed

- Added visible loading and error feedback for Soul Archive operations.
- Added compact selected-Essence badges for attunement, Ascend readiness, and Evolve readiness.
- Added loadout editor context for whether the player is editing a new, active, or saved loadout.
- Added a single draft-slot summary and save hint for loadout editing.

### Out Of Scope For This Pass

- No changes to Essence progression rules, loadout rules, archive data, or backend APIs.
- No redesign of the archive list, Absorb view, or Essence details layout.
- No changes to inventory Essence rows.

## Seventh Slice: Soulstones

### Why This Slice Next

The Soulstones page is a permanent progression surface, but it was still using a very sparse layout. This pass focused on making the upgrade economy and per-upgrade state easier to scan without changing upgrade costs, effects, reset behavior, or backend APIs.

### UX Problems Addressed

- Added a top summary for available Soulstones, total upgrade levels, affordable upgrades, and maxed upgrades.
- Added visible reset feedback and error feedback.
- Reworked upgrade cards to show description, level progress, current effect, per-level effect, next cost, and affordability state.
- Made maxed and unaffordable upgrades clearer before the player clicks.

### Out Of Scope For This Pass

- No changes to Soulstone upgrade math, costs, effects, reset rules, or backend APIs.
- No new filters, sorting, or progression categories.
- No changes to how Soulstones are earned.

## Eighth Slice: Guild

### Why This Slice Next

The Guild area spans several related workflows: guild overview, buildings, missions, shop, and rankings. This pass focused on shared context and scanability across those views without changing membership, building, mission, shop, or ranking rules.

### UX Problems Addressed

- Added an in-guild summary header with guild name, tag, member count, applications, and key resources.
- Tightened the Guild tab into action context plus a cleaner member roster.
- Added building readiness status and cost requirement coloring.
- Added clearer guild-owned resource context through the activity-driven guild systems.
- Added leaderboard summary cards and rank numbers to Rankings.

### Out Of Scope For This Pass

- No changes to guild permissions, applications, invites, building upgrade rules, rankings sort rules, or backend APIs.
- No redesign of the no-guild onboarding flow.
- No changes to guild realtime behavior or chat.

## Ninth Slice: Colosseum

### Why This Slice Next

The Colosseum covers three related competitive views: Arena opponent selection, arena rankings, and battle history. This pass focused on making those views feel like one competitive circuit while keeping the existing challenge, ranking, and match-result flows intact.

### UX Problems Addressed

- Added a shared Colosseum status strip for tickets, current rank, rating, and recent record.
- Reworked Arena opponents from table rows into compact challenge cards with rating, level, and outcome deltas.
- Added Rankings context for the player's standing, current champion, and tracked entries while preserving the shared leaderboard component.
- Fixed Rankings current-player highlighting so it updates when async ranking data arrives.
- Reworked Record of Battles into readable match cards with result state, played date, participants, and rating swings.

### Out Of Scope For This Pass

- No changes to arena ticket rules, opponent selection, rating math, battle simulation, or backend APIs.
- No new ranking filters, seasons, rewards, or tournament features.
- No changes to the Colosseum combat playback screen or result modal.
