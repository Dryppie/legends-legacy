# Input

A text input or textarea in Grimoire's well.

**Status:** Draft — its look is Proposed (D-144), for review

The native `input` or `textarea`, styled: `<input lgInput />`, `<textarea lgInput></textarea>`. The host is the element itself, so `formControlName`, `ngModel`, `[value]`, `(input)`, `type`, `placeholder`, `maxlength` and `autocomplete` work as on any input, and forms need no value accessor. Put it in an `lg-field` for its label, hint and error.

- **A well** (Foundations · Surfaces & Layering): `ground-deep` with a `line-strong` edge (Foundations · Lines: never `line`, too faint to find), at `radius-control`, `control-*` tall for its region's density, `body-compact` words in `ink`, the placeholder in `ink-muted`.
- Hover lifts the edge to `ink-muted`; focus is the `focus-ring`; an error turns the edge `danger` (from its Field); disabled steps the words to `ink-disabled` and the edge to `line`. Read-only drops the well.
- A textarea starts at two controls' height and is resized only downward.
- A placeholder is an example ("e.g. EMBR"), never the label.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | Edge: `line-strong`; Fill: `ground-deep` | The value | Its label and value |
| Hover | Edge: `ink-muted` | — | Nothing |
| Focus-visible | Edge: `focus-ring` | — | Its label |
| Error | Edge: `danger`, with its Field's message | — | "invalid entry" |
| Disabled | Text: `ink-disabled`; Edge: `line` | — | "dimmed" |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A native text box, named by its Field's label (or a `label for` of your own: an `id` you give wins) |
| Keyboard | Native |
| Focus | `focus-ring` |
| Announced | Its label, value, description and state |
| Hover and tap | The edge lifts |
| Target size | `control-*` tall: 32 to 44px |
| Text scaling | rem; it fills its Field's width |
| Colour | Its edge holds 3:1 against the surface (Foundations · Accessibility) |
| Motion | Nothing |

## Related components

- Field — a labelled input, with its hint or error
