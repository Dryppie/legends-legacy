# Shell

The frame every in-game screen sits in — the rail, the top bar, the stage, the Folio, the Chronicle and the key hints — and where chat goes. GameShell builds it. The grid, its breakpoints and the stage width each setup leaves are in Foundations · Layout.

## Rules

**Must**
- Place the Chronicle where the player's **Chat layout** setting says: Docked (the default) or Floating drawer, the same two options as the game's Settings page (Decision D-002). Pass the setting to GameShell as `chatLayout`.
- Keep one Folio per screen (Decision D-004).
- Keep the currency and level in the TopBar and nowhere else.
- Show every game destination in the NavRail, each with its description. One the player journey hasn't introduced yet is Locked, with how it unlocks — never hidden (D-104, D-106).
- Put loot in the Chronicle's Loot channel, not a separate box (Decision D-005).
- Give the shell a real height; the stage, Folio and Chronicle scroll inside it.
- Give the game's shell its textured backdrop (`backdrop`, D-107): the frame shows it, and an information screen shows it through its Page (D-101).
- While screens migrate, hold a screen not built with Grimoire in an `lg-legacy` region in the stage (D-108). Its sheet fills the stage between the frame's lines, with no edge or gutter of its own, and its content keeps the Page's side gutter (D-116).
- Let the frame's lines meet: over a backdrop the TopBar is a `surface` band whose rule continues the docked Chronicle's head and a compact rail's header band; its insets are the Page's gutter (D-115).

**Should**
- Give detail and collection screens one subject: the stage shows it, the Folio explains the selected thing. Dense workbench screens — the Cinder Bazaar, the guild's member list — may set several side by side (Principles · One screen, one subject; D-010).
- Give the TopBar's centre one "now" thing: a run's Track while a dungeon run or tower climb is in progress, else the pinned quest's Objective (D-110); empty when there is neither.
- Put the current action, an Activity, at the head of the NavRail (D-109).
- Show shell-wide conditions — restricted access, progress being caught up, a failed load — as Notices at the head of the stage (D-111).
- Give KeyHints three or four hints, only for keys that work on this screen.

**Never**
- Float anything else: the stage shows the subject, the Folio explains the selected thing, the TopBar says who you are, the Chronicle is what everyone is saying and what just happened.
- Put a health bar in the TopBar — HP belongs on combat screens.
- Put navigation in the Folio.

**Known inconsistency.** LevelPlate shows a level on detail views — the Overview's Combat Profile, a creature in the Archive — while the rule above keeps the level in the TopBar. Read the rule as: the player's own level and currencies, as persistent status, live in the TopBar. Settle it with a Decision Log entry.

## Regions

| Region | Part | Holds |
| --- | --- | --- |
| Rail | NavRail, with an Activity at its head | The current action, then the game's real sections (Character, World, City, System) |
| Top | TopBar | Who you are, how you are doing, what you carry: name or screen title, an eyebrow, CurrencyPills; in its centre one "now" thing — a run's Track or the pinned quest's Objective. Its insets are the Page's gutter, and its rule continues the docked Chronicle's head (D-115) |
| Stage | Stage or Page | The subject: a scene with art, or a scrolling information screen |
| Folio | Folio | The one thing the player has selected |
| Chronicle | Chronicle | Chat and the game log, placed by the chat layout |
| Hints | KeyHints | The keys that work on this screen, at the stage's foot |

## Chat layouts

**Docked** (default): on the right side of the page.
- At 1536px (96rem, so later at larger text) and wider it is its own column at the far right (`chronicle-width`), beside the Folio, and collapses to a `chronicle-strip` vertical tab with the unread count while the stage takes the space.
- From 960 to 1535px it shares the right column with the Folio, under it (`chronicle-height`, collapsing to a `chronicle-collapsed` ticker). Screens without a Folio give it the whole column.

**Floating drawer:** a draggable drawer over the stage — a Level 1 floating surface on the `z-chat-float` layer, above the Folio and below every popover (Foundations · Surfaces & Layering) — starting at its bottom-right beside the Folio (`chronicle-float-width`, `chronicle-float-width-wide` at 1536px and wider, `chronicle-float-height`, and `chronicle-float-tall` when made taller). The grip drags it, arrow keys nudge it 16px, and the shell keeps it inside the frame. Its position can be saved (`chroniclePosition`). Key hints move to the stage's bottom-left.

**Under 960px** (60rem, so sooner at larger text) both layouts become the bottom dock (`surface-solid`), the rail becomes a drawer over `scrim`, the Folio stacks under the stage and key hints hide. The drawer takes focus when it opens, makes the rest `inert`, closes on Escape and returns focus to the menu button. A "Skip to content" link is the shell's first tab stop (Foundations · Accessibility). **Under 640px** (40rem) a TopBar with a centre takes two rows: the title, level and currencies above, the "now" thing across the width below (D-114).

## Tokens used

The tokens are in rem; the values below are at the default text size and grow 15% or 30% with the reading-size setting.

| Token | Value | Role |
| --- | --- | --- |
| `chronicle-width` | 384px | Docked, 1536px and wider: the Chronicle's own column |
| `chronicle-strip` | 48px | Docked and collapsed to a vertical strip |
| `chronicle-height` | 320px | Docked and open under the Folio; the open bottom dock |
| `chronicle-collapsed` | 44px | The one-line ticker: under the Folio, as a floating bar, in the bottom dock |
| `chronicle-float-width` | 352px | Floating drawer width |
| `chronicle-float-width-wide` | 384px | Floating drawer width at 1536px and wider |
| `chronicle-float-height` | 448px | Floating drawer, open |
| `chronicle-float-tall` | 896px | Floating drawer, tall; never taller than the screen |
| `rail-width`, `folio-width`, `topbar-height`, `hintbar-height`, `shell-max` | | The grid (Foundations · Layout) |
| `scrim` | | Behind the rail drawer |

## Do and don't

| Do | Don't |
| --- | --- |
| Read the chat layout from the player's setting and pass it to GameShell. | Pick a chat position per screen. |
| Show the Track of a tower climb in the TopBar's centre, and the pinned quest's Objective otherwise. | Put an HP bar, a quest list and a clock in the TopBar. |
| Announce "You found [Ember Fang]" in the Loot channel. | Open a loot box over the stage. |

## Related components

- GameShell — the screen frame
- Activity — the current action
- Objective — the pinned quest
- Notice — the persistent notice
- NavRail — the main navigation
- TopBar — the top bar
- Stage — the scene backdrop
- Page — the information screen frame
- Folio — the detail panel
- Chronicle — chat and the game log
- KeyHints — the keyboard shortcut hints
- CurrencyPill — the currency amount
