# Frontend Design System

This Angular frontend uses a small internal design foundation instead of a third-party UI library. Keep new UI work incremental, standalone-component friendly, and close to the existing fantasy RPG identity.

## Tokens

Design tokens live in `src/styles/tokens.css` and are loaded by `src/styles.css`.

Use the `--ll-*` variables for repeated visual decisions:

- Typography: `--ll-font-body`, `--ll-font-display`, `--ll-text-*`, `--ll-leading-*`, `--ll-weight-*`
- Color: `--ll-color-bg`, `--ll-color-surface`, `--ll-color-text`, `--ll-color-primary`, `--ll-color-border`, state colors
- Spacing: `--ll-space-*`
- Radius: `--ll-radius-*`
- Elevation and focus: `--ll-shadow-*`
- Layering: `--ll-z-*`

Prefer tokens when a value is part of the shared visual language. Local one-off values are fine for truly unique layout constraints, such as a dungeon map cell size or an image aspect ratio.

## Shared Classes

Reusable UI classes live in the `@layer components` section of `src/styles.css`.

Current shared classes:

- `ll-page-stack`: full-height vertical game page layout.
- `ll-page-header`, `ll-header-row`: page-level title/summary area.
- `ll-eyebrow`, `ll-heading`, `ll-copy`: consistent hierarchy and text color.
- `ll-panel`, `ll-panel-strong`: large framed surfaces.
- `ll-card`, `ll-card-accent`, `ll-card-danger`: smaller repeated surfaces.
- `ll-stat-card`, `ll-stat-label`, `ll-stat-value`: compact metrics.
- `ll-badge`, `ll-badge-accent`, `ll-badge-danger`: status pills and chips.
- `ll-button`: base button affordance used by native buttons and reusable button components.
- `ll-input`, `ll-select`: consistent form controls.
- `ll-toolbar`, `ll-segmented`, `ll-segmented-button`, `ll-segmented-button-active`: filter/action bars and compact mode controls.
- `ll-state`, `ll-state-muted`, `ll-state-danger`: loading, info, and error messages.
- `ll-list-row`, `ll-list-row-danger`: repeated summary/reward rows.
- `ll-table-header`, `ll-table-row`: leaderboard, guild, marketplace, and other data-row layouts.
- `ll-empty-state`: centered empty/loading placeholders.
- `ll-modal-backdrop`, `ll-modal-panel`: modal shell styling.
- `ll-icon-orb`: circular feature icon container.
- `ll-progress-track`, `ll-progress-bar`: progress display.

Use these before creating new local Tailwind combinations for common panels, cards, badges, and messages.

## Usage Examples

```html
<section class="ll-page-header">
  <div class="ll-header-row">
    <div>
      <div class="ll-eyebrow">Dungeon Run</div>
      <h1 class="ll-heading text-2xl">Crypt of Embers</h1>
      <p class="ll-copy">Advance room by room. Checkpoints secure rewards.</p>
    </div>
    <button type="button" class="ll-button">Refresh</button>
  </div>
</section>
```

```html
<div class="ll-stat-card">
  <div class="ll-stat-label">Soulstones</div>
  <div class="ll-stat-value">12</div>
</div>
```

## Component Guidance

Use standalone Angular components. Do not introduce NgModules for design-system work.

Create a shared component when a structure repeats across pages and has behavior, inputs, projection, or accessibility concerns. Use shared CSS classes when the repetition is primarily visual.

Good shared-component candidates:

- Page shells with projected actions.
- Stateful empty/loading/error blocks with standardized ARIA behavior.
- Complex tabs, popovers, modals, or forms.

Good shared-class candidates:

- Panel/card/badge/button styling.
- Typography hierarchy.
- Compact stat rows and list rows.
- Simple loading/error/info message visuals.

## Rules

- Prefer tokens over copied pixel, color, radius, and shadow values.
- Prefer composition over inheritance.
- Keep feature CSS local only when the feature has a unique shape.
- If a visual pattern appears three or more times, move it into a shared class or component.
- Preserve routing, signals, service calls, and game logic when migrating UI.
- Keep the RPG tone: clear, readable, slightly arcane, never noisy.
