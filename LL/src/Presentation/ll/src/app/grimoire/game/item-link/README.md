# ItemLink

An item named in text.

**Status:** Draft

An item named inline in text — chat, loot lines, quest rewards — as a bracketed name in its rarity colour.

**Provide:** the host — `<button lgItemLink>` to open the item's detail (a press is the native `(click)`), `<a lgItemLink>` to go to it, `<span lgItemLink>` to name it — the item name as content, `rarity`, and optional `meta` for the tip.

```html
You found <button lgItemLink rarity="Epic" meta="Sword" (click)="inspect(item)">Ember Fang</button>.
```

- Colour is the `rarity-*` token; weight 600. The rarity and meta show in the tip on hover and focus, and the rarity is read after the name, so the colour never carries it alone.
- Keep links to item names; never link a whole sentence.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `button` when it opens the item, else text; the rarity is read after the name ("Ember Fang, Epic") |
| Keyboard | A button: Enter or Space |
| Focus | `focus-ring` |
| Announced | The name and its rarity by name |
| Hover and tap | The rarity and meta are also in the tip, on hover and on keyboard focus; the meta is the description, and the rarity is read out, so nothing is hover-only |
| Target size | Inline text, exempt; where it is the only way to the item, give it a separate target |
| Text scaling | Wraps with its sentence |
| Colour | The brackets and the spoken rarity carry what the colour says |
| Motion | Nothing |
