# Joint structural search: saved-data diagnosis

15 September 2026. **IndependentlyVerifiedSavedDiagnosis**. Zero new fights, seeds, preparations, generated candidates, scoring callbacks or replays. The 16 structural candidates use 25 of 680 full loadouts, with 9 of ten character slots unchanged across every candidate. Both controls are assessed against the per-character role and unique-recipe restrictions below. This diagnoses search coverage, not the combat effect of removing those restrictions.

## Candidate coverage

| Policy, 16 evaluated teams each | Distinct rosters | Distinct loadout multisets | Distinct character loadouts | Pool loadouts used | Fixed character slots | Changed slots per pair: min / median / max | Loadout replacements per pair: min / median / max |
| --- | ---: | ---: | ---: | ---: | ---: | --- | --- |
| Allocation | 16 | 15 | 38 | 0/680 | 0 | 1 / 10 / 10 | 0 / 10 / 10 |
| Joint structural | 16 | 16 | 25 | 25/680 | 9 | 1 / 1 / 1 | 1 / 1 / 1 |

Allocation: 1-10 distinct loadouts within one team; Joint structural: 10-10 distinct loadouts within one team. Unordered loadout multisets distinguish composition from assigning an unchanged recipe to a different character. All 120 candidate pairs per policy are retained in [findings](../TestResults/balance/tower-joint-structural-diagnosis-20260915/findings.json). These are descriptive distances, not independent trials or strength scores.

| Character slot | Allocation alternatives | Joint structural alternatives |
| --- | ---: | ---: |
| 1 | 10 | 1 |
| 2 | 7 | 1 |
| 3 | 8 | 1 |
| 4 | 8 | 1 |
| 5 | 11 | 1 |
| 6 | 11 | 1 |
| 7 | 8 | 1 |
| 8 | 9 | 1 |
| 9 | 9 | 1 |
| 10 | 9 | 16 |

The structural fixed slots are **1, 2, 3, 4, 5, 6, 7, 8, 9**. Its saved construction traces all respect the 256-state / 250,000-check / 16-party allocation caps and the 48 per-core searches capped at 256 states and 16 recipes. The saved constructor has 768 recorded recipes, 740 distinct recipes and 680 full five-Essence recipes. Pool coverage is bounded-search exposure, not coverage of every legal loadout.

Source inspection explains the measured narrowing: `TowerJointPartyAllocator` returns the first bounded depth-first completions, enforcing diversity inside each party. `TowerJointStructuralSearch` caches those completions and uses one per proposal; neither call imposes a between-candidate coverage target. Distinct team hashes alone do not measure how broadly the 680 recipes were sampled. This source explanation is separate from any unmeasured combat effect.

## Which controls the current construction can represent

| Saved control | Distinct character loadouts | Largest repetition of one loadout | Characters missing an authored role | Exact owners represented in pool | Owners with the full shared set |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1: `team-040e60d3dbc5c127321653c47ed3a9d3` | 7 | 3 | 8/10 | 0/10 | 7/10 |
| 2: `team-49f6979895354870c89362d4abf214bb` | 7 | 3 | 9/10 | 0/10 | 6/10 |

Control 1 violates the all-five-roles-per-character rule and the one-use-per-loadout rule. Control 2 violates the all-five-roles-per-character rule and the one-use-per-loadout rule. These are policy restrictions, not claims that those saved control teams are illegal in combat. Both controls' roles are counted against the same authored metadata as the generated candidates. An authored role label is a construction heuristic; it does not measure effective protection, uptime, targeting or survival.

| Authored role | Control 1 characters carrying it | Control 2 characters carrying it |
| --- | ---: | ---: |
| attack-enabler | 10/10 | 10/10 |
| enemy-pressure | 10/10 | 10/10 |
| protection | 2/10 | 2/10 |
| recovery | 9/10 | 10/10 |
| recurring-control | 9/10 | 9/10 |

Every structural character covers all five authored roles. Complete per-slot missing-role lists and repeated-loadout counts are in [controls](../TestResults/balance/tower-joint-structural-diagnosis-20260915/controls.json). Only **0 distinct control loadouts** occur in the saved 680-recipe pool. Pool absence alone does not prove a loadout could never be constructed; the bounded core searches did not exhaust their search spaces.

The descriptive shared ingredients are **Enchanted Fairy Essence / Pack Howler Essence / Spider Queen Essence — Royal Venom / Venomous Spiderling Essence**: each appears on at least eight characters in each control. They were identified from saved controls, never fed into this search. This is not an established winning combination.

| Saved population | 0 | 1 | 2 | 3 | 4 |
| --- | ---: | ---: | ---: | ---: | ---: |
| Allocation (160 owners) | 115 | 38 | 7 | 0 | 0 |
| Joint structural (160 owners) | 0 | 85 | 73 | 2 | 0 |
| Full recipe pool (680 recipes) | 0 | 309 | 303 | 67 | 1 |

Each column counts how many of those ingredients occur together on one character. Repeated owners count repeatedly. All generated profiles, exact control-loadout matches and traces are retained in [teams](../TestResults/balance/tower-joint-structural-diagnosis-20260915/teams.json).

## Saved fights and frozen selection

| Discovery policy | Wins | Mean boss health remaining | Mean duration (seconds) | Mean saved survival fitness |
| --- | ---: | ---: | ---: | ---: |
| Allocation | 0/64 | 94.85% | 47.90 | 0.00 |
| Joint structural | 0/64 | 85.42% | 47.71 | 0.00 |

| Frozen screen cell | Wins | Mean boss health remaining | Mean duration (seconds) |
| --- | ---: | ---: | ---: |
| `team-f5074d7b6bec6d75aa754e8a6f685e81` | 0/8 | 92.61% | 54.45 |
| `team-79a22313a41d2d7eb0faf0e9cc9b168a` | 0/8 | 93.30% | 48.59 |
| `team-3ccdfb4ad8022238d45fdafe73e45b03` | 0/8 | 83.06% | 52.35 |
| `team-06bcdb0e9c83a2b40c191bcd97f6ca74` | 0/8 | 82.96% | 55.54 |

Selection reconstruction passed: **Allocation: discovery rank 1, 0/8 screen wins; Joint structural: discovery rank 1, 0/8 screen wins**. Discovery ranking follows saved win rate, health, survival, victory duration and identity; screening follows wins, then discovery rank, then identity. The diagnosis does not reselect or score candidates.

| Frozen confirmation | Wins | Mean boss health remaining | Mean duration (seconds) |
| --- | ---: | ---: | ---: |
| Allocation finalist | 0/32 | 92.64% | 54.25 |
| Joint structural finalist | 0/32 | 85.07% | 50.08 |
| Control 1 | 1/32 | 23.60% | 94.11 |
| Control 2 | 1/32 | 30.77% | 92.38 |

All **288 saved records** were independently recounted, with **0 missing health/duration values** retained as missing. The earlier [execution review](Tower-Joint-Structural-Comparison-Execution-Review.md) remains authoritative for the primary comparison: 0.00 percentage-point difference with adjusted paired bounds -22.22 to +22.22. Health/duration are descriptive; neither allocator improvement nor a causal explanation of the control gap is established.

## Next engineering decision

Revise the opt-in candidate allocator to cover different loadout combinations across candidates, with deterministic zero-combat fixtures that detect a fixed prefix. Separately specify whether authored roles must be covered by the team or by each character, and whether repeated legal loadouts are allowed. Keep those choices explicit; do not hardcode control Essences or claim better combat before a separately authorized comparison.

Do not spend more fights on unchanged construction to address this coverage problem. The next implementation can be tested with synthetic loadouts and saved construction inputs first. Broader candidate coverage and permitting legal specialization are separate changes and should have explicit, testable requirements. No implementation or new combat was launched by this diagnosis.

## Verification and resource accounting

Ten pure-reader fixtures passed, followed by an independent recount of all 32 candidates, both controls, 240 pair distances, 680 full recipes, ranks, nominations, screens and 288 saved fight records. The independent reader does not import the analysis functions. Eight comparison backend tests and twelve adapter backend tests, previously executed through `build/run-tests.ps1`, plus seven old-policy parity checks are reused by evidence/source hashes. No backend source changed; no new backend test run or full gameplay build was necessary.

| Diagnostic phase before publication | Seconds |
| --- | ---: |
| analyze | 0.922 |
| fixtures | 0.375 |
| freeze | 4.141 |
| verify | 0.938 |

The incoming total is **2013.319 diagnostic seconds** and **2,584,355,934 output bytes**. This diagnosis is capped at 60 seconds / 16 MiB within the unchanged approved cumulative 2,400 seconds / 4 GiB. The [completion receipt](../TestResults/balance/tower-joint-structural-diagnosis-20260915/completion.json) reports actual phase time, measured publication plus one conservative closure second, remaining time and output accounting. No retries; failure receipts: **none**.

All **51 predecessor packages / 20,072 indexed files** were verified unchanged before/after. Source hashes and the unrelated dirty checkout are checked at publication. All **482,686 seed reservations**, the 253 v19 recipes and 512 unused original confirmation values remain preserved.

Executed once from the repository root:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-joint-structural-diagnosis-20260915'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" fixtures
& $python -B "$work/workflow.py" analyze
& $python -B "$work/workflow.py" verify
& $python -B "$work/publish.py"
```

Stop dependent phases on a failure. Do not rerun the sealed directory; reproduction requires a fresh, separately frozen output/budget using the same pinned inputs. Exact scripts, command receipts, [protocol](Tower-Joint-Structural-Diagnosis-Protocol.md), [input hashes](../TestResults/balance/tower-joint-structural-diagnosis-20260915/freeze.json) and [file seal](../TestResults/balance/tower-joint-structural-diagnosis-20260915/files.json) retain the check.

Changed files: this protocol/review, six active Markdown handoffs and the separate Python evidence package. No production or harness C# changes, gameplay/content edits, migrations, configuration changes or deployment. Fixed ability order, historical reliability **Fail 1/3**, deep recovery **0/3**, v19 **Unresolved** and adoption **Hold** remain unchanged. No Kharad tuning or 129,536-fight confirmation.
