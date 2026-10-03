# Option

An option in a list that keeps focus in its field.

**Status:** Draft

One row of a list that keeps focus in its field — a SearchField's suggestions, a Select's options (D-146). The field keeps focus and moves a highlight through the options (`aria-activedescendant`); the highlighted option is `aria-selected`.

**Provide:** `value` (what choosing it gives the field) and its words as content; `disabled` to show it but pass over it. It reports to the list it sits in through `LgOptionParent`, so it works only inside one.

```html
<lg-search-field label="Find a guild member" [(value)]="member" (pick)="view($event)">
  @for (m of members(); track m.id) {
    <lg-option [value]="m.name" [disabled]="m.away">{{ m.name }} · Lv {{ m.level }}</lg-option>
  }
</lg-search-field>
```

- A row on `radius-container`, `body-compact` words; the highlighted option takes `gilt-soft`, the old hover wash, queued in Governance · Audit & consolidation map with the rest of SearchField's colours. A disabled option is `ink-disabled`.
- In a Select, the current choice is Selected: a 2px `arcana-glow` bar at its start and weight 600 (`LgOptionParent.isChosen`).
- A press keeps focus in the field (the option takes no focus); the highlight follows the pointer.
- The highlighted option scrolls into view.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | An `option` in its list's `listbox`, named by its words; `aria-selected` while highlighted; `aria-disabled` when disabled |
| Keyboard | Through its field: Up and Down move the highlight, passing disabled options; Enter chooses |
| Focus | Stays in the field; the option is the active descendant |
| Announced | Its words when highlighted |
| Hover and tap | Hover highlights; a press chooses |
| Target size | The row: 32px at the default text size |
| Text scaling | Wraps |
| Colour | The highlight is a fill; in forced colours, a system outline |
| Motion | Nothing |

## Related components

- SearchField — search with suggestions
