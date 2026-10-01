# Page

The information screen frame.

**Status:** Draft

The scrolling frame for information screens that have no scene — Overview, Settings, Leaderboard, Guild.

**Provide:** `children` (usually a PageHeader, then panels), optional `label` and `maxWidth` (default `page-max`, 80rem: 1,280px at the default text size). Put it inside GameShell as the stage content instead of a Stage; in any other frame, set `flow`.

- It fills the stage area, scrolls on its own and pads its top by `topbar-height` so the floating TopBar never covers content. In a GameShell with a backdrop the stage starts below the TopBar, which sets `topbar-height` to 0 inside it (D-115).
- **Dense in the game's frame** (D-120): inside a GameShell its panels, and the columns of `lg-aside`, `lg-split` and `lg-compare`, sit `stack-lg` apart, and its content fills the stage to the gutter rather than centring at `page-max`, lining up with the TopBar. An explicit `maxWidth` still caps it.
- **`flow`** (D-097) is for a frame that is not GameShell — the game's own frame, while its screens move to Grimoire one at a time. The Page sits in the normal flow, fills its parent's height and scrolls on its own; nothing floats over it, so it keeps no room for a TopBar, and it takes no side padding of its own because the host frame already gives the gutters. When the game's shell becomes GameShell, drop `flow`.
- **Level 0, the frame's backdrop** (D-101): the Page paints no background of its own. It shows the backdrop of the frame it sits in — `ground` in GameShell, the game's own backdrop in the game's frame — and adds no grain (film grain belongs to art only, the Stage's and the Banner's, Foundations · Ornament). Content art appears only inside a Banner.
- **Text set straight on the Page** — the PageHeader, a section's heading and note — keeps its contrast over the lightest part of the backdrop (4.5:1, or 3:1 at large sizes). Where it can't, a veil goes behind that text, never a dimming of the whole backdrop. In the game's frame it measures 6.4:1 at worst (1 October 2026). Panels and the JourneyCard sit on it at Level 1.
- **It is a region** (`lg-region`): its children (Banner, JourneyCard, Ledger grids, a list and inspector) follow the content tiers of its width, not the window's (Foundations · Layout). At 1,920px with a Folio and the docked Chronicle it is 856px wide: Medium.
- Its side padding is `section-md`, or `inset-lg` under 30rem. `.lg-bleed` takes art edge to edge, through the cap and the padding; text, Ledgers and tables never bleed.
- Prose inside it keeps to 68ch (`.lg-prose`).
