# CurrencyPill

The currency amount.

**Status:** Draft

Cinders or Soulstones with their art and amount, for the TopBar and shop headers.

**Provide:** `name`, `amount`, `iconSrc` (Currency/Cinders.webp for Cinders, Currency/Soulstones.webp for Soulstones), `short` to abbreviate (12.5k), `reserve` (characters of width to hold for a live amount) and, optionally, `onClick`. A short pill is always a button that toggles between 12.5k and 12,480, as the game does today; pass `onClick` only when the game keeps the format as a setting.

- **Art here, line icons inline.** The pill is the display size, so it takes the full-colour art. A cost list, a Ledger row, a reward line or running text uses the resource's line icon at 16px in `currentColor` instead, before the resource's name, and never the art (Foundations · Iconography · Resource, Registries · Resources).
- The art is 22px (1.375rem), contained: the currency emblems are tall shards and need the height (D-126).
- Order: art, amount, name. Amount in `numeral-row` (Barlow Condensed 18px) with tabular figures, name in `label` capitals `ink-muted`, on `surface` at `radius-control`: the name is the game's, but the shape is a small engraved plate, like a Button (D-064). A pill that only shows an amount has a `line` edge; one that opens or toggles is a control, so its edge is `line-strong` and hover is the `surface-raised` wash (Foundations · Lines).
- **Live amounts do not reflow the TopBar.** The amount is right-aligned in its own width, which grows to fit and never shrinks while the pill is on screen; pass `reserve` to hold room for the next digit from the start (D-035). The pill is not a live region — announce gains in the Chronicle's loot line instead.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `button` when it is short or has `onClick`, else text. Screen readers hear the full amount and name ("12,480 Cinders"), whatever is shown |
| Keyboard | A short pill is a Tab stop: Enter or Space toggles the format. A full-figure pill without `onClick` is not focusable |
| Focus | `focus-ring` |
| Announced | The full figure, never "12.5k". It is not a live region: announce gains through the Chronicle's loot line or `LL.announce` |
| Hover and tap | The full figure is in the `title` and is read out, and a click, tap or key shows it, so nothing is hover-only |
| Target size | 30px high |
| Text scaling | In the TopBar, the name gives way to the art under 40rem and the strip scrolls sideways at 320px; the amount keeps its reserved width |
| Colour | The art and the name say which currency, never colour |
| Motion | Nothing |
