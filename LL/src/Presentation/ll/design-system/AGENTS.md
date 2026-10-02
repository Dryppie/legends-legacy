# Grimoire design system: working instructions

These instructions apply to everything under `LL/src/Presentation/ll/design-system/`. They also apply to any change to the Grimoire tokens, to the `lg-*` Angular components, or to their styles. The root `AGENTS.md` and `LL/src/Presentation/ll/AGENTS.md` still apply; where they disagree about Grimoire, this file wins.

## What this is

Grimoire is Legend's Legacy's design system. Until 1 October 2026 it was a Claude Design artifact; it now lives here (D-090). This folder is the **source of truth**. The artifact is a read-only archive: never edit it, never publish to it, and never copy from it again.

**One implementation (D-127).** The `lg-*` Angular components are Grimoire's only implementation. The React reference edition — `components/bundle.js`, `components/index.d.ts`, `components/lib/`, the previews (`components/<Name>/preview.html`), `catalog/` and the `api/components/` cards — and the parity check in `parity/` are frozen: never edit them, never add a case, and never port a change to them. Step 10 of `ANGULAR_DESIGN_SYSTEM_PLAN.md` (repository root) removes them; that plan also sets the order of work and the Angular conventions that new and re-shaped components follow (its section 8). Two things in the frozen area still change until their step lands: `components/bundle.css` stays the source of the app's component styles until plan step 9, and `scripts/build-icons.mjs` still writes the icon block in `bundle.js` and `index.d.ts` until plan step 10. Grimoire keeps the `lg-` prefix (D-128).

Backlog prompts in `DESIGN_SYSTEM_IMPLEMENTATION_PLAN.md` (repository root) were written for Claude Design. Read them as follows:

| The prompt says | It means |
| --- | --- |
| "the Grimoire Design System", "Claude Design" | this folder |
| a section, a page, "add a page to Standards" | a Markdown file under `docs/`, listed in `design-system.json` → `docs.sections` |
| a catalogue entry, a component, a preview | `components/<Name>/README.md`, listed in `manifest.json`; previews are frozen (D-127) |
| the reference component, `LL.<Name>` | frozen (D-127); the `lg-*` Angular component instead, and its styles in `components/bundle.css` until plan step 9 |
| the Angular edition, code parity, DS-133 | the `lg-*` components (below), the only implementation; parity is retired (D-127) |
| "update those components" | update each one fully: its README, its `lg-*` component, and its styles in `components/bundle.css` (until plan step 9); never the frozen reference (D-127) |
| "the page holds at most 24 sections" | no longer applies (D-091) |

**Which copy of a prompt.** Use the item in section 4 (Design System Backlog): its "Should define" list and its paste-ready prompt. Section 5 repeats the first fifteen prompts; treat those as copies.

**When a prompt disagrees with the system.** The prompts were written on 29 September, before most of the items they depend on were done. The docs and the decision log are newer, so they win:
- Map the prompt's wording onto the channels, tokens and components that already exist (Standards · States, Foundations, the Audit & consolidation map). Don't add a parallel channel or a second component because the prompt's wording differs. For example, in Standards · States the arcana ring is focus and selection is a bar.
- Where the item really does change a logged decision, record a new decision and mark the old one "Superseded by D-xxx" (or narrow it, saying so).
- If a prompt names a component that doesn't exist (for example ItemRow), use the existing one that does the job and say which, or add it as a new Draft component following Governance.
- List every conflict and how you resolved it in your report. If resolving one would reverse or narrow a decision already in the log, ask Martin before you change it.

## Where things are

Paths in this file are relative to `design-system/`. `src/…` means `LL/src/Presentation/ll/src/…`, the Angular app. Commands give their full path from the repository root.

| Path | What it holds |
| --- | --- |
| `README.md` | Overview, layer model, reading paths, names. Start here. |
| `docs/` | Sections: principles, foundations, standards, registries, components, patterns, shell, archetypes, governance |
| `docs/10-governance/01-governance.md` | Naming, statuses, the section template and the component README template |
| `docs/10-governance/02-decision-log.md` | Every decision (D-001 onward) |
| `tokens.json` | Every token. **The only source of values.** |
| `tokens.css` | Compiled from `tokens.json` by `scripts/build-tokens.mjs`. Never edit it by hand. |
| `icons.json` | The icon set: each icon's view box, stroke width and shapes. **The only source of icon drawings** (D-125). |
| `components/<Name>/` | `README.md` (guidelines) and `preview.html` (the catalog card, frozen since D-127) |
| `components/bundle.js`, `index.d.ts`, `lib/` | The React reference components on `window.LL`, their prop types and React itself. Frozen (D-127): never edited by hand; only `scripts/build-icons.mjs` still writes the icon block between `// @icons-start` and `// @icons-end`. Removed in plan step 10. |
| `components/bundle.css` | The `lg-*` stylesheet, which `scripts/sync-styles.mjs` copies into the app. Edit it directly until plan step 9 splits it into one CSS file per component. |
| `api/components/<Name>.md`, `api/tokens.md`, `api/assets/<Group>.md` | Short reference cards. The component cards describe the React props and are frozen (D-127); update `api/tokens.md` and `api/assets/` by hand when tokens or assets change. Their `<x-import>` lines are for the old Claude Design canvas; ignore them. |
| `manifest.json` | The component list, groups and summaries the catalog reads |
| `design-system.json` | Asset groups, documentation order (`docs.sections`), libraries |
| `assets/<Group>/` | Image files; previews refer to them as `../../assets/<Group>/<file>`. `assets/Icons/` holds the game's own sidebar SVGs, shown as assets; no component draws from them. |
| `catalog/index.html` | The local viewer for every preview and docs page; frozen with the previews (D-127) |
| `components/Cover/` | The catalog's cover card: a preview only, with no README, card or port |
| `scripts/build-tokens.mjs`, `scripts/build-icons.mjs`, `scripts/sync-styles.mjs`, `scripts/check.mjs` | Token compiler, the icon set's code in both editions (D-125), the copy of the styles into the app (D-092), and static checks |
| `parity/` | The retired parity check (D-093, superseded by D-127); frozen and no longer run. The development build compiles every `lg-*` component through the showcase (D-129). |

**The `lg-*` components (the implementation, D-127):**

| Path | What it holds |
| --- | --- |
| `../src/styles/grimoire/tokens.css`, `fonts/` | Generated copies of `tokens.css` and `fonts/`. Never edit them. |
| `../src/styles/grimoire/components.css` | Generated copy of `components/bundle.css`: the one `lg-*` stylesheet. Never edit it. |
| `../src/app/shared/components/grimoire/grimoire-icons.ts` | Generated from `icons.json` by `scripts/build-icons.mjs`: the icon set `lg-icon` draws. Never edit it. |
| `../src/app/grimoire/showcase/` | The dev-only `/grimoire` showcase: one entry per component, one story per state or variant (D-129). Its `README.md` says how to add an entry. |
| `../e2e/grimoire/` | The snapshot and accessibility run over every story (`npm run grimoire:snapshots`): its fixtures, fonts, per-platform baselines and the known axe debt (`axe-known.json`). |
| `../src/app/shared/components/grimoire/` | Standalone, OnPush, signal-based `lg-*` components, exported as `LG_GRIMOIRE` from `index.ts`. They are the implementation (D-127); their markup still matches the frozen reference until plan phase 3 re-shapes them. Governance · Code parity maps each one's props to Angular inputs, outputs and slots as they stood on 2 October 2026. |

## Rules

**Must**
- Treat `tokens.json` as the only source of values. Change a value there, then run `node LL/src/Presentation/ll/design-system/scripts/build-tokens.mjs`. No hex colour, pixel font size or shadow with its own colour appears anywhere except `tokens.json` and `tokens.css`. (A mask gradient's `#000`, used as an alpha channel, is the one exception.)
- Treat `icons.json` as the only source of icon drawings. Add or change an icon there, then run `node LL/src/Presentation/ll/design-system/scripts/build-icons.mjs`: it writes the icon block in `bundle.js`, the `IconName` type in `index.d.ts` and the app's `grimoire-icons.ts`. A new icon goes into `icons.json`, never into a component or an SVG file (Foundations · Iconography).
- Make each backlog item **one change** that updates, together, everything it affects:
  1. the `docs/` sections (new sections added to `design-system.json` → `docs.sections`);
  2. `tokens.json`, then the compiled `tokens.css`;
  3. the affected component READMEs (new components added to `manifest.json`) and showcase entries (`src/app/grimoire/showcase/entries/`), with a story for every new variant or state (D-129); the previews are frozen (D-127);
  4. the component's styles in `components/bundle.css` (until plan step 9), then the app's copies (`sync-styles.mjs`);
  5. a decision log entry for every rule added, reversed or narrowed;
  6. the `lg-*` component itself. No React port and no parity case (D-127).

  If one of these does not apply, say so in your report.
- Treat the `lg-*` components as the implementation (D-127). Never edit the frozen React reference to match them.
- Regenerate the app's copies of the styles whenever `tokens.css`, `components/bundle.css` or `fonts/` change: `node LL/src/Presentation/ll/design-system/scripts/sync-styles.mjs` (`build-tokens.mjs --sync` compiles the tokens and then runs it). `check.mjs` fails while a copy is out of date.
- Write every new section from the section template and every new or revised component page from the component README template in Governance. Give every part a status. A new part starts at Draft.
- Write copy by Standards · Content (voice, sentence case, the game's nouns, two registers), Standards · States (the exact words of each state) and Foundations · Numerals. DS-025 will extend Standards · Content; until it is done, Standards · Content is the voice rule.
- Keep the layer model (README): a layer draws only on the layers above it.
- Add a decision log entry in the existing format: the next id, today's date, the decision as one present-tense sentence, the reason, the consequence, and a status. Mark a replaced entry "Superseded by D-xxx"; never delete or rewrite an old entry.
- Keep component names identical across the docs, the `lg-*` class names and the Angular selectors (`Folio` → `.lg-folio` → `<lg-folio>`), with the `lg-` prefix (D-128).

**Should**
- Change the highest layer that solves the problem: a token before a component, a component before a pattern.
- Give a new component, or one you re-shape in its plan step, the Angular API in section 8 of `ANGULAR_DESIGN_SYSTEM_PLAN.md`: the host is its box, composition instead of configuration arrays, no `lgSlot`, native `button` and `a` hosts for controls. Don't re-shape an existing component's API outside its step in plan phase 3, because its consumers change with it.
- Clear any raw-value warning in a file you touch.

**Never**
- Edit `tokens.css` or the generated icon code by hand, or type a raw value into a component, preview or Angular style.
- Change anything outside `design-system/`, `src/styles/grimoire/`, `src/app/shared/components/grimoire/`, the showcase (`src/app/grimoire/showcase/`) and the snapshot run (`e2e/grimoire/`), unless the item says to migrate a screen. The `--ll-*` tokens, `src/styles.css`, Tailwind and existing screens are out of bounds.
- Build a second component beside an existing one. Extend Grimoire's component instead.
- Publish, edit or read back from the Claude Design artifact.
- Edit `components/bundle.js`, `components/index.d.ts`, `components/lib/`, a preview, `catalog/`, `parity/` or an `api/components/` card, except through `build-icons.mjs` (frozen, D-127).
- Commit, push or run other git commands unless Martin asks. Suggest a commit message instead.

## Open questions: what to assume

Section 6 of `DESIGN_SYSTEM_IMPLEMENTATION_PLAN.md` ("Missing Information") lists decisions Martin has not made yet. On 1 October 2026 he confirmed its assumptions for now (the migration plan's own section 6 asked for this). Use them until he changes one:

1. Art is optional: text-first everywhere, art only as an enhancement.
2. Stage for collection, progression and encounter screens; Page and Workbench for everything else.
3. Mouse and keyboard, a possible desktop wrapper, no controller.
4. The seven rarity and seven damage hues are frozen; every other colour may move.
5. "Doctrines" is a placeholder glossary entry; build components leave room for a fourth build axis.
6. 1600×900 with the rail and docked chat must fit the Character Overview; 1920×1080 is the comfortable target.

If an item depends on one of these, say which assumption you used.

## Doing a backlog item

1. Read the item in section 4 of `DESIGN_SYSTEM_IMPLEMENTATION_PLAN.md` (its "Should define" list and its prompt). Then read the decision log, and then the sections and component pages it touches. For the game's nouns, use Registries and Standards · Content; the glossary (DS-026) does not exist yet.
2. Check the decision log and the existing pages for anything that already settles it, and resolve conflicts as described above.
3. Make the change from the highest layer down, through the six parts of the one-change rule.
4. Run the checks below and fix every error.
5. Report: the files changed, the decisions logged, the checks run and their results, anything not done, and a suggested commit message that starts with the DS id: `DS-020: State combination and priority rules`.

## Definition of done

- The docs, tokens and component READMEs say the same thing.
- The decision log has an entry for each decision, in the existing format.
- No hex value, pixel font size or coloured shadow appears outside `tokens.json` and `tokens.css`.
- The change looks right in the `/grimoire` showcase, with no console errors, and the component's stories cover what changed.
- `npm run grimoire:snapshots` passes: no difference but the intended ones, whose baselines are updated in the same change, and no new serious or critical axe violation (D-129).
- The change passes the review checklists in Principles and Principles · Anti-generic guardrails (DS-003), and DS-132's acceptance checklist once it exists.
- The app's copies of the styles are up to date, and `npm run build:development` passes (it compiles every `lg-*` component through the showcase).
- Nothing outside `design-system/`, `src/styles/grimoire/`, `src/app/shared/components/grimoire/`, `src/app/grimoire/showcase/` and `e2e/grimoire/` changed, unless the item says to migrate a screen.

## Checks

Every command below runs from the repository root unless it says otherwise.

```text
node LL/src/Presentation/ll/design-system/scripts/check.mjs
```

Zero errors required. It checks that `tokens.css` matches `tokens.json`; that the icon code in both editions matches `icons.json`; that no asset id remains; that every asset path, manifest entry, preview `@dsCard` and docs entry resolves; that decision ids run in order; and that no raw value has been added. It also checks that the app's copies of the styles match, and that the `lg-*` port has no raw value. Warnings are existing debt: don't add to them.

The catalog shows the frozen React reference (D-127), so don't use it to check a change. Look at the change in the `/grimoire` showcase instead (`npm start` from `LL/src/Presentation/ll`, then `http://localhost:4200/grimoire`; `src/app/grimoire/showcase/README.md` says how) and check the browser console. Then run the snapshot and accessibility run, from `LL/src/Presentation/ll`:

```text
npm run grimoire:snapshots
```

Zero differences and no new serious or critical axe violations required (D-129). After an intended change, `npm run grimoire:snapshots:update` rewrites the baselines; commit them with the change. Before the first run on a computer, `npx playwright install chromium`.

Then, from `LL/src/Presentation/ll`, check the app still builds (the development build compiles every `lg-*` component, through the showcase):

```text
npm run build:development
```

If a check cannot run, say which one and why.
