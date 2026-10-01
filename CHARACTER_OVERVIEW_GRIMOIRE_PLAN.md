# Character Overview → Grimoire: scoping

1 October 2026. A read-only pass: nothing in the app was changed. This is the first real screen on the `lg-*` components (Phase D of `DESIGN_SYSTEM_REPO_MIGRATION_PLAN.md`). The target is the design's own showcase of this screen, **ScreenOverview** (`design-system/components/ScreenOverview/`, ArchetypeInformation in Page Archetypes), built from the current page's content.

**Status (1 October 2026):** steps 1–3 are done, and step 4, Martin's review, is next. Martin took all five recommendations in section 8. Design system: Page `flow` (D-097), `lg-aside` (D-098), ProfileIdentity (D-099), and Grimoire roots in the game's frame (D-100), the gap the build found: the game's global `span` and heading styles overrode Grimoire's type. App: journey stage names, a shared Nobility helper, a shared "last seen" formatter, Grimoire's corner ornament in `src/assets/grimoire/` (the background was already in the app), and the new screen in `character-overview-grimoire/` behind Settings → Interface → Character Overview → New look (preview).

**Agreed so far:** content first, inside the existing app shell (sidebar, header, chat stay as they are; GameShell comes later); the new screen sits behind a switch until Martin signs it off; sizes follow the design (D-096).

## 1. What the page is today

`src/app/features/game/character/character-overview/` (component 499 lines, template 484, SCSS 402, spec 242), route `/game/character/character-overview`.

| Part | Data |
| --- | --- |
| Header: "Character · Overview", search field with suggestions, Search, Refresh | `CharacterService.suggestCharacterNames`, `searchCharacter`; `?characterName=` in the URL |
| Loading, error and empty messages | `CharacterStateService.loading()`, `error()` |
| Journey panel (own profile only): phase, title, summary, "Recommended now", two actions, next unlock | `buildPlayerJourneyGuidance(questState.journal(), level)` |
| Combat Profile: name or equipped title with the Nobility ◆, presence (other players), Guild and tag, Essences attuned, Achievement Points, Nobility expiry with Show perks | `CharacterOverviewDto`; `NobilityService` and `TimeSyncService` (inside `app-noble-decoration`) |
| Character Level with Combat XP bar | `level`, `experience`, `experienceUntilNextLevel` (own profile takes live values from `currentCharacter`) |
| Combat Rating | `power.overall` via `toDisplayedCombatRating`, or "Unavailable" |
| Nobility perks list | constants in the component |
| Combat Style (own profile only) | `CombatStyleStateService` (`app-combat-style-overview`) |
| Combat Attributes: Offense, Defense, Recovery, Utility; equipment ratings under some rows; Threat as estimated threat/s; tooltip on every row | `baseCombatAttributes` / `baseAttributes`, `equipmentRatings`; `formatAttributeType`, `formatAttributeTooltip`, `AttributeValueFormatPipe`; `estimateEssenceThreatPerSecond` |
| Essence Loadout: one card per slot (Attuned / Open), Essence preview on hover | `essenceLoadout.slots`; `app-essence-preview` |

## 2. Component map

| Today | Grimoire | Notes |
| --- | --- | --- |
| `app-default-header` | `lg-page-header` (icon `overview`, eyebrow, title, summary; actions slot) | |
| Search input + CDK overlay + keyboard code | `lg-search-field` (PatternPlayerLookup) + `button[lgButton]` Search | The field owns the suggestion panel and keys; most of the component's search code goes |
| Refresh | Refresh on the own profile; **"Back to my profile"** while viewing someone else | Design change (PatternPlayerLookup) |
| Loading / error / empty text | Words from Standards · States inside the Page ("Loading character overview…", the error with Try again) | No Grimoire region-state component yet (gap G6) |
| Journey panel | `lg-journey-card` with the stage track | Needs stage names (D2) |
| Combat Profile panel | `lg-banner` (art optional) with the identity in its body and `lg-level-plate` + `lg-stat-figure` in its aside | Identity block styles are preview-only (G3) |
| Nobility ◆ before the name | the `nobility` crown icon (D-066) | Data from `NobilityService`, without `app-noble-decoration` (D3) |
| `app-presence-indicator` | `lg-presence` | Reuse its "3 hours ago" text (D4) |
| `app-guild-link` + `[TAG]` badge | link to `/game/city/guild/:id` + `lg-tag` | |
| Show perks / perks list | `button[lgButton="link"]` with `aria-expanded`; list with the en dash, not ◆ (D-065) | Disclosure styles are preview-only (G4) |
| `app-combat-style-overview` | `lg-panel` "Combat Style": `lg-sigil` (Mastery), Mastery XP `lg-meter`, Manage link | Small layout, preview-only (G5); same service |
| Combat Attributes grid | four `lg-ledger`s in `lg-ledgergrid`, each row with `description` (PatternExplainedValue) and equipment rating as `sub`/`tipMeta` | Explanations now open on keyboard focus too; values through the existing pipes |
| Essence Loadout cards | `lg-panel` "Essence Loadout" with `lg-loadout-slot`s, "Open Essences" link | Locked future slots need data (D1); keep the Essence preview hover |

## 3. Gaps in Grimoire (design-system work first)

| # | Gap | Why it blocks | Proposal |
| --- | --- | --- | --- |
| G1 | **Page outside GameShell.** `.lg-page` is absolutely positioned and pads for an overlaying TopBar | In the legacy shell the Page would cover the wrong box and start a TopBar's height down | A Page variant for a host that is not GameShell (no TopBar inset, normal flow). One DS item, ported with a parity case. Goes away when the shell moves |
| G2 | **Overview layout** (main column + 320px side column, stacking under a narrow region) exists only in the showcase's own styles | Every information screen with a side panel needs it | A layout in Foundations · Layout and `bundle.css`, ported |
| G3 | **Profile identity** (eyebrow, name with crown and presence, a Guild / Essences / Achievement Points / Nobility list) is preview-only | Guild and Leaderboard profiles will need the same block | A small component or Banner pattern; DS item |
| G4 | **Nobility perks disclosure** is preview-only | Only this screen | Page-local styles from tokens |
| G5 | **Combat Style block** (Sigil beside a note and a Meter) is preview-only | Only this screen for now | Page-local styles from tokens |
| G6 | **Region states** (Loading blocks, Error with an action, Empty) have rules but no component | Every screen | Page-local now with the States words; a DS item once a second screen needs it |

G1–G3 are one or two backlog-sized items. G4–G6 can stay page-local (tokens only), as Frontend `AGENTS.md` allows.

## 4. Data and app gaps

| # | Gap | Proposal |
| --- | --- | --- |
| D1 | **Locked future Essence slots.** The design shows "Slot 3 · Unlocks at level 20"; the DTO lists only the slots the player has | Show the slots the DTO has (today's behaviour) until the server sends the locked slots with their unlock level |
| D2 | **Journey stage names** for the JourneyCard track | Add a name per `PlayerJourneyStage` in `player-journey.ts` (the showcase uses First Hunt, Claim Your Power, Prepare Your Gear, Enter Shenic, Shenic Journey, Journey Complete) |
| D3 | **Nobility without the legacy ◆.** `app-noble-decoration` both reads the membership and draws the ◆ | Read `NobilityService` + `TimeSyncService` directly, or expose the membership from a small helper both can use |
| D4 | **"Last seen" text** is inside `app-presence-indicator` | Move the formatter to a shared function |
| D5 | **Banner art.** The showcase uses a study background and a gilt corner from `design-system/assets` | Copy the two images into `src/assets`, or ship without art (allowed: art is optional, section 6 assumption 1) |
| D6 | **Essence preview on hover** (`app-essence-preview`) is legacy | Keep it around each LoadoutSlot for now; a Grimoire hover card is later backlog work |

## 5. Layout fit

The showcase fits at 1600px inside GameShell, with a stage about 1,000px wide: main column, 320px side column, attributes two Ledgers across. In the current shell at 1600×900 the content area sits between the sidebar and the docked chat (`w-96`, about 336px), so it should be about as wide, depending on the sidebar. This is checked for real in step 3, with screenshots at 1600×900 and 1920×1080, at Default and Large.

## 6. Switching over

A new component (`character-overview-grimoire`) beside the old one; the route picks one. The old component stays untouched until sign-off, then is deleted with its SCSS.

How the route picks — recommended: a **Settings → Interface** toggle, "Preview the new Character Overview", off by default, stored the way the chat layout preference is. Alternative: a `?ui=grimoire` URL switch for testing only.

## 7. Order of work

1. **Design system:** G1 (Page outside GameShell), G2 (side-column layout), G3 (profile identity). Docs, `bundle.*`, the `lg-*` port and parity cases, as for DS-020.
2. **App groundwork:** D2–D5, each small and tested.
3. **The new screen** behind the switch, with its own spec (the current spec's cases: own vs other profile, search, Threat row, equipment ratings, loading and errors).
4. **Review** at 1600×900 and 1920×1080, Default and Large, keyboard and screen reader (Foundations · Accessibility · Testing). Fix, then make it the default and delete the old page.

## 8. Decisions for Martin

1. Locked future Essence slots: show only what the DTO has for now (recommended), or extend the API first?
2. The switch: Settings toggle (recommended) or URL switch?
3. G3 profile identity in the design system (recommended, Guild and Leaderboard need it) or page-local?
4. Banner art: copy the two images (recommended) or no art?
5. "Back to my profile" in place of Refresh while viewing another player, as the design says (recommended)?
