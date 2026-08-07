# Authenticated Page Guides

## Goal

Every authenticated gameplay page and major contextual subpage should expose a consistent, accessible guide from its page header. Guides are persistent reference material; first-party tours remain one-time, interactive onboarding.

Public landing, login, and signup pages are outside this scope.

## Architecture

- Assign a stable `guidePageId` to each supported Angular route.
- Keep all IDs in one typed guide catalog.
- Resolve the current guide from route metadata, while allowing an explicit ID override.
- Render the same inline `? Guide` launcher in default, profession, and custom page headers.
- Store guide content as typed JSON under `src/assets/help/guides`.
- Validate every catalog entry and JSON document before frontend builds.
- Keep the drawer responsive, keyboard accessible, and resilient to loading failures.

## Guide inventory

| Priority | Guide              | Coverage                                                   |
| -------- | ------------------ | ---------------------------------------------------------- |
| 1        | Character overview | Attributes, derived stats, equipment, and power            |
| 1        | Inventory          | Categories, filtering, equipping, and tool requirements    |
| 1        | Essences           | Archive, absorption, attunement, loadouts, and ascension   |
| 1        | Combat             | Encounters, timing, outcomes, rewards, and stopping combat |
| 1        | World and regions  | Travel, areas, gathering, raids, and dungeon access        |
| 1        | Dungeons           | Vigor, routes, rooms, decisions, rewards, and failed runs  |
| 1        | Soulstones         | Earning stones, branches, prerequisites, and permanence    |
| 1        | Crafting           | Recipes, mastery, Potential, tempering, and outcomes       |
| 2        | Prophecies         | Daily and weekly objectives, progress, expiry, and rewards |
| 2        | Guild              | Membership, roles, missions, buildings, shop, and rankings |
| 2        | Colosseum          | Teams, tournaments, entry, brackets, and match outcomes    |
| 2        | Cinder Bazaar      | Buying, selling, listings, fees, and restrictions          |
| 3        | Tournament replay  | Replay controls, historical state, and result summaries    |

Achievements, Leaderboard, and Settings are intentionally guide-free because
their interfaces are self-explanatory and do not need separate reference
content.

## Content rules

- Explain the page purpose, main workflow, requirements, outcomes, and common blockers.
- Prefer stable explanations over hardcoded balance values.
- Direct players to live previews where values are calculated dynamically.
- Use the same terms as the UI and domain model.
- Review mechanics against current services and configuration.
- Include a `lastReviewed` date in every guide.
- Do not duplicate click-by-click tutorial instructions.

## Delivery sequence

1. Add the typed catalog, route metadata, shared launcher behavior, and build validation.
2. Wire default and custom page headers.
3. Author all guide assets, beginning with core progression pages.
4. Add route-resolution and launcher tests.
5. Run formatting, guide validation, the Angular build, focused tests, and responsive accessibility checks.

## Acceptance criteria

- Every supported game-sidebar destination has a visible Guide control on desktop and mobile.
- Intentionally guide-free routes are explicitly marked in route metadata.
- Combat, region, dungeon, and tournament replay custom headers expose the same control.
- Every route metadata ID resolves to a valid guide asset.
- No supported guide request returns a missing asset.
- The drawer fits a 320px viewport and closes with Escape or a backdrop click.
- Guide content matches current mechanics.
- Guide validation, the frontend build, and focused UI tests pass.
