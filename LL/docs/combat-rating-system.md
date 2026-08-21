# Combat Rating

Combat Rating is the player-facing summary of a character's permanent combat
attributes. Internal APIs and persistence retain some historical `PowerRating`
names for compatibility.

`CombatRatingCalculator` projects the character's final direct attributes from
persisted base attributes, equipped items, and resolved active-Essence attribute
modifiers. It applies flat, additive, and multiplicative modifier semantics,
respects useful caps, values the result with the equipment stat-budget catalog,
and rounds the total once.

The displayed rating is the internal rating divided by ten and rounded down.
Equipment rarity, quality, Potential, and tempering have no independent rating
bonus; they contribute only through the attributes they produce.

The detailed response groups contributions by category so the UI can explain the
number. It does not run encounters, predict wins, classify difficulty, or
recommend content.

Character snapshots carry the deterministic rating used by gameplay surfaces
such as raid and World Tower roster displays. Snapshot fingerprints include the
inputs that can change the rating. Temporary combat modifiers and encounter-only
effects are excluded.

Increment `PowerRatingAlgorithm.Version` whenever rating semantics change.
Tests for this subsystem verify deterministic arithmetic, modifier application,
caps, fingerprints, and party aggregation.
