# Activity

The current action.

**Status:** Draft

What the character is doing now — "Engaged in Combat", "Idle" — with the time left, its progress and the way to it. It sits at the head of the NavRail (its `lg-nav-rail-header`), where the game's sidebar has always shown it (D-109).

**Provide:** `label` (the action), `remaining` (the time left, already printed: "00:12"), progress as `value` of `max` or `progress` 0–1, the host — `<button lgActivity>` or `<a lgActivity>` to go to the action (a press is the native `(click)`), `<div lgActivity>` to show it — `openLabel` ("Go to action" by default; `''` for none), and `compact` in the compact rail.

- A Level 1 block in the rail: `surface-solid`, a `line` edge, `radius-container`. As a button or link it is a control, so its edge is `line-strong` and hover is the `surface-raised` wash.
- The action in `label` capitals, `ink`; the time in `caption`, `ink-muted`, tabular figures so it never jitters.
- The bar is 4px, `meter-track` with an `ink-muted` fill: printed progress, not EXP, HP or SP, so it takes no hue (Foundations · Colour · Allocation). It scales on `transform` only.
- `compact` (D-117): the game's own mark — a 2rem ring in which the progress rises in a gilt wash, the ✦ at its centre and the `arcana-glow` live dot on its shoulder — over one short word, `short` ("Battling", "Stopping"; the label by default), in gilt capitals. No box; the rail items' wash on hover. The action and the time left stay its accessible name, the action its tooltip.
- One per screen, in the rail. A screen about the action itself shows it in full there instead.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | Text: the action and the time; the bar | "Engaged in Combat", "00:12" | "Engaged in Combat 00:12 Go to action, button" |
| Idle | Text: "Idle"; an empty bar | "Idle" | "Idle" |
| Hover (a button or link) | Fill: the `surface-raised` wash | — | Nothing |
| Focus-visible | Edge: `focus-ring` | — | Its name |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `button` or link, named by its contents; otherwise text |
| Keyboard | Enter or Space goes to the action |
| Focus | `focus-ring` |
| Announced | Nothing of its own: the time is not a live region, so it never chatters |
| Hover and tap | The `surface-raised` wash |
| Target size | The whole block, at least 44px tall |
| Text scaling | The action truncates; the time keeps its width |
| Colour | No hue: the words carry it |
| Motion | The fill follows the timer at once; nothing else moves |
