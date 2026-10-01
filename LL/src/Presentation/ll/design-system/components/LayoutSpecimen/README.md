# LayoutSpecimen

The list and inspector at three region widths.

**Status:** Draft

Foundations · Layout, shown on an inventory. The same Bags list (eight ListRows in a flush Panel) and its inspector sit in three regions. The inspector is Comfortable, like the Folio. Each region is drawn at the width a real screen gives it at the default text size.

- **1,248px (Wide):** the stage content at 1920px with no Folio and the Chronicle docked. The list and inspector sit side by side at 3 : 2.
- **864px (Medium):** at 1536px. Still side by side; the inspector holds its 20rem minimum and the list gives way.
- **507px (Stacked):** at 1280px with Large text. One column. The region opens on the inspector, with Back; Back or Esc returns to the list and to the row you came from.

Each inspector is a region of its own, so its stat grid follows the inspector: two tiles a row even in the Wide region. The captions measure each region live, so at Large and Extra large text they show the smaller tier it reaches.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | The inspector is a `section` named by the item's heading; Back is a Button |
| Keyboard | The List is one tab stop. In one column, Enter on a row opens the inspector; Back or Escape returns |
| Focus | In one column, opening a row moves focus to the inspector, and Back returns it to that row. Side by side, focus stays on the row |
| Announced | Nothing of its own |
| Hover and tap | The Ledger explanations, where rows have them |
| Target size | List rows 40px; the inspector's Buttons 44px (Comfortable); Back 24px or more |
| Text scaling | The point of the card: at 130% the 1,248px region is Medium and the 864px region is one column |
| Colour | Rarity by colour, code and Tag |
| Motion | Nothing |

## Related components

- ListRow — the list row
- Ledger — the labelled value list
- StatTile — the compact stat
- Panel — the content box
- ItemSlot — the item frame
