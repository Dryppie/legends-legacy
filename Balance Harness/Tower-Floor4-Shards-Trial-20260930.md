# Floor 4: Hall of Shards damage-mix trial — 30 September 2026

**Subsequent result — joint candidate closed:** The [Mirror Lance/Hall of Shards trial](Tower-Floor4-Lance-Shards-Trial-20260930.md) closed **`NoEligibleLanceShardsConfirmation`**. It tested magical coefficients **0.60/0.65**, proportional Mirrorbound scaling, and unchanged physical coefficients **2.00/0.50** across all **98 loadouts / five actual compositions**. Best two-character armor variants won **2/128 for both compositions**, below **25/128**. Full armor won **10/128 and 20/128**; every upper bound is below 50% (maximum **29.66%**), but no recipe reaches the 10% lower bound. **12,544 fresh fights / 128 reservations**, one passing native study fixture, zero retries; no confirmation or live application. The full archive and candidate changes were independently verified. **30 candidate Python checks, four Vaelor tests and 108 backend passes/four opt-in skips were authenticated and reused**, not rerun. Exclusions **917,081**. Live game data, approved progression budgets and supported search remain unchanged. The joint proposal described below has now been tested and rejected. Its original JSON remains an immutable pre-allocation record; use the latest linked trial and handoff for the unexecuted diagnostic follow-up.

## Frozen prospective protocol

The [pressure diagnostic](Tower-Floor4-Pressure-Diagnostic-20260930.md) reproduced 64 saved fights and identified substantial party-wide damage at 16 seconds after Mirror Lance at 14 seconds. In the two-character armor recipes, Hall of Shards dealt mean physical health damage of **1,486 / 1,297** before 20 seconds, versus **1,342 / 1,438** with full armor. Its magical part dealt **1,866 / 1,887**, versus **2,147 / 2,215** with full armor. These descriptive totals include different surviving targets and mitigation; they do not predict a successful adjustment.

Test **one isolated candidate**: Hall of Shards physical coefficient **0.50 → 0.25**, magical coefficient **0.50 → 0.65**. This reduces total base Power scaling from 1.00 to 0.90 while shifting its damage mix toward resistance. The hypothesis is to reduce the advantage of equipping armor across the entire party while retaining pressure on the fully armored controls. This is a test magnitude, not an accepted setting.

Preserve Mirrorbound's original **1% of the corresponding base coefficient per stack**: physical status scaling **0.005 → 0.0025**, magical **0.005 → 0.0065**. Keep status IDs, stack limits, consumption, ability order, targets, cooldowns, Mirror Lance, Reflective Mirrorplate, all guardian scalars and every other field unchanged. Update only Hall's description to match the candidate damage. The v3 candidate contract requires proportional status scaling and consistent descriptions for multiple effects in the same ability; v1/v2 remain unchanged.

- Source: full 98-loadout current-runtime screen, manifest `66cc379b5f226c6925e7b1a240c0f65c8bef6d630bcafde7978ca036140d406d`. Retain all 38 original controls and 60 mixed-armor variants. No source edits, search, imports, party reordering or budget changes.
- Initial exclusions **916,825**, plus any later reservations. Authenticate the source, diagnostic, current runtime/catalogs, candidate safeguards and fresh Vaelor catalog/mechanic tests before allocating seeds.
- Screen all 98 loadouts on **128 fresh shared seeds / 12,544 fights**. Require at least two actual compositions with armor on at most two of five characters (eight specialized items), each lower win-rate bound at least 10%; every loadout upper bound at most 50%. Approximate simultaneous 95% Bonferroni-Wilson across 98 loadouts; gates **25–44 wins / 128**.
- Only a passing complete screen permits a separate **184-seed / 18,032-fight confirmation**, same unchanged candidate/family, gates **33–68 wins / 184**. No pooling with screening or historical fights. A passing confirmation still requires native parity against the candidate before any local application, followed by gameplay regressions.
- Maximum **30,576 fresh fights / 312 reservations**; one candidate, no extensions, retries, dropped controls or replacement seeds. If screening fails, close this scope and report the result without applying the candidate.
- Per native phase: **20,000 fights / 840 seconds / 2 GiB**, owner **900 seconds**. Use doubled measured preceding cost and require projections below 80% of time/storage limits before each phase.

Keep Vaelor health/offense **2.3231953125 / 5.023125**, five level-30 characters/four Essences/T1 Epic Fine Rank 3, the user's repeating gear curve, supported search and prior floor-2/5/6 changes. No dungeon, supply, migration, configuration or deployment work.

Fresh paths: `TestResults/tower-floor4-shards-driver-20260930` and native `tower-balance-pass-floor4-shards-{screen,confirm}-*-20260929`. Preserve failed/completed outputs; never resume them.

## Verified result

**Rejected: `NoEligibleShardsConfirmation`.** All **98 loadouts / five actual compositions** completed the 128-seed screen. Both acceptance requirements failed: neither actual partial-armor composition reached the lower-bound target, and one full-armor control exceeded the ceiling. No confirmation or live application ran.

| Composition | Armored characters | Slots | Wins | Adjusted interval |
| --- | ---: | --- | ---: | ---: |
| A | 2 | 2, 5 | 7/128 | 1.61%–17.01% |
| A | 4 | 1, 2, 3, 5 | 23/128 | 9.12%–32.34% |
| A | 5 | all | 33/128 | 14.86%–40.88% |
| B | 2 | 3, 4 | 10/128 | 2.77%–20.13% |
| B | 4 | 1, 2, 3, 4 | 22/128 | 8.58%–31.45% |
| B | 5 | all | 45/128 | 22.36%–50.51% |

The best two-character variants reached **7/128 and 10/128**, versus the required **25/128**. The strongest full-armor recipe reached **45/128**, above the allowed **44/128**, with adjusted upper **50.51%**. This is not a near-pass: repairing the one-win ceiling miss alone would not establish the partial-armor minimum. The preceding unchanged-guardian screen used different seeds and yielded **5/128 and 4/128** for its best two-character variants; that comparison is descriptive, not a paired causal estimate of improvement.

Fresh work: **12,544 fights / 128 reservations**, **129.54 native seconds**, zero retries. Final exclusions **916,953**. Every catalog in the live game remains unchanged. Live Hall physical/magical remains **0.50/0.50**, with status scaling **0.005/0.005**. The isolated **0.25/0.65** candidate remains archived and rejected. Vaelor health/offense stays **2.3231953125 / 5.023125**.

## Implementation and verification

`analysis/tower-ability-candidate.py` adds an explicit **v3** contract for multiple Power-damage effects of one guardian ability with proportional status scaling. It validates the original coefficients, exact status ratios, unique effects, consistent descriptions, exclusive guardian ownership and immutable unrelated fields. The verifier restores all declared effects before comparing the entire catalog, retaining v1/v2 behavior. This avoids accidentally making Mirrorbound stronger per stack when lowering base damage.

**30 candidate tests passed**, including six new cases for coupled scaling, duplicate effects, conflicting descriptions, legacy contracts and hidden mechanic changes. The diagnostic's nineteen checks make **49 fresh Python passes** this turn. **Four fresh Vaelor catalog/mechanic cases passed through `build/run-tests.ps1 -NoBuild`**, and the bounded native screening fixture passed. The existing **108 backend passes / four opt-in skips** were authenticated and reused; no C# rebuild was required.

The independent collector reauthenticated and recounted every archived fight, reconstructed all 98 adjusted bounds and actual-composition decisions, verified exactly the declared ability edits and unchanged budgets/runtime/settings, checked seed disjointness and the 20,000-fight/840-second/2-GiB/900-owner-second limits, and confirmed clean process exits. The screen did not permit confirmation. No required check remains blocked; confirmation/application were intentionally not run. No migration, configuration or deployment changes.

## Next work

Test one new, separately frozen joint candidate from the unchanged live catalog: Mirror Lance magical **1.00 → 0.60** and Hall of Shards magical **0.50 → 0.65**, with proportional status coefficients **0.010 → 0.006** and **0.005 → 0.0065**. Keep Hall physical at live **0.50**, Mirror Lance physical **2.00**, timings, targets, reflection, stacks and guardian scalars. This probes the 14-second execution hit while retaining physical pressure and testing later magical compensation; it is an untested hypothesis. Retain all 98 loadouts and the two-composition/eight-item/50% ceiling gates. Proposal `TestResults/tower-floor4-lance-shards-proposal-20260930.json` is **ProposedNotAllocated**; no extra seeds were reserved. Maximum future scope: 128-seed screening, conditional independent 184-seed confirmation, 30,576 fights/312 reservations, with fresh resource admission. Do not extend either closed screen or pursue fractional Hall-only sweeps. No active study remains; no dungeon or supply work.

The new hypothesis reduces the 14-second lowest-health execution hit while restoring Hall's physical component to the unchanged live value. Its increased magical Hall component is intended as compensating pressure at 16 seconds; the diagnostic shows greater magical Hall totals against the surviving full-armor groups, but does not predict the candidate's outcome. The proposed values are not accepted gameplay settings. Proposal SHA **`9aff59199b03fff8f1ea081bd9f2b394e68eaf71c17675931b56200673a61680`**. Preserve the independent confirmation and native application checks if a later screen passes.

## Evidence and commands

- Independent trial evidence: `TestResults/tower-floor4-shards-evidence-20260930.json`, SHA **`38940b638154f6872c16086a4d08b4b61f5228ce298f3f8940deee39446a9aad`**.
- Screen manifest: **`ca8952e058ce96aba5271c8631e3fd875fc80d2c9ec72d056c94b5c3f61c6771`**, archive `TestResults/tower-balance-pass-floor4-shards-screen-study-20260929`.
- Latest ledger: `TestResults/tower-balance-pass-floor4-shards-screen-owner-20260929/seed-ledger.json`, SHA **`4dae095fe662e6a07e6ea1f3555ae70f9fb8d352aeee3ecf023b333a3c3b9d64`**, plus ancestors and any later reservations.
- Frozen candidate: `TestResults/tower-floor4-shards-driver-20260930/candidate.json`, SHA **`16da4928cfc0e081c80aebd0a781139feb1e4748bc90813ffaa31a421ffa4dc4`**. Driver declaration, protocol, commands, resource admission, assessment and receipts remain in the same directory.
- Latest publication: `TestResults/tower-floor4-pressure-shards-publication-check-20260930.json`.

Executed with configured Python and `-B -X utf8`: the mixed-armor, gear-diagnostic and ability-candidate test scripts; the floor-4 diagnostic command; its independent collector; `TestResults/tower-floor4-shards-prepare-20260930.py`, the trial driver, independent collector and this publisher. Backend checks and the native study used `build/run-tests.ps1 -NoBuild`; detailed replays used the pinned BalanceHarness CLI. `git diff --check` and updated Markdown links pass. Do not rerun or resume completed study paths.
