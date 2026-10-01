# LayeringSpecimen

The levels, the stack and four layered scenes.

**Status:** Draft

Foundations · Surfaces & Layering, shown on four moments from the game. Along the top are the four elevation levels and the scrim, each with its fill, shadow and edge, and beside them the layer stack from `z-drag` down to `z-content`.

- **A hover card inside the Guild Vault dialog.** The vault is a Level 3 sheet at `z-modal`. Hover or focus a slot for its hover card, a Level 2 floating surface at `z-popover-detached`; tap to pin it. The card sits outside the sheet, so it crosses the sheet's edge. Escape closes the card first, then the dialog, and focus returns to Open vault.
- **A confirmation from the Nobility dialog.** The confirmation, an `alertdialog` at `z-confirm`, overlays the dialog behind its own scrim. The dialog stays mounted and inert. Back is focused first, and Tab stays inside. Back or Escape returns to the dialog with the choice kept; Redeem for 400 closes both and announces the outcome.
- **The floating chat over the dungeon route.** The Chronicle drawer (Level 1 floating, `z-chat-float`) passes over the Folio (`z-folio`). A floor's tooltip, portaled out of the Stage, passes over the drawer, and the toast passes over everything on the page. The drawer is opaque: what it covers is covered, never blurred.
- **The First Steps tour over the NavRail.** The tour's scrim leaves a cut-out around one rail item, ringed in a flat 2px `arcana-glow`. The coach mark is Level 3 at `z-tour`. Next, or choosing the item, moves on. Tab moves between the coach mark and the item, and Escape ends the tour.

Each scene is its own stacking context, so its layers stay inside it. Each also scopes its layers with `LL.layers`, so the four run side by side. In the game each layer portals to the body, and there is one stack.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | Dialogs are `dialog` or `alertdialog` with `aria-modal`, named by their heading; the hover card and the floor's tooltip are `tooltip`; the coach mark is a non-modal `dialog` named by its title |
| Keyboard | Escape closes the topmost layer of the scene you are in, and only that; the dialogs, the confirmation and the tour trap Tab |
| Focus | Opening a layer moves focus into it (Back first in the confirmation); closing returns it to the opener. Nothing takes focus on first render |
| Announced | The redemption's outcome, once, through `LL.announce`; the toast is visual here |
| Hover and tap | The hover card opens on hover and on focus and pins on tap; the floor's tooltip opens on hover and on focus |
| Target size | Buttons 32–40px; route floors 40px; slots 64px |
| Text scaling | At 130% the scenes stack one to a row |
| Colour | Rarity by colour, code and Tag; the tour's target by the ring and the coach mark's words |
| Motion | Nothing |

## Related components

- Folio — the detail panel
- Panel — the content box
- Chronicle — chat and the game log
- ItemSlot — the item frame
- NavRail — the main navigation
- Stage — the scene backdrop
- Ledger — the labelled value list
