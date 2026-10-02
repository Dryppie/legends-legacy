# TopBar

The top bar.

**Status:** Draft

The strip across the top of the stage: who you are, how you are doing, what you carry.

**Provide:** `title` (character name or screen title), `eyebrow` ("Lv. 42", a region), an optional `center` slot — one "now" thing: a run's Track while a dungeon run or tower climb is in progress, else the pinned quest's Objective (D-110) — and CurrencyPills as content. Set `showMenu` to show the menu button on narrow screens; it emits `(menu)`, and inside a GameShell it opens the rail drawer.

- Title is `name-header` (Marcellus 24 / 30); eyebrow in `label` style, `gilt`.
- No health bar in the TopBar: HP belongs on combat screens. Keep the centre to one "now" thing: the run's Track, else the pinned quest's Objective, whose full tracker opens in its popover (D-110). Anything more belongs in the page.
- In a GameShell from 60rem its insets are the Page's gutter, and over a backdrop it is `surface` with a hairline under it that continues the docked Chronicle's (D-115).
- In a GameShell under 40rem the centre moves to a second row, full width, and the TopBar grows to 6rem (D-114). Keep the title short enough for the first row; it truncates.
