# Tower mechanics-core search and diagnostics — 12 September 2026

**The mechanics model is improved, but independent search still fails to find competitive Kharad teams.** All 12 generated finalists won **0/256**, while four saved controls won **17.58–32.03%**. The predeclared reliability gate is **Fail: 0 of 3 passing restarts, with 2 required**. The bounded workload completed **8,712 fights in 206.52 seconds (3 minutes 27 seconds)** of timed execution/reconstruction. **160 relevant tests passed**; approximately **331 MiB** is retained.

This increment changes only the offline harness and its tests/docs. Kharad remains at **Health 3.04881408 / Power 3.85370128**, using ten characters with five Essences each, level 40/tier 1/rank 2/Standard quality, fixed gear and level-1 unascended/unevolved Essences. The full allowed pool, including Rare Essences, assumes hypothetical ownership. Styles and contributions remain absent. Boss settings, retained catalogs, normal search defaults and the 10–50% policy are unchanged. The [2,918-party confirmation](Tower-Staged-Confirmation-Review.md) retains its original scoped Pass; search quality and near-optimality remain unestablished.

## What the saved evidence showed

The [pre-pilot diagnosis](../TestResults/balance/tower-mechanics-core-20260912/diagnosis.json) uses the sealed [v2 pilot](Tower-Independent-Reliability-Review.md), an old detailed winning replay and production source. On v2's held-out schedule, its three coordinated primaries left **58.75%, 63.02% and 82.95%** mean guardian health, with mean battle durations **86.99, 61.35 and 70.64 seconds**. The predeclared saved control left **17.56%** and lasted **107.17 seconds**, including victories. These descriptive comparisons identify a large gap; they do not identify its cause.

Source review resolves several distinctions:

- Enchanted Fairy supplies Corrosion and Stun, not healing. Its repeated presence is not by itself evidence for a sustain mechanism.
- Production `PerformBasicAttack(target)` emits `OnBasicAttack` with the actual attacking recipient as source, subject to action blocking and `CanBasicAttack`. Attack listeners are source-scoped. The old inventory's pair list did not connect forced basic-attack suppliers to these consumers.
- Status lifecycle listeners depend on event source, target or instigator. `AnyEnemyHasCondition` tests a condition when evaluated; it does not guarantee that every character immediately reevaluates on another actor's application.
- A condition applied to an enemy does not enable a negative condition predicate or a predicate on the owner's own condition state.

The selected old winning replay dealt **5,332** guardian health damage through Basic Attack and **4,189** through Poison; its first friendly death occurred at tick **849**. It is one historical winning example, not paired causal evidence or a new rate sample. The new policy's design was informed by historical findings, while its generation inputs remain independent of saved recipes and held-out outcomes.

## Implementation

[TowerMechanicCores.cs](../LL/tools/BalanceHarness/TowerMechanicCores.cs) derives **48 hypotheses: 31 condition pairs, 8 basic-attack pairs and 9 connected chains**. It follows activation edges from each eligible Essence's own abilities, keeps positive enemy-condition relationships, adds source-reviewed attack supplier/listener relationships, and checks self-application for status listeners. Summoned listeners and incompatible families are excluded from these same-owner proposals. Connected cores contain at most three Essences. Duplicate dependency paths do not multiply a recipe's sampling weight. Each core retains its evidence node keys and an explicit limitation covering timing, targets, chance, conditions, cooldowns, caps and blocking.

The opt-in `independent-mechanics-v3` policy requires `coordinated-joint` and `mechanics-joint`. The former is the unchanged v2 comparator. The new method samples core kinds and recipes, places each core on the same character, distributes two selected core groups across the full party, and fills remaining slots using the existing constructive algorithm. Its one-parent `mechanic-core` mutation installs a new core on one or more generated characters. Existing mutations, exploration and combat fitness remain available. Optional owned-copy limits are enforced; exhausted copies allow uncored filling, and an empty core pool explicitly records fallback to coordinated construction.

There are no hardcoded creature/Essence IDs or saved builds in this implementation. Core compatibility remains a structural proposal, not proof of synergy. Saved references, reference scores, actual actor identity vectors and held-out schedules stay outside generation. All generated parents and exact recipes remain recorded. `fresh-mechanics` requires zero parents, and `mechanic-core` requires one generated parent; both are rejected outside the independent mechanics arm.

Existing [TowerBossPartyGenerator.cs](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs), [TowerBossGeneration.cs](../LL/tools/BalanceHarness/TowerBossGeneration.cs) and [TowerBossDiscoveryContract.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs) integrate the policy. The mechanics record gains an optional `cores` field, omitted when null. V1/v2 serialization, proposal streams and the generic inventory's existing interaction list remain unchanged. V1 is still the default; neither Tower Lab nor the retained catalogs were promoted.

## Frozen workload and outcome

The [protocol](../TestResults/balance/tower-mechanics-core-20260912/protocol.json) freezes sources, producing binaries, content, diagnosis, core hypotheses, recipes, schedules and resource limits before combat. It excludes **467,864 historical/reserved seeds**. The scopes are:

| Stage | Frozen allocation and actual use |
| --- | --- |
| Discovery | 3 fresh generation restarts × 2 methods × 96 evaluated parties × 8 shared fresh seeds = **4,608 fights** |
| Proposals | **599 total**, **576 evaluated**; 23 invalid/duplicate proposals consumed no fights |
| Held-out selection | Top two per arm by discovery ranking, plus four saved controls; **16 distinct recipes**, frozen before validation |
| Validation | 16 recipes × 256 shared fresh disjoint seeds = **4,096 fights** |
| Old parity replays | Four saved detailed control reports, exactly matched = **4 fights** |
| New detailed replays | First fresh validation seed for each mechanics primary and the predeclared saved control, without outcome selection = **4 fights** |
| Total / resource caps | **8,712 starts and completions**; zero retries/lost attempts; 600 seconds; 1 GiB package, 512 MiB per campaign; 32-record chunks |

Measured phases: discovery **134.34 s**, discovery reconstruction **12.35 s**, validation **46.54 s**, validation reconstruction **2.72 s**, old parity **0.70 s**, and new detailed replay/verification **9.87 s**. Their unrounded sum is **206.52 s**. Preparation, development, builds, tests, analysis and documentation are outside this timing; backend test simulations are outside the 8,712-fight accounting. This is not a matched performance comparison or a prediction for a large campaign. The [whole-campaign performance gate](Tower-Bookkeeping-Performance-Review.md) remains open before scaling up.

Every discovered party had zero search clears. Leading discovery fitness and independently validated primaries were:

| Restart seed | V2 guardian health remaining | V3 guardian health remaining | V2 primary | V3 primary |
| --- | ---: | ---: | ---: | ---: |
| 1773901019 | 73.41% | 74.70% | 0/256 | 0/256 |
| 1236686265 | 57.30% | 55.86% | 0/256 | 0/256 |
| 1658555294 | 71.86% | 69.12% | 0/256 | 0/256 |

All six secondaries also won **0/256**. Each zero-win cell has a joint-adjusted rate interval **0–3.76%**. The saved controls on the same held-out schedule were:

| Saved control | Wins | Observed rate | Joint-adjusted rate interval |
| --- | ---: | ---: | ---: |
| `team-1abe76ca1891d97a91d484f0a3662048` | 69/256 | 26.95% | 19.17–36.47% |
| `team-3a69c759178064021dc5cf7124d7f4f5` | 82/256 | 32.03% | 23.63–41.78% |
| `team-693ffa8ec0b654154a06722aba06a968` | 45/256 | 17.58% | 11.32–26.28% |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | 60/256 | 23.44% | 16.16–32.71% |

The frozen gate requires at least two mechanics discovery-selected primaries to satisfy all three conditions: rate lower bound at least 10%; paired improvement lower bound above zero against the same-restart coordinated primary; and paired difference lower bound at least −10 percentage points against `team-1abe76ca1891d97a91d484f0a3662048`. **Every restart fails all three.** Each paired difference versus v2 is zero, interval **−3.57 to +3.57 points**. Each difference versus the saved comparison control is **−26.95 points**, interval **−36.20 to −15.78 points**.

The uncertainty budget remains as frozen: alpha 0.05 split into 0.025 over all 16 rate cells and 0.025 over six paired comparisons; each paired interval subtracts two Bonferroni-Wilson discordance bounds with component alpha `0.025 / 12`. Wilson coverage is approximate. Primary selection uses discovery only; secondaries are exploratory. There was no held-out reranking, sample pooling, sample extension or automatic rerun.

The ordinary evaluator's **Pass for this 16-cell diagnostic family** reflects the saved controls' rates and the absence of ceiling breaches. Independent-search reliability is **Fail**. The diagnostic Pass is not new complete-family acceptance and does not update the original 2,918-party confirmation.

## What the new detailed replays add

All four fixed-seed replays were defeats, including the saved control; they were not selected for favorable outcomes. The three mechanics primaries lost their first character at ticks **420, 379 and 359**, versus **579** for the saved control, and ended at ticks **680, 692 and 701**, versus **850**. They recorded guardian damage from Basic Attack and Poison, but this does not attribute the damage to the newly proposed relationships or prove synergy. The [replay diagnostics](../TestResults/balance/tower-mechanics-core-20260912/replay-diagnostics.json) preserve damage sources, activation counts, recovery, deaths and complete 256-seed descriptive summaries.

Across validation, the mechanics primaries left mean guardian health **75.58%, 54.85% and 69.23%**, versus **15.78%** for the predeclared control. Their mean battle durations were **69.87, 65.98 and 71.16 seconds**, versus **109.24 seconds**. Earlier party losses are consistent with a whole-party coverage problem, but neither those observations nor the single-seed replay establishes its cause. Adding source-compatible cores alone did not solve the search problem.

## Saved work, verification and next step

All 599 proposal records, 576 evaluated recipes, parent/core provenance and compact outcomes remain saved. [Validated builds](../TestResults/balance/tower-mechanics-core-20260912/validated-builds.json) exports all 16 exact seed-free scenarios and their measurements. Loading these builds requires **zero battles**. Future changed-content or strength claims need fresh scoped validation. The approximate **331 MiB** [package](../TestResults/balance/tower-mechanics-core-20260912) retains producing binaries, core evidence, minimal nonsecret settings, 16 allowlisted content files and eight replay receipts/reports. No historical archive was copied or deleted, and no catalog promotion occurred.

Both campaigns fully reconstructed without combat. The independent [analysis](../TestResults/balance/tower-mechanics-core-20260912/analysis.json) verifies manifests and exact inventories, chunk hashes, ordered schedules, durable journals, fight accounting, legal recipes, ancestry/reference isolation and discovery-selected primaries; it recomputes the held-out intervals and gate. The four new detailed summaries match their compact originals. All four old detailed reports match. [Final verification](../TestResults/balance/tower-mechanics-core-20260912/final-verification.json) checks current content/catalogs/game assemblies, producing sources, earlier sealed evidence, tests and documentation.

The required wrapper passed **160 tests, zero failed or skipped**:

```powershell
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerMechanicCoreTests|FullyQualifiedName~BalanceHarnessTowerBossGenerationTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryRunTests|FullyQualifiedName~BalanceHarnessTowerBulkTests|FullyQualifiedName~BalanceHarnessTowerBossImprovementTests|FullyQualifiedName~BalanceHarnessTowerStagedTests|FullyQualifiedName~BalanceHarnessTowerCompactTests|FullyQualifiedName~BalanceHarnessTowerBossStudyTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests'
```

[BalanceHarnessTowerMechanicCoreTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerMechanicCoreTests.cs) covers extraction/evidence, negative and self-target rejection, optional metadata serialization, v2 comparator parity, v3 determinism, 4/5/7/10-slot legality, owned-copy limits, fallback, parent immutability and provenance. Expanded [discovery tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBossDiscoveryRunTests.cs) and [bulk tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBulkTests.cs) verify reference invariance through real combat, export/replay/reconstruction and interrupted resume without rerunning committed trials. The [TRX](../TestResults/balance/tower-mechanics-core-20260912/regression-tests.trx) is retained.

Harness and diagnostic builds passed with zero warnings/errors. The full backend test build had five preexisting unrelated warnings and zero errors. A package-free diagnostic restore initially could not read local NuGet configuration; approved access resolved it using empty package sources. No required command remains blocked. `git -c core.safecrlf=false diff --check` passed. Runtime changes are the four harness files linked above; regression changes are the three linked test files. This review, six active plans/policies and the harness README are updated. Preexisting working-tree changes are preserved. There are no gameplay edits, dependency additions, migrations, service configuration changes, deployments or restarts; v3 is an explicit offline experiment option.

Next, freeze a small **saved-team ablation** that changes one component or coverage factor at a time while preserving the remaining party and gear. Pair its seeds and predeclare comparisons, resource caps and reporting of all variants. Measure clears, guardian progress, character-loss timing and pressure/recovery to determine which whole-party functions the independent proposals lack. Label this work reference-derived diagnosis; it is not independent discovery or a new balance acceptance. Use the findings to design a general content-derived search change and test that separately with fresh validation. Do not extend these failed samples, promote v2/v3, or reduce Kharad's strength to accommodate weak generated teams. Floors 6–11, later challenges on floors 2–4 and practical acquisition coverage remain open.
