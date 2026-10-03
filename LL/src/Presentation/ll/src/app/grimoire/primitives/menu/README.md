# Menu

Actions on the thing it opened from.

**Status:** Draft — its look is Proposed (D-146), for review

A short list of actions on one thing: an item's Equip, Compare and Salvage; a player's View Profile and Whisper. It opens from a button, or from the thing itself, and closes once an action is chosen. Settings that stay open while the player ticks several are a Popover of Checkboxes; navigation is links, not a menu.

**Use:**

```html
<button lgButton="quiet" [lgMenuTrigger]="actions">Item actions</button>
<ng-template #actions>
  <lg-menu label="Ashen Blade">
    <button lgMenuItem (triggered)="equip()">Equip</button>
    <button lgMenuItem disabled>Sell</button>
    <button lgMenuItemCheckbox [(checked)]="favourite">Favourite</button>
  </lg-menu>
</ng-template>
```

**Provide:** an `lg-menu` in an `ng-template`, with `label` (its name: what it acts on) or `heading` (the same, shown at its head); its items, `button[lgMenuItem]` (words as content, an optional `icon`, `disabled`) and `button[lgMenuItemCheckbox]` (`[(checked)]`); and `[lgMenuTrigger]` on the element that opens it (`lgMenuPosition`, `(lgMenuOpened)`, `(lgMenuClosed)`). A choice is the item's `(triggered)`, which the keys fire as well as a press. Import `LG_MENU`. On the CDK's menu.

- **A Level 2 float:** `surface-raised`, a `line-strong` hairline, `radius-float`, `shadow-float`, its items `space-1` from its edge, at least 10rem wide. From a trigger it moves `space-1` into place over `duration-fast`.
- **Items** are rows of `body-compact`, at least 32px tall, at `radius-container`. The item under the pointer or the keys takes the Option's wash; the focus ring is the system's. A disabled item stays in view in `ink-disabled`, passed over by the keys.
- **A checkbox item** carries the Checkbox's box: a well, or an `arcana-glow` fill with its tick when checked.
- **Choosing closes it,** and focus goes back to its trigger. A menu shown in place (an `lg-menu` without a trigger, in an overlay of your own) stays: close it in `(triggered)`.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default (an item) | — | Its words | "Equip, menu item" |
| Hover, focus | Fill: the wash; Edge: `focus-ring` on focus | — | Its words |
| Disabled | Text: `ink-disabled` | — | "dimmed"; the keys pass over it |
| Checked (a checkbox item) | Fill: `arcana-glow` box and tick | — | "checked" |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | `menu` named by `label` or `heading`; `menuitem` and `menuitemcheckbox` items; its trigger has `aria-haspopup="menu"` and `aria-expanded` |
| Keyboard | Enter, Space or Down on the trigger open it on its first item; arrows move, Home and End go to the ends, typing finds an item; Enter or Space chooses; Escape closes |
| Focus | Into it on open; back to the trigger when it closes |
| Announced | Each item as focus reaches it |
| Hover and tap | The pointer moves the wash; a press chooses |
| Target size | Items at least 32px tall |
| Text scaling | Items wrap; the menu is at most 20rem wide |
| Colour | Checked is also the tick and `aria-checked` |
| Motion | Moves `space-1` into place; at once under reduced motion |

## Related components

- Popover — controls that stay open
- Button — what opens it
- Checkbox — the box its checkbox items carry
