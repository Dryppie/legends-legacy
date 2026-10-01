# Game shell → Grimoire: scoping

1 October 2026. A read-only pass, like the Character Overview's: nothing in the shell was changed. It compares the game's frame today (`src/app/layout/dashboard/`) with Grimoire's GameShell and the parts it holds (TopBar, NavRail, Stage, Folio, Chronicle, KeyHints), and lists what each has that the other lacks.

**Agreed so far:** the new shell sits behind the same single switch as every migrated screen: Settings → Interface → New look (`GrimoirePreviewPreferenceService.newLook()`, which replaced the per-screen Character Overview preview). With it on, the game shows the Grimoire shell and every screen that has a new look; screens that haven't moved render unchanged inside the new shell.

**Status (1 October 2026, evening): steps 1–3 built.** Design system D-104 to D-114 (NavRail descriptions, gold icons and journey locks with the `lock` marker; GameShell backdrop, `lg-legacy` host, compact rail column and narrow TopBar; Activity, Objective, Notice; the Chronicle's own composer, day breaks, player actions and Nobility). App: `src/app/layout/dashboard-grimoire/` — the whole frame and the chat as a Chronicle with a Loot channel, chosen by `DashboardSwitchComponent` from `newLook()`. Checks: design-system check 0 errors; parity 441/441 static, 25/25 behaviour; 895 unit tests pass; `ng build ll` succeeds. Screenshots were taken from a mocked harness only, so step 4 (review in the running game) is next. Known: mention highlighting and the welcome modal haven't been seen against live data.

**Review fix (D-115):** after Martin's first look the frame's lines were made to meet — the TopBar is a surface band with a rule that continues the chat's head, its insets match the stage gutter, an old screen's sheet is inset by that gutter on every side, the compact rail's icons are centred with small corner counts and an unboxed Activity, and screens inside the world mark the World Map.

**Second review (D-116, D-117):** the inset sheet read as a frame inside a frame, so an old screen's sheet now fills the stage between the frame's lines with no edge of its own, notices included, its content at the Page's side gutter. The compact rail's current action is the old sidebar's mark again: the ring the progress rises in, the ✦, the live dot and "Battling".

## 1. What the frame is today

`dashboard.component` (561 + 217 lines), `game-header` (93 + 98), `sidebar` (233 + 130, with `sidebar-item`), `chat` (1,300 + 912, plus the composer, mentions and item links), `quest-tracker` (176 + 296), `loot-tracker` (99 + 57).

| Part | What it shows and does |
| --- | --- |
| Frame | The game's textured backdrop (`background.webp`) behind everything; `ll-game-surface` (a texture with a warm-to-cool gradient and a border) behind the routed screen. Full-viewport height; nothing scrolls the page. |
| Sidebar (20rem detailed, 8rem compact — Settings → Sidebar layout) | Current action at the top ("Battling" with a progress bar; goes to the action). Four sections — Character (Overview, Inventory, Essences, Combat Styles, Achievements, Soulstones), World (World Map, World Tower, Quests, Prophecies), City (Guild, Colosseum, Cinder Bazaar, Leaderboard), System (Settings). Each item: its line icon (an SVG with a baked gold gradient, always gold, drawn larger than 20px; Combat Styles swaps to a filled variant when active), title, a one-line description ("Stats, vitals, loadout"), a notification count, a quest cue ("Next"). Items are hidden until the player journey reveals them (`filterSidebarForPlayerJourney`). |
| Header | Character name and level; Cinders and Soulstones pills (a click toggles short and full numbers); the pinned quest tracker (title, chain step, current objective with progress, expandable); the current dungeon and raid. On phones: menu button, the page title and its section. |
| Main | Shell notices above the screen: "Multiplayer access restricted" (with the return date), "Catching up offline progress", "Offline progress paused" with Retry; "Entering the game" / "Game state unavailable" with Retry while bootstrapping. Then the routed screen. |
| Chat | Docked (from 1280px: a 336px column, 430px at 1536px+, collapsing to a 42px strip) with the Loot History box under it; or Floating (a draggable drawer, normal or tall). Phones: a bottom dock, 5.5rem or expanded. Channels All, General, Invites, Guild, Whisper, Trade, Help, System and Raid (while in one); a visible-channels chooser; @mentions with a suggestion list; item links; whispers. |
| Phones | Swipe from the edge opens the sidebar (64vw); the header moves above everything. |

## 2. Component map

| Today | Grimoire | Notes |
| --- | --- | --- |
| `dashboard` frame | `lg-game-shell` (`chatLayout` from the existing Chat layout setting) | A new `dashboard-grimoire` beside the old one; the route's layout picks one by `newLook()` |
| Sidebar sections and items | `lg-nav-rail` (`sections`, `activeId`, `navigate`) | Same `SidebarService` data and journey filter; titles and section labels unchanged |
| The sidebar's SVG icons | Grimoire `Icon`s | The same 15 drawings — Grimoire's set was taken from these files, path for path. What changes is how they're shown: `currentColor` instead of the baked gold gradient (`ink-muted` at rest, `gilt` only on the active item), at 20px, and no filled Combat Styles variant when active. World Tower is `legacy-ascension`, Quests `quest-journal` |
| Notification counts | NavRail `badge` + `badgeLabel` | Same counts and labels |
| Quest cue "Next" | NavRail `ready` (the arcana diamond) with words | One mark per item (Standards · State combinations) |
| Sidebar layout: compact | NavRail `compact` | Grimoire's compact is icons only, names as tooltips; today's compact also prints a small title |
| Character name, level, currencies | `lg-top-bar` (`title`, `eyebrow` "Lv. 17") with `lg-currency-pill`s | The short/full toggle stays a pill click |
| Current dungeon / raid | TopBar `center`: a Track | TopBar's rule: the centre holds one Track while a run is in progress |
| Phone header (menu, page title, section) | TopBar with `showMenu`; GameShell's rail drawer | Escape, focus return and `inert` come with the shell |
| Chat | `lg-chronicle` in the shell's `chronicle` slot | See S7 |
| Loot History box | The Chronicle's Loot channel (D-005) | Decision 3 |
| Routed screens | GameShell's stage | A migrated screen is an `lg-page`; a legacy one needs a host (S2) |

## 3. Gaps in Grimoire (design-system work first)

| # | Gap | Why it matters | Proposal |
| --- | --- | --- | --- |
| S1 | **The game's backdrop.** GameShell paints flat `ground`, with art only inside a Stage. | D-101 promised the Overview keeps the game's backdrop when the frame moves to GameShell; nothing in GameShell says so. | GameShell takes a `backdrop` image painted at Level 0 behind the rail, stage and docked chat; the rail and TopBar sit on it in the D-102 panel material. Decision log entry; GameShell docs, port, parity case. |
| S2 | **Legacy screens inside GameShell.** The D-100 guards switch the game's legacy element styles off inside `.lg-shell`, so an unmigrated screen there would lose its headings, labels and inputs. Legacy screens also expect today's padding and the `ll-game-surface` panel. | Every screen that hasn't moved renders inside the new shell. | A legacy host region: guards that stop at `.lg-root` but restart inside a `.ll-legacy` host (or `@scope (…) to (.lg-root)`), plus an app-side `LegacyStage` wrapper giving today's padding and surface. A migrated screen in the shell drops Page `flow`. |
| S3 | **Current action.** No Grimoire place for "what am I doing now" with its progress. | Every idle game screen; players return to it constantly. | A small Current activity block at the head of the NavRail (its `header` slot), with a Meter and the go-to action; in `compact`, the icon with the bar. DS item. |
| S4 | **Pinned quest.** The TopBar's centre is reserved for one Track, and its rule says anything more belongs in the page. | The tracker guides onboarding; it can't vanish. | Decision 4: the TopBar centre shows one "now" thing — the run's Track during a dungeon or raid, else the pinned quest's current objective with its progress — and opens the full tracker as a popover. TopBar rule and docs change. |
| S5 | **Shell notices.** No Grimoire component for a persistent notice with an action (restricted access, offline progress, bootstrap errors). | Every screen, and every later Grimoire screen. | A Notice component (`info`, `warning`, `danger`; a word, an optional action), Level 1 at the head of the stage. DS item; it closes gap G6 of the Overview plan too. |
| S6 | **Journey-hidden destinations.** NavRail's rule (D-087) keeps locked items visible with their reason; the game hides destinations until the player journey introduces them. | Onboarding depends on the reveal. | Decision 2. Recommended: a NavRail rule that a destination not yet introduced is absent, and one blocked for another reason is Locked. |
| S7 | **Chat parity.** Chronicle has channels, unread counts, whispers, item links, mentions, collapse, drag, tall and the phone dock. It lacks the composer's @mention suggestions, and a Loot channel needs data the game keeps only in the Loot History box. | The chat is the largest part of the frame (about 2,500 lines). | Decision 5: the shell first with today's chat placed by GameShell, then the Chronicle port as its own step with a suggestion list added to Chronicle's composer (DS item). |
| S8 | **Rail descriptions.** NavRail items have a title only; today's items carry a description line. | The rail is 14rem in Grimoire, 20rem today. | Decision 1. |

## 4. App gaps

| # | Gap | Proposal |
| --- | --- | --- |
| A1 | Sidebar data to NavRail shape (icon names, badges, ready, locked) | A small adapter beside `SidebarService`, tested |
| A2 | Loot lines in chat | Feed the Loot History entries to the Chronicle as Loot-channel lines (if Decision 3 is yes) |
| A3 | The swipe gesture that opens the sidebar on phones | Keep it in the app, calling GameShell's `openRail()` |
| A4 | Settings | Done: one New look switch. The Sidebar layout and Chat layout settings keep working in both shells |

## 5. Layout fit

At 1600px, docked chat, Default text: Grimoire gives rail 14rem (224px) + stage + chat 24rem (384px), so the stage is about 990px; today it is about 960px (sidebar 280px and chat 336px after the rem rebase, plus gaps). At Large text (115%) the 96rem breakpoint is 1,766px, so at 1600px the chat leaves its own column and takes the right column, as Grimoire specifies for screens without a Folio. Checked for real in step 4 at 1600×900 and 1920×1080, Default and Large, docked and floating, and at phone width.

## 6. Order of work

1. **Design system:** S1 backdrop, S2 legacy host, S3 Current activity, S4 TopBar "now" slot, S5 Notice, S6 the nav rule. Docs, `bundle.*`, the `lg-*` port and parity cases.
2. **App:** `dashboard-grimoire` behind `newLook()`: GameShell, NavRail via the adapter, TopBar with currencies and the "now" slot, notices, legacy host for unmigrated screens. The Overview drops `flow` inside it. Specs.
3. **Chronicle, in the same pass (decision 5):** the chat port (channels, composer with suggestions, mentions, item links, Loot channel), so the new shell ships with it.
4. **Review** at the sizes above, keyboard and screen reader; fix; then make the new look the default and delete the old frame.

## 7. Decisions (Martin, 1 October 2026)

1. **Rail descriptions stay.** Each destination keeps its description line, so a player never has to recall a destination from its icon. NavRail gains `description` (S8).
2. **Destinations the journey hasn't introduced show as Locked**, with how they unlock — D-087's rule as written. The adapter turns the journey filter into `locked` + `reason` instead of hiding (S6).
3. **Loot History moves into the chat as the Loot channel** (D-005).
4. **The pinned quest's current objective sits in the TopBar centre**, replaced by the run's Track during a dungeon or raid and opening the full tracker; the current action sits at the head of the rail (S3, S4).
5. **The chat is ported in the same pass**, not after: the new shell ships with the Chronicle, not today's chat inside it.
6. **Rail icons stay gold at rest**, as today. This narrows Foundations · Iconography's colour rule (gilt for the current location only) for the NavRail's icons; the current destination is still marked by the diamond, the weight and `ink`.
7. **The game's textured backdrop stays across the whole frame** (S1, as D-101 promised).
