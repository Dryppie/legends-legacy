# Checkbox

A choice to tick, with its words.

**Status:** Draft — its look is Proposed (D-144), for review

A native checkbox with its words beside it, for a choice that waits for a Save, or one of several that can all be ticked ("Visible channels"). A setting that takes effect at once is a Switch.

**Provide:** its words as content; `[(checked)]`, or a boolean through `formControlName` and `ngModel`; `indeterminate` for a group that is partly ticked; `disabled`.

```html
<lg-checkbox formControlName="showNobility">Show my Nobility</lg-checkbox>
```

- **A rectangle, not a circle** (Foundations · Shape: a circle means presence): a 16px well at `radius-control` with a `line-strong` edge.
- **Checked is selected**, arcana's job (Foundations · Colour): an `arcana-glow` fill with the tick drawn in `ground`. Mixed is a dash on the same fill.
- Its words are `body-compact` in `ink`, `space-2` from the box; the whole row takes the press.
- Hover lifts the edge to `ink-muted`; focus puts the `focus-ring` on the box; disabled steps the words to `ink-disabled` and the edge to `line`.
- The Chronicle's channel settings use it, in a Popover (D-146); it replaced the native `lg-check` there.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | Edge: `line-strong` on `ground-deep` | Its words | "Show my Nobility, checkbox, not checked" |
| Selected (checked) | Fill: `arcana-glow`; Icon: the tick | — | "checked" |
| Mixed | Fill: `arcana-glow`; Icon: a dash | — | "mixed" |
| Hover | Edge: `ink-muted` | — | Nothing |
| Focus-visible | Edge: `focus-ring` on the box | — | Its words and state |
| Disabled | Text: `ink-disabled`; Edge: `line` | — | "dimmed" |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A native `checkbox`, named by its words (the label around it) |
| Keyboard | Tab to it; Space ticks it |
| Focus | `focus-ring` on the box |
| Announced | Its words and state |
| Hover and tap | The whole row takes the press |
| Target size | The row: at least 24px tall |
| Text scaling | The words wrap beside the box |
| Colour | Checked is also the tick; mixed the dash |
| Motion | Nothing |

## Related components

- Switch — a setting, on or off at once
- Radio group — one choice among a few
