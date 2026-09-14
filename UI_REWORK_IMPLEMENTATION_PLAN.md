# LegendsLegacy player UI implementation plan

Status: proposed implementation sequence; no application implementation performed.
Prepared: 14 September 2026.
Repository baseline: d3d2d569d plus the working tree inspected for this plan.
Target application: LL/src/Presentation/ll.

Implement the [accepted right-chat character-sheet concept][accepted] throughout the player application, using the existing colors, fonts, texture, navigation, and gameplay behavior. Replace the concept's remaining equipment pictures with named equipment-slot rows. Typography, aligned information, and layouts suited to each task should carry the design.

This is a frontend presentation program, not a game-system rewrite. The [45-part UI analysis][analysis] supplies the coverage inventory; current source takes precedence where it has changed since that analysis.

## 1. Agreed direction and scope

| Decision | Implementation contract |
| --- | --- |
| Colors | Preserve current color-token values, rarity and damage-type colors, and success/danger/warning meanings. No palette redesign. |
| Shell | Grouped left sidebar, shallow player/resource header over the game area, persistent collapsible right chat on sufficiently wide screens. |
| Presentation | Existing dark texture, restrained Marcellus headings, Poppins body text, thin rules, readable values, compact controls, modest corners. Preserve reading/font preferences. |
| Character | Compact identity and progression; all 19 attributes; adjacent build information. No portrait or character artwork. |
| Essences | Text-only names, actual slots, levels when available, active/passive ability names, progression and assignment states. No creature portraits or image placeholders. |
| Equipment | Text-only slot/item identity, rarity and relevant properties. Remove equipment thumbnails and slot-picture tiles, while retaining the equipment system and all eight actual slots. |
| Other icons | Keep existing navigation, currency, action, notification, status and meaningful room icons. No new icon pack. |
| Composition | Shared shell and visual rules, with lists, comparisons, route graphs, brackets, forms and logs suited to the activity. |
| Art | No new artwork dependency. Authenticated work areas use subdued existing texture. Remove illustration-heavy page framing as those pages migrate. Existing public branding can remain subordinate to forms. |
| Behavior | Preserve routes, guards, data contracts, ownership, progression, costs, eligibility, rewards, automatic combat, snapshots and recovery. |
| Scope | All reachable player routes, tabs/subviews, dialogs, inspectors, popovers, help, onboarding, feedback, and feature-gated player screens; also login, signup, maintenance and not-found. |
| Boundaries | AdminDashboard, the separate Angular admin dashboard, LL-Chat server internals, game balance, database schema and infrastructure are outside this work. |

The image is a layout reference, not a source of real values, API fields, production branding assets or mechanics. Never hardcode its sample player, messages, counts, wolves, preset name or numbers.

Equipment artwork removal applies consistently to the overview, inventory, inspectors, Bazaar, guild Vault/shop, rewards, loot history and linked-item previews. Replace lost identification with visible text. Do not hide every image through global CSS or delete shared assets.

“All pages” does not activate dormant features. Public Hero/World/FAQ routes are commented out. Some development controls and shop placeholders are not available player workflows. Cover existing conditional player screens while preserving their availability.

## 2. Existing foundation and actual work

| Current evidence | Implementation consequence |
| --- | --- |
| [Dashboard][screen-shell] already owns the sidebar, header, right dock, floating chat and mobile chat. | Resize/restructure the current shell, without adding a second shell or chat transport. |
| Right chat currently docks at viewport width 1280 and uses 24rem, rising to 430px. The detailed sidebar is 20rem. | Reclaim width, remove redundant margins, and make feature layouts respond to remaining space. |
| [Chat preferences][chat-pref] store docked/floating and docked-open; [sidebar preferences][sidebar-pref] store detailed/compact. | Preserve saved preferences. New/default desktop sessions use right dock; do not overwrite explicit floating/compact choices. |
| [Theme tokens][style-tokens], global classes and existing reading preferences supply the chosen colors/fonts. | Change composition, not color-token values or font preferences. |
| [Overview][overview-ts] already groups 19 attributes and estimates Essence threat. | Reuse definitions, formatters and calculations. Most work is layout and truthful data composition. |
| [Equipment presets][equipment-loadouts], [Essence presets][model-essence] and Combat Style have separate state. | Label them separately. A single preset dropdown must not imply one atomic saved build spanning all three. |
| [CharacterOverviewDto][model-character] has attributes/XP but no live current-health field. | Show Max Health on the ordinary profile; current/max health only in a context that actually supplies current health. |
| Overview Essence slots contain ability definitions but not owned-Essence levels; PlayerEssenceDto contains levels. | Join the owner's available archive by playerEssenceId. Never substitute the owner's levels into another player's profile. |
| Equipment display already supports comparison, ownership and rating metadata, using current Gear Value terminology. | Reuse [mapping/comparison][equipment-map] and [display][equipment-display], rather than mockup arithmetic or older “Gear Power” labels. |
| Routes, guards, realtime synchronization, virtualization and accessible interactions already exist. | Preserve those boundaries and verify them while changing presentation. |

The 45 audit entries are screen families and lifecycle states, not 45 independent URLs. Several significant views live within one template. Coverage must include tabs and shared consumers.

## 3. Shared layout contract

### 3.1 Desktop shell

Implement once in [dashboard markup][screen-shell], [behavior][dashboard-ts], [global styles][style-global], [sidebar][screen-sidebar] and [header][screen-header].

- Keep one full-viewport frame with a predictable routed-content scroller.
- Keep sidebar and docked chat outside routed content. Navigation must not recreate connections or duplicate chat instances.
- Reduce detailed sidebar toward 14–16rem and open chat toward 22–24rem, using layout variables. These are initial measurements to verify, not new color tokens.
- Make the center elastic with min-width: 0. Remove unnecessary absolute surface wrappers and repeated margins/padding that reduce usable width.
- Preserve the collapsed chat rail, unread/mention access and saved open state.
- Keep header compact: identity/level, Cinders, Soulstones and existing useful controls. Do not build a notification center merely because a decorative bell appears in the image.
- Keep the current action near the sidebar top with progress, resume and Stop semantics. Preserve active dungeon/raid shortcuts.
- Keep the pinned objective and guide entry available. Allow an intentional secondary row rather than squeezing long quest text between currencies and controls.
- Preserve actual Character / World / City / System destinations, guard/unlock filtering, selected state, quest attention and notifications.
- Reuse actual brand and navigation assets. Generated artwork is not the implementation's asset source.

At 1600×900 or wider and default reading size, target a labeled sidebar, open right chat and complete character-sheet attribute groups without clipping. At smaller sizes, every value remains available through reflow or scrolling; do not force all content into one screen.

### 3.2 Responsive layout and scrolling

Start from the current 640px and 1280px shell thresholds; verify against actual available content width.

| Space | Behavior |
| --- | --- |
| Wide desktop | Sidebar + center + right chat. Attributes/build sit adjacent when they fit; dense pages use appropriate split views. |
| Center narrows with dock still open | Container queries move the build/inspector below or use explicit list/detail navigation. Never shrink body text to preserve a screenshot. |
| Medium viewport below dock threshold | Preserve existing floating/collapsible chat fallback and sidebar preference. Docked preference resumes when space returns. |
| Phone below 640px | Existing navigation drawer/swipes; compact header; bottom chat summary/expansion; stacked page content or deliberate list/detail navigation. |
| Larger text / 200% zoom | Reflow labels, controls and columns. Confine horizontal scrolling to genuine tables/graphs, not the entire application. |

Right chat is the chosen desktop direction. A new desktop bottom-chat mode is not required. Mobile bottom chat and the existing floating preference remain compatibility behavior.

Declare scroll ownership per page: one routed-content scroller by default; separate list/inspector scrollers only where useful. Preserve selection and return context. Sticky actions must respect the mobile keyboard/safe areas and never cover content.

### 3.3 Chat and loot

Update existing [chat][screen-chat] and [styles][style-chat], preserving tested messaging behavior.

- Use compact chronological text, restrained sender emphasis, timestamps and bottom-anchored composer. No portrait tiles or speech bubbles.
- Derive rooms from actual state. Preserve All/Global semantics, guild/raid access, whispers, notices, recipients and room fallback. Do not reduce the model to the image's three sample tabs.
- Preserve mention suggestions and keyboard selection, item/player links, unread/latest indicators, history paging and scroll anchoring.
- Preserve pending/error/retry, slash commands, restrictions and duplicate-send protection.
- Equipment links/popovers become text-first but keep exact item identity and comparisons.
- Present Loot as a local pane/tab in the right-side utility area, backed by the existing [loot tracker][component-loot]. It is not a new server chat channel. Hide the composer in Loot.
- Expose loot in collapsed, floating and mobile layouts too. Preserve history/source labels, refresh and explicit clear-history behavior.
- Switching responsive mode must not lose draft, room, recipient or scroll context, or create duplicate subscriptions. If component recreation currently loses UI state, retain it within the existing chat layer.
- Use actual online counts and user data; no sample activity or hardcoded messages.

## 4. Shared presentation changes

Prefer adapting existing components. Add focused presentation helpers only where repeated use is established.

| Area | Work | Reuse constraint |
| --- | --- | --- |
| Page headings | Compact title, short context, adjacent actions/help; avoid repeated banners. | Adapt [DefaultHeader][component-header] and projection slots. Preserve semantic heading and guide entry. |
| Surfaces | Purposeful sections and fine dividers; reduce decorative nesting. | Migrate scoped callers before changing global card selectors. |
| Attributes | Grouped label/value rows, tabular numbers, optional equipment-rating detail. | Reuse overview grouping and [labels][attribute-labels]/[values][attribute-values]. |
| Essence summaries | Slot/name, verified optional level, active/passive names, assignment/source state. | Adapt existing [preview/details][component-essence-details]; preserve keyboard/touch inspection and dynamic slots. |
| Equipment summaries | Slot, item, rarity, rank/variant, empty/occupied/borrowed state and inspection. | Adapt [equipment overview][equipment-summary], Item/BaseItem and [equipment display][equipment-display]. Remove image columns at their source. |
| Lists/tables | Consistent filters, selected rows and inspector relationships. | Retain semantic tables/lists, sorting, virtualization, paging and stable selection. |
| Build context | Separate equipment preset, Essence preset, Style and submitted snapshot. | Use existing state services. A shared summary is presentational, not a new build state machine. |
| Buttons/tabs/forms | Consistent density, action emphasis and adjacent consequences. | Extend current regular-button/tabs/filters; retain events, focus, pending and disabled behavior. |
| Dialogs/popovers | Same typography/dividers and text entity identity. | Preserve CDK placement and existing focus trapping/restoration. |
| Feedback | Clear saving/error/retry and claim lifecycle. | Preserve actual states; never turn unknown/error into zero or successful empty. |
| Graphs/brackets | Legible relationships and linked selected detail. | Keep real node/round structure and hidden-information rules; no painting dependency. |

Do not create a universal card schema, form generator, replacement framework or parallel state library. Retain Angular standalone components, current signals/RxJS service boundaries, CDK/Material utilities and npm.

## 5. Character sheet reference implementation

Implement this first as a complete page. It establishes the visual reference for all later pages.

### 5.1 Truthful field mapping

| Field | Source and behavior |
| --- | --- |
| Name/title/guild/level/achievements | Existing profile DTO and presence/decoration components. Keep character name distinct from equipped title; respect viewer context. |
| Combat XP | Actual experience/next-level requirement and existing percentage formatting. |
| Combat Rating | Existing availability state and display conversion. Retain Unavailable. |
| Health | Max Health on ordinary profile; current/max only with real contextual current-health data. |
| Attributes | Existing baseCombatAttributes/baseAttributes selection, 19-entry grouping, equipment ratings and tooltips. |
| Threat | Existing estimatedEssenceThreatPerSecond, labeled as the Essence estimate. |
| Equipment | Actual owner equipment state; another player's equipment only where exposed by their data. |
| Essence names/abilities | The viewed loadout's actual slot definitions and existing preview mechanisms. |
| Essence levels | Owner archive joined by owned Essence ID, through existing cached state services. Missing other-player levels stay omitted/unavailable. |
| Combat Style | Owner's existing selected-style state. Never show it as another player's Style. |
| Presets/activity builds | Separate Equipment and Essence selectors/context, explicit current/edited/submitted state and existing save/update actions. |

No backend change is needed merely to reproduce missing mock data. A richer public-profile DTO would be a separately scoped enhancement.

### 5.2 All attributes

| Group | Required rows |
| --- | --- |
| Offense | Power; Attack Speed; Crit Chance; Crit Damage; Armor Penetration; Magic Penetration |
| Defense | Max Health; Physical Damage Reduction; Magical Damage Reduction; Dodge; Block; Damage Reduction |
| Recovery | Healing Power; Health Regen; Life Steal |
| Utility | Cooldown Reduction; Status Resistance; Crowd Control Resistance; Threat |

Use the actual [attribute catalog][attribute-catalog] and frontend formatters for labels, precision, suffixes, descriptions and rating/effective-value differences. Attack Speed is a percentage bonus; Health Regen is HP/5s. Known zero is distinct from unavailable data.

Wide layout: Offense/Defense above Recovery/Utility, alongside build context. Narrower layout: stack groups/build without dropping secondary attributes.

### 5.3 Text equipment and Essence rows

Replace equipment icons with named rows, in two columns where space permits:

Main Hand, Off Hand, Head, Relic, Chest, Necklace, Legs, Ring.

These are the [actual slots][equipment-slots], not the generated cloak/boots imagery. Show slot, item or Empty, rarity/relevant metadata and inspection/navigation. Preserve two-handed/off-hand, ghost/occupied, compatible-slot and ownership states.

Essences retain actual indices, empty/locked/unavailable states, unlocked slot count and eligibility. Active/passive names have focusable details. Mark a Conduit source only when actual equipped-style and eligibility rules make it one. Never hardcode three slots or three wolves.

Retain profile search/refresh, other-player inspection, own/read-only actions, presence, achievement points and optional account details. Verify both a populated owner and a sparse other-player profile; a full-data screenshot alone is insufficient.

## 6. Complete player-page coverage

IDs match sections 6.1–6.45 of the analysis. Each entry must be completed or explicitly resolved as unreachable/out of scope. Source links identify primary owning surfaces; their TS/styles, child components and dialogs are included.

### 6.1 Entry, shell and character development

| ID | Surface / source | Required result and preserved behavior | Phase |
| --- | --- | --- | --- |
| 01 | [Login, guest, maintenance][screen-login] | Compact branded form, clear login/guest/Google choices, validation and recovery. No authenticated sidebar/chat before login. | P8 |
| 02 | [Signup and binding][screen-signup] | Shared field styling with page/dialog-appropriate layout. Preserve new-account versus guest-binding context, validation, pending and errors. | P8 |
| 03 | [Shell][screen-shell], First Steps, [quest tracker][screen-tracker] | Chosen sidebar/header/right chat and responsive fallback. Preserve progression-filtered navigation, current activity, first-hunt guidance, offline catch-up/retry and restrictions. | P2 |
| 04 | [Character overview/inspection][screen-overview] | Section 5: complete attributes, text gear/build, truthful data, owner/other distinction, search and refresh. Establish the reference page. | P3 |
| 05 | [Inventory equipment/comparison][screen-inventory] | Text slot list, sortable/filterable inventory and selected inspector with deltas. Preserve exact identity, compatibility, multi-slot comparisons, favorites, new/equipped states and bulk protections. | P4 |
| 06 | [Equipment management][component-upgrade] and [presets][equipment-loadouts] | Current → next benefits with exact costs and action. Preserve rank/variant/set distinctions, save state, auto-use assignments, locked-preset copy and validation. | P4 |
| 07 | [Stock, containers, transfer][screen-inventory] | Named rows/quantities, selected-container choice, explicit recipient and binding. Preserve required choices, ownership and irreversible-action review. | P4 |
| 08 | [Absorb and shatter][screen-absorb] | Text unbound-Essence list and learned/received result. Keep absorb/shatter distinct, with quantity, ownership, requirements and destructive-action protection. | P4 |
| 09 | [Soul Archive/attunement/progression][screen-essences] | Text list/detail/loadout workspace, real levels/caps/XP/Ascension and ability effects. Preserve virtual scrolling, slot order/eligibility, presets, costs and saving/error states. | P4 |
| 10 | [Creature Archive/Focus][screen-essences] | Text creature → recorded source → desired Essence relationship. Keep discovery/absorption, kills, focus eligibility and cooldown; history subordinate. | P4 |
| 11 | [Essence Codex][screen-essences] | Collection list and selected members/bonus detail. Distinguish undiscovered, missing, absorbed, completed and Ascension states; preserve real source links. | P4 |
| 12 | [Combat Styles][screen-styles] | Text style selector and mechanic/refinement/upgrade workspace. Distinguish equipped/previewed/unsaved; preserve mastery, eligibility and Save/Discard. | P4 |
| 13 | [Achievements/titles][screen-achievements] | Category/achievement rows, progress and title actions. Preserve points, unlocks, completed/current states and equipped-title identity. | P8 |
| 14 | [Soulstone upgrades][screen-soulstones] | Compact list and selected current → next benefit, exact cost/balance/eligibility beside purchase. Preserve permanent progression and economics. | P8 |

### 6.2 World, combat and cooperation

| ID | Surface / source | Required result and preserved behavior | Phase |
| --- | --- | --- | --- |
| 15 | [Regions/combat areas][screen-region] | Named areas grouped by region/progression in place of repeated illustrated cards. Selected detail exposes enemies, sources/drops, access and Start/Resume. No invented geographic coordinates. | P5 |
| 16 | [Dungeon briefing/Sigils/mastery/records][screen-dungeons] | Entry/difficulty setup beside requirements/rewards, with records accessible without losing selection. Preserve guaranteed/chance/first-clear distinctions and Sigil assembly. | P5 |
| 17 | [Active dungeon/results][screen-dungeon-run] | Existing revealed graph plus selected-room decision pane, Vigor forecast and pending loot. Preserve Continue/Retreat, secured/lost/claimed outcomes, and hidden future nodes. | P5 |
| 18 | [Automatic combat/analysis][component-combat] | Text combatant groups, readable life/barrier/status/outcome, detailed damage/healing/threat/stagger/abilities. Preserve context, idle Stop versus playback Skip, history and unit selection. | P5 |
| 19 | [Tower overview/scouting/preparation/shop][screen-tower] | Compact floor rail, named Guardian, shared progress/reveals/readiness near expedition action. Keep First Clear/Echo and eligibility distinct. Preserve currently unavailable/empty shop state. | P6 |
| 20 | [Tower expedition/parties/report][screen-expedition] | Named roster, bench/slots and selected build; snapshot timestamp/update near readiness. Preserve applications, leadership, join/leave, click/drag assignment, balance and report. | P6 |
| 21 | [Personal Tower expeditions][screen-personal-tower] | Scannable attempt/history table, selected detail and resume. Preserve state, filtering/paging and links. | P6 |
| 22 | [Tower Hall of Fame][screen-hall] | Typographic first-clear recognition and floor/player/guild rows. Preserve unique first-clear meaning, records and inspection; no portrait dependency. | P6 |
| 23 | [Raid briefing/recruiting/Trophy Exchange][screen-raids] | Named encounter, recruitment and exact entry/reward context; text catalog plus purchase review. Preserve feature flag, requirements and trophy economics. | P6 |
| 24 | [Raid muster/party builder][screen-raid] | Text roster, three preliminary party assignments and Final Assault context. Keep roles, submitted builds, permissions, readiness/plan preview and click alternatives to drag. | P6 |
| 25 | [Raid playback/results/claims][screen-raid-playback] | Compact encounter/phase sequence and result, shared combat analysis, explicit claim state. Preserve captured parties, participation and eligibility. | P6 |
| 26 | [Regional boss][screen-region-boss] | Lifecycle-specific scheduled signup, live state, revival, contribution/milestone and claim layouts. Preserve server timing, phases, eligibility and restricted controls. | P6 |

### 6.3 Competition, guild and economy

| ID | Surface / source | Required result and preserved behavior | Phase |
| --- | --- | --- | --- |
| 27 | [Colosseum][screen-colosseum] / [Arena][screen-arena] | Compact local tabs, opponent list and selected matchup/action. Preserve tickets, rating/eligibility and submitted defense versus current build. | P6 |
| 28 | [Tournaments][screen-tournament] / [replay][screen-tournament-replay] | Text three-player teams, registration/readiness and clear round bracket. Preserve deadlines, snapshots, elimination relationships and replay navigation. | P6 |
| 29 | [Champion Market][screen-champion-market] | Named goods and selected purchase review. Preserve Glory balance, price, limits, eligibility and stock. | P7 |
| 30 | [Arena rankings][screen-arena-rankings] / [records][screen-arena-records] | Dense rank/history rows and selected result. Preserve metric ordering, links, filters and paging. | P7 |
| 31 | [Guild discovery][screen-no-guild] / [public profile][screen-public-guild] | Searchable guilds and selected public information/membership action. Preserve invitations/applications and viewer permissions. | P6 |
| 32 | [Guild headquarters/members][screen-in-guild] | Compact identity/shared objective, local tabs, named member roster and selected actions. Preserve roles, administration, leadership and recipient clarity. | P6 |
| 33 | [Guild Vault][screen-guild-vault] | Text equipment table with loan/ownership state and selected comparison. Keep borrow, return and permanent donation distinct, including permissions. | P7 |
| 34 | [Guild buildings][screen-guild-buildings] | Named buildings, upgrade/target relationships and contribution setup. Preserve shared resources, target, costs/progress and authorized controls. | P6 |
| 35 | [Guild missions][screen-guild-missions] | Shared objective and personal contribution/claim together; tiers/history secondary. Preserve time windows, permissions and individual eligibility. | P6 |
| 36 | [Guild shop][screen-guild-shop] / [rankings][screen-guild-rankings] | Text transaction/list patterns. Distinguish personal Favor from guild resources; preserve limits/timing, metric ordering and profile links. | P7 |
| 37 | [Bazaar commodities/order books][screen-market-commodity] | Compact catalog, selected commodity, ticket and readable bid/ask books. Keep exact price/quantity/total/fee, stock/balance and immediate fill versus resting order. | P7 |
| 38 | [Bazaar equipment][screen-market-buy] | Text exact-instance rows, filters and selected comparison/purchase. Preserve rarity/rank/tier/variant/quality, price and ownership consequences. | P7 |
| 39 | [Bazaar selling][screen-market-sell], [orders/history][screen-market-orders] | Owned-stock selection plus gross/fee/net; scannable order lifecycle. Preserve escrow, partial fills, capacity/expiry, self-trade restrictions and cancellation. | P7 |
| 40 | [Global Leaderboard][screen-leaderboard] | Compact discipline/scope, own-rank context, search/table and modest recognition. Preserve cursor paging, stale-result/retry and metric semantics. Keep existing tavern URL. | P7 |

### 6.4 Progress, account and shared surfaces

| ID | Surface / source | Required result and preserved behavior | Phase |
| --- | --- | --- | --- |
| 41 | [Quest Journal/reward choices/events][screen-quests] | Chain list and objective/detail workspace, progress/action/claim hierarchy. Separate personal/community eligibility; preserve pinning, destinations and irreversible reward choice. | P8 |
| 42 | [Prophecies][screen-prophecies] | Distinct choose → active → complete → claim → open stages, weekly Favor sequence and caches. Preserve reroll cost/eligibility, deadlines and accepted/expired/claimed states. | P8 |
| 43 | [Settings][screen-settings] / [Nobility][screen-nobility] | Prioritize reading/layout/account; compact Nobility context. Preserve preferences, binding, rename/logout, Signet quantity/expiry/retry and badge options. No new purchase checkout. | P8 |
| 44 | [Chat][screen-chat], [loot][component-loot], [session summary][component-session], help/feedback | Section 3.3 plus text links, accessible help/tours, concise offline rewards, retry/update/toasts. Preserve focus, recipients, claim/history distinction and notifications. | P2, P8 |
| 45 | [Not found][not-found], host routes, dormant/conditional views | Theme active error/access/recovery states; update routing wrappers. Keep dormant public routes/features disabled. Add no gathering/crafting/evolution/faction workflow from file/asset presence alone. | P8, P9 |

Inspect dialogs and transitive shared consumers for every row. A migrated top-level page with an old or broken inspector is not complete.

## 7. Sequence, effort and phase exits

Each phase includes relevant verification; P9 is the final cross-feature pass, not the first testing phase.

Estimate assumptions: one developer familiar with the repository, working local tooling, usable local accounts/fixtures, and no gameplay/backend redesign. Numbers include implementation and relevant verification but exclude waiting for environment access/review. Re-estimate after P3; these are planning ranges, not a delivery commitment.

| Phase | Deliverable | Dependencies | Focused developer days |
| --- | --- | --- | --- |
| P0 | Baseline screenshots/states, live route/subview inventory and existing test baseline. | — | 1–2 |
| P1 | Shared layout/type/row/heading patterns and text entity conventions, with unchanged colors. | P0 | 2–3 |
| P2 | Shell/sidebar/header, right chat/loot, collapse, responsive and UI-state preservation. | P1 | 3–5 |
| P3 | Complete reference character sheet, truthful bindings, text gear and Essence summaries. | P2 | 2–4 |
| P4 | Inventory/management/presets, Archive/Absorb/Creatures/Codex and Combat Styles. | P3 | 4–7 |
| P5 | Regions, dungeon briefing/run/results and shared automatic-combat presentation. | P3; P4 summaries | 4–7 |
| P6 | Tower/raids/boss, Arena/tournaments, guild identity/rosters/buildings/missions. | P4, P5 | 6–10 |
| P7 | Bazaar/Vault/shops, rankings and history surfaces. | P4; P6 context | 3–5 |
| P8 | Achievements/Soulstones/quests/Prophecies, settings/auth/errors, help/overlays. | P3; shared patterns | 3–5 |
| P9 | Full journey/viewport/state review, accessibility/performance/budgets, cleanup and handoff. | P0–P8 | 3–5 |
| Total | Complete player-application migration. | | **31–53 days** |

Approximately 6–11 full-time working weeks. Shell plus reference page arrives earlier: approximately 8–14 focused days including baseline and shared patterns. Part-time work or missing authenticated-state coverage extends calendar time. Complex cooperative/trading/realtime states are the largest uncertainty.

Phase exits:

- **P0:** Assign source ownership and reachable/conditional/unreachable classification to all IDs. Capture local owner/guest/advanced-account states. Record pre-existing failures separately.
- **P1:** Shared controls/text rows work with long labels and reading modes; colors unchanged. Migrate selected callers before blanket global styling changes.
- **P2:** Repeated navigation/resize preserves draft/room and creates no duplicate subscriptions. Chat collapse/mobile fallback, guards and current-action access work.
- **P3:** Reference design works with real owner data, empty slots, unavailable rating, other-player inspection and separate preset context. All attributes appear; no fabricated health/levels or equipment images.
- **P4:** Acquire → inspect → absorb/attune → equip/upgrade → resume works locally. Exact comparisons, restrictions and save/error behavior survive real density.
- **P5:** Idle Start/Stop and dungeon choose → fight → retreat/complete/fail retain server-owned consequences.
- **P6:** Local fixtures/session verify applications, assignments, snapshot update, permissions, phase transitions and eligibility. Feature flags stay unchanged.
- **P7:** Local verification covers immediate trade, resting order, cancellation/escrow, exact comparison, borrow/return/donate and purchases.
- **P8:** Onboarding/account/help/recovery align with new control locations, accessible navigation and correct guide/tour anchors.
- **P9:** Close all coverage IDs, pass final tests/builds and representative states/viewports, and report unresolved blockers honestly.

Use coherent changes per foundation, character reference and feature family. Avoid maintaining two full UI implementations indefinitely. Migrate scoped page classes, then remove obsolete selectors only after identifying their remaining callers.

## 8. File ownership and change boundaries

| Work area | Primary files / owners |
| --- | --- |
| Shell and responsive behavior | [Dashboard HTML][screen-shell], [TS][dashboard-ts], [global styles][style-global], [dashboard specs][dashboard-tests] |
| Sidebar/actions/header | [Sidebar][screen-sidebar], [sidebar service][service-sidebar], [header][screen-header], [quest tracker][screen-tracker], existing current-action/dungeon/raid components |
| Chat/loot/preferences | [Chat HTML][screen-chat], [TS][chat-ts], [SCSS][style-chat], [specs][chat-tests], [chat preferences][chat-pref], [sidebar preferences][sidebar-pref], [loot][component-loot], [equipment links][chat-equipment] |
| Reference sheet/style | [Overview HTML][screen-overview], [TS][overview-ts], [SCSS][style-overview], [Style summary][style-summary], existing character/Essence/equipment state |
| Equipment text/comparison | [Equipment overview][equipment-summary], [display][equipment-display], [mapping][equipment-map], [state][equipment-state], [presets][equipment-loadouts], Item/BaseItem/inventory-item/modal consumers |
| Essence text/details/progression | [Essence page][screen-essences], [Absorb][screen-absorb], [details][component-essence-details], [state][state-essence], [models][model-essence] |
| General shared UI | [Page header][component-header], existing buttons/tabs/filters/popovers, modal-container, attribute tooltips, generic-leaderboard and combat |
| Guides/recovery | [Guide catalog][guide-catalog], [tour overlay][tour], assets/help content, [session summary][component-session], update popup, toasts and guards |
| Individual pages | Owning source in each coverage row and its TS/styles/subcomponents. Keep feature logic in its existing boundary. |
| Build/test plumbing | Existing [npm scripts][package], [Angular config][angular], [Karma config][karma]. No dependency/builder migration planned. |

Core/Infrastructure/API files are not implementation targets for this presentation plan. Domain/catalog links are read-only evidence. Preserve dependency direction.

## 9. Behavior and state acceptance

Use actual state models rather than generic placeholder screens.

| State / risk | Required acceptance |
| --- | --- |
| Loading/refreshing | No false empty results or zeros. Retain old results during refresh where the current flow does so. |
| Empty/undiscovered/unavailable | Explain actual state and valid next action. Distinguish no owned object, locked slot, unknown entity and unsupported action. |
| Permissions/guest/feature gate | Preserve route guards and server eligibility. Gated controls must not appear available. |
| Pending/save failure | Preserve duplicate-action protection, saving/error and retry. Never claim irreversible success before it occurs. |
| Current/submitted build | Local equip changes must not imply Tower/raid/tournament/defense snapshots updated. Keep required explicit capture/update. |
| Comparisons | Preserve rating/effective-value units, modifiers, multi-slot equipment comparisons, fees, net, quantities and ownership. |
| Reward lifecycle | Distinguish earned/unclaimed/claimed, pending/secured/lost, shared/personal eligibility and owned/unopened caches. |
| Realtime changes | Roster/order/deadline/combat updates must not unexpectedly change selected context or permit stale actions. |
| Long content/collections | Stable row keys, virtualization/paging, readable full labels/details and no application-wide overflow. |
| Navigation | Preserve search/tab/selection/return context where supported. Retain list/detail return state introduced by new layouts. |
| Recovery | Keep retry/safe-return, session/maintenance/account restriction and offline catch-up handling. |
| Accessibility | Semantic headings/lists/tables, keyboard controls, visible focus, named icons, dialog focus restoration, reading preferences, reduced motion and touch alternatives. |

A URL redesign is unnecessary. Preserve routes/redirects. If a new layout otherwise loses essential tab/selection on Back/refresh, add narrow query-state support through existing router patterns; avoid an unrelated routing rewrite.

## 10. Verification and quality gates

### 10.1 Automated verification during implementation

Use npm only and keep its cache outside the checkout. Do not install dependencies merely to validate this plan.

From the frontend directory, install only if the lockfile/node_modules state requires it:

```powershell
Set-Location 'C:\repos\Legends-Legacy\legends-legacy\LL\src\Presentation\ll'
$env:npm_config_cache = Join-Path $env:TEMP 'legends-legacy-npm-cache'
npm ci
```

After shell or reference-page behavior changes, run relevant existing specs and update/add tests only for meaningful behavior or regressions:

```powershell
npm run test:ci -- --include=src/app/layout/dashboard/dashboard.component.spec.ts --include=src/app/layout/dashboard/chat/chat.component.spec.ts --include=src/app/features/game/character/character-overview/character-overview.component.spec.ts
npm run build:development
```

As affected components migrate, include their actual preference, chat-composer/mention/link, equipment comparison/loadout, attribute-format, Essence-slot, tab/navigation and help/focus tests. Do not add tests that simply assert decorative classes.

At completion:

```powershell
npm run test:ci
npm run build
git diff --check
```

Scripts generate state-sync/version files; inspect results and retain only intentional source artifacts. Respect current bundle/component-style budgets instead of raising thresholds to hide regressions.

If ChromeHeadless is unavailable, record the limitation and configure a known installed Chrome executable for the existing launcher. Do not switch package managers or silently skip the gate.

Backend tests are not expected for presentation-only work. If implementation exposes a justified backend change, scope it explicitly and use build/run-tests.ps1 as required. No shared/production database migration may be applied.

### 10.2 Visual and journey verification

Use local/test fixtures and local game sessions. Do not send test messages to players or perform transactions in an external environment.

- Check 1920×1080 and 1600×900 with right chat; 1440×900 and 1366×768 with chat open/collapsed and detailed/compact sidebar; 1024×768 medium fallback; 390×844 and 360×800 mobile.
- Repeat critical views with larger reading settings, Atkinson/system fonts, 200% zoom and reduced motion.
- Check docked/collapsed/floating/mobile chat transitions, keyboard/composer focus, draft retention, missing guild/raid rooms, unread states and loot.
- Verify actual color-token values and semantic colors remain unchanged; do not sample implementation colors from the generated image.
- Capture each coverage family in a representative working state and applicable empty/loading/error/locked/pending/claim states.
- On inventory, Archive, Bazaar, rosters and dungeon graphs, confirm all values remain reachable, nothing overlaps and the immediate action's consequence is visible.
- Check dialogs/tooltips near narrow-pane edges with chat open and at zoom.
- Preserve list responsiveness, virtualization and pagination; confirm no large art assets or duplicate realtime connections were introduced.
- Establish local fixtures/accounts for permissions, snapshots, tournaments, raids, orders and rewards. Log missing state coverage as unverified.

### 10.3 Definition of done

- [ ] All 45 coverage entries are completed or explicitly resolved as currently unreachable/non-feature work.
- [ ] All authenticated player routes share the right-chat shell and agreed visual rules.
- [ ] Existing color-token values, reading preferences and semantic colors remain intact.
- [ ] No portrait or Essence artwork is required in the migrated gameplay presentation.
- [ ] Equipment/slot pictures become text throughout applicable player views and linked inspectors.
- [ ] All 19 attributes use correct labels/units, including rating detail and tooltips.
- [ ] Equipment/Essence presets, current build and submitted snapshots stay distinct and truthful.
- [ ] Feature structures, costs, permissions, ownership, eligibility and recovery work.
- [ ] Chat channels/recipients/drafts/history/loot and responsive behavior survive shell changes.
- [ ] Desktop, mobile, overlay, keyboard, reading-mode and error-state checks pass.
- [ ] Relevant tests, full frontend tests, builds and budgets pass; external blockers are explicitly recorded.
- [ ] Guides/tours match implemented controls and anchors.
- [ ] Obsolete layout styles are removed only after caller verification; unrelated assets/features remain untouched.
- [ ] Final changes, verification, limitations and operational implications are documented.

## 11. Risks, tradeoffs and operational implications

| Risk / tradeoff | Treatment |
| --- | --- |
| Right chat/sidebar squeeze dense pages. | Smaller shell widths, container-aware reflow and chat collapse/fallback. Validate real density in P4 before migrating everything. |
| Literal mockup implementation invents data or combines independent systems. | Apply section 5 field mapping; omit unavailable data and show separate preset/snapshot context. |
| Global styles regress hidden shared subviews. | Migrate scoped callers, verify dialogs/inspectors, then remove legacy styling. |
| Text entities lose identity through truncation. | Preserve name/variant/rarity and selection. Allow wrapping/full inspection. Removing pictures must not remove identification. |
| Layout changes disturb asynchronous state. | Preserve existing services/component identity where practical; test draft, subscriptions, pending/save and stale state. |
| Authenticated edge states are hard to reproduce. | Prepare local accounts/fixtures in P0 and re-estimate after P3; keep a coverage log. |
| Screenshot density does not fit every device. | Treat it as a wide-desktop composition; reflow using real reading sizes and deliberate scrolling. |

Expected changes are frontend templates/styles plus focused presentation/state-composition adjustments. No new dependency, environment variable, authentication flow, database schema, service topology or art pipeline is planned.

Keep local preference keys compatible. A future additional preference requires sensible defaults and invalid-value handling, not resetting current choices.

No deployment is authorized by this plan. Implementation should produce reviewable source changes and verified build output. A later release follows the repository's normal process and separate authorization. Small feature-family changes support ordinary source reverts without a database rollback.

## 12. Verification of this planning deliverable

Created only UI_REWORK_IMPLEMENTATION_PLAN.md. Source inspection covered repository rules, the accepted image, audit inventory, route trees, shell/chat/preferences, overview/attribute/Essence/equipment contracts, shared displays, help and frontend test/build configuration.

Local file references, coverage IDs 01–45, phase references, effort arithmetic, Markdown whitespace and diff formatting were checked. No application code, dependencies, database state or external environment changed.

Frontend/backend builds and tests were not run for this documentation-only request. No required planning check was blocked. During discovery, an assumed standalone equipment-loadout model path was absent; the actual exported model was found in EquipmentLoadoutService and is correctly linked.

## Source references

[accepted]: <C:/repos/Legends-Legacy/legends-legacy/docs/design-concepts/2026-09-14-text-profile/01-right-chat.png>
[analysis]: <C:/repos/Legends-Legacy/legends-legacy/UI_REWORK_ANALYSIS.md>
[screen-shell]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/dashboard.component.html>
[chat-pref]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/client-side/chat-layout/chat-layout-preference.service.ts>
[sidebar-pref]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/client-side/sidebar-layout/sidebar-layout-preference.service.ts>
[style-tokens]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/styles/tokens.css>
[overview-ts]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/character-overview/character-overview.component.ts>
[equipment-loadouts]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/equipment/equipment-loadout.service.ts>
[model-essence]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/models/essence-system.ts>
[model-character]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/models/Dtos/characterDto.ts>
[equipment-map]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/equipment/equipment-display.ts>
[equipment-display]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/equipment/equipment-display/equipment-display.component.html>
[dashboard-ts]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/dashboard.component.ts>
[style-global]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/styles.css>
[screen-sidebar]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/sidebar/sidebar.component.html>
[screen-header]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/game-header/game-header.component.html>
[screen-chat]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/chat/chat.component.html>
[style-chat]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/chat/chat.component.scss>
[component-loot]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/loot-tracker/loot-tracker.component.html>
[component-header]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/default-header/default-header.component.html>
[attribute-labels]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe.ts>
[attribute-values]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/pipes/attributes/attribute-value-format/attribute-value-format.pipe.ts>
[component-essence-details]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/essences/essence-details/essence-details.component.html>
[equipment-summary]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/equipment-overview/equipment-overview.component.html>
[attribute-catalog]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Attributes/AttributeCatalog.cs>
[equipment-slots]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/models/Dtos/equipment-slots/equipmentSlot.ts>
[screen-login]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/public/landing/login/login.component.html>
[screen-signup]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/public/landing/signup/signup.component.html>
[screen-tracker]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/quest-tracker/quest-tracker.component.html>
[screen-overview]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/character-overview/character-overview.component.html>
[screen-inventory]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/inventory/inventory.component.html>
[component-upgrade]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/equipment/equipment-upgrade-panel/equipment-upgrade-panel.component.html>
[screen-absorb]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/essences/essences-absorb/essences-absorb.component.html>
[screen-essences]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/essences/essences.component.html>
[screen-styles]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/combat-styles/combat-styles.component.html>
[screen-achievements]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/achievements/achievements.component.html>
[screen-soulstones]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/soulstone-archive/soulstone-archive.component.html>
[screen-region]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region/region.component.html>
[screen-dungeons]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region/dungeons/dungeons.component.html>
[screen-dungeon-run]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region/dungeons/dungeon-page/dungeon-page.component.html>
[component-combat]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/combat/combat.component.html>
[screen-tower]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/tower/overview/tower-overview.component.html>
[screen-expedition]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/tower/rally/tower-rally.component.html>
[screen-personal-tower]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/tower/personal-expeditions/tower-personal-expeditions.component.html>
[screen-hall]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/tower/hall-of-fame/tower-hall-of-fame.component.html>
[screen-raids]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region/raids/raids.component.html>
[screen-raid]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/raid/raid-page.component.html>
[screen-raid-playback]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/raid/playback/raid-playback.component.html>
[screen-region-boss]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region-boss/region-boss.component.html>
[screen-colosseum]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/colosseum.component.html>
[screen-arena]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/arena-battle/arena-battle.component.html>
[screen-tournament]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/tournament-grounds/tournament-grounds.component.html>
[screen-tournament-replay]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/tournament-replay/tournament-replay.component.html>
[screen-champion-market]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/champions-market/champions-market.component.html>
[screen-arena-rankings]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/rankings-glory/rankings-glory.component.html>
[screen-arena-records]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/record-of-battle/record-of-battle.component.html>
[screen-no-guild]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/no-guild/no-guild.component.html>
[screen-public-guild]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/public-guild/public-guild.component.html>
[screen-in-guild]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/in-a-guild.component.html>
[screen-guild-vault]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-vault/guild-vault.component.html>
[screen-guild-buildings]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-buildings/guild-buildings.component.html>
[screen-guild-missions]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-missions/guild-missions.component.html>
[screen-guild-shop]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-shop/guild-shop.component.html>
[screen-guild-rankings]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-rankings/guild-rankings.component.html>
[screen-market-commodity]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place-commodity/market-place-commodity.component.html>
[screen-market-buy]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place-buy/market-place-buy.component.html>
[screen-market-sell]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place-sell/market-place-sell.component.html>
[screen-market-orders]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place-orders/market-place-orders.component.html>
[screen-leaderboard]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/tavern/tavern.component.html>
[screen-quests]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/quests/quest-journal-page.component.html>
[screen-prophecies]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/prophecies/prophecies-page.component.html>
[screen-settings]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/settings/settings.component.html>
[screen-nobility]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/settings/nobility-panel.component.html>
[component-session]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/session-summary-popup/session-summary-popup.component.html>
[not-found]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/error-pages/not-found-page/not-found-page.component.html>
[dashboard-tests]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/dashboard.component.spec.ts>
[service-sidebar]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/client-side/sidebar/sidebar.service.ts>
[chat-ts]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/chat/chat.component.ts>
[chat-tests]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/chat/chat.component.spec.ts>
[chat-equipment]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/chat/chat-equipment-link.component.ts>
[style-overview]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/character-overview/character-overview.component.scss>
[style-summary]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/combat-styles/combat-style-overview.component.ts>
[equipment-state]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/equipment/equipment-state.service.ts>
[state-essence]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/essences/essence-state.service.ts>
[guide-catalog]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/help/guide-catalog.ts>
[tour]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/first-party-tour-overlay/first-party-tour-overlay.component.ts>
[package]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/package.json>
[angular]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/angular.json>
[karma]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/karma.conf.cjs>
