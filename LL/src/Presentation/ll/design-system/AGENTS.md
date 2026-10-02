# Grimoire design system: working instructions

These instructions apply to everything under `LL/src/Presentation/ll/design-system/`. They also apply to any change to the Grimoire tokens, to the `lg-*` Angular components, or to their styles. The root `AGENTS.md` and `LL/src/Presentation/ll/AGENTS.md` still apply; where they disagree about Grimoire, this file wins.

## What this is

Grimoire is Legend's Legacy's design system. Until 1 October 2026 it was a Claude Design artifact; it now lives here (D-090). This folder is the **source of truth**. The artifact is a read-only archive: never edit it, never publish to it, and never copy from it again.

**One implementation (D-127, D-133).** The `lg-*` Angular components in `src/app/grimoire/` are Grimoire's only implementation; the old reference edition, its catalog and the parity check are gone (D-133). This folder holds what is not code: the tokens, the icon drawings, the docs, the source images and the scripts that turn them into code. `ANGULAR_DESIGN_SYSTEM_PLAN.md` (repository root) sets the order of work and the Angular conventions that new and re-shaped components follow (its section 8). Grimoire keeps the `lg-` prefix (D-128).

Backlog prompts in `DESIGN_SYSTEM_IMPLEMENTATION_PLAN.md` (repository root) were written for Claude Design. Read them as follows:

| The prompt says | It means |
| --- | --- |
| "the Grimoire Design System", "Claude Design" | this folder |
| a section, a page, "add a page to Standards" | a Markdown file under `docs/`, listed in `design-system.json` → `docs.sections` |
| a catalogue entry, a component, a preview | the part's folder in `src/app/grimoire/<tier>/<name>/` (its `README.md`) and its showcase entry (`src/app/grimoire/showcase/entries/`) |
| the reference component, `LL.<Name>`, an `LL.*` helper | the `lg-*` Angular component, or the helper in `src/app/grimoire/core/` (Governance · Code lists them); the reference is gone (D-133) |
| the Angular edition, code parity, DS-133 | the `lg-*` components, the only implementation; parity is retired (D-127, D-133) |
| "update those components" | update each one fully: its README, its component, its styles (`<name>.component.css`), its stories and, when behaviour changes, its spec |
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
| `docs/10-governance/04-code.md` | Governance · Code: how the code is organised, its conventions, its helpers and its checks |
| `tokens.json` | Every token. **The only source of values.** `scripts/build-tokens.mjs` compiles it into `src/app/grimoire/tokens/`. |
| `icons.json` | The icon set: each icon's view box, stroke width and shapes. **The only source of icon drawings** (D-125). |
| `fonts/` | The bundled font files (Atkinson Hyperlegible); `build-tokens.mjs` copies them beside the compiled tokens |
| `design-system.json` | Asset groups and the documentation order (`docs.sections`) |
| `assets/<Group>/` | Source images and their pages (`README.md`). `assets/Icons/` holds the game's own sidebar SVGs; no component draws from them. |
| `scripts/build-tokens.mjs`, `scripts/build-icons.mjs`, `scripts/check.mjs` | Token compiler, icon generator and static checks |

**The code (`src/app/grimoire/`, imported as `@grimoire`):**

| Path | What it holds |
| --- | --- |
| `../src/app/grimoire/tokens/` | Generated from `tokens.json` and `fonts/`: `tokens.css` (every token as a `--lg-*` property, the type classes, `@font-face`), `tokens.ts` (durations, easings, breakpoints, layers) and `fonts/`. Never edit them. |
| `../src/app/grimoire/styles/` | What no part owns: `base.css` (type roles, density, reading font, reduced motion, root, focus, elevation), `layout.css` (regions, content tiers, track layouts), `attention.css`, `reason-tip.css`, and `grimoire.css`, which assembles every stylesheet in cascade order (D-132) |
| `../src/app/grimoire/core/` | Shared helpers: formatting, states, the announcer, the reason tip, the layer stack, roving focus, motion and live values, ornament warnings, the generated icon set (`grimoire-icons.ts`, from `icons.json`; never edit it) and the stylesheet loader |
| `../src/app/grimoire/<tier>/<name>/` | One folder per part, in four tiers (`primitives`, `components`, `game`, `shell`): `<name>.component.ts`, `<name>.component.css`, `<name>.component.spec.ts` and `README.md`, the part's guidelines |
| `../src/app/grimoire/testing/` | Component test harnesses, imported as `@grimoire/testing` (D-130) |
| `../src/app/grimoire/showcase/` | The dev-only `/grimoire` showcase: one entry per part, one story per state or variant (D-129). Its `README.md` says how to add an entry. |
| `../e2e/grimoire/` | The snapshot and accessibility run over every story (`npm run grimoire:snapshots`): its fixtures, fonts, per-platform baselines and the known axe debt (`axe-known.json`) |

## Rules

**Must**
- Treat `tokens.json` as the only source of values. Change a value there, then run `node LL/src/Presentation/ll/design-system/scripts/build-tokens.mjs`. No hex colour, pixel font size or shadow with its own colour appears anywhere except `tokens.json` and the compiled `tokens.css`. (A mask gradient's `#000`, used as an alpha channel, is the one exception.) In CSS a token is its prefixed property, `var(--lg-ink-muted)`; in the docs it is its name, `ink-muted` (D-131).
- Treat `icons.json` as the only source of icon drawings. Add or change an icon there, then run `node LL/src/Presentation/ll/design-system/scripts/build-icons.mjs`: it writes `src/app/grimoire/core/grimoire-icons.ts`. A new icon goes into `icons.json`, never into a component or an SVG file (Foundations · Iconography).
- Make each backlog item **one change** that updates, together, everything it affects:
  1. the `docs/` sections (new sections added to `design-system.json` → `docs.sections`);
  2. `tokens.json`, then the compiled tokens (`build-tokens.mjs`);
  3. the affected parts' `README.md` pages and showcase entries (`src/app/grimoire/showcase/entries/`), with a story for every new variant or state (D-129);
  4. the part's styles in its `<name>.component.css` (what no part owns in `src/app/grimoire/styles/`), keeping the order in `styles/grimoire.css` (D-132);
  5. its spec, when behaviour changes, through its harness in `testing/` (D-130);
  6. a decision log entry for every rule added, reversed or narrowed;
  7. the `lg-*` component itself.

  If one of these does not apply, say so in your report.
- Recompile the tokens whenever `tokens.json` or `fonts/` change. `check.mjs` fails while `src/app/grimoire/tokens/` is out of date.
- Keep the boundaries (Governance · Code): `src/app/grimoire/` imports only Angular, the CDK, rxjs and itself; each tier imports only the tiers before it (`tokens`, `styles` → `core` → `primitives` → `components` → `game` → `shell`); the rest of the app imports `@grimoire`, never a path inside it.
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
- Edit `src/app/grimoire/tokens/` or `grimoire-icons.ts` by hand, or type a raw value into a component, its styles or a story.
- Change anything outside `design-system/`, `src/app/grimoire/` and the snapshot run (`e2e/grimoire/`), unless the item says to migrate a screen. The `--ll-*` tokens, `src/styles.css`, Tailwind and existing screens are out of bounds.
- Build a second component beside an existing one. Extend Grimoire's component instead.
- Publish, edit or read back from the Claude Design artifact.
- Bring back a second implementation, a catalog page or a copy of the styles: the showcase is where a part is seen, and its CSS lives beside it (D-133).
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
- No hex value, pixel font size or coloured shadow appears outside `tokens.json` and the compiled tokens.
- The change looks right in the `/grimoire` showcase, with no console errors, and the component's stories cover what changed.
- `npm run grimoire:snapshots` passes: no difference but the intended ones, whose baselines are updated in the same change, and no new serious or critical axe violation (D-129).
- The change passes the review checklists in Principles and Principles · Anti-generic guardrails (DS-003), and DS-132's acceptance checklist once it exists.
- The compiled tokens and icons are up to date, `npm run build:development` passes (it compiles every `lg-*` component through the showcase) and `npm run test:ci` passes.
- Nothing outside `design-system/`, `src/app/grimoire/` and `e2e/grimoire/` changed, unless the item says to migrate a screen.

## Checks

Every command below runs from the repository root unless it says otherwise.

```text
node LL/src/Presentation/ll/design-system/scripts/check.mjs
```

Zero errors required. It checks that the compiled tokens match `tokens.json` and the icon code matches `icons.json`; that no asset id remains; that every asset path and docs entry resolves; that decision ids run in order; that no raw value appears in `src/app/grimoire/` or a `-grimoire` screen folder; that every token property has its `--lg-` prefix and every query width is a breakpoint token; the import boundaries; and that every part has its README and a showcase entry. Warnings are existing debt (a part without a spec): don't add to them.

Look at the change in the `/grimoire` showcase (`npm start` from `LL/src/Presentation/ll`, then `http://localhost:4200/grimoire`; `src/app/grimoire/showcase/README.md` says how) and check the browser console. Then run the snapshot and accessibility run, from `LL/src/Presentation/ll`:

```text
npm run grimoire:snapshots
```

Zero differences and no new serious or critical axe violations required (D-129). After an intended change, `npm run grimoire:snapshots:update` rewrites the baselines; commit them with the change. Before the first run on a computer, `npx playwright install chromium`.

Then, from `LL/src/Presentation/ll`, check the app still builds (the development build compiles every `lg-*` component, through the showcase) and its specs pass:

```text
npm run build:development
npm run test:ci
```

If a check cannot run, say which one and why.
