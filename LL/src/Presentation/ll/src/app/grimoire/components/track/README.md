# Track

The milestone track.

**Status:** Draft

Milestones along a line of diamonds: tiers of Legacy Ascension, floors of the World Tower, steps of a quest chain. The diamond is the milestone's shape (Foundations · Shape).

**Provide:** `steps`, `current` (zero-based), optional `startLabel` / `endLabel`, per-step `labels`, and `tone` (`gilt`, `hp`, `arcana`).

- Done steps are filled, the current step is ringed, the rest are hollow `line-strong` diamonds on a `meter-track` line. The diamonds are marks, drawn at `border-emphasis` so they hold up rotated at 12px (Foundations · Lines).
- Up to about nine steps; beyond that, use a Meter.
