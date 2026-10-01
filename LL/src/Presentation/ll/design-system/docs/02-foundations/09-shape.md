# Foundations · Shape

Square and engraved, with a little rounding only where the hand touches. Grimoire uses a few shapes, and each one means one thing: a hexagon is a sigil, a diamond is a milestone or where you are, a square is a slot for an item, and a circle is presence. Everything else is a rectangle. ShapeSpecimen shows the vocabulary, the retired ✦ marker, the Button as a pill beside the engraved rectangle, and the radius roles.

## Rules

**Must**
- Keep each shape to its one meaning, everywhere (The vocabulary, below). A shape that means something is never a decorative bullet, a badge or a flourish with a meaning of its own.
- Round with the `radius-*` role tokens only: `radius-container`, `radius-control`, `radius-float` and `radius-circle`. Don't invent radii, and don't write 1px, 2px or 999px.
- Keep regions square: the Folio, the Banner, the Stage, the rail and the Page.

**Should**
- Mark a new meaning with a word or an icon rather than a new shape, and add a shape here only with a Decision Log entry.
- Give text labels — Tags, key caps, count badges, rarity codes — the control radius, sized by their text. A label holds a word or a number, never art, so it is never read as a slot.
- Frame portraits, when the game has them, as 3 : 4 rectangles at `radius-container`, never as circles or squares.

**Never**
- Make anything a pill. No element takes a full radius.
- Draw a circle for anything but presence: not a count badge, an icon button, a bullet or a portrait.
- Use a diamond as a bullet, a badge, the Nobility mark or a delta.
- Frame a section icon in a hexagon: a hexagon holds a value.

## The vocabulary

| Shape | Means | Where it lives | Never |
| --- | --- | --- | --- |
| **Hexagon** | A sigil: a stat, mastery or constellation value | The Sigil; the Constellation's values; a Combat Style's mastery | A section icon, or a frame for anything but a value |
| **Diamond** | A milestone, a ready or claimable marker, or the current location | Track and dungeon-route milestones; the Sigil's ready mark (`arcana-glow`); the NavRail's current item and the PageHeader's section mark (`gilt`) | A bullet, a badge, a delta, the Nobility mark, or decoration beside content |
| **Square** | An item or equipment slot | ItemSlot, ListRow thumbnails, the slot inside a LoadoutSlot; dashed inside when empty | A data tile, a portrait, a decoration |
| **Circle** | Presence: online, or when last seen | The Presence dot, and nothing else | A count badge, an icon button, a bullet, a portrait |
| **Rectangle** | A container; at control size, a control or a label | Square-cornered: Panels, dialogs, the Page, tiles. At `radius-control`: Buttons, inputs, tabs, key caps, Tags, count badges, CurrencyPills | A pill |

Three signs sit beside the shapes. They are glyphs and pictures, not shapes, and they keep one meaning too:

| Sign | Means | Where |
| --- | --- | --- |
| ▲ ▼ and ±0 | A delta's direction; unchanged is ±0, with no shape (D-067) | Delta and every comparison built on it |
| Star polygon | The sign of an attribute, school or region, one point count each | The Emblem (Registries · Emblem point counts) |
| Crown | Active Nobility (D-066) | Before a character's name, under the Nobility display rules (Registries · Nobility mark) |

**Drawings keep their own lines.** The Emblem's rings, the Constellation's orbits, the ornament's diamond-chain lattice, the Folio's corners, the icons, the currency art (the Soulstone is a cut gem), the logo and cover art are pictures. They may use any shape as linework, because nothing in them sits beside content to label it. A mark that does sit beside content follows the vocabulary: that is why the Constellation's nodes are now ticks across their ring, not diamonds.

**Circle means presence.** Presence and portraits were the two candidates. Presence won: the Presence dot already exists and appears in every member list and in chat, and at 8px only a circle reads at once. A dot is also what players expect for online status. Portraits do not exist yet (D-003 keeps the game text-first). When they come, a portrait framed as a 3 : 4 rectangle is art in a frame, and avoids the round avatar with a status dot that marks the generic social app. The count badges on the rail and in chat were circles; they are now rectangles at `radius-control`.

## The ✦ list marker

**It conflicts, so it is retired.** At list size, 10–12px in `gilt`, the four-point ✦ loses its thin arms and reads as a diamond. That puts it beside the Track's milestones and the rail's marker. It is also a four-point star beside the Emblem's star polygons. It was registered as the list marker but no component drew it; the Nobility panel's perk lists used ◆ instead, which was worse.

**The list marker is the en dash**, `–` in `ink-muted`, hung before each item. It is a stroke, not a shape, so it marks nothing. A range dash always sits between two numbers with no spaces (12–18), so the two never meet. Registries · Glyphs lists both.

## Radius by role

| Token | Value | Rounds |
| --- | --- | --- |
| — | 0 | Regions: the Folio, the Banner, the Stage, the rail, the Page, and the TopBar |
| `radius-container` | 2px | Containers and rows: Panels, dialog sheets and confirmations, the band SectionRule, the JourneyCard's objective, and row washes (Ledger rows, rail items, EntryList entries, chat lines, suggestions) |
| `radius-control` | 4px | Controls, tags and slots: Buttons, inputs, tabs, key caps, clickable CurrencyPills; Tags, count badges, rarity codes; ItemSlots, thumbnails, LoadoutSlots and StatTiles. Focus rings on links and icon buttons follow it |
| `radius-float` | 8px | Popovers and drawers: tooltips, hover cards, menus, suggestion lists, toasts and the floating chat drawer |
| `radius-circle` | 50% | The Presence dot; the compact Activity's ring and live dot — the character, present and at work (D-117) |

- **Meters are square-ended.** A Meter is a gauge, like the Track's rail, so neither the thin line nor the `bar` rounds its ends.
- **A focus ring follows its element's corner:** 4px on a Button, 2px on a Ledger row.
- **Old names.** `radius-sm` (4px) became `radius-control`; `radius-md` (6px) folded into it; `radius-lg` (8px) became `radius-float`; `radius-full` (999px) is gone, and the Presence dot takes `radius-circle` (D-068).

## The Button: pill or engraved rectangle

The Button was a full pill, and so were the inputs, the key caps, the CurrencyPills and the Meters. A pill is the default control of any rounded app, and beside Grimoire's square Panels, square Folio and gilt frames it read as borrowed. The Button was drawn both ways in three dense moments from the game: a row of Arena opponents with a Challenge button each, a Folio with its one solid action, and a dialog footer (ShapeSpecimen).

| | A · The pill | B · The engraved rectangle |
| --- | --- | --- |
| **Corners** | 999px | `radius-control` (4px) |
| **Fits the house style** | No: soft beside square containers and the Folio's frame | Yes: one family of forms with Panels, slots and Tags |
| **Density** | The round ends need 8px more side room: each Challenge button takes 8px from its row's name column | 4px less on each side: the rows keep room for names |
| **Stands out as a control** | By its shape alone | By its height (32–44px against a Tag's 20px), its fill, its `line-strong` edge and its sentence-case Barlow label |
| **Solid action** | Reads as a call to action from another app | Reads as a brass plate |

**Chosen: B, the engraved rectangle (D-064).** Inputs, key caps, tabs, clickable CurrencyPills and count badges take the same corner, so a SearchField's input and its Search button match. The side padding drops from `cell-x` + 8px to `cell-x` + 4px.

**The chamfer was considered and deferred.** It is the most engraved look of the three: a bevelled corner, like a cut brass plate. But `clip-path` cuts off the edge and the focus ring. The CSS `corner-shape: bevel` draws the chamfer properly, but only in Chromium browsers; Firefox and Safari do not support it yet, so players on those browsers would see a rounded corner instead. Revisit when they ship it. ShapeSpecimen draws the chamfer beside the other two.

## Tokens used

| Token | Role here |
| --- | --- |
| `radius-container` | Containers, dialog sheets and row washes |
| `radius-control` | Controls, tags and slots |
| `radius-float` | Popovers and drawers |
| `radius-circle` | The Presence dot, the compact Activity's ring and live dot |
| `gilt` | The current-location diamond (NavRail, PageHeader), the Nobility crown |
| `arcana-glow` | The ready diamond on a Sigil |
| `sigil-fill`, `sigil-edge` | The Sigil's hexagon, and nothing else |

## Do and don't

| Do | Don't |
| --- | --- |
| A Challenge button as a 4px engraved rectangle in an opponent row. | A pill Challenge button beside square rows. |
| A diamond for a Track milestone and for the rail's current item. | A diamond as a bullet in a perk list. |
| The crown before a noble player's name. | A gilt ◆ before the name, the same mark as the rail's "you are here". |
| ±0 for an unchanged stat. | ◇ 0, which reads as a milestone still to come. |
| A rectangular count badge on the rail. | A round badge: the circle means presence. |
| The PageHeader's section icon in a gilt diamond. | The section icon in a Sigil's hexagon. |
| Perks listed with an en dash. | Perks listed with ✦. |
| A square Panel, told apart by its fill. | A Panel with 16px rounded corners. |

## Related components

- Sigil — the hex stat badge
- Track — the milestone track
- NavRail — the main navigation
- PageHeader — the information screen heading
- ItemSlot — the item frame
- Presence — the online status
- Button — the command button
- CurrencyPill — the currency amount
- Tag — the status label
- Delta — the change
- Emblem — the attribute sign
- Constellation — the star chart
