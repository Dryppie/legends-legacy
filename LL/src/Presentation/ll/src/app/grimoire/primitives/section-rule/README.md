# SectionRule

The dividers.

**Status:** Draft

Dividers between groups of content: a filled label band (Status, Biography), a plain hairline, or the engraved diamond-chain ornament. A divider is the second way to set groups apart, after space and a heading, and a full border is never the third (Foundations · Lines · The decision ladder).

**Provide:** `variant` (`band`, `hairline`, `ornament`), optional `label`, `align="end"` and an `aside` slot (`lgSlot="aside"`).

- `band`: a `line` strip with an `ink` `label`-style caption, for data groups in stat columns. Data groups only.
- `hairline`: a `border-hairline` `line` rule, drawn beside its optional `label` (in `ink-muted`) and `aside`, so it needs no background to cut it. For any other group, and only where space and a heading are not enough.
- `ornament`: a lattice of hollow diamonds masked in `gilt`, between lore and effects. **At most once per surface** — a Folio, a Banner, a dialog or the Page — **and twice per screen**, counting any dialog open over it (Foundations · Ornament). The Folio draws its own, so a Folio holds no other. The Divider asset, where used in its place, counts the same.
- **Forbidden zones:** never in a table, a list, an input, a toast, a menu, a dense Panel (holding a List, a table or a Ledger, `flush`, or Compact), or a dialog other than a major commitment dialog (`data-commitment="major"`). Never between rows of data, and never in a Panel.
- **Warnings:** a second ornament on one surface, a third on one screen inside a GameShell, or one in a forbidden zone logs a console warning.
- These three are the only dividers a screen draws. Don't add a fourth style, or draw a rule as a border on a `div`; the components that close with a rule (the Panel head, the PageHeader, the Folio footer) draw their own.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `separator`, named by its `label` where there is one; the ornament's lattice is hidden |
| Keyboard | Not focusable |
| Focus | Nothing |
| Announced | The label, where there is one |
| Hover and tap | Nothing |
| Target size | Not a target |
| Text scaling | The band grows with its label; the hairline and the lattice fill the space beside the label |
| Colour | The band is a fill and the hairline decoration (`line`, under 3:1): the label and the space around the group carry it |
| Motion | Nothing |

## Related components

- Panel — the content box
- Folio — the detail panel
- Ledger — the labelled value list
- Heading — the headings
