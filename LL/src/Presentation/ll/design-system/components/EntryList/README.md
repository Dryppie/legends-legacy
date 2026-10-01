# EntryList

The browsable name list.

**Status:** Draft

A tall list of names — creatures, guild members, prophecies — where the selected one is marked with a bar and the ends fade out.

**Provide:** `items` (`[{ id, name, tag?, locked?, reason?, ready? }]`), `activeId`, `onSelect`, a height from the parent (it scrolls) and `density`. Up and down arrows move the selection. A locked entry needs its `reason`: how it unlocks.

- Names are Marcellus in `name-header` (24 / 30) in `ink-muted` — a list read one name at a time over stage art, so it keeps the display face (D-030); the active one is `ink` with the selection edge — a 2px `arcana-glow` bar inside its start — and the `surface-raised` wash. It was `gilt`, which is for the current location only (D-086). The star marker is gone: a star polygon is an Emblem's sign, and a four-point star reads as a diamond at that size (D-065, Foundations · Shape).
- **Density:** Comfortable and Standard set 24px names on 48 or 40px rows; Compact sets `name-row` (17px Marcellus) on 32px rows, for a long roster. The list keeps 4px of inline padding, so a focused entry's ring is never clipped.
- **Locked entries stay in reach** (Standards · States, D-087). The name is `ink-disabled` with a Locked Tag (dashed). Up and Down land on it and its unlock condition opens beside it in the reason tip; it is never selected — selection stays where it was — and Enter or a click pins the condition and announces it. Hover takes no wash.
- Tags ("+ New") sit after the name as a `new` Tag. One Tag per row: a locked entry shows Locked in place of its own tag.
- **Attention:** `ready` draws the `arcana-glow` diamond at the row's end — true, or the words screen readers hear ("1 point to spend"). A locked entry takes none, and a row whose Tag already says it takes none either (Standards · State combinations).
- Hover is the neutral `surface-raised` wash, as on every row (it was `gilt-soft`).

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | Text: the name in `ink-muted` | The name | "Dire Wolf" |
| Hover | Fill: the `surface-raised` wash; the name turns `ink` | — | Nothing |
| Focus-visible | Edge: `focus-ring` | — | The name and state |
| Selected | Edge: the 2px `arcana-glow` bar; Fill: `surface-raised`; the name in `ink` | — | "selected" |
| Locked | Text: the name in `ink-disabled` and a Locked Tag (dashed); the reason tip | "Locked", then the condition: "Clear Floor 10 to unlock" | "Ember Knight, unavailable. Locked. Clear Floor 10 to unlock." |
| New | Text: a `new` Tag after the name | "+ New" | "+ New" — the screen's own Tag text |
| Ready | Marker: the `arcana-glow` diamond at the row's end | `ready`'s words, in the Folio | "Dire Wolf, 1 point to spend" |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `listbox` named by `label`; entries are `option`s with `aria-selected`; locked entries are `aria-disabled`, with their condition as the description |
| Keyboard | One tab stop (the focused entry, else the active one): Up and Down move, wrapping, and select every entry but a locked one; Home and End jump; Enter or Space on a locked entry shows its condition |
| Focus | `focus-ring`; the list keeps 4px of inline padding so the ring is never clipped |
| Announced | The name and its Tag; a locked entry: "Ember Knight, unavailable. Locked. Clear Floor 10 to unlock." |
| Hover and tap | A locked entry opens its reason tip beside it on hover and focus; a tap pins it and Escape closes it. Nothing else |
| Target size | 40px rows (32px Compact) |
| Text scaling | Names truncate; the list scrolls |
| Colour | The active entry also takes the bar; a locked one also its Locked Tag |
| Motion | The hover wash fades over `duration-fast` (a layer's opacity); the name's colour and the selection change at once. At once under reduced motion |
