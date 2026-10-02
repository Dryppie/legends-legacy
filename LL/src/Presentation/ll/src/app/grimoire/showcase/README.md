# Grimoire showcase

Every Grimoire component on its own, outside the game, in each state it supports, at **`/grimoire`** on the dev
server. It replaced the old catalog (D-127, D-133), and with the snapshot run below it is the safety net for the
Angular rework (`ANGULAR_DESIGN_SYSTEM_PLAN.md`, D-129).

- **Development builds only.** `app.routes.ts` matches the path only in dev mode, and production builds swap
  `showcase.routes.ts` for `showcase.routes.production.ts` (`angular.json` → `fileReplacements`), so none of it ships.
- **Open it:** `npm start`, then `http://localhost:4200/grimoire`. The app's start-up still asks the API for the server
  time, so run the API as usual (the snapshot run answers that call itself).
- **Toolbar:** density, text size (100, 115, 130%), reduced motion, frame (bare, or inside a Page) and ground (plain,
  surface, the game's backdrop). The choices live in the address, so a link keeps them.

## How it is built

| File | What it holds |
| --- | --- |
| `showcase.registry.ts` | Every entry, by tier (primitives, components, game, shell), in navigation order |
| `entries/<slug>.showcase.ts` | One entry: a component whose template is only `<ng-template scStory="…">` blocks, and its `ShowcaseEntry` |
| `showcase-story.directive.ts` | `scStory` (name, `notes`, `width`, `height`, `flush`, `noSnapshot`) and the entries' base class |
| `showcase-shell.component.*` | The frame: navigation, toolbar; it creates every entry once, off-screen, to read its stories |
| `showcase-entry-page.component.ts` | An entry's page, or one story alone, each story in a `data-story-frame` |
| `showcase.css` | The chrome and the stories' layout helpers (`sc-row`, `sc-col`, `sc-grid`, `sc-cell`, `sc-cap`), global, all prefixed `sc-` |
| `showcase.registry.spec.ts` | Every entry has stories with unique slugs, and every `lg-*` component appears in some entry's `covers` |

Each story has its own address, `/grimoire/<tier>/<entry>/<story>`, and the story's name gives its slug
(`Pending label` → `pending-label`). The address names the story's snapshot, so rename a story only on purpose.

## Adding or changing an entry

1. Copy `entries/button.showcase.ts`. Import single `lg-*` components from `@grimoire`.
2. One story per state or variant worth seeing on its own: the states in Standards · States the component supports, its
   variants and densities, long text. Names in sentence case; one short sentence of `notes` when it helps.
3. Keep it deterministic: fixed data, no timers, no `Date.now()`, no random values. A story that moves by itself gets
   `noSnapshot`.
4. Give a part that fills its parent a `width` (and `height` and `flush` for the frame, the shell, a Page or a Stage),
   the size it has in the game. No styles of its own: layout helpers only, sizes in rem.
5. Images come from the app's `assets/`.
6. Register it in `showcase.registry.ts`, list the components it shows in `covers` (`check.mjs` and the registry spec
   fail on a part no entry covers), and point `readme` at the part's page beside its code.

## The snapshot run

```text
npm run grimoire:snapshots          # compare every story with its baseline, and run axe on every entry
npm run grimoire:snapshots:update   # rewrite the baselines after an intended change
```

`e2e/playwright.config.ts` starts the dev server on port 4300 (or reuses one already there), opens every story at
1600×900 and compares its frame with `e2e/grimoire/__snapshots__/<platform>/<tier>-<entry>-<story>.png`.

- **Before the first run on a computer:** `npx playwright install chromium` (Playwright's own browser, once).
- **Baselines are per platform** (`linux`, `win32`), because each system draws text a little differently. A platform's
  first run writes its set and reports those snapshots as written; run it again and it passes. Commit the set.
- **The run doesn't depend on the network or the clock.** `e2e/grimoire/fixtures.ts` answers the API's `timesync` and
  refuses every other API and chat call (the app starts signed out), serves the Google Fonts faces from
  `e2e/grimoire/fonts/` (Fontsource 5.3.0, OFL-1.1), and fixes the time at 2 October 2026, 12:00 UTC.
- **A difference fails the run.** Open `test-results/grimoire-report/index.html` (`npx playwright show-report
  test-results/grimoire-report`): it shows the expected, actual and difference images per story. If the change was
  intended, run the update command and commit the new baselines with the change.
- **Accessibility:** axe checks every entry page (WCAG 2.0 to 2.2 A and AA) and fails on any serious or critical
  violation not listed in `e2e/grimoire/axe-known.json`, the debt that existed when the run began. The full list of
  what it found is in `test-results/grimoire-axe-report.json`. Remove a known entry when it is fixed.
