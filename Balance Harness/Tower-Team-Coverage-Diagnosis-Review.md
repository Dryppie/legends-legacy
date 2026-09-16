# Team coverage: saved construction diagnosis

15 September 2026. **VerifiedSavedConstructionDiagnosis**. Zero fights, runtime preparations, new seeds or retries. Saved reservations: **482,731**.

## Where the saved construction stopped

Of 10 distinct control recipes, 10 are absent from the team pool; 10 have an eligible authored core. The 85 compatible saved focus routes stopped as follows: {'recipe-limit': 85}. Pool omission precedes allocation wherever a valid control extension has a contained anchor but is absent from the pool. An allocator cannot select those missing recipes. No route classified as exhausted omitted a legal target. Recipes without an eligible core, if any, are a separate entry-condition gap.

Across all 48 cores, the saved team pool recorded **768 recipes**, deduplicated to **560**; all **288 focus routes** stopped as **{'recipe-limit': 288}**. Per-core states ranged from **34 to 40 / 256**. The analysis reads these saved charges and does not expand the search. Candidate allocation used 96 of the team pool's 560 distinct recipes; diversity used 160 of its 680. Full recipes, origins and route witnesses are retained in [analysis.json](../TestResults/balance/tower-team-coverage-diagnosis-20260915/analysis.json).

| Control recipe hash prefix | Character origins | Contained cores | Eligible focus routes | Closest diversity recipe | Closest team recipe |
| --- | ---: | ---: | ---: | ---: | ---: |
| b779c6bea10e | 5 | 2 | 10 | 4/5 | 4/5 |
| 0a71d79ecca4 | 2 | 1 | 5 | 4/5 | 3/5 |
| 359ec0ec7601 | 1 | 1 | 5 | 3/5 | 3/5 |
| 19ac3f5b3212 | 1 | 1 | 5 | 4/5 | 3/5 |
| 069a99bca367 | 1 | 1 | 4 | 2/5 | 2/5 |
| ed94d848f462 | 5 | 2 | 10 | 4/5 | 3/5 |
| 123921f30ea1 | 1 | 3 | 12 | 4/5 | 4/5 |
| c594e6ff7467 | 2 | 2 | 12 | 4/5 | 3/5 |
| 50359888db62 | 1 | 2 | 12 | 4/5 | 3/5 |
| 4251255c3bfd | 1 | 2 | 10 | 3/5 | 2/5 |

The constructor's captured source sorts compatible providers by ordinal ID and starts each focus from its anchor again. Each focus retains only one unrestricted or three role-focused recipes. Counts below show non-anchor fillers over the **768 recorded recipe entries**, including duplicates; the total number of filler slots is **2160**. Pool presence uses the separate 560-distinct-recipe denominator. High exposure is a structural concentration measure, not a measured combat effect.

| Most-used non-anchor filler | Ordinal among 80 | Recorded filler uses | Distinct team-pool recipes | Control owners / 20 |
| --- | ---: | ---: | ---: | ---: |
| essence.alpha_wolf | 1 | 720 | 512 | 0 |
| essence.bark_golem | 2 | 410 | 239 | 5 |
| essence.blackjaw_spider | 3 | 273 | 213 | 0 |
| essence.blood_harpy | 4 | 211 | 206 | 0 |
| essence.blood_zombie | 5 | 198 | 154 | 0 |

## Missing combinations and retained teams

All **191 distinct control subsets** of sizes one through five were checked, with no selection from combat results. This table shows the most frequent control subset at each size two, three and four, breaking ties by ordinal IDs. The full output includes every subset, both pools and both generated owner counts. There are **2** distinct legal unions of two authored cores contained in a control recipe; **1** are absent from the team pool. Exact core pairs are retained, without generating or injecting their unions.

| Control subset | Control owners | Diversity pool | Team pool | Diversity generated owners | Team generated owners |
| --- | ---: | ---: | ---: | ---: | ---: |
| enchanted_fairy, pack_howler | 18/20 | 217 | 51 | 40/160 | 13/160 |
| enchanted_fairy, pack_howler, venomous_spiderling | 17/20 | 61 | 24 | 6/160 | 10/160 |
| enchanted_fairy, pack_howler, spider_queen_royal_venom, venomous_spiderling | 13/20 | 1 | 0 | 0/160 | 0/160 |

| Saved finalist | Candidate number | Discovery rank | Distinct loadouts | Maximum repetition |
| --- | ---: | ---: | ---: | ---: |
| baseline | 16 | 1 | 10 | 1 |
| team-coverage | 8 | 1 | 2 | 9 |

All **32 saved candidates**, four nominations and both finalists retained their original recipes, construction traces and selection. No candidate was regenerated or reselected. The completed comparison's 0/32 confirmation wins for each finalist/control and health differences remain descriptive context; missing a control recipe does not prove that this exact recipe is necessary, optimal, or responsible for its control's lower remaining boss health.

## Recommended next implementation boundary

Prepare a generic, deterministic diversification of filler branches within the existing per-core state/recipe caps. Verify pool coverage and old-policy parity with zero combat before considering another comparison. Keep equipped Essence/ability order fixed and exclude saved control recipes or combat outcomes from construction.

This recommendation concerns which Essence combinations are considered during construction. It does not optimize combat execution order. Freeze any new implementation diagnostics separately, using the remaining time/output budget and a closure reserve. This diagnosis implements no policy change and authorizes no fresh values or fights.


## Verification, limits and reproduction

The [frozen protocol](Tower-Team-Coverage-Diagnosis-Protocol.md) defines all reads/calculations. The independent reader verifies containment and exposure using integer bitmasks, separately from the analysis reader's sets, and reconciles recipes, subset counts, core unions, saved ranks/origins, focus bounds and the completed reservation ledger. All **57 predecessor packages** verified unchanged before and after. No new live-registry audit is claimed. Existing backend test evidence is reused; no backend tests, native build, constructor execution or combat replay ran in this Python-only scope.

| Completed diagnostic phase before publication | Seconds |
| --- | ---: |
| analyze | 0.156 |
| audit | 0.203 |
| freeze | 4.375 |

The [completion receipt](../TestResults/balance/tower-team-coverage-diagnosis-20260915/completion.json) records all phases and publication, including its one-second allowance, actual remaining cumulative time and output. Starting totals were 2,291.022 diagnostic seconds under the 2,400-second ceiling; this scope allows at most 60 seconds and 16 MiB under the unchanged cumulative 4 GiB ceiling. First failure stops dependent work. No old caps, files, histories or unused reservations were changed.

Recorded commands, each run once; never rerun a sealed or failed directory:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-team-coverage-diagnosis-20260915'
& $python -B "$work/diagnose.py" freeze
& $python -B "$work/diagnose.py" analyze
& $python -B "$work/diagnose.py" audit
& $python -B "$work/diagnose.py" publish
```

Changed files are the new read-only Python evidence package, protocol/review and six active Markdown handoffs. All game/harness C# and unrelated dirty work are preserved. No configuration, migrations or deployment. V19 keeps all 253 recipes and unused 512 original confirmation values; v19 Unresolved, reliability Fail 1/3, deep recovery 0/3 and adoption Hold. No Kharad tuning or ability-order optimization.
