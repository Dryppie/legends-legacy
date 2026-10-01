# AccessibilitySpecimen

The accessibility rules on real parts.

**Status:** Draft

Foundations · Accessibility, shown on the parts players use most. The bar at the top sets the game's own settings on the page root: Reading size (Default, Large at 115%, Extra large at 130%) and Reading font (the game default, the readable sans and the system font). Everything below scales with it, because every size is in rem.

- **Inventory:** a flush Panel with a short CurrencyPill and a List of four ListRows. The List is one tab stop: Tab lands on the selected row, and the arrow keys, Home and End move.
- **Combat Attributes:** a Ledger whose explanations open on hover and on focus, pin on click or tap, and close on Esc. Magic Penetration shows "—".
- **Screen reader hears:** what is announced for a short amount (12.5k is read as "12,480 Cinders"; the Inventory pill toggles to the full figure on click, tap or Enter), a rarity code (read by name), a change and an icon-only control.
- **Announcements:** three buttons send a claim result, a completed floor and an error through `LL.announce`. The transcript below them shows what was said and whether it was polite or assertive. Combat hits are never announced.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | The setting choices are toggle Buttons (`aria-pressed`) in labelled groups. The transcript is `aria-hidden`, because the live regions already speak. |
| Keyboard | As the parts it shows: List and Ledger rove; Buttons are Tab stops. |
| Focus | The focus ring on every part. |
| Announced | Only what the Announcements buttons send. |
| Hover and tap | The Ledger explanations. |
| Target size | Small Buttons are 32px high; the icon button is 40px. |
| Text scaling | The point of the card: check it at all three sizes. |
| Colour | Rarity is shown by colour and code, and announced by name. |
| Motion | Nothing |

## Related components

- ListRow — the list row
- Ledger — the labelled value list
- CurrencyPill — the currency amount
- Button — the command button
- Panel — the content box
