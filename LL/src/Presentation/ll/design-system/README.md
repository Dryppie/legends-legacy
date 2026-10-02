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
| Governance · Code | How is the code organised, which conventions does it follow, and how is a change checked? |

Every part also has its own page, its guidelines, beside its code (`src/app/grimoire/<tier>/<name>/README.md`), and its stories in the `/grimoire` showcase.

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
- **Building a part.** Read its page (`README.md` beside its code) and look at its stories in the showcase, then each Foundation and Standard it cites. Take every game list (rarities, channels, resources) from Registries rather than typing it again.
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

- `tokens.json` — every token. `scripts/build-tokens.mjs` compiles it into `src/app/grimoire/tokens/`: `tokens.css` (every token as a `--lg-*` property) and `tokens.ts` (the values script needs) (D-131).
- `icons.json` — the icon set, the one source of icon drawings. `scripts/build-icons.mjs` writes it into `src/app/grimoire/core/grimoire-icons.ts` (D-125).
- `src/app/grimoire/` in the game repository — the `lg-*` Angular components, Grimoire's only implementation (D-127, D-133), imported as `@grimoire`. One folder per part, in four tiers (primitives, components, game, shell), each with its component, its styles, its spec and its page. Governance · Code describes it.
- The `/grimoire` showcase (development builds only) shows every part in each of its states, and `npm run grimoire:snapshots` checks every story against its baseline (D-129).

## How to extend it

Follow Governance. Change the highest layer in the table that solves the problem — a token before a component, a component before a pattern. Write new sections and component pages from the templates in Governance, give every part a status, name new parts plainly, and record each decision in the Decision Log.

---

## Using it in the game

- **Import** single components from `@grimoire`, or spread `LG_GRIMOIRE` into a standalone component's `imports`. Test harnesses come from `@grimoire/testing`.
- **Styles** load by themselves: the tokens, the base and every part's styles are one stylesheet, `grimoire.css`, which the app adds at startup (D-096, D-132). A screen's own styles use tokens only (`var(--lg-ink-muted)`).
- **Assets.** Source images are in `assets/<Group>/`, each group with its page (`assets/<Group>/README.md`); the game serves its own copies from `src/assets/`.
- **Fonts.** Atkinson Hyperlegible is in `fonts/`, copied beside the compiled tokens; the other families load from Google Fonts.

**Read, per thing:** a part's inputs, slots, states and rules: its `README.md` beside its code, and its `.component.ts`; token values and their use: `tokens.json`; assets: `assets/<Group>/README.md`. To change the system, read `AGENTS.md` first.

## Index

**Tokens and icons:** `tokens.json` (every token, with its value and use), `icons.json` (every icon).

**Assets:** `assets/Logos/`, `assets/Icons/`, `assets/Currency/`, `assets/Ornaments/`, `assets/Backgrounds/`, `assets/Cards/`, each with its `README.md`.

**Parts** (42; each page is `src/app/grimoire/<tier>/<name>/README.md`)

- **Shell**: `GameShell` — The screen frame (`shell/game-shell/`) · `TopBar` — The top bar (`shell/top-bar/`) · `NavRail` — The main navigation (`shell/nav-rail/`) · `Stage` — The scene backdrop (`shell/stage/`) · `Page` — The information screen frame (`components/page/`) · `Folio` — The detail panel (`components/folio/`) · `Chronicle` — Chat and the game log (`shell/chronicle/`) · `KeyHints` — The keyboard shortcut hints (`components/key-hints/`) · `Activity` — The current action (`shell/activity/`) · `Objective` — The pinned quest (`shell/objective/`)
- **Components · Actions & input**: `TabStrip` — The tabs (`primitives/tab-strip/`) · `Button` — The command button (`primitives/button/`) · `SearchField` — Search with suggestions (`components/search-field/`)
- **Components · Containers**: `PageHeader` — The information screen heading (`components/page-header/`) · `Banner` — The headline block (`components/banner/`) · `Panel` — The content box (`components/panel/`) · `Notice` — The persistent notice (`components/notice/`)
- **Components · Data**: `StatFigure` — The headline number (`components/stat-figure/`) · `Ledger` — The labelled value list (`components/ledger/`) · `Meter` — The progress bar (`primitives/meter/`) · `Track` — The milestone track (`components/track/`) · `StatTile` — The compact stat (`components/stat-tile/`) · `Delta` — The stat change (`components/delta/`) · `Num` — A number with its unit (`primitives/num/`)
- **Game Components · Character**: `LevelPlate` — The level display (`game/level-plate/`) · `ProfileIdentity` — Who a player is (`game/profile-identity/`) · `Sigil` — The hex stat badge (`game/sigil/`) · `Constellation` — The stat star chart (`game/constellation/`) · `JourneyCard` — The next-step guide (`game/journey-card/`) · `Emblem` — The attribute sign (`game/emblem/`)
- **Game Components · Items & economy**: `LoadoutSlot` — An Essence loadout slot (`game/loadout-slot/`) · `ItemLink` — An item named in text (`game/item-link/`) · `ItemSlot` — The item frame (`game/item-slot/`) · `CurrencyPill` — The currency amount (`game/currency-pill/`)
- **Components · Lists & labels**: `EntryList` — The browsable name list (`components/entry-list/`) · `ListRow` — The list row (`components/list/`) · `Tag` — The status label (`primitives/tag/`) · `Presence` — The online status (`game/presence/`)
- **Components · Type & ornament**: `Heading` — The titles (`primitives/heading/`) · `SectionRule` — The dividers (`primitives/section-rule/`) · `Icon` — The game's icon set (`primitives/icon/`) · `Key` — The key cap (`primitives/key/`)
