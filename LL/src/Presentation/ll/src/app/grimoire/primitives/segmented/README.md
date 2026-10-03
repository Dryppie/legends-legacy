# Segmented

Two to four short choices, side by side.

**Status:** Draft — its look is Proposed (D-144), for review

A setting with two to four short words, one always chosen: Text size, Reading font, Sidebar. It replaces Settings' five `aria-pressed` button groups. Longer choices, or more, are a Radio group or a Select.

**Provide:** `label` (shown above it, like a Field's; `labelHidden` keeps it for screen readers where the setting's name is beside it), `lg-segment`s with their `value` and words, `[(value)]` or `formControlName` and `ngModel`, and `disabled` for it or a segment.

```html
<lg-segmented label="Text size" formControlName="textSize">
  <lg-segment value="default">Default</lg-segment>
  <lg-segment value="large">Large</lg-segment>
</lg-segmented>
```

- **One well, joined segments:** a `line-strong` edge at `radius-control`, `control-*` tall for its region's density, hairline `line` rules between the segments.
- A segment's words are `body-compact` 600 in `ink-muted`; hover takes the `surface-raised` wash and `ink`.
- **Chosen is selected**, arcana's job (Standards · States: the arcana ring): the `arcana-soft` fill with an `arcana` ring, the words in `ink`; the rules beside it give way to its ring.
- A disabled segment is `ink-disabled` and passed over by the keys.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | Text: `ink-muted` | Its words | "Large, radio button, 2 of 3" |
| Hover | Fill: `surface-raised`; Text: `ink` | — | Nothing |
| Focus-visible | Edge: `focus-ring` | — | Its words and state |
| Selected | Fill: `arcana-soft`; Edge: the `arcana` ring | — | "checked" |
| Disabled | Text: `ink-disabled` | — | "dimmed" |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `radiogroup` named by its label; each segment a `radio`, named by its words, `aria-checked` when chosen |
| Keyboard | One tab stop, the chosen segment; Left and Right (and Up and Down) move the choice, passing disabled segments and wrapping; Home and End go to the ends; Space or Enter chooses the focused segment |
| Focus | `focus-ring` |
| Announced | The label, then the segment's words, position and state |
| Hover and tap | The wash; a press chooses |
| Target size | `control-*` tall: 32 to 44px |
| Text scaling | Segments keep their words on one line; past its region's width, use a Radio group |
| Colour | Chosen is also the ring |
| Motion | Nothing |

## Related components

- Radio group — one choice among a few
- Switch — a setting, on or off at once
