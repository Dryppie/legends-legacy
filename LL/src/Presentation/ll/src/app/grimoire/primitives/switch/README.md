# Switch

A setting, on or off at once.

**Status:** Draft — its look is Proposed (D-144), for review

A setting that takes effect the moment it is pressed: New look, Compact lists, damage numbers. A choice that waits for a Save is a Checkbox.

**Provide:** its words as content; `[(checked)]`, or a boolean through `formControlName` and `ngModel`; `disabled`.

```html
<lg-switch formControlName="newLook">New look</lg-switch>
```

- **A rectangle track with a square thumb** (Foundations · Shape: no pill): 36 by 20px at `radius-control`.
- **Off:** a well with a `line-strong` edge, the thumb at the start in `ink-muted`. **On** is selected, arcana's job: the `arcana-soft` track with an `arcana` edge, the thumb at the end in `arcana-glow`. The thumb's place says it too, never colour alone.
- Its words are `body-compact` in `ink`, `space-3` after the track; track and words are one button.
- **Motion** (Foundations · Motion · Feedback): the thumb slides over `duration-fast` on `ease-standard`; at once under reduced motion.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Off | Marker: the thumb at the start; Edge: `line-strong` | Its words | "New look, switch, off" |
| On (selected) | Marker: the thumb at the end in `arcana-glow`; Fill: `arcana-soft` | — | "on" |
| Hover | Edge: `ink-muted` | — | Nothing |
| Focus-visible | Edge: `focus-ring` on the track | — | Its words and state |
| Disabled | Text: `ink-disabled`; Edge: `line` | — | "dimmed" |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `button` with `role="switch"` and `aria-checked`, named by its words |
| Keyboard | Tab to it; Space or Enter turns it |
| Focus | `focus-ring` on the track |
| Announced | Its words and "on" or "off" |
| Hover and tap | The track and the words take the press |
| Target size | At least 24px tall |
| Text scaling | The words wrap beside the track |
| Colour | On is also the thumb's place |
| Motion | The thumb slides over `duration-fast`; at once under reduced motion |

## Related components

- Checkbox — a choice to tick
- Segmented — two to four short choices
