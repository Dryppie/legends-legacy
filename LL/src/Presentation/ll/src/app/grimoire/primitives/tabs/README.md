# Tabs

The tabs.

**Status:** Draft

Tabs across the top of an archive screen, in two levels: engraved section tabs and small-caps filters under them. They were TabStrip (D-138).

**Provide:** `<lg-tabs>` with `label`, the selected tab's key as `[(selected)]`, `level` (`primary` | `secondary`) and `density`; inside it, one `<button lgTab [key]>` per tab, its words as content and an optional `count`, then a `<lg-tab-panel [key]>` per tab if the tabs switch panels. A filter strip has no panels: the screen filters what it shows by `selected`.

```html
<lg-tabs label="Archive" [(selected)]="section">
  <button lgTab key="creatures">Creatures</button>
  <button lgTab key="regions" [count]="2">Regions</button>
  <lg-tab-panel key="creatures">…</lg-tab-panel>
  <lg-tab-panel key="regions">…</lg-tab-panel>
</lg-tabs>
```

**Route tabs.** When each tab is a page, use links: `<nav lgTabNav label="Archive sections">` with `<a lgTabLink routerLink="creatures" routerLinkActive ariaCurrentWhenActive="page">`. The link marked `aria-current="page"` looks selected; Grimoire never reads the Router.

- Primary: `tab` style; the selected tab is a `tile` block with `on-tile` text and an `arcana-glow` underline; `line-strong` separators between the rest.
- Secondary: `label`-sized capitals, selected in `ink` with the same underline; `count` in `arcana`.
- **Density:** a primary tab is the control height — 44, 40 or 32px — with 20, 16 or 12px side padding; a secondary tab is 32, 28 or 24px. Use Compact over a Compact table or list; the text keeps its style in every mode.
- Use primary for what you are browsing (Creatures, Regions), secondary for how it is filtered (All, Discovered). Never three levels.
- A panel's content stays rendered while it is hidden.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | `lg-tabs`: a `tablist` named by `label`; each tab a `button` with `role="tab"`, `aria-selected` and `aria-controls` when it has a panel; each panel a `tabpanel` labelled by its tab. Route tabs: a `nav` named by `label`, its links marked `aria-current="page"` |
| Keyboard | `lg-tabs` is one tab stop (the selected tab): Left and Right move and select, wrapping; Home and End jump (the CDK's `FocusKeyManager`). A panel is the next tab stop. Route tabs are links, each its own tab stop |
| Focus | `focus-ring`; the focused tab rises above its neighbours |
| Announced | The tab's words and its count |
| Hover and tap | Nothing |
| Target size | Primary tabs are the control height (44, 40 or 32px); secondary 24–32px |
| Text scaling | Primary tabs scroll sideways inside the strip when they no longer fit; secondary tabs wrap |
| Colour | The selected tab is a filled block with an underline, not a colour |
| Motion | Nothing |

## Testing

`LgTabsHarness` (`@grimoire/testing`): find tabs by their words, press keys, read the selected tab, the tab stop and the panel that shows. `LgTabNavHarness` reads route tabs and the current one.

## Related components

- List — the list and its rows
- Panel — the content box
- PageHeader — the page's head, where a primary strip often sits
