# Game shell → Grimoire: scoping

1 October 2026. A read-only pass, like the Character Overview's: nothing in the shell was changed. It compares the game's frame today (`src/app/layout/dashboard/`) with Grimoire's GameShell and the parts it holds (TopBar, NavRail, Stage, Folio, Chronicle, KeyHints), and lists what each has that the other lacks.

**Agreed so far:** the new shell sits behind the same single switch as every migrated screen: Settings → Interface → New look (`GrimoirePreviewPreferenceService.newLook()`, which replaced the per-screen Character Overview preview). With it on, the game shows the Grimoire shell and every screen that has a new look; screens that haven't moved render unchanged inside the new shell.

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
2. **App:** `dashboard-grimoire` behind `newLook()`: GameShell, NavRail via the adapter, TopBar with currencies and the "now" slot, notices, legacy host for unmigrated screens, today's chat in the shell's chat area. The Overview drops `flow` inside it. Specs.
3. **Chronicle:** the chat port (channels, composer with suggestions, mentions, item links, Loot channel), replacing today's chat inside the new shell.
4. **Review** at the sizes above, keyboard and screen reader; fix; then make the new look the default and delete the old frame.

## 7. Decisions for Martin

1. Rail descriptions ("Stats, vitals, loadout"): drop them, as Grimoire's narrower rail does (recommended), or add a description line to NavRail?
2. Destinations the journey hasn't introduced: keep them hidden (recommended, with the nav rule in S6), or show them Locked with how they unlock?
3. Loot History: move it into the chat as the Loot channel, as Grimoire decided in D-005 (recommended), or keep a separate box?
4. Pinned quest and current action: the quest's current objective in the TopBar centre (the run's Track replaces it during a dungeon or raid) and the current action at the head of the rail (recommended)?
5. Chat: the shell first with today's chat inside it, then the Chronicle port as its own step (recommended), or both at once?
6. Rail icons: the drawings are the same; the choice is how they're coloured. Grimoire's way, muted at rest and gold only on the current destination (recommended), or every icon gold as today?
7. Backdrop: keep the game's textured backdrop across the whole frame, as D-101 promised (recommended)?
