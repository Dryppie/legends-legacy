# LoadoutSlot

An Essence loadout slot.

**Status:** Draft

One Essence loadout slot: the Essence, its rarity and its two abilities — or an open or locked slot.

**Provide:** `index` (zero-based), `state` (`attuned`, `open`, `locked`), `name`, `rarity`, `active` (`{ name, cooldown }`), `passive` (`{ name }`), `icon` or `image`, `reason` for locked slots — how it unlocks, "Unlocks at level 20" (`unlockLabel` is the older name) — `hint` for open ones, `ready` when something waits for the player, `compact` for a full loadout, and `onClick` to open the Essence preview.

- Attuned (Standards · States · Ownership and use): rarity-edged ItemSlot, the name in `name-row` (Marcellus 17px) in its `rarity-*` colour, ability labels in `label` and ability names in `caption`, and an Attuned Tag in `neutral` — an attuned Essence is not waiting for the player, so it takes no arcana, and in an item context only rarity takes a hue (D-087).
- Open (Data · Empty): the empty ItemSlot frame and "Empty" in `ink-muted`, with the `hint` saying what fills it. No Tag: "Empty" says it.
- Locked (Availability · Locked): a dashed frame, a Locked Tag (dashed) and the unlock condition printed as the name — so players see what is coming. With `onClick` it stays a focusable button (`aria-disabled`): the printed condition is part of its name, and a press announces it instead of opening anything.
- It is an inner surface, so it pads with its region's inner inset (12px in a Standard Panel), one step less than the Panel around it (Foundations · Space & Density).
- It is a well and a bounded object: opaque `ground` with a full edge, one enclosed level inside its Panel (Foundations · Surfaces & Layering). A slot that opens something (`onClick`) is a control, so its edge is `line-strong` and hover is the `surface-raised` wash; otherwise the edge is `line`. Locked slots are dashed (Foundations · Lines).
- **Attention:** `ready` draws the `arcana-glow` diamond at the end of the head, after the Tag — an open slot with an Essence to attune, say. True, or the words ("Essence ready to attune"). A locked slot takes none (Standards · State combinations).
- **Compact** (D-103), for a full loadout — the Character Overview's ten slots: one short row. A 40px frame, the name leading the head, and the abilities on one line that wraps. The slot number is for screen readers only, and an attuned slot drops its Attuned Tag: being in the loadout says it. An open slot says "Empty"; a locked slot keeps its Locked Tag and condition, which are news. Use the default where one slot is the subject — the Essences screen, a Folio.
- Stack slots in a column with `stack-sm` gaps.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default (Attuned) | Edge: the rarity edge; Text: an Attuned Tag (none when compact) | "Attuned", the Essence and its abilities | "Slot 1 Attuned Ember Wolf Essence Active Cinder Bite · 12s…"; compact, "Slot 1 Ember Wolf Essence Active…" |
| Hover (with `onClick`) | Fill: the `surface-raised` wash | — | Nothing |
| Focus-visible | Edge: `focus-ring` | — | Its contents |
| Empty (Open) | Edge: the slot's dashed inner frame | "Empty", and the hint | "Slot 2 Empty Choose an Essence to attune" |
| Ready | Marker: the `arcana-glow` diamond at the head's end | `ready`'s words, in the name | "Slot 2 Essence ready to attune Empty…" |
| Locked | Edge: a dashed frame; Text: a Locked Tag and the condition | "Locked", "Unlocks at level 20" | "Slot 3 Locked Unlocks at level 20, button, unavailable" |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `button` with `onClick`, named by its contents; otherwise a group of text. A locked slot is `aria-disabled`; its printed condition is in its name |
| Keyboard | Enter or Space opens the Essence preview; on a locked slot it announces the condition |
| Focus | `focus-ring` |
| Announced | The slot, its state and its Essence; a locked slot's condition |
| Hover and tap | The `surface-raised` wash on an attuned or open slot with `onClick`; nothing on a locked one |
| Target size | The whole slot, at least 64px tall; compact, at least 44px — an attuned compact slot is 48px, so a full loadout of ten fits a side column (D-123) |
| Text scaling | The name truncates; the body grows |
| Colour | Rarity also by its code; locked also by its dashes and words |
| Motion | Nothing |

