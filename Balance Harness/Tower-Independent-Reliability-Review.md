# Tower independent-search reliability: coordinated cores — 12 September 2026

**The new search policy is implemented and verified, but it does not yet find competitive Kharad builds reliably.** All 12 generated finalists won **0/256** on fresh held-out seeds. Four saved controls won **20.31–28.13%**. The frozen exploratory advancement gate is **Fail: 0 of 3 passing restarts, with 2 required**. The full workload completed **8,708 fights in 190.20 seconds** of timed execution/reconstruction; **150 relevant tests passed**. About **302 MiB** is retained.

This increment targets the offline `LL/tools/BalanceHarness`. Kharad remains at **Health 3.04881408 / Power 3.85370128**. The fixed gear/untrained-Essence budget, retained catalogs and 10–50% acceptance policy are unchanged. The [complete 2,918-party confirmation](Tower-Staged-Confirmation-Review.md) and [checked local application](Tower-Kharad-Checked-Application-Review.md) retain their scopes. Neither this pilot nor that scoped balance Pass establishes near-optimal builds or overall Tower balance.

## Hypothesis and implementation

Inspection of the preceding staged discovery found that independent candidates repeated their most common Essence across a median of only three characters; retained-improvement candidates had a median of eight. The strongest saved recipes use repeated support and pressure components. This suggested that one- or two-character mutations struggle to reach coordinated party structures in a small search. It did not establish repetition as the cause of strength.

The new opt-in policy, `independent-coordinated-v2`, runs two methods: unchanged `constructive-joint` and experimental `coordinated-joint`. The comparator retains the same v1 random stream, construction, fitness and mutation operators. The default remains `independent-teams-v1`. Existing experiments do not silently change behavior.

The coordinated arm adds two operations:

- Fresh construction generates a legal prototype from content-derived capability weights, chooses a subset of its ordered Essences, shares that core across a randomly selected group of actual character positions, and fills remaining slots independently. Optional owned-copy limits constrain construction; hypothetical ownership is explicit in this pilot.
- `broadcast-core` copies a randomly selected subset of a generated donor's Essences into several generated recipients. It replaces conflicting source families, preserves exact slot counts, clones the parent and performs complete legality checks. Invalid and duplicate proposals are recorded and receive no battles.

There are no hardcoded creature/Essence IDs or saved recipes in these operations. Independent generator inputs exclude references, reference scores, actor identities and held-out schedules. The architecture hypothesis was informed by historical observations; its design was not blind to previous findings. Saved controls enter only the final validation family. Explicit supplied-team improvement remains a separate workflow and was not run here.

Runtime changes are [TowerBossGeneration.cs](../LL/tools/BalanceHarness/TowerBossGeneration.cs), [TowerBossPartyGenerator.cs](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs) and [TowerBossDiscoveryContract.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs). Version/method pairing and operator provenance are strict: `fresh-coordinated` requires zero parents, `broadcast-core` one generated parent, and both require the independent coordinated arm.

## Frozen experiment

The [protocol](../TestResults/balance/tower-independent-reliability-20260912/protocol.json), producing sources/binaries, content, schedules and controls were frozen before pilot combat. Preparation excluded **467,595** historical/reserved seeds. No retry or automatic resume was permitted. All inputs passed the producing-source and content checks before and after execution.

| Component | Allocation and actual use |
| --- | --- |
| Party budget | Kharad floor 5; 10 characters; 5 Essences each; level 40, tier 1, rank 2, Standard quality |
| Essence/equipment assumptions | Full allowed pool including Rare Essences; hypothetical ownership; fixed gear; level-1 unascended/unevolved Essences; no styles or contributions |
| Discovery | 3 fresh generation seeds × 2 methods × 96 evaluated parties × 8 shared fresh combat seeds = **4,608 fights** |
| Proposals | **606 total**, **576 evaluated**; 30 rejected/duplicate proposals consumed no fights |
| Frozen validation family | Top two parties per arm by discovery ranking, plus all four saved controls; **16 distinct canonical recipes** |
| Held-out validation | Every recipe × the same **256 fresh disjoint seeds** = **4,096 fights** |
| Diagnostic parity | Four repeats of old detailed control reports = **4 fights**, excluded from new rate estimates |
| Caps and actual | **8,708 / 8,708** starts and completions; 600 seconds; 1 GiB total package and 512 MiB per campaign; zero retries/lost attempts |
| Execution | Prepared mode, 32-record chunks, complete durable attempt journals and full zero-fight reconstruction |

Primary means discovery rank one for each arm, frozen before held-out outcomes. Rank-two builds are exploratory secondaries. No held-out reranking, search/validation pooling, sample extension or outcome-based rerun occurred. Discovery used 132.16 seconds; its full reconstruction 12.31 seconds; validation 42.34 seconds; validation reconstruction 2.69 seconds; four old-report replays 0.71 seconds. Their sum is **190.20 seconds (3 minutes 10 seconds)**. Preparation, code changes, builds, tests, analysis and documentation are outside this measured interval. Backend test simulations are also outside the pilot-fight accounting.

This is a bounded workload measurement, not a matched before/after speed comparison or a prediction for a 600,000-fight campaign. The preceding [bookkeeping profile](Tower-Bookkeeping-Performance-Review.md) improved targeted operations but did not establish whole-campaign speedup. That gate remains open before another large campaign.

## Results

The strongest discovery-ranked party in each arm had no search wins. Its mean remaining guardian health and held-out outcome were:

| Restart seed | Constructive guardian health remaining | Coordinated guardian health remaining | Constructive primary validation | Coordinated primary validation |
| --- | ---: | ---: | ---: | ---: |
| 363477769 | 80.62% | 51.83% | 0/256 | 0/256 |
| 1248877013 | 81.71% | 63.01% | 0/256 | 0/256 |
| 671520403 | 74.46% | 82.57% | 0/256 | 0/256 |

All six exploratory secondaries also won **0/256**. Across all 576 evaluated parties, discovery produced **zero clears**. Coordinated candidates repeated their most common Essence across a median of **8.5, 8.5 and 8** characters in the three restarts, versus **3, 3 and 4** in the comparator. There were **128 evaluated fresh coordinated parties and 23 evaluated broadcast mutations**. Thus the new policy reached the intended repeated structures, and reduced the leading party's remaining guardian health in two restarts, but failed to translate that into viable teams. These selected, eight-seed damage measurements are descriptive search signals, not confirmed win-rate gains.

The saved controls on the same fresh validation seeds were:

| Saved control | Wins / 256 | Observed rate | Joint-adjusted rate interval |
| --- | ---: | ---: | ---: |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | 64 | 25.00% | 17.49–34.39% |
| `team-693ffa8ec0b654154a06722aba06a968` | 52 | 20.31% | 13.55–29.31% |
| `team-1abe76ca1891d97a91d484f0a3662048` — predeclared comparison control | 72 | 28.13% | 20.19–37.71% |
| `team-3a69c759178064021dc5cf7124d7f4f5` | 67 | 26.17% | 18.50–35.64% |

Each zero-win generated cell has a joint-adjusted rate interval of **0–3.76%**. The gate required at least two coordinated discovery-selected primaries to have: a rate lower bound at least 10%; a paired improvement lower bound above zero versus their same-restart constructive primary; and a paired difference lower bound at least −10 percentage points versus the predeclared saved control. **Every restart failed all three conditions.**

Uncertainty follows the frozen protocol: total alpha 0.05 is split into 0.025 across 16 rate cells and 0.025 across six predeclared paired comparisons. Each paired interval subtracts two Bonferroni-Wilson discordance intervals with component alpha `0.025 / (6 × 2)`. This is approximate Wilson coverage. Each coordinated-primary difference versus its zero-win constructive primary is **0 percentage points**, interval **−3.57 to +3.57 points**. Versus the saved comparison control, each is **−28.13 points**, interval **−37.44 to −16.81 points**. Fresh controls have wins; the failure cannot be dismissed as a validation schedule on which nobody can win.

The ordinary balance evaluator reports **Pass for this 16-cell diagnostic family**, because the saved controls satisfy its rate requirements and no cell breaches the ceiling. That is different from independent-search reliability, which is **Fail**. It is not a fresh complete-family Tower acceptance, and no samples are pooled with the previous 2,918-party confirmation.

## Saved data and verification

All evaluated recipes, all 606 proposal records and generated ancestry remain in [discovery.json](../TestResults/balance/tower-independent-reliability-20260912/discovery/discovery.json). The [frozen selection](../TestResults/balance/tower-independent-reliability-20260912/validation-selection.json) retains every primary, secondary and control source. [Validated builds](../TestResults/balance/tower-independent-reliability-20260912/validated-builds.json) exports all 16 exact seed-free scenarios with measurements and content/execution identity for later reuse. Loading those recipes requires **zero battles**; changed content or new claims require appropriately scoped fresh validation. No catalog promotion occurred, so these pilot candidates do not automatically become the normal Tower Lab controls.

The approximately 302 MiB [output package](../TestResults/balance/tower-independent-reliability-20260912) retains compact outcomes, recipes, producing binaries, the minimal nonsecret input configuration and verification receipts. It copies only 16 allowlisted content files, not application configuration or historical archives. No old evidence was deleted. The exact package inventory and byte count are in the [final verification](../TestResults/balance/tower-independent-reliability-20260912/final-verification.json).

Both campaigns fully reconstructed with zero additional fights. All four old detailed reports matched, including their event content. The [independent analysis](../TestResults/balance/tower-independent-reliability-20260912/analysis.json) checks complete file inventories and hashes, decompressed chunk receipts, battle schedules, durable journal rows, accounting, legal recipes, parent/reference isolation, discovery ranking and the frozen selection. It verifies each validation outcome against compact records and independently calculates the intervals and failed gate. Producing gameplay assemblies, current content and both retained catalogs remain unchanged; prior sealed evidence is checked again at finalization.

The required wrapper passed **150 tests, zero failed or skipped**:

```powershell
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBossGenerationTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryRunTests|FullyQualifiedName~BalanceHarnessTowerBulkTests|FullyQualifiedName~BalanceHarnessTowerBossImprovementTests|FullyQualifiedName~BalanceHarnessTowerStagedTests|FullyQualifiedName~BalanceHarnessTowerCompactTests|FullyQualifiedName~BalanceHarnessTowerBossStudyTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests'
```

The [TRX receipt](../TestResults/balance/tower-independent-reliability-20260912/regression-tests.trx) records this result. New/expanded coverage verifies v1 comparator parity, v2 determinism, legal 4/5/7/10-slot parties, optional owned-copy limits, parent immutability, provenance rejection, reference invariance through real combat, reconstruction/export/replay, and compact cancellation/resume without replaying the completed prefix. The earlier 66-test focused run overlaps the final suite and is not an additional 66 unique tests.

The complete backend test build succeeded with five preexisting unrelated warnings and zero errors; the diagnostic driver built with zero warnings/errors. SDK-generated local files and local NuGet configuration required approved build/restore access, which resolved those sandbox restrictions. No required command remains blocked. `git -c core.safecrlf=false diff --check` passed.

Regression changes are [BalanceHarnessTowerBossGenerationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBossGenerationTests.cs), [BalanceHarnessTowerBossDiscoveryRunTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBossDiscoveryRunTests.cs) and [BalanceHarnessTowerBulkTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBulkTests.cs). Documentation changes are this review, the six active discovery/loadout/harness/policy plans and the harness README. Other preexisting working-tree changes were preserved. There are no gameplay edits, catalog promotions, migrations, dependency additions, service configuration changes, deployments or restarts. The only new configuration option is the explicit offline experiment policy described above.

## Next work

Use the saved event and mechanics evidence to examine trigger/recipient compatibility, sustain coverage and the timing of pressure versus party deaths. Broad intent tags and copying a random constructive core did not find the useful combinations in this budget. The next comparison should propose compatible cores from production mechanics and assess their complete-party behavior, rather than assuming that greater repetition implies strength. Keep any saved-team ablation explicitly diagnostic; independent generation must still exclude saved recipes as parents and fitness inputs. Mechanism claims require controlled evidence, not frequency alone.

Freeze that next small experiment, its primary-selection rule, controls, fresh schedules and resource caps before fights. Do not extend this failed sample, promote v2, increase gear/training, tune Kharad downward to fit weak generated teams, or launch another large campaign on the strength of these results. The successful saved recipes remain the current competitive controls. Floors 6–11, post-calibration challenges on floors 2–4 and practical acquisition coverage remain open.
