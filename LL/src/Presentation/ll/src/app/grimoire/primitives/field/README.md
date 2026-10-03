# Field

A labelled input, with its hint or error.

**Status:** Draft — its look is Proposed (D-144), for review

A form field: its label above the control, a hint under it, and an error in the hint's place once the player has touched the control and it is invalid. The control is an `input[lgInput]` or `textarea[lgInput]` (later a Select). Settings is its first screen (plan section 13).

**Provide:** `label`, an optional `hint` (what to enter: "3 to 16 letters") and the control as content. The Field reads the control's errors from its `NgControl` — Reactive Forms or `ngModel` — and says the first in words: `messages` names a validator's error (`{ required: 'Name your character.' }`), and a few have defaults ("Fill this in.", "At least 3 characters."). `error` sets the words yourself, whatever the control's state (a save the server refused). `required` marks the label; `Validators.required` does it by itself.

```html
<form [formGroup]="form">
  <lg-field label="Character name" hint="3 to 16 letters" [messages]="{ required: 'Name your character.' }">
    <input lgInput formControlName="name" />
  </lg-field>
</form>
```

- The label is `body-compact` 600 in `ink`, above the control with `space-1` between; a required one adds a muted " *" (screen readers hear "required" from the control).
- The hint and the error are `caption`. The hint is `ink-muted`; the error is the ✕ and the words in `danger` (Standards · States · Error), and the control's edge turns `danger`. The error says what to do next, in the player's terms (Standards · Content).
- An error waits until the control is touched or changed: an empty required field the player hasn't reached is not wrong yet.
- One control per Field. Group related Fields with space, not boxes (Foundations · Lines).

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | Text: the label above, the hint under | The label; the hint | The label; the hint as the description |
| Error | Text and icon: ✕ and the message in `danger`; Edge: the control's in `danger` | "Name your character." | "invalid entry", then the message (live, polite) |
| Disabled | Text: the control's words in `ink-disabled` | — | "dimmed" or "unavailable" |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `label for` the control's id names it; the hint, or the error, is its `aria-describedby`; an error sets `aria-invalid`; required sets `aria-required` |
| Keyboard | The control's own |
| Focus | The control's `focus-ring` |
| Announced | The error, politely, when it appears (a live region that is always there) |
| Hover and tap | The label focuses the control |
| Target size | The control: `control-*` tall (32 to 44px) |
| Text scaling | The label, hint and error wrap; the control fills the Field's width |
| Colour | An error is also its ✕ and its words |
| Motion | Nothing |

## Related components

- Input — `input[lgInput]`, `textarea[lgInput]`
- SearchField — search with suggestions
