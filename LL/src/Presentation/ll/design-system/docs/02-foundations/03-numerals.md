# Foundations · Numerals

Legend's Legacy is mostly numbers: attributes, prices, quantities, ratings, damage, cooldowns and progress. This section sets how every number is written, aligned and updated, so a player can compare two values at a glance and a live value never moves the interface. The type styles numbers use are in Foundations · Typography.

## Rules

**Must**
- Set numbers in tabular lining figures (`font-variant-numeric: tabular-nums lining-nums`) wherever they align in columns, stack in lists or update live. The root (`.lg-root`) sets this, so every component inherits it.
- Use proportional figures (`proportional-nums lining-nums`) only for a single isolated headline number: the StatFigure value and the LevelPlate level.
- Right-align numeric columns and their headers. Align values of mixed precision on the decimal point (Alignment, below).
- Put a unit immediately after its number, in a smaller, muted style (Units, below).
- Write a negative number with a true minus, U+2212 (−12); a range with an en dash (12–18); a multiplier with × (×1.5); a fraction with a spaced slash (3,120 / 4,150).
- Show "—" (an em dash) for a value that is unknown or does not apply, and "0" for zero. Never leave a value cell empty.
- Give numbers thousands separators: 12,480.
- Format values before passing them to a component (the `lgFormat*` helpers). Components style a unit but never add one.
- Print a meter's value beside its bar.
- Reserve the width of a number that updates live, so it never moves anything beside it (Live values, below).

**Should**
- Keep one precision per stat everywhere, trailing zero included. Attack Speed is always two places (1.10, not 1.1), so a value keeps its width as it changes.
- Set the screen's one headline figure in Marcellus in `gilt`: the level when the level is what the screen is about, or Combat Rating (`level-numeral`, or `numeral-headline` in a StatFigure). Every other number is `ink` (D-015).
- Abbreviate currencies only on request (12.5k), with the full figure in the tooltip and read out in full: screen readers hear "12,480 Cinders", never "12.5k". CurrencyPill's `short` does both, and clicking it toggles the format as the game does today (Foundations · Accessibility).
- Show every change with Delta: ▲ or ▼ with its sign (+12%, −1.2s), or ±0 when nothing changed. Colour it by whether it helps the player, never by its sign (Foundations · Colour · Feedback and polarity).
- Keep Sigil values to three characters ("42", "9%"). Longer numbers belong in a StatTile.

**Never**
- Set a number in Marcellus other than the StatFigure value and the LevelPlate level (D-034). A number that is part of a name, such as Floor 12 or Ascension II, belongs to the name.
- Write a hyphen for a minus or a range, an x for ×, or a slash without spaces in a fraction.
- Leave a cell empty, or write "N/A", "-", "?" or "0" for a value that is unknown.
- Let a live number push its neighbours, or animate a number's width.
- Put numbers in lore.
- Abbreviate a number unless the full figure is one hover or focus away.

## Figures

| Where | Figures | Face and style |
| --- | --- | --- |
| Columns, lists, rows, tables, meters, currency, counters and timers | Tabular lining | Barlow Condensed: `numeral-stat`, `numeral-row`, `numeral-compact` |
| Numbers inside running text (Folio effects, tooltips, the Chronicle) | Tabular lining, inherited from the root | The text's own style, Barlow `body` or `body-compact` |
| The Sigil value | Tabular lining | Barlow Condensed 700, `sigil-numeral` |
| The StatFigure value | Proportional lining | Marcellus in `gilt`, `numeral-headline` |
| The LevelPlate level | Proportional lining | Marcellus in `gilt`, `level-numeral` |

With tabular figures every digit is the same width, so 1ch in a numeral style is exactly one digit. Live values reserve their width in `ch` for this reason.

## Signs and symbols

| Write | For | Never | Helper |
| --- | --- | --- | --- |
| 12,480 | Thousands | 12480, 12 480 | `lgFormatNumber(12480)` |
| −12 | A negative number: the true minus, U+2212 | -12 | `lgFormatNumber(-12)` |
| +12, −1.2s, ±0 | A change, always inside a Delta: ▲ or ▼ with its sign, or ±0 | 12 with no sign | Delta |
| 12–18 | A range: an en dash, no spaces | 12-18, 12 - 18 | `lgFormatRange(12, 18)` |
| ×1.5 | A multiplier: × before the number, no space. A stack of three in an ItemSlot corner is ×3. | x1.5, 1.5x | `lgFormatTimes(1.5)` |
| 3,120 / 4,150 | A value out of a maximum: non-breaking spaces round the slash | 3,120/4,150 | `lgFormatFraction(3120, 4150)` |
| — | Unknown, or does not apply | An empty cell, N/A, -, ? | `LG_NONE`; every helper returns it for a missing value |
| 0 | Zero | —, "none", an empty cell | `lgFormatNumber(0)` |
| 12.5k | Abbreviated, on request only | 12,5k, 12.5K | `lgFormatShort(12480)` |

## Alignment

- **Right-align** every numeric column and its header, so units line up under units, tens under tens and hundreds under hundreds.
- **Align mixed precision on the decimal point.** The integer sits right-aligned in one column, and the fraction and unit sit left-aligned in the next, so 1,284, 24.8%, 84 HP/5s and 184.6 threat/s line up. Ledger does this for you: its rows share the columns through CSS subgrid (D-035). In a table, split each numeric column into two cells with `lgNumberParts`, and give the header `colspan="2"`.
- **One precision needs only right alignment.** Tabular figures already line up whole numbers, or numbers that all have one decimal place.
- **Text in a numeric column** — a Delta, "Locked" or "—" — sits at the right, in the integer column.

## Units

- **A unit follows its number immediately and is smaller and muted.** It is set at the `caption` size in Barlow 500, `ink-muted` (the `.lg-unit` class).
- **Symbol units attach with no space:** 24.8%, 12s, 2h 14m. **Word units take a non-breaking space:** 84 HP/5s, 184.6 threat/s, 40 Soulstones, 240 Armor Rating. `lgFormatUnit(84, 'HP/5s')` chooses the spacing for you.
- **Signs and operators before a number are part of it** and keep its style: +, − and ×.
- **Inside a Delta the unit keeps the delta's colour,** so the change reads as one mark: ▼ −1.2s.
- **In a table, name the unit once,** in the column header or the caption ("Price each", "in Cinders"), not in every cell. A column holds one unit.
- **Durations use at most two units:** 12s, 2m 14s, 2h 14m, 3d 4h. `lgFormatDuration(134)` writes "2m 14s", and `lgSpokenDuration(134)` gives screen readers "2 minutes 14 seconds" — a cooldown's "Ready in" and an "Expires in" read both ways (Standards · States).

## Live values

Combat, auctions and currency update while the player watches. A number that changes must not move the label beside it, the bar below it or the TopBar around it.

1. **Tabular figures.** 9,990 and 9,999 are the same width.
2. **Reserve the widest value** the number will show, in `ch`. Meter reserves the width of "max / max". CurrencyPill grows to fit and never shrinks while it is on screen, and its `reserve` holds room for the next digit from the start. A counter or a timer gets `min-width` in `ch`.
3. **Right-align inside the reserved width.** New digits grow to the left, and whatever follows the number stays put.
4. **Keep the format fixed while the value is live.** Don't switch from 999 to 1k, or from 9.5 to 10, mid-count; show 10.0.
5. **Don't animate width.** Count only a change the player caused — after combat, after a purchase — over `duration-slow` (400ms), inside the reserved width, and not at all under reduced motion. A change that came by itself shows at once, with the live-update mark. `lgLive()` does both (Foundations · Motion · Value change).
6. **Don't make live numbers live regions.** Announce only the outcomes a player must act on — being outbid, or loot that has landed — through the Chronicle or an alert, once. Combat ticks are never announced.

## Number and label, by context

| Context | Order | Example | Part |
| --- | --- | --- | --- |
| TopBar | Art, amount, name | [Cinders art] 12,480 CINDERS | CurrencyPill |
| Cost list | What you pay for, leader, amount, currency | Upgrade cost ····· 40 Soulstones | Ledger |
| Table | The header names the column and its unit; the cell holds only the number | Price each / 1,250 | Table |
| Tooltip | Title, explanation, then a footnote of label, colon, number and unit | Armor · … · From equipment: 240 Armor Rating | Ledger's tooltip |
| Ledger | Label left, value right, unit after the value, sub-line below | Armor ····· 38% / 240 Armor Rating | Ledger |
| StatTile | Label left in capitals, value right, delta under the value | ARMOR 45 ▲ +4 | StatTile |
| StatFigure | Label above, figure, caption below | COMBAT RATING / 1,284 / Your overall strength | StatFigure |
| Meter | Label left, value and maximum right, bar below | HP 3,120 / 4,150 | Meter |
| Tag | Label, then value | WEAKEN 6s · SHORT BY 12 | Tag (`value`) |
| LevelPlate | Kicker and level; the progress fraction and its unit below the bar | LEVEL 142 · 350 / 24,500 EXP | LevelPlate |

## Tokens used

| Token | Role here |
| --- | --- |
| `numeral` (family) | Barlow Condensed, for every number that is not a headline |
| `numeral-stat` | StatTile values, 22px |
| `numeral-row` | Ledger values, Meter values, side stats, currency amounts, 18px |
| `numeral-compact` | Table cells and dense rows: prices, quantities, totals, ItemSlot quantities, 14px |
| `sigil-numeral` | The number inside a Sigil, Barlow Condensed 700 |
| `numeral-headline` | The StatFigure value, Marcellus 64px, proportional figures; never in a table |
| `level-numeral` | The level on a LevelPlate, Marcellus 84px, proportional figures |
| `text-caption`, `ink-muted` | Units after a number (`.lg-unit`) |
| `ink` | Every ordinary value: Ledger values, stat tiles, table cells, meter numbers |
| `gilt` | The screen's one headline figure, and effect magnitudes inside descriptions |
| `delta-better`, `delta-worse`, `delta-neutral` | Changes, through Delta |

Components and screens format numbers with the `@grimoire` helpers in `core/grimoire-format.ts`: `lgFormatNumber`, `lgFormatShort`, `lgFormatRange`, `lgFormatTimes`, `lgFormatFraction`, `lgFormatPercent`, `lgFormatUnit`, `lgNumberParts`, `LG_NONE`, `lgFormatDuration` and `lgSpokenDuration`, all also in `LG_FORMAT`.

## Do and don't

| Do | Don't |
| --- | --- |
| 12,480 | 12480 |
| −12, with a true minus | -12, with a hyphen |
| 12–18 | 12-18 |
| ×1.5 | x1.5, or 1.5x |
| 3,120 / 4,150 | 3,120/4,150 |
| "—" for Magic Penetration when your Essences deal no magic damage | An empty cell, or "N/A" |
| 0% Life Steal | "—" for a stat that is zero |
| 84 HP/5s, with "HP/5s" smaller and muted | 84HP/5s at full size |
| A Price each column right-aligned, with the currency named in the caption | "1,250 Cinders" repeated in every cell |
| 1,284, 24.8% and 184.6 aligned on the decimal point | The same values centred, or left-aligned |
| A bid counter with a reserved width, so "Cinders" after it stays put | A bid counter that pushes "Cinders" right when it reaches 10,000 |
| Attack Speed 1.10 | Attack Speed 1.1 beside 1.12 |
| 12.5k, with 12,480 in the tooltip | 12.5k with no way to see the exact amount |
| Pass "240 Armor Rating" to the Ledger row. | Pass 240 and expect the Ledger to add "Armor Rating". |
| Put a four-digit rating in a StatTile. | Squeeze "1,284" into a Sigil. |

## Related components

- Ledger — the labelled value list
- StatTile — the compact stat
- StatFigure — the headline number
- Meter — the progress bar
- CurrencyPill — the currency amount
- LevelPlate — the level display
- Delta — the stat change
- Tag — the status label
- Sigil — the hex stat badge
