# Tower progression presets — 9 September 2026

The separate `tower-progression-v1` catalog adds three declared progression budgets against all 15 released floors. Select it in the dashboard's **Profile catalog**. The existing reference catalog, its profiles and saved evidence remain unchanged. The dashboard shows a compact character/equipment/Essence budget for each party and retains the full recipe disclosures.

## Declared budgets

| Preset | Level | Equipment | Essences |
| --- | --- | --- | --- |
| early | 20 | Seven Standard tier-1 rank-0 items | 3 per character |
| mid | 50 | Seven Standard tier-2 rank-3 items | 6 per character |
| late | 90 | Seven Standard tier-2 rank-5 items | 10 per character |

Seven items fill eight slots with a two-handed weapon. Each party cell is Guardian, Restorer, two Strikers and Controller, repeated to fill the floor's actual 5/10/15 participant requirement. Equipment definitions are fixed across stages. Essence lists extend the existing canonical role suggestions: the first three, six or ten entries, explicitly pinned in the new catalog. The production factory validates equipment slots, tier eligibility, Essence slots and distinct source families.

Tier 2 becomes legal at level 50; rank 5 is the current equipment maximum. Level 90 unlocks ten Essence slots. All stages use baseline rolls, no styles, level-1 unascended/unevolved Essences and fresh full prepared health without scouting/contributions. Equipment ownership, reinforcement materials and Essence acquisition are conditional assumptions. Early/mid/late are convenient benchmark names, not approved populations, acquisition timelines or progression targets. This deliberately measures whole-build changes; it cannot attribute a difference to character level, gear or an individual Essence in isolation.

At the current tier-2 reinforcement prices, upgrading seven existing rank-0 items to the mid budget would cost 490 parts and 1,092,700 Cinder per character; the late budget would cost 2,170 parts and 4,839,100 Cinder. Those figures exclude acquiring the items/Essences and are documentation of the ownership assumption, not simulated expenditure. New combat runs resolve current content, while these dated cost examples must be rechecked after price edits.

## Measured results

The fixed schedule uses master seed 1337, 20 trials for each of 45 combinations: **900 battles per run**. Presets on a floor share seeds. Every floor is an independent fight; health, rewards and progression do not carry into the next floor.

| Floors | early wins/trials per floor | mid wins/trials per floor | late wins/trials per floor |
| --- | --- | --- | --- |
| 1–10 | 0/20 | 20/20 | 20/20 |
| 11 | 0/20 | 0/20 | 20/20 |
| 12–15 | 0/20 | 0/20 | 0/20 |

There were no draws, tick-limit results, invalid or missing trials. For each cell, 0/20 corresponds to a descriptive 95% Wilson interval of 0–16.11%; 20/20 corresponds to 83.89–100%. The sample does not prove a true 0% or 100% probability. Rates are not pooled across floors or presets, and the starter's 50–90% band does not apply.

The observed transition is floor 11 for the mid budget and floor 12 for the late budget. Mid's mean victory duration at floor 10 was 117.475 seconds; at floor 11 it lost after 128.89 seconds with 45.204% guardian health remaining. Late cleared floor 11 in 78.54 seconds on average, then lost on floor 12 after 125.89 seconds with 13.696% guardian health remaining. These identify useful follow-up cases, not a conclusion that those bosses need tuning. Ascension, styles, quality and alternative compositions remain unmeasured.

## Verification and evidence

`./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessTower'` passed **54 tests**, zero failures/skips, with five existing compiler/analyzer warnings. New checks materialize all 45 combinations, verify each budget and production slot rules, preserve per-floor seed pairing, repeat/compare the new catalog and replay each preset. Independent persisted normal Tower preparation/playback/outcome parity covers early on floor 1, mid on floor 5 and late on floor 15, in addition to existing all-floor parity. HTTP catalog discovery verifies that all presets are exposed. The full backend suite was not rerun for this fixture/UI/test increment; no command remains blocked.

The browser started both full runs, restored the progression reference schedule and displayed all 45 result rows. Budget previews were visually inspected, with no browser console errors. A browser replay of `floor-11.late/tower.0001` verified Victory at seed -701426619, 56.50 seconds and 4,906 combat events. The repeat compared **900 paired battles with zero changed gameplay/evidence records**. This repeated schedule checks determinism, not independent statistical confirmation. Seven diagnostic detailed replays cover `floor-1.early`, `floor-5.mid`, `floor-10.mid`, `floor-11.mid`, `floor-11.late`, `floor-12.late` and `floor-15.late`, each trial `tower.0001`. Transition replays were selected after viewing the first results and are exploratory inspection, not a predeclared acceptance test.

Saved runs remain visible alongside prior dashboard history:

- Reference: `TestResults/balance/tower-dashboard-20260909/browser-runs/dashboard-20260909-102432-af85e6b63f8c4cdaa075f9cad949a146/run/`.
- Repeat/comparison: `TestResults/balance/tower-dashboard-20260909/browser-runs/dashboard-20260909-102537-3c663c4041e64562988323b7c831f1a2/run/`.
- Matching executable, test log/TRX, replay JSON/logs, verification summary and hashes: `TestResults/balance/tower-progression-20260909/`.

Both bundles include frozen recipes/content/execution identity, individual battle results and Markdown/JSON reports. Restart the dashboard with the retained build:

```powershell
dotnet TestResults/balance/tower-progression-20260909/executable/BalanceHarness.dll tower-dashboard --port 5093 --content-root LL/src/API/API.LL --runs-root TestResults/balance/tower-dashboard-20260909/browser-runs
```

## Changed files and remaining work

`Fixtures/tower-progression.json` supplies 12 role/stage profiles and three parties. `Dashboard/dashboard.js` displays party budget summaries. The three Tower test classes expand materialization, normal-path parity, workflow/replay and catalog-discovery coverage. The README and plan document the new catalog and outcomes. No new combat path, envelope schema, production content, runtime requirement, database migration or deployment change was needed. Accepted starter evidence and all earlier Tower catalogs/runs remain preserved.

The next investigation can refine budgets around the observed floor-11/12 transitions, or introduce a separately declared ascension/style budget. No optimizer, visual recipe editor, acquisition model, victory target or automatic balance adjustment is included. The original CLI default remains the reference catalog; pass `--catalog LL/tools/BalanceHarness/Fixtures/tower-progression.json` for these presets. Phase 2 integration remains deferred.
