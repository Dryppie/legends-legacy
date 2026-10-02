# Standards · State combinations

Game objects carry several states at once: an Epic sword that is equipped and can be upgraded, a reward that is locked and new. Without rules, each state adds its own badge, ring or colour and a row turns into badge soup. This page gives each kind of mark one job, caps how many marks one object shows, fixes where each mark sits on every slot, row and rail item, and says what wins when there is no room (D-094, D-095). It builds on Standards · States, which defines each state.

**Status:** Draft

## Rules

**Must**
- Give each channel one state at a time (Channels, below). When two states want the same channel, the one earlier in precedence takes it, and the other is said in words.
- Show at most four marks on one object: its identity, its selection, one ownership mark and one attention mark (How many at once).
- Put each mark in its fixed place (Marker positions). A mark never moves because another is absent, so the player always looks in the same place.
- Say in words every state that a mark doesn't show: in the object's accessible name, and in its detail view (the Folio). A hover card may repeat them, but nothing exists only on hover (Foundations · Accessibility).
- Keep the focus ring over everything, unchanged (Standards · States).

**Should**
- Show one Tag per row: the first state in the Tag order. List the rest in the Folio.
- Leave out a mark that repeats the row's words. A row whose Tag says "Claimable" takes no attention diamond as well.
- Mark Equipped only where equipped and unequipped things appear together. In the equipment grid, every slot is equipped, so none is marked.

**Never**
- More than one Tag on a row, unless the screen's own design says why.
- Two marks for one state, such as a ring and a badge both saying "selected", or a diamond and a Tag both saying "ready". There is no glow to add to them (D-071).
- A colour change for more than one state at once (One colour per object).

## Channels

Each kind of mark belongs to one question about the object. The states, their words and their tokens are in Standards · States; this table says where each one goes when several meet.

| Channel | Answers | Holds | On a slot | On a row | On a rail item |
| --- | --- | --- | --- | --- | --- |
| Identity | What is it? | Rarity: the edge, the name's colour and the rarity code. Not a state: no state takes it, but Undiscovered withholds it | The rarity edge, the code in the top start corner, the name in its rarity colour | The thumbnail's edge, the name's colour, the code after the name | The icon and title |
| Selection | Is it the one I chose? | Selected (Interaction) | The 2px `arcana-glow` ring on the frame | The 2px `arcana-glow` bar at the start, and the wash | — (the rail shows Current, a place, not a choice) |
| Ownership | Whose is it, and what is it doing? | Ownership and use: Equipped, Attuned, Listed, Favourite… | One mark in the bottom start corner: the in-use square (Equipped, Attuned) or the favourite ribbon; the word leads the meta line | The word, as the row's one Tag; the ribbon beside it | — |
| Attention | Is something waiting for me? | Ready and Claimable (D-065: the diamond is a ready or claimable marker) | The `arcana-glow` diamond in the top end corner | The diamond at the row's end | A count badge, or the diamond, at the item's end |
| Availability | Can I use it now? | Unavailable, Locked, Restricted, Insufficient, On cooldown | The name's tone, a dashed frame when locked, and the reason under the name or in the reason tip | The name's tone, the Locked Tag, the reason tip | The title's tone, "Locked" at the item's end, the reason tip |
| Data | Is what I see complete and current? | Loading, Refreshing, Stale, Pending save | Loading blocks in the frame | Loading blocks; "Updating…" or "Updated 5m ago" in the region's head, not the row | — |

**Availability's "dimming" is the text tone, not opacity.** The further from reach, the dimmer the name: `ink-muted` for a thing the player may use soon, `ink-disabled` for a locked one (Standards · States). Opacity is for art only: a thing not owned, a place a dragged thing left. Text, edges and the focus ring are never faded.

**New is a word, not an attention mark.** New, Unread and Claimable are all arcana, since each waits for the player. Claimable and Ready take the diamond; New is the "+ New" Tag, and Unread is a count. That keeps the diamond for things the player can act on.

## How many at once

An object shows at most four marks: **identity, selection, one ownership mark, one attention mark.** Its focus ring is not counted: it is where the keyboard is, not a state of the object.

- **A blocked thing** shows availability in attention's place. A blocked thing isn't waiting for the player, and it can't be selected (Standards · States), so it never takes selection or attention.
- **Everything else is said in words**, in the object's accessible name and in its Folio. On a captioned slot the meta line says the ownership word ("Equipped · Main hand"); a row's one Tag says the first state in the Tag order.
- **Undiscovered** withholds identity: no art, no rarity, and "Undiscovered" in place of the name.

## When there is no room

Two orders, for two different questions. Neither replaces the other.

**Which mark stays, when a part is too small for all of them.** A slot without a caption, a compact rail, a LoadoutSlot. Keep them in this order and drop from the end:

1. Availability — a blocked thing must say so.
2. Selection — what the Folio is showing.
3. Attention — something waiting for the player.
4. Ownership — the in-use square or the ribbon.
5. New — the "+ New" word.

Identity and focus are never dropped. A dropped state is still in the accessible name and the Folio.

**Which state is the row's one Tag.** The first that applies, in this order: Failed, Claimable, Expiring soon, Locked, Listed, In escrow, Borrowed, Equipped, Attuned, Assigned, Captured, In progress, New, Completed, Claimed, Expired, Opened. The order puts what the player must act on first, then what blocks them, then what describes the thing, then history (D-086). `lgTopState` picks it. Markers sit beside the Tag: the favourite ribbon, an unread count, the current diamond, the attention diamond. Markers don't compete with the Tag, and each has its own place.

The two orders agree where they meet: a blocked thing loses its attention mark, and New is always last to show.

## One colour per object

An object's own colour — its name, its edge, its fill — changes for one state at most.

- **Identity's colour is not a state.** The name stays in its rarity colour until availability takes it: a locked slot's name is `ink-disabled`, and its code and edge still say the rarity.
- **Only the row's Tag takes its state's tone.** A second state on the same row is said in `ink-muted` in the meta line. A claimable cache that expires soon has an arcana "Claimable" Tag, and "Expires in 2h" in `ink-muted`, not `warning` as well.
- **Selection is the only lit edge.** A selected slot's ring is `arcana-glow`; no other state lights an edge.

## Marker positions

Each mark has one place on each part. Corner marks sit `space-1` in from the frame's edges, are `icon-marker` (12px) across, and stand on a 2px `ground` ring so they read over art.

**ItemSlot**

| Place | Holds | Channel |
| --- | --- | --- |
| Top start corner | The rarity code | Identity |
| Top end corner | The attention diamond (`ready`, or the Claimable state) | Attention |
| Bottom start corner | The in-use square (Equipped, Attuned) or the favourite ribbon; the square comes first | Ownership |
| Bottom end corner | The quantity, ×3 | Not a state |
| The frame's edge | The rarity edge; the `arcana-glow` ring when selected; a dashed edge when locked | Identity, selection, availability |
| The caption | The name; the reason under it when blocked; the meta line, led by the state's word | Identity, availability, ownership |

**LoadoutSlot.** The head row holds "Slot 3" at its start, then at its end the Tag (Attuned, Locked) and after it the attention diamond (`ready`: an Essence can be attuned). Its small ItemSlot shows identity only.

**EntryList.** The selection bar at the row's start; the name; the one Tag straight after it; the attention diamond at the row's end. A locked entry's Locked Tag takes the Tag's place, and it shows no diamond.

**NavRail.** The current item's gilt bar at its start and its gilt wash (where the player is, D-118); the icon; the title; at the item's end, one of: "Locked" (availability), a count badge, or the attention diamond (`ready`, when there is nothing to count). Locked comes first, then the count. In `compact`, the badge sits on the icon's top end corner and the diamond on the item's (D-115, D-119), and a locked item says so in its tip.

**List and ListRow** follow EntryList: the selection bar at the start, the thumbnail's rarity edge, the name and code, the one Tag after them, and markers beside the Tag. ListRow takes its markers through its `tags` slot (`lgSlot="tags"`) until DS-040 gives it a `ready` input.

## Worked examples

Each example shows what the player sees, and what is said only in words.

| Example | Where | What shows | Said in words only |
| --- | --- | --- | --- |
| An Epic sword, equipped, with an upgrade available | An inventory ItemSlot | The Epic edge, "E" and the name in Epic; the in-use square bottom start; the attention diamond top end; meta "Equipped · Main hand" | "Upgrade available": in the name ("Ashen Blade, Epic, equipped, upgrade available") and as the Folio's Upgrade action |
| A locked reward that is new | A reward row | The name in `ink-disabled`; the Locked Tag (dashed); the unlock condition in the reason tip | New: the Locked Tag comes first, and a blocked thing takes no attention mark. The Folio says "+ New" |
| A claimable Prophecy cache that expires soon | A Prophecy row | The arcana "Claimable" Tag and a Claim Button; "Expires in 2h" in `ink-muted` in the meta line | Nothing is hidden; only the expiry's `warning` tone is dropped. No diamond: the Tag already says it |
| A selected item the player can't afford | A Cinder Bazaar listing | The `arcana-glow` bar or ring: the item stays selectable | The shortfall sits on the committing action, not the item: the Folio's Buy Button is Insufficient, "Short by 250 Cinders", and the cost line reads "1,000 / 1,250" |
| An undiscovered creature that is the current Creature Focus | An EntryList of creatures | "Undiscovered" in place of the name, in `ink-muted`; the "Creature Focus" Tag (Assigned: the Focus activity holds it) | The hint, "Found on Floors 10–20", and the Focus cooldown, in the Folio |
| A Bazaar-listed item that is a favourite | An inventory ItemSlot | The rarity identity; the ribbon bottom start; meta "Listed · 1,200 Cinders" | Nothing: Listed is the meta's word, Favourite the mark |
| A captured Arena defence build that differs from the live build | The Arena defence row | The "In defence snapshot" Tag; "Changed since 14:02" in `ink-muted` | The snapshot rule, in the Folio: "Snapshot from 14:02. Changes apply at the next snapshot." |

## Tokens used

| Token | Role here |
| --- | --- |
| `arcana-glow` | The selection ring or bar, and the attention diamond |
| `arcana`, `arcana-soft` | The rail's count badge, and the "+ New" and "Claimable" Tags |
| `ink` | The ownership marks: the in-use square and the ribbon |
| `ink-muted`, `ink-disabled` | Availability's text tone; a second state's words on a row |
| `ground` | The 2px ring under a corner mark, so it reads over art |
| `icon-marker` | The size of every corner mark: 12px across |
| `space-1` | A corner mark's inset from the frame |
| `border-emphasis` | The width of the selection ring and bar |
| `focus-ring` | Focus, over every mark |

## Code

| Name | What it is |
| --- | --- |
| `lgTopState(states)` | The row's one Tag, by the Tag order |
| ItemSlot `ready` | The attention diamond. `true`, or the words: "Upgrade available". The Claimable state draws it too |
| ItemSlot `state: 'equipped'` or `'attuned'` | The in-use square, bottom start, and the word in the meta line |
| ItemSlot `favourite` | The ribbon, bottom start, when there is no in-use square; until the ribbon is drawn, the word in the meta line |
| EntryList and NavRail items `ready`, LoadoutSlot `ready` | The attention diamond at the end. On a rail item a count `badge` comes first |

## Do and don't

| Do | Don't |
| --- | --- |
| "Equipped" as the one Tag, the ribbon beside it | Equipped, Favourite, In Preset 2 and New as four Tags |
| A selected Epic slot: the Epic edge and code, the selection ring, the diamond for its upgrade | A selected Epic slot that also glows, pulses and wears a "!" badge |
| "Claimable" in arcana, and "Expires in 2h" in `ink-muted` beside it | "Claimable" in arcana and "Expires in 2h" in orange, on a name turned gold |
| A locked entry: its name in `ink-disabled`, the Locked Tag, the reason on hover and focus | A locked entry with a Locked Tag, a "+ New" Tag and a diamond |
| The diamond always in the top end corner, the ribbon always bottom start | The ribbon taking the top end corner when there is no diamond |
| "Upgrade available" in the accessible name and the Folio | "Upgrade available" only in a hover card |

## Related components

ItemSlot, LoadoutSlot, EntryList, NavRail, List and ListRow, Tag, Folio, Button.
