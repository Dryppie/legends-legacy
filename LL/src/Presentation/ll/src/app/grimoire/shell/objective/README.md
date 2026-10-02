# Objective

The pinned quest.

**Status:** Draft

The pinned quest's title and its current objective with the count, in the TopBar's centre — the one "now" thing there when no run is in progress (D-110). A run's Track takes its place during a dungeon run or tower climb. Its `panel` slot, the full tracker, opens in a popover beneath it.

**Provide:** `kicker` ("Quest"), `title` (the quest), `objective` (the current objective's text), `current` and `required` for its count, and the full tracker in a `panel` slot (`lgSlot="panel"`). It keeps its own open state; bind `[(open)]` to control it.

- The kicker in `label` capitals, `ink-muted`; the title in `body-compact` 600, `ink`, truncating; the objective in `caption`, `ink-muted`, with the count in `ink` and tabular figures at its end.
- With a `panel` slot it is a disclosure button (`aria-expanded`, `aria-controls`): a `line` edge at rest, `line-strong` and the `surface-raised` wash on hover and while open.
- The panel is a Level 2 floating surface — `surface-raised`, a `line-strong` edge, `radius-float`, `shadow-float` — at `z-popover`, centred beneath it and at most 26.25rem wide. It rises in over `duration-base`.
- It closes on Escape (returning focus to the button), on a click outside, or on the button. It registers with the layer stack (`lgOpenLayer`), so Escape closes the topmost layer first.
- Keep the text to the objective; rewards and the chain belong in the tracker.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | Text: title, objective and count | "The First Hunt", "Defeat wolves 3 / 5" | "Quest The First Hunt Defeat wolves 3 / 5, button, collapsed" |
| Hover | Fill: the `surface-raised` wash; edge `line-strong` | — | Nothing |
| Focus-visible | Edge: `focus-ring` | — | Its name and state |
| Open | Fill and edge as hover; the panel beneath | — | "expanded" |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `button` with `aria-expanded` and `aria-controls` when it has a tracker; the panel is a region named by the quest |
| Keyboard | Enter or Space opens and closes; Escape closes and returns focus; Tab moves into the panel |
| Focus | `focus-ring` |
| Announced | The expanded state; the objective's count is not live |
| Hover and tap | The wash; a tap opens the panel |
| Target size | At least 44px tall |
| Text scaling | Title and objective truncate; the count keeps its width |
| Colour | No hue |
| Motion | The panel rises in over `duration-base`; at once under reduced motion |
