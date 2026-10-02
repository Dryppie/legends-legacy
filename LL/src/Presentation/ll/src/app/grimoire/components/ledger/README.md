# Ledger

The labelled value list.

**Status:** Draft

A list of label / value rows under a heading, joined by dotted leaders — attributes, a combat summary, a price list, an upgrade's changes — where every row can explain itself.

**Use:**

```html
<lg-ledger heading="Offense">
  <div lgLedgerRow label="Power" [value]="142" description="Raw force behind every blow and spell."></div>
  <div lgLedgerRow label="Armor" value="38%" sub="240 Armor Rating" description="…" tipMeta="From equipment: 240 Armor Rating"></div>
</lg-ledger>
```

**Provide:** `heading`, optional `density` and `columns="2"`, and one `div lgLedgerRow` a row, each with `label` and `value`, and any of `sub` (a second line under the value: "240 Armor Rating"), `description` (what the row means: its explanation), `tipMeta` (a footnote in the explanation, in `ink`), `muted`, and for a change `delta` (a signed number), `deltaPolarity` (`better`, `worse` or `neutral`) and `deltaText`. Import `LG_LEDGER` for both. A feature that builds its rows as data can type them `LgLedgerRow` and write them out with `@for`.

- **The Ledger is its own box** (D-137): a region named by its heading, holding a `dl`. A row is a `div`, because a `dl` holds its term and definition pairs in `div`s (the List's rows are `li` for the same reason).
- Labels are `ink-muted` 14px; values are `numeral-row` (Barlow Condensed 18px) in `ink` with tabular figures — ordinary data values are never gilt (D-018); sub-lines are `caption` in `ink-muted`; leaders are dotted `line-strong` `border-hairline`, from the end of the label to the value.
- **Values align on the decimal point.** The rows share three columns through CSS subgrid — label, integer, fraction and unit — so 1,284, 24.8%, 84 HP/5s and 184.6 threat/s line up however many decimals they carry. A unit after the number (`%`, `s`, `HP/5s`, `Soulstones`) is set smaller and muted; a value that is not a plain number (a word) sits at the right, and a range (18–24) or a fraction (7 / 20) stays whole (D-035).
- **Never empty.** A missing or not-applicable value (`null` or omitted) shows "—"; zero shows "0".
- **Explanations.** A row with a `description` shows it in the tip (D-134) on hover and keyboard focus: the label as its heading in the display face, the description in `ink-muted`, `tipMeta` as a footnote. A press pins it, which keeps the row's Level 2 wash (`surface-raised`). The tip is the page's one float, a Level 2 floating surface on the CDK overlay, so no region clips it (Foundations · Surfaces & Layering).
- **Changes** (D-137). `delta` puts a Delta on the second line, under the value: ▲ +4, ▼ −1.2s, or ±0 when it is 0, in the value's own unit unless `deltaText` says otherwise. Its colour comes from `deltaPolarity` — whether the change helps the player — never from the sign: a cooldown falling from 8s to 6.8s is `better` (D-026). Without `deltaPolarity` the change is `neutral`, because the row can't know the stat's rules. With a `sub` as well, the line reads "▲ +12 · now 142": the comparison pattern (Patterns).
- **Two-up** (`columns="2"`, D-137): two rows a line, each column aligning its own values. It is for secondary stats in a narrow column, such as the Folio's — what StatTile was for, without the boxes. Give each row a one-word label (Armor, Crit); a longer one truncates. Six to eight rows make a group; more want a full Ledger.
- **Density:** rows are 48, 40 or 32px with 12 / 16, 8 / 12 or 4 / 8px padding; labels are `body` in Comfortable and `body-compact` otherwise; values are `numeral-row`, or `numeral-compact` in Compact. The Folio makes its Ledgers Comfortable. A focused row rises over its neighbours, so its ring shows whole.
- The heading is a `label` in `ink-muted` with a trailing `line` separation hairline. A row that explains itself takes the neutral `surface-raised` wash on hover and keyboard focus.
- **Lines** (Foundations · Lines). The dotted leader is the Ledger's own line: it belongs to Ledger rows and rows built like them (a label, then one value), never to a List, a table or a menu. The rows need no separators — the leaders join each label to its value — and a Ledger has no box around it: on the Page, in a Panel or in the Folio, space and its heading set it apart.
- Emphasis comes from the numeral face and alignment, not colour. The screen's one headline figure belongs in a StatFigure, not in a Ledger row.
- Put Ledgers in `<div class="lg-ledgergrid">` (`styles/layout.css`): four a row in a Wide region, two at Medium and Narrow, one Stacked; `lg-ledgergrid--3` and `--2` hold three or two groups. A Ledger in a grid is never narrower than `ledger-min` (15rem): the grid drops a column first (Foundations · Layout).
- Format values before passing them (`lgFormatUnit(84, 'HP/5s')`) — the Ledger splits a unit off to style it, but never adds one.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A region named by its heading; rows are `dt` / `dd` pairs in `div`s, so the label names the value |
| Keyboard | Rows that explain themselves are one tab stop (roving tabindex, the CDK's `FocusKeyManager`): Up, Down, Home and End move, without wrapping, and typing a label's first letters moves to it; Escape closes an open explanation |
| Focus | The focused row rises above its neighbours, so its ring shows whole; the tab stop stays on the last row focused |
| Announced | The label, the value with its unit, a change with its judgement ("+4, better"), and the explanation as the row's description (`aria-describedby`, without the label again); "—" for a value that does not apply |
| Hover and tap | The explanation opens on hover and focus, pins on click or tap, stays while hovered, and closes on Escape, a press elsewhere, or focus moving on (WCAG 1.4.13) |
| Target size | Rows are 32–48px high by density |
| Text scaling | The grid follows its region's tier and keeps every Ledger at `ledger-min` or wider, so labels stay whole at 130%; a label that is still too long truncates, and a value never does |
| Colour | Values are `ink`; a change carries its Delta glyph, sign and word |
| Motion | The explanation arrives over `duration-fast` on `ease-enter` and leaves over `duration-fast` on `ease-exit`, moving `space-1` into place. At once under reduced motion |

## Related components

- Delta — the stat change
- StatFigure — the headline number
- Tooltip — a short explanation beside the thing under the pointer or focus
