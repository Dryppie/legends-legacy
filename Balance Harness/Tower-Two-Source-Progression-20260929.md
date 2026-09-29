# Paired dungeon-source policies — 29 September 2026

## Finding

Spending both personally earned regional sigil families improved acquisition in this matched diagnostic. With perfect assumed idle victories, completion increased from **13/16 Mines-only histories to 16/16** under Mines-first-then-Catacombs spending. With four victories followed by one defeat, it increased from **8/16 to 14/16**. Completion means earning the same seven selected supply items within the fixed activity/attempt limits.

This supports using available dungeon sources before changing bosses or supply cadence. It does **not** establish normal-player acquisition time or universal success. The two unfinished two-family histories exhausted both sigil types. Early entry also wastes resources in these inputs: only **1/34 Catacombs attempts at the first checkpoint** succeeded. Entry readiness is the next useful comparison.

The [joint activity report](Tower-Joint-Activity-Progression-20260929.md) supplies the retained loot histories and limitations. Its older 9/16 four-in-five Mines result is historical; the correct comparator here is the **fresh paired 8/16 baseline**, using the new combat panel. No historical result was overwritten or repinned.

## Matched design

The [new fixture](../LL/tools/BalanceHarness/Fixtures/tower-source-policy.json) declares two fixed policies:

- `mines-only`: spend an available Goblin sigil; otherwise wait for the next checkpoint.
- `mines-first-either`: spend Goblin sigils while available, then Catacombs sigils. Never inspect hidden layouts or future results to choose a source.

Both wait until the same four-Essence quest gate, then use the same cumulative 6/24/72/240 **idle cadence-hour** checkpoints and the same twelve-total-attempt cap. The starting level, quest assumptions, idle outcome pattern, equipment-selection rule, seven-item purchase order and all exclusions remain unchanged. A failed attempt consumes the actual source family's sigil. Catacombs sigils are neither converted nor treated as successful Mines clears.

There are sixteen personal identities, two alternative idle outcome scenarios and two policy arms: **64 conditional histories**, not 64 independent player samples. Owners, random Armor Chest seeds, starting items, ordered Essences, ordinary reward identities and supply item identities are retained from the pinned joint-activity study. Each arm starts from the initial checkpoint; none continues an old censored character. The production reward processor regenerates matching loot prefixes as needed, and all personally earned equipment stays owned. Higher-budget alternatives may remain equipped under the unchanged declared inventory policy.

The [shared activity runner](../LL/tools/BalanceHarness/TowerActivityStudy.cs) was extended rather than duplicating the inventory/reward model. New dungeon panels are indexed by **family and within-family attempt ordinal**. A Catacombs detour therefore does not shift the next Mines seed. Panels are shared across matched policy arms. Production preparation and full dungeon execution include route choice, combat, rest, Vigor and failure. The independent audit found **138 pairs with identical inputs and identical complete run outputs**.

Before freezing, the owner checked **375 relevant content/reward source files** against the previous captured state. The final audit also checked every new history's overlapping loot windows against the pinned historical history and the other policy arm. New records include the chosen family and its attempt ordinal, allowing entry costs and panel use to be reconstructed independently.

## Outcomes

| Idle scenario | Spending policy | Completed / histories | Attempts | Successful runs / supply items | Censored histories |
| --- | --- | ---: | ---: | ---: | ---: |
| Perfect victories | Mines only | 13/16 | 125 | 106 | 3 |
| Perfect victories | Mines first, either family | 16/16 | 150 | 112 | 0 |
| Four victories, one defeat | Mines only | 8/16 | 114 | 95 | 8 |
| Four victories, one defeat | Mines first, either family | 14/16 | 144 | 108 | 2 |

Among the sixteen perfect-victory pairs, three newly completed, four already-successful histories completed at an earlier checkpoint, and nine completed at the same checkpoint. Among the four-in-five pairs, six newly completed, two already-successful histories completed earlier, six completed at the same checkpoint and two remained censored. No matched history lost completion in this sample. These small, correlated counts are descriptive, not population effect estimates.

All fresh Mines-only completions occurred at the 240-hour checkpoint. With both families, four perfect histories and three four-in-five histories completed at 72 hours; the remaining twelve and eleven completed at 240. These are coarse policy observation points, not estimates of the earliest possible completion or elapsed calendar time.

The two-family policy's actual source costs were:

| Idle scenario | Mines wins / attempts | Catacombs wins / attempts | Catacombs combat-readiness failures | Catacombs Vigor-attrition failures |
| --- | ---: | ---: | ---: | ---: |
| Perfect | 83/102 | 29/48 | 13 | 6 |
| Four-in-five | 78/97 | 30/47 | 13 | 4 |

Thus **36/95 Catacombs entries failed**, including ten after Vigor attrition. All were charged. The first 6-hour checkpoint spent 34 Catacombs sigils for one clear; later checkpoints produced 58 clears from 61 Catacombs entries. Gear and supply ownership change over the history, so this does not isolate a universal readiness threshold or justify assuming future perfect success.

The two remaining two-family histories both reached the activity cap, not the attempt cap:

| History | Attempts | Supply items earned | Remaining Goblin / Catacombs sigils |
| --- | ---: | ---: | ---: |
| Striker, path 0, four-in-five | 6 | 4 | 0 / 0 |
| Controller, path 3, four-in-five | 9 | 6 | 0 / 0 |

The first history had only six total entries available under the declared sources, so even perfect dungeon success could not buy seven selected items by that checkpoint. The second also lost entries to failures. Neither result proves that the complete production economy lacks sufficient entry sources: fragment assembly, caches, affordable purchases and other channels remain excluded.

Already-owned Rare controls used supplied entry and inventory and produced 32/32 Mines successes and 28/32 Catacombs successes. They are separate from all acquisition counts. No control rewards were added to a personal history.

Completed-path combat-only time ranged from 23.41–93.81 minutes for perfect Mines-only histories and 27.73–113.30 with both families; the four-in-five ranges were 23.63–103.19 and 28.63–134.63. These sum engine combat seconds, including failed attempts, and exclude leveling, player actions, navigation, breaks, waiting and other activity. **Do not present 240 cadence hours or these combat minutes as measured acquisition time.** There are zero measured player samples.

## Evidence and bounds

The [owner](analysis/run-tower-source-policy.py) froze inputs, historical history copies and runtime assemblies before starting one process through the existing Windows Job owner and repository test wrapper. The [declaration](../TestResults/tower-source-policy-owner-20260929/declaration.json) allows at most 768 acquisition attempts, 64 supplied controls, 112 replay runs, 64,000 room combats, 86,400 idle encounters per history, 900 seconds process time, 840 seconds native time, 256 MiB output and a 1 MiB log. There was no retry, sample extension, outcome-based policy adjustment or search.

Actual execution: **533 funded acquisition attempts**, **64 controls**, **112 matching full-run replays**, **5,805 room combats**, **421 supply items** and **5,106,240 modeled idle encounters** across the alternative histories. Another **249 reward windows** reproduced identical equipment and sigil output when split at different batch boundaries; those checks execute no fights. Process runtime was about 35.52 seconds with zero remaining children. Output totaled 91,293,788 bytes. Computer runtime is unrelated to gameplay duration.

The owner reserved **6,240 fresh dungeon values** against the prior 845,735-value exclusion union. It intentionally reused sixteen historical owner/armor identity seeds for matching; those are not new combat samples. Of the new reservations, 545 values were used and **5,695 unconsumed values remain excluded**. The union is now **851,975**. Preserve the [ledger](../TestResults/tower-source-policy-owner-20260929/seed-ledger.json), SHA-256 `f4a4b0006af3131302983b625b302f058029f8207758a91e14c72d2ded68eef5`, and its predecessors.

Retained local artifacts under ignored `TestResults`:

- [Request](../TestResults/tower-source-policy-owner-20260929/request.json), [source map](../TestResults/tower-source-policy-owner-20260929/source-map.json), [source compatibility](../TestResults/tower-source-policy-owner-20260929/source-compatibility.json), [process receipt](../TestResults/tower-source-policy-owner-20260929/process.json).
- [Results](../TestResults/tower-source-policy-study-20260929/result.json), SHA-256 `3916c04347c74282a5ee2ecd4accb152db4e562a7d685daf25ba92ab84e6c6c4`; adjacent files retain personal histories, inventories, entry costs and complete compressed run evidence.
- [Manifest](../TestResults/tower-source-policy-study-20260929/files.json), trusted pin **`327d5288fdf2a898efbf73870bf8b2d882106ef589d2ba778caff473e703a62e`**.
- [Independent audit](../TestResults/tower-source-policy-owner-20260929/independent-audit.json), [derived comparison tables](../TestResults/tower-source-policy-owner-20260929/derived-summary.json), [study TRX](../TestResults/tower-source-policy-owner-20260929/study-tests.trx), [study log](../TestResults/tower-source-policy-owner-20260929/study.log).

The frozen [file-only verifier](analysis/verify-tower-source-policy.py) checked **1,902 input hashes**, all 64 historical loot prefixes, source selection and actual sigil costs, retained personal gear, earned supply descriptors, quest gates, Vigor, failures, time accounting and exclusions. It checked **4,451 production-prepared diagnostic rooms**, **64 final preparations** and the 138 identical-input run pairs, without new combat. Production reward replays verify batching; the independent audit does not claim to reimplement the entire drop processor or combat engine.

## Verification and scope

Changed the shared activity runner; added the source-policy fixture, [focused tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessSourcePolicyTests.cs), owner and verifier. Added this report and updated the handoff, joint-activity report and supply implementation report. This continuation changed **no production code**. The earlier shared-runner source receipt remains historical; its archived source/runtime and outputs are preserved rather than repinned.

**372 backend tests passed**, with four intentional opt-in study skips; the owned study separately passed its one test. [Regression log](../TestResults/tower-source-policy-regression-20260929.log), [preserved regression TRX](../TestResults/tower-source-policy-build-20260929/regression-tests.trx). The initial sandbox build could not read local NuGet configuration; the required wrapper succeeded with escalation. No required command remains blocked. No frontend code changed or required frontend tests.

Recorded commands; do not reuse these historical output directories:

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-source-policy-build-20260929 -Filter 'FullyQualifiedName~BalanceHarnessSourcePolicyTests|FullyQualifiedName~BalanceHarnessActivityTests|FullyQualifiedName~BalanceHarnessBootstrapTests|FullyQualifiedName~BalanceHarnessDungeonAcquisitionTests|FullyQualifiedName~BalanceHarnessAcquisitionStudyTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~EquipmentAcquisitionTests|FullyQualifiedName~TowerEquipmentSupplyTests|FullyQualifiedName~Dungeon' *> TestResults/tower-source-policy-regression-20260929.log
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONDONTWRITEBYTECODE = '1'
& $python 'Balance Harness/analysis/run-tower-source-policy.py' --owner TestResults/tower-source-policy-owner-20260929 --output TestResults/tower-source-policy-study-20260929 --artifacts TestResults/tower-source-policy-build-20260929
& $python 'TestResults/tower-source-policy-owner-20260929/inputs/Balance Harness/analysis/verify-tower-source-policy.py' --owner TestResults/tower-source-policy-owner-20260929 --manifest-pin 327d5288fdf2a898efbf73870bf8b2d882106ef589d2ba778caff473e703a62e --receipt TestResults/tower-source-policy-owner-20260929/independent-audit.json
```

All concurrent LiveOps/analytics work and historical archives were preserved. The repeating gear curve, stronger inventory carryover and supported search `affinity-creation-with-benchmark-validation-v1` are unchanged. Guardian SHA-256 remains `5fdb290f74b71401a1ca56f7d89be49505077c9846ba139522e533c259947f05`. No application configuration, dependencies, migration, database access, API startup, seeding or deployment changed. No staging or commits occurred.

## Next work

Compare an explicit, visible **entry-readiness rule** with immediate checkpoint spending, using a new frozen declaration and fresh dungeon panel. The first-checkpoint failures justify investigating when to spend scarce sigils; do not select a threshold by repeatedly probing hidden combat outcomes or extending this completed study. A readiness gate alone cannot fix a history with fewer than seven total entries under the seven-purchase target.

For a fuller progression model, add ordinary dungeon rewards, mastery/level growth and other entry sources with their actual eligibility and costs. Keep the seven-selected-item target distinct from a minimally sufficient loadout, and obtain measured activity or an explicit desired farming budget before accepting player pace or changing supply cadence. These results do not justify retuning Tower bosses.

## Subsequent entry-readiness comparison

The [completed readiness study](Tower-Entry-Readiness-20260929.md) compares immediate both-family spending with complete equipped-slot coverage on a fresh shared panel. Failed entries fell **83 → 14**, completion stayed **16/16** under perfect idle victories and improved **14/16 → 15/16** under four-in-five victories. Its remaining failures were Catacombs Vigor attrition. One readiness history cleared all six available attempts but lacked a seventh entry. These are conditional results using the new immediate baseline; this archive and its source/runtime receipts remain historical and unchanged.

The extension passed 385 regression tests, its owned study and independent audit. Next work is production ordinary dungeon rewards and costed entry sources, with level/mastery growth still explicit. No readiness threshold sweep, boss change or measured player-time claim followed.
