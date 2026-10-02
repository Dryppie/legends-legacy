# Button

The command button.

**Status:** Draft

An engraved rectangle with an optional key cap: "Level up (E)".

**Provide:** the label as content (a verb or verb phrase), the variant as the attribute's value (`<button lgButton="quiet">`, also on an `<a>`), optional `hotkey` (you wire the key; the cap and `aria-keyshortcuts` are shown for you), `icon`, `size` and `density`. When it can't act, a `state` and its `reason` (or `shortfall`, or `remaining`); for an action that waits on the server, a `pendingLabel`.

- **An icon accompanies the label** at the density's icon size — 20px in Comfortable and Standard, 16px in Compact (Foundations · Iconography). An icon-only Button is allowed only for a common action (close, back, expand, filter, sort, refresh, copy, link) and needs an `aria-label` and a tooltip with the same words.
- **Height follows the region:** without a `size`, a Button is the control height of its density — 44px Comfortable (the Folio, dialogs), 40px Standard, 32px Compact (a row's action) — with its label, padding and icon to match. `size="md"` pins Standard and `size="sm"` pins Compact wherever it sits (Foundations · Space & Density).
- **Shape:** an engraved rectangle at `radius-control` (4px), with its key cap at the same corner, and side padding of the density's `cell-x` plus 4px. It was a pill; the pill read as any rounded app beside Grimoire's square containers, and needed 8px more room in dense rows (D-064, Foundations · Shape).
- `primary` (default): `surface` fill with a `line-strong` edge; hover turns the edge `gilt`. `danger` takes the same `gilt` edge on hover.
- `solid`: `gilt` fill, `on-gilt` label — one per screen, for the committing action (Enter dungeon). Its hover is one of the system's four lit states: the fill brightens by 6%, flat — never a halo, a gilt glow or a pulse (Foundations · Ornament · Glow).
- `quiet`: text only, for Cancel and Back. `danger`: `danger` outline for Abandon, Sell, Leave guild.
- `link`: `gilt` text, no frame, for navigation inside a panel ("Manage", "Show perks").
- **No ornament:** no corners, frame, texture, gradient or glow on any variant, and no ornament rule beside a button row. A Button is a control; its shape and its `line-strong` edge are all it needs.
- **Motion** (Foundations · Motion · Feedback): hover fades a layer in over `duration-fast` on `ease-standard` — the `gilt` edge on `primary` and `danger`, the 6% brighter fill on `solid` — so only its opacity changes; `quiet` and `link` change at once. A press sinks the Button `border-hairline` (1px) at once (`duration-instant`) and releases over `duration-fast`. Nothing scales, ripples or glows, and a Button works from its first frame, whatever around it is still moving. Under reduced motion hover and press happen at once.
- **Blocked, not disabled** (Standards · States). A Button the player can't use right now takes a `state` — `unavailable`, `locked`, `restricted`, `insufficient` or `cooldown` — and says why. It stays focusable (`aria-disabled`), its label drops to `ink-muted` on a `line` edge (dashed when locked), it takes no hover layer and no press, and its reason opens in the reason tip on hover and focus. A press, click or Enter pins the tip and announces the reason; `(click)` never runs. `[shortfall]="[{ amount: 250, name: 'Cinders' }]"` writes "Short by 250 Cinders" in `warning`; `[remaining]="252"` writes "Ready in 4m 12s" and is read as "Ready in 4 minutes 12 seconds". A blocked `solid` Button loses its fill: the screen's committing action is not available.
- **Plain `disabled` is rare:** only for a limit plain from what sits beside it — Previous on the first page. It leaves the Tab order and greys to `ink-disabled`.
- **Pending:** `state="pending"` shows the `pendingLabel` ("Claiming…"), sets `aria-busy` and ignores a second press. Give `pendingLabel` from the start: both labels share one cell, so the Button keeps the width of the longer and nothing shifts.
- Labels are Barlow 600 in sentence case: `body` (15px) on the 20px row leading in Comfortable and Standard (`md`), `body-compact` (14px) on 18px in Compact (`sm`), with `tracking-button`. They moved from Marcellus because the small size broke the display face's 15px floor, and Buttons sit in dense rows beside Barlow numbers (D-030).

## Supported states

| State                  | Channel                                                                                                                         | Words                                | Screen readers hear                                  |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------ | ---------------------------------------------------- |
| Default                | The variant's own look                                                                                                          | Its label                            | "Sell, button"                                       |
| Hover                  | Fill: the hover layer (`gilt` edge on `primary` and `danger` — a known exception, Foundations · Colour; 6% brighter on `solid`) | —                                    | Nothing                                              |
| Focus-visible          | Edge: `focus-ring`                                                                                                              | —                                    | Its name, state and description                      |
| Pressed                | Position: a 1px drop                                                                                                            | —                                    | Nothing                                              |
| Selected (a toggle)    | `aria-pressed`, with the toggle's own look                                                                                      | —                                    | "pressed"                                            |
| Disabled (rare)        | Text: `ink-disabled`; Edge: `line`                                                                                              | —                                    | Out of the Tab order                                 |
| Unavailable            | Text: `ink-muted`; Edge: `line`; the reason tip                                                                                 | The reason: "Not while in a dungeon" | "Sell, button, unavailable. Not while in a dungeon." |
| Locked                 | Edge: dashed `line`; Text: `ink-muted`; the tip with "Locked"                                                                   | "Locked", then "Unlocks at level 20" | "…, unavailable. Locked. Unlocks at level 20."       |
| Restricted             | As Unavailable                                                                                                                  | Who may: "Officers only"             | "…, unavailable. Officers only."                     |
| Insufficient resources | As Unavailable; the tip's shortfall in `warning`                                                                                | "Short by 250 Cinders"               | "…, unavailable. Short by 250 Cinders."              |
| On cooldown            | As Unavailable                                                                                                                  | "Ready in 4m 12s"                    | "…, unavailable. Ready in 4 minutes 12 seconds."     |
| Pending save           | Text: the progressive label, in room kept for it                                                                                | "Claiming…", "Listing…"              | "Claiming…, button, busy"                            |

## Accessibility notes

| Field         | Notes                                                                                                                                                                                                    |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Role and name | A `button` named by its label; an icon-only Button needs `aria-label`. `hotkey` sets `aria-keyshortcuts`                                                                                                 |
| Keyboard      | Enter or Space. A blocked Button stays in the Tab order; Enter or Space shows its reason. Single-key hotkeys never fire while a text field has focus, and players can remap or turn them off             |
| Focus         | `focus-ring`                                                                                                                                                                                             |
| Announced     | Its label and state (`aria-pressed` when it toggles, unavailable, busy); a blocked Button's reason as its description, and again (polite) when pressed                                                   |
| Hover and tap | Hover changes the edge, or brightens the solid fill; a press sinks the Button 1px; nothing glows. A blocked Button opens its reason tip on hover and keyboard focus; a tap pins it, and Escape closes it |
| Target size   | 44, 40 or 32px by density; a link Button standing alone at least 24px. Primary actions at least 32px                                                                                                     |
| Text scaling  | Grows with its label                                                                                                                                                                                     |
| Colour        | `danger` is also named by its verb (Abandon, Sell); `solid` is also a fill                                                                                                                               |
| Motion        | The hover layer fades over `duration-fast`; a press goes down at once and releases over `duration-fast`. At once under reduced motion                                                                    |
