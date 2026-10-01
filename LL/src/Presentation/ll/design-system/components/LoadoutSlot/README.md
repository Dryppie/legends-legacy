# LoadoutSlot

An Essence loadout slot.

**Status:** Draft

One Essence loadout slot: the Essence, its rarity and its two abilities — or an open or locked slot.

**Provide:** `index` (zero-based), `state` (`attuned`, `open`, `locked`), `name`, `rarity`, `active` (`{ name, cooldown }`), `passive` (`{ name }`), `icon` or `image`, `reason` for locked slots — how it unlocks, "Unlocks at level 20" (`unlockLabel` is the older name) — `hint` for open ones and `onClick` to open the Essence preview.

- Attuned (Standards · States · Ownership and use): rarity-edged ItemSlot, the name in `name-row` (Marcellus 17px) in its `rarity-*` colour, ability labels in `label` and ability names in `caption`, and an Attuned Tag in `neutral` — an attuned Essence is not waiting for the player, so it takes no arcana, and in an item context only rarity takes a hue (D-087).
- Open (Data · Empty): the empty ItemSlot frame and "Empty" in `ink-muted`, with the `hint` saying what fills it. No Tag: "Empty" says it.
- Locked (Availability · Locked): a dashed frame, a Locked Tag (dashed) and the unlock condition printed as the name — so players see what is coming. With `onClick` it stays a focusable button (`aria-disabled`): the printed condition is part of its name, and a press announces it instead of opening anything.
- It is an inner surface, so it pads with its region's inner inset (12px in a Standard Panel), one step less than the Panel around it (Foundations · Space & Density).
- It is a well and a bounded object: opaque `ground` with a full edge, one enclosed level inside its Panel (Foundations · Surfaces & Layering). A slot that opens something (`onClick`) is a control, so its edge is `line-strong` and hover is the `surface-raised` wash; otherwise the edge is `line`. Locked slots are dashed (Foundations · Lines).
- Stack slots in a column with `stack-sm` gaps.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default (Attuned) | Edge: the rarity edge; Text: an Attuned Tag | "Attuned", the Essence and its abilities | "Slot 1 Attuned Ember Wolf Essence Active Cinder Bite · 12s…" |
| Hover (with `onClick`) | Fill: the `surface-raised` wash | — | Nothing |
| Focus-visible | Edge: `focus-ring` | — | Its contents |
| Empty (Open) | Edge: the slot's dashed inner frame | "Empty", and the hint | "Slot 2 Empty Choose an Essence to attune" |
| Locked | Edge: a dashed frame; Text: a Locked Tag and the condition | "Locked", "Unlocks at level 20" | "Slot 3 Locked Unlocks at level 20, button, unavailable" |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `button` with `onClick`, named by its contents; otherwise a group of text. A locked slot is `aria-disabled`; its printed condition is in its name |
| Keyboard | Enter or Space opens the Essence preview; on a locked slot it announces the condition |
| Focus | `focus-ring` |
| Announced | The slot, its state and its Essence; a locked slot's condition |
| Hover and tap | The `surface-raised` wash on an attuned or open slot with `onClick`; nothing on a locked one |
| Target size | The whole slot, at least 64px tall |
| Text scaling | The name truncates; the body grows |
| Colour | Rarity also by its code; locked also by its dashes and words |
| Motion | Nothing |

