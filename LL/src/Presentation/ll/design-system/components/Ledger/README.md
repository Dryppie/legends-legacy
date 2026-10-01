# Ledger

The labelled value list.

**Status:** Draft

A titled list of label / value rows joined by dotted leaders — attributes, a combat summary, a price list — where every row can explain itself.

**Provide:** `title`, `density` and `rows` (`[{ label, value, sub?, description?, tipMeta?, muted? }]`). `sub` is a second line under the value (the equipment rating: "240 Armor Rating"). Rows with a `description` show it in a tooltip on hover and keyboard focus, with `tipMeta` as a footnote in `ink`. The tooltip is a Level 2 floating surface on the `z-popover` layer (`surface-raised`, `line-strong` edge, `radius-float`, `shadow-float`); a pinned row keeps the Level 2 wash (Foundations · Surfaces & Layering).

- Labels are `ink-muted` 14px; values are `numeral-row` (Barlow Condensed 18px) in `ink` with tabular figures — ordinary data values are never gilt (D-018); sub-lines are `caption` in `ink-muted`; leaders are dotted `line-strong` `border-hairline`, from the end of the label to the value.
- **Values align on the decimal point.** The rows share three columns through CSS subgrid — label, integer, fraction and unit — so 1,284, 24.8%, 84 HP/5s and 184.6 threat/s line up however many decimals they carry. A unit after the number (`%`, `s`, `HP/5s`, `Soulstones`) is set smaller and muted; a value that is not a plain number (a Delta, a word) sits at the right, and a range (18–24) or a fraction (7 / 20) stays whole (D-035).
- **Never empty.** A missing or not-applicable value (`null` or omitted) shows "—"; zero shows "0".
- **Density:** rows are 48, 40 or 32px with 12 / 16, 8 / 12 or 4 / 8px padding; labels are `body` in Comfortable and `body-compact` otherwise; values are `numeral-row`, or `numeral-compact` in Compact. The Folio makes its Ledgers Comfortable. A focused row rises over its neighbours, so its ring shows whole.
- The title is a `label` in `ink-muted` with a trailing `line` separation hairline. A row that explains itself takes the neutral `surface-raised` wash on hover and keyboard focus.
- **Lines** (Foundations · Lines). The dotted leader is the Ledger's own line: it belongs to Ledger rows and rows built like them (a label, then one value), never to a List, a table or a menu. The rows need no separators — the leaders join each label to its value — and a Ledger has no box around it: on the Page, in a Panel or in the Folio, space and its title set it apart.
- Emphasis comes from the numeral face and alignment, not colour. The screen's one headline figure belongs in a StatFigure, not in a Ledger row.
- Put Ledgers in `<div class="lg-ledgergrid">`: four a row in a Wide region, two at Medium and Narrow, one Stacked; `lg-ledgergrid--3` and `--2` hold three or two groups. A Ledger in a grid is never narrower than `ledger-min` (15rem): the grid drops a column first (Foundations · Layout).
- Format values before passing them (`LL.format.unit(84, 'HP/5s')`) — the Ledger splits a unit off to style it, but never adds one.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `section` named by its title; rows are `dt` / `dd` pairs, so the label names the value |
| Keyboard | Rows that explain themselves are one tab stop (roving tabindex): Up, Down, Home and End move; Escape closes an open explanation |
| Focus | The focused row rises above its neighbours, so its ring shows whole |
| Announced | The label, the value with its unit, and the explanation as the row's description (`aria-describedby`); "—" for a value that does not apply |
| Hover and tap | The explanation opens on hover and focus, pins on click or tap, stays while hovered, and closes on Escape or a tap elsewhere |
| Target size | Rows are 32–48px high by density |
| Text scaling | The grid follows its region's tier and keeps every Ledger at `ledger-min` or wider, so labels stay whole at 130%; a label that is still too long truncates, and a value never does |
| Colour | Values are `ink`; a change carries its Delta glyph and sign |
| Motion | The explanation arrives over `duration-fast` on `ease-enter` and leaves over `duration-fast` on `ease-exit`, moving `space-1` into place. At once under reduced motion |
