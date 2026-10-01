# Grimoire components

Angular edition of the **Grimoire** design system (the dark, gilt-and-arcana look
whose source of truth is `design-system/`, two folders up from `src/`). Every component is standalone,
OnPush and signal-based, and carries the `lg-` prefix so it can live next to the
current `ll-` UI while screens are moved over one at a time.

Nothing in the app uses these yet — adding them changes no existing screen.

**Same as the design system.** Each component renders the markup of its React
reference component in `design-system/components/bundle.js` and shares its
stylesheet, and the parity check in `design-system/parity/` proves it (D-093).
Every design-system change ports here in the same step (`design-system/AGENTS.md`).

## Setup (already done on this branch)

- `src/styles/grimoire/tokens.css` and `fonts/` — every token and the bundled
  font, copied from `design-system/` by `design-system/scripts/sync-styles.mjs`.
- `src/styles/grimoire/components.css` — all `lg-*` component styles: a copy of
  `design-system/components/bundle.css`, made by the same script (D-092).
  Never edit these copies; change `design-system/` and run the script.
- Both are listed in `angular.json` → `styles`, after `src/styles.css`, so they
  win over Tailwind's preflight.
- `src/index.html` loads Barlow, Barlow Condensed, EB Garamond and Atkinson
  Hyperlegible from Google Fonts (Marcellus was already loaded).

## Using them

Import single components, or spread `LG_GRIMOIRE` to get everything:

```ts
import { LG_GRIMOIRE } from '../../shared/components/grimoire'; // adjust the relative path

@Component({
  standalone: true,
  imports: [...LG_GRIMOIRE],
  // …
})
```

Named slots use the `lgSlot` directive (it is part of `LG_GRIMOIRE`):

```html
<lg-panel title="Character">
  <button lgButton="quiet" lgSlot="aside">Details</button>
  …panel body…
</lg-panel>
```

## The shell and the chat setting

`lg-game-shell` lays out the rail, top bar, stage, Folio and Chronicle. Its
`chatLayout` input takes the same `'docked' | 'floating'` value the Settings
page stores, so it can be bound straight to `ChatLayoutPreferenceService`:

```html
<lg-game-shell [chatLayout]="chatLayout.layout()" [(chroniclePosition)]="chatPosition">
  <lg-nav-rail lgSlot="rail" [sections]="nav" [activeId]="activeRoute()" />
  <lg-top-bar lgSlot="top" title="Overview" eyebrow="Ashenreach" />

  <router-outlet />  <!-- default slot = the stage -->

  <lg-chronicle
    lgSlot="chronicle"
    [channels]="channels()"
    [messages]="messages()"
    [(activeChannel)]="channel"
    [open]="chatLayout.dockedOpen()"
    (openChange)="chatLayout.setDockedOpen($event)"
    (send)="sendMessage($event)"
  />
</lg-game-shell>
```

- **Docked:** the Chronicle gets its own right-hand column when the shell is at
  least 1536px wide, sits under the Folio from 960–1535px, and becomes a bottom
  dock below 960px. Collapsing it hands the space back to the stage.
- **Floating:** a drawer the player drags by its grip (arrow keys nudge it
  16px) and can stretch with the tall toggle.
  `chroniclePosition` is a two-way model, so the position can be saved.

Custom message rendering (item links, player names) goes in an
`ng-template lgChronicleText`:

```html
<lg-chronicle …>
  <ng-template lgChronicleText let-message>
    <app-chat-message-text [message]="message" />
  </ng-template>
</lg-chronicle>
```

## Mapping from the design-system docs

`design-system/` documents the React reference versions (`components/bundle.js`,
`components/index.d.ts`). The Angular ones keep the same names and props, with
these conventions (Governance · Code parity has the full table, component by
component):

| React | Angular |
| --- | --- |
| `<Button variant="quiet">` | `<button lgButton="quiet">` (also on `<a>`) |
| `<Heading level="screen">` | `<h2 lgHeading="screen">` |
| `<List>` with `<ListRow>` | `<lg-list>` with `<li lgListRow>` |
| ReactNode props (`aside`, `footer`, `actions`, `thumb`, …) | projected content with `lgSlot="aside"`, `lgSlot="footer"`, … |
| `onClick` that makes a part a control | `interactive` input plus `(activate)` output |
| other callbacks (`onSelect`, `onNavigate`, `onSend`, …) | outputs (`(select)`, `(navigate)`, `(send)`; SearchField: `(pick)`, `(submitted)`) |
| controlled `value` + `onChange` | `model()` inputs — use `[(value)]`, `[(activeId)]` |
| `renderText` | `ng-template lgChronicleText` |
| `LL.motion.useLive` | `lgLive()` in a field initializer |
| `LL.motion.useLiveList` | `[lgLiveList]` directive (`#live="lgLiveList"`, then `live.rows()`) |
| `LL.layers.use` | `lgOpenLayer()`, closing the handle when the layer goes |
| the reason tip | `[lgWhy]` directive, built into Button, EntryList, ItemSlot, Sigil, LoadoutSlot and NavRail |

Components: Banner, Button, Chronicle, Constellation, CurrencyPill, Delta,
Emblem, EntryList, Folio, GameShell, Heading, Icon, ItemLink, ItemSlot,
JourneyCard, Key, KeyHints, Ledger, LevelPlate, List and ListRow, LoadoutSlot,
Meter, NavRail, Num, Page, PageHeader, Panel, Presence, SearchField,
SectionRule, Sigil, Stage, StatFigure, StatTile, TabStrip, Tag, TopBar, Track.

Helpers:

- `grimoire-format.ts` — numerals (`LG_FORMAT`, `lgFormatNumber`, `lgFormatShort`, …).
- `grimoire-states.ts` — Standards · States (`LG_STATES`, `lgTopState`, `lgBlockedReason`, …).
- `grimoire-a11y.ts` — the announcer, the layer stack, roving focus and the reason tip.
- `grimoire-motion.ts` — motion tokens, reduced motion, live values and live lists.
- `grimoire-ornament.ts`, `grimoire-icons.ts` (`LgIconName` lists the names),
  and `grimoire-core.ts` (`lgSlot`, `lgCx`, `LG_RARITY_CODES`, the `LG_SHELL` token).
