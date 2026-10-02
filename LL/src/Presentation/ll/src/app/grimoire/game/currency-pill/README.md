# CurrencyPill

The currency amount.

**Status:** Draft

Cinders or Soulstones with their art and amount, for the TopBar and shop headers.

**Provide:** `name`, `amount`, `iconSrc` (Currency/Cinders.webp for Cinders, Currency/Soulstones.webp for Soulstones), `short` to abbreviate (12.5k), `reserve` (characters of width to hold for a live amount) and `tooltip` when the tip should say what a press does. The host is the element: `<button lgCurrencyPill>` when a press does something, `<span lgCurrencyPill>` to show the amount. A short pill is a button, so the full figure is one press away: with `toggle` it switches between 12.5k and 12,480 itself; without, your `(click)` decides — the TopBar switches every pill and keeps the format as a setting.

- **Art here, line icons inline.** The pill is the display size, so it takes the full-colour art. A cost list, a Ledger row, a reward line or running text uses the resource's line icon at 16px in `currentColor` instead, before the resource's name, and never the art (Foundations · Iconography · Resource, Registries · Resources).
- The art is 22px (1.375rem), contained: the currency emblems are tall shards and need the height (D-126).
- Order: art, amount, name. Amount in `numeral-row` (Barlow Condensed 18px) with tabular figures, name in `label` capitals `ink-muted`, on `surface` at `radius-control`: the name is the game's, but the shape is a small engraved plate, like a Button (D-064). A pill that only shows an amount has a `line` edge; one that opens or toggles is a control, so its edge is `line-strong` and hover is the `surface-raised` wash (Foundations · Lines).
- **Live amounts do not reflow the TopBar.** The amount is right-aligned in its own width, which grows to fit and never shrinks while the pill is on screen; pass `reserve` to hold room for the next digit from the start (D-035). The pill is not a live region — announce gains in the Chronicle's loot line instead.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `button`, or text in a `span`. Screen readers hear the full amount and name ("12,480 Cinders"), whatever is shown |
| Keyboard | A button pill is a Tab stop: Enter or Space switches the format (`toggle`) or does what your `(click)` does. A `span` pill is not focusable |
| Focus | `focus-ring` |
| Announced | The full figure, never "12.5k". It is not a live region: announce gains through the Chronicle's loot line or `LgAnnouncer` |
| Hover and tap | The full figure (or the `tooltip` words) is in the tip on hover and focus and is read out, and a press shows it, so nothing is hover-only |
| Target size | 30px high |
| Text scaling | In the TopBar, the name gives way to the art under 40rem and the strip scrolls sideways at 320px; the amount keeps its reserved width |
| Colour | The art and the name say which currency, never colour |
| Motion | Nothing |
