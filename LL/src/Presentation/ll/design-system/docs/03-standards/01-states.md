# Standards · States

Every part of the game is always in some state: hovered, locked, claimable, equipped, undiscovered, saving. This page is the one model for all of them. For each state it gives the meaning in one sentence, the visual channel that shows it, its tokens, the exact words on screen and what a screen reader hears. Component pages name their states with the words on this page and nothing else. It replaces the States section that Standards used to hold (D-086).

## How the model is organised

States come in six families. Each family answers one question about a part, and a part is in exactly one state of each family at a time.

| Family | Answers | States |
| --- | --- | --- |
| Interaction | What is the player doing to it? | Default, Hover, Focus-visible, Pressed, Selected, Current, Dragging, Disabled |
| Availability | Can the player do it now — and if not, why not? | Available, Ready, Unavailable, Locked, Restricted, Insufficient resources, On cooldown |
| Lifecycle | Where is it in its life? | New, Unread, In progress, Completed, Claimable, Claimed, Opened, Expiring soon, Expired, Failed |
| Ownership and use | Whose is it, and what is it doing? | Owned, Not owned, Equipped, Attuned, Assigned, Captured, Listed, In escrow, Borrowed, Favourite |
| Knowledge | What does the player know about it? | Discovered, Undiscovered, Hidden, Unknown |
| Data | How complete and how fresh is what is shown? | Loading, Refreshing, Pending save, Stale, Error, Empty, Offline |

Families combine. An entry can be selected (Interaction), locked (Availability) and new (Lifecycle) at once, and each family uses its own channel, so all three can be seen. Standards · State combinations says what wins when two want the same place, how many marks one thing shows, and where each mark sits.

## Rules

**Must**
- Show every state through a channel other than colour, and put it in words wherever the channel alone could be misread.
- Use the exact words on this page. A state's word is completed with its detail ("Ready in 4m 12s"), never paraphrased for one screen.
- Give every blocking state its reason. Unavailable says why. Locked says how it unlocks. Restricted says who may. Insufficient says what is missing and how much. On cooldown says when.
- Keep blocked controls focusable. Use `aria-disabled="true"`, not `disabled`, and make the reason the control's description (`aria-describedby`). A press shows and announces the reason instead of acting (The reason tip).
- Draw the focus ring over every other state, unchanged.
- Announce a state change the player caused, or one that blocks what they are doing, with `LL.announce` (Announcements).
- List each component's states on its page, by the names on this page.

**Should**
- Prefer Unavailable with a reason to plain Disabled. Plain Disabled is for a control whose limit is obvious from what sits beside it: Previous on the first page, − at a stepper's minimum.
- Show one Tag per row (Principles · Anti-generic guardrails). When several states apply, show the first in the Tag order (Standards · State combinations) and list the rest in the Folio.
- Reserve room for words that come and go ("Saving…", "Updating…"), so nothing shifts (Foundations · Motion).

**Never**
- Show a state by colour alone, by fading text, or with a glow (D-012).
- Hide something blocked that the player can work towards. Only the Knowledge family's Hidden is not drawn.
- Take a blocked control out of the focus order, unless it is plain Disabled.
- Announce hover, a background refresh nobody asked for, or a countdown as it ticks.

## Channels

A state uses one or two channels: one to be seen at a glance (an edge, a marker, a fill) and, where it needs saying, text to be read and heard.

| Channel | What it is | Carries | Tokens |
| --- | --- | --- | --- |
| Edge | A line on or around the part: a bar, an underline, a ring, a frame. Solid, dashed or cut | Selected (solid bar or underline), focus (the ring), locked (dashed frame), harmful conditions (cut corners), a valid drop target | `arcana-glow`, `focus-ring`, `line`, `line-strong`, `border-hairline`, `border-emphasis` |
| Fill | A wash or block behind the part, or the part's own fill emptying | Hover (a neutral wash), the live-update mark, loading blocks, a locked Sigil's empty hex | `surface-raised`, `surface`, `changed` |
| Marker | A small solid shape in a fixed place (Standards · State combinations) | Current (gilt diamond), ready and claimable (arcana-glow diamond), unread (a count badge), equipped and attuned on a slot (the in-use square), favourite (the ribbon) | `gilt`, `arcana-glow`, `arcana`, `arcana-soft`, `ink`, `icon-marker` |
| Text | A word or phrase: a Tag, a label, a meta line, the reason tip | Most states. The only channel that can say why | `ink`, `ink-muted`, `ink-disabled`, `warning`, `danger`, `success` |
| Icon | A drawn sign beside the word: ✓, ✕, the lock, the Nobility crown. Never alone (Foundations · Iconography) | Completed, failed, error, locked, restricted to Nobility | `icon-marker`, `icon-sm` |
| Opacity | Art faded. Never text, edges or the focus ring | Art of a thing not owned; the place a dragged thing left | `opacity-unowned`, `opacity-drag-origin` |
| Typeface | A weight or face change | Selected and current (600), an unread thread (600) | Type styles `nav-active`, `body-compact-strong` |
| Position | A 1px drop | Pressed only | `border-hairline`, `duration-instant`, `duration-fast` |

**Colour inside a channel.** Status colours (`warning`, `danger`, `success`) appear in shell, control and data contexts only. Inside item, combat and chat contexts the word and its channel stay and the colour drops to `ink` or `ink-muted` (Foundations · Colour · Context ownership).

**The further from reach, the dimmer.** Available text is `ink`. Unavailable, Restricted, Insufficient and On cooldown labels are `ink-muted`: they are readable, since the player may act soon. A Locked thing's name and a Disabled control's label are `ink-disabled`. Both are inactive, so WCAG exempts them from contrast, but the word "Locked" and the unlock condition beside them are `ink-muted` or `ink` and must pass.

## The reason tip

The reason tip is how a blocked control explains itself.

- **When it shows.** Beside the control on hover and on keyboard focus. A click or tap pins it, so touch can read it. Pressing again, Escape, moving away or tabbing on closes it, and it can be hovered without closing (WCAG 1.4.13).
- **What it says.** The state's word in label capitals where the state has one ("Locked"), then the reason as a short phrase in sentence case: "Unlocks at level 20". A shortfall is in `warning`; every other reason is `ink`.
- **What it looks like.** A float: `surface-raised`, a `line-strong` hairline, `radius-float`, `shadow-float`. It sits on `z-popover`, or `z-popover-detached` when opened from a dialog. It is drawn on the page's top layer, so a scrolling list or the rail never clips it. It arrives over `duration-fast` on `ease-enter` and leaves on `ease-exit`.
- **What screen readers hear.** The same words, as the control's description, on focus. When the control is pressed, they hear them again (polite).
- **One at a time.** Opening one closes any other.
- **When it isn't needed.** A reason already printed beside the control (a LoadoutSlot's "Unlocks at level 20") is the description itself, and no tip repeats it.

Button, EntryList, NavRail, Sigil and ItemSlot build it in. `LL.why` gives it to anything else.

## Interaction

What the player is doing to it now. These states come and go as the pointer, keyboard and finger move.

| State | Means | Channel | Tokens | Words | Screen readers hear |
| --- | --- | --- | --- | --- | --- |
| Default | At rest: nothing is happening to it. | None — the part's own look | The part's own: `surface`, `line-strong`, `ink` | None | Its name and role: "Equip, button" |
| Hover | The pointer is over something that will respond to a press. | Fill: a neutral wash layer that fades in. Button's `gilt` edge is a known exception (Foundations · Colour · Not yet on the allocation) | `surface-raised`, `duration-fast` | None. A tooltip says what it does, never something found nowhere else | Nothing |
| Focus-visible | The keyboard is on it, and Enter or Space will act on it. | Edge: a 2px `ground` gap, then a solid 2px `focus` ring. A focused row rises above its neighbours | `focus-ring`, `focus`, `z-raised` | None | Its name, role, state and description: "Sell, button, unavailable. Not while in a dungeon." |
| Pressed | Being pressed, for as long as the press lasts. | Position: a 1px drop at once, back over `duration-fast`; the hover layer stays | `border-hairline`, `duration-instant`, `duration-fast` | None | Nothing; the result is announced. A toggle that stays on is Selected |
| Selected | Chosen among its peers: what the Folio shows, or what the next action applies to. | Edge: a solid `arcana-glow` bar, underline or ring. Typeface: weight 600 where the face has weights (Marcellus lists rely on the bar) | `arcana-glow`, `border-emphasis`, `surface-raised` | None. With several chosen: "3 selected" | "selected" (`aria-selected` in a list, tabs or grid) or "pressed" (`aria-pressed` on a toggle) |
| Current | Where the player is: the screen, step, floor or turn they are on. | Marker: a small `gilt` diamond; on the NavRail, a 2px `gilt` bar at the item's start and a `gilt` wash (D-118). Typeface: 600, and a `gilt` icon. Gilt's "current location" job, never selection (D-015) | `gilt`, type style `nav-active` | None | "current page" or "current step" (`aria-current`) |
| Dragging | Being moved by the player; it lands where it is dropped. | Opacity: the origin stays in place at `opacity-drag-origin`, so nothing reflows. Edge: the copy under the pointer lifts with `shadow-float`; a valid target takes a solid `arcana-glow` edge | `opacity-drag-origin`, `shadow-float`, `z-drag`, `arcana-glow` | Over a target that won't take it, the reason in a tip: "Not a weapon slot" | "Grabbed Ashen Blade. Arrow keys to choose a slot, Enter to drop, Escape to cancel." Then "Dropped in Main hand." |
| Disabled | Can't be used, and why is plain from what sits beside it. Rare. | Text: the label in `ink-disabled`. Edge: `line` | `ink-disabled`, `line` | None. If it needs words, it is Unavailable | Nothing: it is out of the Tab order (native `disabled`). Read in order: "Previous, button, unavailable" |

Every drag has a way to do the same without dragging: a button or a menu item ("Equip", "Move to Preset 2").

## Availability

Whether the player can do it now, and if not, what stands in the way. Every state here except Available and Ready blocks. A blocked control stays focusable, says why, and answers a press with its reason.

| State | Means | Channel | Tokens | Words | Screen readers hear |
| --- | --- | --- | --- | --- | --- |
| Available | It can be done now. | None — the default look | The part's own | None | Its name and role |
| Ready | It is available and waiting for the player: a point to spend, a stat that can be raised, an Essence that can be attuned. | Marker: a small `arcana-glow` diamond — on a Sigil, and in the attention place of a slot, row or rail item (Standards · State combinations) — or an `arcana` count badge (NavRail) | `arcana-glow`, `arcana`, `arcana-soft` | "Can be raised", "1 point to spend"; a count in the rail | "Strength 24, can be raised"; "Character, 1 point to spend" |
| Unavailable | It can't be done right now, for a reason that will pass or that the player can change, and it says which. | Text: the label in `ink-muted`. Edge: `line`. No hover layer and no press. The reason in the reason tip | `ink-muted`, `line`; the tip's float tokens | The reason, as a short phrase: "Not while in a dungeon", "Already listed", "Inventory full", "Choose a target first" | "Sell, button, unavailable. Not while in a dungeon." |
| Locked | A progression gate: the player hasn't reached it yet, and it says how to unlock it. | Text: "Locked" and the condition. Edge: a dashed `line` on framed parts. Icon: the 12px `lock` marker (D-113) where a part shows it — the NavRail; elsewhere the word stands alone until the part takes the marker | `ink-disabled` for its name; `ink-muted`, `line`, `icon-marker` | "Locked", then the condition: "Unlocks at level 20", "Clear Floor 10 to unlock", "Unlocks after The Ashen Gate" | "Arena, link, unavailable. Locked. Unlocks at level 20." |
| Restricted | The player's account or guild role doesn't allow it, and playing on won't change that. | Text: the label in `ink-muted`; who may, in the tip. Edge: `line`. Icon: the crown, for Nobility only | `ink-muted`, `line`; the Nobility mark's tokens | Who may: "Officers only", "Leader only", "Requires Nobility" | "Withdraw, button, unavailable. Officers only." |
| Insufficient resources | The player lacks part of a cost, and it shows exactly what is missing. | Text: the shortfall in `warning` beside the cost. The committing control is blocked, with the shortfall as its reason | `warning`, `ink-muted`, `line` | "Short by 250 Cinders"; with two: "Short by 250 Cinders and 40 Essence Dust". The cost line reads have / need: "1,000 / 1,250" | "Upgrade, button, unavailable. Short by 250 Cinders." |
| On cooldown | Used recently; it comes back by itself at a known time. | Text: the time left in tabular `ink-muted` figures, in the tip or beside the label. The label stays | `ink-muted`, `line`, `numeral-row` | "Ready in 4m 12s"; under a minute, "Ready in 12s". Two units at most (Foundations · Numerals) | "Teleport, button, unavailable. Ready in 4 minutes 12 seconds." Read on focus, never as it ticks |

**Which reason shows first.** When several stand in the way, show them in this order: Locked, Restricted, Unavailable, Insufficient, On cooldown. The order runs from the gate furthest back to the nearest: whether the feature has been reached, who may use it, what is happening now, what it costs, when it is next free. When two must both be cleared, the tip gives the second on its own line: "Unlocks at level 20" then "Officers only".

**Ready is still Available.** It adds a marker to say that something is waiting. Ready is the one Availability state in `arcana`, because arcana means "waiting for you".

## Lifecycle

Where a thing is in its life, from arriving to ending.

| State | Means | Channel | Tokens | Words | Screen readers hear |
| --- | --- | --- | --- | --- | --- |
| New | It arrived since the player last looked. | Text: a `new` Tag after the name | `arcana`, `arcana-soft` | "+ New", or with a noun: "+ New quest". The + is drawn, not read | "Blackjaw Spider, new". It clears once the player opens or selects it |
| Unread | Messages or entries the player hasn't read, counted. | Marker: a count badge. Typeface: an unread thread's name at 600 | `arcana` on `arcana-soft` in the rail; `ink` on `surface-raised` in the Chronicle, where channels own hue | The count: "3"; above 99, "99+" | "Mail, 3 unread" |
| In progress | Started and not finished. | Fill: a Meter or Track. Text: the progress as a fraction | `meter-xp`, `meter-track`, `ink-muted` | "In progress", with the fraction "3 / 5" | "In progress, 3 of 5" |
| Completed | Its goal is met, and nothing is waiting to be collected. | Text and icon: ✓ and the word in `ink` (a `success` Tag) | `success` | "✓ Completed", or the game's own verb: "✓ Floor cleared", "✓ Upgraded" | "…, completed" |
| Claimable | Its goal is met and a reward is waiting for the player to collect. | Text: a `new` Tag, with "Claim" as the row's committing action. Marker: an `arcana` badge where claimables are counted; on a slot, the attention diamond | `arcana`, `arcana-soft` | "Claimable"; the action, "Claim" or "Claim all" | "…, claimable". Once claimed: "Claimed 120 Cinders" |
| Claimed | Its reward has been collected; the record stays. | Text: a neutral Tag, and the reward in `ink-muted` | `ink-muted`, `line` | "Claimed" | "…, claimed" |
| Opened | A container or message that has been opened, so what is inside is known. | Text: a neutral Tag, or the name in `ink-muted` with its contents listed | `ink-muted` | "Opened" | "…, opened" |
| Expiring soon | It will end, or be lost, within a short and known time: under a day, unless the screen sets its own threshold. | Text: the time left in `warning` | `warning` (`ink` inside item, combat and chat contexts) | "Expires in 2h", "Ends in 14m" | "…, expires in 2 hours" |
| Expired | Its time ran out; it can no longer be used or claimed. | Text: a neutral Tag, and what became of it in `ink-muted` | `ink-muted`; `danger` only when the player lost something | "Expired"; with the outcome: "Expired · Returned to your inventory", "Expired · Reward lost" | "…, expired. Returned to your inventory." |
| Failed | It ended without meeting its goal, and says why. | Text and icon: ✕ and the word in `danger`, with the reason | `danger` | "✕ Failed", then the reason: "Party defeated on Floor 7", "Time ran out" | "…, failed. Party defeated on Floor 7." |

**The arc.** In progress ends in Completed (no reward, or one granted at once), Claimable then Claimed, or Failed. Claimable becomes Expired if it has a deadline and the player misses it — a loss, so "Reward lost" is `danger`.

## Ownership and use

Whose a thing is and what it is doing. These states belong to items, Essences and other things the player can hold.

| State | Means | Channel | Tokens | Words | Screen readers hear |
| --- | --- | --- | --- | --- | --- |
| Owned | The player has it. It is marked only where things they don't have also appear: a collection, a codex, a shop. | Text: a neutral word or count. Nothing in the player's own inventory | `ink-muted` | "Owned", "Owned ×3" | "…, owned" |
| Not owned | The player doesn't have it; seen in collections, codices and shops. | Opacity: its art at `opacity-unowned`. Text: its name in `ink-muted` | `opacity-unowned`, `ink-muted` | "Not owned" | "…, not owned" |
| Equipped | Worn or wielded by the player's character now, so it counts in their stats. | Text: a neutral Tag, or the meta line in a slot grid. Marker: on a slot, the in-use square in the bottom start corner, where equipped and unequipped things appear together | `ink-muted`, `line` | "Equipped" | "…, equipped" |
| Attuned | An Essence bound to a loadout slot; its abilities are active. Equipped, for Essences. | Text: a neutral Tag. Marker: on a slot, the in-use square, as for Equipped | `ink-muted`, `line` | "Attuned" | "…, attuned" |
| Assigned | Spoken for by a preset or an activity, though it is still the player's. | Text: a neutral Tag or a meta line naming what holds it | `ink-muted`, `line` | "In Preset 2", "On expedition", "Assigned to Mining" | "…, assigned to Mining" |
| Captured | Copied into an activity's snapshot, such as a defence team or an arena entry, so changing it now doesn't change the snapshot. | Text: a neutral Tag, and when it was captured | `ink-muted` | "In defence snapshot"; in the Folio, "Snapshot from 14:02. Changes apply at the next snapshot." | "…, in defence snapshot" |
| Listed | On sale on the Cinder Bazaar until it sells or is withdrawn. | Text: a neutral Tag with the price | `ink-muted`, `line` | "Listed", with the price "1,200 Cinders" | "…, listed for 1,200 Cinders" |
| In escrow | Held by a pending trade, order or bid; neither side can use it until it settles. | Text: a neutral Tag | `ink-muted`, `line` | "In escrow"; for Signets or Cinders held by an open order, "Reserved" | "…, in escrow" |
| Borrowed | Taken from the guild Vault: the player may use it, but it belongs to the guild and goes back. | Text: a neutral Tag; the Vault in the meta line | `ink-muted`, `line` | "Borrowed"; the meta, "From the Ashen Order Vault" | "…, borrowed from the Ashen Order Vault" |
| Favourite | Marked by the player to keep, so bulk sell, salvage and discard pass over it. Also called protected. | Marker: the 12px bookmark ribbon in the slot's bottom start corner, in `ink` (Standards · State combinations). Until it is drawn, the word in the meta line | `ink`, `icon-marker` | "Favourite". A bulk action reports it: "3 favourites kept" | "…, favourite" |

**What these states block.** Listed, In escrow and Borrowed stop the item being sold or listed again. Listed and In escrow stop it being equipped too. Assigned to an activity stops anything else using it until the activity ends. The blocked actions are Unavailable, with the state as the reason: "Listed on the Bazaar", "In escrow", "Borrowed from the guild Vault", "Assigned to Mining". A preset only points at an item, so being in a preset blocks nothing.

## Knowledge

How much the player knows about a thing. Here the interface keeps secrets as the game requires.

| State | Means | Channel | Tokens | Words | Screen readers hear |
| --- | --- | --- | --- | --- | --- |
| Discovered | Found: its name and details are known. | None — the default look | The part's own | None. The first time, "+ New" | Its name, as usual |
| Undiscovered | It has a known place, such as a codex entry or a recipe in a list, but the player hasn't found it, so its name and details are withheld. | Fill: an empty frame or a silhouette, no art. Text: the word in `ink-muted`, and a hint if the game gives one | `ink-muted`, `line`, `surface` | "Undiscovered"; a hint: "Found on Floors 10–20"; a total: "12 of 40 discovered" | "Undiscovered". Never "question mark question mark" |
| Hidden | Not to be known yet, not even that it is there: an unrevealed dungeon room. | None: it is not drawn, not counted and not focusable. A map shows unlit ground, not a placeholder | `ground-deep` | None. Totals count only what is known: "7 rooms explored", not "7 / 12" | Nothing |
| Unknown | A value the game doesn't know, or one that doesn't apply. | Text: an em dash where the value would be | `ink` | "—". Zero is "0", never "—" (Foundations · Numerals) | "unknown", or "not applicable" |

**Hidden data is not in the page at all**: not in the markup, a tooltip or the server's response. Players read all three.

## Data

How complete and how fresh is what is on screen. These states belong to regions, lists, fields and the values in them.

| State | Means | Channel | Tokens | Words | Screen readers hear |
| --- | --- | --- | --- | --- | --- |
| Loading | Being fetched for the first time, with nothing to show yet. | Fill: still blocks in the shape of what is coming, shown after 300ms. Text: what is loading, after a second | `surface-raised`, `ink-muted` | "Loading members…" | The region is `aria-busy`. If the wait passes a second: "Loading members" |
| Refreshing | Already shown and being brought up to date; what is there stays usable. | Text: a small "Updating…" in the region's head, in room kept for it. Values that change take the live-update mark | `ink-muted`, `changed` | "Updating…" | Nothing. If the player asked for the refresh, its outcome: "Bazaar updated, 3 new listings" |
| Pending save | Changed by the player, and not yet confirmed by the game. | Text: "Saving…" beside the control in room kept for it, then "Saved" for `duration-reveal`. A committing Button shows its progressive label and ignores a second press | `ink-muted`, `duration-reveal` | "Saving…", then "Saved"; on a Button, its own verb: "Listing…", "Crafting…", "Claiming…" | The Button is `aria-busy`. "Saved" is announced politely; "Saving…" is not |
| Stale | Shown from an earlier fetch that may no longer be true, and it says how old. | Text: the age in `ink-muted`, with a quiet Refresh. The values stay `ink`: they are still the best known | `ink-muted` | "Updated 5m ago"; after a day, "As of 3 Oct" | The age, as part of the region's description |
| Error | It couldn't load or save, and it says what to do next. | Text and icon: ✕ and the message in `danger`, with an action. An inline alert may sit on `danger-soft` | `danger`, `danger-soft` | "Couldn't load the roster. Try again."; for a save, "Couldn't save. Your change was undone." | A failed save the player made: assertive. A region that failed to load: polite |
| Empty | Loaded, with nothing in it, and it says what would fill it. | Text in `ink-muted` with a next step. A slot shows its dashed inner frame and its label | `ink-muted`, `line` | "No listings yet. List an item to sell it." When a filter hides everything: "No matching players", with "Clear filters" | The sentence |
| Offline | The connection to the game dropped: what is shown is held, and actions wait. | Text: a notice under the TopBar in `warning`. Actions that need the server become Unavailable with the same words | `warning`; `success` when it returns | "Reconnecting…", then "Back online". After a minute: "Can't reach the game. Retrying in 30s." | "Connection lost. Reconnecting." (assertive), then "Back online." (polite) |

**Empty is not "no results".** Empty means nothing exists yet, so it offers a way to make something. No results means a filter or search hid everything, so it offers a way to undo the filter.

## Telling them apart

### Disabled, unavailable, locked, insufficient

The four are easy to confuse because they all stop a press. Restricted and On cooldown are given alongside them for completeness.

| | Disabled | Unavailable | Locked | Insufficient | Restricted | On cooldown |
| --- | --- | --- | --- | --- | --- | --- |
| What stops it | Nothing to act on; it is at a limit | The situation | Progress not yet made | A cost not yet met | The player's role or account | Time |
| The player clears it by | Nothing; it is a limit | Changing what they are doing | Playing on | Gathering what is missing | Gaining the role, or Nobility | Waiting |
| Looks | Label in `ink-disabled`, `line` edge | Label in `ink-muted`, `line` edge | Name in `ink-disabled`, dashed edge, "Locked" | The shortfall in `warning` beside the cost | Label in `ink-muted`, `line` edge | The time left |
| Reason given | None; it is obvious | Why | How to unlock it | What is missing, and how much | Who may | When |
| Focus | Out of the Tab order (`disabled`) | In it (`aria-disabled`) | In it | In it | In it | In it |
| A press | Does nothing | Shows and announces the reason | Shows the condition | Shows the shortfall | Shows who may | Shows the time left |
| Example | Previous on page 1 | Sell, while in a dungeon | The Arena, before level 20 | Upgrade, 250 Cinders short | Withdraw from the Vault, as a Member | Teleport, 4m 12s left |

When in doubt, it is Unavailable: name the reason. Disabled is the exception that needs no reason.

### Completed, claimable, claimed

| | Completed | Claimable | Claimed |
| --- | --- | --- | --- |
| Goal met | Yes | Yes | Yes |
| Reward | None, or granted already | Waiting | Collected |
| The player's next step | None | Claim it | None |
| Looks | ✓ and the word: a `success` Tag | A `new` Tag, a Claim button, a badge where counted | A neutral Tag, the reward in `ink-muted` |
| Words | "✓ Completed" | "Claimable"; "Claim" | "Claimed" |
| Becomes | — | Claimed, or Expired if the deadline passes | — |
| Colour, and why | Success: it has happened | Arcana: it is waiting for the player | Neutral: it is history |

A reward that waits is never shown as Completed, so the player never mistakes an unclaimed reward for a finished one.

### Owned, equipped, assigned, captured

| | Owned | Equipped | Assigned | Captured |
| --- | --- | --- | --- | --- |
| Means | The player has it | It is on the character now | A preset or an activity is holding it | It is copied into an activity's snapshot |
| Counts in stats now | No, unless equipped | Yes | Only in that activity | Only in that snapshot |
| Free for other uses | Yes | Swap or unequip it first | In a preset, yes; in an activity, no — other actions are Unavailable: "Assigned to Mining" | Yes; the snapshot keeps its copy |
| Shown | Only in catalogues: "Owned" | "Equipped" | "In Preset 2", "Assigned to Mining" | "In defence snapshot" |
| Combines with | Every other state here | Captured, Favourite | Favourite | Equipped, Assigned, Favourite |
| Example | A sword in the bag | That sword in Main hand | That sword in Preset 2, or on an expedition | That sword in the defence team captured at 14:02 |

Attuned is Equipped, for Essences. Listed, In escrow and Borrowed are about who may use a thing, not where it is.

### Other pairs that get mixed up

- **Selected, current, equipped.** Selected is what the player has chosen to look at (an `arcana-glow` bar). Current is where the player is (a `gilt` diamond; on the NavRail, a `gilt` bar and wash, D-118). Equipped is what the character wears (the word). An equipped sword can be selected on the inventory screen, which is current in the rail.
- **New, unread, claimable.** New marks a thing that arrived. Unread counts messages inside something. Claimable marks a reward waiting. All three are arcana, since each is waiting for the player, so each also has its own word or count.
- **Pressed and selected.** Pressed lasts as long as the finger is down. A toggle that stays on is Selected, which `aria-pressed` reports.
- **Hover and focus.** Hover is a wash and is never announced. Focus is a ring and is always visible.
- **Undiscovered, hidden, unknown.** Undiscovered has a place but no name. Hidden has neither. Unknown is a value, not a thing.
- **Loading, refreshing, pending, stale.** Loading has nothing yet. Refreshing has something and is getting more. Pending is the player's change, not yet confirmed. Stale is old and says how old.
- **Expired and failed.** Expired ran out of time. Failed ran out of chances, and says why.
- **Offline and a player's presence.** Offline is the player's own connection, in Data. A friend shown as offline is Presence, which is not a state of this model.

## Combining states

Standards · State combinations holds these rules (D-094): which channel each kind of state takes, at most four marks on one thing, where each mark sits on slots, rows and rail items, the Tag order, and what drops first when there is no room. Two rules stay here because they are about single states:

- **Blocked and selected.** A blocked thing can't become selected. A screen that needs to show a locked thing's details, such as a dungeon's floors before it opens, makes the entry available with a "Locked" Tag, and makes the Folio's committing action the blocked control.
- **Blocked and focused.** The focus ring draws as usual, and the reason tip opens beside it.
- **Pending and anything.** The new value shows at once, with "Saving…" beside it. If the save fails, the old value returns, with an Error that says so.

## Announcements

`LL.announce` speaks a state change once, politely unless marked, and never repeats the same words within five seconds.

| Change | Announce | How | Words |
| --- | --- | --- | --- |
| The player pressed a blocked control | Yes | Polite | Its reason: "Locked. Unlocks at level 20." |
| A save the player made is confirmed | Yes | Polite | "Saved"; or the outcome: "Listed for 1,200 Cinders" |
| A save the player made failed | Yes | Assertive | "Couldn't save. Your change was undone." |
| A reward was claimed | Yes | Polite | "Claimed 120 Cinders" |
| Something was equipped or attuned | Yes | Polite | "Equipped Ashen Blade" |
| A cooldown ends while the player waits on it | Yes | Polite | "Teleport ready" |
| The connection drops, or returns | Yes | Assertive, then polite | "Connection lost. Reconnecting.", then "Back online." |
| A wait the player started passes a second | Yes | Polite | "Loading members" |
| New things arrive in the list the player is in | Once in five seconds | Polite | "3 new listings" |
| Hover, focus moving, a countdown ticking, a background refresh | Never | — | — |

## Which parts take which states

The matrix says which states each kind of part can show. ● The part shows the state itself, as this page describes. ○ The state appears through a Tag, a marker or text inside the part. — The state doesn't apply. Each component's page says which of its states are built so far.

| Category | Components |
| --- | --- |
| Commands | Button |
| Toggles and tabs | TabStrip, toggle Buttons |
| Navigation | NavRail, ItemLink, the TopBar menu |
| Lists and rows | EntryList, List and ListRow, Ledger rows, Chronicle lines |
| Slots | ItemSlot, LoadoutSlot |
| Stat marks | Sigil, Constellation, StatTile, Meter, Track, LevelPlate |
| Labels | Tag, Presence |
| Values | Ledger values, StatFigure, CurrencyPill, Delta, table figures |
| Regions | Panel, Folio, Page, Stage, Chronicle, JourneyCard |
| Fields | SearchField, inputs, the Chronicle's composer |

| State | Commands | Toggles and tabs | Navigation | Lists and rows | Slots | Stat marks | Labels | Values | Regions | Fields |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| **Interaction** | | | | | | | | | | |
| Default | ● | ● | ● | ● | ● | ● | ● | ● | ● | ● |
| Hover | ● | ● | ● | ● | ● | ● | — | — | — | ● |
| Focus-visible | ● | ● | ● | ● | ● | ● | — | — | — | ● |
| Pressed | ● | ● | — | — | — | — | — | — | — | — |
| Selected | — | ● | — | ● | ● | ● | — | — | — | ● |
| Current | — | — | ● | — | — | ● | — | — | — | — |
| Dragging | — | — | — | ● | ● | — | — | — | ● | — |
| Disabled | ● | ● | — | — | — | — | — | — | — | ● |
| **Availability** | | | | | | | | | | |
| Available | ● | ● | ● | ● | ● | ● | — | — | — | ● |
| Ready | — | ○ | ○ | ○ | ○ | ● | ● | — | ○ | — |
| Unavailable | ● | ● | ● | ● | ● | ● | — | — | ○ | ● |
| Locked | ● | ● | ● | ● | ● | ● | ● | — | ○ | — |
| Restricted | ● | ● | ● | ● | — | — | ● | — | ○ | ● |
| Insufficient resources | ● | — | — | ○ | — | — | ● | ● | — | — |
| On cooldown | ● | — | — | ○ | ● | — | ● | — | — | ● |
| **Lifecycle** | | | | | | | | | | |
| New | — | ○ | ○ | ○ | ○ | — | ● | — | ○ | — |
| Unread | — | ● | ● | ● | — | — | — | — | ○ | — |
| In progress | — | — | — | ○ | ○ | ● | ● | ● | ○ | — |
| Completed | — | — | — | ○ | ○ | ● | ● | — | ○ | — |
| Claimable | — | ○ | ○ | ○ | ○ | — | ● | — | ○ | — |
| Claimed | — | — | — | ○ | ○ | — | ● | — | ○ | — |
| Opened | — | — | — | ○ | ○ | — | ● | — | ○ | — |
| Expiring soon | — | — | — | ○ | ○ | — | ● | ● | ○ | — |
| Expired | — | — | — | ○ | ○ | — | ● | — | ○ | — |
| Failed | — | — | — | ○ | — | — | ● | — | ○ | — |
| **Ownership and use** | | | | | | | | | | |
| Owned | — | — | — | ○ | ○ | — | ● | — | ○ | — |
| Not owned | — | — | — | ● | ● | — | ● | — | — | — |
| Equipped | — | — | — | ○ | ○ | — | ● | — | ○ | — |
| Attuned | — | — | — | ○ | ● | — | ● | — | — | — |
| Assigned | — | — | — | ○ | ○ | — | ● | — | — | — |
| Captured | — | — | — | ○ | ○ | — | ● | — | ○ | — |
| Listed | — | — | — | ○ | ○ | — | ● | — | — | — |
| In escrow | — | — | — | ○ | ○ | — | ● | — | — | — |
| Borrowed | — | — | — | ○ | ○ | — | ● | — | — | — |
| Favourite | — | ● | — | ● | ● | — | — | — | — | — |
| **Knowledge** | | | | | | | | | | |
| Discovered | — | — | — | ● | ● | ● | — | — | ● | — |
| Undiscovered | — | — | — | ● | ● | ● | ● | — | ○ | — |
| Hidden | — | — | — | — | — | — | — | — | ● | — |
| Unknown | — | — | — | ○ | — | ● | — | ● | — | — |
| **Data** | | | | | | | | | | |
| Loading | — | — | — | ● | ● | ○ | — | ● | ● | ● |
| Refreshing | — | — | — | ● | — | — | — | ● | ● | — |
| Pending save | ● | ● | — | ○ | ○ | — | — | ○ | ○ | ● |
| Stale | — | — | — | ○ | — | — | — | ○ | ● | — |
| Error | ○ | — | — | ○ | — | — | — | — | ● | ● |
| Empty | — | — | — | ● | ● | — | — | — | ● | ● |
| Offline | ● | — | — | — | — | — | — | — | ● | ● |

Labels show their states only as words, and Values show theirs only as figures, so neither takes hover or focus. A Tag is never a button (Standards · Content).

## Tokens and code

| Name | What it is |
| --- | --- |
| `opacity-unowned` | 0.4. The art of a thing the player doesn't own. Art only, never text |
| `opacity-drag-origin` | 0.4. The place a dragged thing left, which keeps its room while the copy moves |
| `LL.states` | Every state on this page with its family, word, Tag tone and what screen readers hear. Tag's `state` reads it, so a state's words are the same everywhere |
| `LL.topState(states)` | The one Tag a row shows when several states apply, by the Tag order in Standards · State combinations |
| `LL.why(id, reason, options)` | The reason tip for any focusable element. It returns the props to spread on the element and the description node to render beside it. `options.word` is the state word ("Locked"); `options.tone: 'warning'` is for a shortfall |
| `LL.format.duration(seconds)` | "4m 12s", in two units at most (Foundations · Numerals) |
| `LL.format.spokenDuration(seconds)` | "4 minutes 12 seconds", for screen readers |
| Button `state`, `reason`, `shortfall`, `remaining`, `pendingLabel` | `unavailable`, `locked`, `restricted`, `insufficient`, `cooldown` or `pending`. `shortfall` writes "Short by 250 Cinders"; `remaining` writes "Ready in 4m 12s" |
| Tag `state` | Any state with a word, such as `locked`, `claimable`, `equipped` or `expiring` |
| ItemSlot `state`, `reason`, `favourite`, `ready` | A blocked state with its reason printed under the name, Not owned, Undiscovered, or a word state that leads the meta line; Equipped and Attuned also take the in-use square; `favourite` is the ribbon marker, or its word until it is drawn; `ready` is the attention diamond (Standards · State combinations) |
| EntryList and NavRail items `locked`, `reason`, `ready` | A locked entry or rail item, with how it unlocks; `ready`, the attention diamond |
| LoadoutSlot `reason` | A locked slot's condition, printed as its name |
| Sigil `reason` | The unlock condition of a locked stat |

## Do and don't

| Do | Don't |
| --- | --- |
| "Sell" stays in reach and says "Not while in a dungeon." | Grey "Sell" out and leave the player guessing. |
| "Locked · Unlocks at level 20" | "Locked", alone — or hiding the Arena until level 20. |
| "Short by 250 Cinders", beside the cost | "Not enough resources", or a red cost |
| "Claimable", in arcana, with a Claim button | A green ✓ on a reward still waiting |
| "Equipped" as the one Tag, the favourite ribbon beside it | Equipped, Favourite, In Preset 2 and New as four Tags |
| "Undiscovered · Found on Floors 10–20" | "???" |
| Keep the old rows in place and mark the changed price | Blank the Bazaar list while it refreshes |
| "Couldn't save. Your change was undone." | "Error 500" |

## Related components

Button, Tag, ItemSlot, LoadoutSlot, EntryList, NavRail, Sigil, TabStrip, List and ListRow, Ledger, Meter, Track, Chronicle, SearchField, Presence, Folio.
