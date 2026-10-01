# LevelPlate

The level display.

**Status:** Draft

A huge level numeral with its progress bar and two headline stats — the first thing on a character or creature detail.

**Provide:** `level`, `xp` and `xpMax` (or another progress with `xpUnit`, e.g. "Essence"), and up to two `aside` stats with icons.

- The numeral is `level-numeral`, Marcellus in `gilt` with proportional figures — one of the two places Marcellus sets a number (the other is StatFigure). The kicker ("Level") runs vertically in `label` style.
- The progress reads "350 / 24,500 EXP": a spaced slash, tabular figures, the unit muted after it. Side stats are `numeral-row`, value right; give an `aside` item a `unit` ("12s", "84 HP/5s").
- One per detail view.
