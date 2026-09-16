# Core portfolio: saved-selection diagnosis

16 September 2026. **VerifiedSavedSelectionDiagnosis**. Zero fights, runtime preparations, new values and retries. All **482,821 reservations** preserved.

## What the saved choices establish

All 320 saved choices and both nomination/screening paths match their frozen rules. Core portfolio used 96/198 profiles; profile hashes decided between otherwise tied profiles at 96/160 placements. No exact control recipe exists in either pool.

| Policy | Pool recipes | Used recipes | Used/pool profiles | Final recipe-hash ties | Discovery wins |
| --- | ---: | ---: | ---: | ---: | ---: |
| Team coverage | 560 | 96 | 55/105 | 104/160 | 0/64 |
| Core portfolio | 768 | 96 | 96/198 | 31/160 | 0/64 |

Core portfolio's profile is the set of authored cores present in one complete loadout. The comparator balances earlier profile exposure, alternates variety/reuse within a team, then orders tied profiles by hash. Only after that does it compare recipe exposure, recipe repetition and newly covered roles. Role coverage becomes the first priority only when remaining slots are no more than missing roles. Hashes identify content; they do not measure damage, healing or interaction strength. **96** decisions still had multiple distinct profiles tied before the profile-hash key. Final recipe-hash ties in the table are a separate, later comparison.

All **212,480 scores** across **320 observed prefixes** were independently reconstructed. Every selected recipe ranked first. Both captured runs used **176/256 states**, stopped at **16 parties**, and used **89,600 / 122,880 checks** below the 250,000 limit. These saved paths had no allocation backtracking or state/check exhaustion. All teams covered the five roles. The counters do not authorize more candidates.

Both policies construct their complete finite batch before using combat measurements to rank it. The new coverage policy does not refine its later recipes using earlier wins or health measurements. Broader structural representation therefore does not imply that the tested batch contains stronger builds. This is an explanation of the implemented search, not a causal proof of the boss-health gap.

## Pool availability and observed ordering

The two recipe pools contain **zero of the ten exact control loadouts**. Changing only their order cannot select an absent recipe. All **191** control subsets were measured, including the subsets that are available but unselected. These examples were fixed by control frequency then ordinal IDs, independently of combat results. Values separated by a slash are baseline / core portfolio; generated counts cover 160 character placements per policy, finalist counts ten, controls twenty.

| Control subset | Control owners | Pool recipes | Generated owners | Finalist owners |
| --- | ---: | ---: | ---: | ---: |
| essence.enchanted_fairy, essence.pack_howler | 18/20 | 51 / 16 | 13 / 12 | 9 / 9 |
| essence.enchanted_fairy, essence.pack_howler, essence.venomous_spiderling | 17/20 | 24 / 8 | 10 / 3 | 9 / 0 |
| essence.enchanted_fairy, essence.pack_howler, essence.spider_queen_royal_venom, essence.venomous_spiderling | 13/20 | 0 / 1 | 0 / 0 | 0 / 0 |

| Subset size | First differing key or selected, over 160 observed prefixes | Best-containing rank range |
| --- | --- | ---: |
| 2 | chosen: 12; newRoles: 1; profileHash: 87; recipeHash: 2; urgentNewRoles: 2; withinTeamProfilePreference: 56 | 1–274 |
| 3 | chosen: 3; newRoles: 1; profileHash: 88; urgentNewRoles: 4; withinTeamProfilePreference: 64 | 1–274 |
| 4 | profileHash: 92; urgentNewRoles: 4; withinTeamProfilePreference: 64 | 2–476 |

- Size 2: team 1, slot 1, best containing recipe `33def4ab3126` ranked 4. First differing key: **profileHash**. Recipe: essence.alpha_wolf, essence.enchanted_fairy, essence.pack_howler, essence.spider_queen, essence.venomous_spiderling.
- Size 3: team 1, slot 1, best containing recipe `33def4ab3126` ranked 4. First differing key: **profileHash**. Recipe: essence.alpha_wolf, essence.enchanted_fairy, essence.pack_howler, essence.spider_queen, essence.venomous_spiderling.
- Size 4: team 1, slot 1, best containing recipe `dd60677afa85` ranked 476. First differing key: **profileHash**. Recipe: essence.enchanted_fairy, essence.pack_howler, essence.spider_queen_royal_venom, essence.venomous_spiderling, essence.wandering_ghost.

The first differing key explains an ordering at the saved prefix. Choosing differently could change every later key. No alternative branch was generated or fought, and subset overlap does not establish necessity, sufficiency or strength.

## Selection and saved combat measurements

Discovery ranking, top-two nominations and screen selection all follow the frozen rules. No selection inconsistency was found. This does not establish the best possible finalist: teams outside the nominated family never received screening/confirmation seeds, and confirmation was not used to reselect.

| Finalist policy | Candidate number | Discovery rank | Schedule | Distinct loadouts | Maximum repetition | Discovery mean boss health |
| --- | ---: | ---: | --- | ---: | ---: | ---: |
| baseline | 8 | 1 | reuse | 2 | 9 | 62.44% |
| team-core-portfolio | 4 | 1 | reuse | 2 | 9 | 83.16% |

| Policy | Schedule | Teams | Wins | Mean discovery boss health | Candidate mean range |
| --- | --- | ---: | ---: | ---: | ---: |
| Team coverage | variety | 8 | 0/32 | 92.82% | 87.62–95.34% |
| Team coverage | reuse | 8 | 0/32 | 76.81% | 62.44–95.02% |
| Core portfolio | variety | 8 | 0/32 | 93.42% | 92.87–94.00% |
| Core portfolio | reuse | 8 | 0/32 | 91.93% | 83.16–94.30% |

The [verified combat comparison](Tower-Core-Portfolio-Comparison-Execution-Review.md) remains baseline **0/32**, core portfolio **0/32**, controls **2/32 each**. Mean confirmation health remaining: **62.32%, 79.56%, 33.99%, 30.28%**. Lower remaining health is better, but descriptive. The primary adjusted bounds remain **−22.22 to +22.22 percentage points**. No historical pooling or new combat evidence is added. Complete recipes, provider/core counts, candidate scores, all subset witnesses and profile hashes are in [analysis.json](../TestResults/balance/tower-core-portfolio-selection-diagnosis-20260916/analysis.json).

## Next bounded recommendation

Keep core portfolio unpromoted. The next implementation should make bounded composition refinement use discovery feedback, rather than add another coverage/hash priority. First review and reuse the existing composition-search operators: retain a fixed exploration share, derive parents only from current-run discovery fitness, vary complete legal loadouts or owner counts, and retain the same total evaluation/proposal caps. Verify determinism, parent provenance, cancellation, copy/role legality, fixed ability order and old-policy parity with fabricated outcomes before requesting any combat. Do not seed from controls, reward control overlap, expand budgets or treat this diagnostic as evidence that refinement will win.

This diagnosis completes the saved-data follow-up. It does not implement a new policy, authorize another fight, establish optimality or justify default adoption. Pool expressiveness remains a separate limitation even if feedback-guided refinement is added.


## Verification, resources and reproduction

The [frozen protocol](Tower-Core-Portfolio-Selection-Diagnosis-Protocol.md) defines all calculations and limits. A separate integer-mask reader checks the set-based analysis. All **67 predecessor packages** verified unchanged before and after; no new live registry scan. The [completion receipt](../TestResults/balance/tower-core-portfolio-selection-diagnosis-20260916/completion.json) records measured time/output and remaining allowance, including one closure second.

| Completed phase before publication | Seconds |
| --- | ---: |
| analyze | 0.484 |
| audit | 0.375 |
| freeze | 4.844 |

Incoming cumulative work: **2,809.958 seconds**, remaining **190.042** under the approved **3,000-second** ceiling. This scope is capped at **60 seconds / 16 MiB**, also subject to cumulative **4 GiB**. No budget reset, retry, deletion or output reuse.

Run once in the recorded order; never rerun a sealed or failed package:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-core-portfolio-selection-diagnosis-20260916'
& $python -B "$work/diagnose.py" freeze
& $python -B "$work/diagnose.py" analyze
& $python -B "$work/diagnose.py" audit
& $python -B "$work/diagnose.py" publish
```

No backend build/test command ran in this archived-data-only scope. The prior eight comparison tests through `build/run-tests.ps1`, 83 implementation tests and native/independent combat verification keep their original scope. No engine, constructor, allocator or evaluator call occurred. Changed files: new Python evidence package, protocol/review and six active Markdown handoffs; root harness/game source and unrelated dirty files preserved. No configuration change, migration or deployment.

V19 retains 253 recipes and the unused original 512 confirmation values. V19 Unresolved, reliability Fail 1/3, deep recovery 0/3, adoption Hold. No Kharad tuning, ability-order optimization or large confirmation.
