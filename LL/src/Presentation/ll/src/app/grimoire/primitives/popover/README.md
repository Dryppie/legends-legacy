# Popover

A float that opens on a press, holding controls of its own.

**Status:** Draft — its look is Proposed (D-146), for review

More than a tooltip can hold, beside the thing it belongs to: a list's filters, the pinned quest's tracker, the chat's channel settings. It is a non-modal dialog: the page stays live around it. A float that explains on hover or focus is a Tooltip; a list of actions is a Menu; a task that asks for the player's whole attention is a Dialog.

**Use:**

```html
<button lgButton [lgPopoverTrigger]="filters">Filters</button>
<lg-popover #filters label="Filters" width="16rem">
  <lg-checkbox [(checked)]="equipped">Equipped</lg-checkbox>
</lg-popover>
```

**Provide:** an `lg-popover` with its content, a `label` (its name, as a dialog) and an optional `width` (20rem by default); and on the element that opens it, `[lgPopoverTrigger]` naming the popover, `lgPopoverPlace` (`below`, centred, the default; `below-start`; `end`, beside it) and `[(lgPopoverOpen)]` to bind the state. Import `LG_POPOVER` for both.

- **A Level 2 float** on the CDK overlay: `surface-raised`, a `line-strong` hairline, `radius-float`, `shadow-float`, at the inner inset, at most 70vh tall with its content scrolling inside. It moves `space-1` into place over `duration-base`. Opening it closes the page's tip.
- **Its element** says it opens a dialog (`aria-haspopup`), whether it is open (`aria-expanded`) and which (`aria-controls`).
- **Focus moves into it** when it opens, so Tab reaches what it holds. Escape, a second press on its element, or Tab past either end of it close it, and focus goes back to the element. A press outside, or focus leaving it for the page, closes it where the player went.
- **Escape closes the topmost layer only:** the tip hears it first, then the latest overlay, so a popover opened from a dialog closes before the dialog.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Closed | Its element's own | — | "Filters, button, collapsed, has popup dialog" |
| Open | The float beside its element | Its content | "expanded"; then "Filters, dialog" as focus moves in |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A non-modal `dialog` named by `label`; its element has `aria-haspopup="dialog"`, `aria-expanded` and `aria-controls` |
| Keyboard | Enter or Space on its element opens it; Tab moves through it; Escape or Tab past an end closes it |
| Focus | Into it on open; back to its element on Escape, a press on the element, or Tab past an end |
| Announced | Its name, as focus moves in |
| Hover and tap | Opens on a press, never on hover; a press outside closes it |
| Target size | Its element's and its controls' |
| Text scaling | rem; it scrolls inside past 70vh |
| Colour | — |
| Motion | Moves `space-1` into place over `duration-base`; at once under reduced motion |

## Related components

- Tooltip — words on hover and focus
- Menu — a list of actions
- Dialog — a modal task
- Objective — opens its tracker in one
