# Angular Frontend Instructions

These instructions apply to the LegendsLegacy Angular frontend in this directory.

## Product Feel

- Build actual game UI, not landing-page or SaaS-style screens.
- The interface should feel like a dark fantasy game dashboard: textured, compact, readable, and practical.
- Prefer dense but organized panels over large marketing sections or decorative empty space.
- Keep the player focused on actions, stats, progression, inventory, combat, and decisions.
- Use clear hierarchy through borders, spacing, typography, and the design system's colour roles rather than heavy decoration.

## Design System: Grimoire

Grimoire is the game's design system. Its source of truth is `design-system/`, its rules are in `design-system/AGENTS.md`, and the game uses it through the `lg-*` Angular components. Those components are its only implementation (D-127): the React reference in `design-system/components/` (`bundle.js`, the previews, the catalog) and the parity check are frozen until step 10 of `ANGULAR_DESIGN_SYSTEM_PLAN.md` (repository root) removes them, so never edit them. That plan sets the order of work for the Angular design system.

- New screens, and screens being migrated, use Grimoire:
  - the `lg-*` components in `src/app/shared/components/grimoire/` (import `LG_GRIMOIRE`, or single components);
  - the Grimoire tokens and styles in `src/styles/grimoire/`.
- Take every colour, type style, space, radius, shadow and state from Grimoire. Feature code adds no hex colours, pixel font sizes or shadows of its own.
- Gold (`gilt`) is not a general accent. It has four jobs (Foundations · Colour, D-015). Selection, ready and new are `arcana`, and ordinary values are `ink`. Draw states with the channels and words in Standards · States.
- Read `design-system/AGENTS.md` before changing a Grimoire token, an `lg-*` component or `src/styles/grimoire/`. Those changes go through the design system: docs, tokens, port and decision log together.
- To see what a component looks like and how it behaves, open the dev-only showcase: `npm start`, then `http://localhost:4200/grimoire` (`src/app/grimoire/showcase/README.md`, D-129). The catalog in `design-system/catalog/` shows the frozen React reference; don't use it.
- Migrate a screen on purpose, as its own task. Don't half-convert a screen while fixing something else in it.
- A migrated screen lives beside the legacy one in a folder whose name ends in `-grimoire` (`character-overview-grimoire/`) until it replaces it, behind the one Settings → Interface → New look switch (`GrimoirePreviewPreferenceService.newLook()`), which every migrated screen and the Grimoire shell read: one switch for the whole new look, never one per screen. Its host carries the `lg-root` class, its Page takes `flow` (D-097), and its own styles use Grimoire tokens only. The legacy element styles in `src/styles.css` stop at `lg-root` (D-100), and the build leaves rem in a `-grimoire` folder alone. `CHARACTER_OVERVIEW_GRIMOIRE_PLAN.md` at the repository root is the worked example.

**Legacy styling.** The `--ll-*` tokens (`src/styles/tokens.css`), the `ll-*` shared classes in `src/styles.css`, and Tailwind colour and typography classes (`text-primary`, `border-primary`, `bg-texture`, `border-light_gray`, the `zinc` text colours) are legacy. They stay only for screens not yet migrated. Use them only for a small fix to such a screen, following "Legacy Screens" below. Don't use them in new screens, and don't add new ones.

## Legacy Screens (Not Yet Migrated)

These rules apply only when you change a screen that has not moved to Grimoire.

- Use the existing theme before adding new styles.
- Prefer `bg-texture` for main panels, drawers, modals, popovers, and important surfaces.
- Prefer `border-light_gray` for standard panel borders and `border-primary` for active/selected states.
- Use white or zinc text for body content:
  - `text-white` for primary readable text.
  - `text-zinc-300` or `text-zinc-400` for secondary descriptions and helper text.
- Use `bg-black/30` or similar low-opacity dark fills inside textured panels when content needs grouping.
- Use danger/success colors intentionally for outcomes, warnings, validation, healing, damage, and destructive actions.
- Avoid bright modern gradients, glassy SaaS cards, oversized hero layouts, decorative blobs, and one-off palettes.
- Legacy sizes are written for the old 14px root. The root now follows Grimoire (16px at Default, D-096), and the build multiplies every rem outside `src/styles/grimoire/` and `-grimoire` screen folders by 0.875 (`scripts/postcss-legacy-rem`). Keep writing legacy rem values and Tailwind sizes as before; don't convert them by hand. A rem set outside a stylesheet — an inline `style`, or code that turns rem into pixels from the root's font size — is not rebased, so multiply it by 0.875 yourself.
- Keep rounded corners modest. Existing panels commonly use `rounded`, `rounded-md`, or `rounded-lg`.
- Respect the global font setup: headings use the Marcellus feel through `h1`, `h2`, and `h3`; body text uses Poppins. Use the Tailwind text sizes already in the app.

## Layout

- Preserve the app shell feel: full-height game screens with constrained scrolling inside panels where appropriate.
- For multi-pane game tools, prefer grid or flex layouts with independent scrollable panels on desktop.
- On mobile, stack panels and allow natural vertical scrolling.
- Keep controls close to the data they affect.
- Avoid nesting decorative cards inside decorative cards. Use nested bordered groups only when they clarify content.
- Lists should be scannable: clear names, small status badges, compact metadata, and obvious selected states.
- Grimoire screens follow the layout, density and shell rules in the design system (Foundations · Layout, Foundations · Space and density, Shell). Legacy modals use `bg-texture`, `border-light_gray` or `border-primary`, and compact action rows.

## Typography

- Grimoire screens take type from the Grimoire type ramp (Foundations · Typography and Numerals) through its tokens and the `lg-*` components. They don't use Tailwind text sizes.
- Do not use viewport-scaled text.
- Avoid negative letter spacing.
- Use uppercase labels sparingly for small section metadata only. Write labels in sentence case and let CSS set capitals (Standards · Content).

## Components And Reuse

- Prefer standalone Angular components, matching the existing app.
- Prefer signals and computed state where they match nearby code.
- Keep API calls in services, not components.
- Do not introduce a new state-management library without explicit approval.
- In Grimoire screens, use the `lg-*` components first. If none fits, add the component to the design system (`design-system/AGENTS.md`) rather than building a local one.
- In legacy screens, reuse the existing shared components when they fit:
  - `app-default-header` for game page headers.
  - `app-regular-button` for normal actions.
  - `app-tabs` or `app-filter-tabs` for tabbed game views.
  - `app-selectable-list-filter` for filterable selection lists.
  - Existing modal, popover, item, equipment, essence, combat, and dungeon components.
- If a shared component does not fit, improve or extend it carefully instead of creating a parallel style.
- Keep frontend models aligned with backend DTOs and enums.

## Interaction Patterns

- Selected rows should be unmistakable. In Grimoire screens, use the selection channel from Standards · States (the `arcana` ring, bar or edge). In legacy screens, use `border-primary` and `bg-primary/10`.
- Hover states should be subtle. In Grimoire screens they come with the `lg-*` components. In legacy screens, use `hover:border-primary` or `hover:bg-zinc-300/10`.
- Blocked actions stay visible and explain themselves. In Grimoire screens, an unavailable, locked or unaffordable control stays focusable (`aria-disabled`) and shows its reason (D-087, D-088). In legacy screens, use `disabled:opacity-40` and make sure the control does not look clickable.
- Put destructive actions behind clear danger styling or confirmation when they consume/remove resources.
- Keep loading, empty, error, and success states in mind for any new screen or workflow.
- Do not hide important mechanics behind vague copy; show the relevant resource, level, slot, cooldown, quantity, or requirement near the action.

## Forms And Controls

- Grimoire screens use the Grimoire controls (for example `lg-search-field` and `lgButton`).
- Legacy inputs and selects use dark backgrounds with light borders:
  - `rounded border border-light_gray bg-black/40 text-white`
  - Focus with `focus:border-primary`
- Prefer existing buttons over raw `<button>` unless the interaction is very local and simple.
- For repeated choice controls, use tabs, segmented buttons, selectable lists, or dropdowns depending on the existing nearby pattern.
- Keep form controls compact and aligned.

## Assets And Icons

- Use existing image assets under `assets/` where appropriate.
- In Grimoire screens, use `lg-icon` and the icon rules in Foundations · Iconography.
- Use existing item, equipment, profession, dungeon, character, and essence icon patterns before adding new assets.
- Do not add decorative images unless they reinforce the actual game object, place, entity, or action being displayed.

## Engineering Rules

- This frontend is an npm project. Use `npm ci` with `package-lock.json`; never run pnpm, Yarn, or Bun in this directory because they corrupt the npm-owned `node_modules` tree.
- Keep npm caches outside the repository. In sandboxed Windows sessions, use a directory beneath `$env:TEMP`, never a path inside the checkout.
- Keep changes scoped to the requested frontend feature.
- Follow existing file structure under `src/app`.
- Keep components thin: presentation and user events in components, data fetching and state transitions in services.
- Avoid direct duplicate mapping logic in multiple components; use a shared service or model helper when needed.
- Do not add placeholder implementations unless explicitly requested.
- Do not change backend contracts from the frontend unless the task requires coordinated backend work.
- Do not run frontend builds or tests when the user has explicitly said not to; otherwise run the smallest relevant verification.

## Before Finishing Frontend Work

- Check the result against the right system: Grimoire screens against the design system's rules and states; legacy screens against the legacy styling above.
- Verify responsive behavior in the markup: desktop can use fixed panes, mobile should stack and scroll.
- If you changed anything in `design-system/`, `src/styles/grimoire/` or `src/app/shared/components/grimoire/`, run `node design-system/scripts/check.mjs` from this directory, `npm run build:development` (it compiles every `lg-*` component through the showcase) and `npm run grimoire:snapshots` (every showcase story against its baseline, and axe; D-129). The parity check is retired (D-127).
- Run a lightweight static check such as `git diff --check` unless the user asked for no commands.
- Report any commands not run.
