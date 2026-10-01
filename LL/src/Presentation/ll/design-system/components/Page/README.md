# Page

The information screen frame.

**Status:** Draft

The scrolling frame for information screens that have no scene — Overview, Settings, Leaderboard, Guild.

**Provide:** `children` (usually a PageHeader, then panels), optional `label` and `maxWidth` (default `page-max`, 80rem: 1,280px at the default text size). Put it inside GameShell as the stage content instead of a Stage.

- It fills the stage area, scrolls on its own and pads its top by `topbar-height` so the floating TopBar never covers content.
- **Level 0:** flat `ground`, with no grain — film grain belongs to art only, the Stage's and the Banner's (Foundations · Ornament). Art appears only inside a Banner. Panels and the JourneyCard sit on it at Level 1.
- **It is a region** (`lg-region`): its children (Banner, JourneyCard, Ledger grids, a list and inspector) follow the content tiers of its width, not the window's (Foundations · Layout). At 1,920px with a Folio and the docked Chronicle it is 888px wide: Medium.
- Its side padding is `section-md`, or `inset-lg` under 30rem. `.lg-bleed` takes art edge to edge, through the cap and the padding; text, Ledgers and tables never bleed.
- Prose inside it keeps to 68ch (`.lg-prose`).
