# Radio group

One choice among a few, all in view.

**Status:** Draft — its look is Proposed (D-144), for review

Two to five choices, each with a few words, one of them chosen: a loot rule, a chat layout. More choices, or long ones, go in a Select; two to four short words for a setting take less room as a Segmented.

**Provide:** `label` (the group's legend), `lg-radio`s with their `value` and words, `[(value)]` or `formControlName` and `ngModel`, `orientation="horizontal"` for a row, `disabled` for the group or a radio.

```html
<lg-radio-group label="Chat layout" formControlName="chatLayout">
  <lg-radio value="docked">Docked</lg-radio>
  <lg-radio value="floating">Floating drawer</lg-radio>
</lg-radio-group>
```

- A `fieldset` with its `legend`, set like a Field's label; the radios one under the other, `space-1` apart, or in a row.
- **A rectangle, not a circle** (Foundations · Shape: a circle means presence): the Checkbox's 16px well, told apart by its mark. Chosen, its edge turns `arcana` and an `arcana-glow` square fills its middle, where a Checkbox shows a tick.
- Hover lifts the edge to `ink-muted`; focus puts the `focus-ring` on the box; a disabled radio, or the group, steps back to `ink-disabled` and `line`.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | Edge: `line-strong` on `ground-deep` | Its words | "Docked, radio button, 1 of 2" |
| Selected | Edge: `arcana`; Marker: the `arcana-glow` square | — | "selected" |
| Hover | Edge: `ink-muted` | — | Nothing |
| Focus-visible | Edge: `focus-ring` on the box | — | Its words and state |
| Disabled | Text: `ink-disabled`; Edge: `line` | — | "dimmed" |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `fieldset` named by its `legend`; native radios sharing one name, each named by its words |
| Keyboard | Tab enters at the chosen radio; the arrow keys move the choice; Tab leaves the group |
| Focus | `focus-ring` on the box |
| Announced | The legend, then each radio's words, position and state |
| Hover and tap | The whole row takes the press |
| Target size | Each row: at least 24px tall |
| Text scaling | The words wrap; a row wraps to more lines |
| Colour | Chosen is also the square |
| Motion | Nothing |

## Related components

- Segmented — two to four short choices, side by side
- Checkbox — a choice to tick
