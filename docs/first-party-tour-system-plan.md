# First-Party Tour System Plan

## Goal

Build an internal Angular tour system for the `ll` frontend that can eventually replace the current `driver.js` integration without forcing that migration immediately.

The system should preserve the useful parts of the current tours:

- JSON-driven tour definitions in `src/assets/help/tours/`.
- Existing `data-tour="..."` anchors in templates.
- Highlighted browser elements.
- A popover with title, description, progress, Back, Next, and Finish controls.
- Per-character tour completion storage.

It should improve the tutorial experience by allowing steps to advance from real player actions instead of always requiring a separate `Next` click.

## Why Build It Inside `ll` First

The tour flow is tightly coupled to this Angular app:

- Angular Router navigation.
- Tutorial state.
- Inventory and combat state.
- Existing `data-tour` anchors.
- Per-character local storage.
- Dynamic panels, tabs, and route-loaded screens.

Because of that, the first version should live inside `LL/src/Presentation/ll` as a small internal engine. It should be written with clean boundaries so it can be extracted later if another consumer appears, such as the admin dashboard.

## Proposed Location

Core logic:

```text
LL/src/Presentation/ll/src/app/core/services/client-side/first-party-tour/
```

Global overlay:

```text
LL/src/Presentation/ll/src/app/shared/components/first-party-tour-overlay/
```

Tour definition data:

```text
LL/src/Presentation/ll/src/assets/help/tours/
```

## Public API

The engine should expose a narrow API:

```ts
start(pageId: string, options?: { force?: boolean }): Promise<void>;
forceStart(pageId: string): Promise<void>;
stop(markDone?: boolean): void;
next(): void;
back(): void;
finish(): void;
registerStatePredicate(key: string, predicate: () => boolean): () => void;
```

Existing `driver.js` calls should not be replaced until the new engine is tested and a tour is intentionally migrated.

## Tour Step Model

Each step should support a `kind`:

```ts
type FirstPartyTourStepKind = 'info' | 'click' | 'navigate' | 'waitForState';
```

### `info`

Use this for purely explanatory steps.

Behavior:

- Show Back if available.
- Show Next or Finish.
- Do not wait for user interaction with the highlighted UI.

### `click`

Use this when the player should click a specific browser element.

Behavior:

- Highlight the target.
- Hide Next by default.
- Let the highlighted target receive pointer events.
- Advance after the expected element is clicked.
- Keep Back available.

### `navigate`

Use this when the player should click something that changes route.

Behavior:

- Highlight the route-changing element.
- Hide Next by default.
- Advance only after the expected route is reached.
- Optionally require the click to happen on a selector before watching route completion.

### `waitForState`

Use this when progress depends on app state rather than a raw DOM event.

Examples:

- Essence absorbed.
- Equipment equipped.
- Tutorial step changed.
- Combat summary available.

Behavior:

- Highlight the relevant panel or action area.
- Hide Next by default unless explicitly allowed.
- Advance when a registered state predicate returns true.

## Example JSON

Current schema should keep working as an `info` step:

```json
{
  "element": "[data-tour=inventory-tabs]",
  "title": "Equipment tab",
  "description": "Use the Equipment tab to find your tutorial gear.",
  "position": "bottom"
}
```

New action-aware schema:

```json
{
  "id": "open-equipment-tab",
  "kind": "click",
  "element": "[data-tour=inventory-tabs]",
  "actionSelector": "[data-tour=inventory-equipment-tab]",
  "title": "Equipment tab",
  "description": "Click Equipment to find the Tutorial Sword, Tutorial Chest, and Tutorial Ring.",
  "position": "bottom"
}
```

Route-aware schema:

```json
{
  "id": "open-inventory",
  "kind": "navigate",
  "element": "[data-tour=tutorial-quest]",
  "actionSelector": "[data-tour=tutorial-quest-action]",
  "route": "/game/character/inventory",
  "title": "Open Inventory",
  "description": "Click the quest action to open Inventory.",
  "position": "bottom"
}
```

State-aware schema:

```json
{
  "id": "equip-three-items",
  "kind": "waitForState",
  "element": "[data-tour=equipped-overview]",
  "stateKey": "tutorial.equipment.complete",
  "title": "Equip your gear",
  "description": "Equip the Tutorial Sword, Tutorial Chest, and Tutorial Ring to complete First Steps.",
  "position": "left"
}
```

## Back Button Behavior

Back remains available on every step after the first.

For the first implementation:

- Move to the previous step.
- Restore the previous route if that step was recorded on another route.
- Wait for the previous target to appear.

Later improvements:

- Restore scroll position.
- Restore tab state.
- Restore modal state where practical.

## Overlay Behavior

The overlay should:

- Render once globally.
- Use four backdrop rectangles around the highlighted element so the target itself can still receive clicks.
- Use `getBoundingClientRect()` for positioning.
- Recalculate on window resize, scroll, route changes, and DOM mutations.
- Support fallback centered popovers when a target cannot be found.
- Clamp popovers to the viewport.

## Migration Strategy

1. Build the first-party system alongside `driver.js`.
2. Mount the overlay globally but keep it inactive by default.
3. Keep the old `TourService` and current `driver.js` tours untouched.
4. Migrate one tutorial tour at a time to the new schema.
5. Verify route-aware and action-aware behavior with Playwright.
6. Remove `driver.js` only after every tour has been migrated.

## Testing Plan

Unit tests:

- Step normalization.
- `next`, `back`, `finish`, and completion storage.
- Click action watcher.
- Route watcher.
- State predicate watcher.

Playwright tests:

- Tour starts and highlights the expected target.
- Info steps show Next.
- Click steps hide Next and advance on real clicks.
- Route steps advance only after navigation.
- Back returns to the previous step.
- Missing targets degrade gracefully.

## Risks

- Highlight positioning in nested scroll containers.
- Route steps where the clicked element disappears immediately.
- UI state restoration for Back.
- Ensuring the overlay does not block the action it asks the player to perform.
- Dynamic data timing after API loads.

## Initial Implementation Scope

The first implementation should include:

- Internal models.
- Coordinator service.
- Action watcher service.
- Global overlay component.
- Support for old `info` steps and new `click`, `navigate`, and `waitForState` steps.
- App root mounting.

It should not replace any current `driver.js` usage yet.
