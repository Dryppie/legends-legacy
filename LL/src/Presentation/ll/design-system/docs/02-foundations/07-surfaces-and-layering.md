# Foundations · Surfaces & Layering

Candlelight, not paper. This section covers two things: how a surface looks against what lies under it, and what it covers.

- **Elevation** is the look: four levels and the scrim, each with its own fill, shadow and edge.
- **The layer** is the paint order: the `z-*` stack, from the content up to a drag preview.

The two are separate. An item hover card looks like Level 2, but it sits on the detached-popover layer, above a Level 3 dialog. Stage art stays at the bottom as atmosphere, and nothing glows (D-012). LayeringSpecimen shows the levels, the stack and four scenes from the game.

## Rules

**Must**
- Draw every surface at one of the four levels, with that level's fill, shadow and edge (Elevation, below).
- In flow, step up at most one level from the surface beneath. Keep to at most two enclosed levels inside the stage (Nesting, below).
- Make every floating surface opaque. Level 1 in flow lets the frame's backdrop through, the game's panel material (D-102), but never content. Use no backdrop blur. Only the scrim lets content show through, and it is flat.
- Keep film grain to art: the Stage and the Banner, when they have an image (Foundations · Ornament).
- Give every z-index a `z-*` token (The layer stack, below). Only two exceptions exist:
  - art inside an isolated Stage or Banner sits at −1 and −2;
  - a scrim sits one below the layer it serves (`calc(var(--z-modal) - 1)`).
- Open one modal at a time. A confirmation overlays the dialog that opened it; anything else a dialog leads to replaces its content.
- Close the topmost layer first on Escape, and return focus to the element that opened the layer.
- Give any text set over art `shadow-text-art`.

**Should**
- Build surfaces from the level classes (`lg-level-1`, `lg-level-2`, `lg-level-3`, their `--float` variants and `lg-scrim`), or from the same tokens.
- Detach a popover (portal it to the body) when it must cross the edge of the region or dialog it opens from.
- Register anything that Escape closes with `LL.layers`. The one stack then keeps the order and returns focus.
- Show selection with a solid `border-emphasis` (2px) `arcana-glow` ring or edge — an `ink` bar in item rows and chat — and hover with the neutral `surface-raised` wash (D-015, Foundations · Lines).
- Treat stage art the same way everywhere:
  - darkened, warmed and slightly blurred;
  - a vignette to `ground-deep` and a fade to `ground` at the foot;
  - a film grain.

**Never**
- Glow: no coloured halo, blurred shadow or light bloom on any element.
- Lighten a fill to show height. Height comes from shadow and edge.
- Take one level's fill with another level's shadow, except where the table below says so.
- Blur what lies behind a surface.
- Open a dialog from a dialog, or a second confirmation.
- Invent a z-index, or raise one to win a fight. Move the thing to its layer instead.
- Put information only in art. Art is atmosphere, never content.

## Elevation

| Level | Fill | Shadow | Edge | Corners | Texture | Holds |
| --- | --- | --- | --- | --- | --- | --- |
| **0 · Ground** | `ground` | None | None | — | Only the Stage's art and its grain, beneath the content | The shell's base, the stage and the docked Chronicle. The Page has no surface of its own: it shows its frame's backdrop (D-101) |
| **1 · Surface** | `surface` in flow, translucent over the frame's backdrop (D-102); `surface-solid` when it floats or something scrolls beneath it | None in flow; `shadow-float` when it floats | None in flow: the fill and the space around it carry the region (Foundations · Lines); `line-strong` when it floats | `radius-container` in flow; `radius-float` when it floats | None | The NavRail, Panels, lists and the JourneyCard; floating, the Chronicle drawer |
| **2 · Raised** | `surface-raised` | None for a wash; `shadow-float` when it floats | None for a wash; `line-strong` when it floats | `radius-float` when it floats | None | Washes: hover, selected, a pinned Ledger row, a mention. Floating: tooltips, hover cards, menus, suggestion lists and toasts |
| **3 · Folio** | `folio` | `shadow-panel` | The 1px dark ring built into `shadow-panel`. The Folio adds its gilt double frame; a sheet adds `line-strong`. | Square on the Folio; `radius-container` on a sheet | The Folio only: frame and corners. No grain (D-072) | The Folio, dialog sheets, confirmations and the tour's coach mark |
| **Scrim** | `scrim` (`slate-950` at 80%) | None | None | — | None; never blurred | Beneath a dialog, a confirmation, the rail drawer and the tour's spotlight |

- **Levels are not a lightness ramp.** The fills are ground `#101014`, surface `#101014` at 72% over the backdrop (about `surface-solid`, `#16161b`), surface-raised `#22222a` and folio `#131318`: the Folio is darker than a hover wash. Level 3 reads as the highest through `shadow-panel`, and the Folio through its frame as well. Every level holds `ink` at 12.74:1 or more and `line-strong` edges at 3:1 or more (Foundations · Colour · Contrast).
- **Inner surfaces of Level 2.** StatTiles and the active primary tab take `tile`, a step up from `surface` inside a Level 1 container.
- **Wells sink below Level 0.** Inputs, item slots and the inventory grid's gutters are `ground-deep`; a LoadoutSlot is `ground`. A well counts as an enclosed level, like a tile.
- **Modal surfaces take `shadow-panel`.** A dialog or confirmation over a scrim has Level 3's shadow. So does the rail drawer on small screens: the NavRail keeps its Level 1 fill, but it is modal.
- **The level classes:**

| Class | Level |
| --- | --- |
| `lg-level-0` | Ground |
| `lg-level-1` | A Panel-like surface in flow |
| `lg-level-1--float` | A drawer |
| `lg-level-2` | A wash |
| `lg-level-2--float` | A popover, hover card or toast |
| `lg-level-3` | A sheet |
| `lg-scrim` | The scrim |

## Nesting

- **In flow, a surface placed on another steps up at most one level:** the Page (0), then a Panel (1), then a wash, tile, slot or input inside it (2). A Level 2 or Level 3 box never sits directly on the Page.
- **Inside the stage, at most two enclosed levels.** The shell's regions (the rail, the stage, the Folio and the Chronicle) are the frame and don't count (Principles · Anti-generic guardrails).
- **Floating surfaces are not placed on anything.** They take their own level over whatever lies beneath: a Level 2 tooltip over the Page, or a Level 2 hover card over a Level 3 dialog.
- **Inside a floating surface, one enclosed level.** The Folio, a dialog and the drawer hold tiles, slots, inputs and washes, but never a Panel. A dialog groups its content with headings and SectionRules.

| Allowed | Not allowed |
| --- | --- |
| The Page, then a Panel, then a hovered ListRow's wash | A Panel inside a Panel |
| The Page, then the JourneyCard (Level 1), then its objective wash | A Level 3 card with `shadow-panel` on the Page, as the JourneyCard was before D-055 |
| The Folio, then a StatTile | The Folio, then a Panel, then a StatTile |
| A dialog, then item-slot wells | A dialog, then a Panel, then a LoadoutSlot, then an ItemSlot |
| A tooltip over the Page, with its shadow and edge | A tooltip drawn in flow, on the surface of the row beneath, with no shadow |

## Opacity, blur and texture

- **Every surface hides what lies behind it, but one:** Level 1 in flow — a Panel, a list, the JourneyCard, the NavRail — is `surface`, the game's panel material, a cool near-black at 72% that lets the frame's backdrop show through (D-102). It sits over the backdrop only, never over content: where content scrolls beneath it (a sticky table header or column) or it floats, it takes `surface-solid`. Text on it is measured over the real backdrop, at 4.5:1 over the brightest point behind it.
- **No backdrop blur (`backdrop-filter`) anywhere.** The scrim dims what lies behind it but does not blur it.
- **Softened art is not blurred content.** The Stage and the Banner soften their own picture, which is part of the art treatment. No surface ever blurs what lies behind it.
- **Some things are not surfaces**, and may let the picture show through:
  - the TopBar's fade to `ground` over the stage;
  - the veils over Stage and Banner art;
  - the dark fades at a list's ends.

  They hold no box and no fill of their own (Principles · Anti-generic guardrails · Gradients).
- **Film grain**, at `--lg-grain-opacity`, belongs to art only: the Stage and the Banner, when they have an image. The Folio, the Page, Panels, dialogs, the rail and the Chronicle are flat (D-072, Foundations · Ornament).

## The layer stack

From the bottom up. A layer paints over every layer below it, whatever the level of either.

| Layer | Token | Value | Holds |
| --- | --- | --- | --- |
| Content | `z-content` | 0 | The stage, the Page and everything in flow |
| Raised in place | `z-raised` | 1 | A focused row over its neighbours, and the region the pointer or focus is in. Only inside its own stacking context. |
| Sticky | `z-sticky` | 10 | Sticky table headers, a table's held first column (the corner cell sits one above) and sticky section heads |
| Chrome | `z-chrome` | 20 | The TopBar and KeyHints over the stage, and the NavRail column |
| Folio | `z-folio` | 30 | The Folio, whose shadow falls on the stage and the TopBar's end, and the docked Chronicle beside or under it |
| Floating chat | `z-chat-float` | 40 | The floating Chronicle drawer, above the Folio when dragged across it, and the bottom dock on small screens |
| Overlay | `z-overlay` | 50 | The rail drawer on small screens; its scrim sits at 49 |
| Popover | `z-popover` | 100 | Tooltips, hover cards, menus and suggestion lists opened from the page |
| Modal | `z-modal` | 200 | One dialog; its scrim sits at 199 |
| Confirmation | `z-confirm` | 250 | A confirmation over the dialog that opened it; its scrim sits at 249 |
| Detached popover | `z-popover-detached` | 300 | A popover opened from a dialog or a confirmation, portaled to the body so it is never clipped by the dialog |
| Toast | `z-toast` | 400 | Toasts |
| Tour | `z-tour` | 500 | The guided tour's spotlight scrim and coach mark |
| Drag | `z-drag` | 600 | A drag preview under the pointer |

`z-overlay`, `z-popover`, `z-modal` and `z-popover-detached` keep the game's values. The other ten fill the gaps around them.

- **Layers compare at the top of the document.** The Stage, the Page (a size container), each region and each dialog start their own stacking context. Nothing drawn in place inside one can rise above the shell's layers outside it: an in-place tooltip in the Page stays under the TopBar and the Folio.
- **A popover that must pass over the shell's layers portals to the body:** the page's at `z-popover`, a dialog's at `z-popover-detached`. The Ledger's explanation is drawn in place today; the shared Tooltip (Audit item 2) will portal.
- **Portaled page popovers outrank the drawer.** A floor's tooltip on the dungeon route (100) covers the floating Chronicle (40), which in turn covers the Folio (30).
- **Opening a dialog closes the page's popovers.** They belong to a page that is now inert.
- **Toasts** are Level 2 floating surfaces at the top centre of the stage.
  - At most three show at once, newest on top.
  - Each stays at least 6 seconds, and pauses while hovered or focused.
  - Each can be dismissed, and is never the only way to act.
  - A toast never takes focus. It is announced through `LL.announce`, not a live region of its own (Foundations · Accessibility · Live regions).
- **Drag previews** are the dragged thing's own surface at Level 2 floating: opaque, never faded.
  - Escape cancels the drag and puts the item back.
  - Every drag also has a single-pointer alternative, such as a Move button or a menu (WCAG 2.5.7).

## The modal stack

1. **One modal at a time.** A dialog never opens another dialog. When a dialog leads further — a second step, or a different task — it replaces its own content, with Back. Or it closes first, and the next opens. Focus lands in the new content.
2. **A confirmation overlays the dialog that opened it.**
   - It is an `alertdialog` on Level 3 at `z-confirm`, centred over the dialog, with its own scrim that dims the dialog.
   - The dialog behind stays mounted and visible, but `inert`, with its state kept.
   - There is only ever one confirmation, and it opens nothing else.
   - Its buttons name the action and its cost ("Redeem for 400 Soulstones") beside Back. The committing button is the screen's one `solid` Button.
   - When the action spends currency or can't be undone, focus starts on Back; otherwise it starts on the committing button.
   - **After Confirm**, the action runs and the confirmation closes. If the action finishes the dialog's task, the dialog closes too: focus returns to the dialog's opener, and the outcome is announced. Otherwise focus returns to the button in the dialog that opened the confirmation.
   - **After Back or Escape**, the confirmation closes and focus returns to the button that opened it.
3. **Escape closes the topmost layer, and only that layer.**

   | Order | Layer | What Escape does |
   | --- | --- | --- |
   | 1 | A drag | Cancels it |
   | 2 | The tour | Ends it |
   | 3 | A detached popover | Closes it |
   | 4 | A confirmation | Closes it |
   | 5 | A dialog | Closes it |
   | 6 | A page popover or menu | Closes it |
   | 7 | The rail drawer | Closes it |

   Each layer stops the key there. `LL.layers` keeps this order: the highest z first, then the latest opened. A toast takes Escape only while focus is in it. A screen may use Escape for Back only when no layer is open.
4. **Focus goes into a layer, and back to where it came from.**
   - Opening a layer moves focus into it: a dialog to its first control, a confirmation to its safe button, the tour to its coach mark.
   - Popovers that open on hover or focus don't take focus.
   - A dialog, a confirmation and the tour trap Tab (`trap`), and everything beneath them is `inert`.
   - Closing a layer returns focus to the element that opened it. If that element is gone (the item was withdrawn), focus goes to the nearest thing that remains: the next row, or the dialog's heading.

`LL.layers.open({ kind, onClose, opener, trap })` and, in React, `LL.layers.use(open, options)` implement these rules. The future Dialog, Tooltip, Toast and tour are built on them.

## In the game

### An item hover card inside the Guild Vault dialog

**Layers:** the Guild screen (Level 0, inert) → the scrim (199) → the Guild Vault dialog (a Level 3 sheet, `z-modal`) → the hover card (Level 2 floating, `z-popover-detached`).

- **Why it is detached:** the vault's slot grid scrolls inside the sheet, so an in-place popover would be clipped at the sheet's edge. The card is portaled to the body and placed from the slot, so it sits above the dialog and may cross its edge.
- **How it opens and closes:**
  - It opens on hover and on keyboard focus of a slot, and pins on tap.
  - Escape closes the card and leaves the dialog open, with focus on the slot.
  - A second Escape closes the dialog, and focus returns to Open vault on the Guild screen.
- **What it holds:** what a Ledger explanation holds. That is the name in its rarity colour, the rarity Tag, the item's stats, and who deposited it and when.

### A confirmation from the Nobility redemption dialog

**Layers:** the Overview (inert) → the scrim → the Redeem Nobility dialog (Level 3, `z-modal`) → the confirmation's scrim (249) → the confirmation (Level 3, `z-confirm`).

- **The dialog** offers 7 or 30 days. Redeem opens the confirmation "Spend 400 Soulstones?", with "30 days of Nobility start now. Soulstones aren't refunded." It has two buttons, Redeem for 400 (`solid`) and Back. Focus starts on Back.
- **Back or Escape** closes the confirmation. The dialog keeps the choice, and focus returns to Redeem.
- **Redeem for 400** closes both. Focus returns to Redeem Nobility on the Overview, and "Nobility active for 30 days" is announced.

### The floating chat over the dungeon route

**Layers:** the Stage with the dungeon route (Level 0) → the TopBar (`z-chrome`) → the Folio (Level 3, `z-folio`) → the floating Chronicle (Level 1 floating, `z-chat-float`) → a floor's tooltip (Level 2 floating, `z-popover`) → a toast (`z-toast`).

- **The drawer is opaque.** What lies under it is covered, not blurred, and the player moves or collapses it to see the route.
- **What it covers:** dragged across the Folio's edge, it stays above the Folio. A floor's tooltip opened near it covers it: the tooltip is portaled out of the Stage, and page popovers outrank the drawer.
- **The route stays reachable by keyboard,** whatever the drawer covers.

### The First Steps tour spotlight over the NavRail

**Layers:** everything → the tour's scrim, with a cut-out (`z-tour`) → the spotlight's ring → the coach mark (Level 3, `z-tour`).

- **The spotlight:** the scrim dims everything but the spotlighted rail item, Inventory. The cut-out follows the item with `space-1` to spare, and a flat 2px `arcana-glow` ring marks it: actionable, not glowing.
- **The coach mark** sits beside the rail and points at the item. It reads "First Steps · 2 of 5", with the title, one sentence, Skip tour and Next.
- **The spotlighted item still works:** choosing it opens Inventory and moves the tour on. Tab moves between the coach mark and the item; everything else is inert.
- **Escape or Skip tour** ends the tour. Focus returns to where it was when the tour began, and the tour can be restarted from Settings.

## Tokens used

| Token | Role here |
| --- | --- |
| `ground`, `surface`, `surface-raised`, `folio` | The fills of Levels 0–3 |
| `ground-deep` | Wells, below Level 0 |
| `tile` | Level 2 inner surfaces: StatTiles, the active tab |
| `scrim` | The dimmer beneath a modal layer |
| `line-strong` | Floating and modal edges |
| `border-hairline` | Every surface edge |
| `shadow-float` | Floating Levels 1 and 2 |
| `shadow-panel` | Level 3 and modal surfaces |
| `shadow-text-art` | Text over art |
| `radius-float` | Floating surfaces |
| `z-content`, `z-raised`, `z-sticky`, `z-chrome`, `z-folio`, `z-chat-float`, `z-overlay`, `z-popover`, `z-modal`, `z-confirm`, `z-popover-detached`, `z-toast`, `z-tour`, `z-drag` | The layer stack |
| `arcana-glow` | The tour spotlight's ring |

## Do and don't

| Do | Don't |
| --- | --- |
| A Panel (Level 1) on the Page, its hovered rows washed `surface-raised`. | A Panel inside a Panel, or a `shadow-panel` card on the Page. |
| A hover card on `surface-raised` with `shadow-float` and a `line-strong` edge. | A hover card on the row's own surface, with no shadow. |
| The floating chat opaque over the dungeon route. | A frosted drawer with `backdrop-filter: blur()`. |
| Film grain over the Stage's and the Banner's art. | Grain on the Folio, a Panel, a dialog or the Page. |
| The vault's hover card portaled at `z-popover-detached`. | The hover card clipped by the dialog's scrolling grid. |
| A confirmation over the Nobility dialog, with Back focused. | A second dialog opened on top of the first. |
| Escape closes the hover card, then the dialog. | One Escape that closes the card and the dialog together. |
| `z-index: var(--z-toast)`. | `z-index: 9999`. |
| Set a Sigil label over art with `shadow-text-art`. | Put a dark box behind it. |

## Related components

- LayeringSpecimen — the levels, the stack and four scenes
- Folio — the detail panel
- Panel — the content box
- Chronicle — chat and the game log
- GameShell — the screen frame
- Stage — the scene backdrop
- Banner — the headline block
- Page — the information screen frame
- JourneyCard — the next-step guide
- Ledger — the labelled value list
- SearchField — search with suggestions
- NavRail — the main navigation
