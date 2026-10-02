# Icon

The game's icon set.

**Status:** Draft

The game's own sidebar icons, redrawn to inherit `currentColor` so they take any ink token — and the standard every future icon is drawn to (Foundations · Iconography).

**Provide:** `name` (one of `LL.Icon.names`), `size` (16, 20 or 24; 20 by default; 12 only for a marker) and, for a standalone icon that means something, a `title`.

- **The standard:** a 24-unit grid with a 20-unit live area, a 1.6 stroke, round caps and joins, no fill, in `currentColor` — the geometry of the SVGs in the Icons asset group, without their baked gold gradient. Two strays are queued for redrawing: `combat-styles` (1.75 stroke) and `quest-journal` (a 22-unit view box).
- **Sizes:** `icon-lg` 24px for display, `icon-md` 20px by default, `icon-sm` 16px inline and in Compact, and `icon-marker` 12px for solid markers only. Another size, or a line icon under 16px, logs a console warning.
- **With a label.** Icons accompany words; an icon alone is for a common action only (close, back, expand, menu, search, filter, sort, refresh, copy, link), with a tooltip and an accessible name (Foundations · Iconography · Icons and labels).
- **Colour** comes from the text beside it: `ink-muted`, `ink`, `gilt` for the current location only, or a status, damage or effect colour beside its word. Never a rarity colour, never two colours.
- **An unknown name draws nothing** and logs a warning; the label beside it is the fallback, so a registry entry whose icon isn't drawn yet loses nothing but the icon.
- `nobility` and `lock` are the two icons outside the sidebar set: the filled crown of the Nobility mark (Registries · Nobility mark) and the Locked marker (D-113), drawn solid so they hold at 12px. They are the model for future markers, such as favourite.
- **Where the drawings live:** `icons.json`, the one source of the set. Add or change an icon there and run `scripts/build-icons.mjs`, which writes it into `bundle.js`, `index.d.ts` and the game's `grimoire-icons.ts` (D-125).

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | Decorative by default (`aria-hidden`), since the label beside it names the thing. With `title`, an image named by it: an icon-only control names its button instead |
| Keyboard | Not focusable; the control it sits in is |
| Focus | Nothing of its own |
| Announced | Nothing, or its `title` |
| Hover and tap | Nothing; an icon-only control shows its tooltip |
| Target size | Not a target. An icon-only control is at least 24px, and 32px or more in a row |
| Text scaling | The sizes are in rem, so icons grow with the reading-size setting |
| Colour | `currentColor`; meaning is in the label beside it, never in the icon's colour alone |
| Motion | Nothing |

## Related components

- NavRail — the main navigation
- Button — the command button
- Tag — the status label
- ItemSlot — the item frame
- PageHeader — the information screen heading
- Emblem — the attribute sign
