# Num

A number with its unit.

**Status:** Draft

A value written by Foundations · Numerals, with its unit set smaller and muted right after it: 84 HP/5s, 12s, 24.8%. The ListRow's value and the LevelPlate's side stats use it. Step 12 of the Angular design-system plan replaces `lg-num` with numeral pipes.

**Provide:** `value` — a number, or a value already formatted with its unit (`lgFormatUnit(84, 'HP/5s')`) — and optional `unitClass`, the unit's class (`lg-unit` by default): `<lg-num [value]="'84 HP/5s'" />`.

- **Format first** (Foundations · Numerals). Num splits a unit off to style it, but never adds one: pass values from the `lgFormat*` helpers (`LG_FORMAT`). A plain number gets thousands separators and a true minus (12,480, −12).
- **The unit follows its number** at the `caption` size in Barlow 500, `ink-muted` (`.lg-unit`). Symbol units attach (24.8%, 12s); word units keep their non-breaking space (84 HP/5s).
- **Signs stay whole:** a range (12–18), a multiplier (×1.5) and a fraction (3,120 / 4,150) are one number; only a unit after them is split off.
- **Never empty.** A value that is unknown or does not apply (`null`, `NaN`, an empty string) shows "—"; zero shows "0". Text that is not a number shows as it is.
- **It takes the type around it:** the number is set in its parent's numeral style, with the root's tabular figures. Only the unit is styled.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | Text, with no role of its own |
| Keyboard | Not focusable |
| Focus | Nothing |
| Announced | The number and its unit, as written |
| Hover and tap | Nothing |
| Target size | Not a target |
| Text scaling | Grows with the text around it; the unit's `caption` size is in rem |
| Colour | The unit is also smaller and after its number, not only `ink-muted` |
| Motion | Nothing |

## Related components

- ListRow — the list row
- LevelPlate — the level display
- Ledger — the labelled value list
