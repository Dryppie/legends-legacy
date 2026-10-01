# ItemLink

An item named in text.

**Status:** Draft

An item named inline in text — chat, loot lines, quest rewards — as a bracketed name in its rarity colour.

**Provide:** `children` (the item name), `rarity`, optional `meta` for the tooltip and `onClick` (open the item's detail or tooltip).

- Colour is the `rarity-*` token; weight 600. Rarity also shows in the tooltip, so the colour never carries it alone.
- Keep links to item names; never link a whole sentence.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `button` when it opens the item, else text; the rarity is read after the name ("Ember Fang, Epic") |
| Keyboard | A button: Enter or Space |
| Focus | `focus-ring` |
| Announced | The name and its rarity by name |
| Hover and tap | The rarity and meta are also in its `title`, and the rarity is read out, so nothing is hover-only |
| Target size | Inline text, exempt; where it is the only way to the item, give it a separate target |
| Text scaling | Wraps with its sentence |
| Colour | The brackets and the spoken rarity carry what the colour says |
| Motion | Nothing |
