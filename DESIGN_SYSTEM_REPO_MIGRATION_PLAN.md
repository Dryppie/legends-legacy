# Grimoire Design System: Move to the Repository

Prepared 1 October 2026. This plan moves the Grimoire design system out of the Claude Design artifact and into this repository, then resumes `DESIGN_SYSTEM_IMPLEMENTATION_PLAN.md` from where it stopped.

## 1. Why, and what changes

The Grimoire design system was built as a Design System artifact on another Claude account. This account cannot hold that artifact type, and type-built artifacts cannot be copied between accounts. The repository becomes the home of the design system instead.

| | Before (Claude Design artifact) | After (repository) |
|---|---|---|
| Source of truth | The artifact's `project/` files | `LL/src/Presentation/ll/design-system/` |
| Where prompts go | The Claude Design chat that owned the artifact | A Cowork session with the Legends-Legacy folder connected |
| Browsing the catalog | The artifact's built-in viewer | A local catalog page (`design-system/catalog/index.html`), plus an optional published page |
| Angular port (`lg-*`) | A separate, later step (DS-133) | Part of every item: design and code change together |
| Comments, version history | In the artifact | Git |

What stays the same: the backlog, the item order, the paste-ready prompts, the decision log, and the React reference components used by the previews.

## 2. Where things stand today

**The design system.** The live artifact is at decision D-089 (1 October 2026). Phase 0 (DS-001 to DS-004) is done. Phase 1 is done through DS-019 (State Model), including colour, type, numerals, density, accessibility, layout, surfaces, lines, shape, ornament, motion and iconography. It holds 52 component folders, 24 documentation pages, a 44 KB `tokens.css`, a 72 KB `tokens.json`, a React reference bundle and 28 image assets.

**The Angular port.** `LL/src/Presentation/ll/src/app/shared/components/grimoire/` holds 32 `lg-*` components, with `src/styles/grimoire/tokens.css` (108 lines) and `components.css` (1,010 lines). They were generated from the Grimoire version that existed before the implementation plan started, so none of D-001 to D-089 is in the code yet. The port's README says nothing in the app uses these components yet. That keeps the catch-up in Phase C low-risk: no screen changes until a screen is migrated on purpose.

**Conflicting guidance.** Three styling layers exist side by side: the `--ll-*` tokens (`src/styles/tokens.css`, described in `frontend-design-system.md`), Tailwind classes, and Grimoire. The frontend `AGENTS.md` still says to use `text-primary` for headings, labels, selected states and resource values, which is the gilt-overuse problem DS-006 fixed. Agents will follow those instructions unless they are updated (Phase B).

## 3. Target layout

```text
LL/src/Presentation/ll/
├── design-system/                     ← new: the design system's source of truth
│   ├── README.md                      overview and map (was project/README.md)
│   ├── AGENTS.md                      how to work on the design system (Phase B)
│   ├── design-system.json             index: asset groups, docs order, libraries
│   ├── manifest.json                  component list and groups
│   ├── tokens.json                    token source
│   ├── tokens.css                     generated from tokens.json; copied to src/styles/grimoire/
│   ├── docs/                          principles, foundations, standards, registries, governance
│   │   └── 10-governance/02-decision-log.md
│   ├── api/                           component and token API references
│   ├── components/
│   │   ├── <Name>/README.md, preview.html
│   │   ├── bundle.js, bundle.css      React reference components (window.LL)
│   │   ├── index.d.ts
│   │   └── lib/react*.min.js
│   ├── assets/<Group>/<file>          the 28 images, as real files (were blob ids)
│   ├── fonts/                         Atkinson Hyperlegible
│   └── catalog/index.html             local viewer for every preview (Phase A)
├── src/styles/grimoire/tokens.css     the app's copy of design-system/tokens.css
├── src/styles/grimoire/components.css the lg-* styles
└── src/app/shared/components/grimoire the lg-* Angular components
```

The artifact runtime (`artifact-type/app.js`, `app.css`, `demo.json`, `reference/`, `SKILL.md`) is not imported. It is the Claude Design viewer and its instructions, not part of your design system.

## 4. Phases

### Phase A — Import the baseline

Goal: an exact copy of the artifact's design-system content in the repository, viewable locally.

1. **Download** every file under `project/` from the artifact (version `1790842529-d9d8` or the newest at the time), plus the 28 assets from its asset store, using the shared edit access this account already has.
2. **Place** them in `design-system/` following section 3. Assets go to `assets/<Group>/<file name>` as named in `design-system.json`.
3. **Rewrite asset references.** The previews, `design-system.json`, `manifest.json` and `api/assets/*.md` point at asset-store ids (`/_blob/<id>` or `"blob": "<id>"`). Replace each with the asset's relative path. 19 previews, both JSON files and 6 asset docs are affected; the mapping is the asset table in `design-system.json`.
4. **Build the local catalog.** In the artifact, the viewer injected `tokens.css`, `bundle.css`, React and `bundle.js` into every preview. Add `catalog/index.html`: it lists components by group from `manifest.json`, and renders each `preview.html` in a frame with those four files loaded first, at the width and height from its `@dsCard` comment. It also links the documentation pages.
5. **Record provenance.** Add a decision log entry (D-090) saying the system moved to the repository, from which artifact version, and that the artifact is now an archive.

**Done when:** every file from the artifact's `project/` is present; no file contains a blob id; all 53 previews render in the catalog with images; the docs open from the catalog.

### Phase B — Working agreement

Goal: every future session, whatever the account, works the same way without re-explaining.

1. **`design-system/AGENTS.md`** states:
   - `design-system/` is the source of truth; tokens are the only source of values.
   - Each backlog item updates, in one change: the docs, `tokens.json` and `tokens.css`, the affected component READMEs and previews, the reference bundle, the decision log, and the `lg-*` port.
   - `tokens.css` is copied to `src/styles/grimoire/tokens.css` whenever it changes.
   - The definition of done (section 5) and the checks to run.
   - The documentation and README templates from DS-001 and the voice rules from DS-025 apply.
2. **Update the frontend `AGENTS.md` and `frontend-design-system.md`.** New and migrated screens use Grimoire tokens and `lg-*` components. The `--ll-*` tokens and Tailwind colour classes are legacy, kept only for screens not yet migrated. Remove the "use `text-primary` for headings, labels, selected states and resource values" rule.
3. **Prompt preamble.** The plan's prompts address "the existing Grimoire Design System" in Claude Design. Put this line before each one:

   ```text
   Work on the Grimoire design system in LL/src/Presentation/ll/design-system/ in the Legends-Legacy folder,
   following design-system/AGENTS.md. Port the result to the lg-* components in the same step.
   ```

**Done when:** a new session given only the preamble and a backlog prompt knows where to work and what "done" means.

### Phase C — Catch up the Angular port

Goal: the `lg-*` components match the design system at D-089, so new items port one small change at a time.

This is the largest phase, because the code is 89 decisions behind. Split it so each step can be checked on its own:

1. **Tokens.** Replace `src/styles/grimoire/tokens.css` with the design system's `tokens.css`. Rename or alias any token the `lg-*` components use that changed name.
2. **Foundations in `components.css`.** Apply type ramp and rem sizing, numerals, density modes, layering and z-index, lines, shape and radius, ornament budget, motion and reduced motion. Afterwards no hard-coded hex value, pixel font size or shadow remains.
3. **Existing components.** Update the 32 `lg-*` components to their current READMEs. Follow the consolidation map from DS-004: keep, revise, merge or retire each one.
4. **Shared behaviour.** Port the helpers the React bundle added: the state model (`states`, `topState`), the reason tip (`why`), formatting (`format`, including `duration`), live values and lists (`motion.useLive`, `useLiveList`), layers and Escape handling (`layers`), and announcements.
5. **New components.** Add Angular versions of the components that exist only in the React bundle: Delta and any others the audit added. Specimen previews (the `…Specimen`, `Screen…` and `Pattern…` previews) stay preview-only and are not ported.

**Done when:** `ng build` passes; each `lg-*` component's inputs match its README; a parity table in `design-system/docs/10-governance/` lists each component as matching, partial or missing.

### Phase D — Resume the implementation plan

Goal: work through the backlog again, one item per prompt, starting with **DS-020 — State Combination & Priority Rules**.

For each item:

1. Paste the preamble and the item's prompt from `DESIGN_SYSTEM_IMPLEMENTATION_PLAN.md`.
2. Claude updates `design-system/` and the `lg-*` port together, adds the decision log entries, and reports what changed.
3. Check the result in the catalog and, once a screen uses the component, in the running app.
4. Commit with the DS id in the message.

Plan sections that assume Claude Design read as follows:

- **Section 2.2** (mapping onto the artifact): the same folders, now in `design-system/`.
- **`docs.sections`**: the `docs.sections` list in `design-system/design-system.json`. The 24-page limit no longer applies, but D-089 can stay as it is.
- **DS-133 (code parity)**: already covered by every item; it becomes the parity table plus a check.

### Phase E — Optional: a published catalog page

If you want a link to browse or share, publish the catalog as an artifact on this account. Rerun it after a batch of items. The repository stays the source of truth, and the published page is a snapshot.

## 5. Definition of done for every backlog item

- The docs, tokens, component READMEs and previews say the same thing.
- The decision log has an entry for each decision, in the existing format.
- No hex value, pixel font size or shadow appears outside `tokens.json` and `tokens.css`.
- The affected previews render in the catalog.
- The `lg-*` port matches, and `ng build` passes. Run `ng test` if the component has tests.
- Nothing outside `design-system/`, `src/styles/grimoire/` and `src/app/shared/components/grimoire/` changes, unless the item says to migrate a screen.

## 6. Decisions to make before starting

1. **Folder.** `LL/src/Presentation/ll/design-system/` (recommended: next to the code it governs) or a top-level `design-system/` beside `docs/`.
2. **React reference bundle.** Keep it as the preview implementation and update it with each item (recommended: the plan's prompts and previews keep working unchanged), or drop the React previews and preview Angular components directly (more setup, one implementation).
3. **Branch.** Work on a `design-system` branch and merge per phase, or commit directly to `main`.
4. **The artifact on the other account.** Leave it untouched as an archive (recommended), or keep editing it, which would split the system in two.
5. **Section 6 of the implementation plan.** Confirm or change the "Needed Soon" assumptions, especially colour latitude (4) and target viewport (6), before DS-020 onward relies on them.

## 7. Order of work and rough size

| Step | What | Sessions (estimate) |
|---|---|---|
| A | Import, rewrite asset references, catalog, D-090 | 1 |
| B | AGENTS.md files, frontend guidance, preamble | 1 |
| C1–C2 | Tokens and foundations in `components.css` | 1–2 |
| C3–C5 | Components, shared behaviour, new components, parity table | 3–5 |
| D | DS-020 onward, one item per prompt | ongoing |

Phases A and B are prerequisites for everything else. Phase C can run alongside the first few Phase D items if you prefer, as long as each D item also ports its own change.
