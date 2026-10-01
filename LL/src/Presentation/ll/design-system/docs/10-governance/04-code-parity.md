# Governance · Code parity

The game uses the Angular edition of Grimoire: the `lg-*` components in `src/app/shared/components/grimoire/`. This page records how far that edition matches the React reference (`components/bundle.js`), how its API maps onto the reference props, and how parity is checked. It was written when Phase C of the repository migration brought the port up to D-091 (1 October 2026, D-093).

**Status:** every component in the reference bundle is ported and matches it. Nothing is behind.

## How parity is kept

- **One stylesheet.** `components/bundle.css` is the `lg-*` stylesheet for both editions. `scripts/sync-styles.mjs` copies it, `tokens.css` and the fonts into `src/styles/grimoire/` (D-092). The Angular components carry no styles of their own.
- **The same markup.** Each Angular component renders the DOM its reference component renders: the same elements, classes, ARIA attributes and text. Component hosts use `display: contents`, so they add no box; attribute components (`button[lgButton]`, `h2[lgHeading]`, `li[lgListRow]`) render on the element they sit on.
- **The same behaviour.** Keyboard models, the reason tip, the layer stack, the announcer and live values are ported as shared helpers (below), not rewritten per component.
- **The parity check** (`design-system/parity/`, its README says how to run it) renders every case in both editions on one page and compares them:
  - **Static:** 107 cases across every component and variant, plus the format and state helpers against `LL.format`, `LL.states` and `LL.topState` — 403 comparisons. The DOM is normalised first (attribute and class order, Angular host elements and comments, generated ids).
  - **Behaviour:** 22 scenarios play the same clicks and keys on both sides and compare the DOM, the focused element, the reason tip, handler calls, scroll position, the top layer and every announcement.

  A change to a component is not done until both pass (Checks in `AGENTS.md`).

## Shared behaviour

| Reference | Angular | File |
| --- | --- | --- |
| `LL.format.*`, numeral constants | `LG_FORMAT`, `lgFormatNumber`, `lgFormatShort`, `lgFormatPercent`, `lgFormatDuration`, … | `grimoire-format.ts` |
| `LL.states`, `LL.topState`, blocked and shortfall rules | `LG_STATES`, `LG_TAG_ORDER`, `lgTopState`, `lgIsBlocked`, `lgBlockedReason`, `lgShortfallText` | `grimoire-states.ts` |
| `LL.announce` | `lgAnnounce` | `grimoire-a11y.ts` |
| `LL.layers.open`, `.use`, `.top` | `lgOpenLayer` (returns a handle; close it when the layer goes), `lgTopLayer` | `grimoire-a11y.ts` |
| roving focus (`moveKey`) | `lgMoveKey` | `grimoire-a11y.ts` |
| the reason tip (`useWhy`) | `[lgWhy]` directive, `LgWhyController` | `grimoire-a11y.ts` |
| `LL.motion.ms`, `.reduced`, `.useLive` | `lgMotionMs`, `lgReducedMotion`, `lgLive()` | `grimoire-motion.ts` |
| `LL.motion.useLiveList` | `[lgLiveList]` directive, `exportAs: 'lgLiveList'` (`rows()`, `pending()`, `release()`) | `grimoire-motion.ts` |
| ornament helpers | `grimoire-ornament.ts` | |
| `LL.motion.audit`, `LL.ornament.audit` | not ported: they check catalog previews, not the game | |

## Components

Every row is **Ported · parity-checked**. Lifecycle status (Draft, Stable) is the component page's, and is unchanged by the port.

| Component | Angular | Inputs and outputs that differ from the props |
| --- | --- | --- |
| GameShell | `<lg-game-shell>` | Slots `rail`, `top`, `folio`, `hints`, `chronicle`; the stage is the default content. `[(chroniclePosition)]`. A TopBar inside the shell opens the rail drawer itself (`LgShellApi.openRail()`). |
| TopBar | `<lg-top-bar>` | `(menu)` for `onMenu`; slot `center`. |
| NavRail | `<lg-nav-rail>` | `(navigate)`; slots `header`, `footer`. |
| TabStrip | `<lg-tab-strip>` | `[(activeId)]`. |
| Stage | `<lg-stage>` | — |
| Page | `<lg-page>` | No `embedded` input: the reference has none. |
| PageHeader | `<lg-page-header>` | `actions` is a slot (`lgSlot="actions"`). |
| Banner | `<lg-banner>` | Slots `aside`, `footer`. |
| Folio | `<lg-folio>` | Slots `emblem`, `lore`, `actions`, `footer`. |
| Panel | `<lg-panel>` | Slot `aside`. |
| StatFigure | `<lg-stat-figure>` | — |
| LevelPlate | `<lg-level-plate>` | — |
| Ledger | `<lg-ledger>` | — |
| Meter | `<lg-meter>` | — |
| Track | `<lg-track>` | — |
| StatTile | `<lg-stat-tile>` | — |
| Delta | `<lg-delta>` | `extraClass` for `className`. |
| Sigil | `<lg-sigil>` | `interactive` and `(activate)` for `onClick`. |
| Constellation | `<lg-constellation>` | `(select)`. |
| JourneyCard | `<lg-journey-card>` | `title` for `heading`; `id` is optional. Slot `actions`. |
| LoadoutSlot | `<lg-loadout-slot>` | `interactive` and `(activate)`. |
| Chronicle | `<lg-chronicle>` | `[(activeChannel)]`, `[(open)]`, `[(draft)]`, `(send)`; `toggleable` and `composer` inputs; slot `aside`; `ng-template lgChronicleText` for `renderText`. |
| ItemLink | `<lg-item-link>` | `interactive` and `(activate)`. |
| EntryList | `<lg-entry-list>` | `[(activeId)]`. |
| ListRow | `<lg-list>` with `<li lgListRow>` rows | `interactive` and `(activate)`; slots `thumb`, `tags`, `meta`, `trailing`. |
| ItemSlot | `<lg-item-slot>` | `interactive` and `(activate)`. |
| Tag | `<lg-tag>` | `label` replaces the state word; `ariaHidden` for `hidden`. |
| Presence | `<lg-presence>` | — |
| CurrencyPill | `<lg-currency-pill>` | `interactive` and `(activate)`. |
| Button | `<button lgButton>`, `<a lgButton>` | The variant is the attribute's value: `lgButton="quiet"`. |
| KeyHints | `<lg-key-hints>` | — |
| SearchField | `<lg-search-field>` | `[(value)]`; `(pick)` for `onSelect`, `(submitted)` for `onSubmit` (`select` and `submit` are DOM event names). |
| Heading | `<h2 lgHeading>` (h1–h6) | The level is the attribute's value: `lgHeading="screen"`. |
| SectionRule | `<lg-section-rule>` | Slot `aside`. |
| Emblem | `<lg-emblem>` | — |
| Icon | `<lg-icon>` | — |
| Key, Num | `<lg-key>`, `<lg-num>` | Reference helpers without a page of their own; `lg-num` takes `unitClass`. |

**General rules.** A ReactNode prop is a projected slot (`lgSlot="name"`). An `onClick` that makes a component a control is an `interactive` input plus an `(activate)` output. A controlled value with its `onChange` is a `model()`, bound with `[( )]`. Other callbacks are outputs named for the event, without `on`, except where that name is a DOM event the host would also fire (SearchField).

**Not ported.** The Foundations specimens, the Page Archetype screens (ScreenOverview, ScreenArchive), PatternFeedback and the Cover are catalogue previews, not components.

## Known gaps

- **Root size.** Grimoire's rem sizes assume a 16px root. The game sets the root from the reading-size setting (14, 16 or 18px), so at the default 14px every `lg-*` size is an eighth smaller than in the catalog. Media queries are not affected (they use the browser's 16px). To be settled when the first screen moves to Grimoire.
- **Fonts and body.** The app copy of `components.css` leaves out the catalog's Google Fonts `@import`, its `body` rule and its root font-size rules (`sync-styles.mjs` says why). `src/index.html` loads the same families.
- **Live list, `keep` after release.** In both editions a kept key (`keep`) that has left the data stays in place while the list is held, but drops out on `release()` (the player pressing "2 new listings"), because release forgets the order the key held its place in. Foundations · Motion (Live updates, rule 3) and the comment on `useLiveList` in `bundle.js` say it stays until the player picks another. The port matches the reference; the reference should be fixed in the step that next touches it, and ported with it.
- **Build check.** The app's `ng build` compiles only what the app imports, and no screen imports the `lg-*` components yet, so it does not check them. The parity page's build (`npx ng build` in `design-system/parity`) compiles every component with strict templates; until a screen uses them, that is the compile check.
