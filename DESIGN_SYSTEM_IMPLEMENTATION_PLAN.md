# LegendsLegacy Design System Implementation Plan

Prepared 29 September 2026 for the **Grimoire** design system (the "Legend's Legacy" Design System artifact in Claude Design, mirrored as `lg-*` Angular components on `feature/grimoire-design-system`).

This is a backlog, a roadmap and a prompt library. It does not design the system. Every backlog item ends with an instruction written to be pasted into the Claude Design chat that owns the Grimoire design system.

**Evidence used:** the Grimoire artifact (`project/README.md`, `tokens.json`, 34 component READMEs and previews), the `lg-*` port (`src/styles/grimoire/tokens.css`, `components.css`), `UI_REWORK_ANALYSIS.md`, `UI_REWORK_IMPLEMENTATION_PLAN.md`, the design concepts in `docs/design-concepts/`, the combat lexicon, the Nobility specification, the prophecy currency plan, the attribute redesign records and the frontend enums (rarity, quality, equipment slots).

**How to use the prompts.** Paste them in backlog order, or follow Section 5 for the first fifteen. Each prompt assumes the earlier items it depends on are finished. After Claude Design completes an item, check it against DS-132 (the acceptance checklist) once that exists. Until then, check it against DS-003 (guardrails). Record any deviation Claude Design proposes in the decision log from DS-001. The prompts name existing Grimoire components and tokens on purpose, so that Claude Design extends the system rather than building a second one next to it.

---

## 1. Design System Goals

### 1.1 What the system must accomplish

1. **Screens are assembled, not invented.** A new feature (for example, Stronghold) should need only a page archetype, existing patterns, and new registry entries for its currencies, stats and states. It should not need new colours, new type sizes, new frame styles or a new card treatment.
2. **Dense information stays readable for hours.** Players spend long sessions reading numbers. At 1600×900 with the NavRail and docked Chronicle open, the Character Overview must show all attribute groups, the build and the Essence loadout without clipping. Dense tables must stay scannable at the default reading size and at the larger reading sizes.
3. **One meaning per signal.** Within any context, each colour family, shape, position and typeface has one job. A player should never have to ask whether gold means "selected", "currency", "primary action" or "important value".
4. **Game truth is always visible.** Costs, fees, loss risk (Pending Loot), permanence (guild donations), ownership and binding, captured versus live builds, and reward entitlement are never hidden behind hover or decoration.
5. **Identity without an art dependency.** Screens get their character from hierarchy, typography, relationships and a small curated asset set. When artwork exists it adds to that. When it does not, nothing breaks and nothing looks empty.
6. **The system grows through data.** A new currency, condition, attribute, collection, ranking or item type is a registry entry with an icon and formatting rules. It does not get a bespoke component.
7. **Accessibility is built in.** The reading-font setting, text size scaling, 4.5:1 text contrast, visible focus, keyboard operation, reduced motion and meaning without colour are part of every component's definition. They are not a later pass.
8. **A solo developer can implement and maintain it.** Every Design System component maps one-to-one onto an `lg-*` Angular component. Tokens are the only source of values. Documentation is short enough to be read.

### 1.2 Starting point: what Grimoire already provides

Grimoire is a real foundation and is not being thrown away:

- **Semantic colour tokens:** grounds, ink, gilt, arcana, sigil and tile, meters, status, chat channels, and the game's existing rarity and damage hues. There is also a mapping from the old `--ll-*` tokens.
- **Four typefaces with defined roles.** Marcellus is the display face. EB Garamond is for lore. Barlow is for the interface. Barlow Condensed is for numerals. Atkinson Hyperlegible is mapped for the readable-font setting. There are 17 named text styles, including the readable body style.
- **Spacing, radius and layout.** A 4px spacing scale, radius rules, and a shell (NavRail, Stage or Page, Folio, TopBar, KeyHints, Chronicle) with both of the game's chat layouts (docked and floating) and container-query breakpoints at 960px and 1536px.
- **34 components**, including GameShell, NavRail, TopBar, Page, PageHeader, Stage, Banner, Folio, Panel, Ledger, StatTile, StatFigure, LevelPlate, Meter, Track, Sigil, Constellation, Emblem, ItemSlot, ItemLink, LoadoutSlot, EntryList, TabStrip, Tag, Button, SearchField, Presence, JourneyCard, KeyHints, Chronicle and CurrencyPill, plus two screen compositions (ScreenOverview and ScreenArchive).
- **Voice rules:** you-address, a mechanics register and a separate lore register, sentence-case labels, number formatting, and no emoji.
- **Partial state rules:** hover, selected, ready, locked and focus.

### 1.3 Critical findings this plan addresses

1. **The composition model is built for a console-style detail screen, not a data-heavy PBBG.** "One screen, one subject" with Stage art, a Folio, 56px titles and 84px level numerals suits the Creature Archive. It does not suit the Bazaar, Guild members, the Vault, rankings or raid muster. The system needs density modes (DS-010) and a wider set of page archetypes (DS-110 onward). Your own direction already favours the information-dense Character Overview over decorative layouts.
2. **The type scale has gaps and has already drifted.** The token set defines 17 styles. `components.css` uses 19 distinct hard-coded pixel sizes. There is no section-title size between 17px `tab` and 36px `title-lg`. There is no compact body size for tables and no small tabular numeral. Styles are in px, which does not follow the game's reading-size preference (DS-008, DS-011).
3. **Gilt still carries too many meanings.** The audit found that one accent colour was used for entity names, selection, currency, progress and primary actions. Grimoire assigns gilt to group labels, eyebrows, effect values, Ledger values, the level numeral, the active nav marker, "selected" rules and the solid button. That is the same problem in a new colour (DS-006).
4. **The cool hues collide.** `arcana` #6fcab9, `success` #74c6d6, `info` #9ccaf0, `rarity-uncommon` #41f1b6, `rarity-rare` #7cb7ff, `damage-shadow` #69b6dd and `meter-sp` #4f9fd6 are close together. So are the warm hues (`gilt`, `warning`, `rarity-unique`, `rarity-legendary`, `damage-burn`) and the reds (`danger`, `rarity-legacy`, `damage-bleed`, `meter-hp`). The system needs allocation and co-occurrence rules, not only more tokens (DS-006).
5. **Core interaction components are missing.** There is no tooltip or hover card, popover, menu, dialog, confirmation, checkbox, toggle, select, number or quantity input, data table, toast, or empty, loading or error state. These are needed on nearly every screen (Phase 2).
6. **The state model is incomplete.** Five states are defined. The game needs roughly 30, across availability, lifecycle, ownership, knowledge and data freshness. It also needs rules for how states combine without visual noise (DS-019, DS-020).
7. **Game vocabularies are not yet data.** Only Cinders and Soulstones have currency treatment, but the game has at least 15 resources (Fate Echo, Sigil Fragments, Glory, Arena tickets, Tower Tokens, Guild Favor, Signets, Renown and others). There are 29 combat conditions with stacking models and no component. Item tier, quality, rank, style and set have no hierarchy, so they risk becoming interchangeable coloured badges (DS-051 to DS-062).
8. **The art direction is unresolved.** The 14 September plan chose text-only equipment and Essences with no portraits. Grimoire's ItemSlot, Stage and Banner are image-first. The system has to work in both cases (DS-023).
9. **Build and reward semantics have no visual language.** The live build, an unsaved preview, an assigned preset and a captured snapshot mean different things. So do available, completed, claimable, claimed and opened. Neither distinction has a visual system yet (DS-074, DS-079).
10. **The documentation is one long README.** Every rule is in `project/README.md` and `docs.sections` is empty. As the system roughly triples in size, it needs sections, a decision log and an acceptance checklist, or it will drift the way the current `ll-*` layer did (DS-001, DS-132).

### 1.4 How you will know it is working

- A new game screen can be specified as "Archetype X, with patterns Y and Z, using registry entries A and B", with no invented values.
- No component or screen uses a hex value, a pixel size or a shadow that is not a token.
- Any screen can pass the stress-test specimens (DS-131): long names, maximum numbers, four simultaneous states, the readable font at the largest size, and 1280px with docked chat.
- Every state in DS-019 can be shown on every component where it applies, using the channels DS-020 assigns.
- Adding a currency, a condition or a stat takes one registry entry plus one icon.

---

## 2. Design System Architecture

### 2.1 Layer model

The recommended structure adds two layers to the usual "foundations, components, patterns" stack. **Standards** are cross-cutting rules that every component must obey. **Registries** hold the game's vocabularies as data. Registries are what let the system scale for years.

```text
GOVERNANCE    principles · guardrails · decision log · acceptance checklist · stress tests · code parity
     │
FOUNDATIONS   colour · type · numerals · space & density · accessibility · layout · surfaces & layering
              lines · shape · ornament budget · motion · iconography
     │
STANDARDS     state model · state combinations · information hierarchy · progressive disclosure
(cross-cut)   art-optional contract · content: voice, terminology, number/time formatting, ability grammar
     │
REGISTRIES    rarity · item properties · resources & currencies · attributes & stats
(game data)   conditions · damage types & elements
     │
CORE          actions · overlays · inputs · selection · navigation · rows & tables · containers
COMPONENTS    tags & markers · progress · feedback · data states · shortcuts
     │
GAME          values & costs · stats & deltas · items · Essences & abilities · conditions · time
COMPONENTS    identity · requirements & locks · rewards · activities · combat · rankings · upgrades
     │
PATTERNS      master–detail · collection browser · inspection · comparison · transaction · claim
              loadout editor · roster assembly · milestones · feeds · live events · return summary
     │
SHELL         GameShell · NavRail · TopBar · current action · Chronicle · attention routing
     │
ARCHETYPES    14 page templates, each mapped to the game features that use it
```

**Dependency rule:** a layer may only use layers above it. A pattern may compose game components. A game component may never contain a pattern. A registry entry never defines layout.

### 2.2 How this maps onto the Claude Design artifact

| Layer | Where it lives in the Grimoire artifact |
|---|---|
| Governance, Foundations, Standards, Registries | Documentation sections (populate `docs.sections`), with token groups in `tokens.json`. `project/README.md` shrinks to an overview and a map of the sections. |
| Core and game components | One component folder each (`README.md` and `preview.html`), as today. |
| Patterns | Preview-only components named `Pattern<Name>` (for example `PatternMasterDetail`), each composed from real components. |
| Archetypes | Preview-only components named `Archetype<Name>`. Keep the existing `ScreenOverview` and `ScreenArchive` as worked examples of archetypes. |

**Naming:** keep the evocative names that already exist (Folio, Ledger, Chronicle, Sigil), but give each a plain-language subtitle in its README ("Folio — the detail panel"). New components use plain descriptive names (`DataTable`, `ConditionChip`, `CostList`), so the system stays searchable as it grows.

### 2.3 The registries are the scalability mechanism

Each registry is a documented table of entries. Each entry has an ID, a display name, a short form, an icon, formatting rules, a semantic class and usage notes. Components take a registry key, never a bespoke style.

| Registry | Entries today (examples) | Components that consume it |
|---|---|---|
| Rarity | Common, Uncommon, Rare, Epic, Unique, Legendary, Legacy (codes C to LG) | ItemSlot, ItemRow, ItemLink, Tag, item card |
| Item properties | Tier, Quality (Crude to Masterpiece), Rank, Style/variant, Set, 8 equipment slots | ItemRow, item card, comparison |
| Resources | Cinders, Soulstones, Fate Echo, Sigil Fragments, Sigils, Glory, Arena tickets, Tower Tokens, Trophies, Guild Favor, Supplies, Prophetic Favor, Signets, Renown | Amount, CostList, RewardBundle, TopBar, resource header |
| Attributes | Offense, Defense, Recovery and Utility groups (Power, Attack Speed, Armor Rating, Tenacity, Restoration, Threat…) with units and precision | StatRow, Ledger, breakdown, comparison |
| Conditions | 29 conditions: beneficial or harmful, damage over time, control, charges, intensity stacks | ConditionChip, unit frame, ability text |
| Damage and element | Physical, Magical, Bleed, Burn, Poison, Shadow, None; element affinities | combat numbers, ability text, log |

### 2.4 Page archetypes at a glance

| Archetype | Structural idea | Game features that should use it |
|---|---|---|
| Feature Hub | What needs attention, where to go next, one headline status | Colosseum home, World Tower overview, Guild headquarters landing, region overview |
| Character and Build | Identity, then capability, then the build that causes it | Character Overview (own and others'), Combat Styles |
| Collection Browser | Filterable set, a selected entry, known versus missing | Soul Archive, Creatures, Essence Codex, Achievements, Titles |
| Inventory and Exchange | Dense list, inspector, comparison, transaction | Inventory, Guild Vault, Cinder Bazaar, Champion Market, shops |
| Activity Selection and Preparation | Choose a target, check eligibility, see stakes, commit | Regions and areas, dungeon preview, Tower scouting, Arena opponents, raid preview |
| Encounter and Live Activity | Phase, living state, decisive information, outcome | Combat viewer, dungeon run, raid playback, regional boss, tournament replay |
| Ranking and Records | Ordered comparison with your own position and provenance | Leaderboard, Arena and Guild rankings, Hall of Fame |
| Social and Guild | Belonging, roles, contribution, members | Guild members and permissions, public guild profile, guild discovery |
| Progression Track and Tree | Ordered or branching investment with current and next step | Soulstone constellations, Tower ascent, Combat Style mastery, Prophecy weekly Favor |
| Management and Building | Owned structures, current to next level, timers, costs | Guild buildings, Stronghold (planned) |
| Objectives and Rewards | Lifecycle-driven tasks and claims | Quest Journal, Prophecies, community events |
| Activity History | Chronological, filterable, replayable | Personal expeditions, Record of Battle, trade history, guild activity, loot history |
| Detail and Profile | One entity inspected in full | Public player profile, public guild profile, tournament detail |
| Settings and Account | One utility task at a time, explicit consequences | Settings, account binding, Nobility |

### 2.5 What stays out of the Design System

- **LiveOps, the Admin dashboard and the admin Angular app.** They are separate operator tools and should use a plain utility style. Keeping them out stops operator density from leaking into player screens.
- **Public marketing pages** (the dormant Hero, World and FAQ routes). Only login, signup, maintenance and not-found are covered.
- **Bespoke compositions that belong to a single feature**, such as the dungeon route itself or the tournament bracket layout. The system provides the primitives (DS-086 node graph) and the feature owns the composition.
- **Per-feature colours, fonts or frame styles.** Features differ by structure, not by palette.
- **A light theme.** "Codex" was dropped, and Grimoire is the only theme. Keep token architecture theme-ready, but do not build a theme.
- **Balance values, formulas and prices.** The system defines how numbers are displayed, never what they are.
- **Theming for a general-purpose chart library.** DS-050 covers only the few chart forms the game actually uses.
- **Emoji, looping ambient animation, decorative header illustrations for every feature, and one-off celebration effects.**
- **A component per currency, status effect or activity type.** Those are registry entries (see 2.3).

---

## 3. Implementation Roadmap

### 3.1 Phases

| Phase | Items | Goal | Exit criteria |
|---|---|---|---|
| **0 — Groundwork** | DS-001 to DS-004 | Structure the documentation, set principles and guardrails, audit the 34 existing components | Doc sections exist; each existing component is marked keep, revise, merge or retire; the decision log holds the known decisions |
| **1 — Foundations and Standards** | DS-005 to DS-030 | Fix the rules everything else inherits: colour allocation, type ramp, numerals, density, accessibility, layout, surfaces, states, hierarchy, art-optional contract, shell contract, content | Tokens cover every value in `components.css`; the state model exists; existing components are updated to the new rules |
| **2 — Core Components** | DS-031 to DS-050 | Provide the interaction primitives every screen needs | Any non-game interaction (forms, overlays, tables, feedback) can be built without local styling |
| **3 — Game Registries and Components** | DS-051 to DS-088 | Turn the game's vocabularies into data and build the domain components | Items, Essences, abilities, conditions, rewards, requirements, timers, combat and rankings each have a component |
| **4 — Patterns** | DS-089 to DS-104 | Compose components into reusable interaction patterns | Every journey in the UI analysis (§7) is covered by named patterns |
| **5 — Shell and Page Archetypes** | DS-105 to DS-124 | Harden the shell and standardise page templates | Every current route maps to an archetype; the shell's attention routing is defined |
| **6 — Art, Motion, Sound and Governance** | DS-125 to DS-133 | Asset specifications, icon expansion, motion catalogue, scaling recipes, stress tests, contribution process, code parity | A new feature can be specified entirely from the system; stress tests pass |

### 3.2 Why this order

- **Foundations come before new components** because Grimoire already has 34 components built on assumptions this plan changes: colour roles, the type ramp, px sizing and the state set. Adding tables and dialogs first would copy those assumptions into twice as many places. Each foundation prompt tells Claude Design to update the affected existing components, so there is visible progress from the first prompt.
- **The state model (DS-019 and DS-020) comes before any new component**, because every component must declare which states it supports and through which channel.
- **Content standards (DS-025 to DS-030) sit in Phase 1** because formatting rules decide component anatomy. A cooldown display cannot be designed before the cooldown format is settled.
- **Overlays come early in Phase 2.** Tooltips and hover cards are the backbone of progressive disclosure, and the item card, stat breakdown and ability description all depend on them.
- **Registries (DS-051, DS-053, DS-055, DS-059) open Phase 3.** Game components consume registries, so the registries must exist first.
- **The shell contract (DS-024) is a Phase 1 item.** It decides what information is persistent and what is contextual, which affects TopBar, resource headers and page headers. The detailed shell work (DS-105 to DS-109) comes after the components it uses: badges, toasts and overlays.
- **Archetypes come last.** An archetype is only a template if the parts it names already exist.

### 3.3 Parallel tracks

- **Content (DS-025 to DS-030)** can run alongside Phase 1 visual work once DS-002 exists.
- **Art direction (DS-125 and DS-126)** can start any time after DS-023, if you decide to commission art sooner.
- **Code parity (DS-133)** should run continuously: after each accepted Design System item, port it to `lg-*`.

### 3.4 Critical dependency chains

```text
DS-001 docs ─► DS-002 principles ─► DS-003 guardrails ─► DS-004 audit
DS-005 colour ─► DS-006 allocation ─► DS-007 feedback/delta ─► DS-019 states ─► DS-020 combinations
DS-008 type ─► DS-009 numerals ─► DS-010 density ─► DS-011 a11y/scaling ─► DS-012 layout
DS-019 + DS-021 hierarchy ─► DS-022 disclosure ─► DS-032 tooltip/hover card ─► DS-069 item card ─► DS-070 comparison
DS-053 resource registry ─► DS-054 amount/cost ─► DS-073 rewards ─► DS-074 claim ─► DS-095 claim flow
DS-040 rows ─► DS-041 table ─► DS-090 master–detail ─► DS-091 collection browser ─► DS-113/114 archetypes
DS-079 build identity ─► DS-080 combatant ─► DS-100 roster ─► DS-115 activity preparation
```

## 4. Design System Backlog

Priorities: **Critical** means it blocks many later items or fixes a systemic problem. **High** means it is needed before the related screens migrate. **Medium** means it is valuable but can follow the first screen migrations. **Low** is optional or future.

**Backlog index**

| ID | Name | Category | Priority | Depends On |
|---|---|---|---|---|
| DS-001 | Documentation Architecture & Decision Log | Governance | Critical | None |
| DS-002 | Design Principles | Foundation | Critical | DS-001 |
| DS-003 | Anti-Generic Guardrails | Foundation | Critical | DS-002 |
| DS-004 | Existing Component Audit & Consolidation Map | Governance | Critical | DS-002, DS-003 |
| DS-005 | Colour Architecture | Foundation | Critical | DS-001, DS-004 |
| DS-006 | Colour Channel Allocation & Collision Rules | Foundation | Critical | DS-005 |
| DS-007 | Feedback, Delta & Effect Polarity Colours | Foundation | Critical | DS-005, DS-006 |
| DS-008 | Typography Ramp for Data-Dense UI | Foundation | Critical | DS-002, DS-004 |
| DS-009 | Numeric Typography & Alignment | Foundation | Critical | DS-008 |
| DS-010 | Spacing Scale & Density Modes | Foundation | Critical | DS-004, DS-008 |
| DS-011 | Accessibility Baseline & Text Scaling | Foundation | Critical | DS-005, DS-008, DS-010 |
| DS-012 | Layout Grid, Content Widths & Container Breakpoints | Foundation | Critical | DS-010, DS-011 |
| DS-013 | Surfaces, Elevation & Layering | Foundation | High | DS-005, DS-010 |
| DS-014 | Lines, Dividers & Borders | Foundation | High | DS-005, DS-013 |
| DS-015 | Shape Language & Radius | Foundation | High | DS-003, DS-013 |
| DS-016 | Ornament, Texture & Glow Budget | Foundation | High | DS-003, DS-013, DS-015 |
| DS-017 | Motion Principles & Tokens | Foundation | High | DS-002, DS-011 |
| DS-018 | Iconography Foundations & Taxonomy | Foundation | High | DS-011, DS-015 |
| DS-019 | State Model | Foundation | Critical | DS-004, DS-006, DS-007, DS-011 |
| DS-020 | State Combination & Priority Rules | Foundation | Critical | DS-019 |
| DS-021 | Information Hierarchy Levels | Foundation | Critical | DS-008, DS-009, DS-019 |
| DS-022 | Progressive Disclosure Model | Foundation | Critical | DS-021 |
| DS-023 | Art-Optional Contract | Foundation | Critical | DS-002, DS-004 |
| DS-024 | Application Shell Contract | Layout | Critical | DS-012, DS-013, DS-021 |
| DS-025 | Voice, Capitalisation & Label Rules | Content | High | DS-002, DS-021 |
| DS-026 | Terminology Glossary & Naming Rules | Content | Critical | DS-025 |
| DS-027 | Number, Unit & Stat Formatting | Content | Critical | DS-009, DS-026 |
| DS-028 | Time, Duration, Cooldown & Reset Formatting | Content | High | DS-027 |
| DS-029 | Ability & Effect Description Grammar | Content | High | DS-026, DS-027, DS-028 |
| DS-030 | Action Labels, Confirmations, Errors & Empty-State Copy | Content | High | DS-025, DS-026 |
| DS-031 | Button System (including icon-only buttons and toolbars) | Component | Critical | DS-015, DS-019, DS-030 |
| DS-032 | Tooltip & Hover Card Architecture | Component | Critical | DS-011, DS-013, DS-022 |
| DS-033 | Popover, Menu & Context Menu | Component | High | DS-032 |
| DS-034 | Dialogs, Confirmations & Drawers | Component | Critical | DS-013, DS-030, DS-031 |
| DS-035 | Form Field, Text & Quantity Input | Component | High | DS-019, DS-027, DS-031 |
| DS-036 | Selection & Range Controls | Component | High | DS-035 |
| DS-037 | Select, Dropdown & Combobox | Component | High | DS-033, DS-035 |
| DS-038 | Tabs & Segmented Controls | Component | High | DS-011, DS-019 |
| DS-039 | Filter Bar, Sort & Search | Component | High | DS-036, DS-037, DS-038 |
| DS-040 | Rows & Lists | Component | Critical | DS-010, DS-014, DS-019, DS-020, DS-021 |
| DS-041 | Data Table | Component | Critical | DS-009, DS-039, DS-040 |
| DS-042 | Containers: Panel, Section, Card, Folio & Banner | Component | High | DS-004, DS-013, DS-014, DS-016 |
| DS-043 | Tags, Badges, Counters & Markers | Component | High | DS-019, DS-020 |
| DS-044 | Progress: Meters & Tracks | Component | High | DS-007, DS-019 |
| DS-045 | Wayfinding: Breadcrumbs, Back & Pagination | Component | Medium | DS-038 |
| DS-046 | Toasts & Inline Alerts | Component | High | DS-007, DS-030 |
| DS-047 | Loading, Refreshing & Stale Data | Component | High | DS-017, DS-019 |
| DS-048 | Empty, Error & Recovery States | Component | High | DS-030, DS-047 |
| DS-049 | Keyboard Shortcuts & KeyHints | Component | Medium | DS-011, DS-031 |
| DS-050 | Charts & Data Visualization Basics | Component | Low | DS-006, DS-009 |
| DS-051 | Rarity System | Game Registry | Critical | DS-006, DS-015, DS-020, DS-043 |
| DS-052 | Item Property Hierarchy | Game Registry | Critical | DS-021, DS-026, DS-051 |
| DS-053 | Resource & Currency Registry | Game Registry | Critical | DS-018, DS-026, DS-027 |
| DS-054 | Resource Amount & Cost Display | Game Component | Critical | DS-009, DS-019, DS-031, DS-053 |
| DS-055 | Attribute & Stat Registry | Game Registry | Critical | DS-018, DS-026, DS-027 |
| DS-056 | Stat Row & Stat Block | Game Component | High | DS-021, DS-032, DS-055 |
| DS-057 | Delta & Comparison Indicators | Game Component | High | DS-007, DS-009, DS-027 |
| DS-058 | Attribute Breakdown & Effective Values | Game Component | High | DS-032, DS-056, DS-057 |
| DS-059 | Damage Type & Element Semantics | Game Registry | High | DS-006, DS-018 |
| DS-060 | Condition Registry & Condition Chip | Game Registry + Game Component | High | DS-018, DS-028, DS-029, DS-043 |
| DS-061 | Ability Presentation (Active & Passive) | Game Component | High | DS-029, DS-032, DS-060 |
| DS-062 | Timers, Cooldowns & Countdowns | Game Component | High | DS-028, DS-044 |
| DS-063 | Levels, Ranks, Tiers & XP | Game Component | High | DS-008, DS-026, DS-044 |
| DS-064 | Requirements & Eligibility | Game Component | Critical | DS-019, DS-030 |
| DS-065 | Locked Content & Unlock Preview | Game Component | High | DS-023, DS-064 |
| DS-066 | Player Identity & Social Rows | Game Component | High | DS-026, DS-040 |
| DS-067 | Portrait & Emblem Frames | Game Component | Medium | DS-015, DS-023 |
| DS-068 | Item Slot & Item Row | Game Component | Critical | DS-020, DS-023, DS-040, DS-051, DS-052 |
| DS-069 | Item Inspection Card | Game Component | Critical | DS-032, DS-052, DS-056, DS-057, DS-068 |
| DS-070 | Equipment Comparison | Game Component | High | DS-057, DS-069 |
| DS-071 | Essence Presentation & Loadout Slot | Game Component | High | DS-061, DS-063, DS-068 |
| DS-072 | Creature & Monster Entry | Game Component | High | DS-059, DS-064, DS-067 |
| DS-073 | Reward Display & Bundles | Game Component | Critical | DS-019, DS-054, DS-068 |
| DS-074 | Reward Lifecycle & Claim States | Game Component | High | DS-019, DS-031, DS-073 |
| DS-075 | Objective Entry (Quest, Prophecy, Event Goal) | Game Component | High | DS-062, DS-064, DS-074 |
| DS-076 | Achievements, Titles & Collection Progress | Game Component | Medium | DS-044, DS-066, DS-074 |
| DS-077 | Activity Card | Game Component | High | DS-023, DS-042, DS-054, DS-064, DS-073 |
| DS-078 | Stakes & Risk Summary | Game Component | High | DS-007, DS-054, DS-073 |
| DS-079 | Build Identity, Presets & Snapshot States | Game Component | High | DS-019, DS-026, DS-037 |
| DS-080 | Combatant Summary | Game Component | High | DS-056, DS-066, DS-079 |
| DS-081 | Combat Unit Frame | Game Component | High | DS-044, DS-060, DS-066, DS-067 |
| DS-082 | Combat Log & Event Line | Game Component | High | DS-027, DS-059, DS-060 |
| DS-083 | Combat Result & Outcome | Game Component | High | DS-056, DS-073 |
| DS-084 | Ranking Entry & Position | Game Component | High | DS-041, DS-066 |
| DS-085 | Upgrade Preview (Current → Next) | Game Component | High | DS-054, DS-056, DS-057, DS-062, DS-064 |
| DS-086 | Node Graph Primitives | Game Component | Medium | DS-015, DS-019 |
| DS-087 | Market Listing & Order Book | Game Component | Medium | DS-041, DS-054, DS-068 |
| DS-088 | Attention Indicators & Navigation Badges | Game Component | High | DS-020, DS-043 |
| DS-089 | Page & Feature Header Pattern | Pattern | High | DS-008, DS-031, DS-038, DS-054 |
| DS-090 | Master–Detail Pattern | Pattern | Critical | DS-024, DS-040, DS-041, DS-042 |
| DS-091 | Filterable Collection Browser | Pattern | High | DS-039, DS-040, DS-041, DS-047, DS-048, DS-068 |
| DS-092 | Inspection Layers Pattern | Pattern | High | DS-022, DS-032, DS-069, DS-090 |
| DS-093 | Comparison Pattern | Pattern | High | DS-057, DS-070 |
| DS-094 | Transaction & Confirmation Flow | Pattern | Critical | DS-030, DS-034, DS-046, DS-054 |
| DS-095 | Reward Reveal & Claim Flow | Pattern | High | DS-017, DS-046, DS-073, DS-074 |
| DS-096 | Build Summary & Loadout Editor | Pattern | High | DS-061, DS-068, DS-071, DS-079 |
| DS-097 | Progression Milestone Pattern | Pattern | Medium | DS-044, DS-085, DS-086 |
| DS-098 | Leaderboard Layout | Pattern | Medium | DS-038, DS-041, DS-045, DS-084 |
| DS-099 | Activity Feed & History | Pattern | Medium | DS-028, DS-040, DS-045, DS-082 |
| DS-100 | Party & Roster Assembly | Pattern | High | DS-064, DS-079, DS-080 |
| DS-101 | Live Event & Phase-Based Activity | Pattern | Medium | DS-047, DS-062, DS-081 |
| DS-102 | Return Summary (Offline Progress) | Pattern | Medium | DS-034, DS-073, DS-083 |
| DS-103 | Guidance, Onboarding & Locked Features | Pattern | Medium | DS-004, DS-065 |
| DS-104 | Premium & Nobility Presentation | Pattern | High | DS-053, DS-066, DS-094 |
| DS-105 | NavRail Rules | Shell | High | DS-024, DS-065, DS-088 |
| DS-106 | TopBar & Global Resources | Shell | High | DS-024, DS-054, DS-063 |
| DS-107 | Current Action Indicator | Shell | High | DS-024, DS-044, DS-062 |
| DS-108 | Chronicle Standards (Chat & Game Log) | Shell | High | DS-024, DS-066, DS-068, DS-082 |
| DS-109 | Attention Hierarchy & Notification Routing | Shell | High | DS-034, DS-046, DS-088 |
| DS-110 | Page Archetype Catalogue | Page Archetype | High | DS-024, DS-089, DS-090, DS-091 |
| DS-111 | Archetype: Feature Hub | Page Archetype | Medium | DS-077, DS-088, DS-089, DS-110 |
| DS-112 | Archetype: Character & Build Management | Page Archetype | High | DS-056, DS-079, DS-096, DS-110 |
| DS-113 | Archetype: Collection Browser | Page Archetype | High | DS-076, DS-091, DS-110 |
| DS-114 | Archetype: Inventory & Exchange Workbench | Page Archetype | High | DS-087, DS-090, DS-093, DS-094, DS-110 |
| DS-115 | Archetype: Activity Selection & Preparation | Page Archetype | High | DS-064, DS-077, DS-078, DS-100, DS-110 |
| DS-116 | Archetype: Encounter & Live Activity | Page Archetype | High | DS-081, DS-082, DS-083, DS-086, DS-101, DS-110 |
| DS-117 | Archetype: Ranking & Records | Page Archetype | Medium | DS-084, DS-098, DS-110 |
| DS-118 | Archetype: Social & Guild | Page Archetype | Medium | DS-041, DS-066, DS-088, DS-110 |
| DS-119 | Archetype: Progression Track & Tree | Page Archetype | Medium | DS-085, DS-086, DS-097, DS-110 |
| DS-120 | Archetype: Management & Building | Page Archetype | Medium | DS-062, DS-085, DS-110 |
| DS-121 | Archetype: Objectives & Rewards | Page Archetype | Medium | DS-074, DS-075, DS-095, DS-110 |
| DS-122 | Archetype: Activity History | Page Archetype | Low | DS-045, DS-099, DS-110 |
| DS-123 | Archetype: Detail & Profile Page | Page Archetype | Medium | DS-089, DS-092, DS-110 |
| DS-124 | Archetype: Settings & Account | Page Archetype | Medium | DS-036, DS-104, DS-110 |
| DS-125 | Art Direction & Illustration Usage | Art | High | DS-002, DS-016, DS-023 |
| DS-126 | Asset Specifications, Frames & Cropping | Art | Medium | DS-067, DS-068, DS-125 |
| DS-127 | Game Icon Set Expansion | Art | Medium | DS-018, DS-053, DS-059, DS-060 |
| DS-128 | Motion Catalogue: Feedback & Reward Moments | Foundation | Medium | DS-017, DS-095 |
| DS-129 | UI Sound Feedback (Optional) | Foundation | Low | DS-017, DS-109 |
| DS-130 | Scalability Recipes | Governance | High | DS-053, DS-055, DS-060, DS-088, DS-110 |
| DS-131 | Stress-Test Specimens | Governance | Medium | DS-011, DS-012, DS-020, DS-041, DS-068 |
| DS-132 | Contribution Process & Acceptance Checklist | Governance | High | DS-001, DS-003, DS-131 |
| DS-133 | Code Parity & Handoff Map | Governance | Medium | DS-001, DS-132 |

### Phase 0 — Groundwork

#### DS-001 — Documentation Architecture & Decision Log

**Category:** Governance
**Priority:** Critical
**Depends On:** None

**Purpose:**
All of Grimoire's rules live in one README, and `docs.sections` is empty. The system is about to roughly triple in size. Without a section structure and a record of decisions, later prompts will add rules in inconsistent places, and Claude Design may quietly reverse decisions you have already made.

**Should Define:**

- A section tree: Start here · Principles · Foundations (Colour, Typography, Numerals, Space & Density, Accessibility, Layout, Surfaces & Layering, Lines, Shape, Ornament, Motion, Iconography) · Standards (States, Information Hierarchy, Disclosure, Art-optional, Content) · Registries · Components · Game Components · Patterns · Shell · Page Archetypes · Governance.
- `project/README.md` reduced to an overview: what LegendsLegacy is, the layer model, and how to read and extend the system.
- A standard section template: purpose, rules (must, should, never), tokens used, do/don't pairs, related components.
- A standard component README template: plain-language subtitle, when to use, when not to use, anatomy, supported states (from DS-019), density variants, content rules, accessibility notes, related components, and a status (Stable, Revising, Draft, Deprecated).
- Naming conventions: keep the evocative names (Folio, Ledger, Chronicle) with plain subtitles; give new components plain names; name compositions `Pattern<Name>` and `Archetype<Name>`.
- A decision log format (ID, date, decision, reason, consequence, status), seeded with the decisions already made.

**Examples in LegendsLegacy:**
Decisions to seed the log with: Grimoire is the only theme and the light "Codex" theme is dropped. The docked and floating chat layouts from Settings are kept. Equipment and Essences are text-first until an art strategy is decided (14 September plan). One Folio per screen. Loot is a Chronicle channel. The rarity code is always shown. The Nobility ◆ mark rules. Information-dense Character Overview over decorative layouts.

**Paste-ready Claude Design instruction**

```text
Restructure the documentation of the existing Legend's Legacy (Grimoire) Design System so it can grow roughly
three times larger without becoming one long README. Populate the documentation sections with this tree: Start
here; Principles; Foundations (Colour, Typography, Numerals, Space & Density, Accessibility, Layout, Surfaces
& Layering, Lines, Shape, Ornament, Motion, Iconography); Standards (States, Information Hierarchy,
Progressive Disclosure, Art-optional, Content); Registries; Components; Game Components; Patterns; Shell; Page
Archetypes; Governance. Move the existing README content into the matching sections without changing its
meaning. Reduce project/README.md to a short overview: what LegendsLegacy is (a desktop-first, data-heavy
fantasy PBBG), the layer model, and how to read and extend the system. Create two templates in Governance. The
first is for sections: purpose, rules as must/should/never, tokens used, do/don't pairs, related components.
The second is for component READMEs: plain-language subtitle, when to use, when not to use, anatomy, supported
states, density variants, content rules, accessibility, related components, and a status of Stable, Revising,
Draft or Deprecated. Keep the existing evocative component names (Folio, Ledger, Chronicle, Sigil) and add a
plain subtitle to each, such as "Folio — the detail panel". New components get plain descriptive names;
compositions are named Pattern<Name> and Archetype<Name>. Add a Decision Log page (ID, date, decision, reason,
consequence, status) and seed it with: Grimoire is the only theme and the light Codex theme is dropped; the
Settings chat layouts, docked and floating, are preserved; equipment and Essences are presented text-first
until an art strategy is decided; one Folio per screen; loot is a Chronicle channel; the rarity code always
accompanies the rarity colour; the Nobility ◆ mark follows the display rules; information density is preferred
over decorative layouts. Do not change any component visuals in this step.
```

---

#### DS-002 — Design Principles

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-001

**Purpose:**
Grimoire has voice rules and one composition rule ("one screen, one subject"). It has no principles for resolving the trade-off this game faces constantly: density against atmosphere. Principles are what Claude Design falls back on when a prompt is ambiguous.

**Should Define:**

- Seven or eight principles. Each has a one-line statement, what it means in practice, the trade-off it resolves, and a LegendsLegacy example.
- Recommended set:
  - **Decisions first:** lead with what the player must decide next.
  - **Truth over atmosphere:** every value the game knows is visible and labelled; atmosphere never replaces information.
  - **Dense, not crowded:** density comes from alignment, type and rhythm, not boxes.
  - **One meaning per signal.**
  - **Mechanics give identity:** a route, an ascent or an order book shapes the screen, not decoration.
  - **Art enhances, never carries.**
  - **Calm until it matters:** attention is a budget.
  - **Built to grow:** registries, not bespoke components.
- A priority order for when principles conflict. For example, truth outranks atmosphere and decisions outrank completeness.
- A short "how to apply" checklist to use when reviewing any screen or component.

**Examples in LegendsLegacy:**
Dungeon runs lead with the route choice, Vigor cost and Pending Loot risk, not the room illustration. The Bazaar never hides the fee or the net amount. The Character Overview shows every attribute and explains each one on focus.

**Paste-ready Claude Design instruction**

```text
Add a Principles section to the existing Grimoire Design System. LegendsLegacy is a desktop-first browser RPG
where players spend hours reading numbers, comparing builds and managing many interlocking systems, so the
principles must resolve the constant tension between information density and atmosphere. Write seven or eight
principles. For each, give a one-line statement, what it means in practice, the trade-off it resolves, and one
concrete LegendsLegacy example. Use this set as the starting point and refine the wording to Grimoire's voice.
Decisions first: lead with what the player must decide next. Truth over atmosphere: every value the game knows
is visible, labelled and explainable. Dense, not crowded: density comes from alignment, typography and rhythm,
not from more boxes. One meaning per signal: a colour, shape or position means one thing in a given context.
Mechanics give identity: a dungeon route, a Tower ascent or an order book shapes the screen more than
decoration does. Art enhances, never carries: every screen works without illustration. Calm until it matters:
attention is a budget spent on risk, rewards and required action. Built to grow: new systems are registry
entries, not new visual languages. Then add a priority order for when principles conflict (truth over
atmosphere, decisions over completeness, and so on), and a short review checklist to apply to any new
component or screen. Link the existing "one screen, one subject" rule to the relevant principle and note where
it applies (detail and collection screens) and where it does not (dense workbench screens such as the Bazaar
or guild members). Keep it concise; this is a working tool, not a manifesto.
```

---

#### DS-003 — Anti-Generic Guardrails

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-002

**Purpose:**
The UI audit found specific generic signals: cards used for everything, containers nested inside containers, a repeated heading–subtitle–grid rhythm, rows of equal stat tiles, and look-alike progress cards on different systems. A design system that doesn't forbid these reproduces them at scale. Some existing Grimoire ingredients (pill buttons, glow tokens, corner ornaments, 56–84px display type) are fine in moderation and generic or noisy when overused.

**Should Define:**

- A risk → rule table covering:
  - Cards: only for bounded collectible objects or opponents.
  - Nesting: at most two enclosure levels.
  - Stat tile grids: only when each tile affects the next decision, and at most four.
  - Heading stacks: no eyebrow that repeats the nav group; no icon on every panel heading.
  - Radius: containers stay square.
  - Gradients: only for art fades and vignettes.
  - Glass: no backdrop blur on surfaces.
  - Glow: only for selected, ready, focus and primary hover.
  - Borders: only where they separate decisions or mark interactive edges.
  - Ornament: governed by the budget in DS-016.
  - Hero art: Banner and Stage only with a real subject.
  - Oversized type: one display-size element per screen.
  - Whitespace: governed by density modes.
  - Mobile-first layouts on desktop: no single-column card feeds at desktop widths.
- A banned-tropes list: icon in a circle beside every heading, feature-card triplets, gradient text, sparkle or magic-wand icons, badge soup, emoji, "Welcome back, hero!" copy, uniform padding at every nesting level, identical card grids for mechanically different systems, and parchment textures.
- A **sameness test**: if two mechanically different systems (Achievements and Soulstones, for example) render with the same structure, the difference must be justified or the structure changed.
- A fantasy-balance rule: fantasy comes from type, the lore voice and a few ornaments, never from skeuomorphic parchment, wood or metal UI.
- A review checklist of 10 to 12 yes/no questions.

**Examples in LegendsLegacy:**
The Codex (collection → bonus group → member rows, each boxed). Colosseum's seven status tiles. Soulstone upgrade cards that repeat rank, effect and action. `DefaultHeader` repeated across 15 templates.

**Paste-ready Claude Design instruction**

```text
Add an "Anti-generic guardrails" page to the Principles section of the existing Grimoire Design System. The
goal is to stop LegendsLegacy from drifting toward a generic SaaS dashboard or a stereotypical AI-generated
game UI as the system grows. A previous audit of the game found these specific problems: cards used for every
data group, containers nested three or four deep, a repeated heading → subtitle → grid of tiles rhythm, rows
of equal-sized stat tiles, identical progress cards for mechanically different systems (Achievements,
Soulstones, Essence Codex), and one accent colour used for too many meanings. Write a risk → rule table
covering: excessive cards; container nesting (set a maximum of two enclosure levels); stat tile grids; heading
stacks and repeated eyebrows; rounded rectangles; gradients (allow them only for art fades and vignettes);
glassmorphism; glow (allow it only for selected, ready, focus and primary-button hover); excessive borders;
fantasy ornamentation (defer to the ornament budget); hero artwork without a subject; oversized typography
(one display-size element per screen); excessive whitespace; mobile-app layouts on desktop. Add a
banned-tropes list (an icon in a circle beside every heading, feature-card triplets, gradient text, sparkle or
wand icons, badge soup, emoji, cheerful copy such as "Welcome back, hero!", the same padding at every nesting
level, parchment or wood textures). Add a "sameness test": two mechanically different systems must not share
an identical structure without a stated reason. Add a fantasy-balance rule: the fantasy identity comes from
typography, the lore register and a small set of ornaments, never from skeuomorphic parchment, wood or metal.
End with a 10–12 question yes/no review checklist. Where an existing Grimoire component (Banner, Folio, Stage,
StatTile, Button's pill shape, glow-gilt, glow-selected) is at risk of overuse, name it and state its limit.
```

---

#### DS-004 — Existing Component Audit & Consolidation Map

**Category:** Governance
**Priority:** Critical
**Depends On:** DS-002, DS-003

**Purpose:**
Grimoire has 34 components and two screen compositions. Some overlap, some hard-code values, and several were designed for the art-led "one subject" composition. Deciding now what to keep, revise, merge or retire avoids building new work on top of inconsistencies.

**Should Define:**

- For each component: purpose, status (keep, revise, merge, retire), issues found, and the later backlog item that will revise it.
- Known overlaps to resolve:
  - Five ways to show a number: StatTile, StatFigure, a Ledger row, Sigil and LevelPlate.
  - Four containers: Panel, Folio, Banner and Stage.
  - EntryList versus the upcoming generic Row and List.
  - Tag versus badges and counters.
  - ItemSlot, which is image-first, versus a text-first item row.
  - CurrencyPill, which covers only Cinders and Soulstones.
- A hard-coded values inventory: 19 font sizes in the component CSS against 17 type styles, and raw 1px and 2px radii.
- Where gilt is used today and for what.
- The states each component supports today, and which are missing (disabled, loading, error, locked).
- Whether KeyHints earns a permanent place in a mouse-first browser game.
- Which components fit a data-dense workbench screen and which fit only art-led screens.
- ScreenOverview and ScreenArchive reframed as worked archetype examples.

**Examples in LegendsLegacy:**
StatTile's `delta` against the upcoming DS-057 delta rules. Ledger's gilt values against DS-006. ItemSlot's `image` input against DS-023. LevelPlate's 84px numeral against DS-008's display limits.

**Paste-ready Claude Design instruction**

```text
Audit every existing component in the Grimoire Design System and add the result as an "Audit & consolidation
map" page in the Governance section. For each of the 34 components (GameShell, NavRail, TopBar, Page,
PageHeader, Stage, Banner, Folio, Panel, Ledger, StatTile, StatFigure, LevelPlate, Meter, Track, Sigil,
Constellation, Emblem, ItemSlot, ItemLink, LoadoutSlot, EntryList, TabStrip, Tag, Button, SearchField,
Presence, JourneyCard, KeyHints, Chronicle, CurrencyPill, Heading, Icon, SectionRule) and the ScreenOverview
and ScreenArchive compositions, record: its purpose in one line, a status of keep, revise, merge or retire,
the concrete issues, and which states it supports today (default, hover, focus, selected, disabled, locked,
loading, error). Specifically assess: the five ways a number is currently shown (StatTile, StatFigure, Ledger
row, Sigil, LevelPlate) and when each is justified; the four containers (Panel, Folio, Banner, Stage) and
their boundaries; whether EntryList should become a variant of a general list row; ItemSlot's image-first
design, given that equipment and Essences are currently text-first; CurrencyPill only supporting Cinders and
Soulstones when the game has more than 15 resources; whether KeyHints deserves a permanent bar in a
mouse-first browser game or should appear only where real shortcuts exist; and which components suit dense
workbench screens (Bazaar, guild members, inventory) versus art-led screens (Creature Archive). List every
hard-coded value in the component styles that is not a token: the component CSS uses about 19 distinct font
sizes against 17 type styles, and some radii are raw 1px and 2px. List every place gilt is used and what it
means there. Do not change any component in this step; produce the audit and a prioritised revision queue.
```

---

### Phase 1 — Foundations

#### DS-005 — Colour Architecture

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-001, DS-004

**Purpose:**
Today's tokens mix palette values, semantic roles and component values in one flat list (`sigil-fill` next to `ink` next to `rarity-epic`). A three-tier architecture makes it safe to tune contrast, add registries and deprecate tokens without breaking components.

**Should Define:**

- **Tier 1 – palette primitives.** Named ramps such as umber, bone, brass, verdigris, ember, amber, azure and the rarity hues. Components never reference these directly.
- **Tier 2 – semantic roles.** Ground, surface, ink, line, accent, status and focus.
- **Tier 3 – domain roles.** Rarity, damage type, chat channel, meter, condition polarity and delta, each aliased to primitives.
- **Component tokens** (such as `sigil-fill` and `tile`), explicitly marked as component-scoped.
- Naming convention, how to add a token, how to deprecate one, and how aliases work.
- A contrast table recording every text token against every ground and surface. The README currently claims 4.5:1; verify and record the actual ratios.
- The existing `--ll-*` → Grimoire migration map, kept and moved into this section.

**Examples in LegendsLegacy:**
`channel-trade` aliasing `warning`, and `meter-xp` aliasing `gilt`. These already exist informally; the architecture makes the aliasing explicit.

**Paste-ready Claude Design instruction**

```text
Restructure the colour tokens of the existing Grimoire Design System into a three-tier architecture and
document it in Foundations → Colour. Tier 1 is palette primitives: named ramps (for example umber, bone,
brass, verdigris, ember, amber, azure) plus the seven rarity hues and seven damage hues. Components must never
reference primitives directly. Tier 2 is semantic roles: ground, ground-deep, surface, surface-raised, folio,
scrim, ink, ink-muted, ink-disabled, line, line-strong, gilt, arcana, status, focus. Tier 3 is domain roles:
rarity-*, damage-*, channel-*, meter-*, plus placeholders for condition-beneficial, condition-harmful and
delta roles, to be defined later. Every Tier 2 and Tier 3 token aliases a primitive. Keep all current visible
values identical in this step; this is a restructuring, not a recolour. Mark component-scoped tokens
(sigil-fill, sigil-edge, on-sigil, tile, on-tile, on-tile-muted) as component tokens. Document the naming
convention, how to add a token, how aliases work and how to deprecate one. Build a contrast table showing
every text-capable token (ink, ink-muted, ink-disabled, gilt, arcana, the status colours, every rarity and
damage colour) on ground, surface, surface-raised and folio, with the measured ratio and pass or fail against
4.5:1 for text and 3:1 for UI edges. Flag every failure rather than hiding it. Keep the existing --ll-* →
Grimoire migration map in this section. Update tokens.json and the Colour documentation; no component should
change appearance.
```

---

#### DS-006 — Colour Channel Allocation & Collision Rules

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-005

**Purpose:**
The audit's core colour finding was one accent carrying too many meanings. Grimoire assigns gilt to labels, eyebrows, effect values, Ledger values, the level numeral, the nav marker, selection rules and the solid button. The palette also has three clusters of near-identical hues. For example, `rarity-epic` #e879f9 and `channel-whisper` #e39be2 are nearly indistinguishable, so an Epic item linked in a whisper loses its rarity signal.

**Should Define:**

- An allocation table: each hue family has one job per context.
- Gilt reduced to at most four roles. A suggested set: brand and current location, the one committing action, the screen's headline figure, and effect magnitudes inside descriptions. Ordinary data values move to `ink`.
- Arcana assigned exclusively to "ready, new or actionable" and to selection, and separated from `success` by shape and glyph (or by a hue adjustment). The two are nearly identical today.
- A collision matrix listing token pairs with their perceptual distance, and whether each pair may appear adjacent. It covers three clusters:
  - Cool: arcana, success, info, uncommon, rare, shadow, meter-sp.
  - Warm: gilt, warning, unique, legendary, burn, channel-loot, channel-trade.
  - Red and pink: danger, legacy, bleed, meter-hp, whisper, epic.
- Context ownership: in item contexts rarity owns hue, in combat contexts damage type owns hue, and in chat contexts channels own hue only on speaker names and tags. Any other information in those contexts uses shape, glyph or text.
- Rules for where each domain hue may appear. For example, rarity may colour item name text, the slot edge and the code chip, and never backgrounds, buttons or panels.
- Colour-vision-deficiency checks for the key pairs.
- An explicit rule that rarity and damage hues are player-learned and must not be changed. Other tokens move around them.

**Examples in LegendsLegacy:**
An Epic item link inside a whisper. Uncommon item names beside arcana "New" tags. Legendary names beside warning text. Bleed damage numbers beside `danger` error text.

**Paste-ready Claude Design instruction**

```text
Add colour allocation and collision rules to Foundations → Colour in the Grimoire Design System, and update
tokens and components to follow them. A previous audit of LegendsLegacy found that one accent colour carried
too many meanings. Grimoire currently uses gilt for group labels, eyebrows, effect values, Ledger values, the
level numeral, the active nav marker, selected rules and the solid button, which is the same problem again.
First, write an allocation table assigning each hue family one job per context. Reduce gilt to at most four
roles (suggested: brand and current location, the single committing action, the screen's headline figure, and
effect magnitudes inside descriptions). Move ordinary data values, including Ledger values, to ink. Make
arcana mean "ready, new, actionable or selected" and nothing else. Second, build a collision matrix for three
clusters. Cool: arcana, success, info, rarity-uncommon, rarity-rare, damage-shadow, meter-sp. Warm: gilt,
warning, rarity-unique, rarity-legendary, damage-burn, channel-loot, channel-trade. Red and pink: danger,
rarity-legacy, damage-bleed, meter-hp, channel-whisper, rarity-epic. Show each pair's perceptual distance and
whether it may appear adjacent. Note specifically that success and arcana are nearly identical, and that
channel-whisper and rarity-epic are nearly identical. Third, define context ownership: in item contexts rarity
owns hue; in combat, damage type owns hue; in chat, channels colour only speaker names and tags. Everything
else in those contexts uses glyphs, shapes or words. Rarity and damage hues are learned by players and must
not change; resolve collisions by adjusting the other tokens (for example channel-whisper, success, info) or
by requiring a glyph, and state which you chose. Include colour-vision-deficiency checks for the key pairs.
Update Ledger, Folio, Tag, NavRail and Chronicle to the new allocation and note each change in the Decision
Log.
```

---

#### DS-007 — Feedback, Delta & Effect Polarity Colours

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-005, DS-006

**Purpose:**
Comparisons, stat changes and beneficial or harmful effects are everywhere in LegendsLegacy, but Grimoire only defines the ▲▼ glyphs. "Better" depends on the stat (a lower cooldown is better), so polarity must follow benefit, not the sign of the number.

**Should Define:**

- Tokens: `delta-better`, `delta-worse`, `delta-neutral`, `effect-beneficial` and `effect-harmful`, each aliased to primitives and each paired with a glyph (▲ ▼ ◆ or + −).
- The rule that polarity follows player benefit. For example, a −1.2s cooldown change is "better".
- The difference between warning and danger. Warning is attention or a reversible risk (an expiring offer, insufficient resources). Danger is loss, destruction or failure (Pending Loot lost, abandoning a run, a failed save).
- Where success is used (a confirmed outcome) and where it is not (the "ready" state, which is arcana).
- Soft background usage limits. A soft status background belongs only on inline alerts, never on rows or list items.
- Colour-vision-deficiency safety: the glyph and sign always carry the meaning; colour reinforces it.

**Examples in LegendsLegacy:**
Equipment comparison deltas. Soulstone current → next values. Condition chips for Weaken and Empower. The insufficient-Cinders cost. The Pending Loot loss warning.

**Paste-ready Claude Design instruction**

```text
Add feedback and polarity colour roles to Foundations → Colour in the Grimoire Design System. LegendsLegacy
constantly shows stat changes (equipment comparison, Soulstone upgrades, Essence Ascension previews) and
beneficial or harmful combat effects (Empower versus Weaken, Haste versus Slow). Define Tier 3 tokens
delta-better, delta-worse, delta-neutral, effect-beneficial and effect-harmful, aliased to palette primitives
and consistent with the collision rules already in the Colour section. Establish that polarity follows player
benefit, not the sign of the number: a cooldown going from 8s to 6.8s is "better" even though the number fell.
Every delta must carry a glyph and a sign (▲ +12%, ▼ −1.2s, ◆ for unchanged) so meaning never depends on
colour. Define the semantic difference between warning (attention, reversible risk, insufficient resources,
something expiring soon) and danger (loss, destruction, failure, such as losing Pending Loot, abandoning a
dungeon run or a failed save). Define success as a confirmed outcome only, and note that "ready" or
"claimable" belongs to arcana. Limit soft status backgrounds (danger-soft and the like) to inline alerts and
never apply them to rows, list items or cards. Update StatTile's delta display and the Tag tones to use the
new roles, and show a preview with realistic LegendsLegacy examples: equipment comparison deltas, a cooldown
reduction, a harmful condition, and an insufficient-resource cost.
```

---

#### DS-008 — Typography Ramp for Data-Dense UI

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-002, DS-004

**Purpose:**
The 17 styles leave gaps: there is nothing between 17px `tab` and 36px `title-lg`, no compact body for tables, and no small tabular numeral. The component CSS has already drifted to 19 raw sizes. Large display styles (56px `title-xl`, 84px `level-numeral`) have no usage limits, which invites oversized typography.

**Should Define:**

- A complete ramp organised by role:
  - Display: hero numeral, `title-xl`, `title-lg`.
  - Section titles: roughly 24px and 20px.
  - Entity names: item, creature and player names in headers and in rows.
  - Body: `body`, and a new `body-compact` of 13–14px for dense tables.
  - Labels and captions.
  - Numerals: headline, stat, row and compact sizes.
- A minimum size of 12px for anything a player reads. Only rarity codes and key caps may go to 11px, and only in bold caps.
- Display-face rules: Marcellus only at 15px and above, never for numbers in tables. Evaluate whether buttons and dense list items should stay in Marcellus or move to Barlow, and record the decision.
- Uppercase rules: tracked caps only for labels of three words or fewer, never for sentences.
- Usage limits: one `title-xl` or `level-numeral` per screen, and never inside a list.
- Line-height rules for dense rows compared with prose.
- A mapping of all 19 hard-coded sizes onto ramp styles.

**Examples in LegendsLegacy:**
Section titles such as "Combat Attributes" and "Attuned Essences". Item names in inventory rows. Bazaar order-book numbers. Guild member rows. The Character Overview's single headline Combat Rating.

**Paste-ready Claude Design instruction**

```text
Extend the typography system of the existing Grimoire Design System so it supports a data-dense RPG interface,
and document it in Foundations → Typography. Keep the four families and their jobs (Marcellus display, EB
Garamond lore, Barlow UI, Barlow Condensed numerals, Atkinson Hyperlegible for the readable setting). The
current 17 styles have gaps. Nothing sits between the 17px tab style and the 36px title-lg for section titles.
There is no compact body style for tables and dense lists. There is no small tabular numeral for table cells.
The component CSS already uses about 19 hard-coded font sizes. Define a complete ramp organised by role.
Display: hero numeral, title-xl, title-lg. Section titles: add two sizes, roughly 24px and 20px. Entity names:
one size for names in headers and one for names in rows, for items, creatures, Essences and players. Body:
body plus a new body-compact at 13–14px for tables. Labels and captions. Numerals: headline, stat, row and
compact. Set a minimum of 12px for anything a player reads; only rarity codes and key caps may use 11px, in
bold caps. Restrict Marcellus to 15px and larger and never use it for numbers in tables. Evaluate whether
Button labels and EntryList items should stay in Marcellus or move to Barlow for legibility at small sizes,
and record the choice in the Decision Log. Allow tracked uppercase only for labels of three words or fewer.
Limit title-xl and level-numeral to one per screen and never inside a list. Give line heights for dense rows
and for prose separately. Then map every hard-coded font size in the component styles onto a ramp style and
update the components so no raw sizes remain. Show a specimen with real LegendsLegacy content: section titles,
item names, an attribute table, a Bazaar order-book row and a lore line.
```

---

#### DS-009 — Numeric Typography & Alignment

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-008

**Purpose:**
Numbers are the core content of LegendsLegacy. Players compare them across rows, across screens and against their memory. The system needs exact rules for figure style, alignment, units, signs and unknown values.

**Should Define:**

- Tabular lining figures wherever numbers align, stack or update live. Proportional figures only for isolated headline numbers.
- Right alignment for numeric columns, decimal alignment for mixed precision, and units set smaller or muted immediately after the number.
- Characters: a true minus sign (U+2212), an en dash for ranges (12–18), a multiplication sign for multipliers (×1.5), and fractions with spaces around the slash ("3,120 / 4,150").
- Placeholders: an em dash for "not applicable or unknown", "0" for zero, and never an empty cell.
- The rule that Marcellus numerals appear only in the hero and level numerals.
- How live-updating numbers avoid layout shift.
- The order of number and label ("12,480 Cinders" versus "Cinders 12,480") in each context.

**Examples in LegendsLegacy:**
Ledger attribute values. Bazaar prices and quantities. Arena ratings in rankings. Damage numbers in the combat log. Currency amounts in the TopBar.

**Paste-ready Claude Design instruction**

```text
Add a Numerals page to Foundations in the Grimoire Design System, and update every component that displays
numbers to follow it. LegendsLegacy is mostly numbers: attributes, prices, quantities, ratings, damage,
cooldowns and progress. Define the rules. Use tabular lining figures (font-variant-numeric: tabular-nums
lining-nums) wherever numbers align in columns, stack in lists or update live; use proportional figures only
for a single isolated headline number. Right-align numeric columns, decimal-align mixed precision, and place
units immediately after the number in a smaller or muted style. Use a true minus sign (U+2212), an en dash for
ranges (12–18), × for multipliers (×1.5), and a spaced slash for fractions (3,120 / 4,150). Use an em dash (—)
for unknown or not-applicable values, "0" for zero, and never leave a value cell empty. Restrict Marcellus
numerals to the hero numeral and LevelPlate; everything else uses Barlow Condensed or Barlow with tabular
figures. Explain how numbers that update live (combat, auctions, currency) avoid layout shift, for example by
reserving width. Specify the number-and-label order for each context: TopBar, cost lists, tables, tooltips and
Ledgers. Update Ledger, StatTile, StatFigure, Meter, CurrencyPill and LevelPlate accordingly. Show a preview
with a column of attribute values of mixed precision (1,284; 24.8%; 84 HP/5s; 184.6 threat/s), a Bazaar price
column, a delta column and a live-updating counter.
```

---

#### DS-010 — Spacing Scale & Density Modes

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-004, DS-008

**Purpose:**
Grimoire's spacing rules (`space-6` panel padding, `space-8` between Folio blocks) suit spacious detail screens. The audit found that applying the same padding at every nesting level wastes width, and the most-used screens (inventory, rankings, member lists, order books, logs) need to be much denser than the Folio.

**Should Define:**

- Three density modes:
  - **Comfortable:** identity and detail views, the Folio, dialogs.
  - **Standard:** most panels and forms.
  - **Compact:** tables, inventory lists, rankings, members, logs, order books.
- For each mode: row height, cell padding, gap, text role (`body` or `body-compact`), icon size and control size.
- Semantic spacing tokens: inset, stack, inline and section gaps, aliased to the 4px scale.
- The rule that nested containers never both apply full padding. Inner padding steps down one level.
- Which archetypes and components default to which density.
- Whether the player can choose a density (for example "Compact lists" in Settings), and how that is scoped.

**Examples in LegendsLegacy:**
Inventory rows and guild member rows in Compact. The Character Overview in Standard. The Folio and the Nobility redemption dialog in Comfortable.

**Paste-ready Claude Design instruction**

```text
Add a Space & Density page to Foundations in the Grimoire Design System. Keep the existing 4px spacing scale,
but add semantic spacing tokens (inset, stack, inline and section gaps, each aliased to the scale) and three
density modes. Comfortable is for identity and detail views, the Folio and dialogs. Standard is the default
for panels and forms. Compact is for tables, inventory lists, rankings, guild members, combat logs and Bazaar
order books. For each mode define row height, cell padding, gaps between elements, which text style it uses
(body or body-compact), icon size and control height. Add the rule that nested containers never both apply
full padding: an inner surface steps down one level, and a list inside a Panel uses the Panel's padding
instead of adding its own. Specify which components support which densities (Ledger, EntryList, TabStrip and
Button at minimum) and which density each future page archetype defaults to. Recommend whether players should
get a "Compact lists" preference in Settings and how it would apply. LegendsLegacy players spend hours on
inventory, rankings and trading screens, so Compact must feel deliberate and legible rather than cramped: keep
12px as the minimum text size and keep focus rings intact. Show a preview comparing the same inventory list
and the same attribute Ledger in all three densities, and update affected existing components.
```

---

#### DS-011 — Accessibility Baseline & Text Scaling

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-005, DS-008, DS-010

**Purpose:**
The game already has a readable-font setting, text-size preferences, reduced motion, focus rings and focus-trapped dialogs, and these must not regress. Grimoire expresses type and space in px, which does not follow the game's reading-size preference. That needs a decision before more components are built.

**Should Define:**

- The conformance target (WCAG 2.2 AA is recommended) and what that means for text contrast, UI contrast, target size, focus and keyboard use.
- Text scaling:
  - Convert type, spacing and layout tokens to rem so the reading-size preference scales the whole interface.
  - Define tested scale steps, for example 100%, 115% and 130%.
  - Define what reflows and what scrolls at each step.
- The keyboard model: tab order, roving tabindex for lists, tabs and grids, arrow-key rules and Escape order.
- Target sizes: at least 24×24px, with 32px recommended for primary actions.
- No information that is available only on hover. Hover cards must also open on focus, and pin on tap.
- Live-region rules: which updates are announced (errors, claim results, a completed dungeon) and which are not (every combat tick). Announcements are throttled.
- Accessible names: icon-only buttons need them, and abbreviated numbers are read in full ("12.5k" is announced as "12,480 Cinders").
- Colour independence, restated as a system rule.
- Reduced-motion behaviour.

**Examples in LegendsLegacy:**
Combat playback must not flood screen readers. A rarity is read as "Epic". The readable font applied at the largest size across the Character Overview.

**Paste-ready Claude Design instruction**

```text
Add an Accessibility page to Foundations in the Grimoire Design System and apply its rules to the tokens.
LegendsLegacy already has a readable-font setting (Atkinson Hyperlegible), text-size preferences,
reduced-motion handling, visible focus rings and focus-trapped dialogs; none of these may regress. Set WCAG
2.2 AA as the target and state what that means for text contrast, UI edge contrast, target size, focus
visibility and keyboard operation. Text scaling is the most important decision: Grimoire's type, spacing and
layout tokens are in px, so they ignore the game's reading-size preference. Convert them to rem, or define an
equivalent scale mechanism, so the whole interface scales, and define tested scale steps (100%, 115%, 130%)
with what reflows and what scrolls at each. Define the keyboard model: tab order, roving tabindex for lists,
tabs and grids, arrow-key behaviour, and Escape order for nested overlays. Define minimum target sizes:
24×24px, with 32px recommended for primary actions. State that no information may exist only on hover: hover
cards must open on keyboard focus and pin on tap. Define live-region rules for a realtime game: announce
errors, claim results and completed activities; never announce every combat event; throttle announcements.
Require accessible names for icon-only controls, and require that abbreviated numbers are announced in full
(12.5k is read as "12,480 Cinders"). Restate that colour never carries meaning alone and that rarity is
announced by name. Update tokens.json and add an accessibility notes field to the component README template.
```

---

#### DS-012 — Layout Grid, Content Widths & Container Breakpoints

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-010, DS-011

**Purpose:**
The stage is much narrower than the viewport. At 1920px, the rail (224), Folio (360) and docked Chronicle (384) leave roughly 950px. Layout rules must be written against real content width, using container queries rather than viewport breakpoints, or dense screens will clip and wide screens will look sparse.

**Should Define:**

- A table of stage widths at 1280, 1440, 1536, 1920 and 2560px, with and without the Folio, and with the Chronicle docked, collapsed or floating.
- Content-level container breakpoints, such as wide, medium, narrow and stacked, with layout behaviour at each.
- Standard track layouts:
  - List plus inspector, with ratios.
  - Two-column and three-column ledger grids.
  - A full-width table.
  - A split comparison view.
- Maximum reading width for prose (about 68ch), and when content may go full-bleed.
- Minimum table widths, and when horizontal scrolling is acceptable. Priority columns hide first.
- Alignment rules: shared left edges across sections and a baseline rhythm.
- The rule that everything reflows when the reading size increases.

**Examples in LegendsLegacy:**
Inventory switches from three panes to two to one. The Bazaar order book beside the item. Tower list and detail. The attribute Ledger grid (four columns, then two, then one).

**Paste-ready Claude Design instruction**

```text
Add a Layout page to Foundations in the Grimoire Design System. The key fact is that the stage is much
narrower than the screen. At 1920px, the NavRail (rail-width), Folio (folio-width) and docked Chronicle
(chronicle-width) leave roughly 950px for content, so layouts must respond to available container width, not
viewport width. First, produce a table of stage widths at 1280, 1440, 1536, 1920 and 2560px viewports, with
and without a Folio, and with the Chronicle docked open, docked collapsed and floating. Second, define
content-level container breakpoints (for example wide, medium, narrow and stacked) and the behaviour of each
standard track layout at each: list plus inspector with recommended ratios; two-column and three-column Ledger
grids; a full-width data table; a split comparison view. Third, set a maximum reading width for prose (about
68ch), say when content may go full-bleed, give minimum widths for tables, and say when horizontal scrolling
is acceptable and which columns hide first. Fourth, define alignment rules: section left edges align, and a
baseline rhythm applies across stacked sections. Everything must reflow correctly at the larger reading sizes
from the Accessibility page. Update the existing lg-ledgergrid and lg-statgrid rules to use these container
breakpoints. Show previews at three container widths using a realistic inventory list with inspector and the
Character Overview attribute grid.
```

---

#### DS-013 — Surfaces, Elevation & Layering

**Category:** Foundation
**Priority:** High
**Depends On:** DS-005, DS-010

**Purpose:**
Grimoire has five grounds and two shadows but no elevation model, and z-index tokens with no stacking rules. The audit found that nested translucent surfaces became one low-contrast mass. The game also stacks many layers: popovers opened from modals, toasts, the tour overlay, drag previews and the floating chat.

**Should Define:**

- Elevation levels:
  - 0: ground, the page.
  - 1: surface, for rails, lists and the chat drawer.
  - 2: raised, for hover and popovers.
  - 3: folio, for detail panels and modals.
  - Overlay: the scrim.
- The token for each level: fill, shadow and edge.
- The nesting rule: a surface placed on a surface steps up at most one level, and there are at most two enclosed levels inside the stage.
- Surfaces are opaque, with no backdrop blur. Texture and grain are allowed only on Stage, Folio and Banner.
- A layering stack with z-tokens: content, sticky headers, Folio, floating Chronicle, popovers, modals, detached popovers above modals, toasts, the tour overlay, and drag previews.
- Modal stack rules: one modal at a time, how a confirmation opens from a dialog, Escape order, and focus return.

**Examples in LegendsLegacy:**
An item hover card opened from the Vault modal. The Nobility redemption confirmation. Floating chat over the dungeon route. The First Steps tour spotlight.

**Paste-ready Claude Design instruction**

```text
Add a Surfaces & Layering page to Foundations in the Grimoire Design System. Define an elevation model using
the existing tokens. Level 0 is ground (the page). Level 1 is surface (rail, lists, chat drawer). Level 2 is
surface-raised (hover, popovers). Level 3 is folio (detail panel, modals). Above those sits scrim. Specify
each level's fill, shadow and edge. Add the nesting rule: a surface placed on another steps up at most one
level, and no more than two enclosed levels may exist inside the stage. Surfaces are opaque; do not use
backdrop blur. Film grain and texture are reserved for Stage, Folio and Banner. Then define the layering stack
using the z tokens: content, sticky table headers, Folio, floating Chronicle drawer, popovers, modals,
detached popovers (which sit above the modal they were opened from), toasts, the guided-tour overlay, and drag
previews. Add any z tokens that are missing. Define modal stack rules: only one modal at a time; a
confirmation opened from a dialog replaces or overlays it in a defined way; Escape closes the topmost layer
first; focus returns to the element that opened the layer. LegendsLegacy examples to cover: an item hover card
opened inside the Guild Vault dialog, a confirmation opened from the Nobility redemption dialog, the floating
chat over the dungeon route, and the First Steps tour spotlight over the NavRail. Update Folio, Panel and
Chronicle to reference the elevation levels and show a layered preview.
```

---

#### DS-014 — Lines, Dividers & Borders

**Category:** Foundation
**Priority:** High
**Depends On:** DS-005, DS-013

**Purpose:**
The audit found thin frames used everywhere, so a line could mean grouping, interaction or selection. Grimoire already separates `line` (decorative) from `line-strong` (functional). It still needs rules for when a boundary is justified at all.

**Should Define:**

- The meaning of each line type: separation, interactive edge, selection edge, decorative ornament and dotted leader.
- A decision rule for choosing between spacing, a divider and a border. The default is spacing, then a divider; a border only for interactive edges or for bounded objects.
- Border width tokens: a hairline, and an emphasis width.
- Row separators in dense lists, and the rule that a list never uses both row borders and a container border.
- Usage limits for SectionRule variants (band, ornament, hairline). The ornament variant appears at most once per surface.
- The double gilt frame, reserved for Folio and Banner.
- Dotted leaders only in Ledger-style label and value rows.

**Examples in LegendsLegacy:**
Guild member table rows, inventory rows, Folio lore-to-effects separation, input edges and the selected tab.

**Paste-ready Claude Design instruction**

```text
Add a Lines page to Foundations in the Grimoire Design System. LegendsLegacy's current UI draws thin frames
around nearly everything, so a line can mean grouping, interaction or selection. Grimoire already separates
line (decorative) from line-strong (functional, 3:1). Build on that with an explicit meaning for each line
type: separation, interactive edge, selection edge, decorative ornament and dotted leader. Add a decision
rule: use spacing first, a divider second, and a full border only for interactive edges (inputs, outlined
buttons) or bounded objects (an item slot, an opponent card). Add border-width tokens for a hairline and an
emphasis width. For dense lists and tables, use row separators or zebra rhythm, never both, and never combine
row borders with a container border. Limit the SectionRule variants (band, ornament, hairline): the ornament
variant appears at most once per surface. Reserve the double gilt frame for Folio and Banner. Restrict dotted
leaders to Ledger-style label and value rows. Include do/don't examples using a guild member list, an
inventory list, a Folio and a form. Update SectionRule, Panel and Ledger documentation to reference these
rules.
```

---

#### DS-015 — Shape Language & Radius

**Category:** Foundation
**Priority:** High
**Depends On:** DS-003, DS-013

**Purpose:**
Grimoire already uses shapes semantically: hexagons for Sigils, diamonds for Track milestones and the nav marker, and squares for item slots. Writing that down turns shape into a reliable signal. Radius needs review too: pill buttons, pill currency and pill meters sit uneasily with "square and engraved".

**Should Define:**

- A shape vocabulary:
  - Hexagon: a sigil, meaning a stat or mastery value.
  - Diamond: a milestone, a ready marker or the current location.
  - Square: an item or slot.
  - Circle: presence or a portrait (to be decided).
  - Rectangle: containers.
- The rule that a shape keeps its meaning everywhere. A diamond is never a decorative bullet in a different sense.
- Radius tokens by role: containers 0–2px, controls, tags and slots, popovers.
- A decision on buttons: keep the pill, or move to an engraved rectangle with a small radius or a chamfer. Evaluate against DS-003 and record the result.
- Whether the ✦ list marker conflicts with the diamond meaning.

**Examples in LegendsLegacy:**
Soulstone Constellation hexes, Tower floor diamonds, ready markers on NavRail items, equipment slots and the Arena "Challenge" button.

**Paste-ready Claude Design instruction**

```text
Add a Shape page to Foundations in the Grimoire Design System. Grimoire already uses shapes with meaning:
hexagons for Sigils, diamonds for Track milestones and the active nav marker, squares for item slots. Make
this an explicit vocabulary. Hexagon means a sigil (a stat, mastery or constellation value). Diamond means a
milestone, a ready or claimable marker, or the current location. Square means an item or equipment slot.
Circle should be reserved for presence or portraits; propose which. Rectangles are containers. State that a
shape keeps its meaning everywhere; for example a diamond is never used as a decorative bullet with a
different meaning. Assess whether the ✦ list marker conflicts with the diamond meaning. Then review radius.
The README says "square and engraved; rounding only where the hand touches", but buttons, currency pills and
meters are full pills, which risks the generic rounded-SaaS look the guardrails warn against. Evaluate two
options for Button: keep the pill, or move to an engraved rectangle with a small radius or a subtle chamfer.
Show both side by side in a dense LegendsLegacy context (an Arena opponent row with a Challenge button, a
Folio with one solid action, a dialog footer). Recommend one and record the decision in the Decision Log.
Consolidate radius tokens by role: containers 0–2px; controls, tags and slots; popovers and drawers. Replace
the raw 1px and 2px radii in the component styles with tokens.
```

---

#### DS-016 — Ornament, Texture & Glow Budget

**Category:** Foundation
**Priority:** High
**Depends On:** DS-003, DS-013, DS-015

**Purpose:**
Grimoire's decorative devices (CornerOrnament, Divider, diamond-chain rule, double gilt frame, film grain, vignette, `glow-selected`, `glow-gilt`, `shadow-text-art`) give it identity. The same devices become noise when every panel uses them. A budget keeps them special.

**Should Define:**

- An inventory of every decorative device, with its purpose and where it is allowed.
- A per-screen budget: for example, one ornamented framed surface (a Folio or a Banner), at most two ornament rules, and film grain only on art surfaces.
- Forbidden zones: tables, lists, inputs, toasts, menus, dense panels and dialogs other than the major commitment dialogs.
- Glow rules: allowed only for selected, ready, focus and primary hover; never animated in a loop; never as decoration or to signal rarity.
- Texture rules: no parchment, and texture never behind body text without a contrast surface.
- A way to audit a screen against the budget.

**Examples in LegendsLegacy:**
The Creature Archive Folio uses its one ornament allowance. The guild member list uses none. A Legendary item's rarity is carried by the edge and the code, not a glow.

**Paste-ready Claude Design instruction**

```text
Add an Ornament page to Foundations in the Grimoire Design System that sets a decoration budget. First,
inventory every decorative device in the system: CornerOrnament, Divider, the diamond-chain SectionRule, the
double gilt inset frame, film grain, vignette, glow-selected, glow-gilt and shadow-text-art. For each, state
its purpose and where it is allowed. Then set a per-screen budget: at most one ornamented framed surface (a
Folio or a Banner, not both), at most two ornament rules, and film grain only on art-bearing surfaces. List
the forbidden zones: tables, lists, inputs, toasts, menus, dense Panels, and dialogs other than major
commitment dialogs. Define glow rules: glow is allowed only for selected, ready, keyboard focus and
primary-button hover; it never loops, never decorates, and never signals rarity (rarity is carried by the slot
edge, the name colour and the rarity code). Define texture rules: no parchment or skeuomorphic materials, and
no texture directly behind body text without a contrast surface. Add a quick audit method: count the devices
on a screen and compare against the budget. Update Folio, Banner, SectionRule, Button and ItemSlot
documentation with their limits, and show one compliant screen and one over-decorated counterexample using the
Creature Archive.
```

---

#### DS-017 — Motion Principles & Tokens

**Category:** Foundation
**Priority:** High
**Depends On:** DS-002, DS-011

**Purpose:**
Grimoire has one line on motion ("quiet: 140–220ms… no bouncing"). A realtime game needs more: values that change live, rewards that arrive, combat playback, drawers and reduced-motion alternatives. Without shared tokens, each feature will invent its own timing.

**Should Define:**

- Duration tokens (for example instant, fast 140ms, base 220ms, slow 400ms, reveal) and easing tokens (standard, enter, exit).
- Motion categories: feedback, state change, value change, spatial, reveal and live-update highlight. Each has its tokens and limits.
- A live-update rule: a changed value may highlight briefly without layout shift, and a list refresh never moves the element the player is about to click.
- A reduced-motion alternative for every category.
- A looping rule: only indeterminate progress and live combat playback may loop.
- Performance: animate only transform and opacity.

**Examples in LegendsLegacy:**
The Meter fill after combat, a Cinders count updating, a Bazaar row refreshing, the NavRail drawer, loot arriving in the Chronicle, and the combat viewer.

**Paste-ready Claude Design instruction**

```text
Add a Motion page to Foundations in the Grimoire Design System and add motion tokens to tokens.json. Keep
Grimoire's existing character (quiet, 140–220ms, no bouncing, no looping), but make it systematic for a
realtime browser RPG. Define duration tokens (instant, fast about 140ms, base about 220ms, slow about 400ms,
and one reveal duration) and easing tokens (standard, enter, exit). Define motion categories with rules and
tokens for each. Feedback: hover and press. State change: select and toggle. Value change: meter fill, and a
number updating after combat or a purchase. Spatial: drawers, the rail drawer, the Chronicle drawer. Reveal: a
reward or result appearing. Live-update highlight: a changed value briefly marked. Add the rule that live
updates never cause layout shift and never move the control the player is about to click; a refreshed Bazaar
list or guild roster keeps the selection and scroll position. For each category, specify the reduced-motion
alternative (usually an instant change with an optional brief colour change). Only indeterminate progress
indicators and live combat playback may loop. Animate only transform and opacity. Update Meter, NavRail,
Chronicle and Button to reference the tokens.
```

---

#### DS-018 — Iconography Foundations & Taxonomy

**Category:** Foundation
**Priority:** High
**Depends On:** DS-011, DS-015

**Purpose:**
Grimoire has 15 navigation icons drawn in `currentColor`, plus full-colour currency art. The game needs icons for actions, attributes, resources, conditions, damage types, equipment slots and statuses, all following the same rules. Otherwise mixed Material glyphs and bespoke SVGs will return.

**Should Define:**

- The construction grid (24 units, 1.6 stroke, round caps) and the sizes (16, 20 and 24, plus 12 for inline markers only), with optical rules for small sizes.
- Categories:
  - Navigation (existing).
  - Action: equip, unequip, sell, buy, claim, lock, favourite, filter, sort, refresh, close, back, expand, copy, link, whisper, invite.
  - Attribute and stat.
  - Resource: a line icon for inline use and full-colour art for large display.
  - Condition: beneficial and harmful framing.
  - Damage type.
  - Equipment slot (8).
  - Entity type.
  - Status: success, warning, danger, info.
  - Social: whisper, guild, mention, party.
- The icon-and-label rule: icons accompany labels, and an icon alone is allowed only for common actions, always with a tooltip and an accessible name.
- Colour rules: `currentColor`, no rarity-coloured icons, no multicolour line icons.
- The rule that every registry entry has an icon slot, with a fallback when no icon exists yet.
- An inventory of which icons exist and which are needed. This feeds DS-127.

**Examples in LegendsLegacy:**
Line versions of Cinders and Soulstones for dense cost lists. Condition icons for Bleed and Stun. Slot icons for empty equipment slots such as "Off-hand". A favourite icon for protected items.

**Paste-ready Claude Design instruction**

```text
Expand the Iconography page in Foundations of the Grimoire Design System. The existing Icon component draws
the 15 sidebar icons in currentColor on a 24-unit grid with 1.6 stroke and round caps; keep that as the
construction standard for every future icon. Add the size scale (16, 20, 24, plus 12 for inline markers only)
with optical adjustments for small sizes. Define the icon taxonomy LegendsLegacy needs. Navigation (existing).
Action: equip, unequip, sell, buy, claim, lock, unlock, favourite, filter, sort, refresh, close, back, expand,
copy, link, whisper, invite. Attribute and stat. Resource: decide on a line version for inline use in cost
lists, alongside the existing full-colour Coins and Diamonds art for large display. Condition, framed
differently for beneficial and harmful. Damage type. The eight equipment slots. Entity type. Status (success,
warning, danger, info). Social (whisper, guild, mention, party). State the icon-and-label rule: icons
accompany labels; an icon alone is allowed only for common actions and always has a tooltip and an accessible
name. State the colour rules: currentColor only, no rarity-coloured icons, no multicolour line icons. Require
that every registry entry (resources, attributes, conditions, damage types, slots) has an icon slot with a
documented fallback. Finish with an inventory table showing which icons exist today and which are needed,
grouped by category and priority. Do not draw the new icons yet; that is a separate task.
```

---

#### DS-019 — State Model

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-004, DS-006, DS-007, DS-011

**Purpose:**
Grimoire defines five states (hover, selected, ready, locked, focus). LegendsLegacy needs about thirty, and many look similar but mean different things. Disabled, locked, unavailable and insufficient differ. Completed, claimable and claimed differ. Owned, equipped, assigned and captured differ. This is the most important cross-cutting standard in the system.

**Should Define:**

- State families:
  - **Interaction:** default, hover, focus-visible, pressed, selected, current, dragging, disabled.
  - **Availability:** available, unavailable (with a reason), locked (progression gate), restricted (account or permission), insufficient resources, on cooldown.
  - **Lifecycle:** new, unread, in progress, completed, claimable, claimed, opened, expiring, expired, failed.
  - **Ownership and use:** owned, unowned, equipped, attuned, assigned to a preset or activity, captured in a snapshot, listed on the market, reserved or in escrow, borrowed from the guild, favourite or protected.
  - **Knowledge:** discovered, undiscovered, hidden (unrevealed), unknown.
  - **Data:** loading, refreshing, pending save, stale, error, empty, offline or reconnecting.
- For each state: its meaning, its visual channel (edge, fill, marker, text, icon, opacity or typeface), its token, the words shown, and the accessible announcement.
- The key distinctions:
  - Prefer "unavailable with a reason" over "disabled".
  - A locked item always states its unlock condition.
  - Insufficient resources shows what is missing.
- Which component categories support which states.

**Examples in LegendsLegacy:**
An insufficient-Cinders purchase button. A locked fourth Essence slot ("Unlocks at level 20"). A captured Arena defence build. A claimable Prophecy cache. An unrevealed dungeon room. A borrowed Vault item. A stale ranking after a failed refresh.

**Paste-ready Claude Design instruction**

```text
Add a States page to Standards in the Grimoire Design System. This is the most important cross-cutting
standard, so be thorough. Grimoire currently defines hover, selected, ready, locked and focus; LegendsLegacy
needs a full state model. Organise states into families. Interaction: default, hover, focus-visible, pressed,
selected, current, dragging, disabled. Availability: available, unavailable (always with a reason), locked (a
progression gate, always with its unlock condition), restricted (account or guild permission), insufficient
resources (always showing what is missing), on cooldown. Lifecycle: new, unread, in progress, completed,
claimable, claimed, opened, expiring soon, expired, failed. Ownership and use: owned, unowned, equipped,
attuned, assigned to a preset or activity, captured in an activity snapshot, listed on the market, reserved or
in escrow, borrowed from the guild Vault, favourite or protected. Knowledge: discovered, undiscovered, hidden
(for example unrevealed dungeon rooms), unknown. Data: loading, refreshing, pending save, stale, error, empty,
offline or reconnecting. For each state define: meaning in one sentence; the visual channel it uses (edge,
fill, marker, text, icon, opacity or typeface); the tokens; the exact words shown; and the screen-reader
announcement. Make the easily confused distinctions explicit: disabled versus unavailable versus locked versus
insufficient; completed versus claimable versus claimed; owned versus equipped versus assigned versus
captured. Prefer "unavailable with a reason" over plain disabled, which should be rare. Add a matrix showing
which component categories support which states. Then update Button, Tag, ItemSlot, LoadoutSlot, EntryList,
NavRail and Sigil so their existing states match this model.
```

---

#### DS-020 — State Combination & Priority Rules

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-019

**Purpose:**
Game objects often carry several states at once: an Epic item that is equipped and has an upgrade available, or a locked reward that is also new. Without rules, each state adds a badge, a glow or a colour, and the interface becomes badge soup. This item assigns channels and a precedence order.

**Should Define:**

- Channel ownership: each channel holds one state at a time.
  - **Identity** (rarity edge, name colour, code): who the object is.
  - **Selection** (arcana ring or bar).
  - **Ownership** (a fixed corner marker, or an "Equipped" word in rows).
  - **Attention** (a single diamond marker, top-right).
  - **Availability** (dimming, a lock and reason text).
  - **Data** (skeleton, or stale text).
- The maximum number visible at once: identity, selection, one ownership marker and one attention marker. Everything else moves to the hover card or the detail view.
- Precedence when space is tight: availability, then selection, then attention, then ownership, then "new".
- Fixed marker positions on ItemSlot, ItemRow, LoadoutSlot and NavRail items.
- Worked examples:
  - Epic, equipped and upgrade available.
  - Locked and new.
  - Claimable and expiring soon.
  - Selected and insufficient.
  - Undiscovered and focused (Creature Focus).
  - Listed and favourite.
  - Captured build with a pending change.
- Don'ts: three tags on one row, a glow plus a ring plus a badge, or colour changes for several states at once.

**Examples in LegendsLegacy:**
Inventory rows. The equipment grid on the Character Overview. The Essence Archive list. NavRail items with a quest attention marker and a locked state.

**Paste-ready Claude Design instruction**

```text
Add a "State combinations" page to Standards in the Grimoire Design System, building on the States page. Game
objects in LegendsLegacy often carry several states at once. The rules must stop the interface becoming badge
soup. Assign each visual channel one state at a time. Identity: rarity edge, name colour and rarity code,
saying what the object is. Selection: the arcana ring or bar. Ownership: one fixed corner marker on slots, or
a single word such as "Equipped" in rows. Attention: one diamond marker in a fixed top-right position.
Availability: dimming plus a lock icon plus reason text. Data: a skeleton or stale-data text. Set the maximum
visible at once: identity, selection, one ownership marker and one attention marker; everything else moves
into the hover card or detail view. Define the precedence order when space is limited: availability, then
selection, then attention, then ownership, then "new". Specify exact marker positions for ItemSlot,
LoadoutSlot, EntryList rows and NavRail items, and update those components. Include worked examples, each
showing the resolved visual and what moved into the hover card: an Epic item that is equipped and has an
upgrade available; a locked reward that is new; a claimable Prophecy cache that expires soon; a selected item
the player cannot afford; an undiscovered creature that is the current Creature Focus; a market-listed item
that is also a favourite; a captured Arena defence build that differs from the live build. Add a don't list:
no more than one Tag per row by default, never a glow plus a ring plus a badge on the same object, never a
colour change for more than one state at once.
```

---

#### DS-021 — Information Hierarchy Levels

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-008, DS-009, DS-019

**Purpose:**
The game's screens mix decisions, headline values, names, labels, metadata, mechanical descriptions, lore, warnings and requirements. The type ramp says what sizes exist. This item says which kind of information gets which treatment, so screens do not become walls of equally weighted text and numbers.

**Should Define:**

- Named levels:
  - L1 Decision: the action or choice, one per region.
  - L2 Headline value.
  - L3 Entity name.
  - L4 Primary value.
  - L5 Label.
  - L6 Metadata and secondary values.
  - L7 Mechanical description.
  - L8 Lore.
  - Two interrupting levels: Warning and Requirement.
- For each level: type style, colour role, typical position, and how many are allowed per region.
- Rules:
  - Labels are quieter than values.
  - Units are quieter than numbers.
  - Mechanics come before lore, never in the same line.
  - Metadata trails or moves to a tooltip.
  - Requirements sit next to the action they gate.
  - Warnings sit next to the commitment they qualify.
- Chunking rules: groups of at most about seven rows, aligned columns, and a summary before the detail.
- A "wall test" that flags a region with more than N same-weight items and no grouping.

**Examples in LegendsLegacy:**
A Folio for a creature. A Soulstone upgrade (next effect, cost, requirement). A raid party slot. A Bazaar listing with price, fee and net.

**Paste-ready Claude Design instruction**

```text
Add an Information Hierarchy page to Standards in the Grimoire Design System. LegendsLegacy screens combine
decisions, headline figures, entity names, stat values, labels, metadata, mechanical descriptions, lore,
warnings and requirements. Without explicit levels they turn into walls of equally weighted text and numbers.
Define these levels. L1 Decision: the action or choice, one per region. L2 Headline value: at most one per
screen or panel. L3 Entity name. L4 Primary value. L5 Label. L6 Metadata and secondary values. L7 Mechanical
description. L8 Lore. Add two interrupting levels: Warning and Requirement. For each level, specify the type
style from the Typography ramp, the colour role from the allocation table, its typical position, and how many
are allowed per region. Add the rules: labels are quieter than values; units are quieter than numbers;
mechanical text comes before lore and never shares a line with it; metadata trails the row or moves to a
tooltip; a requirement sits directly next to the action it gates; a warning sits next to the commitment it
qualifies. Add chunking rules: groups of about seven rows or fewer, aligned value columns, summary before
detail. Add a simple "wall test" for reviewing screens. Illustrate each rule with LegendsLegacy examples: a
creature Folio, a Soulstone upgrade showing the next effect, cost and requirement, a raid party slot, and a
Bazaar listing with price, fee and net proceeds. Update Folio, Ledger and PageHeader documentation to
reference the levels.
```

---

#### DS-022 — Progressive Disclosure Model

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-021

**Purpose:**
Showing every stat, every ability event and every completed collection member at once is not necessary for informed action. Hiding costs, loss risk or captured-build state is harmful. The system needs defined disclosure layers and a list of facts that must never be hidden.

**Should Define:**

- The layers:
  - **Glance** (row or slot): name, rarity, one or two key values, one state marker.
  - **Hover card:** the full stat set, deltas against equipped, and short mechanics.
  - **Detail** (Folio or inspector): everything plus actions.
  - **Deep** (a breakdown dialog or page): sources, history and full logs.
- For each layer: what belongs there, how it is triggered (hover, focus, tap to pin, click), and how it closes.
- A never-hide list: total cost, fees and net; irreversible consequences; loss risk; the eligibility or restriction reason; captured versus live build; expiry; ownership and binding restrictions.
- Rules for expandable rows, "Show more" and collapsed sections, including when state persists.
- Keyboard and touch access to every layer.

**Examples in LegendsLegacy:**
An inventory row, then the item hover card, then the inspector, then the stat source breakdown. A combat result summary, then contributions, then the full event log.

**Paste-ready Claude Design instruction**

```text
Add a Progressive Disclosure page to Standards in the Grimoire Design System. Define four disclosure layers
for LegendsLegacy. Glance, a row or slot: name, rarity, one or two key values, one state marker. Hover card:
the full stat set, deltas against what is equipped, short mechanics. Detail, the Folio or an inspector panel:
everything plus actions. Deep, a breakdown dialog or page: stat sources, history, full combat logs. For each
layer, specify what belongs there, how it opens (hover after a delay, keyboard focus, tap to pin, click), how
it closes, and whether its state persists. Add a never-hide list of facts that must appear at the glance or
detail layer where the decision is made and must never be tooltip-only: total cost, fees and net proceeds;
irreversible consequences (a guild donation, selling, abandoning a run); loss risk (Pending Loot on dungeon
failure); the reason an action is unavailable or restricted; whether a build is live or a captured snapshot;
expiry times; ownership and binding restrictions. Add rules for expandable rows, "Show more" and collapsible
sections. Make sure every layer is reachable by keyboard and touch. Show one worked chain as a preview:
inventory row → item hover card → inspector with actions → attribute source breakdown. Show a second chain:
combat result → contribution summary → full event log.
```

---

#### DS-023 — Art-Optional Contract

**Category:** Foundation
**Priority:** Critical
**Depends On:** DS-002, DS-004

**Purpose:**
The 14 September implementation plan chose text-only equipment and Essences with no portraits. Grimoire's ItemSlot, Stage, Banner and LoadoutSlot are image-first. The repository has no creature, Essence or portrait art pipeline. The system must work fully without art, and become richer when art exists, without redesigning each time.

**Should Define:**

- A text-first baseline for every art-capable component. Name, rarity, type and state are always present as text.
- A fallback chain: artwork, then a registry emblem or icon, then a monogram or initial glyph, then text only. Each step is documented per component.
- Layout stability: art-bearing and art-free variants keep the same row or slot height, or a compact text-first variant is defined explicitly.
- The rule that art never carries information: no state, rarity or identity that exists only in the image.
- Loading and failure behaviour for images: reserved space and a fallback.
- A record of the current decision (text-first equipment and Essences) and what would change if art arrives.

**Examples in LegendsLegacy:**
An equipment slot without an icon shows "Off-hand · empty" or "Ashen Longsword · E". An Essence LoadoutSlot works with text only. A region card uses a text title over a generic texture when no regional art exists.

**Paste-ready Claude Design instruction**

```text
Add an "Art-optional contract" page to Standards in the Grimoire Design System. LegendsLegacy's current
direction presents equipment and Essences text-first, with no portraits, and the project has no pipeline for
creature, Essence or character art. Yet ItemSlot, LoadoutSlot, Stage and Banner are image-first. Define a
contract every art-capable component must meet. First, a text-first baseline: name, rarity, type and state
always exist as text, so the component is complete with no image. Second, a fallback chain: artwork, then a
registry emblem or icon, then a monogram or initial glyph, then text only. Document which steps each component
supports. Third, layout stability: the art and no-art variants keep the same dimensions, or a compact
text-first variant is explicitly defined. Fourth, art never carries information: no rarity, state or identity
may exist only in the image. Fifth, loading and failure: reserve space and show the fallback if an image
fails. Apply the contract now. Add a text-first variant to ItemSlot (for example a row form: "Ashen Longsword
· E · Tier 2"), make LoadoutSlot's text-only form the documented default, and define what Stage and Banner
render when no art is supplied. Record in the Decision Log that equipment and Essences are text-first until an
art strategy is approved, and list what would change when art arrives. Show previews of each updated component
in art and no-art forms side by side.
```

---

#### DS-024 — Application Shell Contract

**Category:** Layout
**Priority:** Critical
**Depends On:** DS-012, DS-013, DS-021

**Purpose:**
GameShell defines regions but not an information policy. The UI audit showed the shell competing with content: fifteen equal navigation links, currencies, the pinned objective, the current action and chat all fighting for space. Grimoire's "currency and level only in the TopBar" rule also conflicts with feature currencies such as Fate Echo and Glory, which belong on their own feature pages.

**Should Define:**

- Region jobs:
  - NavRail: where you can go.
  - TopBar: who you are and your global wealth.
  - Stage or Page: the subject.
  - Folio: the selected thing.
  - Chronicle: communication and the game log.
  - Current action: what is running.
  - KeyHints: shortcuts, only where they exist.
- Information classes:
  - Persistent: identity, level, Cinders, Soulstones, the current action, chat access with unread counts, and attention markers.
  - Contextual: feature currencies, feature tabs and selection detail.
  - Transient: toasts and reward reveals.
  - On demand: help, guides and notification history.
- Stage modes. Add a third mode, **Workbench**, for dense multi-pane screens with no stage art, alongside Page and Stage.
- Scroll ownership: which region scrolls, and a rule against nested scrollers where possible.
- A width table for the shell regions, cross-referenced with DS-012.
- The refined currency rule: global currencies appear in the TopBar, and feature currencies appear in that feature's resource header.

**Examples in LegendsLegacy:**
The Bazaar and Inventory in Workbench mode, the Creature Archive in Stage mode, the Character Overview in Page mode, and Fate Echo on the Prophecies page header.

**Paste-ready Claude Design instruction**

```text
Add an "Application shell contract" page to the Shell section of the Grimoire Design System, and update
GameShell's documentation to match. Define each region's single job. NavRail: where you can go. TopBar: who
you are and your global wealth. Stage or Page: the subject of the screen. Folio: the one selected thing.
Chronicle: communication and the game log. Current action: what is running right now (idle combat, a dungeon,
a raid). KeyHints: shortcuts, shown only where real shortcuts exist. Then classify information. Persistent:
identity, level, Cinders, Soulstones, the current action with Stop and Return, chat access with unread counts,
attention markers. Contextual: feature-specific currencies, feature tabs, selection detail. Transient: toasts
and reward reveals. On demand: help and page guides, and notification history. Refine the existing rule
"currency and level only in the TopBar": global currencies (Cinders, Soulstones) live in the TopBar; feature
currencies (Fate Echo on Prophecies, Glory and Arena tickets in the Colosseum, Tower Tokens in the World
Tower, Guild Favor in the Guild) appear in that feature's resource header and never in the TopBar. Add a third
stage mode, Workbench, beside Page and Stage. Workbench is for dense multi-pane screens with no stage art
(Inventory, Cinder Bazaar, Guild Vault, raid muster) and can use an inspector pane instead of the Folio.
Define scroll ownership for every region and avoid nested scrollers. Cross-reference the stage-width table
from the Layout page. Record the Workbench mode and the currency rule in the Decision Log.
```

---

### Phase 1b — Content Standards

#### DS-025 — Voice, Capitalisation & Label Rules

**Category:** Content
**Priority:** High
**Depends On:** DS-002, DS-021

**Purpose:**
Grimoire's voice rules (you-address, a mechanics register and a lore register, sentence case, no emoji) are good but short. As dozens of components gain labels, tags, headings and messages, capitalisation and label length need exact rules. Otherwise "Equipped", "EQUIPPED" and "Currently equipped" will all appear.

**Should Define:**

- The existing voice rules, kept and expanded with do/don't pairs.
- Capitalisation:
  - Game proper nouns (Cinders, Soulstones, Essences, the World Tower, the Cinder Bazaar, Shenic, Meran, Vigor, Pending Loot) always capitalised.
  - Attribute names capitalised as terms (Attack Speed).
  - Everything else in sentence case.
  - Caps applied by CSS only.
- Label length limits: tags 1–3 words, buttons 1–4 words, tabs 1–2 words, section titles at most 5 words.
- Plurals and counts: "1 Essence" and "3 Essences". Decide whether "1 Cinder" or "1 Cinders" and record it.
- Person and tense: second person, present tense; results stated in the past tense ("Floor cleared").
- Where lore may appear (the Folio, item and creature descriptions, quest intros, system lines) and where it may not (buttons, errors, tables, costs).

**Examples in LegendsLegacy:**
The "Equipped" tag, the "Attuned Essences" section title, the "Enter dungeon" button, and a system line such as "The Guardian of Floor 12 has fallen."

**Paste-ready Claude Design instruction**

```text
Expand the Voice guidance in the Content section of the Grimoire Design System into a full "Voice,
capitalisation and labels" page. Keep the existing rules: speak to the player as "you", keep the mechanics
register (Barlow, exact, numeric) separate from the lore register (EB Garamond italic, no numbers), use
sentence case with capitals applied by CSS, no emoji, no exclamation marks. Add do/don't pairs for each. Add
capitalisation rules: game proper nouns (Cinders, Soulstones, Essences, Combat Styles, the World Tower, the
Cinder Bazaar, Shenic, Meran, Vigor, Pending Loot, Nobility, Signets) are always capitalised; attribute names
are capitalised as terms (Attack Speed, Crit Chance); everything else is sentence case. Add length limits:
tags 1–3 words, buttons 1–4 words, tabs 1–2 words, section titles at most 5 words. Add plural and count rules
("1 Essence", "3 Essences"), and decide and record whether one Cinder is written "1 Cinder" or "1 Cinders".
Use second person and present tense for instructions; state results in the past tense without celebration
("Floor cleared", "Defeated"). List where lore may appear (the Folio, item and creature descriptions, quest
intros, Chronicle system lines) and where it may not (buttons, errors, tables, cost lines, confirmations).
Include a table of approved standard labels for recurring states: Equipped, Attuned, New, Locked, Claimable,
Claimed, Listed, Borrowed, Expired.
```

---

#### DS-026 — Terminology Glossary & Naming Rules

**Category:** Content
**Priority:** Critical
**Depends On:** DS-025

**Purpose:**
The UI audit lists a set of concepts that are easy to confuse:

- Combat Rating, Gear Value and Arena Rating.
- An unbound Essence item and an archived Essence.
- Equipment tier, rarity, quality, rank and variant.
- First Clear and Echo.
- A captured build and the live build.

Code names also leak into the interface: `tavern` for the Leaderboard, and `rally` or Legacy Ascension for Tower expeditions. The combat lexicon already enforces "one meaning per term" for mechanics. The interface needs the same discipline.

**Should Define:**

- Glossary entries: term, definition, where it appears, synonyms not to use, and related terms.
- The confusable sets above, each with explicit guidance.
- A mapping from technical names to visible names (for example `tavern` → Leaderboard, and rally → expedition).
- Retired terms, such as "Gear Power" (replaced by Gear Value) and removed systems (Forge, tempering, gathering). These must not appear in new UI.
- The rule that condition and combat verb names come from the combat lexicon and are never paraphrased.
- A placeholder entry for "Doctrines", pending a definition.
- A process for adding a term.

**Examples in LegendsLegacy:**
A player's Combat Rating on the Overview compared with an item's Gear Value in Inventory. Pending Loot compared with secured rewards in dungeons.

**Paste-ready Claude Design instruction**

```text
Add a Terminology page to the Content section of the Grimoire Design System: a glossary with naming rules.
Each entry has: term, one-sentence definition, where it appears, synonyms not to use, related terms. Start
with the concepts LegendsLegacy players most easily confuse, and give each set explicit guidance. Combat
Rating (character summary) versus Gear Value (item comparison) versus Arena Rating (PvP standing); never use
the retired "Gear Power". Character level and Combat XP versus Essence level versus Combat Style mastery.
Equipment tier versus rarity versus quality (Crude, Standard, Fine, Exceptional, Masterpiece) versus rank
versus style or variant versus set. An unbound Essence item (inventory) versus an archived Essence (learned,
with level and Ascension tier) versus an attuned Essence (in a loadout slot). Creature Focus. Essence Codex
collections. Vigor and Pending Loot versus secured rewards. The live build versus an unsaved preview versus an
assigned preset versus a captured snapshot. First Clear versus Echo in the World Tower. Renown, titles and
achievements. Nobility and Signets. Add a technical-to-visible name map: the route "tavern" is shown as
Leaderboard; rally and Legacy Ascension code names are shown as World Tower expeditions. List retired terms
that must not appear in new UI: Gear Power, Forge, tempering, salvaging, gathering. State that condition names
and combat verbs come from the game's combat lexicon exactly and are never paraphrased. Add a placeholder
entry for "Doctrines" marked "definition pending". End with a short process for adding new terms.
```

---

#### DS-027 — Number, Unit & Stat Formatting

**Category:** Content
**Priority:** Critical
**Depends On:** DS-009, DS-026

**Purpose:**
DS-009 decides how numbers look. This item decides what they say. That covers precision per stat, units, abbreviation thresholds, rounding, percentage points compared with percentages, chances, stacks and scaling. Grimoire has `lgFormatNumber` and `lgFormatShort`, and this standard should govern both.

**Should Define:**

- Thousands separators, and the locale assumption (en-US today).
- Abbreviation: when "12.5k" is allowed (compact, non-decision contexts), and the rule that full numbers always appear at the point of commitment (costs, prices, trades).
- Precision by unit type:
  - Flat stats are integers.
  - Percentages take zero or one decimal.
  - Rates take one decimal (184.6 threat/s).
  - Multipliers take up to two decimals.
- A units list: %, percentage points, HP/5s, threat/s, s, ×, and per level.
- Rounding safety: never show 100% before completion, never show 0% for a value above zero (use "<1%"), and never round a requirement in the player's favour.
- Chances ("12% chance"), stacks ("×3"), charges ("2 charges") and scaling ("+60% of Power").
- Signs, deltas, ranges and capacity ("3 / 5 Arena tickets"), cross-referenced with DS-009.
- Raw versus effective values, and how to show values above a cap.

**Examples in LegendsLegacy:**
Attribute Ledgers. Penetration expressed in percentage points. A Bazaar price of 1,240,000 Cinders at commitment. Arena ticket capacity. Condition stacks.

**Paste-ready Claude Design instruction**

```text
Add a "Numbers and units" page to the Content section of the Grimoire Design System that standardises what
numbers say (the Numerals foundation already covers how they look). Define thousands separators and the
current locale assumption (en-US). Define abbreviation: 12.5k and 3.2M are allowed only in compact,
non-decision contexts such as the TopBar or a ranking column, with the full figure available on hover and
focus; full numbers are mandatory at the point of commitment (costs, prices, trades, donations). Define
precision by unit type: flat stats as integers, percentages with 0–1 decimals, rates with 1 decimal (184.6
threat/s), multipliers with up to 2 decimals (×1.25). List the standard units: %, percentage points (spelled
out, for example "40 percentage points" of penetration), HP/5s, threat/s, s, ×, per level. Define rounding
safety: never display 100% before completion, never display 0% for a positive value (use "<1%"), never round a
requirement in the player's favour. Define how to write chances ("12% chance"), stacks ("×3"), charges ("2
charges"), scaling ("+60% of Power") and capacity ("3 / 5 Arena tickets"). Define how raw and effective values
are shown when a value exceeds a cap, for example "62% (capped at 60%)". Make lgFormatNumber and lgFormatShort
follow these rules and note any behaviour change in the Decision Log. Include a reference table with one real
LegendsLegacy example per rule.
```

---

#### DS-028 — Time, Duration, Cooldown & Reset Formatting

**Category:** Content
**Priority:** High
**Depends On:** DS-027

**Purpose:**
LegendsLegacy is full of time: ability cooldowns and durations, Creature Focus cooldowns, Prophecy daily and weekly resets, tournament registration windows, regional boss schedules, offline combat retention, Nobility expiry and chat timestamps. Each currently risks its own format.

**Should Define:**

- Countdown formats by range: days and hours ("2d 4h"), hours and minutes ("3h 12m"), minutes and seconds ("12m 05s"), and a clock format ("04:32") only for live, under-an-hour countdowns.
- Combat durations and cooldowns ("8s", "1.5s", "for 6s"), and ticks ("every 5s").
- Relative past ("just now", "3 h ago", "yesterday") and when to switch to absolute dates.
- Absolute timestamps in local time, with the date format and a 24-hour clock (decide and record).
- Server reset phrasing: "Resets in 5h 12m", with the UTC time available on hover.
- Scheduled event phases: "Starts in", "Live · ends in", "Ended 2h ago".
- Expiry: "Expires in 3d", and when "Expiring soon" (the warning state) begins.
- The rule that a countdown is informative only; the server decides eligibility.

**Examples in LegendsLegacy:**
Creature Focus ("Change available in 1h 42m"). The daily Prophecy reset. The regional boss "Live · ends in 12m 08s". Chat timestamps. "Nobility until 14 Nov 2026".

**Paste-ready Claude Design instruction**

```text
Add a "Time and duration" page to the Content section of the Grimoire Design System. LegendsLegacy uses time
everywhere, and each use needs one format. Countdowns by range: "2d 4h" when a day or more, "3h 12m" when an
hour or more, "12m 05s" under an hour, and a clock style "04:32" only for live countdowns under an hour on
event screens. Combat time: cooldowns and durations as "8s" or "1.5s", ability durations as "for 6s", periodic
effects as "every 5s". Relative past: "just now", "3 h ago", "yesterday", switching to an absolute date after
7 days. Absolute timestamps in the player's local time: choose and record the date format and 24-hour clock.
Server resets: "Resets in 5h 12m", with the UTC reset time on hover. Scheduled events: "Starts in 2h", "Live ·
ends in 12m 08s", "Ended 2h ago". Expiry: "Expires in 3d", and define when an item enters the "expiring soon"
warning state (for example under 24h for daily content, under 1h for events). State that countdowns are
informative only: when time runs out the UI waits for the server before changing availability, with a brief
"Updating…" state. Include LegendsLegacy examples: Creature Focus cooldown, the daily Prophecy reset and
weekly Favor, Arena ticket regeneration, tournament registration, regional boss phases, offline combat
retention ("24h retained"), and Nobility expiry. Update the Presence component's last-seen format to match.
```

---

#### DS-029 — Ability & Effect Description Grammar

**Category:** Content
**Priority:** High
**Depends On:** DS-026, DS-027, DS-028

**Purpose:**
Active and passive abilities, set bonuses, Combat Style refinements, Soulstone upgrades and conditions are all described in mechanical text. The combat lexicon defines conditions, verbs, stacking and targeting precisely. The interface needs a matching sentence grammar and keyword styling, so descriptions are scannable and consistent.

**Should Define:**

- A sentence template: trigger, then target, then verb (from the combat lexicon), then magnitude with scaling, then duration, then condition.
- Phrasing for active and passive abilities: "Active · 8s cooldown" as metadata, not inside the sentence.
- Keyword styling: condition names are hoverable keywords with a condition card; damage-type words carry their hue plus the word; numbers use the effect-magnitude style.
- "Grants" and "applies" for beneficial effects; "inflicts" for harmful effects.
- The order of lines: effect, then scaling, then limits (cap, internal cooldown), then lore (separate).
- Set bonuses (2-piece and 4-piece), upgrade effects ("+2% per rank") and conditional clauses ("while below 50% Max Health").
- What never appears: hidden mechanics, vague words ("greatly", "slightly"), or paraphrased condition names.

**Examples in LegendsLegacy:**
"Alpha Fangs: Deals 140% of Power as Physical damage and inflicts Bleed ×2 for 6s." A Conduit refinement. A Soulstone upgrade.

**Paste-ready Claude Design instruction**

```text
Add an "Ability and effect descriptions" page to the Content section of the Grimoire Design System.
LegendsLegacy has a canonical combat lexicon (conditions, verbs, damage types, stacking, targeting); UI
descriptions must follow it exactly and never paraphrase it. Define the sentence template: trigger (if any),
then target, then verb from the lexicon, then magnitude with scaling, then duration, then conditional clause.
For example: "On hit: deals 140% of Power as Physical damage and inflicts Bleed ×2 for 6s." Put cooldown, cost
and active or passive status in a metadata line ("Active · 8s cooldown"), never inside the sentence. Define
keyword styling: condition names are interactive keywords that open a condition hover card; damage-type words
appear in their damage hue together with the word; numbers use the effect-magnitude style from the colour
allocation. Use "grants" or "applies" for beneficial effects and "inflicts" for harmful ones. Set the line
order: effect, scaling, limits (caps, internal cooldowns), then lore as a separate block. Cover set bonuses
(2-piece, 4-piece), per-rank upgrades ("+2% Crit Chance per rank"), Combat Style refinements, Essence
Ascension changes (current → next) and conditional clauses ("while below 50% Max Health"). List what never
appears: hidden mechanics, vague intensity words ("greatly", "slightly"), and paraphrased condition names.
Include six worked examples: an active Essence ability, a passive Essence ability, a set bonus, a Combat Style
refinement, a Soulstone upgrade, and a condition definition card.
```

---

#### DS-030 — Action Labels, Confirmations, Errors & Empty-State Copy

**Category:** Content
**Priority:** High
**Depends On:** DS-025, DS-026

**Purpose:**
The game has consequential actions: selling, donating to the guild, redeeming Signets, abandoning a run, leaving a guild and listing on the Bazaar. It also has real server restriction reasons and recovery flows. Consistent copy is what makes consequences legible.

**Should Define:**

- Button labels: a specific verb plus an object, with the amount where relevant ("Claim 3 rewards", "List for 1,240 Cinders"). Never "OK", "Submit" or "Yes".
- Destructive labels name the loss ("Abandon run and lose Pending Loot").
- Confirmation anatomy: the title is a question naming the object, the body states the consequence and cost, the primary button repeats the verb, and the secondary button is "Keep…" or "Cancel".
- Error messages: what happened, why (when known), and what to do next. Server restriction reasons are shown as given.
- Requirement wording: "Requires level 20", "Requires Floor 10 cleared", "Needs 1,200 more Cinders".
- Success feedback: a factual past-tense statement ("Listed Ashen Longsword for 1,240 Cinders").
- Empty-state copy for each empty type (see DS-048): why it is empty, and how to fill it.

**Examples in LegendsLegacy:**
The guild donation confirmation, a failed Bazaar order, the empty Soul Archive for a new player, and a Nobility redemption result.

**Paste-ready Claude Design instruction**

```text
Add an "Actions, confirmations and messages" page to the Content section of the Grimoire Design System. Define
button label rules: a specific verb plus an object, including the amount when it matters ("Claim 3 rewards",
"Enter dungeon", "List for 1,240 Cinders", "Redeem 2 Signets"); never "OK", "Submit", "Yes" or "Confirm"
alone. Destructive labels name the loss ("Abandon run and lose Pending Loot", "Donate permanently"). Define
confirmation anatomy: the title is a question naming the object ("Donate Ashen Longsword to the guild?"); the
body states the consequence, the cost and whether it can be undone; the primary button repeats the verb; the
secondary button is "Keep item" or "Cancel". Define error messages: what happened, why (when known), what to
do next. Show server restriction reasons as given and never replace them with generic text. Define requirement
wording: "Requires level 20", "Requires Floor 10 cleared", "Needs 1,200 more Cinders", "Guild officers only".
Define success feedback as a factual past-tense statement without celebration ("Listed Ashen Longsword for
1,240 Cinders"). Define empty-state copy for each empty type (first use, filtered to nothing, all done, not
yet unlocked): why it is empty and how to change that, with a link to the action. Include LegendsLegacy
examples: a guild donation, a Bazaar buy failure, abandoning a dungeon run, a Nobility redemption, a new
player's empty Soul Archive, and "All Prophecies claimed".
```

---

### Phase 2 — Core Components

#### DS-031 — Button System (including icon-only buttons and toolbars)

**Category:** Component
**Priority:** Critical
**Depends On:** DS-015, DS-019, DS-030

**Purpose:**
Button has five variants (primary, solid, quiet, danger, link) and a hotkey cap, but no pending state, no unavailable-with-reason state, no compact size for rows, no icon-only form and no grouping rules. The audit found the same action rendered with different emphasis across screens.

**Should Define:**

- The variant hierarchy and when to use each. Keep one solid button per screen.
- Sizes: md, sm, and a compact size for table rows.
- All interaction states, plus pending (the label changes, for example "Listing…", and the width is kept) and unavailable with a reason (the reason shown inline or on focus).
- Icon-only buttons: square, a tooltip and accessible name required, and an allowed-actions list.
- Toggle buttons with a pressed state (favourite, lock).
- A slot for a cost next to the label, filled in DS-054.
- Button groups and toolbars: spacing, order and overflow into a "More" menu.
- Placement: dialog footer order and the position of destructive actions.
- The shape decision from DS-015.

**Examples in LegendsLegacy:**
"Enter dungeon" (solid), "Challenge" in Arena rows (compact), the favourite toggle on items, the inventory bulk-action toolbar and "Retreat" (danger).

**Paste-ready Claude Design instruction**

```text
Revise the existing Button component in the Grimoire Design System into a complete button system, keeping its
current variants (primary, solid, quiet, danger, link) and hotkey key cap. Apply the shape decision from the
Shape page. Document when to use each variant; keep the rule of one solid button per screen for the committing
action. Add a compact size for table and list rows beside md and sm. Implement every relevant state from the
States page: default, hover, focus-visible, pressed, pending, disabled, and unavailable-with-reason. In the
pending state the label becomes a present participle ("Listing…", "Claiming…") and the button keeps its width.
In the unavailable state the reason is shown inline beneath or beside the button, or on focus when space is
tight ("Requires level 20"). Add an icon-only variant (square, a tooltip and accessible name required) and
list which actions may be icon-only (close, favourite, lock, filter, sort, refresh, more). Add a toggle
variant with a pressed state for favourite and lock. Reserve a slot for a cost beside the label, to be filled
by the future cost component. Add button groups and toolbars: spacing, ordering, and overflow into a "More"
menu. Define placement rules: dialog footer order, where destructive actions sit, and never two solid buttons
side by side. Use LegendsLegacy labels in the preview: Enter dungeon, Challenge (compact, in a row), Retreat
(danger), Claim all, favourite toggle, an inventory bulk-action toolbar, and an unavailable "Ascend" with its
requirement.
```

---

#### DS-032 — Tooltip & Hover Card Architecture

**Category:** Component
**Priority:** Critical
**Depends On:** DS-011, DS-013, DS-022

**Purpose:**
Inspection without navigation is central to LegendsLegacy: items, abilities, conditions, stats and players. Grimoire's only tooltip is the Ledger row explanation. The game's existing CDK popover behaviour (collision handling, Escape and outside-click dismissal, detached popovers above modals) must be preserved and given a single visual architecture.

**Should Define:**

- **Tooltip:** short text of one or two lines, for icon labels, abbreviations and brief explanations. No interactive content.
- **HoverCard:** rich content in a standard anatomy (header, sections, footer hint). Variants for items, abilities, conditions, stats, players and resources.
- Triggers:
  - Hover after a short delay.
  - Keyboard focus opens it immediately.
  - Tap pins it on touch.
  - A pinned card can be hovered into and scrolled.
- Placement, collision handling, maximum widths and maximum height with scrolling.
- Nesting: one level only (for example a condition keyword inside an ability card).
- The rule that a hover card never contains the only path to an action and never holds a never-hide fact.
- The z-layer rule: detached above modals.

**Examples in LegendsLegacy:**
An item card in Inventory and the Bazaar, an ability card in the Essence Archive, the Bleed condition card from an ability description, and a player card from a chat name.

**Paste-ready Claude Design instruction**

```text
Add a Tooltip component and a HoverCard component to the Grimoire Design System, and document the tooltip
architecture in Components. LegendsLegacy relies on inspection without navigation (items, abilities,
conditions, stats, players), and the game already has CDK-based positioned popovers with collision handling,
Escape and outside-click dismissal, and detached popovers that sit above modals; preserve those behaviours.
Tooltip: one or two short lines for icon labels, abbreviations and brief explanations, with no interactive
content. HoverCard: rich inspection content with a standard anatomy (a header with identity, one or more
sections separated by the Lines rules, an optional footer with hints such as "Shift to compare"), on the folio
or surface-raised elevation. Define HoverCard variants to be filled by later game components: item, ability,
condition, stat, player, resource. Define triggers: hover after a short delay; keyboard focus opens
immediately; tap pins on touch; a pinned card can be hovered into and scrolled; Escape closes. Define
placement, collision handling, maximum width and maximum height with internal scrolling. Allow one level of
nesting only (a condition keyword inside an ability card opens a condition card). State that a hover card
never holds the only path to an action and never holds a fact from the never-hide list. Migrate Ledger's row
explanation to use Tooltip. Preview a Tooltip on an icon button, and a HoverCard skeleton for an item and for
a condition, including the nested case.
```

---

#### DS-033 — Popover, Menu & Context Menu

**Category:** Component
**Priority:** High
**Depends On:** DS-032

**Purpose:**
Several recurring interactions need small action surfaces: quick filters, player actions from chat and rankings (whisper, inspect, invite), and item actions (equip, compare, link, list, favourite). Right-click menus are natural on desktop but must always have a visible keyboard equivalent.

**Should Define:**

- A Popover for interactive content, such as a quick filter or a small form.
- A Menu anchored to a "More" button: items with an icon, label, shortcut and meta; separators; destructive items; disabled items with reasons; at most one submenu level.
- A ContextMenu that reuses the Menu, opened by right-click and by a visible "More" button so it is never the only path.
- Keyboard: arrow navigation, type-ahead and Escape.
- Standard menus for player and item actions.

**Examples in LegendsLegacy:**
A chat name (Whisper, Inspect, Invite to guild, Mute). An inventory item (Equip, Compare, Link in chat, List on Bazaar, Favourite). A guild member (Promote, Kick, officers only).

**Paste-ready Claude Design instruction**

```text
Add Popover, Menu and ContextMenu components to the Grimoire Design System, using the elevation, Lines and
Tooltip rules already defined. Popover holds small interactive content such as a quick filter or a two-field
form. Menu is a list of actions anchored to a button. Items have an optional icon, label, keyboard shortcut
and meta text; groups are divided by separators; destructive items use the danger role; unavailable items stay
visible with their reason; at most one level of submenu. ContextMenu reuses Menu and opens on right-click, but
every context menu must also be reachable from a visible "More" (⋯) button so right-click is never the only
path. Define keyboard behaviour: arrow keys move, type-ahead jumps, Enter activates, Escape closes and returns
focus. Define two standard menus as examples. The player-name menu in Chronicle, rankings and guild lists:
Whisper, Inspect profile, Invite to guild, Invite to party, Mute. The item menu in Inventory: Equip, Compare,
Link in chat, List on Cinder Bazaar, Favourite, Donate to guild, with Donate marked as permanent. Also show a
guild member menu where "Kick" is unavailable with the reason "Officers only". Keep menus compact and
text-first with no decorative ornament.
```

---

#### DS-034 — Dialogs, Confirmations & Drawers

**Category:** Component
**Priority:** Critical
**Depends On:** DS-013, DS-030, DS-031

**Purpose:**
Consequential game actions need confirmation. Transfers and redemptions need contained forms. Page guides, filters and mobile navigation need drawers. The game already has a modal container and a focus-trapping `appDialogFocus` directive; the design system should standardise how those surfaces look and behave.

**Should Define:**

- Dialog sizes: small (confirm), medium (form) and large (workbench, for example a guild transfer).
- Dialog anatomy: title, body, a cost or consequence summary region, and a footer.
- Behaviour:
  - Focus is trapped and restored.
  - Escape closes the dialog.
  - Backdrop clicks do not close commitment dialogs.
  - Long bodies scroll.
- Confirmation variants:
  - Standard.
  - Destructive.
  - High-stakes, with a typed or held confirmation for disbanding a guild or transferring leadership.
- Pending and result states inside the dialog, with a safe retry for uncertain responses.
- Drawers: a side sheet for filters, page guides and help; a navigation drawer on narrow screens; width tokens; modal and non-modal behaviour.

**Examples in LegendsLegacy:**
Nobility Signet redemption, a guild donation, leaving a guild, a stock item transfer, the page-guide drawer and the inventory filter drawer on narrow screens.

**Paste-ready Claude Design instruction**

```text
Add Dialog, ConfirmDialog and Drawer components to the Grimoire Design System. Dialog comes in three sizes:
small for confirmations, medium for forms, large for workbench tasks such as guild item transfers. The anatomy
is a title, a body, an optional consequence and cost summary region, and a footer following the Button
placement rules. The surface is folio with shadow-panel and no ornament except for major commitment dialogs,
within the ornament budget. Behaviour follows the game's existing dialog-focus directive: focus is trapped,
focus returns to the opener, Escape closes, backdrop clicks do not close commitment dialogs, and long bodies
scroll internally. ConfirmDialog has three variants. Standard: the title asks a question naming the object,
and the body states consequence, cost and reversibility. Destructive: the danger role, and the primary label
names the loss. High-stakes: a typed or press-and-hold confirmation, reserved for disbanding a guild or
transferring leadership. Dialogs support a pending state (the primary button is pending and the dialog stays
open), a result state, and a failure state with a safe retry that keeps the same operation. Drawer is a side
sheet for filters, page guides and help, and the navigation drawer on narrow screens. Define width tokens and
modal versus non-modal behaviour. Preview these LegendsLegacy cases: redeeming 2 Nobility Signets, donating an
item to the guild (permanent), abandoning a dungeon run with Pending Loot at risk, disbanding a guild
(high-stakes), and the page-guide drawer.
```

---

#### DS-035 — Form Field, Text & Quantity Input

**Category:** Component
**Priority:** High
**Depends On:** DS-019, DS-027, DS-031

**Purpose:**
Grimoire has only SearchField. The game needs text inputs for guild descriptions and chat, numeric and quantity inputs for stock use and market orders, and price inputs with fee and net previews. Quantity entry with limits, a Max option and affordability checks is a core game interaction, not a generic form.

**Should Define:**

- Field anatomy: label, control, help text, error, character counter, and optional or required marking.
- States: default, hover, focus, error, disabled, read-only and pending validation.
- A text input and a textarea.
- A NumberInput with units and a suffix.
- A QuantityStepper:
  - − and + buttons, min and max, and a Max shortcut.
  - Keyboard arrows, with Shift for steps of ten.
  - The available amount shown ("of 240").
  - Invalid-entry handling.
- A PriceInput with a currency icon and a hook for a live fee and net preview.
- Validation timing: live for quantities, on blur for text. Typed values are preserved on error.
- Form layout: labels above by default, and inline compact forms for toolbars.

**Examples in LegendsLegacy:**
Bazaar buy orders (quantity and price), using several stock items, Signet redemption quantity, guild description and the chat composer.

**Paste-ready Claude Design instruction**

```text
Add a form field system to the Grimoire Design System, building on the existing input styles and SearchField.
Define a FormField wrapper: label, control, help text, error message, optional character counter, and optional
or required marking. Define states: default, hover, focus, error, disabled, read-only, pending validation. Add
TextInput and Textarea components (for guild descriptions and similar). Add NumberInput with a unit suffix.
Add QuantityStepper for game quantities: − and + buttons, min and max, a "Max" shortcut, arrow keys to step
with Shift for steps of ten, the available amount shown beside it ("of 240"), and clear handling of invalid or
over-limit entries. Add PriceInput: a numeric input with a currency icon from the resource registry, and a
slot below it for a live fee and net-proceeds preview. Validation is live for quantities and prices and on
blur for text; typed values are never cleared on error. Default forms use labels above controls; also define a
compact inline form for toolbars. All controls use line-strong edges, the focus ring, the compact density size
where needed, and 12px minimum text. Preview LegendsLegacy cases: a Bazaar buy order (quantity 25 of an item
at 1,240 Cinders each, with fee and total), using 3 of 12 stock items, redeeming 1–12 Signets, and a guild
description with a 500-character counter and an error.
```

---

#### DS-036 — Selection & Range Controls

**Category:** Component
**Priority:** High
**Depends On:** DS-035

**Purpose:**
Settings (reading font, chat layout, display Nobility), bulk inventory selection, filters and permissions all need consistent checkboxes, radios and toggles. Sliders are rarely needed, so this item should say explicitly where they are allowed.

**Should Define:**

- A Checkbox with an indeterminate state for bulk selection.
- A RadioGroup, and a card-style radio for two to four rich options.
- A Toggle for immediate-effect settings only.
- A Slider allowed only for continuous preferences (such as volume), with numeric entry for game quantities instead.
- A settings row: label, description, and control aligned to the end.
- Hit areas, label click behaviour, group labels and keyboard behaviour.

**Examples in LegendsLegacy:**
Display Nobility (toggle), Chat layout (radio: docked or floating), bulk selection in Inventory (checkboxes) and the guild permission matrix.

**Paste-ready Claude Design instruction**

```text
Add Checkbox, RadioGroup, Toggle and Slider components to the Grimoire Design System, plus a SettingsRow
layout. Checkbox supports checked, unchecked and indeterminate (for "select all" in bulk inventory actions).
RadioGroup supports a compact list form and a card-style form for two to four rich options, such as the Chat
layout choice between Docked and Floating drawer, each with a one-line description. Toggle is only for
settings that take effect immediately (Display Nobility, Show perks); a form that needs saving uses checkboxes
instead. Slider is only for continuous preferences such as volume; game quantities always use QuantityStepper.
SettingsRow places a label and description on the left and the control aligned to the end, in Standard
density. Define hit areas (the whole label is clickable), group labels, keyboard behaviour (Space toggles,
arrow keys move within radio groups) and every state from the States page, including unavailable-with-reason
(for example a guild permission an officer cannot grant). Use line-strong for control edges, arcana for the
checked or on state, and focus-ring for focus. Preview a Settings section (reading font, chat layout, display
Nobility), an inventory bulk-selection header, and a small guild permission matrix.
```

---

#### DS-037 — Select, Dropdown & Combobox

**Category:** Component
**Priority:** High
**Depends On:** DS-033, DS-035

**Purpose:**
Sort fields, loadout preset selection, filters with many values, and search-with-suggestions for players and items all need a select family. Preset selection is especially important because it carries save state and must not imply that one preset covers equipment, Essences and Combat Style together.

**Should Define:**

- A Select for a single choice.
- A MultiSelect for filters, with a count summary ("Rarity: 3 selected").
- A Combobox with search, generalised from SearchField to cover items, creatures and players.
- Option anatomy: icon, label, meta, unavailable with a reason, and grouping.
- A PresetSelect: preset name, save status (saved, saving, failed) and a scope label (Equipment preset versus Essence preset).
- Keyboard behaviour, type-ahead, virtualisation for long lists, and no-results and loading states.

**Examples in LegendsLegacy:**
Inventory sort, Essence loadout preset, an equipment preset with autosave status, a Bazaar item search and a rarity multi-select filter.

**Paste-ready Claude Design instruction**

```text
Add Select, MultiSelect, Combobox and PresetSelect components to the Grimoire Design System, reusing Popover
and Menu surfaces and the FormField anatomy. Select is a single choice for sort fields and small option sets.
MultiSelect is for filters, summarising the selection in the trigger ("Rarity: 3 selected"). Combobox
generalises the existing SearchField (keep its ARIA combobox behaviour) so it can search players, items and
creatures, with loading, no-results and recent-search states. Option anatomy: optional icon or rarity code,
label, meta text, grouping headers, and unavailable options shown with a reason. Add PresetSelect for
LegendsLegacy loadout presets. It shows the preset name, a scope label (Equipment preset, or Essence preset,
never an ambiguous "Build"), and a save status (Saved, Saving…, Save failed · Retry). Its list includes locked
extra slots with the Nobility note "6 presets with Nobility · 3 free". Support keyboard navigation, type-ahead
and virtualisation for long lists. Preview: inventory sort (Gear Value, Rarity, Tier, Newest), a rarity
MultiSelect showing rarity codes, a Bazaar item Combobox, and an Essence PresetSelect in the saving and failed
states.
```

---

#### DS-038 — Tabs & Segmented Controls

**Category:** Component
**Priority:** High
**Depends On:** DS-011, DS-019

**Purpose:**
The audit found several tab families (content tabs, navigation tabs, filter tabs and ad-hoc segmented controls) with different keyboard behaviour, and no distinction between destinations, subviews and filters. TabStrip has primary and secondary levels; it now needs defined semantics.

**Should Define:**

- Three tab types:
  - **Destination** tabs, backed by the URL and preserved on refresh.
  - **View** tabs, local subviews.
  - **Filter** tabs, which narrow one list. Consider replacing these with chips.
- Each type's visual level and keyboard model. Arrow keys move focus, as the newer navigation tabs already do.
- A SegmentedControl for two to four mutually exclusive modes, such as list or grid, and Buy or Sell.
- Counts and attention markers on tabs, following DS-020.
- Overflow behaviour: scroll, or a "More" menu.
- Rules: at most two tab levels on a screen, tabs are never used for sequential steps, and an inactive tab keeps its state when that matters.

**Examples in LegendsLegacy:**
Colosseum (Arena, Tournament, Market, Rankings, Record) as destination tabs. Essences (Archive, Absorb, Creatures, Codex). Bazaar Buy and Sell as a segmented control.

**Paste-ready Claude Design instruction**

```text
Revise the existing TabStrip component in the Grimoire Design System and add a SegmentedControl. LegendsLegacy
currently has several inconsistent tab families, so define three tab types with distinct semantics.
Destination tabs are URL-backed sections of a feature, preserved on refresh and Back, using the primary
engraved level (Colosseum: Arena, Tournament, Market, Rankings, Record; Essences: Archive, Absorb, Creatures,
Codex). View tabs are local subviews inside a section, using the secondary level. Filter tabs narrow a single
list; recommend whether these should become filter chips instead and record the decision. Define the keyboard
model for all tabs: arrow keys move focus and selection together, Home and End jump. Add counts and attention
markers on tabs following the state combination rules (one marker, fixed position). Define overflow:
horizontal scroll with edge fades, or a "More" menu when there are more than about six tabs. Add rules: at
most two tab levels on one screen; tabs are never used for sequential steps; inactive tabs keep their state
when switching back matters (filters, scroll). Add SegmentedControl for two to four mutually exclusive modes,
such as List and Grid, or Buy and Sell on the Cinder Bazaar. Update ScreenArchive to demonstrate the
destination and view levels correctly.
```

---

#### DS-039 — Filter Bar, Sort & Search

**Category:** Component
**Priority:** High
**Depends On:** DS-036, DS-037, DS-038

**Purpose:**
Inventory, the Soul Archive, Creatures, the Codex, Bazaar listings, rankings and guild discovery all filter and sort large collections. The audit found many equal-weight filter rows that increase scanning cost. One filter bar pattern keeps them consistent and calm.

**Should Define:**

- Anatomy:
  - A search field.
  - Up to three primary filters shown inline.
  - A "More filters" popover or drawer.
  - An active-filter chip summary with a Clear all action.
  - A result count and a sort control (field and direction).
- FilterChip (interactive), kept distinct from Tag (static).
- Standard game filters: rarity (with codes), equipment slot, tier, owned or unowned, new, favourite, and discovered or undiscovered.
- Persistence: filters survive inspection and Back, and reset only on explicit Clear.
- Zero results, linked to Clear filters.

**Examples in LegendsLegacy:**
The inventory gear list, Creatures (discovered, Focus, region), the Bazaar item browser and the guild discovery list.

**Paste-ready Claude Design instruction**

```text
Add a FilterBar component and a FilterChip component to the Grimoire Design System. LegendsLegacy filters and
sorts large collections everywhere (Inventory, Soul Archive, Creatures, Essence Codex, Bazaar listings,
rankings, guild discovery), and the current UI shows many filters at equal weight. Define FilterBar anatomy: a
search field (Combobox); at most three primary filters inline (Select, MultiSelect or SegmentedControl); a
"More filters" button opening a Popover, or a Drawer on narrow widths; a row of active-filter chips with
"Clear all"; a result count ("48 items"); and a sort control with field and direction. FilterChip is
interactive and removable, and must look clearly different from Tag, which is static and never clickable.
Define standard LegendsLegacy filter vocabularies: rarity (showing codes C, UC, R, E, U, L, LG), equipment
slot (the eight slots), tier, owned or unowned, new, favourite, discovered or undiscovered, region. Define
persistence: filters and sort survive opening an item, inspecting a profile and pressing Back, and reset only
on explicit "Clear all". Define the zero-results state with a "Clear filters" action. Use Compact density.
Preview FilterBar on the inventory gear list and on Creatures (region, discovered, Focus target).
```

---

#### DS-040 — Rows & Lists

**Category:** Component
**Priority:** Critical
**Depends On:** DS-010, DS-014, DS-019, DS-020, DS-021

**Purpose:**
Most LegendsLegacy content is rows: items, Essences, creatures, members, rewards, objectives, rankings and log lines. A single row primitive with defined slots replaces many ad-hoc list rows and the `ll-list-row` and `ll-item-row` classes. EntryList becomes one variant of it.

**Should Define:**

- A Row primitive with slots:
  - Leading: an icon, slot, rank or checkbox.
  - Primary text: the name.
  - Secondary text: meta.
  - Trailing values.
  - Trailing actions.
  - State markers in the DS-020 positions.
- Variants: static, navigation (opens detail), selectable (single), checkable (multi), expandable, and draggable (with a click alternative).
- A List container: grouping headers (sticky), separators or rhythm following DS-014, density following DS-010, keyboard movement with the arrow keys, and virtualisation for long lists.
- A selection model that persists across data refreshes.
- EntryList migrated to a List variant (the tall, fading list over a Stage).

**Examples in LegendsLegacy:**
Inventory stock rows, the Essence Archive list, Prophecy offers, guild members and Chronicle loot lines.

**Paste-ready Claude Design instruction**

```text
Add a Row primitive and a List container to the Grimoire Design System; they will underpin most LegendsLegacy
content (items, Essences, creatures, guild members, rewards, objectives, rankings, log lines). Row has slots.
Leading: an icon, ItemSlot, rank number or checkbox. Primary: the name, using the entity-name text style.
Secondary: meta text. Trailing values: right-aligned tabular numbers. Trailing actions: compact buttons or a ⋯
menu. State markers are placed exactly as the State combinations page specifies. Row variants: static,
navigation (opens detail and shows a chevron), selectable (single selection with the arcana bar), checkable
(multi-select), expandable (reveals a detail region), and draggable (always with a click alternative). List
handles: sticky group headers, separators or rhythm per the Lines rules (never both), density per the Space &
Density page, Up and Down keyboard movement with roving focus, virtualisation for long lists, and a selection
model that survives data refreshes without moving the list. Migrate the existing EntryList into a List variant
("stage list", the tall fading list over a Stage) and keep its current behaviour. Preview in Compact and
Standard density: an inventory stock list, the Essence Archive list with Attuned and New states, a guild
member list, and a grouped list of daily and weekly Prophecies.
```

---

#### DS-041 — Data Table

**Category:** Component
**Priority:** Critical
**Depends On:** DS-009, DS-039, DS-040

**Purpose:**
Rankings, guild members, the Bazaar order books, trade history, combat statistics, the permission matrix and the Vault all need true tables with aligned columns, sorting and sticky headers. The audit is explicit: tables are often the right form, and must not become large cards to look more like a game.

**Should Define:**

- Column types: name or entity, numeric, delta, resource amount, status, time and actions. Each has its alignment, width behaviour and text style.
- Header anatomy: label, unit, a sort indicator with direction, and a tooltip explaining the column.
- Sticky header and sticky first column.
- Sorting rules and default sorts.
- Row states: hover, selected, the player's own row highlighted, unavailable, and new.
- Row selection with a bulk-action bar.
- Density modes, loading rows, empty and error states, and a totals row.
- Responsive behaviour: priority columns hide first, then horizontal scrolling with a sticky first column. No card fallback at desktop sizes.
- Pagination versus virtualisation, and a "Jump to my rank" hook.

**Examples in LegendsLegacy:**
The global Leaderboard, guild members, Bazaar buy and sell books, Record of Battle and combat contribution statistics.

**Paste-ready Claude Design instruction**

```text
Add a DataTable component to the Grimoire Design System. LegendsLegacy needs real tables for rankings, guild
members, Bazaar order books, trade history, combat statistics, the guild permission matrix and the Vault, and
they must stay tables (the guardrails forbid turning comparison data into decorative cards). Define column
types, each with alignment, width behaviour and text style: entity (a name plus an optional leading slot),
numeric (right-aligned tabular), delta, resource amount (with icon), status (a single Tag or state word), time
(relative or absolute per the Time rules), and actions (compact buttons or a ⋯ menu). Define the header:
label, optional unit, sort indicator with direction, and an optional explanatory tooltip. Support a sticky
header and a sticky first column; single-column sorting with a documented default per table; row states
(hover, selected, the player's own row highlighted with a distinct but quiet treatment, unavailable, new);
checkbox selection with a bulk-action bar; Compact and Standard density; skeleton loading rows; empty,
filtered-empty and error states; and an optional totals row. Responsive behaviour: hide low-priority columns
first, then scroll horizontally with the sticky first column; never switch to cards at desktop widths. Support
pagination and virtual scrolling, with a hook for "Jump to my rank". Do not use both zebra striping and row
borders. Preview a guild member table (name with presence, role, level, contribution, last seen, actions), a
Leaderboard with the player's own row, and a Bazaar sell book.
```

---

#### DS-042 — Containers: Panel, Section, Card, Folio & Banner

**Category:** Component
**Priority:** High
**Depends On:** DS-004, DS-013, DS-014, DS-016

**Purpose:**
The biggest source of sameness in the current game is enclosure: `ll-panel` 130 times, `ll-card` 82 times, and nesting several levels deep. Grimoire has Panel, Folio, Banner and Stage. What it lacks is an unboxed Section and a strict rule for what earns a box.

**Should Define:**

- A container ladder, from the least enclosure to the most:
  - **Section:** unboxed, with a SectionRule band title. This is the default.
  - **Panel:** bounded, used only when separation from neighbours is needed.
  - **Card:** a bounded object, used only for collectible objects, opponents and activities.
  - **Folio:** the one selected thing.
  - **Banner:** the one identity headline.
- Panel variants: plain and inset (a `ground-deep` well). Anatomy: header (title, aside actions), body and footer.
- Card anatomy: an art-optional region, identity, key values, state and one action.
- The "card test": is this a single thing the player could own, choose or fight? If not, it is not a card.
- Nesting limits, cross-referenced with DS-013.

**Examples in LegendsLegacy:**
Combat Attributes as Sections with Ledgers inside (no panel boxes). An Arena opponent as a Card. The Codex bonus groups as Sections, not nested panels.

**Paste-ready Claude Design instruction**

```text
Define the container system of the Grimoire Design System in Components → Containers, add a new Section
component and a Card component, and revise Panel. The main cause of generic sameness in LegendsLegacy's
current UI is enclosure: panels and cards used over two hundred times, often nested three or four deep. Define
a container ladder from least to most enclosed. Section: unboxed, titled with a SectionRule band, the default
for grouping. Panel: bounded, only when a group must be separated from its neighbours, for example a set of
actions beside content. Card: a bounded object, only for things a player could own, choose or fight (an Arena
opponent, a dungeon, a collectible). Folio: the one selected thing. Banner: the one identity headline. Give
Panel two variants, plain and inset (a ground-deep well), with anatomy of header (title plus aside actions),
body and footer. Give Card an anatomy of art-optional region, identity, key values, one state marker and one
action, and follow the Art-optional contract. Add the "card test" to the documentation. Restate the nesting
limits from Surfaces & Layering, and show a correct and an incorrect nesting example. Update ScreenOverview so
Combat Attributes uses Sections containing Ledgers instead of boxed panels, and show an Arena opponent as a
Card. Update the Panel, Folio and Banner READMEs with when-to-use and when-not-to-use lists.
```

---

#### DS-043 — Tags, Badges, Counters & Markers

**Category:** Component
**Priority:** High
**Depends On:** DS-019, DS-020

**Purpose:**
The audit found badges carrying important meaning (rarity, categories, states) and also badge proliferation that made completed collections look as urgent as required actions. Grimoire's Tag needs clearly separated siblings so each small indicator has one job.

**Should Define:**

- **Tag:** a state word (Equipped, Listed). Static.
- **RarityCode:** the C, UC, R, E, U, L and LG chip, compact and never alone without the rarity colour.
- **CountBadge:** unread or claimable counts, with a cap rule ("9+").
- **Marker:** a diamond or dot that signals attention without a count.
- **KeywordChip:** ability and combat tags such as Slashing, Spells, Arrows and Summon.
- Tone meanings for each type, maximum counts per row, and positions following DS-020.
- The rule that none of these is ever a button (FilterChip is the interactive one).

**Examples in LegendsLegacy:**
"Equipped" in inventory rows, "E" on an Epic slot, "3" on the Prophecies nav item, a diamond on a Soulstone that can be raised, and "Slashing" on an ability.

**Paste-ready Claude Design instruction**

```text
Revise the existing Tag component in the Grimoire Design System and add sibling indicator components so each
small indicator has exactly one job. Tag is a one-to-three-word state label (Equipped, Listed, Borrowed,
Expired), static and never clickable. RarityCode is the compact rarity chip (C, UC, R, E, U, L, LG), always
paired with the rarity colour on the name or edge, never used as the only rarity signal. CountBadge shows
unread or claimable counts, with a cap rule ("9+") and an accessible label ("3 claimable rewards"). Marker is
the diamond or dot for attention without a count, such as the existing "ready" diamond on Sigils and NavRail
items. KeywordChip is for ability and combat tags (Slashing, Blunt, Piercing, Arrows, Spells, Summon). Define
tone meanings for each using the colour allocation (arcana for new, ready or claimable; the status roles only
with words; rarity tones only for RarityCode). Define maximum counts per row and the fixed positions from the
State combinations page. State that none of these is ever interactive; FilterChip is the interactive chip.
Preview an inventory row with a RarityCode and an Equipped Tag, a NavRail item with a CountBadge, a Sigil with
a Marker, and an ability line with KeywordChips.
```

---

#### DS-044 — Progress: Meters & Tracks

**Category:** Component
**Priority:** High
**Depends On:** DS-007, DS-019

**Purpose:**
The audit warns that similar bars must not imply identical stakes: depleting Vigor is not another completion percentage. The game has health and barrier, XP, quest counts, collection completion, upgrade ranks, dungeon Vigor, Tower progress, Prophecy milestones, boss stagger and capacity limits. Meter and Track exist; they need a semantic type system.

**Should Define:**

- Progress types, each with its own visual form:
  - **Depleting resource:** Health, with a barrier overlay, and Vigor. A framed bar, or segmented pips for small integers.
  - **Accumulating progress:** XP and quest counts. A thin line.
  - **Capacity:** inventory slots and tickets. A fill with a cap marker.
  - **Threshold:** stagger and milestone thresholds. Segments with markers.
  - **Completion:** collections. A fraction plus a thin line.
  - **Indeterminate:** loading.
- The rule that numbers are always printed next to the bar (an existing Grimoire rule).
- Tone tokens by type, not by feature.
- Track: completed, current, future and locked steps, reward markers on milestones, and labels.
- Accessible values (`aria-valuenow` and similar) and value-change motion from DS-017.

**Examples in LegendsLegacy:**
Unit health with a barrier, Dungeon Vigor pips, Combat XP, Arena tickets 3 / 5, regional boss stagger, weekly Prophetic Favor milestones and Tower floors.

**Paste-ready Claude Design instruction**

```text
Revise the existing Meter and Track components in the Grimoire Design System into a semantic progress system.
LegendsLegacy shows many different kinds of progress, and similar bars must not imply identical stakes. Define
progress types, each with a distinct visual form. Depleting resource: Health with an optional barrier overlay,
and dungeon Vigor, shown as a framed bar, or as segmented pips for small integers such as Vigor. Accumulating
progress: Combat XP, Essence XP and quest counts, shown as a thin line. Capacity: inventory slots, Arena
tickets 3 / 5, market listings 10 / 10, shown as a fill with a cap marker and a warning state when full.
Threshold: regional boss stagger and milestone thresholds, shown as segments with markers. Completion: Codex
collections, shown as a fraction plus a thin line. Indeterminate: for loading. Keep Grimoire's rule that
numbers always appear next to the bar. Assign tone tokens by progress type, not by feature, and extend the
meter tokens if needed (for example barrier, vigor, capacity). Extend Track with completed, current, future
and locked steps, optional reward markers on milestones, and step labels; it covers Tower floors, quest chain
steps and the weekly Prophetic Favor track. Add accessible value semantics and use the value-change motion
tokens. Preview each type with LegendsLegacy data side by side, demonstrating that depleting Vigor and
accumulating XP look clearly different.
```

---

#### DS-045 — Wayfinding: Breadcrumbs, Back & Pagination

**Category:** Component
**Priority:** Medium
**Depends On:** DS-038

**Purpose:**
Most screens are one or two levels deep and need no breadcrumbs. Some are deeper (tournament, then match, then replay; guild, then buildings, then Treasury; another player's profile), and paged data (rankings, trade history) needs consistent paging. Returning to the exact previous list state matters more than the breadcrumb itself.

**Should Define:**

- Breadcrumbs only at three levels or deeper, with the current page unlinked and truncation in the middle.
- A BackLink for "return to the previous context" ("Back to my profile", "Back to World Tower"), which preserves list, filter and scroll state.
- Pagination for server-paged data: page numbers or next and previous, a results count, and "Jump to my rank".
- "Load more" for feeds and history. No infinite scroll for rankings.

**Examples in LegendsLegacy:**
A tournament match replay, a public guild profile opened from rankings, and Leaderboard pages.

**Paste-ready Claude Design instruction**

```text
Add Breadcrumbs, BackLink and Pagination components to the Grimoire Design System. Breadcrumbs appear only
when a screen is three or more levels deep (for example Colosseum › Tournament Grounds › Match 4 › Replay, or
Guild › Buildings › Treasury). The current page is unlinked, long trails truncate in the middle, and the style
is quiet, using the caption or label style in ink-muted. BackLink is the standard "return to where I was"
control ("Back to my profile", "Back to World Tower", "Back to rankings"). Document that returning must
restore the previous list's filters, sort, selection and scroll position. Pagination is for server-paged data
such as the Leaderboard, Record of Battle and trade history: next and previous with page numbers when the
total is known, a results count, and a "Jump to my rank" action on rankings. Use "Load more" for chronological
feeds; never use infinite scroll for rankings. Keep all three compact and text-first, with no ornament.
Preview a tournament replay header with breadcrumbs, another player's profile with "Back to my profile", and a
Leaderboard footer with pagination and "Jump to my rank".
```

---

#### DS-046 — Toasts & Inline Alerts

**Category:** Component
**Priority:** High
**Depends On:** DS-007, DS-030

**Purpose:**
The game has header indicators, in-feature badges, toasts, an update dialog, the session summary and chat notices, all as urgency channels with no order of precedence. Toasts and inline alerts are the two general feedback components; their limits matter as much as their look.

**Should Define:**

- **Toast:**
  - Severity: success, info, warning, error.
  - Lifecycle: auto-dismiss for success and info; errors persist.
  - One optional action (Retry, View).
  - A maximum of three stacked.
  - A position that avoids the Chronicle and the current-action indicator.
  - Polite or assertive announcement.
- **InlineAlert:**
  - Page level: maintenance, restricted account, reconnecting.
  - Section level: insufficient resources, a warning before a commitment.
  - A tip or callout form, used sparingly.
- What never goes in a toast:
  - Outcomes that need acknowledgement, which use a dialog.
  - Loot, which goes to the Chronicle.
  - The only record of an important event.

**Examples in LegendsLegacy:**
"Listed Ashen Longsword for 1,240 Cinders" (success toast), a failed save with Retry (error toast), the reconnecting banner and a restricted-account notice.

**Paste-ready Claude Design instruction**

```text
Add Toast and InlineAlert components to the Grimoire Design System. Toast has four severities (success, info,
warning, error), each with an icon and words, never colour alone. Success and info auto-dismiss after a set
duration; warnings and errors persist until dismissed. A toast has at most one action (Retry, View, Undo where
the server supports it), at most three stack at once, and it is positioned so it never covers the Chronicle,
the current-action indicator or the committing button. Announce errors assertively and everything else
politely. InlineAlert comes in page-level form (maintenance scheduled, account restricted with the server's
reason, "Reconnecting…"), section-level form (insufficient resources before a purchase, a warning before a
commitment such as "Pending Loot is lost if the party falls"), and a sparing tip or callout form. Document
what never goes in a toast: outcomes the player must acknowledge (use a dialog), loot and routine combat
results (they go to the Chronicle's loot channel), and anything that would otherwise be the only record of an
important event. Use the surface and status tokens without ornament. Preview: a success toast after listing an
item on the Cinder Bazaar, an error toast "Loadout not saved · Retry", a page-level reconnecting alert, and a
section-level warning inside a dungeon run.
```

---

#### DS-047 — Loading, Refreshing & Stale Data

**Category:** Component
**Priority:** High
**Depends On:** DS-017, DS-019

**Purpose:**
The game uses SignalR, domain-version coordination, polling and server time. The audit is explicit that loading, empty, locked, restricted, stale, failed and completed are different states, and that live updates must not disturb an active decision.

**Should Define:**

- **Initial load:** skeletons that match the final layout (rows, Ledgers, cards, tables). No full-page spinners in the stage.
- **Action pending:** handled inside the control (DS-031).
- **Background refresh:** a subtle indicator that keeps the existing content visible.
- **Stale data:** an inline notice ("Updated 5 min ago · Retry") after a failed refresh, keeping the old data.
- **Optimistic updates:** forbidden for currencies, trades and rewards (the server is authoritative), and allowed for preferences.
- **Reconnecting:** a shell-level state.
- A minimum display time to avoid flicker, and reduced-motion skeleton behaviour.

**Examples in LegendsLegacy:**
A ranking refresh failure, a guild roster refreshing during raid muster, bootstrap after login and a chat reconnect.

**Paste-ready Claude Design instruction**

```text
Add Skeleton, RefreshIndicator and StaleNotice components to the Grimoire Design System, and document
data-freshness states in Standards → States. LegendsLegacy is realtime (live updates, polling and server
time), and loading, refreshing, stale, failed and empty must never look alike. Initial load: Skeletons that
match the final layout for Row, Ledger, Card and DataTable, animated subtly and static under reduced motion;
no full-page spinners inside the stage. Action pending is handled by the control itself (Button pending).
Background refresh: a small, quiet RefreshIndicator near the section title; existing content stays visible and
interactive, and selection, filters, scroll and typed input are preserved. Stale data: after a failed refresh,
keep the old data and show a StaleNotice ("Rankings updated 5 min ago · Retry"). Optimistic updates are
forbidden for currencies, trades, rewards and claims, because the server is authoritative, and are allowed for
preferences. Reconnecting is a shell-level state that uses the page-level InlineAlert. Define a minimum
display time so fast responses don't flicker. Preview: a Leaderboard loading with skeleton rows, a guild
roster refreshing during raid muster with the selection preserved, and a stale ranking after a failed refresh.
```

---

#### DS-048 — Empty, Error & Recovery States

**Category:** Component
**Priority:** High
**Depends On:** DS-030, DS-047

**Purpose:**
A blank panel must never look like an empty collection. Failed saves, failed purchases and interrupted redemptions need safe recovery. The game already has bootstrap retry, loadout save failure handling and Nobility retry; the design system should make these consistent.

**Should Define:**

- EmptyState types:
  - First use: how to get started, with a link.
  - Filtered to nothing: Clear filters.
  - All done: calm and positive.
  - Not yet unlocked: the requirement.
  - Not applicable.
- ErrorState types:
  - Load failure: Retry, keeping the old data.
  - Action failure: an inline error at the control, keeping the inputs.
  - Restriction: the server's reason, with no retry.
  - Conflict: the data changed; refresh and review.
  - Offline.
- Recovery rules: a safe retry for uncertain responses, blocking navigation away from a failed save, and never losing typed values.
- Scale: inline, section and full-page variants. No illustrations required (DS-023).

**Examples in LegendsLegacy:**
An empty Soul Archive, an empty "My Orders", "All Prophecies claimed", a failed loadout save, a Bazaar price that changed before the order was placed, and a guild-restricted Vault.

**Paste-ready Claude Design instruction**

```text
Add EmptyState and ErrorState components to the Grimoire Design System, with inline, section and full-page
sizes, text-first and needing no illustration. EmptyState types, each with its own copy pattern from the
Content section. First use: explains how to get started and links to the action, such as an empty Soul Archive
linking to regions where Essences drop. Filtered to nothing: offers "Clear filters". All done: calm and
quietly positive, such as "All Prophecies claimed · Next reset in 5h 12m". Not yet unlocked: states the
requirement. Not applicable. ErrorState types. Load failure: offers Retry and keeps any previously loaded data
visible. Action failure: shown inline at the control, with inputs preserved. Restriction: shows the server's
reason and offers no retry. Conflict: the data changed, such as a Bazaar price moving before the order; the
player can refresh and review with their inputs kept. Offline. Document recovery rules: uncertain responses
retry safely using the same operation (as Nobility redemption already does); navigating away from an unsaved
or failed loadout warns first; typed values are never lost. Make sure an empty collection, a loading
collection and a failed collection can never be confused. Preview each type with LegendsLegacy content,
including a failed Essence loadout save and a restricted guild Vault.
```

---

#### DS-049 — Keyboard Shortcuts & KeyHints

**Category:** Component
**Priority:** Medium
**Depends On:** DS-011, DS-031

**Purpose:**
Grimoire already has key caps on buttons and a KeyHints bar, which comes from console conventions. Power users of a PBBG do benefit from shortcuts, and a possible Steam desktop client makes them more valuable. A permanent hint bar on screens with no real shortcuts is noise.

**Should Define:**

- A shortcut map:
  - Global: open or focus chat, focus search, toggle the rail, open shortcut help ("?").
  - Contextual: equip, compare, claim, next or previous entry.
  - List navigation.
- Scoping: shortcuts are disabled while typing, and scoped to the focused region or the open dialog.
- Conflicts with browser and OS shortcuts.
- Discoverability: key caps on buttons, and a shortcut help overlay.
- The KeyHints bar shown only when a screen has two or more contextual shortcuts, and hidden under 960px.
- Remapping, deferred and noted.

**Examples in LegendsLegacy:**
Up and Down with Enter in the Creature Archive, "E" to equip in Inventory, "C" to compare, and Enter to send in the Chronicle.

**Paste-ready Claude Design instruction**

```text
Add a "Keyboard shortcuts" page to Components in the Grimoire Design System and revise the KeyHints component.
Define a shortcut map for LegendsLegacy. Global: focus chat, focus search, toggle the NavRail, open shortcut
help with "?". Contextual: equip (E), compare (C), claim (Enter on a claimable entry), next and previous entry
(Up and Down). List navigation behaviour. Define scoping: all single-key shortcuts are disabled while typing
in an input; shortcuts are scoped to the focused region or the topmost dialog; browser and OS shortcuts are
never overridden. Define discoverability: key caps on buttons (the existing hotkey input), plus a shortcut
help overlay listing the shortcuts available on the current screen. Change the KeyHints rule so the bar
appears only on screens with at least two contextual shortcuts, never shows global shortcuts, and stays hidden
under 960px as today. Note that shortcut remapping is deferred and would matter more if a desktop client
ships. Preview the Creature Archive with KeyHints (Select ↵, Previous and Next ↑↓, Back Esc), an Inventory
item with key caps on Equip and Compare, and the shortcut help overlay.
```

---

#### DS-050 — Charts & Data Visualization Basics

**Category:** Component
**Priority:** Low
**Depends On:** DS-006, DS-009

**Purpose:**
LegendsLegacy needs only a few chart forms: combat contribution and damage-type breakdown, Bazaar price history, and possibly rating history. Defining just those avoids importing a generic dashboard chart look.

**Should Define:**

- Allowed forms:
  - Horizontal bars for contribution and comparison.
  - A stacked bar for damage by type, using damage hues and labels.
  - A sparkline or line for price and rating history.
- Forbidden forms: pie and donut charts, 3D, gradients and decorative grids.
- Labelling: values printed directly, minimal axes, tooltips, and a table fallback for accessibility.
- Colour: damage and semantic tokens only, and no rarity colours in charts.

**Examples in LegendsLegacy:**
Combat analysis contributions, a raid party damage breakdown and the Bazaar median price history.

**Paste-ready Claude Design instruction**

```text
Add a small Charts page and two chart components (BarChart and Sparkline) to the Grimoire Design System,
covering only what LegendsLegacy needs. Allowed forms: horizontal bars for combat contribution and simple
comparisons; a stacked horizontal bar for damage by type, using the damage-* hues with the type names written;
and a sparkline or simple line for Bazaar price history and rating history. Forbidden forms: pie and donut
charts, 3D, gradients, glow and decorative gridlines. Labelling: print values directly on or beside bars using
tabular numerals, keep axes minimal, show a tooltip for exact values on hover and focus, and provide a table
fallback for screen readers. Colour: only damage and semantic tokens; never rarity colours in charts. Charts
sit inside Sections or Panels in Standard density and never become decorative headers. Preview a combat
contribution chart for a party of three plus a summon, a damage-by-type bar (Physical, Bleed, Shadow), and a
30-day median price sparkline for a Bazaar commodity.
```

---

### Phase 3 — Game Registries & Game Components

#### DS-051 — Rarity System

**Category:** Game Registry
**Priority:** Critical
**Depends On:** DS-006, DS-015, DS-020, DS-043

**Purpose:**
Rarity (Common, Uncommon, Rare, Epic, Unique, Legendary, Legacy) is the strongest learned visual code in the game. The audit warns against copying a reference-style "rarity border on everything" treatment when the game already relies on readable rarity names and colours. A single rarity spec prevents escalating glows and rarity hues leaking onto surfaces.

**Should Define:**

- A registry table: name, code (C, UC, R, E, U, L, LG), token and sort order.
- How rarity is expressed in each context:
  - Rows: name colour and code chip.
  - Slots: edge, code and name colour.
  - Links: a bracketed name in colour.
  - Hover card header: name colour, code and the word ("Epic").
  - Chat: follows ItemLink.
- Where rarity never appears: backgrounds, buttons, panels, whole rows and charts.
- No escalation: higher rarities get no extra glow or animation. At most, one static detail for the top three rarities if needed, applied consistently.
- How to keep Legacy distinct from danger, and Epic distinct from the whisper channel (from DS-006).
- Rarity announced in words for screen readers.
- Which entities carry rarity (equipment, Essences, possibly rewards) and which do not (currencies, conditions).

**Examples in LegendsLegacy:**
Inventory rows, equipment slots, Bazaar listings, Chronicle item links and loot lines, and Essence entries.

**Paste-ready Claude Design instruction**

```text
Add a Rarity page to the Registries section of the Grimoire Design System. Registry table: Common (C),
Uncommon (UC), Rare (R), Epic (E), Unique (U), Legendary (L), Legacy (LG), each with its rarity-* token and
sort order. These hues are learned by players and must not change. Define exactly how rarity is expressed in
each context. Text rows: the name in rarity colour plus a RarityCode chip. ItemSlot: rarity edge, code and
name colour (keep the current treatment). ItemLink: bracketed name in rarity colour. Hover card header: name
colour, RarityCode, and the rarity word ("Epic") in the metadata line. Chronicle: follows ItemLink. List where
rarity colour may never appear: backgrounds, buttons, Panels, whole-row fills, charts and icons. Add a
no-escalation rule: higher rarities get no extra glow, animation or particle effect. If the top rarities need
extra distinction, allow at most one static detail (for example a second hairline on the slot edge), applied
consistently and documented. Reference the collision rules so Legacy never reads as danger and Epic never
reads as the whisper channel. Rarity is always announced as a word for screen readers. List which entities
carry rarity (equipment, Essences, item rewards) and which never do (currencies, conditions, abilities).
Update ItemSlot, ItemLink and Tag to reference this page, and preview all seven rarities in row, slot, link
and hover-card-header form on ground and on folio.
```

---

#### DS-052 — Item Property Hierarchy

**Category:** Game Registry
**Priority:** Critical
**Depends On:** DS-021, DS-026, DS-051

**Purpose:**
Equipment has tier, rarity, quality (Crude, Standard, Fine, Exceptional, Masterpiece), rank, native style or variant, set identity, Gear Value, ownership and binding. The audit's warning is direct: these must not become interchangeable coloured badges. Only rarity owns a colour. Everything else needs a fixed order and form.

**Should Define:**

- A property registry:
  - The eight equipment slots: Head, Chest, Legs, Necklace, Ring, Relic, weapon (one-handed or two-handed) and Off-hand.
  - Tier, quality, rank, style or variant, set, Gear Value, and ownership and binding (owned, borrowed from the guild, guild property, listed, reserved).
- For each property: its display form ("Tier 2" or "T2", the quality word, "Rank 3"), which text style it uses, and whether it is ever coloured. Only rarity is coloured.
- A canonical order in item headers and metadata lines.
- Which properties appear at each disclosure layer: glance, hover card and detail.
- Set display: set name, pieces equipped out of pieces needed ("2 / 4"), and active and inactive bonuses.
- Two-handed weapons occupy both hand slots.

**Examples in LegendsLegacy:**
"Ashen Longsword · E · Tier 2 · Fine · Rank 3". A guild Vault item marked "Guild property · Borrowed by Mira".

**Paste-ready Claude Design instruction**

```text
Add an "Item properties" page to the Registries section of the Grimoire Design System. LegendsLegacy equipment
carries many properties, and a previous audit warned they must not become interchangeable coloured badges.
Registry: the eight equipment slots (Head, Chest, Legs, Necklace, Ring, Relic, weapon as one-handed or
two-handed, Off-hand), tier, rarity, quality (Crude, Standard, Fine, Exceptional, Masterpiece), rank, native
style or variant, set, Gear Value, and ownership and binding states (owned, equipped, borrowed from the guild,
guild property, listed on the Cinder Bazaar, reserved in escrow). For each property define its display form
(for example "Tier 2" in headers and "T2" in compact rows; the quality word written out; "Rank 3"), its text
style, and whether it may use colour. Only rarity uses colour; quality, tier and rank never do. Define the
canonical order for the item header and the compact metadata line, for example: name (rarity colour) ·
RarityCode · slot · Tier · Quality · Rank · Style. Define which properties appear at each disclosure layer
(glance row, hover card, full inspector). Define set display: set name, pieces equipped out of pieces needed
("2 / 4"), and bonuses shown as active or inactive using the State model. Note that a two-handed weapon
occupies both hand slots and state how that appears. Include a preview with three items of different rarity
and property combinations in row, hover-card header and inspector header form, and a guild Vault item borrowed
by another member.
```

---

#### DS-053 — Resource & Currency Registry

**Category:** Game Registry
**Priority:** Critical
**Depends On:** DS-018, DS-026, DS-027

**Purpose:**
Only Cinders and Soulstones have a treatment in Grimoire (CurrencyPill with full-colour art). The game has many system-specific economies, and more will come. A registry is what lets a new currency ship with no new visual work.

**Should Define:**

- The entry schema: ID, name, plural, short label, line icon, optional art, scope, cap, regeneration, tradability, formatting, where it is shown, and a one-line description.
- Scopes:
  - Global: Cinders and Soulstones.
  - Feature: Fate Echo, Sigil Fragments, Prophetic Favor, Glory, Arena tickets, Tower Tokens, raid Trophies.
  - Guild: Guild Favor, Guild XP and Supplies.
  - Item-like: Signets and dungeon Sigils.
  - Run-scoped: Vigor and Pending Loot.
  - Recognition: Renown.
- Placement rules from DS-024: global currencies in the TopBar, feature currencies in the feature's resource header, and run-scoped resources in the activity.
- Capped and regenerating resources: the capacity display and the next-regeneration timer.
- A "verify" flag for entries whose status is uncertain, such as Essence Dust and Monster Cores.
- How to add a new resource (cross-referenced with DS-130).

**Examples in LegendsLegacy:**
Fate Echo on Prophecies (with the reroll cost), Glory and tickets in the Colosseum, Tower Tokens in the Tower shop, and Guild Favor in Guild missions.

**Paste-ready Claude Design instruction**

```text
Add a "Resources and currencies" page to the Registries section of the Grimoire Design System. Today only
Cinders and Soulstones have a treatment (CurrencyPill with full-colour art), but LegendsLegacy has many
system-specific economies and will add more. Define the registry entry schema: id, name, plural, short label,
line icon (from the Iconography taxonomy), optional full-colour art, scope, cap, regeneration, tradability,
number formatting, where it appears, and a one-line description. Populate it with scopes. Global: Cinders,
Soulstones. Feature: Fate Echo (Prophecy rerolls), Sigil Fragments, Prophetic Favor (weekly track), Glory
(Arena), Arena tickets (capped and regenerating), Tower Tokens, raid Trophies. Guild: Guild Favor, Guild XP,
Supplies. Item-like: Signets (Nobility), dungeon Sigils. Run-scoped: Vigor and Pending Loot in dungeon runs.
Recognition: Renown. Mark entries whose current status needs confirmation (Essence Dust, Monster Cores) as
"verify". State the placement rules from the shell contract: global currencies in the TopBar only; feature
currencies in that feature's resource header; run-scoped resources inside the activity; never all resources
everywhere. Define how capped and regenerating resources show capacity and the next-regeneration timer ("3 / 5
· +1 in 42m"). Add a short "adding a new resource" procedure. Use placeholder line icons where final icons
don't exist yet and list them as needed.
```

---

#### DS-054 — Resource Amount & Cost Display

**Category:** Game Component
**Priority:** Critical
**Depends On:** DS-009, DS-019, DS-031, DS-053

**Purpose:**
Costs, balances, prices, fees and rewards all show resource amounts, and affordability decides whether an action is possible. One component family (Amount, CostList and CostButton) replaces ad-hoc currency rendering, and makes CurrencyPill a special case of it.

**Should Define:**

- **Amount:** an icon, a number and an optional name. Sizes: inline, row and header. Full or abbreviated, following DS-027.
- **CostList:** one or more costs, each with an affordability state (affordable, or insufficient with "Need 760 more"). A have/need format ("1,240 / 2,000").
- **CostButton:** a Button with its cost attached. The insufficient state shows unavailable with the reason.
- **PriceBreakdown:** price, fee and net proceeds, and totals for quantity multiplied by unit price.
- "Free" and "No cost" wording.
- CurrencyPill redefined as an Amount in the header size for global currencies.
- The rule that amounts at the point of commitment are never abbreviated.

**Examples in LegendsLegacy:**
A Prophecy reroll ("Reroll · 40 Fate Echo"), a Soulstone upgrade cost, a Bazaar sale showing the fee and net, and a Tower shop purchase that is not affordable.

**Paste-ready Claude Design instruction**

```text
Add Amount, CostList, CostButton and PriceBreakdown components to the Grimoire Design System, all driven by
the Resources registry. Amount shows a resource icon, a number and an optional name, in inline, row and header
sizes, using the Numerals and Numbers-and-units rules. It abbreviates only where allowed and always offers the
full value on hover and focus. CostList shows one or more costs, each with an affordability state: affordable,
or insufficient with the shortfall written ("Need 760 more"), and an optional have/need form ("1,240 / 2,000
Cinders"). CostButton is a Button with its cost attached. When insufficient it shows the
unavailable-with-reason state, never just a greyed button. PriceBreakdown shows unit price × quantity, fee,
total and net proceeds, as used on the Cinder Bazaar. Define "Free" and "No cost" wording. Redefine the
existing CurrencyPill as Amount in header size for global currencies, keeping its current look. State that
amounts at the point of commitment are never abbreviated. Preview LegendsLegacy cases: a Prophecy reroll
button (40 Fate Echo, affordable), a Soulstone upgrade with two costs where one is insufficient, a Tower shop
item priced in Tower Tokens that the player cannot afford, and a Bazaar sale of 25 items at 1,240 Cinders each
with fee and net.
```

---

#### DS-055 — Attribute & Stat Registry

**Category:** Game Registry
**Priority:** Critical
**Depends On:** DS-018, DS-026, DS-027

**Purpose:**
The Character Overview groups attributes into Offense, Defense, Recovery and Utility. The attribute set is versioned: the September redesign introduced Armor Rating, Resistance Rating, Ability Haste, Restoration and Tenacity, with effective caps. Stat display must follow a registry, not hard-coded labels, or every balance change breaks the UI.

**Should Define:**

- The entry schema: ID, name, group, unit, precision, polarity (higher is better, or lower is better), cap, whether it is a rating that converts to an effective percentage, description, emblem point count (for Folio emblems) and icon.
- The four groups, with their order and members as currently shown on the Overview.
- Rating-to-effect pairs (for example Armor Rating to Physical Damage Reduction), and how both appear.
- Version awareness: the registry reflects the active rules version, and retired attributes are marked.
- Derived and estimated stats (for example Threat estimated from attuned Essences), labelled "estimated".
- How to add an attribute.

**Examples in LegendsLegacy:**
Power, Attack Speed, Crit Chance, Max Health, Armor Rating to Physical DR, Tenacity, Restoration and Threat (threat/s, estimated).

**Paste-ready Claude Design instruction**

```text
Add an "Attributes and stats" page to the Registries section of the Grimoire Design System. LegendsLegacy's
attribute set is versioned and recently changed (Armor Rating and Resistance Rating converting to mitigation,
Ability Haste replacing Cooldown Reduction, Restoration, Tenacity, effective caps), so the UI must follow a
registry rather than hard-coded labels. Define the entry schema: id, name, group, unit, precision, polarity
(higher or lower is better), cap if any, whether it is a rating that converts into an effective percentage, a
one-sentence description for the Ledger explanation, an emblem point count for Folio emblems, and an icon
slot. Define the four groups and their order as the Character Overview uses them: Offense, Defense, Recovery,
Utility. Populate the entries with the attributes the game currently shows, and mark any the design team needs
to confirm against the active rules version. Define rating-to-effect pairs (for example Armor Rating →
Physical Damage Reduction) and how both values appear together in a Ledger row, using the existing sub-line
("240 Armor Rating"). Define how derived or estimated stats are labelled, such as Threat estimated from
attuned Essences in threat/s, marked "estimated". Add a rule that retired attributes are kept in the registry,
marked retired and never shown in new UI. Add a short "adding an attribute" procedure. Update ScreenOverview's
attribute Ledgers to draw from this registry.
```

---

#### DS-056 — Stat Row & Stat Block

**Category:** Game Component
**Priority:** High
**Depends On:** DS-021, DS-032, DS-055

**Purpose:**
Ledger is Grimoire's best data component: label, leader, value, sub-line and explanation. Stats also appear inline on cards, in comparisons and with caps. This item makes Ledger the canonical stat display and settles when StatTile, StatFigure, Sigil and LevelPlate are appropriate.

**Should Define:**

- A Ledger extension:
  - A delta slot.
  - An effective or raw value pair with a cap indicator ("62% · capped 60%").
  - A conditional marker for stats that apply only under a condition.
  - A breakdown link (DS-058).
  - A muted state for zero or inactive values.
- **InlineStat:** "Power 1,284", for cards and rows.
- **StatPair:** label, current value and candidate value, for comparison contexts.
- The decision table for choosing a number display:
  - Ledger row: standard stats.
  - StatFigure: the one headline figure (L2).
  - Sigil: mastery or constellation values in a hexagon.
  - LevelPlate: the hero level, once per screen.
  - StatTile: only if the audit keeps it, and never in grids of more than four.
- Values in `ink`, following DS-006, not gilt.

**Examples in LegendsLegacy:**
The Character Overview attributes, the item card stat list, the Soulstone constellation hexes and a combat summary.

**Paste-ready Claude Design instruction**

```text
Extend the existing Ledger component in the Grimoire Design System into the canonical stat display, and add
InlineStat and StatPair. Ledger rows gain: an optional delta slot, following the delta rules; an effective or
raw pair with a cap indicator ("62% · capped at 60%"); a conditional marker for stats that apply only under a
condition ("while above 50% Max Health"); an optional link to the attribute breakdown; and a muted state for
zero or inactive values. Values follow the colour allocation (ink rather than gilt) and the Numerals rules.
Labels, units and precision come from the Attributes registry, so Ledger never formats values itself.
InlineStat is a compact "Power 1,284" form for cards and rows. StatPair shows label, current value and
candidate value for comparisons. Then add a decision table to the Ledger README explaining which number
display to use. Ledger row: standard stats. StatFigure: the single headline figure of a region. Sigil: mastery
and Constellation values. LevelPlate: the hero level, once per screen. StatTile: only as the audit decided,
and never in grids of more than four equal tiles. Update ScreenOverview and ScreenArchive to follow the table.
Preview an Offense Ledger with a capped stat, a conditional stat and a delta; an item card's InlineStat list;
and a StatPair list for a comparison.
```

---

#### DS-057 — Delta & Comparison Indicators

**Category:** Game Component
**Priority:** High
**Depends On:** DS-007, DS-009, DS-027

**Purpose:**
Deltas appear in equipment comparison, upgrades, Ascension previews, combat results, rating changes and ranking movement. Their direction must follow benefit (a lower cooldown is better), and they must stay legible without colour.

**Should Define:**

- **DeltaValue:** glyph, sign, value and unit. Polarity comes from the attribute registry, so direction follows benefit.
- Neutral and unchanged form (◆ or "no change"), and when a delta is hidden.
- Added-stat and removed-stat forms ("new" or "lost").
- A context label ("vs equipped", "vs current rank", "since last week").
- A comparison summary ("4 better · 2 worse · 1 lost set bonus").
- Rank movement in rankings (▲3, ▼1, new).
- Accessible phrasing ("Crit Chance increases by 2.1 percentage points").

**Examples in LegendsLegacy:**
An equipment comparison, a Soulstone rank upgrade, an Arena rating change after a fight, and Leaderboard movement.

**Paste-ready Claude Design instruction**

```text
Add DeltaValue and ComparisonSummary components to the Grimoire Design System. DeltaValue shows a glyph, a
sign, the value and the unit (▲ +2.1%, ▼ −1.2s). Its colour and glyph come from the delta-better and
delta-worse roles, and its polarity comes from the Attributes registry, so a cooldown decreasing counts as
better. Define the unchanged form (◆, or hidden, with the rule for which), an added-stat form ("new"), and a
removed-stat form ("lost"). Add an optional context label ("vs equipped", "vs current rank", "since last
week"). ComparisonSummary condenses a comparison into one line ("4 better · 2 worse · set bonus lost") so a
player can judge an item at a glance before reading the detail. It is never a single "score" that claims one
item is better overall. Add rank movement for rankings (▲3, ▼1, "New"). Define accessible phrasing ("Crit
Chance increases by 2.1 percentage points"). Deltas never rely on colour alone and never animate beyond the
value-change motion token. Preview: an equipment comparison list with mixed deltas including a cooldown
decrease, a Soulstone rank-up preview, an Arena rating change after a win and a loss, and Leaderboard movement
markers.
```

---

#### DS-058 — Attribute Breakdown & Effective Values

**Category:** Game Component
**Priority:** High
**Depends On:** DS-032, DS-056, DS-057

**Purpose:**
Players need to know where a stat comes from: level, equipment by slot, set bonuses, Essence passives, Combat Style, Soulstones and temporary effects. They also need raw against effective values, caps and wasted over-cap budget, which the comparison service already reports. This is the deepest disclosure layer for stats.

**Should Define:**

- A source breakdown: a source label, its contribution and its origin (a slot or Essence name, linked where possible).
- A raw → effective → cap line, including cap waste ("4% over cap, no effect").
- Rating-to-mitigation conversion shown plainly, without formulas unless the lexicon provides a stable one.
- Temporary and combat-only sources, labelled separately.
- Presentation: a hover card from a Ledger row, and a dialog for the full breakdown.
- Other-player context: show only what the server provides.

**Examples in LegendsLegacy:**
Crit Chance from gear, set bonus and Soulstones. Armor Rating 240 → 38% Physical DR. Attack Speed over its weapon-specific cap.

**Paste-ready Claude Design instruction**

```text
Add an AttributeBreakdown component to the Grimoire Design System, opened as a HoverCard from any Ledger row
with a breakdown link, and as a dialog for the full view. It lists each source with its label, contribution
and origin: character level, each equipment slot by item name, set bonuses, Essence passives by Essence name,
Combat Style and refinements, Soulstone upgrades, and temporary or combat-only effects, which are labelled
separately. Link sources to the item or Essence where possible. Below the sources, show the raw total, the
effective value, the cap if any, and cap waste stated plainly ("4% over the cap has no effect"). For
rating-based defences, show the conversion as rating → effective mitigation (240 Armor Rating → 38% Physical
Damage Reduction) without formulas, unless the game's combat lexicon supplies a stable, player-facing one.
When viewing another player, show only what the server provides and state what isn't available. Use Compact
density inside the hover card and Standard in the dialog. Preview a Crit Chance breakdown (gear, 2-piece set
bonus, Soulstones), an Armor Rating conversion, and an Attack Speed breakdown that exceeds its weapon-specific
cap.
```

---

#### DS-059 — Damage Type & Element Semantics

**Category:** Game Registry
**Priority:** High
**Depends On:** DS-006, DS-018

**Purpose:**
Damage types (Physical, Magical, Bleed, Burn, Poison, Shadow, None) are informational colour, not decoration. They appear in ability text, combat numbers, logs and breakdowns. Creatures also have element affinities and defence profiles.

**Should Define:**

- A registry: type, token, icon, word, and which conditions relate to it (for example Bleed and Burn).
- Contexts where the damage hue is allowed: combat numbers, damage-type words in ability text, the log and damage charts. Never elsewhere.
- The rule that the type word or icon always accompanies the hue, with contrast notes (Grimoire already flags bleed on surfaces).
- Element affinity display: "Weak to Burn" or "Resists Shadow" on creatures, with glyphs.
- Critical hit and other damage modifiers: a marker, not a new colour.

**Examples in LegendsLegacy:**
"Deals 140% of Power as Physical damage". Combat log lines. A creature Folio showing weaknesses.

**Paste-ready Claude Design instruction**

```text
Add a "Damage types and elements" page to the Registries section of the Grimoire Design System. Registry:
Physical, Magical, Bleed, Burn, Poison, Shadow, None, each with its damage-* token, a line icon slot, the
display word, and related conditions (Bleed ↔ Bleed condition, Burn ↔ Burn condition, and so on). Define where
damage hues are allowed: combat damage numbers, damage-type words inside ability descriptions, combat log
lines and damage-by-type charts, and nowhere else. The type word or icon always accompanies the hue. Keep
Grimoire's existing contrast note for damage-bleed (set bleed numbers on ground, or bold at 19px and up) and
check the others. Define element affinity display for creatures and bosses ("Weak to Burn", "Resists Shadow"),
using a glyph plus words and never colour alone. Define how critical hits and other modifiers are marked (a
glyph or weight change, never an extra colour). Preview an ability description with two damage types, three
combat log lines, and a creature's affinity summary.
```

---

#### DS-060 — Condition Registry & Condition Chip

**Category:** Game Registry + Game Component
**Priority:** High
**Depends On:** DS-018, DS-028, DS-029, DS-043

**Purpose:**
The combat lexicon defines 29 conditions (28 implemented), classified as beneficial or harmful plus sub-classes (damage over time, control, action denial, threat, reactive, sustain, affliction), with several stacking models (uncapped damage-over-time stacks, intensity stacks, independent duration stacks, unique refresh, charges). Buffs, debuffs and status effects appear in ability text, unit frames, the combat log and tooltips, and new conditions will be added.

**Should Define:**

- A registry mirroring the lexicon: ID, name, classification, sub-class, stacking model, icon and a one-line description. The lexicon is the source of truth.
- **ConditionChip:** icon, name or abbreviation, stack count, intensity, charges and remaining duration, in three sizes:
  - Text: inline keyword.
  - Compact: in unit frames.
  - Detail: in hover cards.
- Beneficial and harmful framing through shape or frame treatment, plus the effect polarity role. Never colour alone.
- Ordering and overflow in unit frames: beneficial first, then harmful, then "+3".
- A ConditionCard hover card: definition, stacking, duration rules and related attributes (such as Tenacity).
- How a new condition is added.

**Examples in LegendsLegacy:**
Bleed ×3 with 4s remaining, Guard with 2 charges, Stun, Doom, Empower and Weaken as unique refreshes, and Soaked as an encounter stack.

**Paste-ready Claude Design instruction**

```text
Add a Conditions registry page and ConditionChip and ConditionCard components to the Grimoire Design System.
LegendsLegacy's combat lexicon is the source of truth: 29 conditions, each classified as beneficial or harmful
with a sub-class (damage over time, control, action denial, threat, reactive, sustain, affliction, encounter
stack), and each following a stacking model (uncapped damage-over-time stacks, intensity stacks, independent
duration stacks, unique refresh, charges). Mirror it in the registry: id, name, classification, sub-class,
stacking model, icon slot and one-line description. ConditionChip comes in three sizes. Text: an inline
keyword inside ability descriptions that opens the ConditionCard. Compact: for unit frames, showing icon,
stack count or intensity, charges and remaining duration. Detail: for hover cards. Distinguish beneficial from
harmful by frame shape or treatment plus the effect-beneficial and effect-harmful roles, never by colour
alone. Define ordering and overflow in unit frames: beneficial first, then harmful, then "+3". ConditionCard
is a HoverCard showing the definition, how it stacks, how its duration behaves, and related attributes (for
example Tenacity's chance to ignore harmful applications). Add a short procedure for adding a condition. Use
placeholder icons where final ones don't exist and list them. Preview Bleed ×3 with 4s remaining, Guard with 2
charges, Stun, Doom, Empower, Weaken and Soaked in all three sizes, and one ConditionCard.
```

---

#### DS-061 — Ability Presentation (Active & Passive)

**Category:** Game Component
**Priority:** High
**Depends On:** DS-029, DS-032, DS-060

**Purpose:**
Essences grant active and passive abilities, and Combat Styles and refinements add behaviour. Abilities appear in LoadoutSlots, the Soul Archive, item cards, the combat viewer and hover cards. The existing Essence description formatter and ability tags should map onto one ability family.

**Should Define:**

- **AbilityRow:** name, an active or passive marker (a word plus a shape), cooldown for actives, KeywordChips and a one-line effect.
- **AbilityCard:** the full description following DS-029, a metadata line, scaling, the source (the Essence or Style that grants it), and an Ascension current → next change.
- An ability icon slot that is art-optional, with a fallback emblem by delivery type.
- Distinguishing active from passive in both text and shape, consistently.
- Cooldown display, reusing DS-062.
- Order-dependent effects (for example Conduit using the first occupied Essence slot) shown explicitly.

**Examples in LegendsLegacy:**
"Active: Alpha Fangs · 8s cooldown", "Passive: Ruthless Instinct", and the Conduit source marker on slot 1.

**Paste-ready Claude Design instruction**

```text
Add AbilityRow and AbilityCard components to the Grimoire Design System. In LegendsLegacy, Essences grant one
active and one passive ability, and Combat Styles and refinements change behaviour. Abilities appear in
LoadoutSlots, the Soul Archive, the combat viewer and hover cards. AbilityRow shows the ability name, an
active or passive marker (a word plus a consistent shape difference, never colour alone), the cooldown for
actives, KeywordChips (Slashing, Spells, Summon and so on) and a one-line effect summary. AbilityCard is the
HoverCard and Folio form: the full description following the Ability-description grammar, with condition
keywords opening ConditionCards and damage words in their damage hue; a metadata line ("Active · 8s
cooldown"); scaling; the source ("Granted by Alpha Wolf Essence"); and, where relevant, an Ascension preview
showing current → next changes with DeltaValues. Include an art-optional ability icon slot with a fallback
emblem by delivery type. Where ability order matters, show it explicitly; for example, Conduit uses the
Essence in the first occupied slot, marked "Conduit source". Update LoadoutSlot to use AbilityRow for its
active and passive lines. Preview an active and a passive ability as rows, a full AbilityCard with a nested
Bleed keyword, and a Conduit-source LoadoutSlot.
```

---

#### DS-062 — Timers, Cooldowns & Countdowns

**Category:** Game Component
**Priority:** High
**Depends On:** DS-028, DS-044

**Purpose:**
Timers show up across the game: ability cooldowns in combat, the Creature Focus cooldown, daily and weekly resets, ticket regeneration, event schedules, expiring claimables, construction timers and offline retention. They need to be informative and never authoritative. When time runs out, the server decides.

**Should Define:**

- **Countdown:** text in the DS-028 formats, with live-updating behaviour and no layout shift.
- **CooldownIndicator:** a combat form (a number and a subtle fill), and an out-of-combat form ("Ready in 1h 42m", or "Ready").
- **ExpiryLabel:** switches to the warning state below a threshold.
- **SchedulePhase:** Upcoming, Live and Ended, each with its time.
- **RegenerationTimer:** capacity plus the time until the next unit ("3 / 5 · +1 in 42m").
- An "Updating…" state when time reaches zero, until the server confirms.
- Reduced motion, and screen-reader behaviour: no per-second announcements.

**Examples in LegendsLegacy:**
Creature Focus, the daily Prophecy reset, Arena tickets, the regional boss schedule, tournament registration and a guild building upgrade.

**Paste-ready Claude Design instruction**

```text
Add Countdown, CooldownIndicator, ExpiryLabel, SchedulePhase and RegenerationTimer components to the Grimoire
Design System, using the Time-and-duration formats. Countdown is live text with reserved width so it never
causes layout shift. CooldownIndicator has a combat form (a remaining-seconds number with a subtle fill on the
ability) and an out-of-combat form ("Ready in 1h 42m", then "Ready" in the arcana ready style). ExpiryLabel
shows "Expires in 3d" and switches to the warning state below the documented threshold. SchedulePhase shows
Upcoming ("Starts in 2h"), Live ("Live · ends in 12m 08s") and Ended ("Ended 2h ago"). RegenerationTimer shows
capacity and the next unit ("3 / 5 Arena tickets · +1 in 42m", or "Full"). When any timer reaches zero, it
shows a brief "Updating…" until the server confirms the new state; timers never unlock actions by themselves.
Screen readers are never told every second; they hear the value on focus and a single announcement on
completion where it matters. Respect reduced motion. Preview LegendsLegacy uses: Creature Focus change
cooldown, the daily Prophecy reset, Arena ticket regeneration, the regional boss schedule in all three phases,
an expiring Prophecy cache, and a guild building upgrade timer.
```

---

#### DS-063 — Levels, Ranks, Tiers & XP

**Category:** Game Component
**Priority:** High
**Depends On:** DS-008, DS-026, DS-044

**Purpose:**
LegendsLegacy has many separate progressions: character level and Combat XP, Essence level (capped by tier) and Ascension tier, Combat Style mastery, equipment rank, Arena rating and standing, guild level, boss rank and Tower floor. The glossary says not to merge them. The visuals must keep them apart too.

**Should Define:**

- **LevelBadge:** an inline "Lv. 48" for rows and names.
- **LevelPlate:** the existing hero form, once per screen.
- **TierMarker:** Essence Ascension tiers 0–3, with the level cap shown ("Lv. 24 / 30").
- Mastery: Sigil plus Meter for Combat Style mastery.
- **RankLabel:** equipment rank, Arena standing or league, and boss rank.
- XP bars labelled with their type ("Combat XP", "Essence XP", "Mastery XP") and "to next level" text, plus a max-level state.
- A distinct form or label for each progression, so two never look interchangeable side by side.

**Examples in LegendsLegacy:**
The Overview header (level and Combat XP), Essence rows (level, cap and tier), a Combat Style panel (mastery), and an Arena opponent row (rating).

**Paste-ready Claude Design instruction**

```text
Add LevelBadge, TierMarker and RankLabel components to the Grimoire Design System, and document how every
LegendsLegacy progression is displayed so no two can be confused. Progressions: character level and Combat XP;
Essence level, capped by Ascension tier (caps 10, 30, 60 and 100 for tiers 0–3); Combat Style mastery;
equipment rank; Arena rating and standing; guild level; boss rank; World Tower floor. LevelBadge is the inline
"Lv. 48" form for names and rows. LevelPlate stays the hero form, once per screen. TierMarker shows Essence
Ascension tier as a compact, consistent mark together with the level and cap ("Lv. 24 / 30"). Mastery uses
Sigil plus Meter. RankLabel covers equipment rank, Arena standing or league, and boss rank, each with its word
so the meanings don't blur. XP Meters are always labelled with their type ("Combat XP", "Essence XP", "Mastery
XP") and show "to next level" text, with a max-level state ("Max level"). Add a side-by-side preview proving
that character level, Essence level with tier, Combat Style mastery and equipment rank are distinguishable in
one row of the Character Overview.
```

---

#### DS-064 — Requirements & Eligibility

**Category:** Game Component
**Priority:** Critical
**Depends On:** DS-019, DS-030

**Purpose:**
Access in LegendsLegacy is dynamic: journey stage, level thresholds, floors cleared, resources, permissions, party size, time windows, account binding and server restrictions. The audit warns against enticing players with unusable actions without an explanation. Every gated action needs its requirement next to it.

**Should Define:**

- **RequirementList:** each requirement shows met or unmet with a glyph and words, plus current against required values ("Level 18 / 20").
- **RequirementInline:** a single requirement next to an action.
- **EligibilitySummary:** "Ready to enter", or "2 requirements not met" with the list expanded.
- Requirement types: level, progression, resource, item or Sigil, role or permission, party size, time window, account (for example a bound account for Nobility) and server restriction.
- Server restriction reasons shown as given, and never presented as something the player can fix.
- The rule that the server remains authoritative; client checks are only a preview.

**Examples in LegendsLegacy:**
Dungeon entry (Sigil, level, previous difficulty cleared), raid sign-up, a guild action for officers only, and Nobility redemption on a guest account.

**Paste-ready Claude Design instruction**

```text
Add RequirementList, RequirementInline and EligibilitySummary components to the Grimoire Design System.
LegendsLegacy gates actions by journey stage, level, progression (for example "Floor 10 cleared"), resources,
items (dungeon Sigils), guild role or permission, party size, time windows, account status (Nobility needs a
bound account) and server-side restrictions. RequirementList shows each requirement as met or unmet, with a
glyph and words (never colour alone) and current against required values ("Level 18 / 20", "Sigil 0 / 1").
RequirementInline shows the single blocking requirement directly beside the action. EligibilitySummary heads a
preparation panel: "Ready to enter", or "2 requirements not met" with the list expanded. Server restriction
reasons are shown as given, visually distinct from fixable requirements, and never suggest a fix the player
can't make. Document that client-side checks are only a preview and the server stays authoritative. Wire the
unavailable-with-reason Button state to use RequirementInline. Preview: dungeon entry with one unmet
requirement, raid sign-up requiring a party of three, an officers-only guild action, and Nobility redemption
blocked on a guest account with the binding requirement.
```

---

#### DS-065 — Locked Content & Unlock Preview

**Category:** Game Component
**Priority:** High
**Depends On:** DS-023, DS-064

**Purpose:**
Locked Essence slots, locked navigation destinations, locked Combat Style refinements, locked dungeon difficulties, locked Tower floors and future Soulstone nodes all need one treatment. The design system must decide when locked content is shown as a teaser and when it is hidden until relevant.

**Should Define:**

- **LockedState** for slots, rows, cards and nav items: dimmed identity, a lock glyph, and the unlock condition in words. Not focusable, or focusable only to read the reason (decide and record).
- **UnlockPreview:** "Unlocks at level 20: a 4th Essence slot". It shows what is gained, not only that something is locked.
- A teaser policy: show the next one or two unlocks. Hide distant content, or collapse it into "More unlocks later".
- The distinction between locked (known, gated) and undiscovered (unknown).
- Never showing a locked action as though it were available.

**Examples in LegendsLegacy:**
Locked LoadoutSlots, NavRail items gated by the First Steps journey, locked Combat Style refinements and future Tower floors.

**Paste-ready Claude Design instruction**

```text
Add a LockedState treatment and an UnlockPreview component to the Grimoire Design System, and apply them to
LoadoutSlot, ItemSlot, Row, Card and NavRail items. A locked element keeps its identity visible but dimmed
(ink-disabled), shows a lock glyph, and always states its unlock condition in words ("Unlocks at level 20").
Decide whether locked elements are focusable so keyboard users can read the reason, and record the decision.
UnlockPreview states what the player gains ("Level 20: a 4th Essence slot", "Floor 15: the Ember Guardian"),
not only that something is locked. Define a teaser policy: show the next one or two unlocks in context; hide
distant content or collapse it into "More unlocks later"; never tease features the player cannot reach in the
current journey stage, which LegendsLegacy controls through First Steps and level thresholds. Define the
difference between locked (known and gated, with a lock) and undiscovered (unknown, handled by the knowledge
states). Never render a locked action so it looks available. Preview a locked fourth Essence slot, a locked
NavRail destination during First Steps, a locked Combat Style refinement, and the next two Tower floors.
```

---

#### DS-066 — Player Identity & Social Rows

**Category:** Game Component
**Priority:** High
**Depends On:** DS-026, DS-040

**Purpose:**
The current `CharacterTag` is one of the most reused components (chat, guild, rankings, parties, inspection), and the audit calls that reuse beneficial. The Nobility specification adds strict display rules: at most one membership mark, never changing prominence or readability, and kept distinct from earned titles and moderator status.

**Should Define:**

- **PlayerName:** inline. Name, the optional Nobility ◆ (when "Display Nobility" is on), and a click or keyboard target that opens the player menu.
- **PlayerPlate:** a row. Name, level, displayed title, guild tag, presence, and an optional role.
- **PlayerHeader:** a profile. Larger identity with the title and guild, following the art-optional contract.
- Guild roles (Guild Master, Officer, Member) and permissions as words.
- A moderator or staff mark, distinct from Nobility.
- Highlighting the player's own name in lists and chat.
- The Nobility rules: one mark only, no border, no text badge, and no change to ranking placement, message prominence or rarity colours.

**Examples in LegendsLegacy:**
Chronicle speaker names, Leaderboard rows, guild members, raid party slots and the other-player profile.

**Paste-ready Claude Design instruction**

```text
Add PlayerName, PlayerPlate and PlayerHeader components to the Grimoire Design System, replacing the game's
CharacterTag everywhere it appears (Chronicle, guild lists, rankings, parties, profiles). PlayerName is
inline: the character name, the optional Nobility ◆ mark before it when the player's "Display Nobility"
setting is on, and a click and keyboard target that opens the standard player menu (Whisper, Inspect, Invite).
PlayerPlate is the row form: name, LevelBadge, displayed title, guild tag, Presence, and an optional guild
role word. PlayerHeader is the profile form, larger, following the Art-optional contract with no portrait
required. Show guild roles (Guild Master, Officer, Member) as words. Add a moderator or staff mark that is
visually distinct from the Nobility mark. Highlight the player's own name quietly in lists and chat. Apply the
Nobility display rules strictly: at most one membership mark beside a name; no border, text badge or special
profile header; it never changes ranking placement, message prominence, rarity colours or readability; it
stays distinct from earned titles, achievements, competitive rank and moderator status. Preview a Chronicle
line, a Leaderboard row with the player's own row, a guild member row with role and presence, and another
player's profile header with a title and guild.
```

---

#### DS-067 — Portrait & Emblem Frames

**Category:** Game Component
**Priority:** Medium
**Depends On:** DS-015, DS-023

**Purpose:**
Even with text-first defaults, some contexts benefit from a visual anchor: the profile, a creature Folio, a Guardian, a boss unit frame or a guild emblem. The frames need sizes, shapes and fallbacks defined once, so art can arrive later without redesigns.

**Should Define:**

- A size scale, from inline up to hero.
- The frame shape per entity class, following DS-015: player, creature, Essence, Guardian or boss, NPC and guild.
- The fallback chain: art, then an Emblem (star polygon, existing), then a monogram, then nothing, as DS-023 describes.
- Rarity treatment for Essence frames: the edge only, as DS-051 describes.
- Boss and Guardian frames: one static distinction, with no glow.
- Crop and safe-area references for DS-126.

**Examples in LegendsLegacy:**
A creature Folio emblem, a Tower Guardian entry, a boss unit frame, a guild emblem and the player profile.

**Paste-ready Claude Design instruction**

```text
Add a PortraitFrame component to the Grimoire Design System that works with or without artwork. Define a size
scale from inline to hero, and a frame shape per entity class consistent with the Shape page: player,
creature, Essence, Guardian or boss, NPC, guild. Follow the Art-optional contract: artwork when available,
otherwise the existing Emblem (a star polygon with a point count per entity family), otherwise a monogram,
otherwise omit the frame entirely with the layout adapting cleanly. Essence frames may carry a rarity edge as
the Rarity page allows, and nothing more. Boss and Guardian frames get one static distinction (for example a
heavier or double edge) and never a glow. Document crop and safe-area requirements for future assets so they
can be referenced by the asset specifications. Preview every size and entity class in the art-free fallback
forms (Emblem and monogram), plus one example using an existing Cards asset as placeholder art, clearly
labelled as placeholder.
```

---

#### DS-068 — Item Slot & Item Row

**Category:** Game Component
**Priority:** Critical
**Depends On:** DS-020, DS-023, DS-040, DS-051, DS-052

**Purpose:**
Items are the most-rendered game objects: inventory, equipment, Vault, Bazaar, rewards, loot, quests and shops. ItemSlot exists and is image-first. The current direction is text-first equipment, so the text row needs to be the primary form, with the slot kept for grids and equipment layouts.

**Should Define:**

- **ItemRow:** Row plus item identity. Name in rarity colour, RarityCode, a compact property line (DS-052), quantity, state markers in fixed positions, and trailing values such as Gear Value or price.
- **ItemSlot:** revised. Art-optional, text fallback, quantity, marker positions, and sm, md and lg sizes.
- **EquipmentSlot:** an ItemSlot or ItemRow bound to one of the eight slots, with empty ("Off-hand · empty"), locked and two-handed spanning states.
- Drag-target states (valid and invalid drop), with a click alternative.
- Stack and quantity display.
- The rule that each row form has one Tag at most (DS-020).

**Examples in LegendsLegacy:**
The inventory gear list, the equipment panel, loot lines, Bazaar listings, quest rewards and the guild Vault.

**Paste-ready Claude Design instruction**

```text
Add ItemRow and EquipmentSlot components to the Grimoire Design System and revise ItemSlot, applying the
Rarity, Item-properties, State-combinations and Art-optional pages. ItemRow is the primary item form, given
LegendsLegacy's text-first equipment direction. It is built on Row: item name in rarity colour, RarityCode, a
compact property line (slot · Tier · Quality · Rank), quantity for stackables, state markers in the fixed
positions (identity, selection, ownership, attention), and trailing values such as Gear Value or price. At
most one Tag. ItemSlot keeps its square form for grids and the equipment layout. It becomes art-optional with
a text fallback, gains documented marker positions, and supports sm, md and lg. EquipmentSlot binds an
ItemSlot or ItemRow to one of the eight slots (Head, Chest, Legs, Necklace, Ring, Relic, weapon, Off-hand). It
supports empty ("Off-hand · empty"), locked, and a two-handed weapon spanning both hand slots. Add drag-target
states (valid drop, invalid drop with a reason), always with a click-to-assign alternative. Preview: an
inventory gear list in Compact density with equipped, new, favourite and listed items; the eight-slot
equipment panel with one empty slot and a two-handed weapon; and a loot line in the Chronicle.
```

---

#### DS-069 — Item Inspection Card

**Category:** Game Component
**Priority:** Critical
**Depends On:** DS-032, DS-052, DS-056, DS-057, DS-068

**Purpose:**
The item card is the most-used hover card in the game. It needs a fixed anatomy so players learn to read it once. It must work as a hover card, as the full inspector with actions, and as a chat item-link preview.

**Should Define:**

- Header: name in rarity colour, RarityCode, the rarity word, slot, tier, quality, rank and style, following DS-052.
- Gear Value, and the stat allocation (core and specialty, where the game exposes them).
- A set section: set name, "2 / 4", and bonuses with active and inactive states.
- Ownership and binding: equipped, borrowed, guild property, listed, reserved, favourite.
- A comparison block against the equipped item (DeltaValues, ComparisonSummary), toggled by a modifier key ("Shift to compare").
- Optional lore, placed last.
- Variants: hover card (no actions), inspector (with actions), and chat link preview (compact).
- Loading and stale states for asynchronous comparison.

**Examples in LegendsLegacy:**
Inventory hover, the Bazaar listing inspector, a Vault borrow view and an item linked in the Chronicle.

**Paste-ready Claude Design instruction**

```text
Add an ItemCard component to the Grimoire Design System, the standard item inspection surface in
LegendsLegacy, built on HoverCard. Define a fixed anatomy so players learn to read it once. Header: name in
rarity colour, RarityCode, then a metadata line with the rarity word, slot, Tier, Quality, Rank and Style, in
the order the Item-properties page sets. Summary: Gear Value. Stats: a Ledger or InlineStat list following the
Attributes registry, grouping core and specialty stats where the game exposes that allocation. Set section:
set name, pieces equipped ("2 / 4"), and each bonus shown active or inactive. Ownership: equipped, borrowed
from the guild, guild property, listed on the Bazaar, reserved in escrow, favourite. Comparison: DeltaValues
against the currently equipped item plus a ComparisonSummary line, toggled by "Shift to compare". Lore: an
optional last line in the lore style. Define three variants. Hover card: no actions. Inspector: the full form
with actions (Equip, Compare, List, Favourite, Donate) in the Folio or an inspector pane. Link preview:
compact, for Chronicle item links. Comparison data is asynchronous, so define loading and stale states.
Preview an Epic two-handed weapon compared against equipped gear, a Rare ring with a partially complete set,
and a borrowed guild Vault item.
```

---

#### DS-070 — Equipment Comparison

**Category:** Game Component
**Priority:** High
**Depends On:** DS-057, DS-069

**Purpose:**
The server compares the complete resulting loadout, not just two items. It accounts for hand replacement, set threshold changes, raw against effective values, caps and unused budget, and it can compare in the context of a specific activity loadout. The audit lists fast, honest comparison first among the things that must not be lost.

**Should Define:**

- **ComparisonTable:** stat, current, candidate and delta, grouped by attribute group, with changed rows first and unchanged rows collapsible.
- Headers for the current item and the candidate item, with a two-handed replacement note ("Replaces Main hand and Off-hand").
- Set impact lines ("Breaks Ashen 4-piece bonus", "Completes Wolfsbane 2-piece").
- Cap effects: effective change against raw change, and budget wasted over a cap.
- An activity context selector ("Compare for: Arena defence loadout").
- A ComparisonSummary header, with no single overall verdict score.
- Loading and stale states, cancelled when the selection changes.

**Examples in LegendsLegacy:**
The inventory inspector, a Bazaar exact-item listing and a guild Vault borrow decision.

**Paste-ready Claude Design instruction**

```text
Add an EquipmentComparison component to the Grimoire Design System. In LegendsLegacy the server compares the
complete resulting loadout, not just two items. It accounts for hand replacement, set threshold changes, raw
against effective values, caps and wasted budget, and it can compare in the context of a specific activity
loadout. Build: a header pair showing the current and candidate items (ItemRow form), with a note when a
two-handed weapon replaces both hands; a ComparisonSummary line ("4 better · 2 worse · set bonus lost"), never
a single verdict score; a ComparisonTable grouped by attribute group, with columns for stat, current,
candidate and DeltaValue, changed rows first and unchanged rows collapsed behind "Show unchanged"; set impact
lines ("Breaks Ashen 4-piece bonus", "Completes Wolfsbane 2-piece"); cap effects shown as effective against
raw change, with wasted budget stated ("+3% Attack Speed, 2% over the weapon cap"); and an activity context
selector ("Compare for: Current build / Arena defence / Tower expedition"). Comparison data is asynchronous,
so include loading, stale and error-with-retry states, and cancel when the selection changes. Use Compact
density. Preview a one-handed weapon versus the equipped one, a two-handed candidate replacing main hand and
off-hand, and a ring that breaks a set bonus.
```

---

#### DS-071 — Essence Presentation & Loadout Slot

**Category:** Game Component
**Priority:** High
**Depends On:** DS-061, DS-063, DS-068

**Purpose:**
An Essence has several forms: an unbound inventory item (to absorb, shatter or trade), an archived Essence (with level, XP, Ascension tier, abilities and favourite status), an attuned Essence in a numbered loadout slot, and a Codex member. The audit notes that owning a drop differs from owning its archived progression, and that slot order is mechanically meaningful.

**Should Define:**

- **EssenceRow:** name, rarity if applicable, level against cap with the TierMarker, active and passive ability names, a favourite marker, and "Attuned · slot 2".
- **EssenceItem:** the unbound inventory form, with Absorb and Shatter actions, visibly different from an archived Essence.
- **LoadoutSlot** revised: slot number meaning, the Conduit source marker, and the attuned, open and locked states (existing) plus assigning and drag.
- **EssenceFolio** content: source creature and habitat, abilities, and an Ascension preview. This is a pattern of existing parts, not a new container.
- Preset scope labels ("Essence preset", from DS-079).

**Examples in LegendsLegacy:**
The Soul Archive list, the Absorb tab, the Character Overview loadout and the Essence preset editor.

**Paste-ready Claude Design instruction**

```text
Add EssenceRow and EssenceItem components to the Grimoire Design System and revise LoadoutSlot. In
LegendsLegacy an Essence exists in several forms that must not look interchangeable. First, an unbound Essence
item in the inventory that can be absorbed, shattered or traded. Second, an archived Essence with level, XP,
Ascension tier, active and passive abilities and favourite status. Third, an attuned Essence occupying a
numbered loadout slot. Fourth, a member of an Essence Codex collection. EssenceRow shows the name, LevelBadge
with cap and TierMarker ("Lv. 24 / 30"), active and passive ability names as AbilityRows in compact form, a
favourite marker, and "Attuned · slot 2" when attuned. EssenceItem is the unbound inventory form: an ItemRow
with Absorb and Shatter actions and a clear "Not yet absorbed" distinction from archived Essences. LoadoutSlot
keeps its attuned, open and locked states and adds: explicit slot numbers, because order is mechanical; the
"Conduit source" marker on the first occupied slot when the Conduit Combat Style is equipped; assigning and
drag states with a click alternative; and the Essence preset scope label. Document the Essence Folio content
as a composition of existing parts (source creature and habitat, AbilityCards, Ascension current → next
preview with requirements). Preview the Soul Archive list, an unbound Essence in inventory, and a three-slot
loadout with Conduit active.
```

---

#### DS-072 — Creature & Monster Entry

**Category:** Game Component
**Priority:** High
**Depends On:** DS-059, DS-064, DS-067

**Purpose:**
Creatures appear in the Creature Archive, region area lists, the Codex, Creature Focus, dungeon encounters and Tower Guardians. The audit asks for the relationship between a creature, where it lives, its drop opportunity and the Focus cooldown to be visible, and for "known compared with missing" to drive discovery.

**Should Define:**

- **CreatureRow:** name, archetype, boss rank, habitat areas, Essence drop, kill count, and discovered or undiscovered state. An undiscovered creature shows no name, only "Undiscovered" with a hint such as its habitat, if the game allows.
- A Creature Focus state and cooldown on the row.
- Creature Folio content: archetype, affinities, abilities, habitats (linked), drops and Codex membership.
- An enemy group preview for areas and encounters ("Wolf ×3, Alpha Wolf").
- Boss and Guardian variants.

**Examples in LegendsLegacy:**
The Creature Archive, the Shenic area enemy list, the Focus target, the Codex collection members and a Tower Guardian.

**Paste-ready Claude Design instruction**

```text
Add CreatureRow and EnemyGroup components to the Grimoire Design System and document the creature Folio
composition. CreatureRow shows name, archetype, boss rank (using RankLabel), habitat areas, the Essence it
drops, kill count, and the knowledge state. Undiscovered creatures show "Undiscovered" instead of a name, plus
only the hints the game allows (such as the habitat), never a fake silhouette unless art exists. A creature
that is the current Creature Focus target shows the Focus marker and the change cooldown ("Focus · change in
1h 42m"), following the State combinations rules. The creature Folio composition shows archetype, element
affinities ("Weak to Burn"), abilities as AbilityRows, habitats linked to their regions and areas, drops, and
Codex collection membership. EnemyGroup is a compact preview for areas and encounters ("Wolf ×3 · Alpha
Wolf"). Add boss and Guardian variants using PortraitFrame fallbacks. Keep everything text-first. Preview the
Creature Archive list with discovered, undiscovered and Focus creatures, an area's EnemyGroup in Shenic, and a
Tower Guardian entry.
```

---

#### DS-073 — Reward Display & Bundles

**Category:** Game Component
**Priority:** Critical
**Depends On:** DS-019, DS-054, DS-068

**Purpose:**
Rewards appear in quests, Prophecies, dungeons, the Tower (First Clear compared with Echo), raids, achievements, events, shops and combat results. The audit warns against flattening reward states and against mixing personal with community entitlement. Rewards are one of the few places a game can feel generous, but they must stay honest.

**Should Define:**

- **RewardItem:** an item (ItemRow or ItemSlot) or a resource (Amount), with quantity.
- **RewardBundle:** a grouped list (resources first, then items, or another fixed order), with overflow ("+4 more") into a hover card.
- Certainty: guaranteed, chance ("12% chance"), and "one of" choice rewards.
- Qualifiers: First Clear bonus, repeat or Echo, personal against community threshold, and a Nobility or other modifier where one exists.
- Preview ("Rewards") against received ("You received").
- No shine, sparkle or rarity glow. Celebration belongs to the reveal flow (DS-095) and stays within the motion budget.

**Examples in LegendsLegacy:**
The quest reward preview, dungeon loot, a Tower First Clear reward, a community event threshold and a branching quest choice.

**Paste-ready Claude Design instruction**

```text
Add RewardItem and RewardBundle components to the Grimoire Design System. RewardItem shows a single reward: an
item (ItemRow or ItemSlot) or a resource (Amount) with its quantity. RewardBundle is a grouped list with a
fixed order (resources first, then items by rarity, or another order you justify), and overflow into a
HoverCard ("+4 more"). Define reward certainty: guaranteed; chance ("12% chance"); and choice ("Choose 1 of
3"), for branching quest rewards. Define qualifiers that must be visible when they apply: First Clear bonus as
opposed to repeat or Echo rewards in the World Tower; personal as opposed to community threshold rewards in
community events; and any modifier that changes the amount. Define a preview form ("Rewards") and a received
form ("You received") that are clearly different. Rewards must not use shine, sparkle, animated glow or rarity
glow; celebration belongs to the reward reveal flow and stays within the motion budget. Preview a quest reward
preview with resources and an item, a branching "Choose 1 of 3" reward, a Tower floor showing First Clear and
Echo rewards side by side, and a community event with personal and shared thresholds.
```

---

#### DS-074 — Reward Lifecycle & Claim States

**Category:** Game Component
**Priority:** High
**Depends On:** DS-019, DS-031, DS-073

**Purpose:**
The audit lists distinct reward lifecycles as something that must not be lost: available, accepted, completed, claimable, claimed and opened, personal against community, and safe against carried dungeon rewards. Claiming happens in many features, so it needs one grammar.

**Should Define:**

- Lifecycle states, each with its visual and wording: locked, available, accepted or in progress, completed but not claimable yet, claimable, claiming (pending), claimed, opened (for caches), expired and forfeited.
- **ClaimButton** (with a count) and **ClaimAll**, with a result summary.
- Expiring claimables: when and how urgency appears.
- Safe against at-risk rewards: secured loot compared with Pending Loot.
- Claim location: inline in the list where the reward lives, never forcing a separate page.
- The attention cascade (DS-088): claimable rewards are the main source of "attention".

**Examples in LegendsLegacy:**
A Prophecy cache (claim, then open), quest completion, achievement rewards, Pending Loot compared with secured loot in dungeons, and guild mission rewards.

**Paste-ready Claude Design instruction**

```text
Document the reward lifecycle in the Grimoire Design System and add ClaimButton and ClaimAll components.
Define every lifecycle state with its visual treatment (using the State model channels) and exact wording:
locked; available; accepted or in progress; completed but not yet claimable; claimable (the arcana ready
style, the main source of attention markers); claiming (pending); claimed (calm, receding); opened (for caches
that are claimed and then opened, as Prophecy caches are); expired; and forfeited (for example Pending Loot
lost on dungeon failure). ClaimButton shows the count when claiming several ("Claim 3"), goes pending while
the server processes the claim, and on success hands off to a result summary. ClaimAll claims everything
claimable in a list and summarises the results in one place, flagging anything that failed. Expiring
claimables show ExpiryLabel and enter the warning state near expiry. Distinguish secured rewards from at-risk
rewards, meaning dungeon Pending Loot that is lost on failure and secured on retreat, and never style them the
same. Claims happen inline where the reward lives; never force a trip to a separate page. Preview a Prophecy
list with claimable, claimed and opened caches, an achievement with a claimable reward, and a dungeon run
summary showing secured loot against Pending Loot.
```

---

#### DS-075 — Objective Entry (Quest, Prophecy, Event Goal)

**Category:** Game Component
**Priority:** High
**Depends On:** DS-062, DS-064, DS-074

**Purpose:**
Quests (branching chains, direct objectives), Prophecies (three daily offers, reroll with Fate Echo, a weekly Favor track, caches), community events (shared thresholds and personal contribution) and the pinned objective in the shell are all objectives with different lifecycles. The audit found that similar frames obscured their differences.

**Should Define:**

- **ObjectiveRow:** title, objective text with progress ("12 / 20 Wolves"), a destination link ("Go to Wolfsbane Reach"), a reward preview, expiry and lifecycle state.
- Variants, each structurally distinct:
  - Quest step: chain position "3 / 7" and branch indicators.
  - Prophecy offer: a choice among three, accept, and reroll with its cost.
  - Community goal: a shared threshold plus your contribution.
  - Pinned objective: the compact shell form.
- Accepted against offered states.
- Completion hand-off to the claim states.

**Examples in LegendsLegacy:**
The Quest Journal, daily Prophecies, community events and the pinned First Steps objective.

**Paste-ready Claude Design instruction**

```text
Add an ObjectiveRow component with four structurally distinct variants to the Grimoire Design System. The
shared anatomy is a title, objective text with progress ("12 / 20 Wolves defeated"), a destination link that
takes the player to where the objective is completed ("Go to Wolfsbane Reach"), a compact RewardBundle
preview, an optional ExpiryLabel, and the lifecycle state from the reward lifecycle. Variant 1, Quest step:
shows chain position ("Step 3 / 7") and branch points where the player chooses a path, with the choice's
consequence. Variant 2, Prophecy offer: part of a set of three daily offers the player chooses from, with
Accept, and a set-wide reroll using a CostButton ("Reroll offers · 40 Fate Echo", or "Free reroll"), plus
accepted and offered states. Variant 3, Community goal: a shared progress threshold with the player's own
contribution shown separately, making clear which rewards are personal and which are shared. Variant 4, Pinned
objective: the compact form shown in the shell for First Steps and pinned quests. Make the variants visibly
different in structure, so a Prophecy doesn't look like a quest step with a different label, as the sameness
test requires. Preview each variant in accepted, in-progress and claimable states, including the three daily
Prophecy offers with a reroll.
```

---

#### DS-076 — Achievements, Titles & Collection Progress

**Category:** Game Component
**Priority:** Medium
**Depends On:** DS-044, DS-066, DS-074

**Purpose:**
Achievements are durable accomplishments that award Renown. Titles are public identity. Codex collections grant bonuses at completion. The audit found them rendered as generic progress cards. They need recognition-oriented forms that differ from quests and upgrades.

**Should Define:**

- **AchievementRow:** name, what was done, progress for incomplete achievements, the Renown value, the earned date, and the claimable reward.
- **TitleRow:** title text as it displays, its source, "Displayed" or "Display" as the action, and a locked state with its unlock condition.
- **CollectionProgress:** members as known, absorbed and ascended (distinct counts, not one "collected" count), the bonus at completion, and the next missing member.
- Category summaries: one line per category, not a grid of stat tiles.
- Recognition emphasis for earned items: quiet but distinct. No trophies or confetti.

**Examples in LegendsLegacy:**
The Achievements page, Title selection, Essence Codex collections and World Tower titles.

**Paste-ready Claude Design instruction**

```text
Add AchievementRow, TitleRow and CollectionProgress components to the Grimoire Design System, designed for
recognition rather than generic progress. AchievementRow: the achievement name, what was accomplished in one
line, progress for incomplete ones, its Renown value, the earned date for completed ones, and a claimable
reward if any. TitleRow: the title text exactly as it displays beside a name, its source ("World Tower · Floor
20 first clear"), a "Display" action or a "Displayed" state, and a locked state with the unlock condition.
CollectionProgress, for Essence Codex collections: distinct counts for discovered, absorbed and ascended
members (never a single "collected" number), the bonus granted on completion shown active or inactive, and the
next missing member with a hint where to find it. Category summaries are a single line per category ("Combat ·
34 / 50 · 1,240 Renown"), not a grid of stat tiles. Earned recognition is quiet but distinct, using the lore
register for one-line flavour, and never trophies, confetti or glow. Make sure these components pass the
sameness test against ObjectiveRow and the upgrade preview. Preview an Achievements category list, a title
selection list, and a Codex collection with mixed member states.
```

---

#### DS-077 — Activity Card

**Category:** Game Component
**Priority:** High
**Depends On:** DS-023, DS-042, DS-054, DS-064, DS-073

**Purpose:**
Regions, combat areas, dungeons and difficulties, encounters, Tower floors, raids and events are all selectable activities with the same questions: what is it, can I do it, what does it cost, and what do I get. One Activity Card, with Row and Card forms and type variants, prevents a bespoke card per system. It also avoids the audit's "every area uses the same art tile" problem.

**Should Define:**

- Anatomy:
  - Identity: name, type and region.
  - Recommended level or Combat Rating.
  - EnemyGroup preview.
  - Entry cost (tickets, Sigils, Vigor).
  - A reward preview.
  - Status: locked, available, active now, completed, First Clear available, or on cooldown.
  - One action.
- Row and Card forms. Card only when the list is short and art exists (DS-023).
- Type variants: area, dungeon (difficulty and mastery), Tower floor (Guardian, First Clear or Echo), raid, and scheduled event (SchedulePhase).
- The current activity (idle combat running here) marked.
- Identity through data (enemies, rewards, requirement), not shared art.

**Examples in LegendsLegacy:**
Shenic and Meran area lists, dungeon difficulty selection, Tower floors, the raid preview and the regional boss.

**Paste-ready Claude Design instruction**

```text
Add an ActivityCard component with Row and Card forms and type variants to the Grimoire Design System, so
every selectable activity in LegendsLegacy answers the same four questions consistently: what is it, can I do
it, what does it cost, what do I get. Anatomy: identity (name, type, region); recommended level or Combat
Rating; EnemyGroup preview; entry cost with CostList (Arena tickets, dungeon Sigils, Vigor); a compact
RewardBundle preview; status (locked with requirement, available, active now, completed, First Clear
available, on cooldown); and one action. Use the Row form by default. The Card form is only for short lists,
and follows the Art-optional contract; never reuse the same generic image for every area. Identity comes from
data (enemies, rewards, requirements) when art is missing. Type variants: area (enemies, Essence drops,
idle-combat target), dungeon (difficulty, mastery, records), World Tower floor (Guardian, First Clear or Echo
reward eligibility), raid, and scheduled event (SchedulePhase). Mark the activity where idle combat is
currently running. Preview a Shenic area list in Row form with one active area and one locked, a dungeon
difficulty selector, three Tower floors (cleared, current with First Clear available, locked), and the
regional boss event card.
```

---

#### DS-078 — Stakes & Risk Summary

**Category:** Game Component
**Priority:** High
**Depends On:** DS-007, DS-054, DS-073

**Purpose:**
The audit calls risk a first-class decision. Examples are dungeon Vigor and Pending Loot, where failure loses the loot and retreat secures it, Arena rating stakes, tournament registration, raid participation and irreversible guild donations. Players must see what they commit, what they can gain and what they can lose, in one place, before committing.

**Should Define:**

- **StakesSummary:** Commit (costs), Gain (rewards and rating up), Risk (what can be lost), and Reversibility (retreat option, cancellation window).
- Risk severity using warning or danger (from DS-007), with explicit words.
- A live version during a dungeon run: Vigor remaining, the Pending Loot value, and the retreat consequence.
- Placement: directly beside the committing action, never in help text.
- Compact and full forms.

**Examples in LegendsLegacy:**
Dungeon room choice, "Retreat to secure Pending Loot", an Arena challenge (+18 / −12 rating), tournament registration and a raid Final Assault.

**Paste-ready Claude Design instruction**

```text
Add a StakesSummary component to the Grimoire Design System. In LegendsLegacy, risk is a real decision:
dungeon runs spend Vigor and carry Pending Loot, which is lost on failure and secured on retreat; Arena fights
put rating at stake; and some actions are irreversible. StakesSummary sits directly beside the committing
action and never in help text. It has four labelled parts: Commit (costs via CostList), Gain (a compact
RewardBundle and rating gain), Risk (what can be lost, in explicit words, using warning or danger per the
feedback roles), and Reversibility (whether the player can retreat, cancel or undo, and how). Provide a
compact form for rows (an Arena opponent row showing "+18 / −12 rating") and a full form for preparation
panels. Add a live form for an active dungeon run: Vigor remaining shown with the depleting-resource Meter,
the Pending Loot contents and value, and the retreat consequence ("Retreat now: secure 6 items. Continue: next
room costs 2 Vigor"). Preview a dungeon room choice, an active run with a Retreat decision, an Arena
challenge, and tournament registration.
```

---

#### DS-079 — Build Identity, Presets & Snapshot States

**Category:** Game Component
**Priority:** High
**Depends On:** DS-019, DS-026, DS-037

**Purpose:**
LegendsLegacy has a live build made of three separately saved axes (equipment presets, Essence presets and the Combat Style), unsaved Combat Style previews, presets auto-used by activities, and captured snapshots for Arena defence, tournaments, Tower and raids. The implementation plan warns that a single preset dropdown must not imply one atomic build across all three.

**Should Define:**

- A **BuildStateLabel** vocabulary:
  - Live build.
  - Preview, unsaved.
  - Equipment preset "Name".
  - Essence preset "Name".
  - Combat Style "Name".
  - Assigned to an activity ("Used by: Arena attacks").
  - Captured snapshot, with a date.
  - Snapshot differs from live, with an "Update snapshot" action.
- **SaveStatus:** Saved, Saving… and Save failed · Retry, including the autosave behaviour.
- A **BuildSummary** line showing the three axes with their sources.
- Rules for showing the captured state wherever an activity uses a snapshot.

**Examples in LegendsLegacy:**
Character Overview "Current build", the Arena defence snapshot, a tournament team, a Tower application and a Combat Style preview.

**Paste-ready Claude Design instruction**

```text
Add BuildStateLabel, SaveStatus and BuildSummary components to the Grimoire Design System. In LegendsLegacy a
build has three separately saved axes: equipment presets, Essence presets and the equipped Combat Style.
Activities can auto-use presets, and competitive and cooperative activities (Arena defence, tournaments, World
Tower expeditions, raids) capture snapshots that do not change when the live build changes. Define the
BuildStateLabel vocabulary with a consistent visual form for each: "Live build"; "Preview · unsaved" (a Combat
Style preview); "Equipment preset · Wolfsbane Hunt"; "Essence preset · Pack Leader"; "Combat Style · Conduit";
"Used by Arena attacks" (an assigned preset); "Captured 12 Sep · Arena defence" (a snapshot); and "Snapshot
differs from live build" with an "Update snapshot" action. Never label a single preset as if it were the whole
build. SaveStatus shows "Saved", "Saving…" and "Save failed · Retry", matching the game's autosave and
failed-save blocking behaviour. BuildSummary is a compact line showing all three axes with their sources, for
headers and activity preparation. Preview the Character Overview "Current build" area, an Arena defence
snapshot that differs from the live build, a Combat Style preview with unsaved changes, and a failed Essence
preset save.
```

---

#### DS-080 — Combatant Summary

**Category:** Game Component
**Priority:** High
**Depends On:** DS-056, DS-066, DS-079

**Purpose:**
Arena opponents, tournament team members, raid party members, Tower expedition applicants and other-player inspections all summarise a combatant. Today these are separate implementations. One component with context variants carries identity, power, build and readiness.

**Should Define:**

- Anatomy: PlayerPlate (or creature identity), level, Combat Rating, BuildSummary highlights (Combat Style and key Essences), and a captured-build marker when relevant.
- Context variants:
  - Arena opponent: rating, stakes (DS-078), tickets and a Challenge action.
  - Party member: readiness, role, captured build and whether they are online.
  - Applicant: application state, with Accept and Decline.
  - Tournament team member.
- Row and Card forms. Card only where an opponent is the bounded object (DS-042).
- Readiness states: ready, not ready (with a reason), offline and snapshot outdated.

**Examples in LegendsLegacy:**
The Arena opponent list, the tournament team of three, raid muster parties and Tower applications.

**Paste-ready Claude Design instruction**

```text
Add a CombatantSummary component to the Grimoire Design System, with Row and Card forms and context variants,
replacing separate opponent, party-member and applicant presentations in LegendsLegacy. Anatomy: PlayerPlate
(or creature identity for PvE), LevelBadge, Combat Rating, BuildSummary highlights (Combat Style and the first
one or two attuned Essences), and the captured-build marker when the activity uses a snapshot. Context
variants. Arena opponent: Arena rating, compact StakesSummary ("+18 / −12"), and a compact "Challenge"
CostButton using an Arena ticket. Party member: readiness state, role, captured build, and Presence.
Expedition applicant: application state with Accept and Decline for the leader. Tournament team member.
Readiness states: ready, not ready with a reason, offline, and snapshot outdated with an update prompt. Use
the Card form only where the opponent is the bounded object being chosen, as the container rules allow; lists
use the Row form. Preview an Arena opponent list with three opponents, a tournament team of three with one
member not ready, and a World Tower expedition with two applicants.
```

---

#### DS-081 — Combat Unit Frame

**Category:** Game Component
**Priority:** High
**Depends On:** DS-044, DS-060, DS-066, DS-067

**Purpose:**
The shared combat component is used in idle combat, dungeons, raids, the Tower, the regional boss and tournament replays. It needs a standard unit frame for players, summons, enemies and bosses. The audit notes that the combat viewer's statistical inspection dominates the moment-to-moment view, so the frame must show the decisive state first.

**Should Define:**

- **UnitFrame:** name, level, a Health meter with a barrier overlay, a condition strip (DS-060 ordering), and a role or threat indicator (Taunt or highest threat) where relevant.
- Summons nested under their owner.
- Defeated, stunned or controlled, and selected states.
- **BossFrame:** a larger form with health, stagger or threshold meter, phase or Fury state and key mechanics.
- A compact party frame for three to five units.
- Update behaviour: values change without layout shift, and damage flashes stay within the motion budget.

**Examples in LegendsLegacy:**
The idle combat viewer, raid playback, the regional boss (Fury and stagger), and tournament replays.

**Paste-ready Claude Design instruction**

```text
Add UnitFrame and BossFrame components to the Grimoire Design System for LegendsLegacy's shared combat viewer
(idle combat, dungeon rooms, raids, World Tower, regional boss, tournament replays). UnitFrame shows name,
LevelBadge, a Health meter with a barrier overlay (the depleting-resource Meter type) with numbers, a
ConditionChip strip in compact size ordered beneficial then harmful with overflow, and a role or threat
indicator where it matters (Taunt, highest threat). Summons nest under their owner. States: defeated,
controlled (Stun or Freeze shown by the condition, not a new colour), selected for inspection. BossFrame is
the larger form with Health, a stagger or threshold meter, the current phase or Fury state, and one line of
key mechanics. Add a compact party frame for three to five units. Values update live without layout shift; hit
feedback uses only the motion tokens and has a reduced-motion alternative. Decisive state (who is alive, who
is controlled, the boss phase) must read first, before any statistics. Preview a party of three with a summon
against two enemies, and a regional boss with stagger and Fury.
```

---

#### DS-082 — Combat Log & Event Line

**Category:** Game Component
**Priority:** High
**Depends On:** DS-027, DS-059, DS-060

**Purpose:**
Accurate outcome explanation is on the "must not be lost" list: detailed statistics, typed effects, summons, contributions and historical replay. The combat log and the Chronicle's system lines need one event grammar.

**Should Define:**

- An event grammar: actor, ability, target, result (damage with type, healing, condition applied or expired, death, summon), and modifiers (crit, blocked, dodged, ignored by Tenacity).
- **EventLine** with density modes, damage numbers in the damage hue with the type word or icon, and timestamps or round markers.
- Filters: damage, healing, conditions, deaths, by unit.
- Grouping by round or turn, with collapsible repetition ("Bleed ticks ×6 · 1,240 total").
- A shared grammar with Chronicle system and loot lines.
- An accessible table alternative.

**Examples in LegendsLegacy:**
The combat analysis log, the tournament replay log and Chronicle combat summary lines.

**Paste-ready Claude Design instruction**

```text
Add CombatLog and EventLine components to the Grimoire Design System. Define an event grammar used everywhere
combat is narrated in LegendsLegacy: actor, then ability, then target, then result, then modifiers. Results:
damage with type, healing, barrier gained, condition applied, stacked or expired, death, summon. Modifiers:
critical hit, blocked, dodged, "ignored by Tenacity". For example: "Aelric · Alpha Fangs → Grey Wolf: 1,284
Physical (critical)". EventLine uses the Compact density by default, damage numbers in their damage hue with
the type word or icon, condition names as keywords, and a round or time marker. CombatLog adds filters
(damage, healing, conditions, deaths, per unit), grouping by round, and collapsing of repetition ("Bleed ticks
×6 · 1,240 total"). It sits behind the combat result by default as the deepest disclosure layer. Align the
grammar with the Chronicle's system and loot lines so combat summaries read the same in both. Provide an
accessible table alternative. Preview twelve lines of a fight with mixed damage types, a crit, a dodge, a
Bleed stack and a summon, plus the filtered view showing only conditions.
```

---

#### DS-083 — Combat Result & Outcome

**Category:** Game Component
**Priority:** High
**Depends On:** DS-056, DS-073

**Purpose:**
Every combat context ends with an outcome: an idle encounter, a dungeon room, an Arena fight, a Tower floor, a raid or a tournament match. Grimoire's voice says victory is stated, not cheered. The result needs a consistent hierarchy: outcome, why, what you got, what next.

**Should Define:**

- **ResultHeader:** "Defeated", "Floor cleared" or "Victory · 42s", in a plain statement.
- Key facts: duration, top contributor, damage dealt and taken, and the decisive cause of a loss where known.
- Rewards (a RewardBundle in received form), rating change (DeltaValue), and progress changes (XP, mastery).
- Next actions: Continue, Replay, View log, Return.
- Context variants for idle, dungeon room, Arena, Tower, raid and tournament.
- A link to the full log (DS-082), never shown first.

**Examples in LegendsLegacy:**
A dungeon room cleared, an Arena loss with a rating delta, a Tower floor First Clear and a raid Final Assault.

**Paste-ready Claude Design instruction**

```text
Add a CombatResult component to the Grimoire Design System with a consistent hierarchy for every LegendsLegacy
combat outcome: outcome, then why, then what you got, then what next. ResultHeader states the outcome plainly,
following the voice rules ("Floor cleared", "Defeated", "Victory · 42s"), with no celebration copy. Key facts:
duration, top contributor, damage dealt and taken, and the decisive cause of a defeat when the game knows it.
Gains: RewardBundle in received form, rating change as a DeltaValue, and XP and mastery progress. Next
actions: Continue, Replay, View log, Return, with at most one solid button. Link to the full CombatLog as a
deeper layer; never show the full log first. Context variants: idle encounter (compact, often only in the
Chronicle), dungeon room (with Vigor and Pending Loot updates), Arena (rating change and ticket), World Tower
floor (First Clear status), raid (party contributions and Final Assault result), tournament match (round
progression). Preview a dungeon room cleared, an Arena defeat with a rating loss, and a World Tower First
Clear.
```

---

#### DS-084 — Ranking Entry & Position

**Category:** Game Component
**Priority:** High
**Depends On:** DS-041, DS-066

**Purpose:**
The global Leaderboard, Arena and Guild rankings, the Tower Hall of Fame and tournament standings all order players or guilds by a metric. The audit asks for trustworthy histories: the exact metric, your own rank in context, and the provenance of a first clear. The Nobility rules forbid decorations that change placement or prominence.

**Should Define:**

- **RankNumber:** tabular. The top three get a quiet distinction (weight or a marker), not trophies.
- A ranking row: RankNumber, movement, PlayerPlate or guild identity, the metric value (the metric named in the column header), and a secondary metric.
- The player's own row, highlighted and pinned (sticky) when off-screen.
- Ties, and "unranked" or "not enough data" states.
- Provenance: season or scope, last updated, and first-clear details (date and party) for the Hall of Fame.
- A guild ranking variant.

**Examples in LegendsLegacy:**
The global Leaderboard, Arena rankings, Guild rankings and the World Tower Hall of Fame.

**Paste-ready Claude Design instruction**

```text
Add RankNumber and RankingRow components to the Grimoire Design System, designed to work inside DataTable.
RankNumber uses tabular numerals. The top three get a quiet, consistent distinction (weight or a small
marker), never trophies, crowns or glow. RankingRow shows RankNumber, rank movement (▲3, ▼1, "New"),
PlayerPlate or a guild identity, the ranked metric's value (the metric is named in the column header, for
example "Arena Rating" or "Highest floor"), and an optional secondary metric. The player's own row is quietly
highlighted and pinned to the top or bottom of the table when scrolled out of view. Define ties, "Unranked"
and "Not enough data" states. Define provenance: scope and season ("Season 3 · Global"), "Updated 5 min ago",
and for the World Tower Hall of Fame, first-clear details (date and party members), so a historic first clear
never looks like an ordinary record. Apply the Nobility display rules: decorations never change placement or
prominence. Add a guild ranking variant. Preview a global Leaderboard page with the player's own row pinned,
an Arena ranking with movement, and three Hall of Fame entries.
```

---

#### DS-085 — Upgrade Preview (Current → Next)

**Category:** Game Component
**Priority:** High
**Depends On:** DS-054, DS-056, DS-057, DS-062, DS-064

**Purpose:**
Soulstone upgrades, guild buildings, Essence Ascension, Combat Style mastery and upgrades, Stronghold (planned) and any future equipment upgrade all ask the same question: what do I get next, what does it cost, and can I do it. The audit found Soulstone upgrade cards interchangeable. One upgrade preview with a strong current → next structure serves all of them.

**Should Define:**

- **UpgradePreview:**
  - Current level against the next.
  - Effect lines as current → next, with DeltaValues.
  - CostList, RequirementList and an optional construction timer.
  - One action.
- A max-level state, and a "next three levels" optional expansion.
- Refund or reset consequences where the system allows them, stated as never-hide facts.
- Variants: permanent (Soulstones), shared (guild building, with contribution progress), and timed (construction).
- The rule that repeated upgrade items in a list show only the next marginal benefit, not full cards.

**Examples in LegendsLegacy:**
A Soulstone constellation node, the guild Treasury level 3 → 4, Essence Ascension tier 1 → 2, and a Combat Style mastery milestone.

**Paste-ready Claude Design instruction**

```text
Add an UpgradePreview component to the Grimoire Design System, the single pattern for "what do I get next,
what does it cost, can I do it" across LegendsLegacy: Soulstone upgrades, guild buildings, Essence Ascension,
Combat Style mastery and upgrades, the planned Stronghold, and any future equipment upgrade. Anatomy: current
level → next level; effect lines as current → next with DeltaValues ("Crit Chance 4% → 5%"); CostList with
affordability; RequirementList; an optional construction or research timer; one action (CostButton). Add a
max-level state and an optional "next three levels" expansion. Refund or reset consequences, where a system
allows them, are stated beside the action as never-hide facts. Variants. Permanent: Soulstones. Shared: a
guild building funded by member contributions, showing contribution progress and who can trigger the upgrade.
Timed: construction that completes after a duration. In lists of many upgradeable things, each row shows only
the next marginal benefit and cost, so a list of Soulstone nodes doesn't become a grid of identical cards.
Preview a Soulstone node that is affordable, a guild Treasury upgrade from level 3 to 4 with contribution
progress, Essence Ascension from tier 1 to 2 with its requirements, and a maxed node.
```

---

#### DS-086 — Node Graph Primitives

**Category:** Game Component
**Priority:** Medium
**Depends On:** DS-015, DS-019

**Purpose:**
The dungeon route (revealed rooms only), the Soulstone Constellation, tournament brackets and raid parties feeding a Final Assault are all nodes and edges. The audit praises these structures as screen identity. The design system should supply node and edge primitives, while each feature owns its composition.

**Should Define:**

- **Node:**
  - States: available, current, completed, selected, locked, hidden or unrevealed, and failed.
  - Shape by meaning (DS-015).
  - A label slot and a type icon (for a dungeon room: combat, elite, treasure, rest or boss).
- **Edge:** traversed, available, locked and hidden.
- Keyboard traversal and a list-view alternative for accessibility.
- The relationship to the existing Constellation, which becomes a composition built on these primitives.
- The rule that hidden information stays hidden. Never render unrevealed rooms.

**Examples in LegendsLegacy:**
The dungeon run route, the Soulstone Constellation, tournament rounds and a raid party → Final Assault diagram.

**Paste-ready Claude Design instruction**

```text
Add GraphNode and GraphEdge primitives to the Grimoire Design System and document how feature compositions use
them. Features own their layout; the design system owns how nodes and edges look and behave. GraphNode states:
available, current, completed, selected, locked, hidden or unrevealed, failed. Shape follows the Shape page
(hexagon for sigil or constellation nodes, diamond for milestone or current). A node has a label slot and a
type icon, for example dungeon room types: combat, elite, treasure, rest, boss. GraphEdge states: traversed,
available, locked, hidden. Provide keyboard traversal between connected nodes and an equivalent list view for
accessibility. Hidden information stays hidden: never render unrevealed dungeon rooms or their connections.
Rebuild the existing Constellation as a composition of these primitives without changing its appearance.
Preview four compositions as examples: a dungeon route with revealed and unrevealed rooms and the current
position, the Soulstone Constellation, a tournament bracket of eight teams, and three raid parties feeding a
Final Assault.
```

---

#### DS-087 — Market Listing & Order Book

**Category:** Game Component
**Priority:** Medium
**Depends On:** DS-041, DS-054, DS-068

**Purpose:**
The Cinder Bazaar has commodity order books, exact equipment listings, a sell flow, My Orders and trade history, with escrow and fees. The audit lists order books among the game's existing strengths ("trustworthy numbers outrank atmospheric whitespace"). Signets and Nobility capacity also interact with the market.

**Should Define:**

- **OrderBook:** buy and sell sides, price, quantity and total, best bid and ask, the spread, and the player's own orders marked.
- **ListingRow:** an exact equipment listing (ItemRow, price, seller and listed time).
- **OrderStatus:** open, partially filled, filled, cancelled and expired, with escrow or reserved quantities.
- Capacity: "8 / 10 sell listings", or 30 with Nobility, with the capacity Meter.
- PriceBreakdown (DS-054) at commit, and a price history sparkline (DS-050).

**Examples in LegendsLegacy:**
Commodity trading, exact gear purchases, My Orders, Signet trading and trade history.

**Paste-ready Claude Design instruction**

```text
Add OrderBook, ListingRow and OrderStatus components to the Grimoire Design System for the Cinder Bazaar.
OrderBook shows buy and sell sides in DataTable Compact density: price, quantity and total columns with
tabular numerals, best bid and best ask emphasised, the spread shown between them, and the player's own orders
marked with the ownership marker. ListingRow is an exact equipment listing: ItemRow, price as Amount, seller
as PlayerName and listed time, with the ItemCard on hover. OrderStatus covers open, partially filled ("12 / 25
filled"), filled, cancelled and expired, and shows escrow or reserved quantities explicitly. Show listing
capacity with the capacity Meter ("8 / 10 sell listings"), noting that Nobility raises it to 30 without
styling the note as an advertisement. At commit, always use PriceBreakdown (unit price × quantity, fee, total
or net) and never abbreviate amounts. Include a price history sparkline from the Charts page. Preview a
commodity order book, three exact equipment listings, and a My Orders list with each status, including a
Signet sell order.
```

---

#### DS-088 — Attention Indicators & Navigation Badges

**Category:** Game Component
**Priority:** High
**Depends On:** DS-020, DS-043

**Purpose:**
The audit found badge proliferation made a completed collection look as urgent as a required action. NavRail items support a badge and a locked state, and quests have attention markers. There is no rule for what deserves attention, how it cascades from a tab to a nav group, or when it clears.

**Should Define:**

- Attention classes:
  - **Action required:** a failed save, or an expiring claimable. Marker plus word.
  - **Claimable:** CountBadge.
  - **New content:** Marker.
  - **Informational:** no marker.
- Things that never get attention: completed items, claimed rewards, routine progress, and promotional content.
- The cascade: an item marker propagates to its tab, then to the nav item. Groups show at most one marker.
- Clear rules: when "new" clears (on view or on interaction), and persistence across sessions.
- Caps ("9+") and accessible summaries ("Prophecies, 3 claimable").

**Examples in LegendsLegacy:**
Claimable Prophecies, new Essences in the Archive, a quest attention marker, and a new guild application (for officers only).

**Paste-ready Claude Design instruction**

```text
Add an "Attention" page to Game Components in the Grimoire Design System, and update NavRail, TabStrip and Row
to follow it. Define attention classes. Action required (a failed save, a claimable about to expire, a guild
application awaiting an officer): a Marker plus a word. Claimable: a CountBadge. New content (a new Essence in
the Archive, a new title): a Marker. Informational: no marker at all. List what never gets attention:
completed items, already-claimed rewards, routine progress, and promotional or Nobility content. Define the
cascade: an entry's marker propagates to its tab, and the tab's to its NavRail item. Each level shows at most
one marker, with the highest class winning. Define clear rules: when "new" clears (on view, or on first
interaction; choose and record), and whether markers persist across sessions. Define caps ("9+") and
accessible summaries ("Prophecies, 3 claimable"). Preview a NavRail with Prophecies (3 claimable), Essences
(new), Guild (an application for officers only), and Achievements with nothing to claim showing no marker.
```

---

### Phase 4 — Composite Patterns

#### DS-089 — Page & Feature Header Pattern

**Category:** Pattern
**Priority:** High
**Depends On:** DS-008, DS-031, DS-038, DS-054

**Purpose:**
The audit found `DefaultHeader` repeated across 15 templates as the same icon plus heading, which gives every feature the same opening. PageHeader exists. What is missing are variants that match the page's job, a resource header for feature currencies (from DS-024), and rules against redundant eyebrows and icons.

**Should Define:**

- **Information header:** title, an optional one-line summary, and actions.
- **Feature header:** title, a resource header (feature currencies as Amounts, placed at the end), and destination tabs attached.
- **Detail header:** BackLink or breadcrumbs, entity identity and entity actions.
- **Activity header:** phase, timer and a StakesSummary compact form, for live activities.
- Rules:
  - No eyebrow repeating the nav group when the NavRail already shows it.
  - No icon badge unless it adds meaning.
  - A summary line is optional, and one line at most.
  - Headers stay shallow and do not become heroes.
- Whether headers stay pinned or scroll away, per archetype.

**Examples in LegendsLegacy:**
The Prophecies header with Fate Echo and Sigil Fragments. The Colosseum header with Glory and tickets, plus destination tabs. Another player's profile header with "Back to rankings". The dungeon run header.

**Paste-ready Claude Design instruction**

```text
Add a PatternPageHeader composition to the Grimoire Design System and revise the PageHeader component with
four variants matched to a page's job. Information header: title, an optional one-line summary, and actions.
Feature header: title, a resource header at the end showing that feature's currencies as Amounts (per the
shell contract, feature currencies live here and not in the TopBar), and the destination TabStrip attached
beneath. Detail header: BackLink or Breadcrumbs, entity identity (PlayerHeader, item or guild), and entity
actions. Activity header, for live activities: phase, Countdown and a compact StakesSummary. Add rules. No
eyebrow that repeats the NavRail group, which the player can already see. No icon badge unless it adds
meaning. The summary is optional and at most one line. Headers stay shallow and never become hero banners (a
Banner is a separate, once-per-screen element). Specify per archetype whether the header stays pinned or
scrolls away. Preview: Prophecies (Fate Echo and Sigil Fragments in the resource header), Colosseum (Glory and
Arena tickets with destination tabs), another player's profile ("Back to rankings"), and an active dungeon run
(phase, Vigor, Pending Loot).
```

---

#### DS-090 — Master–Detail Pattern

**Category:** Pattern
**Priority:** Critical
**Depends On:** DS-024, DS-040, DS-041, DS-042

**Purpose:**
List-plus-detail is the dominant structure in LegendsLegacy: Inventory, the Soul Archive, Creatures, the Tower list, guild members and the Bazaar. The audit stresses keeping selection across refreshes and giving small screens a way back to the original selection. Grimoire's Folio is one detail form, and the Workbench inspector pane is another.

**Should Define:**

- Anatomy: a list pane (List or DataTable) and a detail pane (the Folio in Stage mode, or an inspector pane in Workbench mode).
- Selection:
  - Kept through data refreshes, filtering (when still visible) and Back.
  - URL-backed when shareable (for example `/essences/:id`).
- The empty detail state ("Select an Essence to see its abilities").
- Keyboard: Up and Down move the selection and the detail follows. Focus stays in the list.
- Multi-select turns the detail pane into bulk actions.
- Responsive behaviour: at narrow widths the detail replaces the list with a Back control, and the decision summary is retained.
- The rule that there is one detail pane per screen.

**Examples in LegendsLegacy:**
Inventory (Workbench inspector), the Creature Archive (Folio), Tower expedition list and detail, and the guild member list with a member inspector.

**Paste-ready Claude Design instruction**

```text
Add a PatternMasterDetail composition and document the master–detail pattern in the Patterns section of the
Grimoire Design System. It is the dominant structure in LegendsLegacy. Anatomy: a list pane (List or
DataTable) and a detail pane, which is the Folio in Stage mode or an inspector pane in Workbench mode (per the
shell contract), and never both. Selection rules: the selection survives data refreshes, filtering while the
item is still visible, and Back navigation. It is URL-backed when the selection is worth sharing or refreshing
(for example /character/essences/:essenceId). Define the empty detail state ("Select an Essence to see its
abilities"). Keyboard: Up and Down move the selection, the detail updates, and focus stays in the list; Enter
moves focus into the detail. Multi-select mode turns the detail pane into a bulk-action summary ("4 items
selected · Sell · Favourite"). Responsive behaviour uses the Layout container breakpoints: at narrow widths
the detail replaces the list, with a BackLink that restores the list's scroll and selection and keeps a
one-line summary of the selection. Preview three configurations: Inventory in Workbench mode with an
inspector, the Creature Archive in Stage mode with the Folio, and the narrow-width detail view with Back.
```

---

#### DS-091 — Filterable Collection Browser

**Category:** Pattern
**Priority:** High
**Depends On:** DS-039, DS-040, DS-041, DS-047, DS-048, DS-068

**Purpose:**
Inventory, the Soul Archive, Creatures, the Essence Codex, Achievements, Titles, Bazaar browsing and guild discovery are all large, filterable collections. They share mechanics (filter, sort, count, select, bulk act, know what is missing) but not their identity, and the pattern must leave room for the differences.

**Should Define:**

- Anatomy: FilterBar, result count, a List or Grid toggle (grid only where a slot grid helps), the collection, a master–detail pane, and optional CollectionProgress.
- Ordering and grouping options: by rarity, by slot, by region, or known before missing.
- How to present missing or undiscovered entries alongside known ones.
- Bulk actions: selection mode and a protected-items rule (favourite and equipped items are excluded from bulk sell or donate).
- Virtualisation, loading, empty, filtered-empty and stale states.
- Filter and scroll persistence.
- A statement of how collections differ structurally, following the sameness test.

**Examples in LegendsLegacy:**
The inventory gear list, the Soul Archive, the Creature Archive, the Codex collections and Bazaar item search.

**Paste-ready Claude Design instruction**

```text
Add a PatternCollectionBrowser composition to the Grimoire Design System and document the pattern. It serves
Inventory, the Soul Archive, Creatures, the Essence Codex, Achievements, Titles, Bazaar browsing and guild
discovery. Anatomy: FilterBar; result count; an optional List or Grid SegmentedControl (grid only where slot
shapes help recognition, as in the equipment grid); the collection as List or DataTable; a master–detail pane;
and optional CollectionProgress. Define grouping and ordering options (by rarity, slot, region, or known
before missing), and how undiscovered entries appear alongside known ones using the knowledge states. Define
bulk actions: a selection mode with a bulk-action bar, and a protection rule that favourite and equipped items
are excluded from bulk Sell and Donate, with the exclusion stated ("2 protected items skipped"). Cover
virtualisation for large collections, and the loading, empty, filtered-empty and stale states. Filters, sort
and scroll persist through inspection and Back. Add a note applying the sameness test: the Soul Archive (a
build resource), the Codex (completion bonuses) and Achievements (recognition) share this pattern's mechanics
but must differ in what they emphasise. Show how. Preview the inventory gear list with bulk selection, and the
Creature Archive with discovered and undiscovered entries.
```

---

#### DS-092 — Inspection Layers Pattern

**Category:** Pattern
**Priority:** High
**Depends On:** DS-022, DS-032, DS-069, DS-090

**Purpose:**
Items, Essences, abilities, creatures, players, stats and guilds can all be inspected from many places (chat links, rankings, rewards, parties). This pattern applies DS-022 consistently and guarantees that inspecting something never loses the player's place.

**Should Define:**

- The standard chain for each entity type: glance, then hover card, then detail (Folio, inspector or a profile page), then deep (breakdown, history).
- An "Inspect" entry from anywhere (the item menu, the player menu, a chat link).
- Context preservation: opening a profile from rankings or a party returns to exactly where the player was.
- Cross-entity links inside hover cards (an item's set leads to the set members, an Essence to its creature).
- Mouse, keyboard and touch access at every step.

**Examples in LegendsLegacy:**
An item linked in chat, a player inspected from the Leaderboard, a creature from an area's enemy list, and a stat breakdown from the Overview.

**Paste-ready Claude Design instruction**

```text
Add a PatternInspection composition to the Grimoire Design System and document inspection layers per entity
type, applying the Progressive disclosure page. For each entity (item, Essence, ability, condition, creature,
player, guild, stat), specify the chain: glance form (Row or slot), hover card (ItemCard, AbilityCard,
ConditionCard, player card, AttributeBreakdown), detail form (Folio, inspector pane, or profile page), and
deep form (breakdown dialog, history, full log). Define how "Inspect" is reached from anywhere: the item menu,
the player menu, a Chronicle item link, a reward, a party slot. Define context preservation: inspecting a
player from the Leaderboard or a raid party and pressing Back returns to exactly the same list position,
filters and selection. Define cross-entity links inside hover cards (an item's set name leads to the set
members; an Essence to its source creature; a condition keyword to its ConditionCard), limited to one nested
hover level. Verify mouse, keyboard and touch access at every step. Preview two chains: an item linked in the
Chronicle through to the full inspector, and a player on the Leaderboard through to their profile and back.
```

---

#### DS-093 — Comparison Pattern

**Category:** Pattern
**Priority:** High
**Depends On:** DS-057, DS-070

**Purpose:**
Comparison goes beyond equipment. Players compare an Essence against the one in a slot, two presets, their build against an opponent, a building's current and next level, and two Combat Styles. The equipment comparison from DS-070 generalises into a pattern.

**Should Define:**

- The comparable pairs: item against equipped, Essence against slotted, preset against preset, self against opponent, current level against next, and Style against Style.
- Layout options: side-by-side columns, or a delta overlay on a single list. When to use each.
- Comparison context: which build or activity the comparison applies to.
- A persistent compare mode (a toggle, or holding Shift), and a limit of two entities.
- ComparisonSummary first, then detail.

**Examples in LegendsLegacy:**
Inventory, the Essence loadout editor, Arena opponent scouting and the Combat Style preview.

**Paste-ready Claude Design instruction**

```text
Add a PatternComparison composition to the Grimoire Design System, generalising EquipmentComparison into a
pattern. Document the comparable pairs in LegendsLegacy: item against equipped item; Essence against the
Essence in a loadout slot; one preset against another; the player's build against an opponent in Arena
scouting; a building's or upgrade's current level against the next; one Combat Style against another in
preview. Define two layouts and when each applies: side-by-side columns (two entities of the same kind with
many stats) and a delta overlay on a single list (a candidate replacing something, shown as changes). Always
state the comparison context ("for your Arena defence build"). Define a persistent compare mode, toggled or
held with Shift, and limit comparisons to two entities. Always lead with ComparisonSummary, then the detailed
rows. Never output a single overall verdict score. Preview an Essence against the slotted Essence, a
self-against-opponent scouting view, and a Combat Style preview comparison.
```

---

#### DS-094 — Transaction & Confirmation Flow

**Category:** Pattern
**Priority:** Critical
**Depends On:** DS-030, DS-034, DS-046, DS-054

**Purpose:**
Honest costs and ownership top the audit's "must not be lost" list: before and after values, fees and net, guild property restrictions, donation permanence, affordability and server restriction reasons. The game already uses stable operation IDs for safe retries. One flow gives every transaction the same review, commit and recovery grammar.

**Should Define:**

- Steps: select, then review (PriceBreakdown or CostList, StakesSummary, consequences, ownership changes), then confirm (at the right level), then pending, then result, with failure and safe retry as a branch.
- Confirmation levels:
  - None: reversible and low-cost, such as a Prophecy reroll.
  - Inline review: a purchase.
  - ConfirmDialog: permanent, such as a donation.
  - High-stakes: disbanding a guild.
- Server re-quote and conflict handling (the price changed, or stock is gone). The player's inputs are kept.
- Balance updates after the result, and where the result is reported (toast or inline).
- A no-dark-patterns rule: no pre-checked extras and no misleading defaults.

**Examples in LegendsLegacy:**
A Bazaar buy or sell, a guild Vault donation, Signet redemption, a Tower shop purchase, a Soulstone upgrade and a Prophecy reroll.

**Paste-ready Claude Design instruction**

```text
Add a PatternTransaction composition and document the transaction flow in the Patterns section of the Grimoire
Design System. Steps. Select (item and quantity via QuantityStepper). Review: PriceBreakdown or CostList,
StakesSummary where something can be lost, stated consequences, and ownership changes ("becomes guild
property", "moves to escrow"). Confirm, at the right level. Pending. Result. Failure with safe retry is a
branch from pending. Define four confirmation levels with examples. None: reversible, low-cost actions such as
a Prophecy reroll. Inline review: a normal Bazaar purchase. ConfirmDialog: permanent actions such as a guild
donation or Signet redemption. High-stakes: disbanding a guild. Define server re-quote and conflict handling:
if the price changed or stock is gone, show the new terms and let the player review again, keeping their
inputs. Define the result: balances update from the server response, and success is reported with a factual
toast or inline confirmation. Define recovery: uncertain responses retry with the same operation, as Nobility
redemption already does. Add a no-dark-patterns rule: no pre-selected extras, no misleading defaults, and no
urgency copy. Preview a Bazaar sell flow end to end, a guild donation with permanence, a Signet redemption,
and a price-changed conflict.
```

---

#### DS-095 — Reward Reveal & Claim Flow

**Category:** Pattern
**Priority:** High
**Depends On:** DS-017, DS-046, DS-073, DS-074

**Purpose:**
Claiming should feel good without becoming noisy or blocking. Most loot flows into the Chronicle's loot channel. Some moments deserve more, such as a Prophecy cache opening or a Tower First Clear, and a few edge cases (a full inventory, partial failure) must be handled explicitly.

**Should Define:**

- A reveal scale with a celebration budget:
  - Small: an inline update and a Chronicle loot line.
  - Medium: a result panel.
  - Large: a reveal dialog, only for caches and major first clears.
- Claim all: a batched result summary with failures listed.
- Overflow: what happens when the inventory is full (held, mailed or blocked, as the game defines it), stated before the claim.
- Motion within DS-017 and DS-128, with reduced-motion equivalents.
- Rare drops: an optional toast and the Chronicle, with no full-screen interruption.

**Examples in LegendsLegacy:**
Idle combat loot, a Prophecy cache opening, Claim all on quests, a Tower First Clear and a raid Trophy payout.

**Paste-ready Claude Design instruction**

```text
Add a PatternRewardReveal composition and document the reward reveal and claim flow in the Grimoire Design
System. Define a reveal scale with a celebration budget. Small: routine idle-combat loot updates inline and
appears as a Chronicle loot line, with no popup. Medium: a result panel inside the current page after a
dungeon, a quest claim or a raid payout. Large: a reveal dialog, reserved for opening Prophecy caches and
major first clears such as a World Tower First Clear. Claim all produces one batched result summary listing
everything received, and lists any failures separately with Retry. Define overflow: when the inventory is
full, state it before the claim and say what will happen (use the game's actual behaviour and mark it "verify"
if unknown). Rare drops may add a single toast alongside the Chronicle line, never a full-screen interruption.
Motion stays within the Motion tokens, with no particles, confetti or looping shine, and each reveal has a
reduced-motion equivalent. Preview each scale with LegendsLegacy content and a Claim all with one failed
claim.
```

---

#### DS-096 — Build Summary & Loadout Editor

**Category:** Pattern
**Priority:** High
**Depends On:** DS-061, DS-068, DS-071, DS-079

**Purpose:**
The audit's clearest relationship problem is that gear, Essence loadouts and Combat Style choices are spread across pages without a single account of the build that will actually be used. Loadout editing also has autosave, failed-save blocking, locked-preset copy and recovery, and activity assignment, and all of that must stay visible.

**Should Define:**

- A BuildSummary header showing the three axes with their sources (DS-079), and the activities using this build.
- Slot areas: eight equipment slots, Essence slots (ordered) and the Combat Style.
- Assignment: select a slot then choose from a list (the primary method), or drag and drop (secondary, with valid and invalid targets).
- Stat impact while editing: Ledger rows with deltas against the saved state.
- Preset management: save, rename and copy; locked presets beyond the free limit, with copy and recovery; and SaveStatus.
- Snapshot awareness: "Arena defence uses a snapshot from 12 Sep · Update".
- Leaving with unsaved or failed changes.

**Examples in LegendsLegacy:**
The Essence loadout editor, equipment presets in Inventory, and the Combat Styles build preview.

**Paste-ready Claude Design instruction**

```text
Add a PatternLoadoutEditor composition and document the build summary and loadout editor pattern in the
Grimoire Design System. This addresses a known LegendsLegacy problem: equipment, Essence loadouts and the
Combat Style are spread across pages without one clear account of the build that will be used. Anatomy. A
BuildSummary header showing the three axes with their sources and the activities that use them. Slot areas:
the eight EquipmentSlots, the ordered Essence LoadoutSlots with the Conduit source rule, and the Combat Style.
A source list in master–detail form. Assignment: select a slot, then choose from the filtered list (the
primary method); drag and drop is secondary, with valid and invalid target states. Stat impact: Ledger rows
with DeltaValues against the saved state while editing. Preset management: save, rename and copy; presets
beyond the free limit shown locked, with copy and recovery available; SaveStatus with autosave, and the
failed-save state blocking further edits until Retry. Snapshot awareness: "Arena defence uses a snapshot from
12 Sep · Update snapshot". Leaving with unsaved or failed changes asks first. Keep the equipment, Essence and
Combat Style scopes labelled separately. Preview the Essence loadout editor with one slot being reassigned,
and the same editor after a failed save.
```

---

#### DS-097 — Progression Milestone Pattern

**Category:** Pattern
**Priority:** Medium
**Depends On:** DS-044, DS-085, DS-086

**Purpose:**
The Tower ascent rail and the Prophecy weekly milestone track are cited as good, identity-giving structures. Quest chains, Combat Style mastery milestones and level-up unlocks share the same logic: a sequence with a current position and the next reward.

**Should Define:**

- Anatomy: Track or GraphNode sequence, current position, the next milestone with its reward and distance ("420 Favor to next cache"), and claimable milestones.
- Past milestones collapse; future ones show only the next few, following the DS-065 teaser policy.
- Horizontal and vertical orientations.
- Integration with UpgradePreview and the claim flow.

**Examples in LegendsLegacy:**
The World Tower ascent, the weekly Prophetic Favor, Combat Style mastery and a quest chain.

**Paste-ready Claude Design instruction**

```text
Add a PatternMilestones composition to the Grimoire Design System for sequences with a current position and a
next reward. Anatomy: Track (or GraphNodes for branching sequences), the current position, the next milestone
with its reward (RewardBundle) and the distance to it ("420 Prophetic Favor to the next cache"), and claimable
milestones using the claim states. Past milestones collapse into a summary ("Floors 1–11 cleared"); future
milestones show only the next few, per the unlock teaser policy. Support horizontal orientation (weekly
tracks) and vertical orientation (the World Tower ascent). Selecting a milestone opens its detail or
UpgradePreview. Preview the World Tower ascent with a realm First Clear marker, the weekly Prophetic Favor
track with one claimable cache, and a Combat Style mastery track.
```

---

#### DS-098 — Leaderboard Layout

**Category:** Pattern
**Priority:** Medium
**Depends On:** DS-038, DS-041, DS-045, DS-084

**Purpose:**
The leaderboard redesign plan asks for clearer information architecture, a compact header rather than a large hero, and trustworthy provenance. Local Arena and Guild rankings and the global Leaderboard serve different purposes, and the audit says that difference should be understood rather than removed.

**Should Define:**

- An "own standing" strip: your rank, metric value, distance to the next rank and movement. No podium hero.
- A scope selector (global, guild, season) and a metric selector, as tabs or a Select.
- The ranking table (DS-084) with a pinned own row, pagination and "Jump to my rank".
- Provenance: scope, season and last updated.
- Context rankings embedded in features (Arena, Guild) against the global browser. When each is used.

**Examples in LegendsLegacy:**
The global Leaderboard, Arena rankings inside the Colosseum, and Guild rankings.

**Paste-ready Claude Design instruction**

```text
Add a PatternLeaderboard composition to the Grimoire Design System. Structure. An "own standing" strip at the
top: the player's rank, metric value, distance to the next rank ("1,240 Rating to rank 41") and movement. This
replaces any podium or hero art. A scope selector (Global, Guild, Season) and a metric selector (Select or
destination tabs, depending on how many boards there are). The ranking DataTable using RankingRow, with the
player's own row pinned, pagination and "Jump to my rank". Provenance ("Season 3 · Updated 5 min ago").
Document when a context ranking embedded in a feature (Arena rankings in the Colosseum, Guild rankings in the
Guild) is used versus the global Leaderboard browser, and keep both, since they serve different purposes.
Preview the global Leaderboard and an Arena ranking tab inside the Colosseum.
```

---

#### DS-099 — Activity Feed & History

**Category:** Pattern
**Priority:** Medium
**Depends On:** DS-028, DS-040, DS-045, DS-082

**Purpose:**
Record of Battle, trade history, personal expeditions, guild activity and loot history are all chronological, filterable and linkable to replays or detail. The audit asks for trustworthy histories, including "old data unavailable" states.

**Should Define:**

- Entry anatomy: time, actor, event, result (a DeltaValue or reward), and a link to a replay or detail.
- Grouping by day with sticky headers, filters, and "Load more".
- Retention and unavailable states ("History before 1 Sep is no longer available").
- The rule that significant events (a first clear, a record) stand out quietly from routine entries.

**Examples in LegendsLegacy:**
Record of Battle, Bazaar trade history, Tower personal expeditions, guild activity and loot history.

**Paste-ready Claude Design instruction**

```text
Add a PatternActivityFeed composition to the Grimoire Design System for chronological histories. Entry
anatomy: time (following the Time rules), actor (PlayerName), event in the shared event grammar, result
(DeltaValue, Amount or compact RewardBundle), and a link to a replay or detail where one exists. Group entries
by day with sticky headers; add filters by event type; use "Load more" rather than infinite scroll. Define
retention and unavailable states ("History before 1 Sep is no longer available") so missing data never looks
like an empty history. Significant events (a first clear, a new record, a tournament win) stand out quietly
from routine entries by weight or a marker, never by glow. Use Compact density. Preview Record of Battle
(Arena attacks and defences with rating deltas and replay links), Bazaar trade history, and a guild activity
feed.
```

---

#### DS-100 — Party & Roster Assembly

**Category:** Pattern
**Priority:** High
**Depends On:** DS-064, DS-079, DS-080

**Purpose:**
Tower expeditions (applications, parties), raid muster (three parties feeding one Final Assault), tournament teams of three and the regional boss all assemble people and builds under deadlines. The audit says party comparison cannot be hidden behind separate decorative profiles.

**Should Define:**

- Slots: filled, open, locked and reserved, with role labels where roles exist.
- CombatantSummary per member: readiness, captured build and presence.
- A readiness summary ("2 / 3 ready") and the requirement to start.
- Joining: invite and apply flows, and the leader's Accept and Decline.
- Assignment: click-to-assign (primary) and drag (secondary).
- The deadline or timer, and what happens at expiry.
- Multi-party structures (raid): three party rosters and their contribution to the Final Assault.
- Live changes that never move the control a player is about to press.

**Examples in LegendsLegacy:**
Tower expedition parties, raid muster and the party builder, tournament teams and the regional boss participation list.

**Paste-ready Claude Design instruction**

```text
Add a PatternRoster composition to the Grimoire Design System for assembling parties and teams under
deadlines. Slots are filled, open, locked or reserved, with role labels where the activity has roles. Each
member is a CombatantSummary in Row form showing readiness, captured-build state and Presence. A readiness
summary ("2 / 3 ready") sits beside the start or submit action, with its RequirementList. Joining: invite and
apply flows, including the leader's Accept and Decline for applicants. Assignment is click-to-assign as the
primary method, with drag as a secondary method. A Countdown shows the deadline and states what happens at
expiry. For raids, show three party rosters and how each party feeds the Final Assault, using GraphNodes if
that helps. Live roster changes must be noticeable without moving the control a player is about to press.
Preview a World Tower expedition party with one applicant, raid muster with three parties, and a tournament
team of three with one member whose snapshot is outdated.
```

---

#### DS-101 — Live Event & Phase-Based Activity

**Category:** Pattern
**Priority:** Medium
**Depends On:** DS-047, DS-062, DS-081

**Purpose:**
The regional boss (scheduled, live, completed, Fury and revival), raids, tournament registration and rounds, and Tower expeditions change what matters by phase. The audit found that the live event resembles just another combat report.

**Should Define:**

- A phase model: scheduled, registration, live, resolving, completed and cancelled. Each has its primary content and actions.
- Live state: shared boss health or progress, the player's own contribution, and revival or participation state.
- Late-join and reconnect behaviour.
- Updates that preserve an active decision.
- Transitions between phases, with the server authoritative ("Updating…").

**Examples in LegendsLegacy:**
The regional boss, tournament registration and rounds, raid playback and Tower expeditions.

**Paste-ready Claude Design instruction**

```text
Add a PatternLiveEvent composition to the Grimoire Design System for activities whose content changes by
phase. Phases: scheduled (SchedulePhase, requirements, reminder), registration (eligibility, team or roster,
deadline), live (shared state such as boss Health and stagger via BossFrame, the player's own contribution,
revival or participation state), resolving ("Updating…" until the server confirms), completed (CombatResult
and rewards), and cancelled (the reason). For each phase, define the primary content, the one primary action
and what recedes. Define late-join and reconnect behaviour, including a page-level "Reconnecting…" alert and
resuming without losing context. Live updates never disturb an active decision. Preview the regional boss in
scheduled, live (with Fury and the player's contribution) and completed phases, and tournament registration
moving to round 1.
```

---

#### DS-102 — Return Summary (Offline Progress)

**Category:** Pattern
**Priority:** Medium
**Depends On:** DS-034, DS-073, DS-083

**Purpose:**
Idle and offline combat resolves on the server, and the game shows a session summary on return. The audit's entry journey asks for the return to show who you are, what happened, what continues and what is next. The summary must not become a wall.

**Should Define:**

- Anatomy: time away, what was resolved (encounters, XP, levels, loot, Essences), the retention limit (24h free, 168h with Nobility) and any time not retained, what continues now (the current action), and the next suggested step.
- A collapsible detail list and a link to the loot history.
- Presentation: a dialog on login, or a panel. Dismissible, and able to be reopened.
- Honest wording about lost time, with no upsell pressure (DS-104).

**Examples in LegendsLegacy:**
The login after a night offline, and a return after the free retention window was exceeded.

**Paste-ready Claude Design instruction**

```text
Add a PatternReturnSummary composition to the Grimoire Design System for LegendsLegacy's return-from-offline
summary. Anatomy: time away ("Away for 9h 12m"); what was resolved (encounters fought, Combat XP and levels
gained, loot as a compact RewardBundle, new Essences); the offline retention limit and any time not retained,
stated plainly ("24h retained; nothing was lost" or "3h beyond the 24h limit were not retained"); what
continues now (the current action, with View and Stop); and one suggested next step (the pinned objective).
Details are collapsible, with a link to the full loot history. It opens as a dialog on login, can be
dismissed, and can be reopened from the current-action area. Mention Nobility's longer retention only as a
neutral fact where time was lost, following the premium presentation rules, never as a sales prompt. Preview a
normal overnight return and a return where retention was exceeded.
```

---

#### DS-103 — Guidance, Onboarding & Locked Features

**Category:** Pattern
**Priority:** Medium
**Depends On:** DS-004, DS-065

**Purpose:**
First Steps guidance, pinned objectives, page guides (help JSON with a `lastReviewed` date), the first-party tour spotlight and journey-gated navigation prevent disorientation for new players. Grimoire has JourneyCard. The pattern must keep guidance accurate and never block the action it points to.

**Should Define:**

- The layers: JourneyCard (the stage and next step), the pinned objective (shell), page guides (Drawer), a tour spotlight, and contextual hints (one-time, dismissible).
- Rules:
  - Guidance never covers the committing action or critical values.
  - It is always dismissible and able to be reopened.
  - Targets must exist, so a guidance step fails gracefully when its target is gone.
- Revealing features in the NavRail as the journey progresses (DS-065).
- Page guide content anatomy: purpose, main workflow, requirements, outcomes and common blockers. This follows the page guides plan.

**Examples in LegendsLegacy:**
A new character's First Steps, the Overview JourneyCard, the Essences page guide and the tour over the NavRail.

**Paste-ready Claude Design instruction**

```text
Add a PatternGuidance composition to the Grimoire Design System, covering LegendsLegacy's guidance layers.
JourneyCard (existing): the current journey stage and the recommended next step. Pinned objective in the
shell. Page guides in a Drawer, whose content anatomy follows the game's page guide plan: purpose, main
workflow, requirements, outcomes, common blockers, and a last-reviewed date. A tour spotlight over a target
element. Contextual one-time hints. Rules: guidance never covers the committing action or the values the
player needs to decide; every guidance element is dismissible and can be reopened from help; a tour step whose
target no longer exists is skipped gracefully. Define how features appear in the NavRail as the First Steps
journey progresses, using the locked and unlock-preview rules. Guidance copy follows the voice rules: short
imperatives, no celebration. Preview a new player's Character Overview with the JourneyCard and pinned
objective, the Essences page guide drawer, and a tour spotlight on the World Map nav item.
```

---

#### DS-104 — Premium & Nobility Presentation

**Category:** Pattern
**Priority:** High
**Depends On:** DS-053, DS-066, DS-094

**Purpose:**
Nobility is a Signet-based membership. Each Signet adds a month. Signets are tradable for Cinders on the Bazaar. Benefits are defined (offline retention, extra loadouts, Arena ticket cap, Creature Focus cooldown, market capacity, an extra reroll, the ◆ mark), and so are display rules. Premium UI is where generic game design most often slides into dark patterns. The audit also noted that Nobility dominates Settings, when guests usually come to Settings to bind an account.

**Should Define:**

- Nobility status: active until a date, and the "Show perks" list of benefits with exact values and the free comparison.
- The Signet redemption flow (quantity, the extension preview, the bound-account requirement), reusing DS-094.
- In-context capacity notes ("3 free · 6 with Nobility"), shown only where the limit is reached or visible. Neutral, and never modal.
- Mark rules from DS-066.
- Anti-dark-pattern rules: no countdown pressure, no fake scarcity, no interrupting upsell dialogs, no crossed-out "free" features, and no styling that makes free players' UI look broken.
- Settings placement: account binding first for guests, and Nobility as its own section.
- The future direct-support badge, reserved and separate from membership.

**Examples in LegendsLegacy:**
Settings → Nobility, the Signet redemption dialog, the loadout preset limit, market listing capacity and offline retention in the return summary.

**Paste-ready Claude Design instruction**

```text
Add a PatternNobility composition and a "Premium presentation" page to the Grimoire Design System.
LegendsLegacy's Nobility is a membership extended by redeeming Signets, one month each. Signets can be traded
on the Cinder Bazaar. Benefits include 168h offline retention instead of 24h, 6 Essence and 6 equipment
loadouts instead of 3, an Arena ticket cap of 8 instead of 5, a 2h Creature Focus cooldown instead of 8h, 30
market listings and orders instead of 10, an extra free Prophecy reroll, and the optional ◆ mark. Define the
Nobility status view: "Nobility active until 14 Nov 2026" and a "Show perks" list with exact values beside the
free values. Define the Signet redemption flow using the Transaction pattern: quantity, extension preview, and
the bound-account requirement for guests. Define in-context capacity notes ("3 of 3 presets · 6 with
Nobility"), shown only where a limit is visible or reached, worded neutrally, and never as a modal or banner.
Apply the mark rules from the player identity components. Add explicit anti-dark-pattern rules: no countdown
pressure, no fake scarcity, no interrupting upsell dialogs, no crossed-out or greyed free features, no styling
that makes the free experience look broken or lesser. In Settings, account binding comes first for guests and
Nobility is its own section, not the page's largest element. Reserve, but do not design, the future permanent
support badge, which stays separate from membership. Preview the Settings Nobility section, the redemption
dialog, and a loadout preset limit note.
```

---

### Phase 5 — Application Shell & Page Archetypes

#### DS-105 — NavRail Rules

**Category:** Shell
**Priority:** High
**Depends On:** DS-024, DS-065, DS-088

**Purpose:**
NavRail exists, with sections, compact mode, badges and a locked state. The audit asks whether all fifteen destinations need equal persistent weight, notes that access changes with the journey, and says new systems will keep arriving. The rail needs rules for growth.

**Should Define:**

- The group model: Character, World, City and System (plus an Economy group if it becomes real), and where new features go.
- Item anatomy: icon, label, badge or marker (DS-088), and the locked or hidden state (DS-065). The current-location diamond is kept.
- Compact mode: icons only, with tooltips and the badges kept.
- The journey reveal rule: hidden before the relevant journey stage, then revealed with a "new" marker.
- Capacity: a maximum per group, and what happens when a group overflows (sub-destinations become tabs, not rail items).
- The current-action block above the navigation (DS-107).
- The drawer on narrow screens, and keyboard navigation.

**Examples in LegendsLegacy:**
Adding Stronghold (City or Character?), and adding a new seasonal activity.

**Paste-ready Claude Design instruction**

```text
Expand the NavRail documentation in the Shell section of the Grimoire Design System into rules for growth, and
update the component where needed. Define the group model (Character, World, City, System, plus Economy if it
becomes a real group) and a placement rule for new features: which group, and when a feature becomes a
destination tab inside an existing item instead of a new rail item. Set a maximum number of items per group.
Item anatomy: icon, label, a single badge or marker following the Attention page, the current-location
diamond, and locked or hidden states following the locked-content rules. Compact mode: icons only, with
tooltips and badges kept. Journey reveal: destinations are hidden until the relevant First Steps stage or
level, then appear with a "new" marker that clears on first visit. Define the current-action block that sits
above navigation. Define the narrow-screen drawer and keyboard navigation (arrow keys within the rail).
Include a worked example of adding a planned feature (Stronghold) and a seasonal activity to the rail.
```

---

#### DS-106 — TopBar & Global Resources

**Category:** Shell
**Priority:** High
**Depends On:** DS-024, DS-054, DS-063

**Purpose:**
TopBar carries identity and global wealth. The health bar was just removed from it, which was the right call. The implementation plan also warns against adding a notification bell just because reference images have one. The TopBar needs a fixed content policy.

**Should Define:**

- Content: character name, LevelBadge, Cinders and Soulstones (Amount in header size, with the abbreviation toggle), a help or guide entry, and the centre slot for in-run progress.
- Exclusions: health, feature currencies, notifications (unless DS-109 decides otherwise), and decorative elements.
- An optional secondary row for the pinned objective, when there is one.
- Behaviour on narrow screens.

**Examples in LegendsLegacy:**
The standard game screen, and the dungeon run with a Track in the centre slot.

**Paste-ready Claude Design instruction**

```text
Expand the TopBar documentation in the Grimoire Design System into a fixed content policy. Include: character
name, LevelBadge, Cinders and Soulstones as header-size Amounts (keeping the abbreviation toggle and the full
value on hover and focus), a help or page-guide entry, and the existing centre slot for in-run progress (a
Track during a dungeon or expedition). Explicitly exclude: health (recently removed, and it stays out),
feature currencies (they belong to the feature's resource header), a notification bell (unless the
notification routing decision adds one), and decorative elements. Define an optional secondary row for the
pinned objective when the player has one, rather than squeezing quest text between currencies. Define
narrow-screen behaviour (the menu button, abbreviated amounts). Preview the standard TopBar, a dungeon run
with a Track in the centre slot, and the pinned-objective row.
```

---

#### DS-107 — Current Action Indicator

**Category:** Shell
**Priority:** High
**Depends On:** DS-024, DS-044, DS-062

**Purpose:**
The current action (idle combat, an active dungeon, an active raid or expedition) is persistent in the shell, with progress, resume and Stop. It is one of the game's continuity anchors, and the audit says it must be preserved through any redesign.

**Should Define:**

- Anatomy: activity type, location or name, progress (a Meter or Track), status, and View and Stop.
- States: running, paused or offline, completing, stopped, and error with Retry.
- Several concurrent activities (for example idle combat plus a pending raid): which one leads, and how the others are summarised.
- The compact form for the compact rail and for narrow screens.
- The link to the return summary (DS-102).

**Examples in LegendsLegacy:**
"Idle combat · Wolfsbane Reach · View · Stop", an active dungeon, and a raid in progress.

**Paste-ready Claude Design instruction**

```text
Add a CurrentAction component to the Shell section of the Grimoire Design System (formalising the idle-combat
block shown above the NavRail in current concepts). Anatomy: activity type label ("Idle combat", "Dungeon",
"Raid", "Tower expedition"), location or name ("Wolfsbane Reach"), progress (a thin Meter or a Track), status,
and View and Stop actions. States: running, paused or offline, completing, stopped, error with Retry. When
several activities exist at once (idle combat plus an active raid), define which one leads and how the others
are summarised ("+1 active"). Provide a compact form for the compact NavRail and narrow screens. Link it to
reopen the return summary. Stop is a quiet action, not danger, unless stopping loses something, in which case
the stakes rules apply. Preview idle combat running, an active dungeon, and the error state.
```

---

#### DS-108 — Chronicle Standards (Chat & Game Log)

**Category:** Shell
**Priority:** High
**Depends On:** DS-024, DS-066, DS-068, DS-082

**Purpose:**
The Chronicle merges chat and the game log across nine channels, with docked, floating and bottom-dock layouts. The audit's social continuity requirements are a clear channel and recipient, mentions, item links, presence and profile access. Moderation and density rules do not exist yet.

**Should Define:**

- Message anatomy: time, speaker (PlayerName), and text. System and loot lines use the shared event grammar.
- The composer: channel and recipient clarity (whisper target), item-link insertion, character limit and send state.
- Mentions: highlight and attention.
- ItemLink hover cards inside chat.
- Moderation states: muted, removed message, reported and rate-limited.
- Unread and mention counts per channel, and the collapsed strip or ticker.
- Density, virtualisation, and scroll lock when the player has scrolled up ("12 new messages ↓").
- Keyboard: focus-chat shortcut, channel switching and Escape.

**Examples in LegendsLegacy:**
Global chat, a guild channel, whispers, the loot channel and system lines ("The Guardian of Floor 12 has fallen").

**Paste-ready Claude Design instruction**

```text
Expand the Chronicle documentation in the Grimoire Design System into full chat and game-log standards. Keep
its channel model (general, trade, help, guild, whisper, raid, invites, system, loot) and its docked, floating
and bottom-dock layouts. Message anatomy: time, speaker as PlayerName (with the player menu), and text in ink.
System and loot lines use the shared event grammar in the lore-small style. The composer: the current channel
and whisper recipient always visible ("To Kaelen"), item-link insertion from inventory, a character limit and
a send state. Mentions: highlighted lines plus a mention count on the channel. ItemLinks open ItemCard hover
cards. Moderation states: muted player, removed message, reported, rate-limited ("Slow down · 3s"). Unread and
mention counts per channel and on the collapsed strip or ticker. Scroll lock when the player scrolls up, with
a "12 new messages ↓" jump. Virtualisation for long histories. Keyboard: a focus-chat shortcut, switching
channels, Escape back to the game. Preview docked Chronicle with a whisper, a mention, an item link hover, a
loot line and a system line, plus the collapsed strip with counts.
```

---

#### DS-109 — Attention Hierarchy & Notification Routing

**Category:** Shell
**Priority:** High
**Depends On:** DS-034, DS-046, DS-088

**Purpose:**
The game currently signals through header and quest indicators, in-feature dots, toasts, an update dialog, the session summary and chat notices, with no order of precedence. A routing table decides where each event goes. It also answers whether a notification centre is actually needed.

**Should Define:**

- The channel ladder, from most to least intrusive: blocking dialog, toast, inline alert, attention marker, Chronicle system line, and the return summary.
- A routing table by event type: loot, rare drop, claimable reward, guild invite or application, whisper, failed save, restriction, maintenance, app update, event starting, tournament round ready and raid started.
- Batching and rate limits (for example at most one toast per N seconds, with others folded into the Chronicle).
- A recommendation, with reasoning, on whether an inbox or notification centre is needed, or whether the Chronicle system channel covers it.

**Examples in LegendsLegacy:**
A tournament round starting while the player is in the Bazaar, a raid invite, and a failed loadout save.

**Paste-ready Claude Design instruction**

```text
Add an "Attention and notification routing" page to the Shell section of the Grimoire Design System. Define
the channel ladder from most to least intrusive: blocking dialog (only for what must be acknowledged, such as
an app update or an account restriction), toast, inline alert, attention marker (badges and markers),
Chronicle system or loot line, and the return summary. Write a routing table assigning each LegendsLegacy
event type to one primary channel and at most one secondary channel: routine loot, rare drop, claimable
reward, guild invite, guild application (officers), whisper, mention, failed save, account restriction,
scheduled maintenance, app update, regional boss starting soon, tournament round ready, raid started, Arena
defence result. Define batching and rate limits: for example at most one toast every few seconds, with the
rest folded into the Chronicle; repeated events collapse ("3 new guild applications"). Then evaluate whether
LegendsLegacy needs a notification centre or inbox, or whether the Chronicle system channel plus attention
markers covers it. Recommend one with reasons and record it in the Decision Log. Do not add a bell icon just
because game UIs often have one.
```

---

#### DS-110 — Page Archetype Catalogue

**Category:** Page Archetype
**Priority:** High
**Depends On:** DS-024, DS-089, DS-090, DS-091

**Purpose:**
Archetypes are what make new screens assembled rather than invented. The catalogue maps every current route to one archetype, and requires every new feature to choose one.

**Should Define:**

- The 14 archetypes (DS-111 to DS-124). For each: purpose, shell mode (Page, Stage or Workbench), default density, required patterns, optional patterns, and features that use it.
- A route mapping table covering all current player routes and major tabs.
- A rule for hybrids (for example the Colosseum is a Feature Hub with Ranking and Activity Selection tabs).
- The rule that every new feature declares its archetype before any screen is designed.

**Examples in LegendsLegacy:**
The whole route map: Character, World, City, Quests, Prophecies, Settings and Combat.

**Paste-ready Claude Design instruction**

```text
Add a Page Archetypes section overview to the Grimoire Design System: a catalogue of the 14 page archetypes
LegendsLegacy uses. They are Feature Hub, Character & Build, Collection Browser, Inventory & Exchange
Workbench, Activity Selection & Preparation, Encounter & Live Activity, Ranking & Records, Social & Guild,
Progression Track & Tree, Management & Building, Objectives & Rewards, Activity History, Detail & Profile, and
Settings & Account. For each, give: purpose in one sentence, shell mode (Page, Stage or Workbench), default
density, required patterns, optional patterns, and the features that use it. Then add a route mapping table
covering the game's current player routes and major tabs: Character Overview, Inventory, Essences (Archive,
Absorb, Creatures, Codex), Combat Styles, Achievements, Soulstones, World regions (Shenic, Meran), dungeon
run, raid, World Tower (and expeditions, personal expeditions, Hall of Fame), regional boss, Guild (discovery,
headquarters, Vault, buildings, missions, shop, rankings), Colosseum (Arena, Tournament, Champion Market,
rankings, Record of Battle), Cinder Bazaar, Leaderboard, Quests, Prophecies, Settings, and the combat viewer.
Define how hybrids are documented (for example the Colosseum is a Feature Hub whose tabs use the Ranking and
Activity Selection archetypes). Add the rule that every new feature declares its archetype before any screen
is designed. Keep ScreenOverview and ScreenArchive as worked examples and label which archetype each
demonstrates.
```

---

#### DS-111 — Archetype: Feature Hub

**Category:** Page Archetype
**Priority:** Medium
**Depends On:** DS-077, DS-088, DS-089, DS-110

**Purpose:**
The "overview or dashboard" pages (Colosseum home, World Tower overview, Guild headquarters, region overview) are where the audit found stat tile grids and SaaS character. The hub must answer "what needs me, and where do I go" instead of "here are seven numbers".

**Should Define:**

- Structure:
  - A feature header with a resource header.
  - A "Needs attention" block: claimables, required actions and deadlines, or nothing when empty.
  - One primary next action.
  - Entry points to sub-areas (ActivityCard rows or tabs).
  - A single-line status summary instead of tile grids.
  - A short recent-activity feed.
- The anti-dashboard rule: at most one headline figure, no tile grids, and summary numbers only if they change the next decision.
- Page mode, Standard density.

**Examples in LegendsLegacy:**
Colosseum home, the World Tower overview, Guild headquarters and the region overview.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeFeatureHub composition to the Grimoire Design System, for LegendsLegacy feature landing pages:
Colosseum home, World Tower overview, Guild headquarters and region overviews. These are where the previous UI
drifted into SaaS dashboards with grids of equal stat tiles, so the hub must answer "what needs me, and where
do I go". Structure: a Feature header with its resource header; a "Needs attention" block listing claimables,
required actions and deadlines (it disappears when empty, never showing "nothing to do" filler); one primary
next action; entry points to the feature's sub-areas as ActivityCard rows or destination tabs; a single status
line instead of tiles ("Rating 1,842 · Rank 41 · 3 / 5 tickets"); and a short PatternActivityFeed of recent
events. Anti-dashboard rules: at most one headline figure; no grids of stat tiles; a summary number appears
only if it changes the next decision. Page mode, Standard density. Demonstrate with the Colosseum home and
Guild headquarters, the latter showing different content for an ordinary member and an officer.
```

---

#### DS-112 — Archetype: Character & Build Management

**Category:** Page Archetype
**Priority:** High
**Depends On:** DS-056, DS-079, DS-096, DS-110

**Purpose:**
The Character Overview is the reference screen for the redesign, and your stated preference is its information-dense content over decorative layouts. This archetype turns ScreenOverview into a template that also serves Combat Styles and another player's profile.

**Should Define:**

- Structure: identity (PlayerHeader, LevelPlate, Combat Rating as the one headline figure), then the build (BuildSummary, equipment, Essences and Combat Style), then attributes (a Section grid of Ledgers with breakdown links).
- Own profile against another player's profile: the JourneyCard and management links appear only on your own; presence and "Back to my profile" appear on others'.
- A Combat Styles variant: selected against preview, mastery, refinements and upgrades, with the conditions under which the Style operates.
- Page mode. No portrait required.

**Examples in LegendsLegacy:**
The Character Overview (own and others'), and Combat Styles.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeCharacterBuild composition to the Grimoire Design System, generalising ScreenOverview into a
template. LegendsLegacy's preferred direction is the information-dense Character Overview, not a decorative
layout. Structure: identity first (PlayerHeader, LevelPlate for Combat XP, Combat Rating as the single
headline figure); then the build (BuildSummary with the three axes, the eight EquipmentSlots in text-first
form, the ordered Essence LoadoutSlots, and the Combat Style with mastery); then Combat Attributes as Sections
containing Ledgers for Offense, Defense, Recovery and Utility, each row explainable, with breakdown links.
Define own-profile against other-player differences: the JourneyCard and management links appear only on the
player's own profile; Presence, a read-only build and "Back to my profile" appear when viewing others, showing
only data the server exposes for other players. Add a Combat Styles variant: the selected Style against an
unsaved preview (BuildStateLabel), mastery via Sigil and Meter, refinements and upgrades with their
requirements, and a plain statement of the conditions under which the Style's mechanic operates (for example
Conduit uses the first occupied Essence slot). Page mode; no portrait required, per the Art-optional contract.
Verify it fits at 1600×900 with the NavRail and docked Chronicle open.
```

---

#### DS-113 — Archetype: Collection Browser

**Category:** Page Archetype
**Priority:** High
**Depends On:** DS-076, DS-091, DS-110

**Purpose:**
The Soul Archive, Creatures, the Essence Codex, Achievements and Titles are collections that, according to the audit, must not look interchangeable. ScreenArchive already shows the Stage variant. This archetype defines both the Stage and Page variants, and what each collection emphasises.

**Should Define:**

- Structure: header and tabs, FilterBar, list (the stage list or rows), Folio or inspector, and CollectionProgress where relevant.
- When the Stage variant (art atmosphere) is allowed, and when the Page variant is required.
- What each collection emphasises:
  - Soul Archive: the build role.
  - Creatures: known against missing, and where to find them.
  - Codex: completion bonuses.
  - Achievements: recognition.
  - Titles: identity.

**Examples in LegendsLegacy:**
Essences (Archive, Creatures, Codex), Achievements and Titles.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeCollection composition to the Grimoire Design System, with a Stage variant (building on
ScreenArchive) and a Page variant. Structure: a Feature header with destination tabs, FilterBar, the
collection (the stage list over art in the Stage variant; Rows or a DataTable in the Page variant), a Folio or
inspector for the selection, and CollectionProgress where the collection has completion. State when the Stage
variant is allowed (browsing identity-rich entities, such as creatures, where art atmosphere helps) and when
the Page variant is required (dense or administrative collections). Then specify what each LegendsLegacy
collection emphasises, so they pass the sameness test. Soul Archive: the Essence's role in builds (abilities,
level and tier, attunement). Creatures: known against missing, and where to find them (habitat, Focus).
Essence Codex: collection completion and the bonus it grants. Achievements: recognition and Renown. Titles:
public identity and what is displayed. Demonstrate the Soul Archive in the Stage variant and Achievements in
the Page variant.
```

---

#### DS-114 — Archetype: Inventory & Exchange Workbench

**Category:** Page Archetype
**Priority:** High
**Depends On:** DS-087, DS-090, DS-093, DS-094, DS-110

**Purpose:**
Inventory, the Guild Vault, the Cinder Bazaar, the Champion Market and shops are high-density workbenches where comparison and trustworthy numbers matter most. The audit lists adjacent comparison as a functional requirement.

**Should Define:**

- Workbench mode, Compact density.
- Structure: tabs (Gear, Stock, Loadouts; or Buy, Sell, My Orders), FilterBar, list, an inspector pane with comparison, and the transaction area.
- Capacity (inventory slots and listings), bulk actions with protection rules, and guild ownership rules in the Vault.
- Responsive behaviour: three panes, then two, then one, with Back.

**Examples in LegendsLegacy:**
Inventory, the Guild Vault, the Cinder Bazaar and the Champion Market.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeWorkbench composition to the Grimoire Design System for LegendsLegacy's exchange and inventory
screens: Inventory, Guild Vault, Cinder Bazaar, Champion Market and shops. Use Workbench mode (no stage art)
with Compact density. Structure: a Feature header (a resource header on shops and the Bazaar), destination or
view tabs (Inventory: Gear, Stock, Loadouts; Bazaar: Buy, Sell, My Orders), FilterBar, the item list (ItemRow
or DataTable), an inspector pane with ItemCard and EquipmentComparison, and a transaction area using the
Transaction pattern with PriceBreakdown. Include capacity displays (inventory slots, listing limits), bulk
actions with the protection rule, and Vault ownership states (guild property, borrowed by, return). Responsive
behaviour: list + inspector + comparison at wide widths, then list + inspector, then a single pane with Back,
following the Layout container breakpoints. Demonstrate Inventory with an item selected and compared, and the
Bazaar Buy view with an order book.
```

---

#### DS-115 — Archetype: Activity Selection & Preparation

**Category:** Page Archetype
**Priority:** High
**Depends On:** DS-064, DS-077, DS-078, DS-100, DS-110

**Purpose:**
Choosing where to fight and preparing for it (region areas, dungeon preview and Sigil assembly, Tower scouting and preparation, Arena opponent choice, raid preview) share a decision sequence: choose, check, understand the stakes, prepare, commit. The audit notes that the supporting panels currently compete with the consequences.

**Should Define:**

- Structure: an activity list, then a briefing (enemies, affinities, requirements, stakes, rewards), then preparation (build and party readiness, with BuildSummary and the Roster), then commit (one solid action).
- Identity from the activity's data (enemies, route, Guardian), with optional art (DS-023).
- Page or Stage mode, depending on art. Standard density.

**Examples in LegendsLegacy:**
Shenic and Meran areas, the dungeon preview, Tower scouting and preparation, Arena opponent selection and the raid preview.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeActivityPrep composition to the Grimoire Design System for choosing and preparing activities
in LegendsLegacy: region areas, dungeon preview and Sigil assembly, World Tower scouting and preparation,
Arena opponent selection, and raid preview. It follows one decision sequence. Choose: an ActivityCard list, or
CombatantSummary rows for Arena. Check: a briefing for the selected activity with EnemyGroup, element
affinities, RequirementList and EligibilitySummary. Understand the stakes: StakesSummary with costs, rewards
(First Clear or Echo where relevant) and risks. Prepare: BuildSummary with the snapshot state, and
PatternRoster for group activities. Commit: one solid action. The consequences of the choice must outrank
supporting panels. Identity comes from the activity's data (enemies, route, Guardian), with optional art per
the Art-optional contract; never reuse one generic image for every area. Page mode by default; Stage mode only
if real regional art exists. Demonstrate a dungeon preview with Sigil assembly, and World Tower preparation
for a floor with First Clear available.
```

---

#### DS-116 — Archetype: Encounter & Live Activity

**Category:** Page Archetype
**Priority:** High
**Depends On:** DS-081, DS-082, DS-083, DS-086, DS-101, DS-110

**Purpose:**
The combat viewer, dungeon run, raid playback, regional boss and tournament replay are the game's live moments. The audit's core point: the decisive state (the next route choice, Vigor, who is alive, the boss phase) must read first, and the full analytic report is a deeper layer. Playback controls are presentation, not tactics.

**Should Define:**

- Structure: an activity header (phase, timer, stakes), then the decisive state (unit frames, route graph, BossFrame), then player controls (Stop, Retreat, route choice), then the result, with the log as a deep layer.
- A playback-controls rule: Skip and instant playback are labelled as viewing controls, not abilities or outcome changes.
- Context variants: idle combat, dungeon run (the route and Pending Loot), raid, regional boss and tournament replay.
- Stage mode allowed. Live update rules (DS-047, DS-017).

**Examples in LegendsLegacy:**
The combat viewer, an active dungeon, raid playback, the regional boss and a tournament replay.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeEncounter composition to the Grimoire Design System for LegendsLegacy's live moments: the idle
combat viewer, an active dungeon run, raid playback, the regional boss and tournament replays. Hierarchy: an
activity header (phase, Countdown, compact StakesSummary), then the decisive state (UnitFrames and BossFrame,
or the dungeon route built from GraphNodes with only revealed rooms), then the controls that actually exist
(Stop, Retreat, choose the next room), then the result (CombatResult), with the CombatLog as the deepest layer
and never the first thing shown. Add a playback-controls rule: combat is automatic, so Skip and instant
playback are labelled as viewing controls ("Skip to result") and must never look like abilities or imply a
different outcome. Context variants: idle combat (compact), dungeon run (route, Vigor, Pending Loot, Retreat
decision), raid (three parties and the Final Assault), regional boss (live Fury, stagger and personal
contribution), tournament replay (round context and breadcrumbs). Stage mode is allowed. Follow the
live-update rules so updates never disturb a decision. Demonstrate the dungeon run at a route choice and the
regional boss live.
```

---

#### DS-117 — Archetype: Ranking & Records

**Category:** Page Archetype
**Priority:** Medium
**Depends On:** DS-084, DS-098, DS-110

**Purpose:**
The global Leaderboard, Arena and Guild rankings and the Hall of Fame are ordered comparisons whose trust depends on exact metrics, the player's own position and provenance.

**Should Define:**

- Structure: header, scope and metric selection, the own-standing strip, the ranking table and pagination.
- A Hall of Fame variant: records with provenance (date and party), where historic firsts are distinct from ordinary records.
- Page mode, Compact density.

**Examples in LegendsLegacy:**
The Leaderboard, Colosseum rankings, Guild rankings and the World Tower Hall of Fame.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeRanking composition to the Grimoire Design System using PatternLeaderboard, in Page mode with
Compact density. Structure: header; scope and metric selection; the own-standing strip; the ranking DataTable
with RankingRows, the pinned own row and pagination with "Jump to my rank"; and provenance. Add a Hall of Fame
variant for the World Tower: each record shows the floor, the party members, the date and the realm
first-clear status, so a historic first clear is visibly distinct from an ordinary personal record, without
trophies or glow. Demonstrate the global Leaderboard and the World Tower Hall of Fame.
```

---

#### DS-118 — Archetype: Social & Guild

**Category:** Page Archetype
**Priority:** Medium
**Depends On:** DS-041, DS-066, DS-088, DS-110

**Purpose:**
The audit asks whether the Guild should put administration first for ordinary members, or the current collective objective and participation. Guild members and permissions, the public guild profile and guild discovery need structures that express belonging and authority.

**Should Define:**

- The member view: guild identity, the current collective objective (a mission or building target), the player's own contribution, and the member list.
- The officer view adds: applications, the permission matrix, and administration placed behind a clear section. Disband and transfer controls sit low and are protected (DS-034, high-stakes).
- The public guild profile and guild discovery (a collection of guilds, with requirements to join).
- The members DataTable: role, presence, contribution and actions by permission.

**Examples in LegendsLegacy:**
Guild headquarters members, permissions, discovery and the public guild profile.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeGuild composition to the Grimoire Design System. Define a member view that leads with
belonging and purpose rather than administration: guild identity (name, tag, level), the current collective
objective (the active guild mission or building target, with progress), the player's own contribution, and the
member DataTable (PlayerPlate, role, Presence, contribution, and actions limited by permission). Define an
officer view that adds applications (with Accept and Decline), the permission matrix built from Checkbox and
DataTable, and administration grouped in a clearly separate section. Disband and leadership transfer sit at
the bottom and use the high-stakes ConfirmDialog. Add a public guild profile variant (identity, description,
requirements to join, members summary, Apply) and a guild discovery variant as a PatternCollectionBrowser of
guilds. Page mode, Standard density for headquarters, Compact for member tables. Demonstrate the member view,
the officer view and the public profile.
```

---

#### DS-119 — Archetype: Progression Track & Tree

**Category:** Page Archetype
**Priority:** Medium
**Depends On:** DS-085, DS-086, DS-097, DS-110

**Purpose:**
Soulstone constellations, the World Tower ascent, Combat Style mastery and the Prophecy weekly Favor track are investments along a structure. The structure itself should be the subject of the screen, which the audit and the Constellation component both support.

**Should Define:**

- Structure: the track or tree as the stage subject (Constellation, Track or GraphNodes), the selected node opening a Folio with UpgradePreview, and overall progress as a single line.
- Current investment and the next marginal benefit, with refund or reset consequences stated where they apply.
- Stage mode allowed.

**Examples in LegendsLegacy:**
The Soulstone Archive, the World Tower ascent and Combat Style mastery.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeProgression composition to the Grimoire Design System for investments along a structure:
Soulstone constellations, the World Tower ascent, Combat Style mastery and the weekly Prophetic Favor track.
The structure itself is the subject of the stage (Constellation, Track or GraphNodes). Selecting a node opens
the Folio with an UpgradePreview (current → next, cost, requirements, action). Overall progress is a single
line ("Constellation 34 / 60 nodes · 1,240 Soulstones invested"), not a tile grid. Show the next marginal
benefit clearly and state refund or reset consequences beside the action where the system has them. Stage mode
is allowed, using the existing Constellation treatment. Demonstrate the Soulstone Archive with a selected
affordable node, and the World Tower ascent with the current floor selected.
```

---

#### DS-120 — Archetype: Management & Building

**Category:** Page Archetype
**Priority:** Medium
**Depends On:** DS-062, DS-085, DS-110

**Purpose:**
Guild buildings (Guild Hall, Treasury, Market, War Room, Workshop, Sanctum and others) and the planned Stronghold are structures that the player owns or shares, each with levels, upgrade paths, costs, contributions and timers.

**Should Define:**

- Structure: a resource header, a list of structures (level, state, and "upgrade available" or "in progress"), the selected structure's UpgradePreview and benefits, and timers.
- Shared funding: contribution progress, and who can trigger the upgrade.
- An extensibility note for Stronghold and future building systems.

**Examples in LegendsLegacy:**
Guild buildings, and the planned Stronghold.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeManagement composition to the Grimoire Design System for owned or shared structures:
LegendsLegacy guild buildings (such as Guild Hall, Treasury, Market, War Room, Workshop, Sanctum) and the
planned Stronghold. Structure: a Feature header with a resource header (Guild Favor, Supplies or the relevant
currencies); a list of structures as Rows showing level, state ("Upgrade available", "Upgrading · 3h 12m",
"Max level") and one attention marker at most; the selected structure's current benefits and UpgradePreview in
the detail pane, including contribution progress for shared funding and who is allowed to trigger the upgrade;
and timers for construction. Page or Workbench mode, Standard density. Add a note on how Stronghold or any
future building system plugs in using registry entries and this archetype without new visuals. Demonstrate
guild buildings with one upgrade in progress and one awaiting contributions.
```

---

#### DS-121 — Archetype: Objectives & Rewards

**Category:** Page Archetype
**Priority:** Medium
**Depends On:** DS-074, DS-075, DS-095, DS-110

**Purpose:**
The Quest Journal, Prophecies and community events are lifecycle-driven: offered, accepted, in progress, claimable and claimed, with resets and expiries. The audit found their loops scattered across repeated claim and progress sections.

**Should Define:**

- Structure: grouping by lifecycle (claimable first, then active, then available, with completed collapsed), reset timers in the header, Claim all, and destination links.
- A Prophecies variant: three daily offers with reroll, the weekly Favor track (DS-097) and caches.
- A community events variant: shared progress and personal contribution.
- Page mode, Standard density.

**Examples in LegendsLegacy:**
The Quest Journal, Prophecies and community events.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeObjectives composition to the Grimoire Design System for the Quest Journal, Prophecies and
community events. Structure: a Feature header with reset Countdowns ("Daily reset in 5h 12m") and the relevant
resource header (Fate Echo, Sigil Fragments); ObjectiveRows grouped by lifecycle, with claimable first (and
Claim all), then active, then available offers, with completed collapsed; and destination links on every
actionable objective. Prophecies variant: the three daily offers with the set-wide reroll CostButton, the
weekly Prophetic Favor track via PatternMilestones, and caches that are claimed and then opened. Community
events variant: shared threshold progress with the player's own contribution and a clear split between
personal and shared rewards. Page mode, Standard density. Make sure the three variants are structurally
distinct, per the sameness test. Demonstrate Prophecies mid-week with one claimable cache, and a quest chain
with a branching choice.
```

---

#### DS-122 — Archetype: Activity History

**Category:** Page Archetype
**Priority:** Low
**Depends On:** DS-045, DS-099, DS-110

**Purpose:**
Personal expeditions, Record of Battle, trade history, guild activity and loot history are chronological records. They are secondary destinations and need reliable, quiet structure.

**Should Define:**

- Structure: header, filters, a PatternActivityFeed or DataTable, links to replays and detail, and retention notices.
- When a table suits better than a feed (numeric comparisons such as trade history).
- Page mode, Compact density.

**Examples in LegendsLegacy:**
Tower personal expeditions, Record of Battle, trade history and loot history.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeHistory composition to the Grimoire Design System for chronological records: World Tower
personal expeditions, Record of Battle, Bazaar trade history, guild activity and loot history. Structure: a
quiet header, filters, the history as PatternActivityFeed (for narrative events) or DataTable (for numeric
records such as trade history, where columns aid comparison), replay and detail links, "Load more", and the
retention notice when older data isn't available. Page mode, Compact density, no ornament. Demonstrate Record
of Battle as a feed and trade history as a table.
```

---

#### DS-123 — Archetype: Detail & Profile Page

**Category:** Page Archetype
**Priority:** Medium
**Depends On:** DS-089, DS-092, DS-110

**Purpose:**
Some entities deserve a full page: another player's profile, a public guild profile, a tournament detail. These are reached from many places (rankings, chat, parties) and must return the player to where they came from.

**Should Define:**

- Structure: a Detail header (BackLink or breadcrumbs, identity, actions for the viewer), key facts, and content sections.
- Viewer-specific actions (Whisper, Invite, Apply to guild).
- A return-to-context guarantee (DS-092).
- Privacy: only the data the server exposes for other players.

**Examples in LegendsLegacy:**
Another player's profile, a public guild profile and a tournament detail page.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeDetail composition to the Grimoire Design System for full-page inspection of one entity:
another player's profile (a read-only variant of the Character & Build archetype), a public guild profile and
a tournament detail page. Structure: a Detail header (BackLink or Breadcrumbs, entity identity, the actions
the viewer can take such as Whisper, Invite to party or Apply to guild), a key facts line, and content
Sections. Guarantee return to context: Back restores the originating list, filters and scroll (Leaderboard,
Chronicle, party roster). Show only data the server exposes for other players, and state plainly where
something isn't visible rather than showing empty values. Page mode, Standard density. Demonstrate another
player's profile opened from the Leaderboard, and a tournament detail page with rounds.
```

---

#### DS-124 — Archetype: Settings & Account

**Category:** Page Archetype
**Priority:** Medium
**Depends On:** DS-036, DS-104, DS-110

**Purpose:**
Settings covers reading font and size, chat layout, sidebar density, account binding and Nobility. The audit found Nobility dominating the page while guests mostly come to bind their account. Settings should handle one utility task at a time, with explicit consequences.

**Should Define:**

- Structure: section navigation (Reading, Layout, Account, Nobility, and others), and SettingsRows.
- Immediate-apply controls (toggles) against saved forms, never mixed in one section.
- Account binding shown prominently for guests.
- Nobility as its own section (DS-104).
- Consequence text for account operations.

**Examples in LegendsLegacy:**
Settings → Reading (font and size), Layout (chat docked or floating, compact rail), Account (binding) and Nobility.

**Paste-ready Claude Design instruction**

```text
Add an ArchetypeSettings composition to the Grimoire Design System for LegendsLegacy's Settings. Structure:
section navigation (Reading, Layout, Account, Nobility, and room for future sections such as Audio) and one
section visible at a time, built from SettingsRows. Reading: reading font (default, readable, system) and text
size, with a live preview line. Layout: Chat layout (Docked or Floating drawer as a card radio with
descriptions), compact NavRail, and density preference if adopted. Account: guest account binding, shown first
and prominently for guest players, with its consequence explained. Nobility: its own section, following the
premium presentation rules, never the largest element on the page. Never mix immediate-apply toggles and
save-required fields in one section; if a section needs saving, it has a single Save action and an
unsaved-changes state. Page mode, Standard density. Demonstrate a guest player's Settings (Account first) and
a Nobility member's Nobility section.
```

---

### Phase 6 — Art, Motion, Sound & Governance

#### DS-125 — Art Direction & Illustration Usage

**Category:** Art
**Priority:** High
**Depends On:** DS-002, DS-016, DS-023

**Purpose:**
The repository has a small set of scenes (a candlelit study, the Colosseum, the Bazaar, a tavern, a temple, one combat-area card) and no pipeline for creature, Essence or character art. The concepts from 13 and 14 September used generated focal art. Whatever art arrives must share one direction and have clear rules about where it belongs, so large amounts of artwork can combine without chaos.

**Should Define:**

- Art roles:
  - **Atmosphere:** Stage and Banner backgrounds.
  - **Place:** region and area identity.
  - **Identity:** portraits, Guardians and bosses.
  - **Object:** item and ability icons.
  - **Brand:** logo and login.
- Direction for all art: candlelit warmth, the umber, brass and verdigris palette, controlled saturation so art sits beneath the interface, one consistent light direction, painterly or line (choose one per role), and readable silhouettes at small sizes.
- Complexity: detail concentrated in the focal zone, with quiet areas where interface overlaps.
- Where art is used: login, Stage atmosphere, region identity, bosses and Guardians, and selected identity moments.
- Where art is never used: behind tables, forms or dense text; in every page header; as one generic image reused for different places.
- An existing asset inventory with its usability.
- Provenance and licensing recorded for every asset, generated art included.
- An asset budget suited to a solo developer: a finite set per role.

**Examples in LegendsLegacy:**
Shenic and Meran region identity, World Tower Guardians, the Colosseum Stage and the login scene.

**Paste-ready Claude Design instruction**

```text
Add an "Art direction" page to the Art section of the Grimoire Design System, so any artwork LegendsLegacy
adds later (creatures, Essences, Guardians, regions, items) combines without visual chaos. Define art roles:
Atmosphere (Stage and Banner backgrounds), Place (region and area identity), Identity (portraits, Guardians,
bosses), Object (item and ability icons), Brand (logo, login). Set one direction for all roles: candlelit
warmth; the Grimoire palette of umber, bone, brass and verdigris; controlled saturation so art sits beneath
the interface rather than competing with it; a single consistent light direction; readable silhouettes at
small sizes. Choose painterly or line treatment per role and record the choice. Complexity rules: concentrate
detail in the focal zone and keep quiet zones where UI overlaps. Define where art is used (login, Stage
atmosphere, region identity, bosses and Guardians, selected identity moments) and where it is never used
(behind tables, forms or dense text; in every page header; one generic image reused for different places,
which is a known problem with the current combat-area card). Inventory the existing assets in the Backgrounds,
Cards, Ornaments and Logos groups and note their usability (several are only 512px wide). Require provenance
and licence records for every asset, including generated art. Recommend a finite asset budget per role
suitable for a solo developer. This page defines direction only; it does not create art.
```

---

#### DS-126 — Asset Specifications, Frames & Cropping

**Category:** Art
**Priority:** Medium
**Depends On:** DS-067, DS-068, DS-125

**Purpose:**
The current image pipeline converts backgrounds to 512px-wide WebP, which is fine for texture and not for focal art. Before any art is commissioned or generated, each asset type needs dimensions, aspect ratios, safe zones, padding and delivery budgets. Frames and rarity must be drawn by the interface, not baked into images.

**Should Define:**

- For each asset type: master size, display sizes, aspect ratio, safe zone or focal area, padding, background (transparent or scene), format, 1x and 2x exports, and a file-size budget. Asset types:
  - Item icons.
  - Ability icons.
  - Portraits.
  - Creature and Essence art.
  - Guardian and boss art.
  - Region and area art.
  - Stage backgrounds.
  - Banner art.
  - Building illustrations.
  - Currency and resource art.
  - Ornaments.
- Crop rules for each frame shape in PortraitFrame and ItemSlot.
- The rule that frames, rarity edges and state markers are drawn by components, never baked into art.
- Consistent lighting and padding across a set, so icons align in grids.
- Naming conventions and fallback requirements.

**Examples in LegendsLegacy:**
A 128px-master item icon with 10% padding, a 2560×1440 region background with a dark left-third safe zone for text, and a 1:1 creature portrait with an eye-line guide.

**Paste-ready Claude Design instruction**

```text
Add an "Asset specifications" page to the Art section of the Grimoire Design System. For each asset type,
define: master size, display sizes, aspect ratio, safe zone or focal area, padding, background (transparent or
scene), format (SVG for icons and ornaments; WebP or AVIF for raster), 1x and 2x exports, and a file-size
budget. Asset types: item icons, ability icons, portraits, creature and Essence art, Guardian and boss art,
region and area art, Stage backgrounds, Banner art, building illustrations, currency and resource art,
ornaments. Include safe zones that respect the shell: Stage art must keep its focal subject clear of where the
Folio and TopBar overlap; Banner art keeps a dark side for text, as the Banner gradient requires. Define crop
rules for each PortraitFrame and ItemSlot shape. State that frames, rarity edges and state markers are drawn
by components and never baked into images. Require consistent lighting direction and padding within a set so
icons align in grids. Define file naming and fallback requirements. Note that the current conversion pipeline
outputs 512px-wide WebP, which is suitable only for texture, and recommend what focal art needs. Show a spec
sheet diagram for each asset type.
```

---

#### DS-127 — Game Icon Set Expansion

**Category:** Art
**Priority:** Medium
**Depends On:** DS-018, DS-053, DS-059, DS-060

**Purpose:**
The icon taxonomy (DS-018) lists what is missing: actions, the eight equipment slots, statuses, social icons, resource line icons, damage types and the 29 conditions. Drawing them in the existing sidebar style (24 grid, 1.6 stroke, round caps, `currentColor`) completes the registries.

**Should Define:**

- Icons drawn in priority batches:
  1. Actions and statuses.
  2. Equipment slots and social.
  3. Resource line icons.
  4. Damage types.
  5. Conditions, with beneficial and harmful framing.
- Each icon named to its registry ID and added to the Icons group.
- A size test at 16, 20 and 24px, and a test against the fallback.
- A consistency review against the existing 15 icons.

**Examples in LegendsLegacy:**
Equip, claim, lock and favourite. The Head, Relic and Off-hand slots. Fate Echo and Tower Tokens. Bleed, Stun and Guard.

**Paste-ready Claude Design instruction**

```text
Draw the missing icons listed in the Iconography inventory of the Grimoire Design System, in exactly the
existing sidebar style: 24-unit grid, 1.6 stroke, round caps and joins, drawn in currentColor, no fills except
small accents as the existing set uses. Work in priority batches. Batch 1: actions (equip, unequip, sell, buy,
claim, lock, unlock, favourite, filter, sort, refresh, close, back, expand, copy, link, whisper, invite) and
statuses (success, warning, danger, info). Batch 2: the eight equipment slots (Head, Chest, Legs, Necklace,
Ring, Relic, weapon, Off-hand) and social (guild, mention, party). Batch 3: line icons for resources (Cinders,
Soulstones, Fate Echo, Sigil Fragments, Glory, Arena tickets, Tower Tokens, Guild Favor, Signets, Renown).
Batch 4: damage types. Batch 5: the combat conditions, with a consistent frame treatment separating beneficial
from harmful. Name each icon with its registry id and add it to the Icons asset group and the Icon component.
Test every icon at 16, 20 and 24px and adjust for legibility. If this is too much for one pass, complete
batches 1 and 2 and list the remaining icons still to draw. Show a contact sheet of new icons beside the
existing fifteen for a consistency check.
```

---

#### DS-128 — Motion Catalogue: Feedback & Reward Moments

**Category:** Foundation
**Priority:** Medium
**Depends On:** DS-017, DS-095

**Purpose:**
DS-017 sets the principles and tokens. This catalogue specifies each recurring animation once, so features stop inventing their own. Motion is a real part of the game experience and should be designed, but within a budget.

**Should Define:**

- A catalogue entry for each motion: button press, selection, tab change, drawer, toast in and out, meter fill after combat, number change highlight, level up, item equip (slot fill), claim, reward reveal (three scales), combat hit, critical hit, heal, condition applied or expired, unit defeated, dungeon room revealed, and a live roster change.
- For each entry: trigger, property, duration and easing tokens, and the reduced-motion alternative.
- A budget: the maximum number of simultaneous animations, and nothing looping except live combat and indeterminate progress.
- Performance: transform and opacity only.

**Examples in LegendsLegacy:**
A level-up after idle combat, a critical hit in the combat viewer, a dungeon room reveal and a Prophecy cache opening.

**Paste-ready Claude Design instruction**

```text
Add a "Motion catalogue" page to Foundations → Motion in the Grimoire Design System, specifying every
recurring animation in LegendsLegacy once. For each entry give the trigger, the animated properties, duration
and easing tokens, and the reduced-motion alternative. Entries: button press; selection change; tab change;
drawer open and close; toast in and out; meter fill after combat; number change highlight; level up on
LevelPlate; item equip into a slot; claim; reward reveal at small, medium and large scale; combat hit,
critical hit and heal feedback; condition applied and expired; unit defeated; dungeon room revealed on the
route; live roster change in a party. Set a budget: a maximum number of simultaneous animations on screen, and
nothing loops except live combat playback and indeterminate progress. Animate only transform and opacity. Keep
Grimoire's quiet character: no bounce, no particles, no confetti, no looping shine. Provide small animated
previews for the level up, the critical hit, the dungeon room reveal and the large reward reveal.
```

---

#### DS-129 — UI Sound Feedback (Optional)

**Category:** Foundation
**Priority:** Low
**Depends On:** DS-017, DS-109

**Purpose:**
The game has no UI audio documented. If audio is planned, for example for a desktop client, sound cues need the same discipline as visual attention: a small set, never the only signal, and easy to control.

**Should Define:**

- Cue categories if audio is adopted: confirm, error, claim, rare drop, whisper or mention, event starting, and your party ready.
- Defaults (off, or low), master volume and per-category toggles in Settings → Audio.
- Rules: never the only signal; muted when the tab is hidden, unless the player chooses otherwise; no sound on routine idle events; rate limiting.
- Mapping to the attention routing in DS-109.

**Examples in LegendsLegacy:**
A whisper received, a tournament round ready and a rare drop.

**Paste-ready Claude Design instruction**

```text
Add an optional "UI sound" page to Foundations in the Grimoire Design System, to apply only if LegendsLegacy
adopts interface audio. Define a small cue set mapped to the attention routing table: confirm, error, claim,
rare drop, whisper or mention, event starting soon, and your party ready. Define defaults (off, or low), a
master volume and per-category toggles in a future Settings → Audio section. Rules: a sound is never the only
signal; sounds are muted when the game tab is hidden unless the player opts in; routine idle-combat events
never play sounds; cues are rate-limited so bursts collapse into one. Describe the intended character of the
sounds (soft, warm, short, in keeping with the candlelit Grimoire tone) without producing audio files. Mark
the page as Draft until audio is confirmed.
```

---

#### DS-130 — Scalability Recipes

**Category:** Governance
**Priority:** High
**Depends On:** DS-053, DS-055, DS-060, DS-088, DS-110

**Purpose:**
LegendsLegacy will keep adding systems for years. Recipes turn the registries and archetypes into step-by-step procedures, so a new currency, status effect or activity ships without a new visual language.

**Should Define:**

- A recipe for each of: a new currency or resource, a new condition, a new attribute, a new collection, a new ranking, a new activity type, a new item type or slot, a new progression system, a new social feature and a new building type.
- Each recipe covers: the registry entries to add, the icon needed, the components that support it automatically, the archetype and patterns to use, the states it needs, content (glossary, formatting), and an "avoid" list, such as a bespoke card, a new colour or a TopBar slot.
- A worked example: adding Stronghold end to end.

**Examples in LegendsLegacy:**
A new seasonal currency, a new condition such as a future Proposed one, a new Codex-like collection and a guild raid ranking.

**Paste-ready Claude Design instruction**

```text
Add a "Scalability recipes" page to the Governance section of the Grimoire Design System. LegendsLegacy will
keep adding systems for years, and each should ship without a new visual language. Write a step-by-step recipe
for each of: a new currency or resource; a new combat condition; a new attribute; a new collection; a new
ranking; a new activity type; a new item type or equipment slot; a new progression system; a new social
feature; a new building type. Each recipe lists: the registry entries to add (Resources, Conditions,
Attributes, Item properties, Damage types); the icon needed; which components support it automatically
(Amount, CostList, ConditionChip, Ledger, ItemRow and so on); which archetype and patterns the screen uses;
which states from the State model apply; content work (glossary entry, formatting rule); and an "avoid" list
(for example: no bespoke card, no new colour, no TopBar slot, no feature-specific font). Finish with a worked
end-to-end example: adding the planned Stronghold system, from registry entries to the Management & Building
archetype.
```

---

#### DS-131 — Stress-Test Specimens

**Category:** Governance
**Priority:** Medium
**Depends On:** DS-011, DS-012, DS-020, DS-041, DS-068

**Purpose:**
Generated concepts and polished previews hide the hard cases. The audit's verification guidance: evaluate at real content width, with large text, long names, expanded details, keyboard focus and chat open. A standing set of specimens makes every component prove itself.

**Should Define:**

- Specimens:
  - Long names: items, guilds, players and titles of 32 characters or more.
  - Maximum numbers: 999,999,999 Cinders and 100.0% values.
  - Every rarity.
  - Four simultaneous states.
  - Every data state (empty, loading, error, stale).
  - The readable font at the largest scale.
  - 1280px with the Folio and the docked Chronicle.
  - The 960px boundary.
  - Reduced motion and a visible keyboard focus path.
  - A 200-row table.
  - A unit frame with overflowing conditions.
- A checklist for running a component through the specimens.

**Examples in LegendsLegacy:**
"Heartwood Warden's Unbroken Oath" in an ItemRow at Compact density. A guild member table at 1280px with docked chat.

**Paste-ready Claude Design instruction**

```text
Add a set of stress-test specimen previews to the Governance section of the Grimoire Design System, and a
checklist for running any component through them. Specimens: long names (item "Heartwood Warden's Unbroken
Oath", a 24-character guild name, a player with a long title); maximum numbers (999,999,999 Cinders, 9,999
stack quantities, 100.0% and <1% values, negative deltas); every rarity side by side; four simultaneous states
on one object (the Epic, equipped, upgrade-available, favourite case); all data states (empty, loading,
refreshing, stale, error); the readable font at the largest scale step; 1280px with the Folio and docked
Chronicle open; exactly at the 960px boundary; reduced motion; a visible keyboard focus path through a whole
screen; a 200-row DataTable; a UnitFrame with more conditions than fit. Build specimens for ItemRow, Ledger,
DataTable, ItemCard, LoadoutSlot, UnitFrame and ScreenOverview, flag every failure you find, and fix those
that belong to the components.
```

---

#### DS-132 — Contribution Process & Acceptance Checklist

**Category:** Governance
**Priority:** High
**Depends On:** DS-001, DS-003, DS-131

**Purpose:**
Each prompt in this backlog will produce work that needs checking. A definition of done, a status lifecycle and a changelog stop the system drifting the way the `ll-*` layer did.

**Should Define:**

- An acceptance checklist for any component or pattern:
  - Purpose and a when-not-to-use section.
  - Tokens only.
  - Supported states declared, and combination rules followed.
  - Density variants.
  - Content rules.
  - Accessibility (keyboard, contrast, names, reduced motion).
  - The art-optional contract.
  - A preview with realistic data, and stress specimens passed.
  - The guardrail checklist passed.
  - The decision log updated.
  - Code parity status.
- Status lifecycle: Draft, Revising, Stable, Deprecated. How deprecation works (replacement and removal).
- A changelog format.
- A request template for future additions, which this document's prompts already approximate.

**Examples in LegendsLegacy:**
Reviewing a new ActivityCard variant for a seasonal event before marking it Stable.

**Paste-ready Claude Design instruction**

```text
Add a "Contribution and acceptance" page to the Governance section of the Grimoire Design System. Write an
acceptance checklist that every new or revised component, pattern and archetype must pass before it is marked
Stable. The checklist covers: purpose plus when-not-to-use; tokens only, with no raw values; supported states
declared per the State model, with combination rules followed; density variants; content rules followed;
accessibility (keyboard path, contrast, accessible names, reduced motion, text scaling); the Art-optional
contract; a preview with realistic LegendsLegacy data; the stress-test specimens passed; the anti-generic
guardrail checklist passed; the Decision Log updated; code parity status recorded. Define the status lifecycle
(Draft, Revising, Stable, Deprecated), how deprecation works (name the replacement, set a removal condition),
and a changelog format with dated entries. Add a request template for future additions: context, player
problem, which archetype or pattern, registry entries, states, and acceptance criteria. Apply the checklist
retroactively to the existing components and set each component's status.
```

---

#### DS-133 — Code Parity & Handoff Map

**Category:** Governance
**Priority:** Medium
**Depends On:** DS-001, DS-132

**Purpose:**
The Grimoire components are already ported as standalone, signal-based Angular `lg-*` components on `feature/grimoire-design-system`, with tokens generated into `tokens.css`. Nothing uses them yet. As the design system grows, a parity map keeps design and code in step and shows which `ll-*` classes each component replaces.

**Should Define:**

- A parity table: design system component, `lg-*` Angular selector, status (in sync, drifted, not ported), last synced date, and the `ll-*` classes or legacy components it replaces.
- A token export rule: `tokens.json` is the source, `tokens.css` is generated, and there are no manual edits.
- Naming parity for inputs and slots between the design system docs and the Angular inputs.
- Migration notes: which screens use which components, and the order to migrate them.

**Examples in LegendsLegacy:**
Ledger to `lg-ledger`, replacing `ll-stat-card` and ad-hoc attribute rows. Button to `button[lgButton]`, replacing `RegularButton` and `ll-button`.

**Paste-ready Claude Design instruction**

```text
Add a "Code parity" page to the Governance section of the Grimoire Design System. The components are already
ported to Angular as standalone, signal-based lg-* components (for example lg-game-shell, lg-ledger, lg-folio,
button[lgButton]), with tokens generated into a tokens.css file. No game screen uses them yet. Create a parity
table with columns: Design System component; Angular selector; status (in sync, drifted, not ported); last
synced date; and the legacy ll-* classes or old Angular components it replaces (for example Ledger replaces
ll-stat-card and ad-hoc attribute rows; Button replaces RegularButton, MiniButton and ll-button; Tag replaces
ll-badge; Panel replaces ll-panel and ll-card where appropriate). Mark every component added after the initial
34 as "not ported". State the token export rule: tokens.json is the single source, tokens.css is generated
from it, and there are no manual edits. Require that component inputs and slot names in the documentation
match the Angular inputs. Add a migration-order note listing screens in a suggested order, starting with
Character Overview, then Inventory, then the Essences screens, and following the archetype map.
```

---

## 5. Recommended First 15 Tasks

These fifteen are deliberately foundational. Grimoire already has 34 components, so the most valuable early work is fixing the rules they are built on. Each prompt also updates the affected existing components, so you see changes from the first prompt. Every item's dependencies appear earlier in this list.

After these fifteen, continue with DS-012, DS-013, DS-014, DS-015, DS-016, DS-017, DS-018, DS-022 and DS-024, then the content standards (DS-025 to DS-030), then Phase 2 in backlog order.

### 1. DS-001 — Documentation Architecture & Decision Log

**DS number:** DS-001

**Name:** Documentation Architecture & Decision Log

**Why now:** The documentation needs a structure before anything else goes in. Without sections and a decision log, the next 130 items would pile into one README, and Claude Design could undo decisions you have already made (text-first equipment, the two chat layouts, Grimoire as the only theme).

**Paste-ready Claude Design instruction**

```text
Restructure the documentation of the existing Legend's Legacy (Grimoire) Design System so it can grow roughly
three times larger without becoming one long README. Populate the documentation sections with this tree: Start
here; Principles; Foundations (Colour, Typography, Numerals, Space & Density, Accessibility, Layout, Surfaces
& Layering, Lines, Shape, Ornament, Motion, Iconography); Standards (States, Information Hierarchy,
Progressive Disclosure, Art-optional, Content); Registries; Components; Game Components; Patterns; Shell; Page
Archetypes; Governance. Move the existing README content into the matching sections without changing its
meaning. Reduce project/README.md to a short overview: what LegendsLegacy is (a desktop-first, data-heavy
fantasy PBBG), the layer model, and how to read and extend the system. Create two templates in Governance. The
first is for sections: purpose, rules as must/should/never, tokens used, do/don't pairs, related components.
The second is for component READMEs: plain-language subtitle, when to use, when not to use, anatomy, supported
states, density variants, content rules, accessibility, related components, and a status of Stable, Revising,
Draft or Deprecated. Keep the existing evocative component names (Folio, Ledger, Chronicle, Sigil) and add a
plain subtitle to each, such as "Folio — the detail panel". New components get plain descriptive names;
compositions are named Pattern<Name> and Archetype<Name>. Add a Decision Log page (ID, date, decision, reason,
consequence, status) and seed it with: Grimoire is the only theme and the light Codex theme is dropped; the
Settings chat layouts, docked and floating, are preserved; equipment and Essences are presented text-first
until an art strategy is decided; one Folio per screen; loot is a Chronicle channel; the rarity code always
accompanies the rarity colour; the Nobility ◆ mark follows the display rules; information density is preferred
over decorative layouts. Do not change any component visuals in this step.
```

---

### 2. DS-002 — Design Principles

**DS number:** DS-002

**Name:** Design Principles

**Why now:** Principles are what Claude Design falls back on whenever a later prompt is ambiguous. Settling density against atmosphere now prevents the art-led composition from spreading to data-heavy screens.

**Paste-ready Claude Design instruction**

```text
Add a Principles section to the existing Grimoire Design System. LegendsLegacy is a desktop-first browser RPG
where players spend hours reading numbers, comparing builds and managing many interlocking systems, so the
principles must resolve the constant tension between information density and atmosphere. Write seven or eight
principles. For each, give a one-line statement, what it means in practice, the trade-off it resolves, and one
concrete LegendsLegacy example. Use this set as the starting point and refine the wording to Grimoire's voice.
Decisions first: lead with what the player must decide next. Truth over atmosphere: every value the game knows
is visible, labelled and explainable. Dense, not crowded: density comes from alignment, typography and rhythm,
not from more boxes. One meaning per signal: a colour, shape or position means one thing in a given context.
Mechanics give identity: a dungeon route, a Tower ascent or an order book shapes the screen more than
decoration does. Art enhances, never carries: every screen works without illustration. Calm until it matters:
attention is a budget spent on risk, rewards and required action. Built to grow: new systems are registry
entries, not new visual languages. Then add a priority order for when principles conflict (truth over
atmosphere, decisions over completeness, and so on), and a short review checklist to apply to any new
component or screen. Link the existing "one screen, one subject" rule to the relevant principle and note where
it applies (detail and collection screens) and where it does not (dense workbench screens such as the Bazaar
or guild members). Keep it concise; this is a working tool, not a manifesto.
```

---

### 3. DS-003 — Anti-Generic Guardrails

**DS number:** DS-003

**Name:** Anti-Generic Guardrails

**Why now:** The guardrails must exist before any new component is built, so every later prompt is judged against them. They also set limits on existing ingredients that are easy to overuse (Banner, Folio, glows, pill buttons, large display type).

**Paste-ready Claude Design instruction**

```text
Add an "Anti-generic guardrails" page to the Principles section of the existing Grimoire Design System. The
goal is to stop LegendsLegacy from drifting toward a generic SaaS dashboard or a stereotypical AI-generated
game UI as the system grows. A previous audit of the game found these specific problems: cards used for every
data group, containers nested three or four deep, a repeated heading → subtitle → grid of tiles rhythm, rows
of equal-sized stat tiles, identical progress cards for mechanically different systems (Achievements,
Soulstones, Essence Codex), and one accent colour used for too many meanings. Write a risk → rule table
covering: excessive cards; container nesting (set a maximum of two enclosure levels); stat tile grids; heading
stacks and repeated eyebrows; rounded rectangles; gradients (allow them only for art fades and vignettes);
glassmorphism; glow (allow it only for selected, ready, focus and primary-button hover); excessive borders;
fantasy ornamentation (defer to the ornament budget); hero artwork without a subject; oversized typography
(one display-size element per screen); excessive whitespace; mobile-app layouts on desktop. Add a
banned-tropes list (an icon in a circle beside every heading, feature-card triplets, gradient text, sparkle or
wand icons, badge soup, emoji, cheerful copy such as "Welcome back, hero!", the same padding at every nesting
level, parchment or wood textures). Add a "sameness test": two mechanically different systems must not share
an identical structure without a stated reason. Add a fantasy-balance rule: the fantasy identity comes from
typography, the lore register and a small set of ornaments, never from skeuomorphic parchment, wood or metal.
End with a 10–12 question yes/no review checklist. Where an existing Grimoire component (Banner, Folio, Stage,
StatTile, Button's pill shape, glow-gilt, glow-selected) is at risk of overuse, name it and state its limit.
```

---

### 4. DS-004 — Existing Component Audit & Consolidation Map

**DS number:** DS-004

**Name:** Existing Component Audit & Consolidation Map

**Why now:** You already have 34 components. Auditing them now exposes drift (19 hard-coded font sizes, gilt used for too many things, five ways to show a number) and produces a revision queue that the foundation prompts then work through.

**Paste-ready Claude Design instruction**

```text
Audit every existing component in the Grimoire Design System and add the result as an "Audit & consolidation
map" page in the Governance section. For each of the 34 components (GameShell, NavRail, TopBar, Page,
PageHeader, Stage, Banner, Folio, Panel, Ledger, StatTile, StatFigure, LevelPlate, Meter, Track, Sigil,
Constellation, Emblem, ItemSlot, ItemLink, LoadoutSlot, EntryList, TabStrip, Tag, Button, SearchField,
Presence, JourneyCard, KeyHints, Chronicle, CurrencyPill, Heading, Icon, SectionRule) and the ScreenOverview
and ScreenArchive compositions, record: its purpose in one line, a status of keep, revise, merge or retire,
the concrete issues, and which states it supports today (default, hover, focus, selected, disabled, locked,
loading, error). Specifically assess: the five ways a number is currently shown (StatTile, StatFigure, Ledger
row, Sigil, LevelPlate) and when each is justified; the four containers (Panel, Folio, Banner, Stage) and
their boundaries; whether EntryList should become a variant of a general list row; ItemSlot's image-first
design, given that equipment and Essences are currently text-first; CurrencyPill only supporting Cinders and
Soulstones when the game has more than 15 resources; whether KeyHints deserves a permanent bar in a
mouse-first browser game or should appear only where real shortcuts exist; and which components suit dense
workbench screens (Bazaar, guild members, inventory) versus art-led screens (Creature Archive). List every
hard-coded value in the component styles that is not a token: the component CSS uses about 19 distinct font
sizes against 17 type styles, and some radii are raw 1px and 2px. List every place gilt is used and what it
means there. Do not change any component in this step; produce the audit and a prioritised revision queue.
```

---

### 5. DS-005 — Colour Architecture

**DS number:** DS-005

**Name:** Colour Architecture

**Why now:** A tiered colour architecture makes the colour changes that follow safe, and nothing visible changes yet. It also produces the contrast table the accessibility work needs.

**Paste-ready Claude Design instruction**

```text
Restructure the colour tokens of the existing Grimoire Design System into a three-tier architecture and
document it in Foundations → Colour. Tier 1 is palette primitives: named ramps (for example umber, bone,
brass, verdigris, ember, amber, azure) plus the seven rarity hues and seven damage hues. Components must never
reference primitives directly. Tier 2 is semantic roles: ground, ground-deep, surface, surface-raised, folio,
scrim, ink, ink-muted, ink-disabled, line, line-strong, gilt, arcana, status, focus. Tier 3 is domain roles:
rarity-*, damage-*, channel-*, meter-*, plus placeholders for condition-beneficial, condition-harmful and
delta roles, to be defined later. Every Tier 2 and Tier 3 token aliases a primitive. Keep all current visible
values identical in this step; this is a restructuring, not a recolour. Mark component-scoped tokens
(sigil-fill, sigil-edge, on-sigil, tile, on-tile, on-tile-muted) as component tokens. Document the naming
convention, how to add a token, how aliases work and how to deprecate one. Build a contrast table showing
every text-capable token (ink, ink-muted, ink-disabled, gilt, arcana, the status colours, every rarity and
damage colour) on ground, surface, surface-raised and folio, with the measured ratio and pass or fail against
4.5:1 for text and 3:1 for UI edges. Flag every failure rather than hiding it. Keep the existing --ll-* →
Grimoire migration map in this section. Update tokens.json and the Colour documentation; no component should
change appearance.
```

---

### 6. DS-006 — Colour Channel Allocation & Collision Rules

**DS number:** DS-006

**Name:** Colour Channel Allocation & Collision Rules

**Why now:** One colour carrying many meanings is the most systemic visual problem, and Grimoire has it again with gilt. The colliding hues (for example Epic against whisper, and success against arcana) affect every item, chat and status component that follows.

**Paste-ready Claude Design instruction**

```text
Add colour allocation and collision rules to Foundations → Colour in the Grimoire Design System, and update
tokens and components to follow them. A previous audit of LegendsLegacy found that one accent colour carried
too many meanings. Grimoire currently uses gilt for group labels, eyebrows, effect values, Ledger values, the
level numeral, the active nav marker, selected rules and the solid button, which is the same problem again.
First, write an allocation table assigning each hue family one job per context. Reduce gilt to at most four
roles (suggested: brand and current location, the single committing action, the screen's headline figure, and
effect magnitudes inside descriptions). Move ordinary data values, including Ledger values, to ink. Make
arcana mean "ready, new, actionable or selected" and nothing else. Second, build a collision matrix for three
clusters. Cool: arcana, success, info, rarity-uncommon, rarity-rare, damage-shadow, meter-sp. Warm: gilt,
warning, rarity-unique, rarity-legendary, damage-burn, channel-loot, channel-trade. Red and pink: danger,
rarity-legacy, damage-bleed, meter-hp, channel-whisper, rarity-epic. Show each pair's perceptual distance and
whether it may appear adjacent. Note specifically that success and arcana are nearly identical, and that
channel-whisper and rarity-epic are nearly identical. Third, define context ownership: in item contexts rarity
owns hue; in combat, damage type owns hue; in chat, channels colour only speaker names and tags. Everything
else in those contexts uses glyphs, shapes or words. Rarity and damage hues are learned by players and must
not change; resolve collisions by adjusting the other tokens (for example channel-whisper, success, info) or
by requiring a glyph, and state which you chose. Include colour-vision-deficiency checks for the key pairs.
Update Ledger, Folio, Tag, NavRail and Chronicle to the new allocation and note each change in the Decision
Log.
```

---

### 7. DS-007 — Feedback, Delta & Effect Polarity Colours

**DS number:** DS-007

**Name:** Feedback, Delta & Effect Polarity Colours

**Why now:** Deltas and beneficial or harmful polarity are needed by comparisons, costs, conditions and upgrades. The item is small and completes the colour foundation.

**Paste-ready Claude Design instruction**

```text
Add feedback and polarity colour roles to Foundations → Colour in the Grimoire Design System. LegendsLegacy
constantly shows stat changes (equipment comparison, Soulstone upgrades, Essence Ascension previews) and
beneficial or harmful combat effects (Empower versus Weaken, Haste versus Slow). Define Tier 3 tokens
delta-better, delta-worse, delta-neutral, effect-beneficial and effect-harmful, aliased to palette primitives
and consistent with the collision rules already in the Colour section. Establish that polarity follows player
benefit, not the sign of the number: a cooldown going from 8s to 6.8s is "better" even though the number fell.
Every delta must carry a glyph and a sign (▲ +12%, ▼ −1.2s, ◆ for unchanged) so meaning never depends on
colour. Define the semantic difference between warning (attention, reversible risk, insufficient resources,
something expiring soon) and danger (loss, destruction, failure, such as losing Pending Loot, abandoning a
dungeon run or a failed save). Define success as a confirmed outcome only, and note that "ready" or
"claimable" belongs to arcana. Limit soft status backgrounds (danger-soft and the like) to inline alerts and
never apply them to rows, list items or cards. Update StatTile's delta display and the Tag tones to use the
new roles, and show a preview with realistic LegendsLegacy examples: equipment comparison deltas, a cooldown
reduction, a harmful condition, and an insufficient-resource cost.
```

---

### 8. DS-008 — Typography Ramp for Data-Dense UI

**DS number:** DS-008

**Name:** Typography Ramp for Data-Dense UI

**Why now:** The type ramp gaps (no section-title size, no compact body, no small tabular numeral) block tables, lists and headers. Mapping the 19 hard-coded sizes stops the drift.

**Paste-ready Claude Design instruction**

```text
Extend the typography system of the existing Grimoire Design System so it supports a data-dense RPG interface,
and document it in Foundations → Typography. Keep the four families and their jobs (Marcellus display, EB
Garamond lore, Barlow UI, Barlow Condensed numerals, Atkinson Hyperlegible for the readable setting). The
current 17 styles have gaps. Nothing sits between the 17px tab style and the 36px title-lg for section titles.
There is no compact body style for tables and dense lists. There is no small tabular numeral for table cells.
The component CSS already uses about 19 hard-coded font sizes. Define a complete ramp organised by role.
Display: hero numeral, title-xl, title-lg. Section titles: add two sizes, roughly 24px and 20px. Entity names:
one size for names in headers and one for names in rows, for items, creatures, Essences and players. Body:
body plus a new body-compact at 13–14px for tables. Labels and captions. Numerals: headline, stat, row and
compact. Set a minimum of 12px for anything a player reads; only rarity codes and key caps may use 11px, in
bold caps. Restrict Marcellus to 15px and larger and never use it for numbers in tables. Evaluate whether
Button labels and EntryList items should stay in Marcellus or move to Barlow for legibility at small sizes,
and record the choice in the Decision Log. Allow tracked uppercase only for labels of three words or fewer.
Limit title-xl and level-numeral to one per screen and never inside a list. Give line heights for dense rows
and for prose separately. Then map every hard-coded font size in the component styles onto a ramp style and
update the components so no raw sizes remain. Show a specimen with real LegendsLegacy content: section titles,
item names, an attribute table, a Bazaar order-book row and a lore line.
```

---

### 9. DS-009 — Numeric Typography & Alignment

**DS number:** DS-009

**Name:** Numeric Typography & Alignment

**Why now:** Numbers are the game's main content. Figure style, alignment, units and placeholders must be fixed before any data component exists.

**Paste-ready Claude Design instruction**

```text
Add a Numerals page to Foundations in the Grimoire Design System, and update every component that displays
numbers to follow it. LegendsLegacy is mostly numbers: attributes, prices, quantities, ratings, damage,
cooldowns and progress. Define the rules. Use tabular lining figures (font-variant-numeric: tabular-nums
lining-nums) wherever numbers align in columns, stack in lists or update live; use proportional figures only
for a single isolated headline number. Right-align numeric columns, decimal-align mixed precision, and place
units immediately after the number in a smaller or muted style. Use a true minus sign (U+2212), an en dash for
ranges (12–18), × for multipliers (×1.5), and a spaced slash for fractions (3,120 / 4,150). Use an em dash (—)
for unknown or not-applicable values, "0" for zero, and never leave a value cell empty. Restrict Marcellus
numerals to the hero numeral and LevelPlate; everything else uses Barlow Condensed or Barlow with tabular
figures. Explain how numbers that update live (combat, auctions, currency) avoid layout shift, for example by
reserving width. Specify the number-and-label order for each context: TopBar, cost lists, tables, tooltips and
Ledgers. Update Ledger, StatTile, StatFigure, Meter, CurrencyPill and LevelPlate accordingly. Show a preview
with a column of attribute values of mixed precision (1,284; 24.8%; 84 HP/5s; 184.6 threat/s), a Bazaar price
column, a delta column and a live-updating counter.
```

---

### 10. DS-010 — Spacing Scale & Density Modes

**DS number:** DS-010

**Name:** Spacing Scale & Density Modes

**Why now:** Density modes decide row heights and padding for every list and table. They are also the main counterweight to Grimoire's spacious, art-led composition.

**Paste-ready Claude Design instruction**

```text
Add a Space & Density page to Foundations in the Grimoire Design System. Keep the existing 4px spacing scale,
but add semantic spacing tokens (inset, stack, inline and section gaps, each aliased to the scale) and three
density modes. Comfortable is for identity and detail views, the Folio and dialogs. Standard is the default
for panels and forms. Compact is for tables, inventory lists, rankings, guild members, combat logs and Bazaar
order books. For each mode define row height, cell padding, gaps between elements, which text style it uses
(body or body-compact), icon size and control height. Add the rule that nested containers never both apply
full padding: an inner surface steps down one level, and a list inside a Panel uses the Panel's padding
instead of adding its own. Specify which components support which densities (Ledger, EntryList, TabStrip and
Button at minimum) and which density each future page archetype defaults to. Recommend whether players should
get a "Compact lists" preference in Settings and how it would apply. LegendsLegacy players spend hours on
inventory, rankings and trading screens, so Compact must feel deliberate and legible rather than cramped: keep
12px as the minimum text size and keep focus rings intact. Show a preview comparing the same inventory list
and the same attribute Ledger in all three densities, and update affected existing components.
```

---

### 11. DS-011 — Accessibility Baseline & Text Scaling

**DS number:** DS-011

**Name:** Accessibility Baseline & Text Scaling

**Why now:** The decision to move from px to rem, which lets the reading-size preference scale the whole interface, has to be made before more px-based components exist. The keyboard and live-region rules shape every component after this.

**Paste-ready Claude Design instruction**

```text
Add an Accessibility page to Foundations in the Grimoire Design System and apply its rules to the tokens.
LegendsLegacy already has a readable-font setting (Atkinson Hyperlegible), text-size preferences,
reduced-motion handling, visible focus rings and focus-trapped dialogs; none of these may regress. Set WCAG
2.2 AA as the target and state what that means for text contrast, UI edge contrast, target size, focus
visibility and keyboard operation. Text scaling is the most important decision: Grimoire's type, spacing and
layout tokens are in px, so they ignore the game's reading-size preference. Convert them to rem, or define an
equivalent scale mechanism, so the whole interface scales, and define tested scale steps (100%, 115%, 130%)
with what reflows and what scrolls at each. Define the keyboard model: tab order, roving tabindex for lists,
tabs and grids, arrow-key behaviour, and Escape order for nested overlays. Define minimum target sizes:
24×24px, with 32px recommended for primary actions. State that no information may exist only on hover: hover
cards must open on keyboard focus and pin on tap. Define live-region rules for a realtime game: announce
errors, claim results and completed activities; never announce every combat event; throttle announcements.
Require accessible names for icon-only controls, and require that abbreviated numbers are announced in full
(12.5k is read as "12,480 Cinders"). Restate that colour never carries meaning alone and that rarity is
announced by name. Update tokens.json and add an accessibility notes field to the component README template.
```

---

### 12. DS-019 — State Model

**DS number:** DS-019

**Name:** State Model

**Why now:** Every component must declare which states it supports and how it shows them. Without the model, each new component invents its own locked, claimable or equipped look. This prompt also brings the existing components into line.

**Paste-ready Claude Design instruction**

```text
Add a States page to Standards in the Grimoire Design System. This is the most important cross-cutting
standard, so be thorough. Grimoire currently defines hover, selected, ready, locked and focus; LegendsLegacy
needs a full state model. Organise states into families. Interaction: default, hover, focus-visible, pressed,
selected, current, dragging, disabled. Availability: available, unavailable (always with a reason), locked (a
progression gate, always with its unlock condition), restricted (account or guild permission), insufficient
resources (always showing what is missing), on cooldown. Lifecycle: new, unread, in progress, completed,
claimable, claimed, opened, expiring soon, expired, failed. Ownership and use: owned, unowned, equipped,
attuned, assigned to a preset or activity, captured in an activity snapshot, listed on the market, reserved or
in escrow, borrowed from the guild Vault, favourite or protected. Knowledge: discovered, undiscovered, hidden
(for example unrevealed dungeon rooms), unknown. Data: loading, refreshing, pending save, stale, error, empty,
offline or reconnecting. For each state define: meaning in one sentence; the visual channel it uses (edge,
fill, marker, text, icon, opacity or typeface); the tokens; the exact words shown; and the screen-reader
announcement. Make the easily confused distinctions explicit: disabled versus unavailable versus locked versus
insufficient; completed versus claimable versus claimed; owned versus equipped versus assigned versus
captured. Prefer "unavailable with a reason" over plain disabled, which should be rare. Add a matrix showing
which component categories support which states. Then update Button, Tag, ItemSlot, LoadoutSlot, EntryList,
NavRail and Sigil so their existing states match this model.
```

---

### 13. DS-020 — State Combination & Priority Rules

**DS number:** DS-020

**Name:** State Combination & Priority Rules

**Why now:** The combination rules fix marker positions and precedence before ItemRow, LoadoutSlot and NavRail badges are extended. This is what prevents badge soup.

**Paste-ready Claude Design instruction**

```text
Add a "State combinations" page to Standards in the Grimoire Design System, building on the States page. Game
objects in LegendsLegacy often carry several states at once. The rules must stop the interface becoming badge
soup. Assign each visual channel one state at a time. Identity: rarity edge, name colour and rarity code,
saying what the object is. Selection: the arcana ring or bar. Ownership: one fixed corner marker on slots, or
a single word such as "Equipped" in rows. Attention: one diamond marker in a fixed top-right position.
Availability: dimming plus a lock icon plus reason text. Data: a skeleton or stale-data text. Set the maximum
visible at once: identity, selection, one ownership marker and one attention marker; everything else moves
into the hover card or detail view. Define the precedence order when space is limited: availability, then
selection, then attention, then ownership, then "new". Specify exact marker positions for ItemSlot,
LoadoutSlot, EntryList rows and NavRail items, and update those components. Include worked examples, each
showing the resolved visual and what moved into the hover card: an Epic item that is equipped and has an
upgrade available; a locked reward that is new; a claimable Prophecy cache that expires soon; a selected item
the player cannot afford; an undiscovered creature that is the current Creature Focus; a market-listed item
that is also a favourite; a captured Arena defence build that differs from the live build. Add a don't list:
no more than one Tag per row by default, never a glow plus a ring plus a badge on the same object, never a
colour change for more than one state at once.
```

---

### 14. DS-021 — Information Hierarchy Levels

**DS number:** DS-021

**Name:** Information Hierarchy Levels

**Why now:** Hierarchy levels tie type, colour and position together, so a screen does not become a wall of equally weighted numbers. Folio, Ledger and every header depend on them.

**Paste-ready Claude Design instruction**

```text
Add an Information Hierarchy page to Standards in the Grimoire Design System. LegendsLegacy screens combine
decisions, headline figures, entity names, stat values, labels, metadata, mechanical descriptions, lore,
warnings and requirements. Without explicit levels they turn into walls of equally weighted text and numbers.
Define these levels. L1 Decision: the action or choice, one per region. L2 Headline value: at most one per
screen or panel. L3 Entity name. L4 Primary value. L5 Label. L6 Metadata and secondary values. L7 Mechanical
description. L8 Lore. Add two interrupting levels: Warning and Requirement. For each level, specify the type
style from the Typography ramp, the colour role from the allocation table, its typical position, and how many
are allowed per region. Add the rules: labels are quieter than values; units are quieter than numbers;
mechanical text comes before lore and never shares a line with it; metadata trails the row or moves to a
tooltip; a requirement sits directly next to the action it gates; a warning sits next to the commitment it
qualifies. Add chunking rules: groups of about seven rows or fewer, aligned value columns, summary before
detail. Add a simple "wall test" for reviewing screens. Illustrate each rule with LegendsLegacy examples: a
creature Folio, a Soulstone upgrade showing the next effect, cost and requirement, a raid party slot, and a
Bazaar listing with price, fee and net proceeds. Update Folio, Ledger and PageHeader documentation to
reference the levels.
```

---

### 15. DS-023 — Art-Optional Contract

**DS number:** DS-023

**Name:** Art-Optional Contract

**Why now:** It removes the art dependency. Every game component in Phase 3 can then proceed text-first, whatever you later decide about artwork.

**Paste-ready Claude Design instruction**

```text
Add an "Art-optional contract" page to Standards in the Grimoire Design System. LegendsLegacy's current
direction presents equipment and Essences text-first, with no portraits, and the project has no pipeline for
creature, Essence or character art. Yet ItemSlot, LoadoutSlot, Stage and Banner are image-first. Define a
contract every art-capable component must meet. First, a text-first baseline: name, rarity, type and state
always exist as text, so the component is complete with no image. Second, a fallback chain: artwork, then a
registry emblem or icon, then a monogram or initial glyph, then text only. Document which steps each component
supports. Third, layout stability: the art and no-art variants keep the same dimensions, or a compact
text-first variant is explicitly defined. Fourth, art never carries information: no rarity, state or identity
may exist only in the image. Fifth, loading and failure: reserve space and show the fallback if an image
fails. Apply the contract now. Add a text-first variant to ItemSlot (for example a row form: "Ashen Longsword
· E · Tier 2"), make LoadoutSlot's text-only form the documented default, and define what Stage and Banner
render when no art is supplied. Record in the Decision Log that equipment and Essences are text-first until an
art strategy is approved, and list what would change when art arrives. Show previews of each updated component
in art and no-art forms side by side.
```

---

## 6. Missing Information

Only information that would change what gets built is listed here. For each "Needed Soon" item, the plan states the assumption it uses until you decide, so work can start now.

### Needed Soon

1. **Art strategy.** Is the 14 September decision (text-first equipment and Essences, no portraits) still the direction? Or do you plan creature, Essence, portrait or item art? If so, from which source (commissioned, generated, asset packs), at what scale, and with what licence and provenance? This decides the defaults in DS-023, how far ItemSlot, Stage and Banner are used, whether PortraitFrame (DS-067) matters early, and when DS-125 and DS-126 become urgent.
   *Assumption until decided:* text-first everywhere, with art as an optional enhancement.

2. **How much of the game uses the "console" composition.** Grimoire's Stage + Folio + KeyHints composition, with its 56px titles, 84px numerals and Constellation, is distinctive. Is it meant as the signature for a subset of screens (collections, progression, encounters), or was it exploratory? This decides how screens split between the Page, Stage and Workbench modes (DS-024, DS-110 onward), and how strictly DS-008 limits display sizes.
   *Assumption:* Stage for collection, progression and encounter screens; Page and Workbench for everything else.

3. **Input and platform roadmap.** Is the Steam desktop client (assessed on 12 September as "experiment first"), or controller support, a realistic near-term target? Controller support would make spatial focus navigation, focus-visible styling everywhere and KeyHints core systems, and would raise target sizes. Mouse and keyboard only keeps them light (DS-011, DS-049, DS-015).
   *Assumption:* mouse and keyboard, with a possible desktop wrapper and no controller.

4. **Colour latitude.** May non-rarity tokens (`success`, `info`, `arcana`, `channel-whisper`, `channel-trade`) change to resolve the collisions found? Are the seven rarity and seven damage hues truly frozen? DS-006 depends on this.
   *Assumption:* rarity and damage hues are frozen, and everything else may move.

5. **What "Doctrines" are.** Your brief lists "Doctrines / build customization", but the repository has no Doctrine concept. Is it a new name for Combat Styles, a new build layer beside equipment, Essences and Combat Styles, or a planned system? This affects the glossary (DS-026), build identity (DS-079) and the loadout editor (DS-096).
   *Assumption:* a placeholder glossary entry, and build components designed so a fourth build axis can be added.

6. **Primary target viewport.** Which desktop size should feel "comfortable" by default (1920×1080 at 100%, or 1600×900), and do most players keep the Chronicle docked? This sets the density defaults (DS-010) and the stage-width table (DS-012).
   *Assumption:* 1600×900 with the rail and docked chat must fit the Character Overview; 1920×1080 is the comfortable target.

### Can Be Decided Later

- **Which planned systems ship, and when:** Stronghold, raids in production, and the future equipment upgrades. This changes archetype priorities (DS-120) and the Upgrade Preview variants (DS-085), not the foundations.
- **Localisation plans:** text expansion, and number and date locales. The formatting standards (DS-027, DS-028) assume en-US and should add locale rules later. The stress tests (DS-131) would add long-language specimens.
- **Mobile support level:** secondary support, which is the current state, or parity. This affects DS-012, DS-024 and DS-113 onward, but not the desktop-first foundations.
- **Audio:** whether UI sound is wanted at all (DS-129 stays Draft until then).
- **A notification centre:** DS-109 will recommend. Your product preference can override it later.
- **Future monetisation surfaces:** the direct-support badge, and any cosmetics. This affects DS-104 only.
- **Seasonal or event theming:** token architecture (DS-005) keeps this possible, but it should not be designed now.
- **Who produces final icons and art:** Claude Design SVGs or an artist. This affects DS-127 batches and DS-126 export specifications.
- **A player-facing density preference:** whether "Compact lists" becomes a Settings option (DS-010 recommends).
- **Accessibility target confirmation:** WCAG 2.2 AA is assumed from the existing WCAG colour audit work (DS-011).
