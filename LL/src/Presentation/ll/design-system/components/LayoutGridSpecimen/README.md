# LayoutGridSpecimen

The attribute grid at three region widths.

**Status:** Draft

Foundations · Layout, shown on the Character Overview's Combat Attributes: the section on its Level 1 surface (no border), its heading, a one-line summary held to the reading width, and the four Ledgers (Offense, Defense, Recovery, Utility) in `lg-ledgergrid`.

- **1,216px (Wide):** the stage content at 1920px with no Folio and the Chronicle docked. Four Ledgers a row.
- **832px (Medium):** at 1536px. Two a row. Four groups never split three and one.
- **470px (Stacked):** at 1280px with Large text. One a row.

The grid's region sets the most Ledgers a row may hold. `ledger-min` sets the narrowest each may be, so no label truncates. Below that width the grid drops a column. The captions count the columns live. "Show columns and rhythm" outlines each column and draws the 8px rhythm the sections sit on.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | Each section is named by its heading; each Ledger is a list with its title |
| Keyboard | Each Ledger is one tab stop; Up, Down, Home and End move between rows |
| Focus | The ring on the focused row, whole in every column |
| Announced | Each row's label and value; explanations when opened |
| Hover and tap | Explanations open on hover and focus, pin on tap, close on Escape |
| Target size | 40px rows; the toggle Button 32px |
| Text scaling | At 130% the 1,216px region is Medium (two a row) and the 832px region Narrow (two a row) |
| Colour | Nothing depends on colour |
| Motion | Nothing |

## Related components

- Ledger — the labelled value list
- Heading — the titles
- Button — the command button
