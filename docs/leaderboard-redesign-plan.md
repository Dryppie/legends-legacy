# Leaderboard Redesign Plan

## Equipment progression update — 3 September 2026

The equipment/crafting transition supersedes the profession/default-board assumptions below. Combat Level is the current equipment-era default; references to cohorts, legacy professions, or Forge navigation are historical. See the [equipment consumer audit](design/equipment-consumer-audit.md) and [current equipment status](design/equipment-implementation-status.md).

## Executive summary

The leaderboard should become the **Hall of Legends**: a central competitive hub that explains what is ranked, shows the current player's position immediately, and connects progression, PvE, PvP, professions, and guild prestige.

The existing page has a useful visual foundation—a dark tavern theme, gold accents, a podium, and a scan-friendly table—but it currently behaves like a database result. It does not explain why a board matters, how ties work, whether the ranking resets, where the current player stands, or how characters participate.

The implementation should prioritize credibility and clarity before adding many new boards. In particular, seeded, administrative, test, banned, or otherwise noncompetitive characters must never appear in public results.

## Current-state findings

### What works

- The tavern imagery and gold treatment fit the world.
- The podium makes the first three places feel more prestigious.
- Numeric values are right-aligned and rows are easy to scan.
- The generic leaderboard component is already reused by the Colosseum.
- The API already includes the requesting player outside the top 50.

### UX and product gaps

- The hero takes substantial space without naming or explaining the page.
- All boards appear in one flat tab grid, mixing overall and profession rankings.
- The active board does not explain its metric, tie-breaker, participation rule, time window, or refresh time.
- The current player's rank is not presented as a first-class result.
- There are no distinct loading, refresh, failure, empty, or unranked states.
- The podium does not display explicit rank numbers or medal labels.
- Long generated names consume excessive space and are difficult to compare.
- The current Wealth board rewards hoarding, exposes private economic state, and is sensitive to inflation and exploitation.

### Correctness and implementation gaps

- Current-player highlighting returns a literal string rather than the current character ID.
- Fishing and Skinning are returned by the backend but are absent from the UI.
- `LeaderboardEntry.Level` represents character level, total level, profession level, Cinders, and Arena rating depending on the caller.
- The endpoint returns every board in one response even though only one is visible.
- Every request loads every character and every profession into memory, then calculates all rankings.
- Wealth casts a `long` Cinder value to `int`.
- Equal scores receive arbitrary sequential ranks and several boards lack a stable tie-breaker.
- The response does not contain total participant count, scoring rules, or update time.
- The main leaderboard repository and UI have no focused test coverage.

## Information architecture

Use two navigation levels: a primary category and a board within that category.

```text
Hall of Legends
├── Overall
│   ├── Total Level
│   ├── Combat Level
│   ├── Soul Archive Completion
│   └── Achievement Renown
├── PvE
│   ├── Dungeon First Clears
│   ├── Most Dungeon Clears
│   └── Dungeon Mastery
├── PvP
│   ├── Arena Rating
│   └── Tournament Points
├── Professions
│   ├── Crafting
│   ├── Mining
│   ├── Woodcutting
│   ├── Fishing
│   └── Skinning
└── Guilds
    ├── Guild Renown
    └── Weekly Guild Contribution
```

Detailed activity-specific boards should remain in their natural feature pages. The Hall of Legends should reuse those ranking sources and provide a unified overview instead of inventing competing rank definitions.

## Recommended leaderboard catalog

| Category    | Board                                            | Window               | Recommendation                                                                      |
| ----------- | ------------------------------------------------ | -------------------- | ----------------------------------------------------------------------------------- |
| Overall     | Total Level                                      | All-time             | Primary permanent progression board.                                                |
| Overall     | Combat Level                                     | All-time             | Rank by level, then experience.                                                     |
| Overall     | Soul Archive Completion                          | All-time             | Implemented by unique absorbed Essence count.                                       |
| Overall     | Achievement Renown                               | All-time             | Implemented by achievement points, then completed achievement count.                 |
| PvE         | Dungeon First Clears                             | Permanent history    | Keep contextual to dungeon and difficulty; summarize centrally.                     |
| PvE         | Most Dungeon Clears                              | All-time             | Implemented by total clears, then distinct dungeons completed.                       |
| PvE         | Dungeon Mastery                                  | All-time             | Implemented by combined mastery levels, then combined mastery experience.            |
| PvP         | Arena Rating                                     | Current standings    | Implemented by current rating, then lifetime-high rating.                           |
| PvP         | Tournament Points                                | Current month        | Implemented using the Tournament Grounds placement scoring and monthly window.       |
| Professions | Crafting, Mining, Woodcutting, Fishing, Skinning | All-time             | Rank by profession level, then experience.                                          |
| Professions | Seasonal Experience Gained                       | Season               | Later catch-up competition for newer players.                                       |
| Guilds      | Guild Renown                                     | All-time             | Implemented by guild level, then guild experience.                                  |
| Guilds      | Weekly Guild Contribution                        | Current week         | Implemented globally by contribution score, then mission contribution.               |

### Boards to avoid

- Current Cinders or raw bank balance.
- Marketplace sales or trade volume before anti-manipulation controls exist.
- Hours played, daily kills, or other bot-friendly activity metrics.
- Lucky-drop or loot-rarity rankings dominated by randomness.
- An opaque power score players cannot reproduce.
- Guild rankings based primarily on member count.
- Separate global boards for every creature, item, route, or currency.

## Screen design

### Compact hero

Reduce the banner height and overlay:

- `Hall of Legends`.
- A one-line purpose statement.
- The active season and end date when applicable.
- Last-updated time and a compact refresh action.

### Navigation

- Primary categories: Overall, PvE, PvP, Professions, Guilds.
- Secondary board selector scoped to the active category.
- Use stable keys such as `total-level` and `profession-mining`, never display labels as identifiers.
- On narrow screens, allow the secondary selector to wrap or become a native select.

### Board context

Every board should display:

- Board name and description.
- Primary metric and optional secondary metric.
- Time window and reset date.
- Last-updated timestamp.

### Current-player standing

Show the player's rank and total participants as a compact label beside participant search.

### Podium

- Preserve the second/first/third visual order.
- Display explicit `#1`, `#2`, and `#3` labels or medals.
- Use a consistently labeled primary metric.
- Optionally show title or guild when it adds identity.
- Truncate long names safely and expose the full name accessibly.
- Condense the presentation on narrow screens.
- Pair color with labels so placement never depends on color alone.

### Ranking table

- Use semantic table markup.
- Keep the header sticky inside long result sets.
- Use a narrow rank column, flexible character column, and right-aligned values.
- Format values with locale-aware separators.
- Highlight the viewer's row with more than color alone.
- Support cursor pagination or incremental loading at scale.
- Provide participant search that opens the page containing the matching rank.

### UI states

Implement distinct states for:

- Initial loading skeleton.
- Refreshing while retaining existing data.
- Network failure with Retry.
- Legitimately empty board.
- Unranked player.

## Ranking and reward rules

- Assign each entry a unique ordinal position (`1, 2, 3, 4`) so podium and table positions are unambiguous.
- Publish and apply secondary ordering metrics such as experience.
- Order remaining ties by character name and then character ID for stable, deterministic positions.
- Permanent progression boards should grant prestige rather than power: titles, badges, historical records, and cosmetics.
- Seasonal boards may grant cosmetics and modest currencies.
- Reward percentile bands as well as the top three.
- Avoid large power rewards that allow winners to compound their advantage.
- Archive season winners so a reset does not erase prestige.

## API and data contract

Use a focused board endpoint:

```http
GET /api/v1/leaderboard/{key}?limit=50
```

The response should contain:

- Board key, category, title, and description.
- Metric labels.
- Period label, start/end time, and reset information when applicable.
- Top entries.
- The requesting player's entry separately.
- Total participant count.
- Server-generated `updatedAt`.
- A pagination cursor when pagination is introduced.

Use neutral `primaryValue` and optional `secondaryValue` fields rather than overloading `Level` for unrelated metrics.

Query only the requested board, project only the required data, and cache global result pages briefly when player count justifies it.

## Delivery phases

### Phase 0: correctness and credibility

- Fix current-character highlighting.
- Include every character profile in global leaderboards.
- Add Fishing and Skinning.
- Define unique ordinal ranking and deterministic tie ordering.
- Add loading, error, refreshing, and empty states.
- Format large values.
- Correct the leaderboard DTO model.
- Add focused tests.

### Phase 1: redesigned page

- Add stable board keys and a data-driven catalog.
- Replace the flat tab grid with category navigation.
- Add board descriptions, personal standing, participant counts, rules, and timestamps.
- Improve podium and table responsiveness.
- Remove Wealth from the primary public lineup.

### Phase 2: focused query path

- Load one selected board per request.
- Return the viewer separately from the top entries.
- Add server-side limits and validation.
- Add opaque cursor pagination and jump-to-participant search. Implemented.
- Add brief caching when required by scale.

### Phase 3: meaningful expansion

- Add seasonal Arena and profession-gain history.
- Add season archives and prestige rewards.

## Success criteria

- A player can identify the active metric, rules, and time window without guessing.
- The current player's standing is visible without scrolling.
- Every character profile can appear in global leaderboards.
- All implemented professions have consistent boards.
- Board changes request only the selected dataset.
- Equal scores receive unique, deterministic positions.
- The layout remains usable at desktop and mobile widths.
- Every loading, failure, empty, unranked, and refresh state is understandable.
- Backend tests cover ranking and participation behavior; frontend verification covers selection and state transitions.
