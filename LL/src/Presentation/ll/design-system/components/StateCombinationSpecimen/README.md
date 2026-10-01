# StateCombinationSpecimen

Several states on one thing, resolved.

**Status:** Draft

Standards · State combinations, drawn on parts from the game (D-094, D-095). Two parts, top to bottom:

- **Every mark in its place.** One large ItemSlot that is Epic, selected, equipped, ready to upgrade and stacked, beside what each corner holds: identity top start, attention top end, ownership bottom start, the quantity bottom end, selection on the frame, the words in the caption.
- **Worked examples.** The seven combinations from the page, each drawn with the real component and followed by what shows and what is said only in words: equipped with an upgrade; locked and new; claimable and expiring soon; selected and too dear; undiscovered and the Creature Focus; listed and a favourite; a captured defence build changed since its snapshot.

The favourite ribbon is not drawn yet, so the listed favourite says "Favourite" in its meta line, as ItemSlot does until the icon exists.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | Each part is a `section` named by its heading; each example a `section` named by its title. Slots are buttons named by the item, its rarity, its state and its `ready` words |
| Keyboard | The slots and the Claim and Buy Buttons are tab stops; each List and the EntryList is one tab stop |
| Focus | `focus-ring` on every control |
| Announced | The locked slot and the Buy Button announce their reasons when pressed |
| Hover and tap | Slots take the wash; the locked slot's reason is printed, and the Buy Button's shortfall opens in the reason tip |
| Target size | Slots 112px; rows 32–40px; Buttons the control height |
| Text scaling | The examples drop to one column in a narrow region |
| Colour | Every mark differs by shape and place as well as colour |
| Motion | Nothing |

## Related components

ItemSlot, ListRow, EntryList, Tag, Button, NavRail, LoadoutSlot.
