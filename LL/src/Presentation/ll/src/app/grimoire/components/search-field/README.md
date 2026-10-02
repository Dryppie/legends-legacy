# SearchField

Search with suggestions.

**Status:** Draft

A search input with a suggestions list — the player search on Overview, item search in the Bazaar.

**Provide:** `[(value)]`, `suggestions` (strings from a service) or projected `lg-option`s for richer rows, `(pick)` (an option chosen: its `value`), `(submitted)` (Enter with nothing highlighted), `loading`, `searched` (show "No matching players" when the list is empty) and a `label` or `placeholder`.

```html
<lg-search-field label="Search character by name" [(value)]="query" [suggestions]="names()" [loading]="finding()" [searched]="searched()" (pick)="view($event)" (submitted)="search($event)" />

<lg-search-field label="Find a guild member" [(value)]="member" (pick)="view($event)">
  @for (m of members(); track m.id) {
    <lg-option [value]="m.name" [disabled]="m.away">{{ m.name }} · Lv {{ m.level }}</lg-option>
  }
</lg-search-field>
```

- It is an ARIA combobox: arrows move a highlight through the suggestions while focus stays in the field (the CDK's `ActiveDescendantKeyManager`), Enter picks, Escape closes.
- The list opens on the CDK overlay, beneath the field and as wide as it (above it when there is no room), so no region clips it; it closes when the field loses focus. A press on an option keeps focus in the field.
- Input is `ground-deep` with a `line-strong` edge at `radius-control`, the same corner as the Search button beside it; the list is `surface-raised` with `shadow-float` at `radius-float`; the highlighted option (`lg-option`) takes `gilt-soft`, queued in the audit.
- The input is the control height of its region: 44, 40 or 32px (Foundations · Space & Density); the Search button beside it matches.
- Pair it with an explicit Search button — players expect one.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | An ARIA combobox named by `label` or `placeholder`, with a `listbox` of `option`s, named the same, while it is open (`aria-controls`) |
| Keyboard | Down opens and moves through suggestions, wrapping and passing disabled ones; Enter picks; Escape closes the list and stops there |
| Focus | Stays in the input; the highlighted option is `aria-activedescendant` |
| Announced | The suggestion count and "No matching players" when a search finds nothing |
| Hover and tap | Nothing |
| Target size | The control height: 44, 40 or 32px |
| Text scaling | The input fills its row; suggestions wrap |
| Colour | The highlighted option also takes the raised wash, never colour alone |
| Motion | Nothing |

## Testing

`LgSearchFieldHarness` (`@grimoire/testing`): types, presses keys, reads the suggestions, the highlighted one and the list's note, and clicks a suggestion; it finds the list on the overlay through the field's `aria-controls`.

## Related components

- Option — an option in a list that keeps focus in its field
- Button — the command button
