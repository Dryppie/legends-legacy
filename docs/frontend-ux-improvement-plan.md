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
| Inventory and equipment | Browse items, inspect equipped gear, and scrap unwanted tempered equipment. | Scrap mode is useful but destructive; selected item consequences could be more explicit and selection controls could be tighter on mobile. | Strengthen scrap confirmation context, improve selected item summary, and clarify empty states. | Medium |
| Essence management | Browse Essences, understand abilities/progression, manage loadouts, absorb. | Powerful but dense; archive, selected Essence, progression, and loadout controls compete for attention. | Improve selected Essence hierarchy, action eligibility explanations, and loadout editing clarity. | Medium |
| Profession gathering/crafting | Choose resource nodes or crafting actions and monitor progression. | Gathering cards are listed with minimal page-level guidance; empty/loading states are not obvious from the container. | Add page-level header/help, improve grid spacing, and clarify available actions/requirements. | Low |
| Marketplace | Buy/sell resources, equipment, and Essences with Cinders. | Header is functional but plain; selected category and buy/sell flow can feel utilitarian compared with game screens. | Improve market header, category summary, empty states, and Cinders context. | Medium |
| Guild | Manage membership, buildings, vault, rankings, or find/create a guild. | Parent slice delegates well, but no-guild and in-guild paths likely need separate focused passes. | Audit subviews independently; improve no-guild onboarding and in-guild action hierarchy. | Medium |
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
