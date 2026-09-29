# Grimoire components

Angular edition of the **Grimoire** design system (the dark, gilt-and-arcana look
from the Legend's Legacy design-system artifact). Every component is standalone,
OnPush and signal-based, and carries the `lg-` prefix so it can live next to the
current `ll-` UI while screens are moved over one at a time.

Nothing in the app uses these yet — adding them changes no existing screen.

## Setup (already done on this branch)

- `src/styles/grimoire/tokens.css` — colour, type, spacing and layout tokens.
- `src/styles/grimoire/components.css` — all `lg-*` component styles.
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

The artifact documents the React versions. The Angular ones keep the same names
and props, with these conventions:

| React | Angular |
| --- | --- |
| `<Button variant="quiet">` | `<button lgButton="quiet">` (also on `<a>`) |
| `<Heading level="screen">` | `<h2 lgHeading="screen">` |
| `aside={…}`, `footer={…}` props | projected content with `lgSlot="aside"` / `lgSlot="footer"` |
| `onSelect`, `onChange` callbacks | outputs (`(select)`, `(pick)`, `(navigate)`) |
| controlled `value` + `onChange` | `model()` inputs — use `[(value)]`, `[(activeId)]` |
| `renderText` | `ng-template lgChronicleText` |

Components: Banner, Button, Chronicle, Constellation, CurrencyPill, Emblem,
EntryList, Folio, GameShell, Heading, Icon, ItemLink, ItemSlot, JourneyCard,
KeyHints, Ledger, LevelPlate, LoadoutSlot, Meter, NavRail, Page, PageHeader,
Panel, Presence, SearchField, SectionRule, Sigil, Stage, StatFigure, StatTile,
TabStrip, Tag, TopBar, Track.

Helpers in `grimoire-core.ts`: `lgFormatNumber`, `lgFormatShort`, `lgPolygon`,
`LG_RARITY_CODES`, the `LgRarity` type and the `LG_SHELL` token.
Icons live in `grimoire-icons.ts` (`LgIconName` lists the names).
