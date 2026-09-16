# Group-variation trajectory: saved evidence audit

15 September 2026. **Independently verified without combat:** 88 saved evaluations, 92 proposals, 86 parent edges, 28 loadout-library hashes, 59 recorded module uses, 5 variation bundles and 4 screen recipes. All 11 reader fixtures passed. No candidate generation, scoring callback, combat preparation, fight, seed allocation or replay.

All 88 evaluated teams had zero discovery wins. Both zero-win screens retained discovery rank 1. The shortlist did not discard a demonstrated winning discovery team. Health-based descriptive differences do not establish an unseen team's confirmation performance. 3 of 4 policy/control-group rows were absent from every generated team; those groups could not be propagated from the saved loadout library.

The preceding [combat comparison](Tower-Group-Variation-Comparison-Execution-Review.md) remains unchanged: both generated finalists 0/32, mean remaining boss health 90.30% group/count and 82.18% variation, versus controls 28.25% and 34.48%. The 8.11-point health difference is descriptive; adjusted win-rate improvement remains unresolved. This audit locates coverage, ancestry and selection observations, not causal combat mechanics or general search quality.

## Fresh construction and finalist ancestry

| Policy | Evaluated fresh teams | Best fresh boss health | Fresh teams with a group on at least 5 owners | Best health among those teams | Fresh roots in finalist ancestry |
| --- | ---: | ---: | ---: | ---: | ---: |
| baseline | 19 | 90.55% | 9 | 90.55% | 2 |
| group-variation | 19 | 81.68% | 12 | 81.68% | 1 |

| Policy | Evaluation ordinal | Operator | Requested group for fresh roots | Discovery boss health | Maximum repeated group owners | Final discovery rank |
| --- | ---: | --- | --- | ---: | ---: | ---: |
| baseline | 4 | fresh-coverage | Frost Imp Essence / Pack Howler Essence / Venomous Snake Essence | 91.17% | 4 | 10 |
| baseline | 9 | fresh-coverage | Blood Harpy Essence / Cinder Beetle Essence / Smolder Rat Essence | 91.80% | 8 | 17 |
| baseline | 16 | placement | mutation | 91.04% | 3 | 8 |
| baseline | 28 | recombine | mutation | 90.50% | 3 | 4 |
| baseline | 36 | coverage-count | mutation | 89.99% | 3 | 2 |
| baseline | 44 | double | mutation | 89.42% | 3 | 1 |
| group-variation | 26 | fresh-coverage | Bog Mite Essence / Giant Bat Essence / Pack Howler Essence / Venomous Spiderling Essence | 84.95% | 5 | 10 |
| group-variation | 28 | loadout-placement | mutation | 84.35% | 5 | 6 |
| group-variation | 40 | loadout-refine | mutation | 81.20% | 5 | 1 |

The ancestry table includes every parent ancestor of the finalist, in evaluation order, and the finalist itself. Complete parent IDs and recipes are in [details](../TestResults/balance/tower-group-variation-trajectory-20260915/details.json). A multi-parent child does not attribute its outcome to any one parent. All beam/exploration eligibility and 128-module library order/hashes were reconstructed from earlier evaluations only. Rejected/duplicate proposals did not become extra evaluations.

| Policy | Evaluated child/parent edges | Lower / equal / higher child boss health | Edges losing any group count | Edges gaining any group count |
| --- | ---: | ---: | ---: | ---: |
| baseline | 46 | 23 / 0 / 23 | 32 | 26 |
| group-variation | 35 | 14 / 0 / 21 | 24 | 14 |

These are edges, not independent teams or samples; multi-parent children appear once per parent. Losing a group is not automatically a regression, and gaining one is not automatically beneficial. The complete [nodes](../TestResults/balance/tower-group-variation-trajectory-20260915/nodes.json), [edges](../TestResults/balance/tower-group-variation-trajectory-20260915/edges.json) and [library/parent traces](../TestResults/balance/tower-group-variation-trajectory-20260915/traces.json) retain both improvements and regressions.

## Count and filler requests

| Bundle | Requested group | Owners / filler draw | Outcome | Discovery boss health | Rank | Direct parent uses | Fresh root of finalist |
| --- | --- | ---: | --- | ---: | ---: | ---: | --- |
| 0 | Bog Mite Essence / Cave Bat Essence / Pack Howler Essence / Venomous Snake Essence / Venomous Spiderling Essence | 1 / 0 | evaluated | 93.80% | 38 | 1 | no |
| 0 | Bog Mite Essence / Cave Bat Essence / Pack Howler Essence / Venomous Snake Essence / Venomous Spiderling Essence | 5 / 0 | evaluated | 93.45% | 37 | 1 | no |
| 0 | Bog Mite Essence / Cave Bat Essence / Pack Howler Essence / Venomous Snake Essence / Venomous Spiderling Essence | 10 / 0 | evaluated | 93.09% | 34 | 1 | no |
| 0 | Bog Mite Essence / Cave Bat Essence / Pack Howler Essence / Venomous Snake Essence / Venomous Spiderling Essence | 5 / 1 | evaluated | 94.52% | 41 | 2 | no |
| 1 | Blood Harpy Essence / Blood Zombie Essence / Dire Wolf Essence | 1 / 0 | evaluated | 91.98% | 26 | 0 | no |
| 1 | Blood Harpy Essence / Blood Zombie Essence / Dire Wolf Essence | 5 / 0 | evaluated | 91.12% | 20 | 4 | no |
| 1 | Blood Harpy Essence / Blood Zombie Essence / Dire Wolf Essence | 10 / 0 | evaluated | 91.44% | 23 | 0 | no |
| 1 | Blood Harpy Essence / Blood Zombie Essence / Dire Wolf Essence | 5 / 1 | evaluated | 94.02% | 40 | 0 | no |
| 2 | Blackjaw Spider Essence / Bog Mite Essence / Pack Howler Essence / Venomous Snake Essence | 1 / 0 | evaluated | 91.92% | 25 | 0 | no |
| 2 | Blackjaw Spider Essence / Bog Mite Essence / Pack Howler Essence / Venomous Snake Essence | 5 / 0 | evaluated | 91.35% | 21 | 3 | no |
| 2 | Blackjaw Spider Essence / Bog Mite Essence / Pack Howler Essence / Venomous Snake Essence | 10 / 0 | evaluated | 93.08% | 33 | 0 | no |
| 2 | Blackjaw Spider Essence / Bog Mite Essence / Pack Howler Essence / Venomous Snake Essence | 5 / 1 | evaluated | 94.72% | 42 | 2 | no |
| 3 | Bog Mite Essence / Giant Bat Essence / Pack Howler Essence / Venomous Spiderling Essence | 1 / 0 | evaluated | 90.10% | 12 | 2 | no |
| 3 | Bog Mite Essence / Giant Bat Essence / Pack Howler Essence / Venomous Spiderling Essence | 5 / 0 | evaluated | 84.95% | 10 | 1 | yes |
| 3 | Bog Mite Essence / Giant Bat Essence / Pack Howler Essence / Venomous Spiderling Essence | 10 / 0 | evaluated | 81.68% | 3 | 1 | no |
| 3 | Bog Mite Essence / Giant Bat Essence / Pack Howler Essence / Venomous Spiderling Essence | 5 / 1 | evaluated | 92.89% | 31 | 0 | no |
| 4 | Grave Wisp Essence / Pack Howler Essence / Venomous Snake Essence / Venomous Spiderling Essence / Viper Essence | 1 / 0 | evaluated | 93.41% | 36 | 0 | no |

Each request uses the same four discovery seeds. Missing or rejected variants remain explicit and receive no invented fitness. Every eighth fresh request is uniform and belongs to no guided bundle. Parent-use counts cover recorded proposals, including rejected children; root membership uses the complete saved graph. A root evaluated near the end had fewer opportunities to supply parents, so zero uses alone do not demonstrate a selection defect.

| Bundle | Group | First 5-owner filler health | Second 5-owner filler health | Second minus first (points) |
| --- | --- | ---: | ---: | ---: |
| 0 | Bog Mite Essence / Cave Bat Essence / Pack Howler Essence / Venomous Snake Essence / Venomous Spiderling Essence | 93.45% | 94.52% | +1.07 |
| 1 | Blood Harpy Essence / Blood Zombie Essence / Dire Wolf Essence | 91.12% | 94.02% | +2.90 |
| 2 | Blackjaw Spider Essence / Bog Mite Essence / Pack Howler Essence / Venomous Snake Essence | 91.35% | 94.72% | +3.38 |
| 3 | Bog Mite Essence / Giant Bat Essence / Pack Howler Essence / Venomous Spiderling Essence | 84.95% | 92.89% | +7.93 |

These finite four-seed recipe comparisons retain fixed group count and scheduled placement, while changing filler recipes. They show observed recipe sensitivity; they do not estimate general filler quality or justify cherry-picking a policy. All 1/5/10 observations and incomplete bundles are in [findings](../TestResults/balance/tower-group-variation-trajectory-20260915/findings.json).

## Screening

| Policy | Discovery rank | Discovery boss health | Screen wins | Screen boss health | Missing health records | Selected |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| baseline | 1 | 89.42% | 0/8 | 90.31% | 0 | yes |
| baseline | 2 | 89.99% | 0/8 | 90.40% | 0 | no |
| group-variation | 1 | 81.20% | 0/8 | 80.40% | 0 | yes |
| group-variation | 2 | 81.28% | 0/8 | 80.39% | 0 | no |

The frozen selector compares screen wins, then original discovery rank, then ordinal identity. Screen health is descriptive and did not alter that selector. Any unselected recipe with lower observed screen health is retained in the findings, without retrospectively confirming or adopting it. Confirmation remains limited to the original finalists and controls.

## Groups represented by the controls

| Policy | Complete catalogue group | Owners in control 1, 2 | Guided requests | Maximum generated owners | Library snapshots containing group | Recorded carrier-module uses | Finalist owners |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| baseline | Pack Howler Essence / Spider Queen Essence — Royal Venom / Venomous Spiderling Essence | 7, 7 | 0 | 0 | 0 | 0 | 0 |
| baseline | Pack Howler Essence / Venomous Spiderling Essence / Web Weaver Spider Essence | 1, 0 | 0 | 6 | 14 | 1 | 0 |
| group-variation | Pack Howler Essence / Spider Queen Essence — Royal Venom / Venomous Spiderling Essence | 7, 7 | 0 | 0 | 0 | 0 | 0 |
| group-variation | Pack Howler Essence / Venomous Spiderling Essence / Web Weaver Spider Essence | 1, 0 | 0 | 0 | 0 | 0 | 0 |

Only groups that occur in at least one fixed control appear here. This is a post-hoc diagnostic slice of the full 214-group catalogue. Group presence in a strong control does not prove that the group causes its performance; other Essences, placement and interactions differ. The existing controls remained outside search. Carrier uses count saved module uses, not independent fights or guaranteed final retention.

## Implications and next boundary

The evidence separates three questions: whether the required combinations were constructed, whether available recipes survived mutation/parent selection, and whether screening changed the nominee. The tables and independent reconstruction answer those questions for this trajectory. They cannot tell how untested groups or deeper searches would perform, or assign causal value to an Essence from aggregate fight outcomes.

Before another combat study, use these specific coverage and ancestry gaps to choose one content-driven search change with a zero-combat construction proof. Preserve group diversity while deciding how to spend count/filler requests; do not simply increase repetitions, protect every concentrated group, copy the controls into generation or use ability order as a knob. Any proposed policy change needs its own explicit implementation scope and frozen comparison; no further fights or fresh values are authorized here. Adoption stays Hold.

## Accounting, commands and changed files

New diagnostics before publication: **3.563 seconds**; carried-forward workload **534.628 seconds**. Final [completion](../TestResults/balance/tower-group-variation-trajectory-20260915/completion.json) includes publication and a conservative one-second receipt/seal allowance within 120 seconds new / 1,800 cumulative. Output is capped at 32 MiB new / 4 GiB cumulative, with the entire preceding retained chain included. All **8,494 indexed files across 21 earlier sealed packages** verified unchanged before/after.

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$audit = 'TestResults/balance/tower-group-variation-trajectory-20260915'
& $python -B "$audit/workflow.py" freeze
& $python -B "$audit/workflow.py" fixtures
& $python -B "$audit/workflow.py" analyze
& $python -B "$audit/workflow.py" verify
& $python -B "$audit/publish.py"
```

Commands ran once; do not rerun these sealed paths. Reproduction requires a separately frozen output and budget using the exact captured inputs. No command failed or was blocked. The unchanged backend scope retains its earlier 71 passing tests through `build/run-tests.ps1`; this audit changed only Python saved-data readers and Markdown, so no backend build/test repetition was needed.

Changed files: the new protocol/review, separate evidence package and six active Markdown handoffs. No harness/gameplay changes, configuration, migrations or deployment. All **482,506 reservations** remain, including v19's unused 512. Fixed ability order; no Kharad tuning, 129,536-fight confirmation or old-cap increase. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption **Hold** are unchanged.
