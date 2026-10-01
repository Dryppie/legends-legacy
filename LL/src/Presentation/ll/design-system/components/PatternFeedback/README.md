# PatternFeedback

Whether a change, an effect or a cost works for the player or against them.

**Status:** Draft

**Problem:** Legend's Legacy keeps showing changes to judge — equipment comparison, Soulstone upgrades, Essence Ascension previews — and effects in combat that help or hurt, and it asks for costs the player cannot always pay. The player must see at a glance which way each one goes, without depending on colour.

**Composition:**
- **A comparison** is a Ledger whose rows show the new value and, in the `sub` line, a Delta and the current value: "154 — ▲ +12 · now 142". Inside an item's view the comparison is a data block about the player, so its deltas keep their polarity colours while the item's name keeps its rarity (D-024).
- **An upgrade or Ascension preview** is StatTiles with `delta` and `deltaPolarity`. A cooldown that falls from 8s to 6.8s is ▼ −1.2s and `better`.
- **Conditions** are Tags in the `harmful` and `beneficial` tones, named and timed ("Weaken 6s", the time passed as `value` so it stays out of capitals), with what they do as Delta rows. In combat they are the only hue beside the damage types (D-024).
- **A cost the player cannot pay** is a `warning` Tag naming the shortfall ("Short by 12") beside the cost, and the committing action disabled. A shortfall is a warning — reversible — never `danger` (D-025).

**Rules:** every delta carries ▲ or ▼ and a sign, or ±0; polarity comes from the stat's rules, never from the sign; no row, list item or card takes a status wash.

**Seen in:** Inventory (equipment comparison), Essences (Ascension previews), Soulstones (upgrades), dungeon and Colosseum combat (conditions).

## Related components

- Delta — the stat change
- StatTile — the compact stat
- Ledger — the labelled value list
- Tag — the status label
- Panel — the content box
- Button — the command button
- ItemLink — an item named in text
