# Angular Frontend Instructions

These instructions apply to the LegendsLegacy Angular frontend in this directory.

## Product Feel

- Build actual game UI, not landing-page or SaaS-style screens.
- The interface should feel like a dark fantasy game dashboard: textured, compact, readable, and practical.
- Prefer dense but organized panels over large marketing sections or decorative empty space.
- Keep the player focused on actions, stats, progression, inventory, combat, and decisions.
- Use clear hierarchy through borders, spacing, typography, and primary accent color rather than heavy decoration.

## Visual System

- Use the existing theme before adding new styles.
- Prefer `bg-texture` for main panels, drawers, modals, popovers, and important surfaces.
- Prefer `border-light_gray` for standard panel borders and `border-primary` for active/selected states.
- Use `text-primary` for headings, important labels, selected states, resource values, and meaningful accents.
- Use white or zinc text for body content:
  - `text-white` for primary readable text.
  - `text-zinc-300` or `text-zinc-400` for secondary descriptions and helper text.
- Use `bg-black/30` or similar low-opacity dark fills inside textured panels when content needs grouping.
- Use danger/success colors intentionally for outcomes, warnings, validation, healing, damage, and destructive actions.
- Avoid bright modern gradients, glassy SaaS cards, oversized hero layouts, decorative blobs, and one-off palettes.
- Keep rounded corners modest. Existing panels commonly use `rounded`, `rounded-md`, or `rounded-lg`.

## Layout

- Preserve the app shell feel: full-height game screens with constrained scrolling inside panels where appropriate.
- For multi-pane game tools, prefer grid or flex layouts with independent scrollable panels on desktop.
- On mobile, stack panels and allow natural vertical scrolling.
- Keep controls close to the data they affect.
- Avoid nesting decorative cards inside decorative cards. Use nested bordered groups only when they clarify content.
- Lists should be scannable: clear names, small status badges, compact metadata, and obvious selected states.
- Modals should use `bg-texture`, `border-light_gray` or `border-primary`, `text-primary` headings, and compact action rows.

## Typography

- Respect the global font setup:
  - Headings use the Marcellus feel through `h1`, `h2`, and `h3`.
  - Body text uses Poppins.
- Use `text-primary` headings for sections and entity names.
- Keep labels compact and readable.
- Do not use viewport-scaled text. Prefer Tailwind text sizes already used in the app.
- Avoid negative letter spacing.
- Use uppercase labels sparingly for small section metadata only.

## Components And Reuse

- Prefer standalone Angular components, matching the existing app.
- Prefer signals and computed state where they match nearby code.
- Keep API calls in services, not components.
- Do not introduce a new state-management library without explicit approval.
- Reuse existing shared components when they fit:
  - `app-default-header` for game page headers.
  - `app-regular-button` for normal actions.
  - `app-tabs` or `app-filter-tabs` for tabbed game views.
  - `app-selectable-list-filter` for filterable selection lists.
  - Existing modal, popover, item, equipment, essence, combat, and dungeon components.
- If a shared component does not fit, improve or extend it carefully instead of creating a parallel style.
- Keep frontend models aligned with backend DTOs and enums.

## Interaction Patterns

- Selected rows should be unmistakable: usually `border-primary`, `bg-primary/10`, and/or `text-primary`.
- Hover states should be subtle: `hover:border-primary`, `hover:bg-zinc-300/10`, or `hover:text-primary`.
- Disabled actions should remain visible but subdued with `disabled:opacity-40` and should not look clickable.
- Put destructive actions behind clear danger styling or confirmation when they consume/remove resources.
- Keep loading, empty, error, and success states in mind for any new screen or workflow.
- Do not hide important mechanics behind vague copy; show the relevant resource, level, slot, cooldown, quantity, or requirement near the action.

## Forms And Controls

- Inputs and selects should use dark backgrounds with light borders:
  - `rounded border border-light_gray bg-black/40 text-white`
  - Focus with `focus:border-primary`
- Prefer existing buttons over raw `<button>` unless the interaction is very local and simple.
- For repeated choice controls, use tabs, segmented buttons, selectable lists, or dropdowns depending on the existing nearby pattern.
- Keep form controls compact and aligned.

## Assets And Icons

- Use existing image assets under `assets/` where appropriate.
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

- Check the result against the existing game styling: `bg-texture`, `border-light_gray`, `text-primary`, compact panels, readable state.
- Verify responsive behavior in the markup: desktop can use fixed panes, mobile should stack and scroll.
- Run a lightweight static check such as `git diff --check` unless the user asked for no commands.
- Report any commands not run.
