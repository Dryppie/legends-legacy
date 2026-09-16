# Practical incumbent pilot: completed; improvement not demonstrated

**The authorized pilot completed 1,408 fights and passed native reconstruction and independent saved-evidence verification. Its novel challenger won 167/256 (65.23%); both unchanged anchors won 161/256 (62.89%). The observed advantage is 2.34 percentage points against each anchor, below the prospectively required five points, and both adjusted paired intervals include zero. The fixed decision is `ImprovementNotDemonstrated`. This pilot is closed.**

| Team | Confirmation wins | Win rate | Adjusted Wilson interval |
| --- | ---: | ---: | ---: |
| Selected challenger | 167/256 | 65.23% | 56.91–72.72% |
| Anchor 040e | 161/256 | 62.89% | 54.52–70.55% |
| Anchor 49f6 | 161/256 | 62.89% | 54.52–70.55% |

Anchor labels abbreviate `team-040e60d3dbc5c127321653c47ed3a9d3` and `team-49f6979895354870c89362d4abf214bb`. The selected party is `a0f9ffe3bcccabf28f1d7b3c2290075e94efb06b59df88a87af74a2be5134462`. Its exact current-cohort recipe is in the [selected scenario export](../TestResults/balance/tower-incumbent-practical-pilot-20260916/study/exports/cell-50ab9e8905d1d211df904c1100df16fa4680f96022a62ad1879234a74e0d19ec.json); all three recipes and origins are in the [frozen confirmation family](../TestResults/balance/tower-incumbent-practical-pilot-20260916/study/confirmation-freeze.json).

The adjusted paired intervals for challenger minus anchor are **−12.07 to +16.62 percentage points** against 040e and **−11.06 to +15.62 points** against 49f6. There were 66 gained /60 lost wins against 040e and 54 gained /48 lost against 49f6. The challenger clears the 10% adjusted win-rate lower-bound requirement, but fails both the observed-gain and positive paired-bound requirements. These data establish neither superiority nor equivalence to the anchors. The fixed family remains seven Bernoulli quantities with approximate Bonferroni-Wilson adjustment. See the [independent audit](../TestResults/balance/tower-incumbent-practical-pilot-20260916/independent-audit.json) and [native decision](../TestResults/balance/tower-incumbent-practical-pilot-20260916/pilot-summary.json).

## What nomination and selection did

The unchanged constructor evaluated 64 parties from 67 proposals; three proposals were duplicates. It stopped at the candidate budget. The new policy nominated the two exact admitted incumbents and the two highest-ranked distinct challengers, then froze their discovery-rank order.

| Nominee | Discovery rank | Discovery wins | Selection wins |
| --- | ---: | ---: | ---: |
| Selected challenger a0f9 | 1 | 7/8 | 26/32 |
| Other challenger 5645 | 2 | 7/8 | 20/32 |
| Anchor 49f6 | 20 | 4/8 | 23/32 |
| Anchor 040e | 26 | 4/8 | 23/32 |

Both anchors reached selection despite their low discovery ranks. Incumbent eligibility therefore worked as specified. Selection chose the challenger by wins; the zero-win health tie-break did not affect this decision. Its 81.25% selection rate fell to 65.23% on independent confirmation. This illustrates why selection scores are not confirmation strength; one trajectory cannot isolate selection noise from other search effects.

The selected challenger came from a `recombine` proposal with ancestry from anchor 040e. Its immediate proposal changed character slot 3 relative to its construction parent. This is practical search using supplied knowledge, not independent discovery from scratch. The other challenger came from a whole-character proposal with ancestry from both anchors. Exact IDs, proposal provenance, ranks and stage counts are retained in the [trajectory summary](../TestResults/balance/tower-incumbent-practical-pilot-20260916/trajectory-summary.json).

The earlier pilot's failed result remains intact. This run's stronger finalist point estimate cannot establish that the nomination change caused an improvement: construction roots and combat panels differ, and there is no contemporaneous comparison arm. Historical and current outcomes are not pooled. The second challenger's confirmation strength is unknown because it was not selected.

## Scope and stopping decision

Execution followed the [frozen protocol](Tower-Incumbent-Practical-Pilot-Protocol.md): `retained-composition-incumbents-v1`, one root, 64 evaluated parties, at most 256 proposals, four nominees, and one selected party plus both anchors. It used current floor-5 Kharad, ten level-40 characters with five Essences each, fixed Standard tier-1/rank-2 equipment, neutral identities, and fixed ordinal ability order. The pool contains 85 Essences /82 families. `OwnedCopies=null` assumes sufficient copies, so legality is conditional on that inventory assumption.

Discovery used 512 fights, selection 128 and confirmation 768. All 1,408 attempted fights completed. No diagnostics, replays, retry, resume, second root or extra samples followed. All three confirmation rates exceed 50% and remain visible. The separate generic balance assessment is **Fail**, with scientific exit code **1** and study status **Complete**; that is not an execution failure or the pilot's improvement rule.

**Close this pilot without another minor variant or a sample extension.** Retain the challenger recipe as measured evidence, with its uncertainty and ancestry, without promoting it as superior to the anchors. The nomination repair has preserved incumbent eligibility; the remaining practical strength advantage is unproven. This result does not establish search reliability, global optimality or general Tower balance. Adoption remains **Hold**; V19 reliability remains **Unresolved**, with its 253 required recipes and 512 unused confirmation values preserved.

## Verification, accounting and preservation

The complete sealed harness runtime and its 40 passing implementation checks were reused after verifying source/runtime identities. Only the isolated launcher/test project was restored and built. The 13 focused readiness cases passed through:

```powershell
build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/balance/tower-incumbent-practical-pilot-20260916/tests -Filter 'FullyQualifiedName~PilotTests|FullyQualifiedName~PilotHistoryIntegrationTests'
```

Readiness covered the exact contract, authorization and reservation interruption behavior, durable counters, once-only launch, all decision branches, distinct verification receipt property names, and full production history reconciliation. Recovery-aware `Refresh`/`Recheck` verified 189 history files and 483,343 exclusions; binding repeated the live reconciliation before deriving any fresh value. The pinned master/domain produced exactly 297 accepted values, with zero collisions. The complete exclusion union is now **483,640**. Original Pending/recovery artifacts and all previous reservations remain unchanged.

The bounded workflow ran `bindingAndRun main` once, then `verification native` and `verification independent`. Native `TowerBossStudy.VerifyAsync` reconstructed the saved study without combat and successfully serialized its receipt. The independent audit verified all 1,543 study files, both exact incumbent nominations, the top two challenger slots, selection and recipes, durable allocation/fight journals, saved confirmation outcomes and all seven statistical quantities. Both verification commands passed with zero new fights. No relevant verification command was blocked or left unrun; production rebuilds and repetition of the 40 implementation checks were intentionally outside this scope.

Readiness is charged **81.063 seconds**, including its fixed preparation allowance, under 90 seconds /16 MiB. Binding and the one run used **84.234 seconds**, under 1,200 seconds /300 MiB. Verification/publication is charged under 120 seconds /16 MiB. The approved 16-MiB transfer changes engineering/run byte ceilings to 496 MiB each, with no time transfer or overall increase. The [completion receipt](../TestResults/balance/tower-incumbent-practical-pilot-20260916/completion.json) carries prior charges forward, records conservative output allowances and reports remaining balances. Unused capacity does not authorize more work or combat.

Changed files are this review, the isolated execution/evidence package, the harness README and five current handoff notices. Final publication checks source/runtime pins, earlier sealed inventories, historical recovery bytes, unrelated dirty files, document links and scoped `git diff --check`. No production or gameplay code, content, migrations, persistent configuration or deployment changed during this pilot.
