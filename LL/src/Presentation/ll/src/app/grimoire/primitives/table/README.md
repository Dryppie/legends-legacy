# Table

The data table.

**Status:** Draft — its look is Proposed (D-146), for review

Rows of like things read across columns: listings on the Cinder Bazaar, guild members, an inventory's items, rankings. The native `table`, styled: its caption, column heads and row heads stay the screen's own markup, so screen readers read it as a table.

**Use:**

```html
<div class="lg-tablewrap" style="--lg-table-min: 24rem">
  <table lgTable density="compact">
    <caption>Your listings</caption>
    <thead>
      <tr><th scope="col">Item</th><th scope="col" data-priority="3">Seller</th><th scope="col" class="lg-table__num">Price each</th></tr>
    </thead>
    <tbody>
      <tr><th scope="row">Ashen Blade</th><td data-priority="3">Maren</td><td class="lg-table__num">1,200</td></tr>
    </tbody>
  </table>
</div>
```

**Provide:** the native elements, with `scope` on every `th`; `density` to pin it (tables are usually Compact; without it the table follows its region); `rhythm="zebra"` for stripes in place of hairlines; `class="lg-table__num"` on a column of numbers' cells and head. A wide table goes in an `lg-tablewrap` (Foundations · Layout): it holds the first column and the head while the table scrolls, and hides columns by `data-priority` as its region narrows; set `--lg-table-min` to the sum of the priority-1 columns.

- **Column heads** are labels: Barlow 600 capitals in `ink-muted`, over a `line` hairline that travels with a sticky head.
- **Rows** are the density's row height, in its row text (`body-compact` when Compact or Standard) with its cell paddings, parted by `line` hairlines, or striped on `row-stripe` with `rhythm="zebra"`, never both. A row's own head is weight 600.
- **Numbers** are tabular Barlow Condensed 600 at the end of their cells; the unit is named once, in the head ("Price each"), never in each cell (Foundations · Numerals).
- **No edge and no fill:** it sits on its region's surface. In a flush Panel its outer cells take the Panel's inset, so the first column lines up with the Panel's title.
- **Its styles are global,** as the Button's are, because its cells are the screen's own markup.
- **Not yet:** sorting, selection and row actions; they come with the first screen that needs them (Market, Leaderboard, Inventory).

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | Text; Edge: hairlines between rows | — | "Your listings, table, 4 rows, 3 columns" |
| Empty, loading, error | A Region state in place of the body | Standards · States' words | The Region state's |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | The native `table`, named by its `caption`; `th` with `scope` |
| Keyboard | Nothing of its own; the wrapper scrolls with the keys when it overflows |
| Focus | Its controls' |
| Announced | As a table |
| Hover and tap | Nothing of its own |
| Target size | Its controls' |
| Text scaling | Columns hide by priority, then the wrapper scrolls with the first column held |
| Colour | — |
| Motion | None |

## Related components

- List — one column of rows, with selection
- Ledger — labelled values
- Region state — a table with no rows yet
