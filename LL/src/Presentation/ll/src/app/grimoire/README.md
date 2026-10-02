# Grimoire

The code of the **Grimoire** design system: its only implementation (D-127, D-133), imported as `@grimoire`. Its
rules, token values, icon drawings and docs are in `design-system/` (two folders up from `src/`), and
`design-system/AGENTS.md` says how to change it. Every part is a standalone, OnPush, signal-based component with the
`lg-` prefix (D-128), so it can live next to the legacy `ll-` UI while screens move over one at a time. The Grimoire
shell and the Character Overview use it, behind Settings → Interface → New look.

## What is where

| Folder | Holds |
| --- | --- |
| `index.ts` | The public API: every part, directive, helper and token constant, and `LG_GRIMOIRE` |
| `tokens/` | Generated from `design-system/tokens.json` by `design-system/scripts/build-tokens.mjs`: `tokens.css` (every token as a `--lg-*` property, the `.lg-type-*` classes, `@font-face`), `tokens.ts` (`LG_DURATION`, `LG_EASING`, `LG_BREAKPOINT`, `LG_CONTENT_TIER`, `LG_LAYER`) and `fonts/`. Never edit them (D-131). |
| `styles/` | What no part owns: `base.css`, `layout.css`, `attention.css`, `tip.css` (the one tip float, D-134), and `grimoire.css`, which puts every global stylesheet in cascade order (D-132); `frame-corners.css` is the corner ornaments the Folio and the Banner each take into their own styles |
| `core/` | Shared helpers (below), and `grimoire-icons.ts`, generated from `design-system/icons.json` by `design-system/scripts/build-icons.mjs` |
| `primitives/`, `components/`, `game/`, `shell/` | One folder per part: `<name>.component.ts` (or `<name>.directive.ts` for a part without markup), with the part's region components in the same file, `<name>.component.css`, `<name>.component.spec.ts` (where there is one yet) and `README.md`, its guidelines |
| `testing/` | Test harnesses for the parts, and the announcer helpers, imported as `@grimoire/testing` (D-130) |
| `showcase/` | The dev-only `/grimoire` showcase: every part in each of its states (D-129). Its README says how to add a story. |

A folder imports only the folders before it: `tokens`, `styles` → `core` → `primitives` → `components` → `game` →
`shell`. Grimoire imports only Angular, the CDK, rxjs and itself, and the app imports `@grimoire`, never a path inside
it. `design-system/scripts/check.mjs` checks both.

## Using it

Import single parts, or spread `LG_GRIMOIRE` to get everything:

```ts
import { LG_GRIMOIRE } from '@grimoire';

@Component({
  imports: [...LG_GRIMOIRE],
  // …
})
```

A part's regions are child components, each styling itself (D-136); a part with regions also exports them together
(`LG_PANEL`, `LG_FOLIO`, …):

```html
<lg-panel>
  <lg-panel-header>
    <lg-panel-title>Character</lg-panel-title>
    <button lgButton="quiet">Details</button>
  </lg-panel-header>
  …panel body…
</lg-panel>
```

Parts not yet re-shaped (plan phase 3, steps 14 to 18) still take named slots through the `lgSlot` directive
(`<lg-tag lgSlot="tags">`); it is part of `LG_GRIMOIRE` and goes in step 19.

A screen that has moved to Grimoire lives in a `-grimoire` folder, carries the `lg-root` class on its host, and styles
itself with tokens only (`var(--lg-ink-muted)`); `src/app/features/game/character/character-overview-grimoire/` is the
worked example.

### The shell and the chat setting

`lg-game-shell` lays out the rail, top bar, stage, Folio and Chronicle. Its `chatLayout` input takes the same
`'docked' | 'floating'` value the Settings page stores, so it can be bound straight to `ChatLayoutPreferenceService`:

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

- **Docked:** the Chronicle gets its own right-hand column when the shell is at least 96rem wide (`breakpoint-wide`,
  1536px at the default text size), sits under the Folio from 60rem (`breakpoint-shell`), and becomes a bottom dock
  below it. Collapsing it hands the space back to the stage.
- **Floating:** a drawer the player drags by its grip (arrow keys nudge it 16px) and can stretch with the tall toggle.
  `chroniclePosition` is a two-way model, so the position can be saved.

Custom message rendering (item links, player names) goes in an `ng-template lgChronicleText`:

```html
<lg-chronicle …>
  <ng-template lgChronicleText let-message>
    <app-chat-message-text [message]="message" />
  </ng-template>
</lg-chronicle>
```

## Styles

- `styles/grimoire.css` imports the tokens, the base and the global part files in cascade order.
  `angular.json` builds it as its own stylesheet, `grimoire.css` (`bundleName: "grimoire"`, `inject: false`), outside
  the app's first load, and `main.ts` calls `lgLoadStyles(APP_VERSION)` from `core/grimoire-styles.ts`, which adds it
  after `src/styles.css` without blocking the first render, so it wins over Tailwind's preflight (D-096, D-132).
- A re-shaped part (Panel, Page, PageHeader, Banner, Folio, Notice and Ledger so far, D-136, D-137) is its own box and takes its CSS
  through `styleUrl` under Angular's default emulated encapsulation: it styles its own template and its host
  (`:host`), never what is projected into it. Its regions carry their few rules in `styles`. Context reaches it through
  inherited custom properties (`--lg-panel-inset`, `--lg-layout-gap`) or `LG_SHELL`, never a selector from another part.
- The other part files are still plain global CSS, not attached to their components: selectors still reach from one
  part into another (`.lg-shell .lg-topbar`), so keep the order in `grimoire.css`. Plan phase 3 moves each file to
  its component's `styleUrl`, encapsulated, as it re-shapes the component.
- The root text size follows Grimoire: 16px at Default, 115% and 130% for the larger reading sizes (`src/styles.css`).
  Legacy styles keep their size because the build rebases their rem values by 0.875 (`scripts/postcss-legacy-rem`);
  it leaves everything under `src/app/grimoire/` and the `-grimoire` folders alone (D-096).
- `src/index.html` loads Marcellus, Barlow, Barlow Condensed, EB Garamond and Atkinson Hyperlegible from Google Fonts;
  `tokens/fonts/` holds the bundled Atkinson Hyperlegible for the readable font setting.

## Tests

Each part's spec sits beside it and drives the part through its harness in `testing/` (`LgButtonHarness`,
`LgTipHarness`, `LgPanelHarness`, …), so a spec says what a player does and sees, not how the markup is built; when a part's
markup changes, only its harness does. `testing/announcer.ts` reads what screen readers are told, with the fakeAsync
rules the announcer's queue needs. A feature's own spec uses the same harnesses from `@grimoire/testing`.

## Helpers

| File | What it gives |
| --- | --- |
| `core/grimoire-format.ts` | Numerals (Foundations · Numerals): `LG_FORMAT`, `lgFormatNumber`, `lgFormatShort`, `lgFormatDuration`, `lgSpokenDuration`, … |
| `core/grimoire-numerals.ts` | The same in templates (D-135): the pipes `lgNumber`, `lgShort`, `lgPercent`, `lgUnit`, `lgDuration` and `lgValue`, and `LG_NUMERAL_PIPES` |
| `core/grimoire-states.ts` | Standards · States: `LG_STATES`, `LG_TAG_ORDER`, `lgTopState`, `lgIsBlocked`, `lgBlockedReason`, `lgShortfallText` |
| `core/grimoire-announcer.ts` | `LgAnnouncer`: what screen readers are told, on the CDK's `LiveAnnouncer`, rate-limited and de-duplicated (D-134) |
| `core/grimoire-tip.ts` | `LgTip`: the one tip float on the CDK overlay, which tooltips, Ledger explanations and reason tips share (D-134) |
| `core/grimoire-blocked.ts` | A blocked control's reason (Standards · States): `[lgBlocked]`, `LgBlockedController`, `lgBlockedSpoken` |
| `core/grimoire-a11y.ts` | Roving focus (`lgMoveKey`), until the CDK's key managers replace it (the Ledger uses `FocusKeyManager`) |
| `core/grimoire-motion.ts` | `LG_MOTION`, `lgMotionMs`, `lgReducedMotion`, live values (`lgLive()`) and live lists (`[lgLiveList]`) |
| `core/grimoire-ornament.ts` | The ornament budget's warnings (`LG_ORNAMENT_BUDGET`, `lgForbiddenZone`, `lgCheckFramed`, `lgCheckOrnamentRule`) |
| `core/grimoire-icons.ts` | `LG_ICONS`, `LgIconName`, `LG_ICON_NAMES`, `LG_ICON_MARKERS` (generated) |
| `core/grimoire-core.ts` | `lgSlot`, `lgCx`, `lgUniqueId`, `LG_RARITY_CODES`, the `LG_SHELL` token |
| `core/grimoire-styles.ts` | `lgLoadStyles` |

Governance · Code (`design-system/docs/10-governance/04-code.md`) has the conventions, each part's selector, slots and
two-way bindings, and the checks a change must pass.
