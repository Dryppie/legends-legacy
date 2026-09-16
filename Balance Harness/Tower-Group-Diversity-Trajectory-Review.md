# Group-diversity trajectory: saved evidence audit

15 September 2026. **Independently verified without combat:** 88 saved evaluations, 92 proposals, 90 parent edges, 28 loadout-library hashes, 70 recorded module uses, 22 group-visit bundles and 4 screen recipes. All 13 reader fixtures passed. No candidate generation, scoring callback, combat preparation, fight, seed allocation or replay.

All 88 evaluated teams had zero discovery wins. Both zero-win screens retained discovery rank 1. The shortlist did not discard a demonstrated winning discovery team. Health-based descriptive differences do not establish an unseen team's confirmation performance. 1 of 4 policy/control-group rows were absent from every generated team; those groups could not be propagated from the saved loadout library.

The preceding [combat comparison](Tower-Group-Diversity-Comparison-Execution-Review.md) remains unchanged: both generated finalists 0/32, mean remaining boss health 88.99% variation and 84.16% diversity, versus controls 27.63% and 32.58%. The 4.84-point health difference is descriptive; adjusted win-rate improvement remains unresolved. This audit locates coverage, ancestry and selection observations, not causal combat mechanics or general search quality.

## Fresh construction and finalist ancestry

| Policy | Evaluated fresh teams | Best fresh boss health | Fresh teams with a group on at least 5 owners | Best health among those teams | Fresh roots in finalist ancestry |
| --- | ---: | ---: | ---: | ---: | ---: |
| baseline | 20 | 88.45% | 13 | 88.45% | 1 |
| group-diversity | 19 | 87.07% | 17 | 87.07% | 1 |

| Policy | Evaluation ordinal | Operator | Requested group for fresh roots | Discovery boss health | Maximum repeated group owners | Final discovery rank |
| --- | ---: | --- | --- | ---: | ---: | ---: |
| baseline | 36 | fresh-coverage | Pack Howler Essence / Venomous Spiderling Essence / Viper Essence / Wind Harpy Essence | 88.45% | 5 | 1 |
| group-diversity | 15 | fresh-coverage | Pack Howler Essence / Spider Queen Essence — Webbed Domain / Venomous Spiderling Essence | 87.07% | 5 | 6 |
| group-diversity | 28 | loadout-placement | mutation | 87.12% | 5 | 7 |
| group-diversity | 32 | mechanic-core | mutation | 85.14% | 5 | 1 |

The ancestry table includes every parent ancestor of the finalist, in evaluation order, and the finalist itself. Complete parent IDs and recipes are in [details](../TestResults/balance/tower-group-diversity-trajectory-20260915/details.json). A multi-parent child does not attribute its outcome to any one parent. All beam/exploration eligibility and 128-module library order/hashes were reconstructed from earlier evaluations only. Rejected/duplicate proposals did not become extra evaluations.

| Policy | Evaluated child/parent edges | Lower / equal / higher child boss health | Edges losing any group count | Edges gaining any group count |
| --- | ---: | ---: | ---: | ---: |
| baseline | 38 | 15 / 0 / 23 | 21 | 16 |
| group-diversity | 47 | 15 / 0 / 32 | 30 | 31 |

These are edges, not independent teams or samples; multi-parent children appear once per parent. Losing a group is not automatically a regression, and gaining one is not automatically beneficial. The complete [nodes](../TestResults/balance/tower-group-diversity-trajectory-20260915/nodes.json), [edges](../TestResults/balance/tower-group-diversity-trajectory-20260915/edges.json) and [library/parent traces](../TestResults/balance/tower-group-diversity-trajectory-20260915/traces.json) retain both improvements and regressions.

## Count and filler requests

| Policy | Group visit | Requested group | Owners / filler draw | Outcome | Discovery boss health | Rank | Direct parent uses | Fresh root of finalist |
| --- | --- | --- | ---: | --- | ---: | ---: | ---: | --- |
| baseline | 0 | Grave Wisp Essence / Spider Queen Essence — Webbed Domain / Web Weaver Spider Essence | 1 / 0 | evaluated | 95.38% | 40 | 1 | no |
| baseline | 0 | Grave Wisp Essence / Spider Queen Essence — Webbed Domain / Web Weaver Spider Essence | 5 / 0 | evaluated | 96.65% | 43 | 0 | no |
| baseline | 0 | Grave Wisp Essence / Spider Queen Essence — Webbed Domain / Web Weaver Spider Essence | 10 / 0 | evaluated | 96.85% | 44 | 0 | no |
| baseline | 0 | Grave Wisp Essence / Spider Queen Essence — Webbed Domain / Web Weaver Spider Essence | 5 / 1 | evaluated | 94.88% | 37 | 0 | no |
| baseline | 1 | Bog Mite Essence / Venomous Spiderling Essence / Web Weaver Spider Essence | 1 / 0 | evaluated | 94.84% | 36 | 1 | no |
| baseline | 1 | Bog Mite Essence / Venomous Spiderling Essence / Web Weaver Spider Essence | 5 / 0 | evaluated | 94.05% | 29 | 2 | no |
| baseline | 1 | Bog Mite Essence / Venomous Spiderling Essence / Web Weaver Spider Essence | 10 / 0 | evaluated | 95.58% | 41 | 0 | no |
| baseline | 1 | Bog Mite Essence / Venomous Spiderling Essence / Web Weaver Spider Essence | 5 / 1 | evaluated | 93.96% | 27 | 2 | no |
| baseline | 2 | Grave Hound Essence / Spider Queen Essence — Webbed Domain / Web Weaver Spider Essence | 1 / 0 | evaluated | 93.36% | 17 | 5 | no |
| baseline | 2 | Grave Hound Essence / Spider Queen Essence — Webbed Domain / Web Weaver Spider Essence | 5 / 0 | evaluated | 93.92% | 26 | 2 | no |
| baseline | 2 | Grave Hound Essence / Spider Queen Essence — Webbed Domain / Web Weaver Spider Essence | 10 / 0 | evaluated | 94.03% | 28 | 0 | no |
| baseline | 2 | Grave Hound Essence / Spider Queen Essence — Webbed Domain / Web Weaver Spider Essence | 5 / 1 | evaluated | 94.33% | 33 | 2 | no |
| baseline | 3 | Pack Howler Essence / Venomous Spiderling Essence / Viper Essence / Wind Harpy Essence | 1 / 0 | evaluated | 94.08% | 31 | 1 | no |
| baseline | 3 | Pack Howler Essence / Venomous Spiderling Essence / Viper Essence / Wind Harpy Essence | 5 / 0 | evaluated | 93.05% | 16 | 2 | no |
| baseline | 3 | Pack Howler Essence / Venomous Spiderling Essence / Viper Essence / Wind Harpy Essence | 10 / 0 | evaluated | 91.84% | 8 | 0 | no |
| baseline | 3 | Pack Howler Essence / Venomous Spiderling Essence / Viper Essence / Wind Harpy Essence | 5 / 1 | evaluated | 88.45% | 1 | 2 | yes |
| baseline | 4 | Bog Mite Essence / Pack Howler Essence / Venomous Snake Essence / Venomous Spiderling Essence / Viper Essence | 1 / 0 | evaluated | 94.94% | 38 | 0 | no |
| baseline | 4 | Bog Mite Essence / Pack Howler Essence / Venomous Snake Essence / Venomous Spiderling Essence / Viper Essence | 5 / 0 | evaluated | 93.90% | 25 | 0 | no |
| group-diversity | 0 | Bog Mite Essence / Pack Howler Essence / Venomous Snake Essence / Venomous Spiderling Essence / Viper Essence | 5 / 0 | evaluated | 93.90% | 30 | 1 | no |
| group-diversity | 1 | Cave Bat Essence / Grave Wisp Essence / Web Weaver Spider Essence | 5 / 0 | evaluated | 95.72% | 44 | 0 | no |
| group-diversity | 2 | Blood Harpy Essence / Cinder Beetle Essence / Smolder Rat Essence | 5 / 0 | evaluated | 93.33% | 25 | 4 | no |
| group-diversity | 3 | Blood Zombie Essence / Bloodfang Wolf Essence / Goblin Essence | 5 / 0 | evaluated | 94.55% | 37 | 0 | no |
| group-diversity | 4 | Giant Bat Essence / Grave Hound Essence / Pack Howler Essence / Venomous Spiderling Essence | 5 / 0 | evaluated | 93.05% | 21 | 3 | no |
| group-diversity | 5 | Flame Harpy Essence / Flame Imp Essence / Smolder Rat Essence | 5 / 0 | evaluated | 94.97% | 39 | 1 | no |
| group-diversity | 6 | Blackjaw Spider Essence / Goblin Warrior Essence / Pack Howler Essence | 5 / 0 | evaluated | 93.16% | 24 | 2 | no |
| group-diversity | 7 | Frost Imp Essence / Pack Howler Essence / Wind Harpy Essence | 5 / 0 | evaluated | 93.79% | 29 | 1 | no |
| group-diversity | 8 | Bog Mite Essence / Poisonous Rat Essence / Rotroot Shambler Essence | 5 / 0 | evaluated | 95.24% | 43 | 1 | no |
| group-diversity | 9 | Pack Howler Essence / Spider Queen Essence — Royal Venom / Venomous Spiderling Essence / Web Weaver Spider Essence | 5 / 0 | evaluated | 94.30% | 33 | 2 | no |
| group-diversity | 10 | Pack Howler Essence / Spider Queen Essence — Webbed Domain / Venomous Spiderling Essence | 5 / 0 | evaluated | 87.07% | 6 | 5 | yes |
| group-diversity | 11 | Bog Mite Essence / Green Slime Essence / Venomous Spiderling Essence | 5 / 0 | evaluated | 95.12% | 42 | 1 | no |
| group-diversity | 12 | Blood Harpy Essence / Dire Wolf Essence / Goblin Essence | 5 / 0 | evaluated | 94.63% | 38 | 0 | no |
| group-diversity | 13 | Venomous Spiderling Essence / Viper Essence / Web Weaver Spider Essence | 5 / 0 | evaluated | 95.04% | 41 | 1 | no |
| group-diversity | 14 | Cave Bat Essence / Grave Hound Essence / Venomous Spiderling Essence | 5 / 0 | evaluated | 92.77% | 18 | 0 | no |
| group-diversity | 15 | Bog Mite Essence / Pack Howler Essence / Venomous Snake Essence | 5 / 0 | evaluated | 93.63% | 28 | 0 | no |
| group-diversity | 16 | Rotroot Shambler Essence / Venomous Snake Essence / Viper Essence | 5 / 0 | evaluated | 94.28% | 32 | 0 | no |

Each request uses the same four discovery seeds. Missing or rejected variants remain explicit and receive no invented fitness. Every eighth fresh request is uniform and belongs to no guided bundle. Parent-use counts cover recorded proposals, including rejected children; root membership uses the complete saved graph. A root evaluated near the end had fewer opportunities to supply parents, so zero uses alone do not demonstrate a selection defect.

| Bundle | Group | First 5-owner filler health | Second 5-owner filler health | Second minus first (points) |
| --- | --- | ---: | ---: | ---: |
| 0 | Grave Wisp Essence / Spider Queen Essence — Webbed Domain / Web Weaver Spider Essence | 96.65% | 94.88% | -1.77 |
| 1 | Bog Mite Essence / Venomous Spiderling Essence / Web Weaver Spider Essence | 94.05% | 93.96% | -0.09 |
| 2 | Grave Hound Essence / Spider Queen Essence — Webbed Domain / Web Weaver Spider Essence | 93.92% | 94.33% | +0.41 |
| 3 | Pack Howler Essence / Venomous Spiderling Essence / Viper Essence / Wind Harpy Essence | 93.05% | 88.45% | -4.60 |

The paired filler rows concern the variation baseline; diversity reached only the first five-owner sweep. These finite four-seed recipe comparisons retain fixed group count and scheduled placement, while changing filler recipes. They show observed recipe sensitivity; they do not estimate general filler quality or justify cherry-picking a policy. All 1/5/10 observations and incomplete bundles are in [findings](../TestResults/balance/tower-group-diversity-trajectory-20260915/findings.json).

## Screening

| Policy | Discovery rank | Discovery boss health | Screen wins | Screen boss health | Missing health records | Selected |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| baseline | 1 | 88.45% | 0/8 | 88.94% | 0 | yes |
| baseline | 2 | 89.48% | 0/8 | 88.31% | 0 | no |
| group-diversity | 1 | 85.14% | 0/8 | 83.50% | 0 | yes |
| group-diversity | 2 | 86.17% | 0/8 | 85.72% | 0 | no |

The frozen selector compares screen wins, then original discovery rank, then ordinal identity. Screen health is descriptive and did not alter that selector. Any unselected recipe with lower observed screen health is retained in the findings, without retrospectively confirming or adopting it. Confirmation remains limited to the original finalists and controls.

## Groups represented by the controls

| Policy | Complete catalogue group | Owners in control 1, 2 | Exact group requests | Maximum generated owners | Library snapshots containing group | Recorded carrier-module uses | Finalist owners |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| baseline | Pack Howler Essence / Spider Queen Essence — Royal Venom / Venomous Spiderling Essence | 7, 7 | 0 | 0 | 0 | 0 | 0 |
| baseline | Pack Howler Essence / Venomous Spiderling Essence / Web Weaver Spider Essence | 1, 0 | 0 | 1 | 4 | 0 | 0 |
| group-diversity | Pack Howler Essence / Spider Queen Essence — Royal Venom / Venomous Spiderling Essence | 7, 7 | 0 | 5 | 14 | 1 | 0 |
| group-diversity | Pack Howler Essence / Venomous Spiderling Essence / Web Weaver Spider Essence | 1, 0 | 0 | 5 | 14 | 1 | 0 |

Only groups that occur in at least one fixed control appear here. This is a post-hoc diagnostic slice of the full 214-group catalogue. Group presence in a strong control does not prove that the group causes its performance; other Essences, placement and interactions differ. The existing controls remained outside search. Carrier uses count saved module uses, not independent fights or guaranteed final retention.

## Where the diversity schedule reached

The archived prefix contains **17 guided requests**. It covered **34 evidence keys, 34 source cores and 28 Essences** as metadata sets. The full catalogue order contains 214 groups. Positions below are one-based and include exact groups and groups containing their entire Essence set. They are schedule locations, not predicted strength.

| Control group | Exact guided position | Earliest containing guided position | Corresponding fresh request including uniform slots | Containing groups actually requested | Maximum generated owners |
| --- | ---: | ---: | ---: | ---: | ---: |
| Pack Howler Essence / Spider Queen Essence — Royal Venom / Venomous Spiderling Essence | 44 | 10 | 11 | 1 | 5 |
| Pack Howler Essence / Venomous Spiderling Essence / Web Weaver Spider Essence | 83 | 10 | 11 | 1 | 5 |

The independent verifier reconstructed the seeded order and all these positions from the catalogue, then matched the saved prefix. See [schedule coverage](../TestResults/balance/tower-group-diversity-trajectory-20260915/positions.json). Complete-combination coverage and metadata coverage are distinct: covering an Essence or evidence key elsewhere does not mean its complete control combination was built. Absence here does not prove that copying the control group would improve combat.

## Implications and next boundary

The more specific finding is **incomplete loadout coverage after reaching the component group**. Diversity's eleventh fresh request constructed Pack Howler / Royal Venom / Venomous Spiderling / Web Weaver on five characters. That team ranked **33/44**, leaving **94.3025%** boss health in discovery. A loadout-compose child ranked **34/44**, leaving **94.3075%**. Modules carrying the control groups appeared in **14** library snapshots and were used once, but neither group remained in the finalist. This is not evidence that the library discarded a demonstrated strong team: these measured teams performed poorly.

Across all **88** generated teams, no character carried the entire **Enchanted Fairy / Pack Howler / Royal Venom / Venomous Spiderling** set. The controls carried that set on **7 and 6 characters**, respectively. This post-hoc reference comes from the independently checked `sharedSetIds`, `sharedSetOwners` and `maximumGeneratedSharedOwners` fields in [analysis](../TestResults/balance/tower-group-diversity-trajectory-20260915/analysis.json) and [nodes](../TestResults/balance/tower-group-diversity-trajectory-20260915/nodes.json). It identifies an untested complete combination; it does not establish that adding Enchanted Fairy alone would close the gap.

The diversity finalist instead descended from the Webbed Domain group: discovery boss health **87.0675%** at its fresh root, **87.1225%** after loadout placement, then **85.1425%** after a mechanic-core mutation. Screening retained that discovery leader. Variation's runner-up had **0.6275 points** less boss health in screening, but both nominees had zero wins and the frozen tie-break retained discovery rank 1. Neither screen discarded a measured winner.

The next engineering target is **compatible Essence completion around a selected group**, guided by authored mechanics and proved first without combat. Inspect the existing coverage-based completion before choosing a change; do not assume its current filler is uniform random. Compare completed combinations and legal slot/family handling while preserving the broad group schedule. The control recipes remain diagnostic references, not generation inputs or hardcoded targets.

The evidence separates three questions: whether the required combinations were constructed, whether available recipes survived mutation/parent selection, and whether screening changed the nominee. The tables and independent reconstruction answer those questions for this trajectory. They cannot tell how untested groups or deeper searches would perform, or assign causal value to an Essence from aggregate fight outcomes.

Before another combat study, use these specific coverage and ancestry gaps to choose one content-driven search change with a zero-combat construction proof. Preserve group diversity while deciding how to spend count/filler requests; do not simply increase repetitions, protect every concentrated group, copy the controls into generation or use ability order as a knob. Any proposed policy change needs its own explicit implementation scope and frozen comparison; no further fights or fresh values are authorized here. Adoption stays Hold.

## Accounting, commands and changed files

Final completion: **7.782 seconds** new diagnostics and **842.082 / 1,800 seconds** cumulative; **4,798,913 bytes** new before sealing, plus **1,158,075,322 bytes** carried forward. All 13 fixtures and independent checks passed. The sealed evidence hash is `da16f20225caf08bbb534e9a63af1f6529a9e584273345fb9e223d8a296741cf`. This active review adds the above interpretation and final totals after publication; the sealed evidence remains unchanged.

New diagnostics before publication: **4.360 seconds**; carried-forward workload **834.300 seconds**. Final [completion](../TestResults/balance/tower-group-diversity-trajectory-20260915/completion.json) includes publication and a conservative one-second receipt/seal allowance within 120 seconds new / 1,800 cumulative. Output is capped at 32 MiB new / 4 GiB cumulative, with the entire preceding retained chain included. All **11,276 indexed files across 27 earlier sealed packages** verified unchanged before/after.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$audit = 'TestResults/balance/tower-group-diversity-trajectory-20260915'
& $python -B "$audit/workflow.py" freeze
& $python -B "$audit/workflow.py" fixtures
& $python -B "$audit/workflow.py" analyze
& $python -B "$audit/workflow.py" verify
& $python -B "$audit/publish.py"
```

Commands ran once; do not rerun these sealed paths. Reproduction requires a separately frozen output and budget using the exact captured inputs. No command failed or was blocked. The unchanged backend scope retains its earlier 87 passing tests through `build/run-tests.ps1`; this audit changed only Python saved-data readers and Markdown, so no backend build/test repetition was needed.

Changed files: the new protocol/review, separate evidence package and six active Markdown handoffs. No harness/gameplay changes, configuration, migrations or deployment. All **482,551 reservations** remain, including v19's unused 512. Fixed ability order; no Kharad tuning, 129,536-fight confirmation or old-cap increase. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption **Hold** are unchanged.
