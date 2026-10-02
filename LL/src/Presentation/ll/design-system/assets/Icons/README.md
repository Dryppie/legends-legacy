# Icons

The game's sidebar icon set: 24×24 grid, stroke 1.6 (combat-styles 1.75), round caps and joins. Each file's stroke is a baked gold gradient (#FEEFD0 → #FCD587) for dark grounds. In components use `<lg-icon>` with the file name (without `.svg`) as its `name`; it draws the same paths in `currentColor` from `icons.json` (D-125). These files are copies of the game's own sidebar files (`src/assets/icons/sidebar/`), shown here as assets; nothing in the design system draws from them, and they retire with the old sidebar.

- `overview`, `inventory`, `essences`, `combat-styles`, `achievements`, `soulstones` — Character
- `world-map`, `legacy-ascension` (World Tower), `quest-journal` (Quests), `prophecies` — World
- `guild`, `colosseum`, `cinder-bazaar`, `leaderboard` — City
- `settings` — System

This geometry is the construction standard for every future icon: a 24-unit grid, a 20-unit live area, a 1.6 stroke, round caps and joins, `currentColor`, no gradient (Foundations · Iconography · Construction). New icons are added to `icons.json`, the set `<lg-icon>` draws, not as files with a baked colour. The game's other icon files — equipment slots, toasts, filters, back and info — are off the standard and are listed for redrawing in Foundations · Iconography · Inventory.
