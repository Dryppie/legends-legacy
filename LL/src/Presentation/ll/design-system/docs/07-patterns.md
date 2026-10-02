# Patterns

Recurring compositions: several parts arranged the same way every time they solve the same problem. Patterns are named `Pattern<Name>`. Each is written down here from the screens that already use it.

## Rules

**Must**
- Name a pattern `Pattern<Name>`, after the problem it solves.
- Build it only from existing Components, Game Components and Registries.
- Name the screens that use it.

**Should**
- Reuse a pattern before composing a new arrangement for the same problem.

**Never**
- Change a component's look inside a pattern; if a part needs a new look, it needs a documented variant.

## PatternExplainedValue

**Problem:** a value the player must understand, not just read.
**Composition:** a Ledger row with a `description` shown on hover and keyboard focus, and `tipMeta` as a footnote in `ink`; for a headline number, a StatFigure with its `title`.
**Seen in:** the Character Overview — every Combat Attribute row.
**Status:** Draft

## PatternPlayerLookup

**Problem:** finding another player and looking at their profile.
**Composition:** a SearchField (suggestions, "No matching players" once searched) paired with an explicit Search button in the PageHeader. While viewing someone else, Presence appears beside their name, the JourneyCard and Combat Style disappear, and "Back to my profile" replaces Refresh.
**Seen in:** the Character Overview.
**Status:** Draft

## PatternBrowseAndInspect

**Problem:** browsing many things of one kind and inspecting one.
**Composition:** TabStrips for what is browsed and how it is filtered, an EntryList (or a Constellation of Sigils) on the stage, and the Folio for the selection — choosing an entry fills the Folio.
**Seen in:** no game screen yet.
**Status:** Draft

## PatternLootLine

**Problem:** telling the player what they found.
**Composition:** a Chronicle line in the Loot channel — `lore` italic in `ink-muted`, with each item an ItemLink in its rarity colour. It replaces the separate Loot History box (Decision D-005).
**Seen in:** the Chronicle, on every screen.
**Status:** Draft

## PatternLockedPreview

**Problem:** showing what is coming before the player can use it.
**Composition:** the locked state of the part itself (Standards · States · Availability) — a LoadoutSlot with a dashed frame and its unlock level ("Unlocks at level 20"), a locked EntryList entry or NavRail item that says "Locked" and opens its condition in the reason tip — shown in place, never hidden, and still in the Tab order.
**Seen in:** the NavRail — its locked destinations. The Character Overview's Essence Loadout shows its future slots this way once the server sends them.
**Status:** Draft

## PatternFeedback

**Problem:** telling the player whether a change, an effect or a cost works for them or against them.
**Composition:** a comparison is a Ledger whose rows show the new value and, in `sub`, a Delta and the current value ("154 — ▲ +12 · now 142"); inside an item's view it is a data block about the player, so its deltas keep their polarity colours while the item's name keeps its rarity (D-024); an upgrade or Ascension preview is StatTiles with `delta` and `deltaPolarity` (a cooldown that falls from 8s to 6.8s is ▼ −1.2s and `better`); conditions are `beneficial` and `harmful` Tags, named and timed ("Weaken 6s", the time passed as `value` so it stays out of capitals), with what they do as Delta rows, and in combat they are the only hue beside the damage types (D-024); a cost the player cannot pay is a `warning` Tag naming the shortfall beside the cost ("Short by 12 Soulstones"), never `danger`, since a shortfall can be made up (D-025), and the committing action is an `insufficient` Button with the same shortfall as its reason — focusable, never plain disabled (Foundations · Colour · Feedback and polarity, Standards · States · Availability).
**Rules:** every delta carries ▲ or ▼ and a sign, or ±0; polarity comes from the stat's rules, never from the sign; no row, list item or card takes a status wash.
**Seen in:** Inventory (equipment comparison), Essences (Ascension previews), Soulstones (upgrades), dungeon and Colosseum combat (conditions).
**Status:** Draft

## Tokens used

None of their own: each pattern uses the tokens of the parts it is built from.

## Do and don't

| Do | Don't |
| --- | --- |
| A Search button beside the SearchField — players expect one. | A search that only works on Enter. |
| Loot lines in the Chronicle's Loot channel. | A separate Loot History panel on the stage. |
| Locked future slots shown with their unlock level. | Only the slots the player has today. |
| "Cooldown 6.8s — ▼ −1.2s" in `delta-better`. | A red −1.2s because the number fell. |

## Related components

- Ledger — the labelled value list
- StatFigure — the headline number
- SearchField — search with suggestions
- Presence — the online status
- EntryList — the browsable name list
- TabStrip — the tabs
- Folio — the detail panel
- Chronicle — chat and the game log
- ItemLink — an item named in text
- LoadoutSlot — an Essence loadout slot
- Delta — the stat change
- StatTile — the compact stat
- Tag — the status label
