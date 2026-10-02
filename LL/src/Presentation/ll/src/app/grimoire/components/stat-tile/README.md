# StatTile

The compact stat.

**Status:** Draft

A compact label-and-value tile for secondary stats, laid out in `.lg-statgrid`.

**Provide:** `label` (short capitals: ARMOR, CRIT), `value`, optional `suffix` — the unit, set smaller and muted right after the value ("%", "s") — and for a change `delta` (a signed number), `deltaPolarity` (`better`, `worse` or `neutral`) and, when the default formatting will not do, `deltaText`.

- The delta is a Delta: ▲ +4, ▼ −1.2s, or ±0 when `delta` is 0. Its colour comes from `deltaPolarity` — whether the change helps the player — never from the sign: a cooldown falling from 8s to 6.8s is `better` (D-026). Without `deltaPolarity` the delta is `neutral`, because the tile cannot know the stat's rules.

- `tile` fill with `on-tile` value in `numeral-stat` with tabular figures (the suffix in `numeral-compact`, `on-tile-muted`) and `on-tile-muted` label in `label` style above; the delta is `caption` size. A missing value shows "—", never an empty tile. It has no edge: the `tile` fill sets it off its surface, and a line around each tile is the frame-around-everything the guardrails forbid (Foundations · Lines).
- Wrap tiles in `<div class="lg-statgrid">`: two a row in a Stacked region (the Folio, an inspector), three at Narrow, four at Medium, up to six at Wide, and never narrower than `stat-min` (8.5rem). A tile with a Delta needs more room: in a Stacked region give it a one-word label. Six to eight per group; more wants a Ledger (Foundations · Layout).
