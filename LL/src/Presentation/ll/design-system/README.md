Legend's Legacy is a desktop-first, data-heavy fantasy PBBG: a persistent browser RPG played in long sessions of reading, comparing and deciding — stats, loadouts, markets, guilds, dungeons. It is read by candlelight, and its interface is a grimoire: umber ground, bone ink, brass rules, verdigris hexagons and painted scenes seen through a vignette. There is one theme, **Grimoire**. Components read semantic tokens, never raw values, so the whole look can be tuned from `tokens.json`.

Start here. This page says how the documentation is organised, which way to read it, and what its names mean.

## How the documentation is organised

| Section | Answers |
| --- | --- |
| Principles | What do we believe, and what wins when rules pull apart? |
| Principles · Anti-generic guardrails | What keeps the game from drifting into a generic dashboard or an AI-made game UI? |
| Foundations · Colour … Iconography | What is the raw material, and what is each token for? |
| Standards | What must every part do — hierarchy, disclosure, art-optional, content? |
| Standards · States | Which states can a part be in, and how does each one look, read and sound? |
| Standards · State combinations | When a thing is in several states at once, which marks show, where, and what is said in words? |
| Registries | What are the game's canonical lists, and how is each one shown? |
| Components | Which parts exist — generic and game-specific — and what state are they in? |
| Patterns | Which compositions recur, and how are they built? |
| Shell | How is every in-game screen framed, and where does chat go? |
| Page Archetypes | Which whole-screen templates exist? |
| Governance | How does the system change? Naming, statuses, templates. |
| Governance · Decision Log | What was decided, when, and why? |
| Governance · Audit & consolidation map | Which parts to keep, revise, merge or retire, and in what order? |
| Governance · Code parity | How does the game's Angular edition map onto the reference components, and how is that checked? |

Every component also has its own page in the component catalogue: a live preview, its props, and its guidelines.

## The layer model

The system is built in layers. Each layer draws only on the layers above it in this table, never on one below.

| Layer | What it holds | Read |
| --- | --- | --- |
| Principles | The beliefs every decision answers to | Principles |
| Tokens and Foundations | The raw material: colour, type, numerals, space, layout, surfaces, lines, shape, ornament, motion, icons | `tokens.json`, Foundations · … |
| Standards | What every part must do: states, hierarchy, disclosure, art-optional, content | Standards, Standards · States, Standards · State combinations |
| Registries | The game's canonical lists: rarities, damage types, channels, resources, attributes, conditions, slots, glyphs, marks | Registries |
| Components | Generic interface parts | Components |
| Game Components | Parts that carry the game's own vocabulary | Components |
| Patterns | Recurring compositions, named `Pattern<Name>` | Patterns |
| Shell | The frame every in-game screen sits in, and where chat goes | Shell |
| Page Archetypes | Whole-screen templates, named `Archetype<Name>` | Page Archetypes |

Governance sits beside every layer: how the system changes, its templates, and the Decision Log.

## Reading paths

- **New here:** this page, then Principles.
- **Designing a screen.** Pick the closest Page Archetype. Read Shell for the frame and the two chat layouts. Then read the Patterns and components the archetype names, and Standards · States for every state the screen can be in.
- **Building a part.** Read its component page, then each Foundation and Standard it cites. Take every game list (rarities, channels, resources) from Registries rather than typing it again.
- **Writing copy.** Standards (Content) for voice and labels, Standards · States for the exact words of every state, Foundations · Numerals for numbers, Registries for the game's nouns, codes and marks.
- **Changing the system.** Governance first. Then log what you decided in the Decision Log.

## Names

The core parts keep their evocative names. Each has a plain subtitle, so nobody has to guess what it is.

| Name | Plain subtitle |
| --- | --- |
| Folio | the detail panel |
| Ledger | the labelled value list |
| Chronicle | chat and the game log |
| Sigil | the hex stat badge |
| Constellation | the stat star chart |
| Emblem | the attribute sign |
| Stage | the scene backdrop |
| GameShell | the screen frame |

New parts get plain descriptive names, like LoadoutSlot or SearchField. Compositions are named `Pattern<Name>` (Patterns) and `Archetype<Name>` (Page Archetypes).

## Words these pages use

- **Must, should, never.** *Must* is required; breaking it is a bug. *Should* is the default; departing from it needs a reason, written down where you depart. *Never* is forbidden.
- **Status.** Every part is Stable, Revising, Draft or Deprecated — Governance defines each. (A part's *status* is not its *state*: states are what Standards · States describes.)
- **Ground, surface, ink, gilt, arcana.** The colour roles — Foundations · Colour.
- **Stage and Page.** The two kinds of stage content: a scene with art, or a scrolling information screen — Shell.

## Where the code lives

- `tokens.json` — every token. `scripts/build-tokens.mjs` compiles it to `tokens.css`.
- `icons.json` — the icon set, the one source of icon drawings. `scripts/build-icons.mjs` writes it into both editions (D-125).
- `components/bundle.js`, `components/bundle.css`, `components/index.d.ts` — the reference components (React, on `window.LL`), their `lg-` styles and their types. Frozen since D-127: `bundle.css` stays the source of the app's component styles until step 9 of `ANGULAR_DESIGN_SYSTEM_PLAN.md` (repository root), and step 10 removes the rest.
- In the game repository — an Angular edition of the same components (standalone `lg-*` components) under `src/app/shared/components/grimoire`. It renders the same markup and shares `bundle.css`: `scripts/sync-styles.mjs` copies the tokens, styles and fonts into `src/styles/grimoire` (D-092), and the parity check in `parity/` compared the two editions (D-093, Governance · Code parity). Since D-127 the Angular edition is the only implementation and the parity check is retired.

## How to extend it

Follow Governance. Change the highest layer in the table that solves the problem — a token before a component, a component before a pattern. Write new sections and component pages from the templates in Governance, give every part a status, name new parts plainly, and record each decision in the Decision Log.

---

## Consuming this system

`components/bundle.js` defines `window.LL` (49 components); `components/bundle.css` is its stylesheet; `tokens.css` is every token as a CSS variable plus `@font-face` for the fonts, compiled from `tokens.json`. `components/bundle.css` reads its variables from `tokens.css`. The bundle needs `components/lib/react.production.min.js` (`window.React`) and `components/lib/react-dom.production.min.js` (`window.ReactDOM`), loaded before it. A preview (`components/<Name>/preview.html`) expects all of these to be loaded first.

- **The catalog.** `catalog/index.html` loads them for every preview and shows the documentation. Serve this folder (`python -m http.server 4600 -d LL/src/Presentation/ll/design-system`) and open `http://localhost:4600/catalog/`. Frozen since D-127; the dev-only `/grimoire` showcase in the app replaces it (D-129).
- **The game.** The game uses the Angular `lg-*` components in `src/app/shared/components/grimoire/` and the generated copies of these styles in `src/styles/grimoire/`, not this bundle.
- **Assets.** Image files in `assets/<Group>/`, listed in `api/assets/<Group>.md`. From a preview, refer to one as `../../assets/<Group>/<file>`.
- **Fonts.** Atkinson Hyperlegible is in `fonts/`; the other families load from Google Fonts.

**Read, per thing:** a component’s props, parts and examples: `api/components/<Comp>.md`; token values: `api/tokens.md`; assets and their paths: `api/assets/<Group>.md`. `components/<Comp>/README.md` and `assets/<Group>/README.md` are the long-form pages a card links to. `tokens.json`, `manifest.json`, `components/index.d.ts` and `design-system.json` are sources for tools. To change the system, read `AGENTS.md` first.

## Index

**Tokens**

- `api/tokens.md` — Every token: surface, text, fill, palette, type, spacing, radius, shadow, font-size, line-height, letter-spacing, density, icon-size, border-width, opacity, layout, z-index, motion. (46.4k)

**Icons and assets**

- `api/assets/Logos.md` — 1 file, as files in `assets/`. (1.0k)
- `api/assets/Icons.md` — 15 files, as files in `assets/`. (3.0k)
- `api/assets/Currency.md` — 2 files, as files in `assets/`. (1.4k)
- `api/assets/Ornaments.md` — 2 files, as files in `assets/`. (1.4k)
- `api/assets/Backgrounds.md` — 5 files, as files in `assets/`. (1.4k)
- `api/assets/Cards.md` — 3 files, as files in `assets/`. (1.2k)

**Components** (`api/components/<Comp>.md`, 54; 2 of them showcase pages)

- **Shell**: `GameShell` — The screen frame · `TopBar` — The top bar · `NavRail` — The main navigation · `Stage` — The scene backdrop · `Page` — The information screen frame · `Folio` — The detail panel · `Chronicle` — Chat and the game log · `KeyHints` — The keyboard shortcut hints · `Activity` — The current action · `Objective` — The pinned quest
- **Components · Actions & input**: `TabStrip` — The tabs · `Button` — The command button · `SearchField` — Search with suggestions
- **Components · Containers**: `PageHeader` — The information screen heading · `Banner` — The headline block · `Panel` — The content box · `Notice` — The persistent notice
- **Components · Data**: `StatFigure` — The headline number · `Ledger` — The labelled value list · `Meter` — The progress bar · `Track` — The milestone track · `StatTile` — The compact stat · `Delta` — The stat change
- **Game Components · Character**: `LevelPlate` — The level display · `ProfileIdentity` — Who a player is · `Sigil` — The hex stat badge · `Constellation` — The stat star chart · `JourneyCard` — The next-step guide · `Emblem` — The attribute sign
- **Game Components · Items & economy**: `LoadoutSlot` — An Essence loadout slot · `ItemLink` — An item named in text · `ItemSlot` — The item frame · `CurrencyPill` — The currency amount
- **Components · Lists & labels**: `EntryList` — The browsable name list · `ListRow` — The list row · `Tag` — The status label · `Presence` — The online status
- **Components · Type & ornament**: `Heading` — The titles · `SectionRule` — The dividers · `Icon` — The game's icon set
- **Foundations**: `AccessibilitySpecimen` — The accessibility rules on real parts · `DensitySpecimen` — The three densities side by side · `LayeringSpecimen` — The levels, the stack and four layered scenes · `LayoutGridSpecimen` — The attribute grid at three region widths · `LayoutSpecimen` — The list and inspector at three region widths · `LinesSpecimen` — The line types, the ladder and the rhythms, drawn both ways · `MotionSpecimen` — The motion tokens, and every motion category beside its reduced twin · `NumeralSpecimen` — The numeral rules in use · `OrnamentSpecimen` — The decorative devices and the budget, counted on the Creature Archive · `ShapeSpecimen` — The shape vocabulary, the Button's two shapes and the radius roles · `TypeRamp` — Every type style, by role · `TypeSpecimen` — The type ramp in use
- **Standards**: `StateCombinationSpecimen` — Several states on one thing, resolved
- **Patterns**: `PatternFeedback` — Whether a change, an effect or a cost works for the player or against them
- **Page Archetypes**: `ScreenArchive` (showcase page) — The showcase for ArchetypeArchive: the Creature Archive · `ScreenOverview` (showcase page) — The showcase for ArchetypeInformation: the Character Overview
