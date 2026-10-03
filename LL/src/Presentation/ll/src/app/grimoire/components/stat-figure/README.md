# StatFigure

The headline number.

**Status:** Draft

A headline number with its label and one line of explanation — Combat Rating, Achievement Points, Arena Rating.

**Provide:** `label`, `value`, optional `caption` and `size` (`sm`). The full explanation, where there is one, is the host's own `title` attribute (D-143).

- The value is `numeral-headline`, Marcellus in `gilt` (64px, or the `title-lg` size, 36px, for `sm`) — never in a table. It is the one number set in proportional figures, because it stands alone; the label is `label` style in `ink-muted` above it and the caption below.
- A default StatFigure is a display-size element, and a screen has only one (Principles · Anti-generic guardrails). Beside a LevelPlate, use `sm`.
