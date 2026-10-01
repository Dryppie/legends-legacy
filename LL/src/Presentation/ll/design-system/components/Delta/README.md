# Delta

The stat change.

**Status:** Draft

A change to a number, shown so its meaning never depends on colour: a glyph for which way the number moved, a sign, the size of the change, and a colour for whether it helps the player. Equipment comparison, Soulstone upgrades, Essence Ascension previews, and StatTile's own delta.

**Provide:** `direction` (`up`, `down` or `none`), `value` — the size of the change, formatted, without a sign ("12%", "1.2s") — and `polarity` (`better`, `worse` or `neutral`).

- **Polarity follows the player's benefit, not the sign.** A cooldown going from 8s to 6.8s is `direction: 'down'`, `polarity: 'better'`: ▼ −1.2s in `delta-better`. Set polarity from the game's rules for that stat; the component never guesses it from the number.
- Every delta shows its direction and its sign: ▲ +12%, ▼ −1.2s, and ±0 when nothing changed (`delta-neutral`). Unchanged has no glyph: it used to be ◇ 0, but a hollow diamond is a milestone still to come (D-067, Foundations · Shape). The ± is hidden from screen readers, which hear "0, unchanged".
- The minus is a true minus (−), not a hyphen. Screen readers hear the sign, the value and the judgement: "+12, better".
- Colours: `delta-better` (lichen), `delta-worse` (madder), `delta-neutral` (muted ink). A worse delta is not `danger`: a lower stat is not a loss or a failure.
- It takes the size of the text around it: `caption` (12px) in a StatTile and in a Ledger row's `sub`.
- Use `neutral` with a direction for a change the game does not judge — a role-dependent stat such as Threat.

## Related components

- StatTile — the compact stat
- Ledger — the labelled value list
- Tag — the status label
