# Supplied-search end-to-end quality gate

16 September 2026. Target: offline `LL/tools/BalanceHarness`.

**The prospective quality gate failed. Stop this block-search proposal before native admission or combat.** Fixing operator coverage did not establish better end-to-end search. At equal evaluation budgets, v2 block search recovered fewer cross-character optima and had greater aggregate regret than the retained-composition comparator. Keep the simpler comparator as the reference and preserve the historical recipes; do not follow this result with another ratio, seed or archive-size variation.

The complete matrix ran once: **nine cases, 54 arms, 3,456 fabricated evaluations and 6,265 emitted proposals**. Every arm completed its full 64-evaluation budget within 256 proposals. The execution/accounting test passed, but the separately persisted **scientific gate is Fail**. No production code changed. No native admission, fresh balance values, fights or replay followed.

## Frozen comparison and outcome

The [protocol](../TestResults/balance/tower-supplied-quality-gate-20260916/protocol.md) was written before implementation/execution. It uses two characters with four Essence slots each and four families with two alternatives each: exactly 256 legal parties. Each method receives the same all-zero supplied anchor, six initial fresh opportunities, scalar evaluator and constraints. Full adaptive search runs with real proposal randomness and archive feedback, using construction labels 17/31/47 under three prospectively fixed Essence-label mappings.

The additive landscape rewards each upgraded bit, with optimum 8. The other two sum four deceptive pair traps: a pair scores 2 for neither upgrade, 0 for exactly one, and 3 for both. One joins pairs within a character; the other joins corresponding families across characters. Both have optimum 12 and an all-zero local optimum at 8. Every one-bit edit from that anchor loses score. Exhaustive enumeration verifies the unique all-one optimum and the legal search space. Whole-character, recombination and later fresh routes all count; no particular ancestry is required.

| Landscape | Comparator optimum recoveries | V2 block optimum recoveries | Comparator total normalized regret | V2 block total normalized regret |
| --- | ---: | ---: | ---: | ---: |
| Additive | 7/9 | 7/9 | 0.250 | 0.250 |
| Within-character pairs | 3/9 | 4/9 | 0.500 | 0.500 |
| Cross-character pairs | 5/9 | 2/9 | 0.417 | 1.000 |
| All cases | 15/27 | 13/27 | **1.167** | **1.750** |

Normalized regret is `(oracle score - best score) / oracle score`, summed across cases; lower is better. These values are synthetic score gaps, not win rates or combat health findings. Across the 27 paired runs, block finished ahead twice, behind six times and tied 19 times.

The gate required complete budgets; optimum recovery with strict improvement after the initial seven opportunities in at least two of three block roots **for each coordinated landscape/map case**; and no worse aggregate normalized regret than the comparator. All initial batches were below the oracle optimum, so every reported optimum recovery also improved after initialization.

| Coordinated case | Block recoveries across the three roots | Required |
| --- | ---: | ---: |
| Within, identity labels | 2/3 | 2/3 |
| Within, reverse labels | 1/3 | 2/3 |
| Within, affine labels | 1/3 | 2/3 |
| Cross, identity labels | 1/3 | 2/3 |
| Cross, reverse labels | 0/3 | 2/3 |
| Cross, affine labels | 1/3 | 2/3 |

Budget completion passed. Coordinated recovery and aggregate non-regression both failed. The [independent audit](../TestResults/balance/tower-supplied-quality-gate-20260916/audit.json) reconstructs all evaluated recipes, family legality, fixed order, scores, initial/final bests, first best-finding operators, counts and gate decisions. It obtains exact total regrets **7/4** for block and **7/6** for the comparator. All full definitions/reports are retained under `results`; no roots or label mappings were dropped.

## What the traces can and cannot explain

The [saved counts](../TestResults/balance/tower-supplied-quality-gate-20260916/saved-trajectory-counts.json) show a substantial difference in where evaluations went. The comparator emitted 4,216 proposals to obtain 1,728 evaluations; block emitted 2,049 for the same 1,728 evaluations. The comparator evaluated 1,032 fresh parties versus block's 541. Fresh construction is scheduled by emitted proposal count, so the comparator's rejected/duplicate mutations let its fresh stream advance further before the common evaluation cap. Both arms stayed within the identical proposal ceiling; unused proposals were not converted into extra evaluations.

Block evaluated 619 Essence-block, 369 character-block and 172 donor-block proposals, alongside its fresh parties and 27 starts. Thus its failure cannot be described simply as an absence of coordinated operators. The results are compatible with differences in exploration, operator usefulness, parent selection or preservation of useful partial combinations. They do not causally isolate which factor dominates, and they do not justify selecting another ratio after seeing the outcome. Fewer proposals also do not establish a whole-run performance improvement.

This is a finite, deterministic engineering gate, without evaluator noise or an independent combat holdout. The three mappings reuse semantic landscapes; they are robustness checks, not independent gameplay replications. Failure ends this candidate under the agreed stopping rule. It does not prove coordinated search can never help or that the comparator is combat-optimal. The earlier controlled scheduling results and the previous failed 52/53 gate retain their separate meanings.

## Files, verification and stopping point

- [BalanceHarnessSuppliedQualityTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessSuppliedQualityTests.cs) adds the frozen matrix, exhaustive oracle checks and full report/gate export. Its passing test result certifies execution and accounting only; `quality-gate.json` explicitly carries Pass/Fail for search quality.
- `TestResults/balance/tower-supplied-quality-gate-20260916` contains the protocol, isolated fixture/admission helper build, source and dependency pins, full reports, audit, command logs, TRX and receipts. The admission helper compiled but was not invoked because the quality gate failed.
- Harness README and this review record the closed proposal. The v2 production implementation and default v1 behavior are unchanged.

Verification used `build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/balance/tower-supplied-quality-gate-20260916/tests -Filter "FullyQualifiedName~EssenceSystem.Tests.BalanceHarnessSuppliedQualityTests"`. The one matrix test passed; its quality decision failed. The isolated build succeeded with zero errors and one xUnit2031 style warning about the `Assert.Single` predicate overload. No compile/search retry occurred. The independent audit and scoped `git diff --check` passed. Current harness/gameplay binaries were reused only after verifying their hashes and the 1,337 pinned gameplay source files; neither was rebuilt.

Native admission is explicitly **not run**, as required by the failed gate. It is not a permission or tooling block. No combat request was prepared or reopened. The old anchor-admission receipt remains historical evidence for its own execution identity, not a current v2 admission receipt.

The [completion receipt](../TestResults/balance/tower-supplied-quality-gate-20260916/completion.json) records exact charges within the new **45-second / 64-MiB** scope and unchanged cumulative/engineering caps. Four predecessor packages, unrelated dirty work and all **483,046** reservations are preserved. All **891 authorized fresh values and up to 6,912 fights remain unused**. Adoption remains Hold; V19 reliability remains Unresolved.

No gameplay content, persistent configuration, migration or deployment changed.
