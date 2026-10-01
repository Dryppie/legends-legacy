# ListRow

The list row.

**Status:** Draft

One row of a list of similar things — an inventory item, a ranked player, a guild member, a sell order — with its columns lined up against every other row. Rows go inside a `List`, which shares its columns with them.

**Provide:** `title`, and any of `rarity`, `icon` / `image` / `thumb`, `meta`, `tags`, `quantity`, `value`, `live`, `trailing`, `selected`, `muted`, `onClick` and `density`. Wrap rows in `List` (`label`, `density`, `rhythm`).

## When to use

- Lists players scan and compare: the inventory, the Leaderboard, guild members, Bazaar orders, recent trades.

## When not to use

- Label and value pairs about one thing: use a Ledger. A browsable list of scene names over stage art: EntryList. A square slot where position is the meaning: ItemSlot.

## Anatomy

1. **Thumbnail** — the item's icon or art, a bounded object in a `border-hairline` frame edged in its `rarity-*` colour; 40, 32 or 24px, filling the row with 4px above and below.
2. **Name** — `name-row` (Marcellus 17px), in the rarity colour, followed by the rarity code (`code`), which screen readers hear as the rarity's name.
3. **Tags and meta** — Tags ("Equipped") after the name; `meta` in `caption` `ink-muted`: beside the name and truncating, or on its own line in Comfortable.
4. **Quantity** — ×3, in `ink-muted` numerals; "—" in a row with none when others have one.
5. **Value** — right-aligned tabular numerals (`numeral-row`, or `numeral-compact` in Compact), a unit after it muted. Name the unit once, in a caption or a column header.
6. **Trailing** — one action (a `sm` Button) or a status (Presence).

## Supported states

| State | Looks like | Tokens |
| --- | --- | --- |
| Default | A separator hairline between rows, or the List's `rhythm` | `line`, `row-stripe` |
| Hover (with `onClick`) | Raised wash across the row | `surface-raised` |
| Focus | The focus ring round the whole row, above its neighbours | `focus-ring` |
| Selected | A 2px `ink` selection edge at the start and the raised wash: a shape, so it is right in item contexts, where rarity owns hue | `ink`, `border-emphasis`, `surface-raised` |
| Muted | Name and value in `ink-muted` | `ink-muted` |
| Changed (`live`) | The value's `changed` wash, on at once, held for `duration-reveal`, faded over `duration-slow` | `changed` |
| Gone | A row whose item left while the list was held: `muted`, its action disabled, a word in `meta` ("Sold", "Left the guild") | `ink-muted` |

## Row rhythm

`List` takes one `rhythm` (Foundations · Lines):

| Rhythm | Draws | Use for |
| --- | --- | --- |
| `separators` (default) | A `line` hairline between rows | Lists read row by row: inventory, guild members, orders |
| `zebra` | Every second row on `row-stripe`, no separators | Wide rows read across: rankings, a combat log |
| `spacing` | Nothing; the row height sets them apart | Short lists of about five rows |

Never two at once, and never inside a bordered container: put the List in a `flush` Panel, which has no border. Hover and selection wash over the stripe. Zebra needs a surface under it (Level 1 or the Folio); on `ground` use separators.

## Density variants

| Variant | Prop | Use in |
| --- | --- | --- |
| Comfortable | `density="comfortable"` | A short list in a detail view or dialog: 48px rows, meta on its own line |
| Standard | default | Lists in panels: 40px rows |
| Compact | `density="compact"` on the List or its container | Inventory, rankings, guild members, order books: 32px rows, one line |

## Content rules

- One thing per row, one line in Standard and Compact. Detail goes to the inspector: beside the list in `lg-split` on a Page, or the Folio on a stage screen (Foundations · Layout).
- Values formatted with `LL.format`; "—" for unknown, "0" for zero.

## Live lists

A Bazaar list or a guild roster refreshes while the player uses it, and must never move the row under the pointer (Foundations · Motion · Live updates). Feed the rows through `LL.motion.useLiveList(items, { keep: selectedId })` and put its `ref` on the List's container:

- While the pointer is over the list or focus is inside it, rows keep their order. A row whose item has gone comes back with `gone: true`: show it `muted`, disable its action and say why in `meta`. New items wait, counted in `pending`: show "2 new listings" as a `quiet` Button in the Panel head's `aside`, where it takes no row's place — keep the slot's height when it is empty — and call `release()` from it.
- Set `live` on the rows, so a price or status that changed by itself takes the live-update mark.
- Key rows by the item's id, never by index, so the scroll position holds. The selection stays even if its item leaves (`keep`).

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `li` in a `role="list"`; with `onClick` the name is a `button` named by the name and rarity, and `selected` sets `aria-pressed` |
| Keyboard | The List is one tab stop (the selected row, else the first): Up, Down, Home and End move; Right moves into the trailing action and Left back; Enter or Space opens the row. Trailing actions leave the tab order |
| Focus | The ring is drawn round the whole row, which rises above its neighbours; in forced colours, a system outline |
| Announced | The name, then the rarity by name ("Epic"), the meta, "Quantity 3", the value with its unit; a Tag's words |
| Hover and tap | Nothing: hover only washes the row |
| Target size | The name's hit area covers the row: 48, 40 or 32px |
| Text scaling | Meta truncates first, then the name; values and quantities never truncate |
| Colour | Rarity also shows its code and is announced by name; selection is a shape (the `ink` bar) |
| Motion | A `live` value's mark fades over `duration-slow`; rows themselves never move while the list is in use, and arrive without sliding. The mark goes at once under reduced motion |

## Related components

- Ledger — the labelled value list
- EntryList — the browsable name list
- ItemSlot — the item frame
- Panel — the content box
- Presence — the online status
