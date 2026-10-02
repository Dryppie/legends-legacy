# Foundations · Motion

Motion is quiet. In a realtime game it has three jobs: confirm what the player did, show what changed, and keep everything else where it was. It never performs. Five durations and three easings, all motion tokens, cover six categories, each with its rules and its reduced-motion alternative. Only `transform` and `opacity` animate, nothing bounces, and only two things may loop.

## Rules

**Must**
- Take every duration and easing from the motion tokens. No raw milliseconds or curves in component styles.
- Animate only `transform` and `opacity`. Colours, edges, fills, shadows, sizes and positions change at once. A wash that should fade is a layer whose opacity fades (Properties, below).
- Keep live updates from moving anything: no layout shift, and never move the control the player is about to click. A refreshed Bazaar list or guild roster keeps the selection and the scroll position (Live updates, below).
- Give every category its reduced-motion alternative. Under `prefers-reduced-motion: reduce`, or `data-motion="reduced"` on an ancestor, every transition and animation ends at once (Reduced motion, below).
- Let only an indeterminate progress indicator and live combat playback loop (Loops, below).

**Should**
- Show a value that changes on screen with `lgLive()`, and a list that refreshes with the `[lgLiveList]` directive.
- Check every new screen by the Audit below.

**Never**
- Bounce, overshoot, spring, wobble or shake.
- Loop anything else: no pulse, breathing, shimmer, sweep or idle sway.
- Animate width, height, position, margin, padding, colour, a border, a shadow, a filter or a number's width.
- Hold up input. A control works from its first frame, whatever around it is still moving.
- Run a transition longer than `duration-reveal`.

## Tokens

`tokens.json` · `motion`. The tokens keep their values under reduced motion; the system ends the transitions instead.

| Token | Value | For |
| --- | --- | --- |
| `duration-instant` | 0ms | No transition: a press going down, a selection or toggle landing, the current location moving, a drag, a drawer collapsing into its bar. What every category becomes under reduced motion. |
| `duration-fast` | 140ms | Feedback and state: a hover wash, a press releasing, a Sigil settling, a tooltip, a live Meter. And anything leaving. |
| `duration-base` | 220ms | Spatial, arriving: the rail drawer and its scrim, the Chronicle's body, a sheet, content a toggle opens (`lg-enter`). |
| `duration-slow` | 400ms | Value change: a Meter's step, a number counting after combat or a purchase. The live mark's fade. |
| `duration-reveal` | 600ms | A reward or result appearing, once per result. How long the live mark holds. The longest duration. |

| Token | Value | For |
| --- | --- | --- |
| `ease-standard` | `cubic-bezier(0.4, 0, 0.2, 1)` | Something changing where it stands: a wash, a selection settling, a fill, a count, the mark's fade. |
| `ease-enter` | `cubic-bezier(0, 0, 0.2, 1)` | Something arriving: a drawer, a popover, a reveal. It decelerates into place. |
| `ease-exit` | `cubic-bezier(0.4, 0, 1, 1)` | Something leaving, always at `duration-fast`: a drawer closing, a popover going. It accelerates away. |

**No easing overshoots.** Every control point lies between 0 and 1, so nothing passes its end and comes back. **Leaving is quicker than arriving:** in over `duration-base`, out over `duration-fast`.

**Distances.** What arrives in place rises `space-2` (8px) as it fades in; a popover moves `space-1` (4px) into place. A drawer travels its own width from its edge. A press sinks `border-hairline` (1px). A selected Sigil grows to 1.08, and 1.06 on hover. Nothing turns as it moves.

Script reads the same values from `LG_DURATION` and `LG_EASING` in `src/app/grimoire/tokens/tokens.ts`, which the token compiler writes from `tokens.json`; `lgMotionMs('reveal')` reads the live `--lg-duration-reveal` when `tokens.css` is loaded.

## Categories

| Category | Covers | Duration | Easing | What animates | Reduced motion |
| --- | --- | --- | --- | --- | --- |
| **Feedback** | Hover, press | `fast`; a press goes down `instant` | `standard` | The opacity of a wash or edge layer; the press's 1px `transform` | The wash, edge and press at once |
| **State change** | Select, toggle | The mark `instant`; a Sigil's scale `fast` | `standard` | `transform`: the Sigil's scale | The mark and the size at once |
| **Value change** | A Meter's fill; a number after combat or a purchase | `slow`; a live Meter `fast` | `standard` | `transform`: the fill's `scaleX`. A count is text | The fill and number at once, with the live mark if it helps |
| **Spatial** | Drawers: the rail drawer, the Chronicle drawer | In `base`, out `fast`; collapsing `instant` | `enter`, `exit` | `transform` and `opacity` | At once, in the final place |
| **Reveal** | A reward or result appearing | `reveal` | `enter` | `opacity` and a `space-2` rise | The whole result at once |
| **Live-update highlight** | A changed value, briefly marked | On `instant`, held `reveal`, off `slow` | `standard` | The opacity of the `changed` wash | On at once, off at once after the hold |

### Feedback: hover and press

- **Hover shows over `duration-fast`** as a layer whose opacity fades: the `surface-raised` wash on NavRail items, the `gilt` edge on a primary or danger Button, the solid Button's fill 6% brighter, EntryList's wash. Text turns `ink` at once.
- **A press goes down at once** (`duration-instant`) and comes back over `duration-fast`. The Button sinks `border-hairline`. Nothing scales, ripples or lights up.
- **Focus is never animated.** The focus ring is there in the frame focus arrives (Foundations · Accessibility).
- **Reduced motion:** the wash, the edge and the sink happen at once.

### State change: select and toggle

- **The new state lands in the frame of the press:** the selection ring, bar or edge, `aria-pressed`, the toggled label. The old state clears in the same frame, so two things never look selected at once. A selection that lags reads as a missed click.
- **One transform may settle over `duration-fast`:** a selected Sigil grows to 1.08. No indicator travels between items, and no tab bar slides.
- **Content a toggle opens** ("Show perks") is placed at once and rises in over `duration-base` (`lg-enter`: `space-2` and opacity). Nothing slides open, and no height animates. It closes at once.
- **The current location moves at once:** the NavRail's active item with its gilt bar and wash, the Track's current diamond. A change of place is never animated.
- **Reduced motion:** the mark and the size at once.

### Value change: a Meter's fill, and numbers after combat or a purchase

- **A Meter's fill is scaled, never resized.** A step — experience after a fight, a potion — moves over `duration-slow`. A Meter fed by combat playback (`live`) follows each tick over `duration-fast`, so it is never more than a tick behind its number.
- **The printed value is the truth** and changes at once. The fill catches up.
- **A number the player caused to change counts** to its new value over `duration-slow` on `ease-standard`: experience and loot totals after combat, the currency after a purchase. `lgLive(() => value, () => ({ cause: 'player' }))`. A new change mid-count carries on from where the count is.
- **A number that changes by itself never counts.** It changes at once and takes the live mark (below).
- **A count is a text change, not an animated property.** It runs in tabular figures, inside a width reserved for the widest value, in a fixed format (Foundations · Numerals · Live values), so it never moves a neighbour.
- **Reduced motion:** the fill and the number change at once. The live mark may note the change.

### Spatial: drawers

- **An overlay drawer** — the rail drawer under 60rem, a sheet — slides in from its edge by its own width over `duration-base` on `ease-enter`, with its scrim fading in over the same time. It leaves over `duration-fast` on `ease-exit`. Once it has left it is hidden, so nothing off-screen can take focus.
- **A drawer that collapses into a bar** — the Chronicle, docked, floating or the bottom dock — opens with its body rising `space-2` and fading in over `duration-base` on `ease-enter`. It closes at once: the frame becomes its bar in one frame, since height never animates.
- **Dragging follows the pointer with no transition.** The floating Chronicle's arrow-key nudges (16px) land at once.
- **Focus moves in the frame the drawer opens,** and returns when it closes (GameShell).
- **Reduced motion:** the drawer and its scrim appear and leave at once, in their final place.

### Reveal: a reward or result appearing

- **A reveal is for what the player waited for:** a fight's end, loot, a craft, a chest, a new level. `lg-reveal` rises `space-2` and fades in over `duration-reveal` on `ease-enter`, the whole result as one piece. No staggered rows, no slot-machine digits, no burst.
- **Once per result.** Key it to the result, so a re-render or a tab switch does not play it again. A result that repeats, like an idle loop's tenth victory, appears at once.
- **It never holds up input.** The result's buttons work from the first frame.
- **Its figures appear at their final values.** The balance they add to counts (Value change).
- **Reduced motion:** the whole result at once.

### Live-update highlight

- **A value that changed by itself** — a Bazaar price, a roster's status, a guild payout, a bid — takes the `changed` wash behind it: on at once, held for `duration-reveal`, then faded over `duration-slow` on `ease-standard`. `lgLive(() => value)`, or ListRow's `live`.
- **The mark says that it changed, not whether it helps.** It is not a hover, a selection or a rarity signal. It never loops, and it never repeats while the value is still.
- **Mark what the player may need to notice.** A value that ticks every second, like combat HP or a timer, is not marked.
- **Count what the player caused; mark what changed by itself; never both.** `lgLive` chooses from `cause`.
- **The wash sits outside the value's box,** absolutely placed, so the value and its neighbours never move. `ink` holds 9.21:1 on it and `ink-muted` 4.98:1.
- **Reduced motion:** the mark shows at once and goes at once after the hold. The brief colour change is the whole of it.

## Live updates

Prices, rosters, bids, chat and combat change while the player watches. **A live update never causes a layout shift, and never moves the control the player is about to click.**

1. **Nothing shifts.** Numbers reserve their width (Foundations · Numerals). New rows go below what the player sees, or wait. A notice arrives as a toast or in a place kept for it, never pushed into the flow above content.
2. **A list in use is held.** While the pointer is over a list, or focus is inside it, its rows keep their order and values update in place. A row whose item has gone stays where it was, muted, its action disabled ("Sold"). New rows wait behind a count ("2 new listings") in the Panel head, which the player can press. When the player leaves the list, the new order applies. The `[lgLiveList]` directive.
3. **Selection and scroll stay.** The selection is kept by id. If the selected item leaves, its row stays in place, gone, until the player picks another (`keep`). Rows are keyed by id, never by index, so the scroll position holds.
4. **The Chronicle keeps its place.** It follows the newest line only while the player is at its foot. Scrolled up, the line they are reading stays put, even as old lines leave the top, and "3 new lines" jumps to the latest.
5. **Arrivals don't move.** A new row appears at once, without sliding in. A value that changed takes the live mark.

| Screen | While the player is using it | What moves |
| --- | --- | --- |
| **Bazaar list**, refreshed | Rows hold their order; prices update in place with the mark; a sold listing stays, muted, "Sold"; new listings wait as "2 new listings" | Nothing but the marks |
| **Guild roster**, refreshed | The same: statuses update in place with the mark; someone who leaves stays, muted, until the list is released | Nothing but the marks |
| **Chronicle**, new lines | At the foot, the log follows. Scrolled up, it keeps its place and counts | Nothing, until the player jumps |
| **TopBar currency**, a purchase | The amount counts over `duration-slow` inside its reserved width | Its digits only |
| **TopBar currency**, a payout | The amount changes at once, marked | The mark only |
| **Combat**, HP ticks | A `live` Meter follows each tick; the number changes at once; nothing is marked | The fill |

## Loops

Two things may loop, and only while what they show is happening. Neither is decoration, and both stop when their wait or their fight ends.

| Loop | When | How | Reduced motion |
| --- | --- | --- | --- |
| **Indeterminate progress** | A real wait with no measurable progress: searching the Bazaar, joining a raid | `role="progressbar"` with no `aria-valuenow`, named by what is happening ("Searching the Bazaar"). A segment sweeps its track by `transform`, linear, once every 1.2s (twice `duration-reveal`). A wait that can be measured shows a Meter instead | The sweep stops and the track stays empty; the label says what is happening |
| **Live combat playback** | While a fight runs | Its container carries `data-motion="playback"`. Swings, hits, the swing timer and floating damage move by `transform` and `opacity`; its Meters are `live` | Travel becomes cuts: a hit lands in place, a damage number appears without drifting, the swing timer stops, nothing shakes |

A wait that runs past five seconds says in words what is happening. Combat playback is the activity itself, and an indeterminate indicator is essential while its wait lasts, which is how both meet WCAG 2.2.2 (Foundations · Accessibility). Every other loop is a fault (Audit, below).

## Properties

| Animates | Never animates |
| --- | --- |
| `transform` (translate, scale) and `opacity` | Width, height, position (`top`, `left`, `inset`), margin, padding |
| `visibility`, as a step at the start or end of a fade, so what has gone cannot be focused or clicked | Colour, background, border, box-shadow, filter, font and anything else that repaints or reflows |

**A wash that fades is a layer.** The Button, NavRail and EntryList draw their hover washes and edges on a pseudo-element behind their content, and fade its opacity. **A fill that grows is scaled:** the Meter sets `scaleX` from its left edge. **A count is text:** it changes digits in script, in a box already sized for it.

## Reduced motion

The game's rule (D-043): under `prefers-reduced-motion: reduce`, every transition and animation in the system ends at once and scrolling is instant. `data-motion="reduced"` on any ancestor does the same for its subtree — for an in-game setting, or the showcase's reduced-motion option. `lgReducedMotion(el)` answers for script, and `lgLive` stops counting under it.

Reduced motion changes how things move, never what the player learns. States, values, focus and the live mark's colour still arrive, at once. Each category's alternative is in its section above, and in the Categories table.

## Audit

A quick check for any screen, before it ships:

1. **Check every transition and animation** on the screen: each is on `transform` or `opacity`, none runs past `duration-reveal`, and nothing loops but the two allowed (Loops, above).
2. **Watch the screen update** with the pointer resting on a list row: nothing under the pointer may move.
3. **Turn on reduced motion** and do it again: everything arrives, at once.

## Tokens used

| Token | Role here |
| --- | --- |
| `duration-instant`, `duration-fast`, `duration-base`, `duration-slow`, `duration-reveal` | Every duration |
| `ease-standard`, `ease-enter`, `ease-exit` | Every easing |
| `changed` | The live-update mark (Foundations · Colour) |
| `surface-raised`, `gilt`, `gilt-soft` | The hover layers |
| `scrim` | The drawer's dimmer |
| `space-1`, `space-2`, `border-hairline` | The distances: a popover settling, an arrival rising, a press |

## Do and don't

| Do | Don't |
| --- | --- |
| Fade a NavRail item's `surface-raised` wash in over 140ms. | Transition its background colour, or scale the row with a spring. |
| Fill the XP Meter over 400ms when experience lands. | Pulse the Meter while it waits, or animate its width. |
| Count the Cinders down over 400ms after a purchase. | Count a guild payout; mark it instead. |
| Hold the Bazaar list while the pointer is on a row; mark the new prices. | Re-sort the rows under the pointer, so Buy lands on another listing. |
| Keep the Chronicle's place and show "3 new lines". | Scroll the log to the bottom while the player reads back. |
| Slide the rail drawer in over 220ms and out over 140ms. | Let it overshoot and settle. |
| Reveal the victory once, over 600ms. | Replay it on every tab switch, or stagger the loot in one row at a time. |
| Sweep an indeterminate bar while the Bazaar search runs. | Loop a shimmer on a Legendary item. |

## Related components

- Meter — the progress bar
- NavRail — the main navigation
- Chronicle — chat and the game log
- Button — the command button
- GameShell — the screen frame
- ListRow — the list row
- EntryList — the browsable name list
- Sigil — the hex stat badge
- Ledger — the labelled value list
