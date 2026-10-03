# Dialog

A modal sheet over the scrim, and the confirmation over it.

**Status:** Draft — its look is Proposed (D-145), for review

A task the player steps into and comes back from: rename a character, redeem Nobility, transfer items, open the guild Vault. And the confirmation before an action that spends or can't be undone: "Spend 400 Soulstones?". `LgDialog` opens them on the CDK's `Dialog`, which keeps the modal manners Foundations · Surfaces & Layering · The modal stack sets: the scrim, focus moved in and trapped, the rest of the page hidden from screen readers, Escape and a press on the scrim closing it, focus back to what opened it.

**Use:**

```ts
const ref = this.dialog.open(RenameDialogComponent, { data: { name } });   // inject(LgDialog)
ref.closed.subscribe((name) => name && this.rename(name));

this.dialog
  .confirm({ heading: 'Spend 400 Soulstones?', text: "30 days of Nobility start now. Soulstones aren't refunded.", confirm: 'Redeem for 400' })
  .closed.subscribe((yes) => yes && this.redeem());
```

The dialog's component reads its data with `inject(DIALOG_DATA)` and its `DialogRef` with `inject(DialogRef)`, and its template is the sheet and its regions (`LG_DIALOG`):

```html
<lg-dialog>
  <lg-dialog-header>Rename character</lg-dialog-header>
  <lg-dialog-content>
    <lg-field label="Character name"><input lgInput [formControl]="name" /></lg-field>
  </lg-dialog-content>
  <lg-dialog-actions>
    <button lgButton="quiet" lgDialogClose>Cancel</button>
    <button lgButton="solid" [lgDialogClose]="name.value">Save</button>
  </lg-dialog-actions>
</lg-dialog>
```

**Provide:**

- `LgDialog.open(component, { data, ariaLabel, disableClose, closePredicate, autoFocus, restoreFocus })`; `closed` emits the result, or undefined for Escape, Close and the scrim.
- `LgDialog.confirm({ heading, text, confirm, back, costly, tone })`; `closed` emits true for the action, false for Back, undefined for Escape. `costly` (true by default) starts focus on Back; `tone: 'danger'` makes the action the danger Button.
- `lg-dialog`'s `size`: `sm` 24rem (a confirmation), `md` 32rem (the default), `lg` 44rem.
- `lg-dialog-header`: the title, which names the dialog, and its Close button (`closable="false"` leaves it out).
- `lg-dialog-content`: the body, which scrolls when the sheet is taller than the window allows.
- `lg-dialog-actions`: the buttons, at the foot and end, the committing one last.
- `lgDialogClose` closes the dialog it is in, with its value as the result.

- **The sheet** is Level 3: `folio`, `shadow-panel`, a `line-strong` hairline, `radius-container`; Comfortable; no ornament. It keeps 16px from the window's sides and 24px from its top and foot.
- **The scrim** is flat `scrim` (slate-950 at 80%), never blurred. The sheet rises in over `duration-base` on `ease-enter`; the scrim fades in with it.
- **Focus** starts on the element marked `cdkFocusInitial`, or the first control in the content, then in the actions, and the Close button last. It returns to the element that opened the dialog.
- **One modal at a time:** opening a dialog closes the one open first, so a dialog that leads to another hands over to it. Opening either closes the page's tip.
- **The confirmation** is an `alertdialog`: its question is its name and its text its description. It overlays the dialog that opened it, on its own scrim, which dims the dialog; the dialog behind stays, with its state, and is `inert` until it closes. Escape closes only the confirmation, and focus goes back to the button that opened it. There is only ever one.
- **The committing button** is the screen's one `solid` Button (the danger Button for a loss), named by its action and its cost, beside Back or Cancel.
- **A dialog holds** inputs, tiles, slots and washes, never a Panel; it groups its content with headings and SectionRules.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Open | Fill: the scrim under a Level 3 sheet | Its title | "Rename character, dialog" |
| Confirmation | A second scrim, the sheet at `sm` | Its question, its action and cost | "Spend 400 Soulstones?, alert dialog. 30 days of Nobility start now…" |
| Behind a confirmation | Dimmed by the confirmation's scrim; `inert` | — | Nothing: out of reach until the confirmation closes |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | `dialog` (`alertdialog` for a confirmation), `aria-modal`, named by its title (`aria-labelledby`), a confirmation described by its text |
| Keyboard | Tab stays inside (the CDK's focus trap); Escape closes the topmost layer only |
| Focus | Into it on open; back to the opener on close, or to the nearest thing left |
| Announced | Its name and role, as focus moves in; the rest of the page is hidden from screen readers while it is open |
| Hover and tap | A press on the scrim closes it, unless `disableClose` |
| Target size | Comfortable controls: 44px |
| Text scaling | The sheet is rem; its content scrolls inside it at 130% and 400% zoom |
| Colour | — |
| Motion | The sheet rises in over `duration-base`; at once under reduced motion |

## Related components

- Button — its actions; the `solid` one commits
- Field and Input — a dialog's form
- Toast — the outcome, once the dialog has closed
