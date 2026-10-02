# SearchField

Search with suggestions.

**Status:** Draft

A search input with a suggestions list — the player search on Overview, item search in the Bazaar.

**Provide:** `[(value)]`, `suggestions` (strings), `(pick)` (a suggestion chosen), `(submitted)` (Enter with nothing highlighted), `loading`, `searched` (show "No matching players" when the list is empty) and a `label` or `placeholder`.

- It is an ARIA combobox: arrows move through suggestions, Enter picks, Escape closes.
- Input is `ground-deep` with a `line-strong` edge at `radius-control`, the same corner as the Search button beside it; the list is `surface-raised` with `shadow-float` at `radius-float`; the highlighted option takes `gilt-soft`.
- The input is the control height of its region: 44, 40 or 32px (Foundations · Space & Density); the Search button beside it matches.
- Pair it with an explicit Search button — players expect one.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | An ARIA combobox named by `label` or `placeholder`, with a `listbox` of suggestions |
| Keyboard | Down opens and moves through suggestions; Enter picks; Escape closes the list and stops there |
| Focus | Stays in the input; the highlighted option is `aria-activedescendant` |
| Announced | The suggestion count and "No matching players" when a search finds nothing |
| Hover and tap | Nothing |
| Target size | The control height: 44, 40 or 32px |
| Text scaling | The input fills its row; suggestions wrap |
| Colour | The highlighted option also takes the raised wash, never colour alone |
| Motion | Nothing |
