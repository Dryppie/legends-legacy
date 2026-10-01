# ShapeSpecimen

The shape vocabulary, the Button's two shapes and the radius roles.

**Status:** Draft

Foundations · Shape, shown on parts from the game. Four parts, top to bottom:

- **Shape vocabulary.** The hexagon (Sigils, one with its ready diamond), the diamond (a World Tower Track and the rail's current item), the square (an ItemSlot and an empty slot), the circle (Presence) and the rectangle (a Panel with a Tag and a Button). Beside them, the three signs that are not shapes: ▲ ▼ and ±0 in Deltas, the Emblem's star polygon, and the Nobility crown.
- **✦ list marker.** ✦, ◆ and ◇ enlarged; Nobility perks marked with ✦ beside a Track, where the markers pair with the milestones; and the same perks with the en dash.
- **Button shape.** The same three moments drawn twice — a row of Arena opponents with a Challenge button each, an opponent's Folio with its one solid action, and a dialog footer. Column A restores the pill in the preview's own styles; column B is the system's Button, the engraved rectangle (D-064). Below them, the three corners at 44, 40 and 32px: pill, rectangle and chamfer. The chamfer is drawn with `corner-shape: bevel`, so in Firefox and Safari it shows as a rounded corner.
- **Radius by role.** Square regions, `radius-container`, `radius-control`, `radius-float` and `radius-circle`, each on a real part.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | Each part is a `section` named by its heading. The Arena list is a named list; the dialog sample is named "Challenge Seraphine"; the crown is an image named "Noble" |
| Keyboard | Buttons are ordinary Tab stops; the List of opponents follows List's keyboard model |
| Focus | `focus-ring` on every control, following each corner: 4px on a Button, round on the pill in column A |
| Announced | Deltas: "12, better", "1.2s, better", "0, unchanged" — the ± is hidden. Presence as Online or the time last seen |
| Hover and tap | The Buttons show their hover edge |
| Target size | Buttons 32, 40 or 44px by density; key caps are marks, not targets |
| Text scaling | The cells wrap within their columns at 115% and 130%; the two Button columns stay side by side |
| Colour | Every shape's meaning is also in its label, its word or its position: the Track's labels, "Online", the rarity code, "Noble" |
| Motion | Nothing |

## Related components

- Sigil — the hex stat badge
- Track — the milestone track
- ItemSlot — the item frame
- Presence — the online status
- Button — the command button
- Delta — the change
- Emblem — the attribute sign
- Folio — the detail panel
- ListRow — the list row
