# Sigil

The hex stat badge.

**Status:** Draft

The hexagon stat badge — a number in a verdigris hex, with its name beside it. The hexagon means a sigil — a stat, mastery or constellation value — and nothing else draws one (Foundations · Shape).

**Provide:** `value`, `label`, `labelPosition`, `size` (`sm` 40px, `md` = `sigil-size`, `lg` 68px), `state` and, when locked, a `reason` ("Unlocks at level 20"). Set `interactive` to make it a toggle button that emits `(activate)`.

- Fill `sigil-fill`, edge `sigil-edge`, numeral `on-sigil` in `sigil-numeral` — Barlow Condensed 700 with tabular figures, since D-034 keeps Marcellus numerals for the headline figure and the level; label in `sigil-label` with `shadow-text-art`, so it reads over stage art.
- `selected` swaps the edge to `arcana-glow` and scales the badge to 1.08 (1.06 on hover); `ready` adds an `arcana-glow` diamond meaning "can be raised" — the diamond's ready meaning; `locked` empties the hex to `surface` with a `line-strong` edge and greys the value to `ink-disabled`. A locked `interactive` Sigil stays a focusable button (`aria-disabled`): its unlock condition opens in the reason tip on hover and focus, and a press shows it instead of selecting (Standards · States). In a Constellation, arrow keys land on it too.
- **Motion** (Foundations · Motion · State change): the scale settles over `duration-fast` on `ease-standard`; the edge changes colour at once. Only `transform` moves — the old `filter` transition is gone. At once under reduced motion.
- Values stay short: up to three characters ("42", "9%"). Longer numbers belong in a StatTile.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | The `sigil-fill` hex with its `sigil-edge` | The value and label | "Power 11, toggle button" |
| Hover (with `interactive`) | Position: the badge scales to 1.06 | — | Nothing |
| Focus-visible | Edge: `focus-ring` round the badge | — | Its name and state |
| Selected | Edge: `arcana-glow`; the badge at 1.08 | — | "pressed" |
| Ready | Marker: an `arcana-glow` diamond | — | "Resistance 3, can be raised" |
| Locked | Fill: the hex empties to `surface`, `line-strong` edge, value in `ink-disabled`; the reason tip | "Locked", then the condition | "Life Steal 1, button, unavailable. Locked. Unlocks at level 20." |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `button` with `aria-pressed` when it is `interactive`, otherwise a labelled figure; named "<label> <value>", with ", can be raised" when ready |
| Keyboard | Enter or Space toggles it; in a Constellation one Sigil is the tab stop and arrow keys, Home and End move between them, locked ones included |
| Focus | `focus-ring` round the badge |
| Announced | Its label, value and state; a locked Sigil's condition as its description, and again when pressed |
| Hover and tap | The badge scales; a locked one opens its reason tip, which a tap pins and Escape closes |
| Target size | 40, 52 or 68px badges |
| Text scaling | The label grows with the reading size; the badge is in rem |
| Colour | Selected also scales; ready is also a diamond; locked is also an empty hex and its words |
| Motion | The scale settles over `duration-fast`; at once under reduced motion |

