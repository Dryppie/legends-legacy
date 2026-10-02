<!-- Imported from the Claude Design artifact (D-090). Files live in assets/Icons/; keep this page in step with them. -->
# Assets · Icons

15 files under `assets/Icons/`, stored as files in this repository. Reference one by its relative path (from a component preview: `../../assets/Icons/<file>`). Copy assets, never approximate them.

These are the game's own sidebar files, with a baked gold gradient. `LL.Icon` and every `icon=` prop draw the same shapes in `currentColor` from `icons.json` (D-125) — mount the component wherever the bundle runs; the files are for surfaces without it (a deck).

## Use

```html
<img src="../../assets/Icons/overview.svg" alt="…">
```

## Usage notes

# Icons

The game's sidebar icon set: 24×24 grid, stroke 1.6 (combat-styles 1.75), round caps and joins. Each file's stroke is a baked gold gradient (#FEEFD0 → #FCD587) for dark grounds. In components use `LL.Icon` with the file name (without `.svg`), which draws the same paths in `currentColor` from `icons.json` (D-125). These files are copies of the game's own sidebar files (`src/assets/icons/sidebar/`), shown here as assets; nothing in the design system draws from them, and they retire with the old sidebar.

- `overview`, `inventory`, `essences`, `combat-styles`, `achievements`, `soulstones` — Character
- `world-map`, `legacy-ascension` (World Tower), `quest-journal` (Quests), `prophecies` — World
- `guild`, `colosseum`, `cinder-bazaar`, `leaderboard` — City
- `settings` — System

This geometry is the construction standard for every future icon: a 24-unit grid, a 20-unit live area, a 1.6 stroke, round caps and joins, `currentColor`, no gradient (Foundations · Iconography · Construction). New icons are added to `icons.json`, the set `LL.Icon` draws, not as files with a baked colour. The game's other icon files — equipment slots, toasts, filters, back and info — are off the standard and are listed for redrawing in Foundations · Iconography · Inventory.

## Files

| file | open |
| --- | --- |
| `assets/Icons/overview.svg` | [overview.svg](../../assets/Icons/overview.svg) |
| `assets/Icons/inventory.svg` | [inventory.svg](../../assets/Icons/inventory.svg) |
| `assets/Icons/essences.svg` | [essences.svg](../../assets/Icons/essences.svg) |
| `assets/Icons/combat-styles.svg` | [combat-styles.svg](../../assets/Icons/combat-styles.svg) |
| `assets/Icons/achievements.svg` | [achievements.svg](../../assets/Icons/achievements.svg) |
| `assets/Icons/soulstones.svg` | [soulstones.svg](../../assets/Icons/soulstones.svg) |
| `assets/Icons/world-map.svg` | [world-map.svg](../../assets/Icons/world-map.svg) |
| `assets/Icons/legacy-ascension.svg` | [legacy-ascension.svg](../../assets/Icons/legacy-ascension.svg) |
| `assets/Icons/quest-journal.svg` | [quest-journal.svg](../../assets/Icons/quest-journal.svg) |
| `assets/Icons/prophecies.svg` | [prophecies.svg](../../assets/Icons/prophecies.svg) |
| `assets/Icons/guild.svg` | [guild.svg](../../assets/Icons/guild.svg) |
| `assets/Icons/colosseum.svg` | [colosseum.svg](../../assets/Icons/colosseum.svg) |
| `assets/Icons/cinder-bazaar.svg` | [cinder-bazaar.svg](../../assets/Icons/cinder-bazaar.svg) |
| `assets/Icons/leaderboard.svg` | [leaderboard.svg](../../assets/Icons/leaderboard.svg) |
| `assets/Icons/settings.svg` | [settings.svg](../../assets/Icons/settings.svg) |
