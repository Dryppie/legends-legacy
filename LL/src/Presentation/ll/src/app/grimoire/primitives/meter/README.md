# Meter

The progress bar.

**Status:** Draft

A value against its maximum — health, stamina, experience.

**Provide:** `value`, `max`, `label`, optional `unit` ("HP", set smaller and muted after the maximum), `tone` (`hp` → `meter-hp`, `sp` → `meter-sp`, `xp` → `meter-xp`), `size` (`thin` 4px line, `bar` 10px framed bar) and `live` for combat playback. Both are square-ended gauges, like the Track's rail: no pill (Foundations · Shape).

- The track is `meter-track`; the value is always printed beside it as a spaced fraction ("3,120 / 4,150", the maximum muted), so the fill never carries meaning alone. Screen readers hear "3,120 of 4,150".
- **Live values do not move.** The value is right-aligned in tabular figures and reserves the width of "max / max", so HP ticking in combat never shifts the label or the bar (D-035).
- Use `bar` on combat screens; `thin` in stat columns and plates.
- **Motion** (Foundations · Motion · Value change): the fill is scaled from its left edge (`scaleX`), never resized. A step — experience after a fight, a potion — moves over `duration-slow` on `ease-standard`. `live` Meters, fed by combat playback, follow each tick over `duration-fast`, so the fill is never more than a tick behind its number. The printed value is the truth and changes at once; to count a number the player caused, use `lgLive()`. Under reduced motion the fill moves at once. The Meter never pulses, and nothing is marked while HP ticks.
