# Objective

The pinned quest.

**Status:** Draft

The pinned quest's title and its current objective with the count, in the TopBar's centre — the one "now" thing there when no run is in progress (D-110). A run's Track takes its place during a dungeon run or tower climb. Its full tracker, an `lg-objective-panel`, opens in a Popover beneath it (D-146).

**Provide:** `kicker` ("Quest"), `heading` (the quest's title), `objective` (the current objective's text), `current` and `required` for its count, and the full tracker in an `lg-objective-panel`. It keeps its own open state; bind `[(open)]` to control it. Its host is its box (D-143).

- The kicker in `label` capitals, `ink-muted`; the title in `body-compact` 600, `ink`, truncating; the objective in `caption`, `ink-muted`, with the count in `ink` and tabular figures at its end.
- With a tracker it is a button that opens it (`aria-haspopup`, `aria-expanded`, `aria-controls`): a `line` edge at rest, `line-strong` and the `surface-raised` wash on hover and while open.
- The tracker is a Popover on the CDK overlay, centred beneath it and at most 26.25rem wide, named by the quest: a Level 2 float that moves `space-1` into place over `duration-base`. Focus moves into it.
- Escape, a press on the button, or Tab past the tracker's end close it and return focus to the button; a press outside closes it where the player pressed. An Escape something above it took first (the tip) leaves it open, so Escape closes the topmost layer first (D-134).
- Keep the text to the objective; rewards and the chain belong in the tracker.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | Text: title, objective and count | "The First Hunt", "Defeat wolves 3 / 5" | "Quest The First Hunt Defeat wolves 3 / 5, button, collapsed, has popup dialog" |
| Hover | Fill: the `surface-raised` wash; edge `line-strong` | — | Nothing |
| Focus-visible | Edge: `focus-ring` | — | Its name and state |
| Open | Fill and edge as hover; the tracker beneath | — | "expanded"; then the tracker's name as focus moves in |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `button` with `aria-haspopup`, `aria-expanded` and `aria-controls` when it has a tracker; the tracker is a non-modal dialog named by the quest |
| Keyboard | Enter or Space opens it with focus inside; Escape or Tab past its end closes it and returns focus |
| Focus | `focus-ring` |
| Announced | The expanded state; the objective's count is not live |
| Hover and tap | The wash; a tap opens the panel |
| Target size | At least 44px tall |
| Text scaling | Title and objective truncate; the count keeps its width |
| Colour | No hue |
| Motion | The panel rises in over `duration-base`; at once under reduced motion |
