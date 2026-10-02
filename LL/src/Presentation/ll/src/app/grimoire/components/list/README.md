# List

The list and its rows.

**Status:** Draft

One row of a list of similar things — an inventory item, a ranked player, a guild member, a sell order, a creature on the stage — with its columns lined up against every other row. Rows (`<li lgListRow>`) go inside a List (`<lg-list>`), which shares its columns with them. The `scene` variant is the browsable name list over stage art (it was EntryList, D-138).

**Provide:** on each row, `name`, and any of `key`, `rarity`, `icon` or `image`, `meta`, projected `lg-tag`s, `quantity`, `amount`, `live`, `muted`, `state` with `reason`, `ready` and `density`. A row that does something gets one action, a native control covering the row: `<button lgListRowAction (click)="…"></button>` (or `<a lgListRowAction routerLink="…">`). What sits at the row's end goes in `<lg-list-row-trailing>`. On the List: `label`, `density`, `rhythm`, `variant`, `selectable`, the selection as `[(selected)]` (the selected row's `key`, else its `name`) and, for a scene list, `fade`.

```html
<lg-list label="Inventory" [(selected)]="selectedId">
  @for (item of items; track item.id) {
    <li lgListRow [key]="item.id" [name]="item.name" [rarity]="item.rarity" [icon]="item.icon" [quantity]="item.count" [amount]="item.price | lgNumber">
      <button lgListRowAction></button>
      @if (item.equipped) {<lg-tag>Equipped</lg-tag>}
      <lg-list-row-trailing><button lgButton size="sm" (click)="sell(item)">Sell</button></lg-list-row-trailing>
    </li>
  }
</lg-list>
```

## When to use

- Lists players scan and compare: the inventory, the Leaderboard, guild members, Bazaar orders, recent trades.
- `variant="scene"`: a tall list of names browsed one at a time over stage art — creatures, prophecies, a roster — with the selection shown in the Folio beside it.

## When not to use

- Label and value pairs about one thing: use a Ledger. A square slot where position is the meaning: ItemSlot. A choice of two to five options in a row: Segmented (when it exists).

## Two kinds of list

| Kind | Markup | Role | Keys |
| --- | --- | --- | --- |
| Things to act on (the default) | Rows with a `button[lgListRowAction]`; anything else in `lg-list-row-trailing` | `list` of `li`s; the action is a `button` (or link) named by the row's name and rarity, `aria-pressed` when the List tracks a selection | One tab stop, on the selected row's action, else the last one focused, else the first: Up, Down, Home, End and a name's first letters move; Right moves into the trailing region and Left back; Enter and Space press the action. Trailing controls leave the tab order. A row without an action takes no part: its trailing controls are ordinary tab stops |
| A choice (`selectable`) | Rows with no action and no controls | `listbox` of `option`s with `aria-selected` | One tab stop: Up and Down move and select, wrapping; Home, End and typeahead too. A blocked row is reached but never selected; Enter or Space on it shows its reason |

Pressing a row's action selects it when the List tracks a selection (`[(selected)]` bound); its own `(click)` runs as well. Without `[(selected)]` the List tracks none and the action does only what its `(click)` says.

## Anatomy

1. **Thumbnail** — the item's icon or art, a bounded object in a `border-hairline` frame edged in its `rarity-*` colour; 40, 32 or 24px, filling the row with 4px above and below.
2. **Name** — `name-row` (Marcellus 17px), in the rarity colour, followed by the rarity code (`code`), which screen readers hear as the rarity's name.
3. **Tags and meta** — Tags ("Equipped") after the name; `meta` in `caption` `ink-muted`: beside the name and truncating, or on its own line in Comfortable.
4. **Quantity** — ×3, in `ink-muted` numerals; "—" in a row with none when others have one.
5. **Amount** — right-aligned tabular numerals (`numeral-row`, or `numeral-compact` in Compact), a unit after it muted. Name the unit once, in a caption or a column header.
6. **Trailing** — one action (a `sm` Button) or a status (Presence), in `lg-list-row-trailing`.
7. **Attention** — `ready` draws the `arcana-glow` diamond at the row's end: true, or the words screen readers hear ("1 point to spend").

## Supported states

| State | Looks like | Tokens |
| --- | --- | --- |
| Default | A separator hairline between rows, or the List's `rhythm` | `line`, `row-stripe` |
| Hover (a row with an action, or an option) | Raised wash across the row | `surface-raised` |
| Focus | The focus ring round the whole row, above its neighbours | `focus-ring` |
| Selected | A 2px `ink` selection edge at the start and the raised wash: a shape, so it is right in item contexts, where rarity owns hue | `ink`, `border-emphasis`, `surface-raised` |
| Muted | Name and amount in `ink-muted` | `ink-muted` |
| Blocked (`state`: `locked`, `unavailable`, `restricted`) | The name in `ink-disabled` and the state's Tag in place of the row's own; the reason in the tip on hover and focus; a press pins it and says it again. Never selected; its action does not act | `ink-disabled` |
| Ready (`ready`) | The `arcana-glow` diamond at the row's end; a blocked row takes none, and nor does a row whose Tag already says it (Standards · State combinations) | `arcana-glow` |
| Changed (`live`) | The amount's `changed` wash, on at once, held for `duration-reveal`, faded over `duration-slow` | `changed` |
| Gone | A row whose item left while the list was held: `muted`, its action disabled, a word in `meta` ("Sold", "Left the guild") | `ink-muted` |

## The scene variant

`variant="scene"` is the browsable name list over stage art. Give it a height: it scrolls, and its ends fade (`fade`, on by default).

- Names are Marcellus in `name-header` (24 / 30) in `ink-muted` — a list read one name at a time over stage art, so it keeps the display face (D-030); the selected one is `ink` with the selection edge — a 2px `arcana-glow` bar inside its start — and the `surface-raised` wash. It was `gilt`, which is for the current location only (D-086).
- **Density:** Comfortable and Standard set 24px names on 48 or 40px rows; Compact sets `name-row` (17px Marcellus) on 32px rows, for a long roster. The list keeps 4px of inline padding, so a focused row's ring is never clipped. Rows stand 2px apart with nothing drawn between them.
- **Locked rows stay in reach** (Standards · States, D-087). The name is `ink-disabled` with a Locked Tag (dashed). Up and Down land on it and its unlock condition opens beside it in the tip; it is never selected — selection stays where it was — and Enter or a click pins the condition and announces it. Hover takes no wash.
- Tags ("+ New") sit after the name as a `new` Tag. One Tag per row: a locked row shows Locked in place of its own.
- Hover is the neutral `surface-raised` wash, fading over `duration-fast`, as on every row.
- Use it `selectable`, with `[(selected)]`: the selection follows focus.

## Row rhythm

`lg-list` takes one `rhythm` (Foundations · Lines):

| Rhythm | Draws | Use for |
| --- | --- | --- |
| `separators` (default) | A `line` hairline between rows | Lists read row by row: inventory, guild members, orders |
| `zebra` | Every second row on `row-stripe`, no separators | Wide rows read across: rankings, a combat log |
| `spacing` | Nothing; the row height sets them apart | Short lists of about five rows |

Never two at once, and never inside a bordered container: put the List in a `flush` Panel, which has no border. Hover and selection wash over the stripe. Zebra needs a surface under it (Level 1 or the Folio); on `ground` use separators. A scene list draws no rhythm.

## Density variants

| Variant | Input | Use in |
| --- | --- | --- |
| Comfortable | `density="comfortable"` | A short list in a detail view or dialog: 48px rows, meta on its own line |
| Standard | default | Lists in panels: 40px rows |
| Compact | `density="compact"` on the List or its container | Inventory, rankings, guild members, order books: 32px rows, one line |

## Content rules

- One thing per row, one line in Standard and Compact. Detail goes to the inspector: beside the list in `lg-split` on a Page, or the Folio on a stage screen (Foundations · Layout).
- Amounts formatted with the numeral pipes (`lgNumber`, `lgShort`, D-135); "—" for unknown (`null`), "0" for zero. Leave `amount` unset for a list with no amount column.
- A blocked row says why: Locked takes how it unlocks ("Clear Floor 10 to unlock").
- A selectable row holds no controls: an option's content is read as one name. A row that needs a control is in a list of things to act on.

## Live lists

A Bazaar list or a guild roster refreshes while the player uses it, and must never move the row under the pointer (Foundations · Motion · Live updates). Feed the rows through the `[lgLiveList]` directive on the List's container (`[lgLiveList]="items" [keep]="selectedId" #live="lgLiveList"`) and draw `live.rows()`:

- While the pointer is over the list or focus is inside it, rows keep their order. A row whose item has gone comes back with `gone: true`: show it `muted`, disable its action and say why in `meta`. New items wait, counted in `pending()`: show "2 new listings" as a `quiet` Button in the Panel head, where it takes no row's place — keep the slot's height when it is empty — and call `release()` from it.
- Set `live` on the rows, so a price or status that changed by itself takes the live-update mark.
- Key rows by the item's id, never by index, so the scroll position holds. The selection stays even if its item leaves (`keep`).

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `ul` with `role="list"`, or `listbox` when `selectable`, named by `label` (a selectable List needs one). A row's action is a `button` or link named by the row's name and rarity (`aria-labelledby`); with a tracked selection it carries `aria-pressed`. In a listbox each row is an `option` with `aria-selected` |
| Keyboard | See Two kinds of list. Built on the CDK's `FocusKeyManager` |
| Focus | The ring is drawn round the whole row, which rises above its neighbours; in forced colours, a system outline |
| Announced | The name, then the rarity by name ("Epic"), the Tags' words, the meta, "Quantity 3", the amount with its unit, and the ready words. A blocked row: "Ember Knight, unavailable. Locked. Clear Floor 10 to unlock." |
| Hover and tap | Hover washes the row; a blocked row opens its reason tip beside it on hover and focus, a tap pins it and Escape closes it |
| Target size | The action covers the row: 48, 40 or 32px |
| Text scaling | Meta truncates first, then the name; amounts and quantities never truncate. A scene list scrolls |
| Colour | Rarity also shows its code and is announced by name; selection is a shape (the bar); a blocked row also has its Tag |
| Motion | A `live` amount's mark fades over `duration-slow`; a scene row's hover wash over `duration-fast`. Rows never move while the list is in use, and arrive without sliding. Both go at once under reduced motion |

## Testing

`LgListHarness` and `LgListRowHarness` (`@grimoire/testing`): find rows by name, press keys where focus is, and read the selection, the tab stops (`{ row, on: 'row' | 'trailing' }`), blocked rows and a row's description.

## Related components

- Ledger — the labelled value list
- ItemSlot — the item frame
- Panel — the content box
- Presence — the online status
- Tabs — the tabs
