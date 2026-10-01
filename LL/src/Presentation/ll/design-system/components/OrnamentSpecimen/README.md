# OrnamentSpecimen

The decorative devices and the budget, counted on the Creature Archive.

**Status:** Draft

Foundations · Ornament, shown on the game's own parts. Two parts, top to bottom:

- **Decorative devices.** Each device with a sample, its status, what it is for and where it may go: CornerOrnament, the Divider asset, the diamond-chain rule, the double gilt frame, film grain (magnified), the vignette, the retired `glow-selected` and `glow-gilt` (with the flat marks that replaced them), `shadow-text-art`, and the Emblem, which is a sign rather than an ornament.
- **Budget on a screen.** The Creature Archive twice, at 1,280px. The first is ScreenArchive as the system builds it, with its Folio's corners: one framed surface, no ornament rule, grain and a vignette over the Stage's art, nothing lit but the selection. The second adds what the budget forbids: a Banner over the stage beside the Folio, ornament rules instead of the Folio's bands and inside a dense Panel, corners and a parchment texture on that Panel, grain behind the Folio's text, halos on the selected creature, the Rare Essence slot and the level numeral, and a pulsing count. Under each, `LL.ornament.audit` counts the devices and lists what is over.

The over-decorated screen's extra devices are static markup in the preview's own styles, tagged `data-ornament` so the audit counts them. They are specimens of the mistakes, not styles to copy, and they keep the components' own console warnings quiet in the catalogue.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | Each part is a `section` named by its heading; each screen is a GameShell with its own landmarks; each tally is a named list |
| Keyboard | Both screens work as ScreenArchive does: the rail, the tabs and the EntryList are live |
| Focus | `focus-ring` on every control. The halos in the second screen are the faults on show, not focus |
| Announced | Every device is hidden from assistive technology (`aria-hidden`); the tallies read as text |
| Hover and tap | As in ScreenArchive |
| Target size | As in ScreenArchive |
| Text scaling | The screens keep their 1,280px width and reflow inside it, as the game does |
| Colour | An over-budget count says "Over" as well as taking the `danger` edge |
| Motion | The pulsing count in the second screen is the fault on show; it stops under reduced motion |

## Related components

- Folio — the detail panel
- Banner — the headline block
- SectionRule — the dividers
- Stage — the scene behind the screen
- ItemSlot — the item frame
- Button — the command button
- Emblem — the attribute sign
