# LinesSpecimen

The line types, the ladder and the rhythms, drawn both ways.

**Status:** Draft

Foundations · Lines, shown on content from the game. Four parts, top to bottom:

- **Five line types.** Separation, interactive edge, selection edge, decorative ornament and dotted leader, each on a real part with its colour, width, style and where it goes. Every sample sits on a Level 1 surface with no border, except the ornament, which sits in a gilt frame.
- **The decision ladder.** Space between two Ledger-style groups; a `hairline` SectionRule over a reward line; a full border only on an ItemSlot and an input.
- **Row rhythm.** Separators on a guild member list; zebra on an arena rankings table in `lg-tablewrap--zebra`; spacing alone on three rewards (`rhythm="spacing"`).
- **Do and don't.** A guild member list, an inventory list, a Folio and a form, each drawn once by the rules and once with the lines they forbid. The don'ts are the same components with forbidden lines added in the preview's own styles, so they are specimens of mistakes, not styles to copy. The don't Folio's extra ornaments are static markup, so the SectionRule warning does not fire in the catalogue.

At Large and Extra large text each pair takes a full row, so the lists keep their names.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | Each part is a `section` named by its heading. Lists are named lists; the rankings table is a focusable, named region; the form's groups are named by their headings (fieldsets in the don't); each input has a visible label |
| Keyboard | The Lists are one tab stop each: Up and Down move. The tabs move with Left and Right. The rankings table scrolls with the arrow keys when it overflows |
| Focus | `focus-ring` on every control; a focused row rises above its neighbours |
| Announced | Rarity by name after each item; Presence as Online or the time last seen; nothing of its own |
| Hover and tap | Rows wash on hover; the Ledger rows here have no explanations |
| Target size | Rows 32–48px by density; inputs and Buttons the control height; the small Button 32px |
| Text scaling | At 115% and 130% the pairs stack one to a row, and the table scrolls inside its wrapper |
| Colour | Every line type differs by more than colour: position, width, style or a wash. `line` separators and `row-stripe` are under 3:1 and carry nothing the text does not |
| Motion | Nothing |

## Related components

- SectionRule — the dividers
- Panel — the content box
- Ledger — the labelled value list
- ListRow — the list row
- Folio — the detail panel
- ItemSlot — the item frame
- CurrencyPill — the currency amount
- TabStrip — the tabs
- Button — the command button
