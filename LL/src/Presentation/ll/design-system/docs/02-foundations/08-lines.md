# Foundations · Lines

The game draws a thin frame around nearly everything, so a line can mean a group, a control or a selection, and the player has to guess which. In Grimoire each line has one meaning, carried by its colour, width and style. Most groups need no line at all: space and a heading set them apart. LinesSpecimen shows the rules on a guild member list, an inventory list, a Folio and a form.

## Rules

**Must**
- Draw every line as one of the five types below, with that type's colour, width and style.
- Edge whatever a player can press, type into or open in `line-strong`, which holds 3:1 on every level: inputs, outline Buttons, a CurrencyPill or LoadoutSlot that opens something. A `line` edge is decoration and is never the only sign of a control.
- Draw every line at `border-hairline` (1px) or `border-emphasis` (2px). Emphasis is for selection edges, and for Track diamonds; nothing at rest is 2px, and no line is 1.5 or 3px.
- Give each list or table one row rhythm: separators, zebra or spacing.

**Should**
- Set things apart with space first, a divider second, and a full border last. A full border is only for an interactive edge or a bounded object (The decision ladder, below).
- Let a region's fill carry its edge. A Panel, the JourneyCard and a list at Level 1 have no border in flow (Foundations · Surfaces & Layering).
- Keep SectionRule to its three variants and their jobs: `band` over data groups in stat columns, `hairline` for any other group, `ornament` between lore and effects.
- Keep the double gilt frame to the Folio and the Banner, and the dotted leader to Ledger-style label and value rows.

**Never**
- Put rows with separators inside a container with a border.
- Combine row separators and zebra rhythm in one list or table.
- Use the ornament twice on one surface, or between rows of data.
- Frame each row, tile or group in a box of its own.
- Show selection with a 1px line, or change a line's width on hover.

## Five line types

| Type | Means | Colour | Width | Style | Where |
| --- | --- | --- | --- | --- | --- |
| **Separation** | These are distinct but belong together | `line` | `border-hairline` | Solid | Between rows of a List or table; the Panel head's rule; the PageHeader's closing rule; the Folio footer's rule; the trailing rule after a Ledger title or rail group; SectionRule `hairline`; the docked Chronicle's rules |
| **Interactive edge** | You can press, type into or open this | `line-strong` (3:1) | `border-hairline` | Solid | Inputs and the chat composer; outline Buttons; a CurrencyPill that opens or toggles; a LoadoutSlot that opens; the separators between primary tabs |
| **Selection edge** | This is the one chosen | `arcana-glow`; `ink` in item rows and in chat, where verdigris stays out (Foundations · Colour · Allocation) | `border-emphasis` | Solid bar or ring | The active tab's bar; the current EntryList entry; the selected ItemSlot's ring (queued to become a shape, Foundations · Colour); the selected ListRow's start bar in `ink`; the active Chronicle channel; a Chronicle line that mentions you (chat has no selected line, so the bar cannot be misread) |
| **Decorative ornament** | Brand: this surface is the special one | `gilt` | `border-hairline` | Solid double frame, or the diamond-chain lattice | The double frame of the Folio and the Banner, with their corners; SectionRule `ornament` |
| **Dotted leader** | This label's value is at the end of the row | `line-strong` | `border-hairline` | Dotted | Ledger rows, and rows built like them: a label on the left, one value on the right |

A selection edge always comes with the `surface-raised` wash, or the tile fill for a tab, so it never relies on the line alone. Hover is the wash, never a line: a CurrencyPill or LoadoutSlot keeps its `line-strong` edge and takes `surface-raised` on hover. The Button still turns its edge `gilt` on hover; that is off the allocation and queued (Foundations · Colour · Not yet on the allocation).

### Edges that are not line types

- **Surface edges follow their level** (Foundations · Surfaces & Layering). In flow, Level 1 has no edge. A floating surface — the chat drawer, a popover, a tooltip, a toast — has a `line-strong` hairline, because it lies over content of any fill; Level 3 has the dark ring inside `shadow-panel`.
- **Bounded objects keep a full edge.** An ItemSlot, a ListRow thumbnail, a LoadoutSlot and an opponent's card in the Colosseum are each one thing the player picks, compares or equips. Their edge is the rarity colour when the object is an item, `line-strong` when it is a control, and `line` otherwise.
- **Empty and locked objects are dashed.** An empty ItemSlot shows a dashed inner frame; a locked ItemSlot, LoadoutSlot, Button or Tag has a dashed edge (Standards · States). Dashes mean "nothing here yet", and nothing else.
- **Marks are shapes, not lines:** key caps, Tags, Track diamonds, the Presence dot and a `bar` Meter's outline use the two widths but sit outside the decision ladder. Drawings — the Sigil, the Emblem and the Constellation's rings and ticks — keep their own stroke weights (Foundations · Shape).
- **The focus ring is not a line.** It is `focus-ring`, a `ground` gap and a 2px `focus` ring drawn as a shadow, never a border and never a selection (Foundations · Accessibility). In forced colours (Windows contrast themes) shadows drop, so focus and selection fall back to system outlines.

## The decision ladder

Set two things apart with the first of these that works, and stop there.

1. **Space.** Leave a wider gap between groups than inside them: `section-*` between groups, `stack-*` inside one (Foundations · Space & Density). A heading or a label names the group. Most groups need nothing more.
2. **A divider.** Add a separation hairline or a SectionRule when space alone would let two groups read as one: rows in a dense list, a Panel's head over its body, lore over an item's effects.
3. **A full border.** Use one only for an interactive edge (an input, an outline Button) or a bounded object (an ItemSlot, a LoadoutSlot, an opponent's card).

A region is not a bounded object. A Panel, the JourneyCard, the Combat Attributes section and the NavRail are told apart by their fill and the space around them, not by a frame.

## Dense lists and tables

| Rhythm | Draws | Use for | Set with |
| --- | --- | --- | --- |
| **Separators** (the default) | A `line` hairline between rows, never above the first or below the last | Lists read row by row: inventory, guild members, an order book | `List`; a table in `lg-tablewrap` |
| **Zebra** | Every second row on `row-stripe`, and no separators | Wide rows read across many columns: rankings, a combat log, a table of six columns or more | `List rhythm="zebra"`; `lg-tablewrap lg-tablewrap--zebra` |
| **Spacing** | Nothing: the row height does it | Short lists of about five rows or fewer, with room to breathe: a Folio's rewards, recent trades | `List rhythm="spacing"` |

- **One rhythm, never two.** Separators and zebra together draw two rhythms, and the eye reads neither.
- **No container border around rows.** A Panel has no border of its own, so a `flush` Panel's List keeps its separators and its rows run to the edge. Don't wrap a list in a bordered box to hold it.
- **The table's header rule stays.** One `line` hairline closes the header row and travels with it when it sticks. It divides the header from the body, so it is not a row separator, and it stays in zebra.
- **Hover and selection lie over the rhythm.** A hovered or selected row takes the `surface-raised` wash over its stripe; the selected row adds its 2px start bar.
- **Zebra needs a surface.** `row-stripe` is darker than `surface` and the Folio (1.06:1 and 1.05:1), so use it on Level 1 or in the Folio. On `ground` it all but disappears (1.02:1): use separators there.
- Rows never get boxes, and the table has no border around it.

## SectionRule

| Variant | Its job | Limit |
| --- | --- | --- |
| `band` | A `line` strip with a label over a group of values in a stat column: Status, Essence | Data groups only |
| `hairline` | A `line` rule beside an optional label and aside, for any other group | Only where space and a heading are not enough |
| `ornament` | The gilt diamond-chain lattice between lore and effects | At most once per surface. The Folio draws its own, so a Folio holds no other. Never between rows of data, and never in a Panel |

A surface is one surface at one level: a Folio, a Panel, a Banner, a dialog or the Page. A second ornament on the same surface logs a console warning.

## The gilt frame

- The double gilt frame — an outer hairline and an inner one 4px inside it, with the corner ornaments — belongs to the Folio and the Banner only.
- It is their one edge. The Banner has no border outside it; the Folio adds only its Level 3 shadow.
- Nothing else takes a gilt frame, single or double: not a Panel, a dialog, a card or the JourneyCard (D-055).

## Dotted leaders

- Leaders join a label to its value across a row: Ledger rows, and rows built the same way, such as a fee summary or a price list.
- They are a dotted `line-strong` hairline from the end of the label to the value, on the text's baseline.
- They never appear in a List or table with columns, in a menu, or between an item's name and its quantity. Those align in columns instead.

## In the game

### A guild member list

- **Do:** the members are one Compact List in a `flush` Panel titled "Guild members", with the count in the head. Rank is the row's meta, contribution its value and Presence the trailing column. Separators divide the rows. The Panel has no border, so its head's rule and the separators are the only lines.
- **Don't:** frame each member in a card, frame the list, and add separators too. That is three lines between two names. Don't show Online with a coloured border either: Presence is a dot and a word.

### An inventory list

- **Do:** the Bags are a Standard List. Each thumbnail is a bounded object with its rarity edge, and separators divide the rows. The selected row takes the `surface-raised` wash and a 2px `ink` start bar. On a wide inventory table with many columns, use zebra instead of separators.
- **Don't:** outline the selected row with a 1px gilt border, box each row, or stripe the rows and separate them as well.

### A Folio

- **Do:** the double gilt frame is the Folio's only edge. One ornament sits between the lore and the effects. The stats are a Ledger, with dotted leaders and no box around it, and the footer's rule sets off the Track.
- **Don't:** put an ornament above each group, a Panel inside the Folio, or a bordered box around the stats or the effects.

### A form

- **Do:** listing an item on the Cinder Bazaar takes three inputs — quantity, price each and duration. Each input has a `line-strong` edge, and a label above it. The groups (the item, the price, the fees) are set apart by space and a heading. The fees are Ledger-style rows with leaders, and the committing action is the solid Button.
- **Don't:** put a fieldset border around each group, edge the inputs in `line`, or border the whole form inside a bordered Panel.

## Tokens used

| Token | Role here |
| --- | --- |
| `line` | Separation; bounded objects that are not controls; the band's fill |
| `line-strong` | Interactive edges, floating surfaces, dotted leaders, marks |
| `arcana-glow`, `ink` | Selection edges |
| `gilt` | The double frame, its corners and the ornament |
| `row-stripe` | Zebra rows |
| `surface-raised` | The hover and selection washes that lie over the rhythm |
| `border-hairline` | Every line at rest |
| `border-emphasis` | Selection edges and Track diamonds |

## Do and don't

| Do | Don't |
| --- | --- |
| Edge a text input in `line-strong`. | Edge it in `line`, which is too faint to find. |
| Separate a guild's members with hairlines in a Panel with no border. | Frame each member in a card inside a framed list. |
| Stripe a wide rankings table, with no separators. | Stripe it and separate its rows as well. |
| Mark the selected inventory row with the wash and a 2px `ink` bar. | Outline it with a 1px gilt border. |
| One ornament in the Folio, between lore and effects. | An ornament above every group in the Folio. |
| Put a `band` SectionRule ("Status") above a group of stat tiles. | Put the ornament between stat groups. |
| Join a Ledger's labels to their values with dotted leaders. | Put leaders between an item's name and its quantity in a List. |
| Group a form's fields with space and a heading. | Put a fieldset border around each group. |
| A Banner edged only by its double gilt frame. | A `line` border around the gilt frame. |

## Related components

- SectionRule — the dividers
- Panel — the content box
- Ledger — the labelled value list
- List and ListRow — the list and its row
- Folio — the detail panel
- Banner — the headline block
- ItemSlot — the item frame
- LoadoutSlot — an Essence loadout slot
- CurrencyPill — the currency amount
- Button — the command button
- SearchField — search with suggestions
- TabStrip — the tabs
