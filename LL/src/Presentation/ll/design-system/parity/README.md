# Parity check

Proves that the `lg-*` Angular components render and behave like the React reference components (D-093). Governance · Code parity explains what is compared and lists each component's API mapping.

## What is here

| File | What it holds |
| --- | --- |
| `angular.json`, `tsconfig.json`, `main.ts`, `index.html` | A small Angular workspace of its own. It builds into `LL/src/Presentation/ll/dist/grimoire-parity/` and uses the app's `node_modules`. |
| `parity.component.ts` | The Angular side: every case, in `#ng`. It also compares the format and state helpers with `LL.format`, `LL.states` and `LL.topState`. |
| `react-cases.js` | The React side: the same cases (`CASES`) and the interactive ones (`ICASES`), in `#react`. |
| `data.json` | The props both sides share. |
| `normalize.js` | Turns a rendered case into comparable text (attribute and class order, Angular hosts and comments, generated ids). |
| `check-parity.mjs` | Opens the page in headless Chromium, reads the static result and plays the behaviour scenarios on each side. |

Every case in `parity.component.ts` has a twin with the same `data-case` name in `react-cases.js`, and every interactive case (`i-…`) has a scenario in `check-parity.mjs`.

## Running it

From `LL/src/Presentation/ll/design-system/parity` (npm only; the app's `npm ci` must have run):

```text
npx ng build
node check-parity.mjs
```

`npx ng build` also compiles every `lg-*` component with strict templates: until a screen imports them, it is the only build that does.

`check-parity.mjs` needs the `playwright` package, which the app does not install. Either install it beside the app without saving it (`npm install --no-save playwright`, then `npx playwright install chromium`), or point to a copy installed elsewhere with `PLAYWRIGHT_MODULE=<path to its playwright folder>`. `--static` skips the behaviour scenarios, which take about four minutes because the announcer is rate-limited and each scenario waits for it to settle.

To look at the page, run `npx ng serve` and open `http://localhost:4610/`: the result is at the top, followed by both editions of every case. `?only=react` or `?only=angular` renders one side.

A pass prints:

```text
Static: 415 of 415 cases match.
Behaviour: 22 of 22 scenarios match.
Announcements: the same 9 lines on both sides.
```

Anything else exits 1 and prints the differing case, step and field.

## Adding a case

When a component gains a prop, a variant or a behaviour, add a case for it on both sides with the same `data-case` name, and for a behaviour a scenario in `SCENARIOS`. Keep the props in `data.json` when both sides use them.
