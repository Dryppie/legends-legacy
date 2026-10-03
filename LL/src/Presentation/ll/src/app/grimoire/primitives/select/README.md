# Select

One choice from a list, in a field.

**Status:** Draft — its look is Proposed (D-146), for review

A filter or a setting with more choices than a Segmented holds, or choices too long to show side by side: rarity, slot, sort order, a character's class. The choices are known and few enough to scroll; a search through many players is a SearchField.

**Use:**

```html
<lg-field label="Rarity">
  <lg-select formControlName="rarity" placeholder="Any rarity">
    <lg-option value="Common">Common</lg-option>
    <lg-option value="Epic">Epic</lg-option>
  </lg-select>
</lg-field>
```

**Provide:** its options as `lg-option`s (`value`, the words as content, `disabled`); the value through `formControlName` or `ngModel` (a value accessor), or `[(value)]`; `placeholder` while nothing is chosen ("Any rarity": an example, never the label); `label` when it stands outside a Field; `disabled`. `(picked)` says the player chose, not when the value is set from outside. Values are strings.

- **The button is a well**, as an Input is: `ground-deep` with a `line-strong` edge at `radius-control`, as tall as the region's controls. The choice is `body-compact` in `ink`, the placeholder in `ink-muted`; the `expand` chevron sits at its end in `ink-muted` and turns over while the list is open.
- **The list** opens on the CDK overlay beneath it, at least as wide, so no region or dialog clips it: a Level 2 float of `lg-option`s, at most 16rem tall, moving `space-1` into place over `duration-fast`. The highlighted option takes the Option's wash; **the chosen one is Selected**: a 2px `arcana-glow` bar at its start and weight 600.
- **Focus stays on the button** while the highlight moves (`aria-activedescendant`), as the ARIA select-only combobox does.
- **In a Field** the Field's label names it, its hint or error describes it, and an error turns its edge `danger`.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | Edge: `line-strong` | The choice, or the placeholder | "Rarity, combobox, Epic" |
| Hover | Edge: `ink-muted` | — | Nothing |
| Focus-visible | Edge: `focus-ring` | — | Its name and choice |
| Open | Marker: the chevron turned over; the list beneath | — | "expanded"; the highlighted option |
| Selected (an option) | Edge: the `arcana-glow` bar; Typeface: 600 | — | "selected" on the highlighted option |
| Error | Edge: `danger`; the Field's ✕ and words | "Choose a class." | Its description |
| Disabled | Text: `ink-disabled`; Edge: `line` | — | "dimmed" |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `button` with `role="combobox"` and `aria-haspopup="listbox"`, named by the Field's label (or `label`); the list is a `listbox` of `option`s |
| Keyboard | Down, Up, Enter or Space open it; arrows move, Home and End go to the ends, typing finds an option by its words; Enter or Space chooses; Escape closes without a choice; Tab chooses the highlighted option and moves on |
| Focus | Stays on the button; `focus-ring` on it |
| Announced | The highlighted option as it moves (`aria-activedescendant`) |
| Hover and tap | The pointer highlights an option; a press chooses it |
| Target size | The button is a control's height; options are at least 32px |
| Text scaling | The choice truncates in the button; options wrap in the list |
| Colour | The chosen option is also weight 600 and its bar |
| Motion | The list moves `space-1` into place; at once under reduced motion |

## Related components

- Field and Input — its label, hint and error
- Option — its options, shared with the SearchField
- Segmented — two to four short choices, all in view
- SearchField — searching many names
