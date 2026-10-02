# Rarity

The rarity mark.

**Status:** Draft

An item's rarity as the game prints it: the code (C, UC, R, E, U, L, LG) in the rarity's hue, and its name — read by screen readers after a pause (", Epic") and shown in the tip on hover. Rarity is never colour alone (Foundations · Colour, Registries · Rarity): the code is the shape, the hue the colour, the name the words.

**Provide:** `rarity` (Common … Legacy). `tip` (default true) shows the name in the tip on hover; set it false where the mark sits in a control whose own tip matters more (a blocked ItemSlot). `nameId` puts an id on the spoken name, for a control's `aria-labelledby` (a List row's action).

```html
<span lgItemLink rarity="Epic">Ember Fang</span> <lg-rarity rarity="Epic" />
```

- Type: `code` at weight 700 with `tracking-code`, in `rarity-*`. The line height is the container's, so the mark sits in a row's line or an ItemSlot's corner chip alike.
- The seven hues are the game's own and never change (the `hue-rarity-*` primitives, aliased by `rarity-*`).
- Used by ItemSlot (its corner code) and the List row (after the name). ItemLink names the rarity in its own words and needs no mark.
- It lives with the primitives, not the game parts: a List, a generic component, shows it too, and a tier imports only the tiers before it (D-139).

## When to use

- Beside an item's name wherever the hue alone would carry the rarity: rows, cards, a Folio's header.

## When not to use

- A rarity spelled out as a word in a status position: a Tag with the rarity's tone. An item named in a sentence: ItemLink.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | Text: the code is hidden from screen readers, which hear ", Epic" instead |
| Keyboard | Not focusable; the name is in the words, so nothing depends on the tip |
| Focus | — |
| Announced | The rarity by name, after the item's name |
| Hover and tap | The name in the tip on hover, a convenience for sighted players learning the codes; it adds no description |
| Target size | — |
| Text scaling | Scales with the text; never truncates |
| Colour | The code and the spoken name carry what the hue says |
| Motion | Nothing |

## Related components

- ItemSlot — the item frame
- List — the list and its rows
- ItemLink — an item named in text
- Tag — the status label
