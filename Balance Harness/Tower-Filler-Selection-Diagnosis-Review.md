# Filler diversity: saved team-selection diagnosis

16 September 2026. **VerifiedSavedSelectionDiagnosis**. Zero fights, runtime preparations, new seeds and retries. All **482,776 reservations** remain preserved.

## What the saved choices establish

All 320 saved placements match the captured comparator exactly. Filler diversity uses 81/768 pool recipes; none of the ten exact control recipes exists in its pool. The most frequent four-Essence control subset occurs in 1 pool recipe(s) and 0/160 generated placements.

| Policy | Pool recipes | Used recipes | Exact control recipes in pool | Hash tie-break decisions | Discovery wins |
| --- | ---: | ---: | ---: | ---: | ---: |
| Team coverage | 560 | 96 | 0/10 | 104/160 | 0/64 |
| Filler diversity | 768 | 81 | 0/10 | 81/160 | 0/64 |

The captured allocator ranks a complete loadout by newly covered team roles, exposure in earlier completed teams, current-team reuse/variety preference, new Essence IDs, then recipe hash. None of those keys measures combat strength, damage, healing amount or the quality of an interaction. Role labels establish coverage, not effectiveness. The last column above is the existing four-fight-per-team discovery evidence, not a new evaluation.

All **212,480 option scores** over the **320 observed prefixes** were independently checked. Every saved choice ranked first; all teams covered the five required roles. Both traces recorded **176/256 states** and stopped at **16 retained parties**, with **89,600 / 122,880 checks** under 250,000. The observed paths therefore had no allocation backtracking or state/check exhaustion. Unused budget is not permission to expand search. Exact control recipes absent from a pool cannot be selected by any ordering of that pool.

Filler pool role-count histogram: `{'1': 26, '2': 200, '3': 386, '4': 154, '5': 2}`; baseline: `{'0': 1, '1': 32, '2': 181, '3': 261, '4': 85}`. First-slot newly covered roles across all 16 filler teams: `[5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5]`. The [complete analysis](../TestResults/balance/tower-filler-selection-diagnosis-20260916/analysis.json) includes every pool recipe's rank aggregates, roles, cores, all provider counts, all 32 candidate records and every decision's tie groups/top ten options. A hash tie-break decision means more than one recipe tied on all four preceding keys; hashes are deterministic identifiers, not strength scores.

## Pool availability versus allocator choice

Examples are fixed as the most frequent control subset at each size two, three and four, tied by ordinal IDs. All **191 subsets** were checked. Counts before/after the slash below are team coverage / filler diversity. Owner denominators: generated 160 per policy, finalist ten per policy, controls 20 total. No control target entered generation or ranking.

| Control subset | Control owners | Pool recipes | Generated owners | Finalist owners |
| --- | ---: | ---: | ---: | ---: |
| enchanted_fairy, pack_howler | 18/20 | 51 / 16 | 13 / 0 | 9 / 0 |
| enchanted_fairy, pack_howler, venomous_spiderling | 17/20 | 24 / 8 | 10 / 0 | 9 / 0 |
| enchanted_fairy, pack_howler, spider_queen_royal_venom, venomous_spiderling | 13/20 | 0 / 1 | 0 / 0 | 0 / 0 |

For each subset and each observed filler-team prefix, the reader finds the best-ranked containing recipe and compares it with the saved winner. Counts below classify the **first differing key**, not independent causal effects. Ranks and blockers can change after a hypothetical different choice; this diagnosis never visits such a branch.

| Subset size | Outcomes across 160 observed prefixes | Best-containing rank range |
| --- | --- | ---: |
| 2 | newEssences: 73; newRoles: 16; recipeHash: 6; withinTeamUsePreference: 65 | 5–562 |
| 3 | newEssences: 77; newRoles: 16; recipeHash: 2; withinTeamUsePreference: 65 | 23–661 |
| 4 | newEssences: 77; newRoles: 16; recipeHash: 2; withinTeamUsePreference: 65 | 72–759 |

The first archived witness for each fixed example follows. Score entries are **negative newly covered roles, prior-team exposure, signed current-team recipe uses, negative new Essence IDs**; smaller sorts first. The recipe hash breaks remaining ties. Negative use preference applies only to reuse teams.

- Size 2, team 1 slot 1: best containing recipe `20477ab73fcd` ranked **25**; selected `95277f1aa937`. First differing key: **newRoles**. Selected score `[-5, 0, 0, -5]`; containing-recipe score `[-4, 0, 0, -5]`. Containing recipe: blood_zombie, cave_bat, enchanted_fairy, goblin_warrior, pack_howler.
- Size 3, team 1 slot 1: best containing recipe `33def4ab3126` ranked **37**; selected `95277f1aa937`. First differing key: **newRoles**. Selected score `[-5, 0, 0, -5]`; containing-recipe score `[-4, 0, 0, -5]`. Containing recipe: alpha_wolf, enchanted_fairy, pack_howler, spider_queen, venomous_spiderling.
- Size 4, team 1 slot 1: best containing recipe `dd60677afa85` ranked **138**; selected `95277f1aa937`. First differing key: **newRoles**. Selected score `[-5, 0, 0, -5]`; containing-recipe score `[-4, 0, 0, -5]`. Containing recipe: enchanted_fairy, pack_howler, spider_queen_royal_venom, venomous_spiderling, wandering_ghost.

## Was a stronger evaluated candidate discarded later?

Both policies' discovery ranks and their top-two nominations match the saved fitness rule exactly. Screening selections match victories, prior nomination rank and ID, retaining every origin. No selection inconsistency was found, and no team was reselected. Recorded finalists:

| Policy | Candidate number | Discovery rank | Allocation schedule | Distinct loadouts | Maximum repetition | Mean discovery boss health |
| --- | ---: | ---: | --- | ---: | ---: | ---: |
| baseline | 8 | 1 | reuse | 2 | 9 | 62.70% |
| team-filler-diverse | 2 | 1 | reuse | 1 | 10 | 92.53% |

The two predefined allocation schedules have these descriptive discovery results (eight teams and four fights each):

| Policy | Schedule | Teams | Wins | Mean candidate boss health | Candidate mean range |
| --- | --- | ---: | ---: | ---: | ---: |
| Team coverage | variety | 8 | 0/32 | 93.22% | 88.17–95.44% |
| Team coverage | reuse | 8 | 0/32 | 77.09% | 62.70–94.97% |
| Filler diversity | variety | 8 | 0/32 | 93.44% | 92.64–95.00% |
| Filler diversity | reuse | 8 | 0/32 | 94.18% | 92.53–95.54% |

The [completed comparison](Tower-Filler-Diversity-Comparison-Execution-Review.md) remains 0/32 confirmation wins for both finalists and both controls, with mean remaining health 62.18%, 92.84%, 32.02% and 33.74%, respectively. The adjusted primary bounds remain -22.22 to +22.22 percentage points. Role coverage, missing control combinations and these health differences cannot establish that a particular absent recipe would improve combat. The unselected pool recipes were never fought; their strength remains unknown.

## Next bounded recommendation

Keep filler diversity unpromoted. Prepare a bounded allocation change that lets more distinct authored core combinations reach the existing 16-team evaluation budget while retaining team role coverage, legal copies, fixed ability order and exact old-policy behavior. Use all core/role coverage diagnostics and generic rules; do not insert saved controls, reward control overlap or increase the candidate cap. First verify the change with zero combat. Combat benefit of any such change remains unknown.

This is a structural diagnosis and recommendation, not an implemented policy or another combat authorization. It explains why specific available combinations lost under the current comparator; it does not prove that a different traversal will beat the boss. Existing measured performance remains a separate result.


## Verification, resources and reproduction

The [protocol](Tower-Filler-Selection-Diagnosis-Protocol.md) froze every input, calculation and limit. A separate integer-bitmask reader verifies the set-based analysis, including every observed choice, complete ranking, tie-group size, subset count/witness, recipe aggregate, core/provider/role count, nomination/finalist and sealed reservation union. All **62 predecessor packages** verified unchanged before and after. No new live registry audit is claimed.

| Diagnostic phase before publication | Seconds |
| --- | ---: |
| analyze | 0.422 |
| audit | 0.328 |
| freeze | 4.515 |

The [completion receipt](../TestResults/balance/tower-filler-selection-diagnosis-20260916/completion.json) records actual total/remaining time and output, including one closure second. Starting totals: **2,543.598 seconds**, leaving **156.402** under the approved **2,700-second** cumulative limit. Scope maximum: **60 seconds / 16 MiB**, subject to the unchanged cumulative **4 GiB** output cap. Failed phases stop dependent work without retry. Old caps and files remain unchanged.

Recorded commands, each executed once; do not rerun a sealed/failed package:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-filler-selection-diagnosis-20260916'
& $python -B "$work/diagnose.py" freeze
& $python -B "$work/diagnose.py" analyze
& $python -B "$work/diagnose.py" audit
& $python -B "$work/diagnose.py" publish
```

No backend test or build command ran: this scope only reads archived evidence with Python. The prior eight comparison tests through `build/run-tests.ps1`, 71 implementation tests, and completed native/independent combat verification retain their original scope. No constructor, allocator, evaluator or combat engine was called.

Changed files: new Python evidence package, protocol/review and six active Markdown handoffs. All harness/game C# and unrelated dirty files were preserved. No configuration, migration or deployment changes. V19 remains Unresolved, retaining 253 required recipes and unused original 512 confirmation values; reliability Fail 1/3, deep recovery 0/3, adoption Hold. No Kharad tuning or ability-order optimization.
