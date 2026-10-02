# ItemSlot

The item frame.

**Status:** Draft

A framed square for an item, essence or equipment slot, edged in its rarity colour. The square is the slot's shape and nothing else's (Foundations · Shape); its corners are `radius-control`.

**Provide:** `name`, `meta`, `rarity` (Common … Legacy), `image` or `icon`, optional `quantity`, `selected`, `size` (`sm`, `md`, `wide` for 16:9 profession cards) and `interactive` with `(activate)`. For a state, `state` (and `reason`, `shortfall` or `remaining` when it blocks), `favourite` and `ready`.

- The frame is `ground-deep` with a 1px inset edge in the `rarity-*` token; the name is set in the same rarity colour; the game's rarity code (C, UC, R, E, U, L, LG) sits in the corner in `code` (11px bold capitals) so rarity never depends on colour alone. The name is `body-compact-strong`, the quantity `numeral-compact`.
- Empty slots show a dashed inner frame and the `slotLabel`. An empty equipment slot also takes its slot's icon (`slot-head` … `slot-off-hand`) at 24px in `ink-muted`; until the icon is drawn, the label alone is its fallback — never a placeholder (Registries · Equipment slots, Foundations · Iconography).
- A filled slot shows its item, never the slot icon.
- **Marks keep fixed corners** (Standards · State combinations, D-095): the rarity code top start; the attention diamond top end; the ownership mark bottom start; the quantity bottom end. Each is `icon-marker` (12px) across, `space-1` in, on a 2px `ground` ring so it reads over art. A mark never moves into a corner another has left empty.
  - **Attention:** `ready` draws the `arcana-glow` diamond — true, or the words ("Upgrade available"), which join the accessible name. The Claimable state draws it too. A blocked or undiscovered slot takes none.
  - **Ownership:** Equipped and Attuned draw the in-use square, in `ink` — the square is the slot's shape (Foundations · Shape), so a small one says "in a slot". Otherwise a favourite item takes the 12px solid ribbon once it is drawn; until then, or when the square holds the corner, the word "Favourite" goes in the meta line. Don't pass Equipped in the equipment grid: every slot there is equipped.
  - The `lock` marker will join the Locked state in the caption, not a corner.
- **States** (Standards · States). A word state — `equipped`, `attuned`, `assigned`, `captured`, `listed`, `escrow`, `borrowed`, `new`, `claimable`… — leads the meta line ("Equipped · Main hand") and the accessible name. `not-owned` fades the art to `opacity-unowned` and the name to `ink-muted`, and says "Not owned". `undiscovered` withholds the art and the name and says "Undiscovered", with any hint in `meta`.
- **Blocked slots** — `locked`, `unavailable`, `restricted`, `insufficient`, `cooldown` — print their reason under the name ("Locked" / "Unlocks at level 20", "Ready in 12s"), which is also the description. A locked slot's frame is dashed and its name `ink-disabled`. With `interactive` it stays a focusable button (`aria-disabled`): no hover, no press, and a press announces the reason. With no caption, the reason opens in the reason tip.
- Hover (with `interactive`) is the neutral `surface-raised` wash around the frame and caption.
- `selected` adds a solid 2px `arcana-glow` ring — the slot's one lit state, drawn flat.
- **Rarity never glows** (Foundations · Ornament · Glow). It is carried by the rarity edge, the name's colour and the rarity code, and nothing else: no halo, bloom, shimmer or pulse, not even for a Legendary or Legacy item.
- **No ornament:** no corners, gilt frame, parchment or texture on the slot or behind it. A slot is a bounded object with its rarity edge (Foundations · Lines).

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | Edge: the rarity edge; the rarity code | The name and meta | "Soul Prism, Epic" |
| Hover (with `interactive`) | Fill: the `surface-raised` wash | — | Nothing |
| Focus-visible | Edge: `focus-ring` | — | The name and state |
| Selected | Edge: a 2px `arcana-glow` ring | — | "pressed" |
| Empty | Edge: a dashed inner frame | The `slotLabel`: "Off-hand" | "Off-hand" |
| Locked | Edge: a dashed frame; Text: the name in `ink-disabled`, "Locked" and the condition | "Locked", "Unlocks at level 20" | "Bag slot 9, button, unavailable. Locked. Unlocks at level 20." |
| Unavailable, Restricted, Insufficient, On cooldown | Text: the name in `ink-muted`, the reason under it | "Inventory full", "Short by 250 Cinders", "Ready in 12s" | "…, unavailable. Ready in 12 seconds." |
| Not owned | Opacity: the art at `opacity-unowned`; Text: `ink-muted` | "Not owned" | "Crown of Ash, Epic, not owned" |
| Undiscovered | Fill: the empty frame, no art | "Undiscovered" | "Undiscovered" |
| Equipped, Attuned, Assigned, Captured, Listed, In escrow, Borrowed | Text: the word leads the meta line | "Equipped · Main hand", "Listed · 1,200 Cinders" | "…, equipped" |
| Equipped, Attuned (also) | Marker: the in-use square, bottom start | The word leads the meta line | "…, equipped" |
| Favourite | Marker: the 12px ribbon, bottom start, once drawn and when there is no in-use square; otherwise the word | "Favourite" | "…, favourite" |
| Ready, Claimable | Marker: the `arcana-glow` diamond, top end | `ready`'s words, in the name and the Folio | "…, upgrade available" |

Selection is still the `arcana-glow` ring, which shares its form with an Uncommon edge — a known exception queued in Foundations · Colour · Not yet on the allocation.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `button` when it opens something, named by the item's name, rarity, "quantity 3", its state word, `ready`'s words and "favourite"; otherwise an image frame with text. A blocked slot is `aria-disabled`, described by its reason |
| Keyboard | A button: Enter or Space |
| Focus | `focus-ring` |
| Announced | The rarity by name ("Epic"), never just the code |
| Hover and tap | The wash, with `interactive`. The code's rarity is also in its `title`; nothing else is hover-only, and a blocked slot's reason is printed (or, without a caption, opens in the reason tip) |
| Target size | 64, 112 or 176px |
| Text scaling | The frame grows with the text; names truncate (Audit item 8) |
| Colour | The rarity edge always comes with the code; the marks differ by shape and place, and keep their shape in forced colours |
| Motion | Nothing: no rarity animation |
