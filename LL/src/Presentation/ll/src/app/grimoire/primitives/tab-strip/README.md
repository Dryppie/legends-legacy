# TabStrip

The tabs.

**Status:** Draft

Tabs across the top of an archive screen, in two levels: engraved section tabs and small-caps filters under them.

**Provide:** `tabs` (`[{ id, label, count? }]`), `[(activeId)]`, `level` (`primary` | `secondary`) and `density`. Arrow keys move between tabs.

- Primary: `tab` style; the active tab is a `tile` block with `on-tile` text and an `arcana-glow` underline; `line-strong` separators between the rest.
- Secondary: `label`-sized capitals, active in `ink` with the same underline; `count` in `arcana`.
- **Density:** a primary tab is the control height — 44, 40 or 32px — with 20, 16 or 12px side padding; a secondary tab is 32, 28 or 24px. Use Compact over a Compact table or list; the text keeps its style in every mode.
- Use primary for what you are browsing (Creatures, Regions), secondary for how it is filtered (All, Discovered). Never three levels.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `tablist` named by `label`; each tab is a `tab` with `aria-selected` |
| Keyboard | One tab stop (the active tab): Left and Right move and select |
| Focus | `focus-ring`; the focused tab rises above its neighbours |
| Announced | The tab's label and its count |
| Hover and tap | Nothing |
| Target size | Primary tabs are the control height (44, 40 or 32px); secondary 24–32px |
| Text scaling | Primary tabs scroll sideways inside the strip when they no longer fit |
| Colour | The active tab is a filled block with an underline, not a colour |
| Motion | Nothing |
