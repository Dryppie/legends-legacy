# Tag

The status label.

**Status:** Draft

A short all-caps label beside a name: "+ New quest", "Equipped", "Weaken", a rarity.

**Provide:** a `state` from Standards · States, or the words as content (one to three words) and a `tone`; for a number or a time, `value`.

- **States come with their words.** `state` takes the word, tone and glyph from `LG_STATES`, so a state reads the same on every screen: `locked` → "Locked" with a dashed edge; `claimable` → "Claimable" in `new`; `completed` → "✓ Completed"; `failed` → "✕ Failed" in `danger`; `expiring` with `value="2h"` → "Expires in 2h" in `warning`; `in-progress` with `value="3 / 5"`; `equipped`, `attuned`, `listed`, `borrowed`, `escrow` and `claimed` in `neutral`. `label` replaces the word where the state names something: `state="assigned"` with `label="In Preset 2"`.
- **One Tag per row.** When several states apply, show the first in the Tag order in Standards · State combinations (`lgTopState` picks it) and list the rest in the Folio.
- `locked` is outlined in `ink-muted` with a dashed edge — the same dash as a locked slot's frame: a gate the player will pass.
- `neutral` (the default) is outlined in `ink-muted`: states and facts with no colour of their own — "Equipped", "Relic", a guild tag, a count.
- `new` = `arcana` on `arcana-soft`, for new, ready, claimable or actionable things only — "+ New quest", "Claimable".
- The status tones are outlined, never filled: soft status washes belong to inline alerts, not tags (D-025). `success` — a confirmed outcome ("Upgraded", "Floor cleared") — is `ink` and adds ✓ itself. `warning` — a reversible risk, a shortfall, something expiring ("Short by 12", "Expires in 2h") — is outlined in `warning`. `danger` — a loss or failure ("Defeated", "Loot lost") — is outlined in `danger` (D-027).
- `beneficial` and `harmful` name an effect or condition — "Empower", "Haste"; "Weaken", "Slow" — outlined in `effect-beneficial` and `effect-harmful`. Screen readers hear "beneficial" or "harmful" after the name. Always use the effect's own name, with its time left when it has one.
- **The frame says the polarity** (D-084, Foundations · Iconography · Condition): a `beneficial` Tag keeps its `radius-control` corners, and a `harmful` one has its corners cut, so the two differ in greyscale and for colour-blind players, not by colour alone. A condition's icon, once drawn, sits before its name at 16px in the Tag's colour; until then the Tag alone is its fallback (Registries · Effects and conditions).
- Rarity tones are outlined in their `rarity-*` colour and always name the rarity.
- **`gilt` is deprecated (D-020)** and renders as `neutral`: none of gilt's four jobs is a label. Use `neutral`.
- **Context:** inside an item context — an ItemSlot, a LoadoutSlot, an item's Folio, loot and market rows — only rarity tones take a hue; every other tag there is `neutral`. In combat only a condition's `beneficial` or `harmful` tone takes a hue beside the damage types; in chat, tags are `neutral` (Foundations · Colour · Context ownership).
- **Numbers go in `value`,** after the label: `value="6s"` with "Weaken" reads WEAKEN 6s. The value keeps tabular figures and stays out of capitals, so a time never reads "6S" (Foundations · Numerals).
- Tags state facts; never use one as a button. A Tag is a 20px rectangle at `radius-control`, the same corner as a Button; its height, transparent fill and capitals keep it apart from one (Foundations · Shape).

## Supported states

A Tag is a label, so it has no interaction states; it shows other parts' states as words (Standards · States · Which parts take which states).

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Ready, New, Claimable | Text in `arcana` on `arcana-soft` | "Ready", "+ New", "Claimable" | The word; the + is not read |
| In progress, Claimed, Opened, Expired | Text: outlined `ink-muted` | "In progress 3 / 5", "Claimed", "Opened", "Expired" | The word and its value |
| Completed | Text and icon: ✓ in `ink` | "✓ Completed" | "Completed" |
| Expiring soon, Insufficient resources | Text: outlined `warning` | "Expires in 2h", "Short by 12 Soulstones" | The words |
| Failed | Text and icon: ✕ in `danger` | "✕ Failed" | "Failed" |
| Locked | Edge: dashed; Text: `ink-muted` | "Locked" | "Locked" |
| Owned, Not owned, Equipped, Attuned, Assigned, Captured, Listed, In escrow, Borrowed | Text: outlined `ink-muted` | "Equipped", "In Preset 2", "In defence snapshot", "Listed 1,200 Cinders" | The words |
| Undiscovered | Text: outlined `ink-muted` | "Undiscovered" | "Undiscovered" |

