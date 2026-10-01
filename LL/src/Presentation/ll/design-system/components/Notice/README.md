# Notice

The persistent notice.

**Status:** Draft

A notice that stays until its cause goes: restricted access, progress being caught up, an error with a way out. It sits at the head of the stage or a region (D-111).

**Provide:** `tone` (`info`, `warning`, `danger`), `title` (what happened, in words), the detail as `children`, an `action` (usually one small Button: "Retry"), and `busy` while something is under way (`busyLabel` names it).

- Level 1 `surface-solid` with a 2px start bar in its tone — `info` azure, `warning` amber, `danger` ember — the title in `body-compact` 600 `ink`, the detail in `caption` `ink-muted`, the action at its end.
- The title carries the meaning; the bar backs it and is never alone (Foundations · Colour).
- `danger` is an alert (`role="alert"`, announced at once); the others are a status, announced politely.
- `busy` adds a small pulsing dot in the tone: an indeterminate progressbar, the one kind of loop Foundations · Motion allows outside playback.
- One notice per cause; stack several with `stack-sm`. A passing confirmation is a toast, not a Notice.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Info | Edge: the azure bar | The title | "Multiplayer access restricted …" (polite) |
| Warning | Edge: the amber bar | The title | The title and detail (polite) |
| Danger | Edge: the ember bar | The title | The title and detail (assertive) |
| Busy | Marker: the pulsing dot | The title | "In progress, progress bar, busy" |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | `status`, or `alert` for `danger` |
| Keyboard | Only its action |
| Focus | Its action's `focus-ring` |
| Announced | When it appears: politely, or at once for `danger` |
| Hover and tap | Nothing of its own |
| Target size | Its action's |
| Text scaling | The text wraps; the action keeps its width |
| Colour | Tone backs the words, never alone |
| Motion | The busy dot pulses; it stops under reduced motion |
