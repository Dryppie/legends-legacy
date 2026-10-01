# Constellation

The stat star chart.

**Status:** Draft

Sigils placed on thin orbit rings over the stage — the character sheet as a star chart.

**Provide:** a coordinate space (`width`, `height`), `items` (`[{ id, label, value, x, y, labelPosition? }]`), `rings` and `nodes` in the same coordinates, plus `selectedId` / `onSelect` (usually driving the Folio).

- Rings are `ink` at 30%; a `strong` ring is `gilt`. Nodes are short gilt ticks across the nearest ring, like an astrolabe's graduations. They were small diamonds; a diamond beside a Sigil reads as its ready mark (D-065, Foundations · Shape).
- The component keeps its aspect ratio and scales to its container's width; lay it out in pixel coordinates that match the stage size you design for.
- Five to seven items. Point labels away from the centre and keep them off the Folio edge.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `group` named by `label`; each Sigil is a toggle button named by its label and value |
| Keyboard | One tab stop (the selected Sigil, else the first); the arrow keys, Home and End move; Enter or Space selects |
| Focus | `focus-ring` on the Sigil's hex |
| Announced | "Power 11", with ", can be raised" or ", locked"; `aria-pressed` for the selected one |
| Hover and tap | Nothing |
| Target size | Sigils are 40, 52 or 68px |
| Text scaling | It keeps its aspect ratio and scales with its container; labels grow with the text |
| Colour | Ready is a diamond as well as a colour; selection also scales the badge |
| Motion | A selected Sigil scales to 1.08 over `duration-fast` on `ease-standard`; its ring changes at once. At once under reduced motion |
