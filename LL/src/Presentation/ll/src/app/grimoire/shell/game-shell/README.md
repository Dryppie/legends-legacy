# GameShell

The screen frame.

**Status:** Draft

The full-screen frame every in-game screen sits in: NavRail on the left, the stage in the middle, the Folio and the Chronicle (chat) on the right.

**Provide:** a `rail` slot (a NavRail), a `top` slot (a TopBar — with `showMenu`, its menu button opens the rail drawer on phones), the stage as content (a Stage), and optionally the `folio`, `hints` and `chronicle` (a Chronicle) slots. Pass the player's chat-layout setting straight through as `chatLayout`: `docked` or `floating` — the same values as the game's `ChatLayout` preference. For the floating drawer you can control and persist its position with `[(chroniclePosition)]` (`{ left, bottom }` in px from the shell's bottom-left).

**Backdrop** (D-107). `backdrop` takes an image — in the game, its textured backdrop — painted at Level 0 behind the rail, the stage and the docked Chronicle. Over it the rail and the docked Chronicle take Level 1's translucent `surface` (D-102), and a Page shows it (D-101); without it the shell is `ground`. Over a backdrop the TopBar is a band of the frame too — `surface` with a hairline under it — and the stage starts below it rather than under it, so nothing scrolls beneath a translucent band (D-115).

**Legacy content** (D-108). While screens migrate, a screen not built with Grimoire sits in the stage inside an `lg-legacy` region: it fills the stage below the TopBar, and Grimoire's inherited type and colour stop there. Set `--lg-legacy-font`, `--lg-legacy-size`, `--lg-legacy-leading`, `--lg-legacy-weight` and `--lg-legacy-ink` to the host's own body values; the host's element styles restart inside it and stop again at any `lg-root` within. A migrated screen in the stage is a Page and needs none of this.

**Layout.** Columns are `rail-width` · fluid · `folio-width`, inside a host capped at `shell-max`. The TopBar floats over the stage art on a `ground` fade (over a backdrop, it is a band above the stage; D-115). **The lines meet** (D-115): from 60rem the TopBar's insets are the Page's gutter, so its title starts and its currencies end where the screen's edges are; the docked Chronicle's head, beside no Folio, is `topbar-height` tall, so its rule and the TopBar's are one line; a rail's header starts level with the TopBar's controls, and a compact rail's header is a band of the same height with the same rule. A screen that hasn't migrated fills the stage between those lines with no edge of its own, its content inset by the Page's side gutter (D-116).

**Layers** (Foundations · Surfaces & Layering). The stage is Level 0 content. The TopBar, KeyHints and the NavRail sit on `z-chrome`, the Folio and the docked Chronicle on `z-folio`, and the floating drawer and bottom dock on `z-chat-float`. The rail drawer and its scrim sit on `z-overlay`. Popovers, dialogs, toasts and the tour sit above them all.

**Chat layout: Docked.** The Chronicle lives on the right side of the page.
- 96rem (1536px at the default text size) and wider: its own column at the far right, `chronicle-width`, next to the Folio. Collapsed, it shrinks to a `chronicle-strip` vertical tab with the unread count, and the stage takes the space.
- 60–96rem (960–1535px): it shares the right column with the Folio, under it — `chronicle-height` open, a `chronicle-collapsed` ticker when collapsed. On screens without a Folio it takes the whole right column.

**Chat layout: Floating drawer.** The Chronicle becomes a drawer over the stage, `chronicle-float-width` wide (`chronicle-float-width-wide` at 96rem and wider), `chronicle-float-height` open or `chronicle-float-tall` when made taller, a bar when collapsed. It starts at the stage's bottom-right, beside the Folio; the grip drags it (arrow keys nudge it 16px) and the shell keeps it inside the frame. KeyHints move to the stage's bottom-left.

**Small screens.** Under 60rem (960px) of container width (a container query, so it works inside any host) the rail becomes an off-canvas drawer over `scrim`, the Folio stacks below the stage, KeyHints hide and both chat layouts become the bottom dock. The dock is `surface-solid`, so the page never shows through its messages. Under 40rem a TopBar with a `center` takes a second row for it (the TopBar is 6rem tall; D-114). The drawer slides in over `duration-base` and out over `duration-fast`; once it has left it is hidden, so nothing off-screen takes focus (Foundations · Motion · Spatial).

- Do give the shell a real height (`100vh` default) — the stage, Folio and Chronicle scroll inside it, the page never does.
- Do keep one Folio per screen; it explains the one thing the player has selected.
- Don't put the currency or level anywhere but the TopBar.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | `main` holds the stage and TopBar; the rail, Folio and Chronicle name themselves |
| Keyboard | "Skip to content" is the first tab stop; then the rail, TopBar, stage, Folio and Chronicle in that order. Under 60rem the rail is a drawer: Escape closes it |
| Focus | Opening the drawer moves focus to its first item and makes the rest `inert`; closing returns focus to the menu button |
| Announced | Nothing of its own; one announcer (`lgAnnounce`) serves the app |
| Hover and tap | Nothing |
| Target size | The menu button is 36px |
| Text scaling | Columns are rem, so the shell reflows sooner at larger text: the Chronicle leaves its own column under 96rem and the rail becomes a drawer under 60rem (Foundations · Accessibility · Text scaling) |
| Colour | Nothing |
| Motion | The rail drawer slides in over `duration-base` on `ease-enter`, with its scrim, and out over `duration-fast` on `ease-exit`, then hides; dragging the floating Chronicle follows the pointer at once. At once under reduced motion |
