# Icon button

A common action shown by its icon alone.

**Status:** Draft — its look is Proposed (D-145), for review

The few actions players meet on every screen and read at a glance: close, back, expand and collapse, menu, search, filter, sort, refresh, copy, link, the drag grip (Foundations · Iconography · Icons and labels). Anything that commits or changes state — equip, sell, claim, favourite — keeps its word: use a Button.

**Use:**

```html
<button lgIconButton="close" label="Close" (click)="close()"></button>
<button lgIconButton="expand" [label]="open ? 'Hide details' : 'Show details'" [pressed]="open" (click)="open = !open"></button>
```

**Provide:** the icon as `lgIconButton`; `label`, required — the action in words ("Close", not "Cross"), its accessible name and its tooltip; optionally `variant` (`quiet`, the default, or `framed`), `size` (`md` or `sm` pins a density; without it the button follows its region's), `pressed` for a toggle, `disabled`, and `state` with `reason` (or `remaining`) when it is blocked. It is a native `button` or `a`: your `(click)` is the action.

- **A rectangle** at `radius-control`, a square of the control height (`--lg-control`: 44, 40 or 32px), never a circle (Foundations · Shape). The icon is 20px, 16px when Compact.
- **Quiet:** the icon in `ink-muted`, no edge; on hover the `surface-raised` wash fades in and the icon turns `ink`.
- **Framed:** the primary Button's `surface` and `line-strong` edge, with its `gilt` hover edge, for a row of Buttons.
- **Pressed** (a toggle that is on) is Selected: an `arcana-glow` edge inside, the icon `ink`. Its `label` says what a press does next.
- **The tooltip** is the label, on hover and on keyboard focus; it adds no description, since the label is already the name.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | — | The tooltip on hover and focus | "Close, button" |
| Hover | Fill: the `surface-raised` wash; the icon `ink` | — | Nothing |
| Focus-visible | Edge: `focus-ring` | The tooltip | Its name |
| Pressed | Position: a 1px drop | — | Nothing |
| Selected (`pressed`) | Edge: `arcana-glow` inside | — | "pressed" |
| Unavailable, Locked, Restricted, On cooldown | Text: the icon `ink-muted`; Edge: `line` (framed), dashed for Locked | The reason tip, with the action named above it | "Sort by level, button, unavailable. Not while the list loads." |
| Disabled | Text: `ink-disabled` | — | Out of the Tab order |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A native `button` (or `a`) named by `label` (`aria-label`) |
| Keyboard | Tab to it; Enter or Space acts |
| Focus | `focus-ring`, outside the pressed edge |
| Announced | A blocked press says its reason (polite) |
| Hover and tap | The tooltip on hover and focus; a blocked one pins its reason on a press |
| Target size | 32px at least (Compact), past the 24px minimum |
| Text scaling | rem: it grows with the reading size |
| Colour | Pressed is also its name and `aria-pressed` |
| Motion | The wash fades over `duration-fast`; at once under reduced motion |

## Related components

- Button — every action that commits or changes state, with its word
- Icon — the drawings
- Tooltip — the label, on hover and focus
